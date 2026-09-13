using System.Collections.Generic;
using System.Reflection;
using AsiActionEngine.RunTime.GValueEquation;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    /// <summary>
    /// 运行时把调用者（IEquationExposedHolder）的暴露绑定应用到被引用公式的内部 LocalXxxParams，
    /// 并在求值结束后还原，防止污染共享的公式配置。
    ///
    /// 设计：所有 GraphEvent_GValue_GEquation* 的 Init 同步执行，因此 Apply / Restore 配对的
    /// "覆写-还原" 在单帧内不会并发；但必须通过 finally 配对，防止异常导致未还原。
    ///
    /// 零 GC 设计：Saved 状态以 SavedBuffer class 存在，由各 caller 作为 [NonSerialized]
    /// 字段持有；Apply 只向其内部 List 追加、Restore 只 Clear，稳态后不再分配。
    /// </summary>
    public static class EquationExposedParamRuntime
    {
        public struct SavedSlot<T>
        {
            public int typedIdx;
            public T orig;
            public bool existedInList;
        }

        /// <summary>
        /// 可复用的"原值暂存+目标指针"容器。由每个 GraphEvent_GValue_GEquation* caller 作为
        /// [NonSerialized] 字段常驻持有；List 首次扩容后一直复用其底层数组，稳态 0 分配。
        /// </summary>
        public sealed class SavedBuffer
        {
            public INodeEdiDataHolder Target;
            public readonly List<SavedSlot<int>>   Ints   = new List<SavedSlot<int>>();
            public readonly List<SavedSlot<float>> Floats = new List<SavedSlot<float>>();
            public readonly List<SavedSlot<bool>>  Bools  = new List<SavedSlot<bool>>();
            public readonly List<SavedSlot<string>> Strings = new List<SavedSlot<string>>();

            internal void Reset()
            {
                Target = null;
                Ints.Clear();
                Floats.Clear();
                Bools.Clear();
                Strings.Clear();
            }
        }

        /// <summary>
        /// 把调用者（IEquationExposedHolder）的 pin 求值结果，临时覆写到 evalPart 所在状态机的公式内部 LocalXxxParams。
        /// 所有暂存数据写入 buffer，由调用方在 finally 里配对 Restore。
        /// </summary>
        /// <param name="caller">持有 ExposedBindings 的调用者节点</param>
        /// <param name="callerPart">调用者当前所处的 part（pin 在此上下文求值）</param>
        /// <param name="evalPart">公式实际求值使用的 part（通过它找到公式实例并覆写其 Lists）</param>
        /// <param name="time">当前 machine time</param>
        /// <param name="buffer">调用方提供的复用缓冲（不能为 null）</param>
        public static void Apply(IEquationExposedHolder caller, ActionStatePart callerPart, ActionStatePart evalPart, ActionMachineTime time, SavedBuffer buffer)
        {
            if (buffer == null) return;
            buffer.Reset();

            if (caller == null) return;
            List<EquationExposedParamBinding> bindings = caller.ExposedBindings;
            if (bindings == null || bindings.Count == 0) return;

            GEquation equation = caller.GEquationVal;
            if (equation == null) return;

            if (!TryResolveEquationHolder(evalPart, equation, out INodeEdiDataHolder holder) || holder == null)
                return;

            NodeEdiData node = holder.NodeEdiData;
            if (node == null || node.localParams == null || node.localParams.Count == 0) return;

            buffer.Target = holder;

            for (int i = 0; i < bindings.Count; i++)
            {
                EquationExposedParamBinding b = bindings[i];
                if (b == null || b.pin == null) continue;
                if (string.IsNullOrEmpty(b.paramName)) continue;

                if (!TryLocate(node, b.paramName, b.paramType, out int typedIdx)) continue;

                b.pin.Init(callerPart, time);

                switch (b.paramType)
                {
                    case EBluePrintLocalParamType.Int:
                    {
                        if (b.pin is Graph.BluePrint_Int ip)
                        {
                            List<int> list = holder.LocalIntParams;
                            if (list == null) continue;
                            SaveAndWriteInt(list, typedIdx, ip.value, buffer.Ints);
                        }
                        break;
                    }
                    case EBluePrintLocalParamType.Float:
                    {
                        if (b.pin is Graph.BluePrint_Float fp)
                        {
                            List<float> list = holder.LocalFloatParams;
                            if (list == null) continue;
                            SaveAndWriteFloat(list, typedIdx, fp.value, buffer.Floats);
                        }
                        break;
                    }
                    case EBluePrintLocalParamType.Bool:
                    {
                        if (b.pin is Graph.BluePrint_Bool bp)
                        {
                            List<bool> list = holder.LocalBoolParams;
                            if (list == null) continue;
                            SaveAndWriteBool(list, typedIdx, bp.value, buffer.Bools);
                        }
                        break;
                    }
                    case EBluePrintLocalParamType.String:
                    {
                        if (b.pin is Graph.BluePrint_String sp)
                        {
                            List<string> list = holder.LocalStringParams;
                            if (list == null) continue;
                            SaveAndWriteString(list, typedIdx, sp.value, buffer.Strings);
                        }
                        break;
                    }
                }
            }
        }

        public static void Restore(SavedBuffer buffer)
        {
            if (buffer == null) return;
            if (buffer.Target == null) { buffer.Reset(); return; }

            List<int> intList = buffer.Target.LocalIntParams;
            if (intList != null)
            {
                for (int i = buffer.Ints.Count - 1; i >= 0; i--)
                {
                    SavedSlot<int> s = buffer.Ints[i];
                    if (s.existedInList)
                    {
                        if (s.typedIdx >= 0 && s.typedIdx < intList.Count) intList[s.typedIdx] = s.orig;
                    }
                    else
                    {
                        if (s.typedIdx >= 0 && s.typedIdx < intList.Count) intList.RemoveAt(s.typedIdx);
                    }
                }
            }

            List<float> floatList = buffer.Target.LocalFloatParams;
            if (floatList != null)
            {
                for (int i = buffer.Floats.Count - 1; i >= 0; i--)
                {
                    SavedSlot<float> s = buffer.Floats[i];
                    if (s.existedInList)
                    {
                        if (s.typedIdx >= 0 && s.typedIdx < floatList.Count) floatList[s.typedIdx] = s.orig;
                    }
                    else
                    {
                        if (s.typedIdx >= 0 && s.typedIdx < floatList.Count) floatList.RemoveAt(s.typedIdx);
                    }
                }
            }

            List<bool> boolList = buffer.Target.LocalBoolParams;
            if (boolList != null)
            {
                for (int i = buffer.Bools.Count - 1; i >= 0; i--)
                {
                    SavedSlot<bool> s = buffer.Bools[i];
                    if (s.existedInList)
                    {
                        if (s.typedIdx >= 0 && s.typedIdx < boolList.Count) boolList[s.typedIdx] = s.orig;
                    }
                    else
                    {
                        if (s.typedIdx >= 0 && s.typedIdx < boolList.Count) boolList.RemoveAt(s.typedIdx);
                    }
                }
            }

            List<string> stringList = buffer.Target.LocalStringParams;
            if (stringList != null)
            {
                for (int i = buffer.Strings.Count - 1; i >= 0; i--)
                {
                    SavedSlot<string> s = buffer.Strings[i];
                    if (s.existedInList)
                    {
                        if (s.typedIdx >= 0 && s.typedIdx < stringList.Count) stringList[s.typedIdx] = s.orig;
                    }
                    else
                    {
                        if (s.typedIdx >= 0 && s.typedIdx < stringList.Count) stringList.RemoveAt(s.typedIdx);
                    }
                }
            }

            buffer.Reset();
        }

        private static void SaveAndWriteInt(List<int> list, int typedIdx, int newVal, List<SavedSlot<int>> savedList)
        {
            if (typedIdx < 0) return;

            bool existed = typedIdx < list.Count;
            int orig = existed ? list[typedIdx] : 0;
            savedList.Add(new SavedSlot<int> { typedIdx = typedIdx, orig = orig, existedInList = existed });

            while (list.Count <= typedIdx) list.Add(0);
            list[typedIdx] = newVal;
        }

        private static void SaveAndWriteFloat(List<float> list, int typedIdx, float newVal, List<SavedSlot<float>> savedList)
        {
            if (typedIdx < 0) return;

            bool existed = typedIdx < list.Count;
            float orig = existed ? list[typedIdx] : 0f;
            savedList.Add(new SavedSlot<float> { typedIdx = typedIdx, orig = orig, existedInList = existed });

            while (list.Count <= typedIdx) list.Add(0f);
            list[typedIdx] = newVal;
        }

        private static void SaveAndWriteBool(List<bool> list, int typedIdx, bool newVal, List<SavedSlot<bool>> savedList)
        {
            if (typedIdx < 0) return;

            bool existed = typedIdx < list.Count;
            bool orig = existed ? list[typedIdx] : false;
            savedList.Add(new SavedSlot<bool> { typedIdx = typedIdx, orig = orig, existedInList = existed });

            while (list.Count <= typedIdx) list.Add(false);
            list[typedIdx] = newVal;
        }

        private static void SaveAndWriteString(List<string> list, int typedIdx, string newVal, List<SavedSlot<string>> savedList)
        {
            if (typedIdx < 0) return;

            bool existed = typedIdx < list.Count;
            string orig = existed ? list[typedIdx] : string.Empty;
            savedList.Add(new SavedSlot<string> { typedIdx = typedIdx, orig = orig, existedInList = existed });

            while (list.Count <= typedIdx) list.Add(string.Empty);
            list[typedIdx] = newVal;
        }

        /// <summary>
        /// 在 NodeEdiData.localParams 中按 paramName+paramType 定位，返回该类型下的子索引（typed index）。
        /// </summary>
        private static bool TryLocate(NodeEdiData node, string paramName, EBluePrintLocalParamType paramType, out int typedIdx)
        {
            typedIdx = -1;
            int typed = 0;
            for (int i = 0; i < node.localParams.Count; i++)
            {
                BluePrintLocalParamDef p = node.localParams[i];
                if (p == null) continue;
                if (p.paramType != paramType) continue;
                if (p.paramName == paramName)
                {
                    typedIdx = typed;
                    return true;
                }
                typed++;
            }
            return false;
        }

        /// <summary>
        /// 从 ActionStateMachine.Equations 取出被引用公式，并反射读取其内部 GraphEvent 属性（为 GraphEvent_NoValue_*，即 INodeEdiDataHolder）。
        /// </summary>
        private static bool TryResolveEquationHolder(ActionStatePart part, GEquation equation, out INodeEdiDataHolder holder)
        {
            holder = null;
            if (part == null || part.ActionStateMachine == null) return false;
            if (part.ActionStateMachine.Equations == null) return false;
            if (!part.ActionStateMachine.Equations.TryGetValue(equation.mValueGroupIndex, out EngineEquation eeq)) return false;
            if (eeq == null || eeq.mGValueEquation == null) return false;
            if (equation.mValueIndex >= eeq.mGValueEquation.Length) return false;

            GValueEquation_Part eqPart = eeq.mGValueEquation[equation.mValueIndex];
            if (eqPart == null) return false;

            object graphEvent = GetGraphEventInstance(eqPart);
            holder = graphEvent as INodeEdiDataHolder;
            return holder != null;
        }

        private static readonly Dictionary<System.Type, PropertyInfo> s_GraphEventPropertyCache
            = new Dictionary<System.Type, PropertyInfo>();

        private static object GetGraphEventInstance(GValueEquation_Part eqPart)
        {
            System.Type t = eqPart.GetType();
            if (!s_GraphEventPropertyCache.TryGetValue(t, out PropertyInfo pi))
            {
                pi = t.GetProperty("GraphEvent", BindingFlags.Public | BindingFlags.Instance);
                s_GraphEventPropertyCache[t] = pi;
            }
            if (pi == null) return null;
            return pi.GetValue(eqPart);
        }
    }
}
