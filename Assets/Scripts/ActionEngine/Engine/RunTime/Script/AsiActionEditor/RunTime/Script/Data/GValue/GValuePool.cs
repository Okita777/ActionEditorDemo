using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public class GValuePool
    {
        /// <summary>
        /// 池内某个可同步槽位的值发生变化。只给键不给值：订阅方按类型走对应 getter 取值，
        /// 避免为每种类型各开一个委托签名。
        /// </summary>
        public delegate void DOnValueChanged(EGValueType type, ushort group, ushort id);

        /// <summary>
        /// 可同步类型的写入通知。挂在池上而不是各个 GValue 子类的 SetValue 上，
        /// 是因为存在绕过 GValue 对象直接写池的路径（ActionEngineManager_GValue 的外部写入、
        /// GVMonitor 的聚合写入），只在 GValue 层挂钩会漏掉它们。
        /// </summary>
        public DOnValueChanged OnValueChanged;

        private Dictionary<ushort, EngineGValue> mEngineGValueDic =>
            EngineResourcesManager.Instance.GetEngineGValue();

        private Dictionary<(ushort, ushort), int> mGvalueDic_Int = new Dictionary<(ushort, ushort), int>();
        private Dictionary<(ushort, ushort), bool> mGvalueDic_Bool = new Dictionary<(ushort, ushort), bool>();
        private Dictionary<(ushort, ushort), float> mGvalueDic_Float = new Dictionary<(ushort, ushort), float>();
        private Dictionary<(ushort, ushort), string> mGvalueDic_String = new Dictionary<(ushort, ushort), string>();
        private Dictionary<(ushort, ushort), Vector3> mGvalueDic_Vector3 = new Dictionary<(ushort, ushort), Vector3>();
        private Dictionary<(ushort, ushort), byte> mGvalueDic_Enum = new Dictionary<(ushort, ushort), byte>();
        private Dictionary<(ushort, ushort), byte> mGvalueDic_PointData = new Dictionary<(ushort, ushort), byte>();
        private Dictionary<(ushort, ushort), byte> mGvalueDic_Unit = new Dictionary<(ushort, ushort), byte>();
        private Dictionary<(ushort, ushort), byte> mGvalueDic_Transform = new Dictionary<(ushort, ushort), byte>();

        private Dictionary<(ushort, ushort), int[]> mGvalueDic_GroupInt = new Dictionary<(ushort, ushort), int[]>();
        private Dictionary<(ushort, ushort), float[]> mGvalueDic_GroupFloat = new Dictionary<(ushort, ushort), float[]>();
        private Dictionary<(ushort, ushort), bool[]> mGvalueDic_GroupBool = new Dictionary<(ushort, ushort), bool[]>();
        private Dictionary<(ushort, ushort), string[]> mGvalueDic_GroupString = new Dictionary<(ushort, ushort), string[]>();

        private Dictionary<(ushort, ushort), List<PointData>> mGvalueDic_GroupPointData = new();
        private Dictionary<(ushort, ushort), List<ActionEngine_Unit>> mGvalueDic_GroupUnit = new();
        // GGroupUnit 持久存储列表（非 CreateUnits 环形池），避免池复用 Clear 清空 GV
        private readonly HashSet<List<ActionEngine_Unit>> mOwnedGroupUnitLists = new HashSet<List<ActionEngine_Unit>>();
        private Dictionary<(ushort, ushort), List<Transform>> mGvalueDic_GroupTransform = new();
        private readonly Dictionary<(ushort, ushort), GDictionaryStorage> mGvalueDic_Dictionary = new();

        //todo: ??????????????????????????????
        //private int[] mDefaltGI => new int[0];
        //private bool[] mDefaltGB => new bool[0];
        //private float[] mDefaltGF => new float[0];
        //private string[] mDefaltGS => new string[0];

        private List<PointData> mDefaltGP => new List<PointData>(32);
        //private List<PointData> mDefaltGP
        //{
        //    get {
        //        return new List<PointData>(); 
        //    }
        //}
        private List<Transform> mDefaltGT => new();

        private T GetValue<T>(Dictionary<(ushort, ushort), T> dict, ushort group, ushort id, System.Func<EngineGValue, ushort, T> getInitialValue, T defaultValue)
        {
            var key = (group, id);
            if (dict.TryGetValue(key, out T value))
            {
                return value;
            }
            else
            {
                if (mEngineGValueDic.ContainsKey(group))
                {
                    T initialValue = getInitialValue(mEngineGValueDic[group], id);
                    dict.Add(key, initialValue);
                    return initialValue;
                }
                else
                {
#if UNITY_EDITOR
                    EngineDebug.LogWarning($"<color=#ffcc00>????GV???GV???????????</color>  [<color=#ff0000>{group}</color>]");
#endif
                    T initialValue = defaultValue;
                    dict.Add(key, initialValue);
                    return initialValue;
                }

            }
        }

        private T GetValue<T>(Dictionary<(ushort, ushort), T> dict, ushort group, ushort id, T defaultValue)
        {
            var key = (group, id);
            if (dict.TryGetValue(key, out T value))
            {
                return value;
            }
            else
            {
                dict.Add(key, defaultValue);
                return defaultValue;
            }
        }

        private void SetValue<T>(Dictionary<(ushort, ushort), T> dict, ushort group, ushort id, T value)
        {
            if (!dict.TryAdd((group, id), value))
                dict[(group, id)] = value;

            //var key = (group, id);
            //dict[key] = value;
        }

        /// <summary>
        /// 带变更通知的写入，仅用于可参与网络同步的类型。写入行为与 <see cref="SetValue{T}"/> 完全一致，
        /// 只是额外判定值是否真的变了。
        ///
        /// 数组类型走的是引用比较：换一个新数组会通知，拿 <c>GetGroupInt</c> 的返回值原地改元素不会。
        /// </summary>
        private void SetSyncableValue<T>(
            Dictionary<(ushort, ushort), T> dict,
            ushort group,
            ushort id,
            T value,
            EGValueType type)
        {
            bool changed = !dict.TryGetValue((group, id), out T old)
                           || !EqualityComparer<T>.Default.Equals(old, value);

            SetValue(dict, group, id, value);

            if (changed) OnValueChanged?.Invoke(type, group, id);
        }

        /// <summary>
        /// 遍历池中已实例化的可同步条目，供全量快照使用。
        /// 未被访问过的槽位不在池里，它们在各端都会懒加载出同一份定义默认值，不需要同步。
        /// </summary>
        public void VisitSyncableEntries(DOnValueChanged visitor)
        {
            if (visitor == null) return;

            foreach (var pair in mGvalueDic_Int) visitor(EGValueType.GInt, pair.Key.Item1, pair.Key.Item2);
            foreach (var pair in mGvalueDic_Float) visitor(EGValueType.GFloat, pair.Key.Item1, pair.Key.Item2);
            foreach (var pair in mGvalueDic_Bool) visitor(EGValueType.GBool, pair.Key.Item1, pair.Key.Item2);
            foreach (var pair in mGvalueDic_Enum) visitor(EGValueType.GEnum, pair.Key.Item1, pair.Key.Item2);
            foreach (var pair in mGvalueDic_GroupInt) visitor(EGValueType.GGroupInt, pair.Key.Item1, pair.Key.Item2);
            foreach (var pair in mGvalueDic_GroupFloat) visitor(EGValueType.GGroupFloat, pair.Key.Item1, pair.Key.Item2);
            foreach (var pair in mGvalueDic_GroupBool) visitor(EGValueType.GGroupBool, pair.Key.Item1, pair.Key.Item2);
        }

        /// <summary>本类型是否在网络同步的支持范围内。引擎不存在 GGroupEnum，enum 只有标量形态。</summary>
        public static bool IsSyncableType(EGValueType type)
        {
            switch (type)
            {
                case EGValueType.GInt:
                case EGValueType.GFloat:
                case EGValueType.GBool:
                case EGValueType.GEnum:
                case EGValueType.GGroupInt:
                case EGValueType.GGroupFloat:
                case EGValueType.GGroupBool:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 该槽位是否被标记为参与网络同步。组定义缺失时按默认常态放行。
        /// </summary>
        public bool IsNetSync(EGValueType type, ushort group, ushort id)
        {
            Dictionary<ushort, EngineGValue> _defines = mEngineGValueDic;
            if (_defines == null || !_defines.TryGetValue(group, out EngineGValue _define) || _define == null)
                return true;

            return _define.IsNetSync(type, id);
        }

        public void ResetAllValue()
        {
            //Debug.LogError("????GV");
            mGvalueDic_Int.Clear();
            mGvalueDic_Bool.Clear();
            mGvalueDic_Float.Clear();
            mGvalueDic_String.Clear();
            mGvalueDic_Vector3.Clear();
            mGvalueDic_Enum.Clear();
            mGvalueDic_PointData.Clear();
            mGvalueDic_Unit.Clear();
            mGvalueDic_Transform.Clear();

            mGvalueDic_GroupInt.Clear();
            mGvalueDic_GroupFloat.Clear();
            mGvalueDic_GroupBool.Clear();
            mGvalueDic_GroupString.Clear();

            mGvalueDic_GroupPointData.Clear();
            mGvalueDic_GroupUnit.Clear();
            mOwnedGroupUnitLists.Clear();
            mGvalueDic_GroupTransform.Clear();
            mGvalueDic_Dictionary.Clear();
        }

        // int???????
        public int GetInt(ushort group, ushort id) =>
            GetValue(mGvalueDic_Int, group, id, (g, i) => g.mEngineInt[i], 0);
        //public int GetInt(ushort group, ushort id)
        //{
        //    return GetValue(mGvalueDic_Int, group, id, (g, i) => g.mEngineInt[i], 0);
        //}

        public void SetInt(ushort group, ushort id, int value) =>
            SetSyncableValue(mGvalueDic_Int, group, id, value, EGValueType.GInt);
        //public void SetInt(ushort group, ushort id, int value)
        //{
        //    if ((group, id) == (7, 115))
        //    {
        //        EngineDebug.LogError($"<color=#ffcc00>????????????: </color> ????[{value}]");
        //    }
        //    SetValue(mGvalueDic_Int, group, id, value);
        //}

        // bool???????
        public bool GetBool(ushort group, ushort id) =>
            GetValue(mGvalueDic_Bool, group, id, (g, i) => g.mEngineBool[i], false);
        //public bool GetBool(ushort group, ushort id)
        //{
        //    if (mEngineGValueDic.TryGetValue(group, out EngineGValue value))
        //    {
        //        if(id >= value.mEngineBool.Length)
        //        {
        //            EngineDebug.LogError($"??????????? [{id}] {value.mEngineBool.Length}");
        //        }
        //    }
        //    return GetValue(mGvalueDic_Bool, group, id, (g, i) => g.mEngineBool[i], false);
        //}

        public void SetBool(ushort group, ushort id, bool value) =>
            SetSyncableValue(mGvalueDic_Bool, group, id, value, EGValueType.GBool);

        // float???????
        public float GetFloat(ushort group, ushort id) =>
            GetValue(mGvalueDic_Float, group, id, (g, i) => g.mEngineFloat[i], 0.0f);

        public void SetFloat(ushort group, ushort id, float value) =>
            SetSyncableValue(mGvalueDic_Float, group, id, value, EGValueType.GFloat);

        // string???????
        public string GetString(ushort group, ushort id) =>
            GetValue(mGvalueDic_String, group, id, (g, i) => g.mEngineString[i], "?????");

        public void SetString(ushort group, ushort id, string value) =>
            SetValue(mGvalueDic_String, group, id, value);

        // Vector3???????
        public Vector3 GetVector3(ushort group, ushort id) =>
            GetValue(mGvalueDic_Vector3, group, id, (g, i) => g.mEngineVector3[i], Vector3.zero);

        public void SetVector3(ushort group, ushort id, Vector3 value) =>
            SetValue(mGvalueDic_Vector3, group, id, value);

        // Enum???????
        public byte GetEnum(ushort group, ushort id) =>
            GetValue(mGvalueDic_Enum, group, id, (g, i) => g.mEngineEnum[i], (byte)0);

        public void SetEnum(ushort group, ushort id, byte value) =>
            SetSyncableValue(mGvalueDic_Enum, group, id, value, EGValueType.GEnum);

        // PointData???????
        public byte GetPointData(ushort group, ushort id) =>
            GetValue(mGvalueDic_PointData, group, id, (g, i) => g.mEnginePointData[i], (byte)0);

        public void SetPointData(ushort group, ushort id, byte value) =>
            SetValue(mGvalueDic_PointData, group, id, value);

        // Unit???????
        public byte GetUnit(ushort group, ushort id) =>
            GetValue(mGvalueDic_Unit, group, id, (g, i) => g.mEngineUnit[i], (byte)0);

        public void SetUnit(ushort group, ushort id, byte value) =>
            SetValue(mGvalueDic_Unit, group, id, value);

        // Transform???????
        public byte GetTransform(ushort group, ushort id) =>
            GetValue(mGvalueDic_Transform, group, id, (g, i) => g.mEngineTransform[i], (byte)0);

        public void SetTransform(ushort group, ushort id, byte value) =>
            SetValue(mGvalueDic_Transform, group, id, value);





        // GroupInt???????
        public int[] GetGroupInt(ushort group, ushort id) =>
            GetValue(mGvalueDic_GroupInt, group, id, (g, i) => g.mEngineGroupInt[i], new int[1]);

        public void SetGroupInt(ushort group, ushort id, int[] value) =>
            SetSyncableValue(mGvalueDic_GroupInt, group, id, value, EGValueType.GGroupInt);


        // GroupFloat???????
        public float[] GetGroupFloat(ushort group, ushort id)
            => GetValue(mGvalueDic_GroupFloat, group, id, (g, i) => g.mEngineGroupFloat[i], new float[1]);

        public void SetGroupFloat(ushort group, ushort id, float[] value) =>
            SetSyncableValue(mGvalueDic_GroupFloat, group, id, value, EGValueType.GGroupFloat);

        // GroupBool???????
        public bool[] GetGroupBool(ushort group, ushort id) =>
            GetValue(mGvalueDic_GroupBool, group, id, (g, i) => g.mEngineGroupBool[i], new bool[1]);

        public void SetGroupBool(ushort group, ushort id, bool[] value) =>
            SetSyncableValue(mGvalueDic_GroupBool, group, id, value, EGValueType.GGroupBool);
        // GroupString???????
        public string[] GetGroupString(ushort group, ushort id) =>
            GetValue(mGvalueDic_GroupString, group, id, (g, i) => g.mEngineGroupString[i], new string[1]);

        public void SetGroupString(ushort group, ushort id, string[] value) =>
            SetValue(mGvalueDic_GroupString, group, id, value);

        // GroupPointData???????
        public List<PointData> GetGroupPointData(ushort group, ushort id) =>
            GetValue(mGvalueDic_GroupPointData, group, id, mDefaltGP);
        public void SetGroupPointData(ushort group, ushort id, List<PointData> value) =>
            SetValue(mGvalueDic_GroupPointData, group, id, value);
        //public List<PointData> GetGroupPointData(ushort group, ushort id)
        //{
        //    List<PointData> _returnValue;
        //    if (!mGvalueDic_GroupPointData.TryGetValue((group, id), out _returnValue))
        //    {
        //        Debug.LogWarning("?????????Point????");
        //        _returnValue = new List<PointData>();
        //        mGvalueDic_GroupPointData.Add((group, id), _returnValue);
        //    }
        //    return _returnValue;
        //}

        //public void SetGroupPointData(ushort group, ushort id, List<PointData> value)
        //{
        //    if(!mGvalueDic_GroupPointData.TryAdd((group, id), value))
        //        mGvalueDic_GroupPointData[(group, id)] = value;
        //}

        // GroupUnit???????
        public List<ActionEngine_Unit> GetGroupUnit(ushort group, ushort id)
        {
            var key = (group, id);
            if (mGvalueDic_GroupUnit.TryGetValue(key, out var value))
            {
                // 兼容旧逻辑已写入的池引用：读时脱离到自有 List
                if (value != null && !mOwnedGroupUnitLists.Contains(value))
                {
                    var healed = new List<ActionEngine_Unit>(value.Count);
                    healed.AddRange(value);
                    mOwnedGroupUnitLists.Add(healed);
                    mGvalueDic_GroupUnit[key] = healed;
                    return healed;
                }
                return value;
            }

            var created = new List<ActionEngine_Unit>();
            mOwnedGroupUnitLists.Add(created);
            mGvalueDic_GroupUnit.Add(key, created);
            return created;
        }

        public void SetGroupUnit(ushort group, ushort id, List<ActionEngine_Unit> value)
        {
            var key = (group, id);
            if (!mGvalueDic_GroupUnit.TryGetValue(key, out var owned) || owned == null || !mOwnedGroupUnitLists.Contains(owned))
            {
                owned = new List<ActionEngine_Unit>(value?.Count ?? 8);
                mOwnedGroupUnitLists.Add(owned);
                mGvalueDic_GroupUnit[key] = owned;
            }
            else if (ReferenceEquals(owned, value))
            {
                return;
            }

            owned.Clear();
            if (value != null && value.Count > 0)
                owned.AddRange(value);
        }

        // GroupTransform???????
        public List<Transform> GetGroupTransform(ushort group, ushort id) =>
            GetValue(mGvalueDic_GroupTransform, group, id, mDefaltGT);

        public void SetGroupTransform(ushort group, ushort id, List<Transform> value) =>
            SetValue(mGvalueDic_GroupTransform, group, id, value);

        public void ValidateDictionarySignature(
            ushort group,
            ushort id,
            GDictionarySignature signature)
        {
            GetDictionaryStorage(group, id).ValidateSignature(signature);
        }

        public bool TryGetDictionaryValue<TKey, TValue>(
            ushort group,
            ushort id,
            TKey key,
            out TValue value)
        {
            GDictionaryKey dictionaryKey = GDictionaryKey.Create(key);
            EGValueType valueType = GDictionaryValue.GetValueType<TValue>();
            GDictionaryStorage storage = GetDictionaryStorage(group, id);
            storage.ValidateSignature(new GDictionarySignature(dictionaryKey.KeyType, valueType));
            if (!storage.TryGet(dictionaryKey, out GDictionaryValue dictionaryValue))
            {
                value = default;
                return false;
            }

            value = dictionaryValue.Get<TValue>();
            return true;
        }

        public bool TryGetDictionaryValue(
            ushort group,
            ushort id,
            GDictionaryKey key,
            out GDictionaryValue value)
        {
            return GetDictionaryStorage(group, id).TryGet(key, out value);
        }

        public void SetDictionaryValue<TKey, TValue>(
            ushort group,
            ushort id,
            TKey key,
            TValue value)
        {
            GDictionaryKey dictionaryKey = GDictionaryKey.Create(key);
            GDictionaryValue dictionaryValue = GDictionaryValue.Create(value);
            GDictionaryStorage storage = GetDictionaryStorage(group, id);
            storage.ValidateSignature(
                new GDictionarySignature(dictionaryKey.KeyType, dictionaryValue.ValueType));
            storage.Set(dictionaryKey, dictionaryValue);
        }

        public void SetDictionaryValue(
            ushort group,
            ushort id,
            GDictionaryKey key,
            GDictionaryValue value)
        {
            GetDictionaryStorage(group, id).Set(key, value);
        }

        public bool DictionaryContainsKey<TKey>(ushort group, ushort id, TKey key)
        {
            return DictionaryContainsKey(group, id, GDictionaryKey.Create(key));
        }

        public bool DictionaryContainsKey(
            ushort group,
            ushort id,
            GDictionaryKey key)
        {
            return GetDictionaryStorage(group, id).Contains(key);
        }

        public bool DictionaryContainsValue<TValue>(
            ushort group,
            ushort id,
            TValue value)
        {
            return DictionaryContainsValue(
                group,
                id,
                GDictionaryValue.Create(value));
        }

        public bool DictionaryContainsValue(
            ushort group,
            ushort id,
            GDictionaryValue value)
        {
            return GetDictionaryStorage(group, id).ContainsValue(value);
        }

        public bool RemoveDictionaryValue<TKey>(ushort group, ushort id, TKey key)
        {
            return RemoveDictionaryValue(group, id, GDictionaryKey.Create(key));
        }

        public bool RemoveDictionaryValue(
            ushort group,
            ushort id,
            GDictionaryKey key)
        {
            return GetDictionaryStorage(group, id).Remove(key);
        }

        public void ClearDictionary(ushort group, ushort id)
        {
            GetDictionaryStorage(group, id).Clear();
        }

        public int GetDictionaryCount(ushort group, ushort id)
        {
            return GetDictionaryStorage(group, id).Count;
        }

        public void ResetDictionary(ushort group, ushort id)
        {
            var key = (group, id);
            GetDictionaryStorage(group, id);
            mGvalueDic_Dictionary.Remove(key);
        }

        private GDictionaryStorage GetDictionaryStorage(ushort group, ushort id)
        {
            var key = (group, id);
            if (mGvalueDic_Dictionary.TryGetValue(key, out GDictionaryStorage storage))
            {
                return storage;
            }

            Dictionary<ushort, EngineGValue> engineGValues = mEngineGValueDic;
            if (engineGValues == null ||
                !engineGValues.TryGetValue(group, out EngineGValue engineGValue) ||
                engineGValue == null)
            {
                throw GDictionaryTypeUtility.InvalidSignature(
                    $"GDictionary定义组不存在, Group[{group}], ID[{id}]");
            }

            GDictionaryDefinition[] definitions = engineGValue.mEngineDictionary;
            if (definitions == null || id >= definitions.Length)
            {
                int length = definitions == null ? 0 : definitions.Length;
                throw GDictionaryTypeUtility.InvalidSignature(
                    $"GDictionary定义ID越界, Group[{group}], ID[{id}], Length[{length}]");
            }

            storage = new GDictionaryStorage(definitions[id], group, id);
            mGvalueDic_Dictionary.Add(key, storage);
            return storage;
        }
    }
}
