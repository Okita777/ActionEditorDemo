using System.Collections.Generic;
using AsiActionEngine.RunTime.GraphVal;
using AsiActionEngine.RunTime.GValueEquation;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GEquation : GValue
    {
        private bool TryGetPart(ActionStatePart _part, out GValueEquation_Part _equation)
        {
            _equation = null;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EngineDebug.LogWarning("非运行模式下无法获取公式, 将返回默认值");
                return false;
            }
#endif
            if (_part.ActionStateMachine.Equations is null)
            {
                EngineDebug.LogError("公式组获取错误，公式组未加载");
                return false;
            }
            if (!_part.ActionStateMachine.Equations.TryGetValue(mValueGroupIndex, out EngineEquation _eeq))
            {
                EngineDebug.LogError($"公式组获取错误，不存在公式组【{mValueGroupIndex}】");
                return false;
            }
            if (_eeq.mGValueEquation.Length <= mValueIndex)
            {
                EngineDebug.LogError($"公式配置错误，组【{mValueGroupIndex}】，索引值【{mValueIndex}】超出公式最大长度【{_eeq.mGValueEquation.Length}】");
                return false;
            }
            _equation = _eeq.mGValueEquation[mValueIndex];
            return true;
        }

        public float value(ActionStatePart _part, ActionMachineTime _time)
        {
            if (!TryGetPart(_part, out GValueEquation_Part eq)) return 0f;
            if (eq is GValueEquation_FloatPart floatPart) return floatPart.Value(_part, _time, false);
            EngineDebug.LogError($"[GEquation] 公式类型不匹配，期望Float，实际为{eq.ReturnType}，组【{mValueGroupIndex}】索引【{mValueIndex}】");
            return 0f;
        }
        public bool Graph_Float(out GraphEvent_NoValue_Float value)
        {

            value = null;
            return false;
        }

        public int intValue(ActionStatePart _part, ActionMachineTime _time)
        {
            if (!TryGetPart(_part, out GValueEquation_Part eq)) return 0;
            if (eq is GValueEquation_IntPart intPart) return intPart.Value(_part, _time, false);
            EngineDebug.LogError($"[GEquation] 公式类型不匹配，期望Int，实际为{eq.ReturnType}，组【{mValueGroupIndex}】索引【{mValueIndex}】");
            return 0;
        }

        public bool boolValue(ActionStatePart _part, ActionMachineTime _time)
        {
            if (!TryGetPart(_part, out GValueEquation_Part eq)) return false;
            if (eq is GValueEquation_BoolPart boolPart) return boolPart.Value(_part, _time, false);
            EngineDebug.LogError($"[GEquation] 公式类型不匹配，期望Bool，实际为{eq.ReturnType}，组【{mValueGroupIndex}】索引【{mValueIndex}】");
            return false;
        }

        public string stringValue(ActionStatePart _part, ActionMachineTime _time)
        {
            if (!TryGetPart(_part, out GValueEquation_Part eq)) return string.Empty;
            if (eq is GValueEquation_StringPart stringPart) return stringPart.Value(_part, _time, false);
            EngineDebug.LogError($"[GEquation] 公式类型不匹配，期望String，实际为{eq.ReturnType}，组【{mValueGroupIndex}】索引【{mValueIndex}】");
            return string.Empty;
        }

        public TargetUnit unitValue(ActionStatePart _part, ActionMachineTime _time)
        {
            if (!TryGetPart(_part, out GValueEquation_Part eq)) return null;
            if (eq is GValueEquation_UnitPart unitPart) return unitPart.Value(_part, _time, false);
            EngineDebug.LogError($"[GEquation] 公式类型不匹配，期望Unit，实际为{eq.ReturnType}，组【{mValueGroupIndex}】索引【{mValueIndex}】");
            return null;
        }

        public Vector3 vector3Value(ActionStatePart _part, ActionMachineTime _time)
        {
            if (!TryGetPart(_part, out GValueEquation_Part eq)) return Vector3.zero;
            if (eq is GValueEquation_Vector3Part v3Part) return v3Part.Value(_part, _time, false);
            EngineDebug.LogError($"[GEquation] 公式类型不匹配，期望Vector3，实际为{eq.ReturnType}，组【{mValueGroupIndex}】索引【{mValueIndex}】");
            return Vector3.zero;
        }

        public PointData pointValue(ActionStatePart _part, ActionMachineTime _time)
        {
            if (!TryGetPart(_part, out GValueEquation_Part eq)) return default;
            if (eq is GValueEquation_PointPart pointPart) return pointPart.Value(_part, _time, false);
            EngineDebug.LogError($"[GEquation] 公式类型不匹配，期望Point，实际为{eq.ReturnType}，组【{mValueGroupIndex}】索引【{mValueIndex}】");
            return default;
        }

        public List<float> groupFloatValue(ActionStatePart _part, ActionMachineTime _time)
        {
            if (!TryGetPart(_part, out GValueEquation_Part eq)) return null;
            if (eq is GValueEquation_GroupFloatPart gfPart) return gfPart.Value(_part, _time, false);
            EngineDebug.LogError($"[GEquation] 公式类型不匹配，期望GroupFloat，实际为{eq.ReturnType}，组【{mValueGroupIndex}】索引【{mValueIndex}】");
            return null;
        }

        public List<int> groupIntValue(ActionStatePart _part, ActionMachineTime _time)
        {
            if (!TryGetPart(_part, out GValueEquation_Part eq)) return null;
            if (eq is GValueEquation_GroupIntPart giPart) return giPart.Value(_part, _time, false);
            EngineDebug.LogError($"[GEquation] 公式类型不匹配，期望GroupInt，实际为{eq.ReturnType}，组【{mValueGroupIndex}】索引【{mValueIndex}】");
            return null;
        }

        public List<ActionEngine_Unit> groupUnitValue(ActionStatePart _part, ActionMachineTime _time)
        {
            if (!TryGetPart(_part, out GValueEquation_Part eq)) return null;
            if (eq is GValueEquation_GroupUnitPart guPart) return guPart.Value(_part, _time, false);
            EngineDebug.LogError($"[GEquation] 公式类型不匹配，期望GroupUnit，实际为{eq.ReturnType}，组【{mValueGroupIndex}】索引【{mValueIndex}】");
            return null;
        }

        public override GValue Clone()
        {
#if UNITY_EDITOR
            GEquation n = new GEquation();
            n.mType = mType;
            n.mValueIndex = mValueIndex;
            n.mValueGroupIndex = mValueGroupIndex;
            return n;
#endif
            return this;
        }
    }
}
