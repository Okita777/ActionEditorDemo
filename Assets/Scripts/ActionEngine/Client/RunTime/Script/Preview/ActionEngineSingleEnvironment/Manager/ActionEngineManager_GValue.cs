using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime;

namespace AsiTimeLine.RunTime
{
    public class ActionEngineManager_GValue
    {
        private const int MaxPendingSetRequests = 4096;

        private struct PendingSetRequest
        {
            public ActionEngine_Unit Unit;
            public ushort GroupIndex;
            public int Index;
            public object Value;
        }

        #region Instance
        private static ActionEngineManager_GValue _instance;
        public static ActionEngineManager_GValue Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = new ActionEngineManager_GValue();
                }

                return _instance;
            }
        }
        #endregion

        //public void SetGvalueToUnit(ActionEngine_Unit _unit, ushort _groupIndex, ushort _index, EGValueType _type, object _value)
        //    => OnSetGvalueToUnit(_unit, _groupIndex, _index, _type, _value);
        public void SetGvalueToUnit(ActionEngine_Unit _unit, ushort _groupIndex, int _index, object _value)
        {
            if (_unit == null)
            {
                EngineDebug.LogError($"SetGvalueToUnit failed: unit is null, group={_groupIndex}, index={_index}");
                return;
            }

            if (!mLoadFlish || EngineGValueDic.Count == 0)
            {
                EnqueuePendingSetRequest(_unit, _groupIndex, _index, _value);
                return;
            }

            OnSetGvalueToUnit(_unit, _groupIndex, _index, _value);
        }
        public bool GetGvalue_Key(ActionEngine_Unit _unit, ushort _groupIndex, int _index, out (ushort, ushort) _key)
            => OnGetGvalue_Key(_unit, _groupIndex, _index, out _key);
        public bool GetGvalue_Int(ActionEngine_Unit _unit, ushort _groupIndex, int _index, out int _value)
            => OnGetGvalue_Int(_unit, _groupIndex, _index, out _value);
        public bool GetGvalue_Enum(ActionEngine_Unit _unit, ushort _groupIndex, int _index, out byte _value)
            => OnGetGvalue_Enum(_unit, _groupIndex, _index, out _value);
        public bool GetGvalue_Float(ActionEngine_Unit _unit, ushort _groupIndex, int _index, out float _value)
            => OnGetGvalue_Float(_unit, _groupIndex, _index, out _value);
        public bool GetGvalue_GroupInt(ActionEngine_Unit _unit, ushort _groupIndex, int _index, out int[] _value)
            => OnGetGvalue_GroupInt(_unit, _groupIndex, _index, out _value);
        public bool GetGvalue_GroupFloat(ActionEngine_Unit _unit, ushort _groupIndex, int _index, out float[] _value)
            => OnGetGvalue_GroupFloat(_unit, _groupIndex, _index, out _value);
        public Dictionary<ushort, EngineGValue> EngineGValueDic = new Dictionary<ushort, EngineGValue>();
        public Dictionary<ushort, EngineEquation> EngineGEquationDic = null;

        //private EngineGValue[] _engineGValue = null;

        public void Init()
        {
            _engineGValues.Clear();
            mLoadWait.Clear();
            _engineEquations.Clear();
            _pendingSetRequests.Clear();
            mLoadFlish = false;
            //EngineDebug.LogWarning("GV加载初始化");
            GetGValue((gvalue, gequation) =>
            {
                //_engineGValue = _engineGValues.ToArray();
            });
        }

        private List<Action<Dictionary<ushort, EngineGValue>, Dictionary<ushort, EngineEquation>>> mLoadWait =
            new List<Action<Dictionary<ushort, EngineGValue>, Dictionary<ushort, EngineEquation>>>();
        private readonly Queue<PendingSetRequest> _pendingSetRequests = new Queue<PendingSetRequest>();
        private List<EngineGValue> _engineGValues = new List<EngineGValue>();
        private Dictionary<ushort, EngineEquation> _engineEquations = new Dictionary<ushort, EngineEquation>();
        private bool mLoadFlish = false;
        public void GetGValue(Action<Dictionary<ushort, EngineGValue>, Dictionary<ushort, EngineEquation>> callback)
        {
            if (mLoadFlish)
            {
                //SetGV();
                callback(EngineGValueDic, EngineGEquationDic);
                return;
            }

            if (mLoadWait.Count > 0)
            {
                mLoadWait.Add(callback);
                return;
            }

            mLoadWait.Add(callback);

            int loadNumber = 0;
            bool loadFailed = false;
            Action _loadFlish = () =>
            {
                loadNumber--;
                if (loadNumber <= 0)
                {
                    if (!loadFailed)
                    {
                        try
                        {
                            SetGV();
                        }
                        catch (Exception exception)
                        {
                            loadFailed = true;
                            EngineDebug.LogError($"GValue 初始化失败: {exception}");
                        }
                    }

                    CompleteLoadWaiters(!loadFailed);
                }
            };

            ActionEngineManager_AssetKey.Instance.Load(_engine =>
            {
                if (_engine == null)
                {
                    loadFailed = true;
                    loadNumber = 1;
                    _loadFlish();
                    return;
                }

                //EngineResourcesManager.Instance.Update
                loadNumber = _engine.mGValues.Count + _engine.mEquations.Count;
                if (loadNumber <= 0)
                {
                    _loadFlish();
                    return;
                }

                foreach (var engineMGValue in _engine.mGValues)
                {
                    ActionEnginLoadData.Instance.LoadInfo(EInfoType.GValue, (_target) =>
                    {
                        if (_target is EngineGValue engineGValue)
                            _engineGValues.Add(engineGValue);
                        else
                            loadFailed = true;
                        _loadFlish();
                    }, engineMGValue.mID);
                }

                foreach (var equation in _engine.mEquations)
                {
                    ActionEnginLoadData.Instance.LoadInfo(EInfoType.Equation, (_target) =>
                    {
                        if (_target is EngineEquation engineEquation)
                            _engineEquations.Add(engineEquation.mID, engineEquation);
                        else
                            loadFailed = true;
                        _loadFlish();
                    }, equation.mID);
                }
            });
        }

        private void CompleteLoadWaiters(bool succeeded)
        {
            var waiters = new List<Action<Dictionary<ushort, EngineGValue>, Dictionary<ushort, EngineEquation>>>(mLoadWait);
            mLoadWait.Clear();
            mLoadFlish = succeeded;
            Dictionary<ushort, EngineGValue> gValues = succeeded ? EngineGValueDic : null;
            Dictionary<ushort, EngineEquation> equations = succeeded ? EngineGEquationDic : null;
            if (!succeeded)
            {
                _engineGValues.Clear();
                _engineEquations.Clear();
                EngineGValueDic.Clear();
                EngineGEquationDic = null;
            }

            for (int index = 0; index < waiters.Count; index++)
            {
                try
                {
                    waiters[index]?.Invoke(gValues, equations);
                }
                catch (Exception exception)
                {
                    EngineDebug.LogError($"GValue 完成回调失败 index=[{index}]: {exception}");
                }
            }
        }

        private void SetGV()
        {
            //EngineDebug.Log("加载GV");
            EngineGValueDic.Clear();
            foreach (EngineGValue item in _engineGValues)
            {
                EngineGValueDic.Add(item.mID, item);
            }
            EngineGEquationDic = _engineEquations;
            FlushPendingSetRequests();
        }

        private void OnSetGvalueToUnit(ActionEngine_Unit _unit, ushort _groupIndex, int _index, object _value)
        {
            if (EngineGValueDic.TryGetValue(_groupIndex, out EngineGValue _gvalue))
            {
                if (_gvalue.GetRealyID(_index, out int _id))
                {
                    ushort _realyID = _gvalue.RunTimeID[_id];
                    EGValueType _type = _gvalue.ValueType[_id];
                    OnSetGvalueToUnit(_unit, _groupIndex, _realyID, _type, _value);
                }
                else
                {
                    EngineDebug.LogError($"SetGvalueToUnit failed: gv index [{_index}] not found in group [{_groupIndex}]");
                }
            }
            else
            {
                EngineDebug.LogError($"SetGvalueToUnit failed: gv group [{_groupIndex}] not found or not loaded");
            }
        }
        private void OnSetGvalueToUnit(ActionEngine_Unit _unit, ushort _groupIndex, ushort _index, EGValueType _type, object _value)
        {
            if (_unit == null || _unit.ActionStateMachine == null || _unit.ActionStateMachine.GValuePool == null)
            {
                EngineDebug.LogError($"SetGvalueToUnit failed: state machine or gvalue pool missing, group={_groupIndex}, runtimeIndex={_index}, value={_value}");
                return;
            }
            ActionStateMachine actionStateMachine = _unit.ActionStateMachine;
            switch (_type)
            {
                case EGValueType.GBool:
                    bool _boolVal = Convert.ToBoolean(_value);
                    bool _boolVal_old = actionStateMachine.GValuePool.GetBool(_groupIndex, _index);
                    _unit.ActionStateMachine.GValuePool.SetBool(_groupIndex, _index, _boolVal);
                    SendChangeGBool(actionStateMachine, _groupIndex, _index, _boolVal_old, _boolVal);
                    break;
                case EGValueType.GInt:
                    int _intVal = Convert.ToInt32(_value);
                    int _intVal_old = actionStateMachine.GValuePool.GetInt(_groupIndex, _index);
                    _unit.ActionStateMachine.GValuePool.SetInt(_groupIndex, _index, _intVal);
                    SendChangeGInt(actionStateMachine, _groupIndex, _index, _intVal_old, _intVal);
                    break;
                case EGValueType.GFloat:
                    float _floatVal = Convert.ToSingle(_value);
                    float _floatVal_old = actionStateMachine.GValuePool.GetFloat(_groupIndex, _index);
                    _unit.ActionStateMachine.GValuePool.SetFloat(_groupIndex, _index, _floatVal);
                    SendChangeGFloat(actionStateMachine, _groupIndex, _index, _floatVal_old, _floatVal);
                    break;
                case EGValueType.GString:
                    _unit.ActionStateMachine.GValuePool.SetString(_groupIndex, _index, Convert.ToString(_value));
                    break;
                case EGValueType.GEnum:
                    byte _enumVal = Convert.ToByte(_value);
                    byte _enumVal_old = actionStateMachine.GValuePool.GetEnum(_groupIndex, _index);
                    _unit.ActionStateMachine.GValuePool.SetEnum(_groupIndex, _index, _enumVal);
                    SendChangeGEnum(actionStateMachine, _groupIndex, _index, _enumVal_old, _enumVal);
                    break;
                case EGValueType.GTransform:
                    _unit.ActionStateMachine.GValuePool.SetTransform(_groupIndex, _index, Convert.ToByte(_value));
                    break;
                case EGValueType.GPoint:
                    _unit.ActionStateMachine.GValuePool.SetPointData(_groupIndex, _index, Convert.ToByte(_value));
                    break;
                case EGValueType.GUnit:
                    byte _unitByte_old = actionStateMachine.GValuePool.GetUnit(_groupIndex, _index);
                    byte _unitByte_new = Convert.ToByte(_value);
                    TargetUnit _unit_old = actionStateMachine.GetUnit(_groupIndex, _index, _unitByte_old);
                    _unit.ActionStateMachine.GValuePool.SetUnit(_groupIndex, _index, _unitByte_new);
                    TargetUnit _unit_new = actionStateMachine.GetUnit(_groupIndex, _index, _unitByte_new);
                    SendChangeGUnit(actionStateMachine, _groupIndex, _index, _unitByte_old, _unit_old, _unit_new);
                    if (_unitByte_old != _unitByte_new)
                        SendChangeGUnit(actionStateMachine, _groupIndex, _index, _unitByte_new, _unit_old, _unit_new);
                    break;
                case EGValueType.GGroupInt:
                    _unit.ActionStateMachine.GValuePool.SetGroupInt(_groupIndex, _index, CloneGroupIntValue(_unit, _groupIndex, _index, _value));
                    break;
                case EGValueType.GGroupFloat:
                    _unit.ActionStateMachine.GValuePool.SetGroupFloat(_groupIndex, _index, CloneGroupFloatValue(_unit, _groupIndex, _index, _value));
                    break;
                case EGValueType.GGroupBool:
                    _unit.ActionStateMachine.GValuePool.SetGroupBool(_groupIndex, _index, CloneGroupBoolValue(_unit, _groupIndex, _index, _value));
                    break;
                case EGValueType.GGroupString:
                    _unit.ActionStateMachine.GValuePool.SetGroupString(_groupIndex, _index, CloneGroupStringValue(_unit, _groupIndex, _index, _value));
                    break;

                //case EGValueType.GGroupTransform:
                //    _unit.ActionStateMachine.GValuePool.SetGroupTransform(_groupIndex, _index, Convert.ToByte(_value));
                //    break;
                //case EGValueType.GGroupPoint:
                //    _unit.ActionStateMachine.GValuePool.SetGroupPointData(_groupIndex, _index, Convert.ToByte(_value));
                //    break;
                //case EGValueType.GGroupUnit:
                //    _unit.ActionStateMachine.GValuePool.SetGroupUnit(_groupIndex, _index, Convert.ToByte(_value));
                //    break;
                default:
                    EngineDebug.LogError($"未实现GV类型 [<color=#ffcc00>{_type.ToString()}</color>]");
                    break;
            }

#if UNITY_EDITOR
            EngineDebug.Log($"SetGvalueToUnit applied: group={_groupIndex}, runtimeIndex={_index}, type={_type}, value={_value}");
#endif
        }

        private static GInt mRefer_Gint = new GInt();
        private static GFloat mRefer_GFloat = new GFloat();
        private static GEnum mRefer_GEnum = new GEnum();
        private static GBool mRefer_GBool = new GBool();
        private static GUnit mRefer_GUnit = new GUnit();

        private static void SendChangeGInt(ActionStateMachine _actionState, ushort _groupIndex, ushort _index, int _oldVal, int _value)
        {
            mRefer_Gint.mValueGroupIndex = _groupIndex;
            mRefer_Gint.mValueIndex = _index;
            _actionState.SendChangeMessage_GInt(mRefer_Gint, _oldVal, _value);
        }

        private static void SendChangeGFloat(ActionStateMachine _actionState, ushort _groupIndex, ushort _index, float _oldVal, float _value)
        {
            mRefer_GFloat.mValueGroupIndex = _groupIndex;
            mRefer_GFloat.mValueIndex = _index;
            _actionState.SendChangeMessage_GFloat(mRefer_GFloat, _oldVal, _value);
        }

        private static void SendChangeGEnum(ActionStateMachine _actionState, ushort _groupIndex, ushort _index, byte _oldVal, byte _value)
        {
            mRefer_GEnum.mValueGroupIndex = _groupIndex;
            mRefer_GEnum.mValueIndex = _index;
            _actionState.SendChangeMessage_GEnum(mRefer_GEnum, _oldVal, _value);
        }

        private static void SendChangeGBool(ActionStateMachine _actionState, ushort _groupIndex, ushort _index, bool _oldVal, bool _value)
        {
            mRefer_GBool.mValueGroupIndex = _groupIndex;
            mRefer_GBool.mValueIndex = _index;
            _actionState.SendChangeMessage_GBool(mRefer_GBool, _oldVal, _value);
        }

        private static void SendChangeGUnit(ActionStateMachine _actionState, ushort _groupIndex, ushort _index, byte _serValue, TargetUnit _oldVal, TargetUnit _newVal)
        {
            mRefer_GUnit.mValueGroupIndex = _groupIndex;
            mRefer_GUnit.mValueIndex = _index;
            mRefer_GUnit.mSerValue = _serValue;
            _actionState.SendChangeMessage_GUnit(mRefer_GUnit, _oldVal, _newVal);
        }

        /// <summary> GValuePool.SetGroupInt 需要 int[]；技能/Modifier 等路径常传入标量 int。 </summary>
        private static int[] CloneGroupIntValue(ActionEngine_Unit unit, ushort groupIndex, ushort runtimeId, object value)
        {
            if (value is int[] srcInt)
            {
                var copy = new int[srcInt.Length];
                Array.Copy(srcInt, copy, srcInt.Length);
                return copy;
            }
            var cur = unit.ActionStateMachine.GValuePool.GetGroupInt(groupIndex, runtimeId);
            var next = new int[cur.Length];
            Array.Copy(cur, next, cur.Length);
            if (next.Length > 0)
                next[0] = Convert.ToInt32(value);
            return next;
        }

        private static float[] CloneGroupFloatValue(ActionEngine_Unit unit, ushort groupIndex, ushort runtimeId, object value)
        {
            if (value is float[] srcFloat)
            {
                var copy = new float[srcFloat.Length];
                Array.Copy(srcFloat, copy, srcFloat.Length);
                return copy;
            }
            var cur = unit.ActionStateMachine.GValuePool.GetGroupFloat(groupIndex, runtimeId);
            var next = new float[cur.Length];
            Array.Copy(cur, next, cur.Length);
            if (next.Length > 0)
                next[0] = Convert.ToSingle(value);
            return next;
        }

        private static bool[] CloneGroupBoolValue(ActionEngine_Unit unit, ushort groupIndex, ushort runtimeId, object value)
        {
            if (value is bool[] srcBool)
            {
                var copy = new bool[srcBool.Length];
                Array.Copy(srcBool, copy, srcBool.Length);
                return copy;
            }
            var cur = unit.ActionStateMachine.GValuePool.GetGroupBool(groupIndex, runtimeId);
            var next = new bool[cur.Length];
            Array.Copy(cur, next, cur.Length);
            if (next.Length > 0)
                next[0] = Convert.ToBoolean(value);
            return next;
        }

        private static string[] CloneGroupStringValue(ActionEngine_Unit unit, ushort groupIndex, ushort runtimeId, object value)
        {
            if (value is string[] srcStr)
            {
                var copy = new string[srcStr.Length];
                Array.Copy(srcStr, copy, srcStr.Length);
                return copy;
            }
            var cur = unit.ActionStateMachine.GValuePool.GetGroupString(groupIndex, runtimeId);
            var next = new string[cur.Length];
            Array.Copy(cur, next, cur.Length);
            if (next.Length > 0)
                next[0] = Convert.ToString(value);
            return next;
        }

        private void EnqueuePendingSetRequest(ActionEngine_Unit unit, ushort groupIndex, int index, object value)
        {
            if (_pendingSetRequests.Count >= MaxPendingSetRequests)
            {
                _pendingSetRequests.Dequeue();
                EngineDebug.LogError($"GValue pending queue overflow, dropped oldest request. max={MaxPendingSetRequests}");
            }

            _pendingSetRequests.Enqueue(new PendingSetRequest
            {
                Unit = unit,
                GroupIndex = groupIndex,
                Index = index,
                Value = value
            });
        }

        private void FlushPendingSetRequests()
        {
            if (_pendingSetRequests.Count == 0)
            {
                return;
            }

            int flushCount = _pendingSetRequests.Count;
            for (int i = 0; i < flushCount; i++)
            {
                var request = _pendingSetRequests.Dequeue();
                if (request.Unit == null)
                {
                    continue;
                }
                OnSetGvalueToUnit(request.Unit, request.GroupIndex, request.Index, request.Value);
            }
#if UNITY_EDITOR
            EngineDebug.Log($"GValue pending queue flushed: count={flushCount}");
#endif
        }
        private bool OnGetGvalue_Key(ActionEngine_Unit _unit, ushort _groupIndex, int _index, out (ushort, ushort) _key)
        {
            if (GetEngineGValueDic(_groupIndex, _index, out EngineGValue _gvalue, out int _id))
            {
                ushort _realyID = _gvalue.RunTimeID[_id];
                _key = (_groupIndex, _realyID);
                return true;
            }
            _key = (0, 0);
            return false;
        }
        private bool OnGetGvalue_Int(ActionEngine_Unit _unit, ushort _groupIndex, int _index, out int _value)
        {
            if (GetEngineGValueDic(_groupIndex, _index, out EngineGValue _gvalue, out int _id))
            {
                ushort _realyID = _gvalue.RunTimeID[_id];
                _value = _unit.ActionStateMachine.GValuePool.GetInt(_groupIndex, _realyID);
                return true;
            }
            _value = 0;
            return false;
        }
        private bool OnGetGvalue_Float(ActionEngine_Unit _unit, ushort _groupIndex, int _index, out float _value)
        {
            if (GetEngineGValueDic(_groupIndex, _index, out EngineGValue _gvalue, out int _id))
            {
                ushort _realyID = _gvalue.RunTimeID[_id];
                _value = _unit.ActionStateMachine.GValuePool.GetFloat(_groupIndex, _realyID);
                return true;
            }
            _value = 0;
            return false;
        }
        private bool OnGetGvalue_GroupInt(ActionEngine_Unit _unit, ushort _groupIndex, int _index, out int[] _value)
        {
            if (GetEngineGValueDic(_groupIndex, _index, out EngineGValue _gvalue, out int _id))
            {
                ushort _realyID = _gvalue.RunTimeID[_id];
                _value = _unit.ActionStateMachine.GValuePool.GetGroupInt(_groupIndex, _realyID);
                return true;
            }
            _value = EngineResourcesManager.Instance.GetGroupIntNull;
            return false;
        }
        private bool OnGetGvalue_GroupFloat(ActionEngine_Unit _unit, ushort _groupIndex, int _index, out float[] _value)
        {
            if (GetEngineGValueDic(_groupIndex, _index, out EngineGValue _gvalue, out int _id))
            {
                ushort _realyID = _gvalue.RunTimeID[_id];
                _value = _unit.ActionStateMachine.GValuePool.GetGroupFloat(_groupIndex, _realyID);
                return true;
            }
            _value = EngineResourcesManager.Instance.GetGroupFloatNull;
            return false;
        }
        private bool OnGetGvalue_Enum(ActionEngine_Unit _unit, ushort _groupIndex, int _index, out byte _value)
        {
            if (GetEngineGValueDic(_groupIndex, _index, out EngineGValue _gvalue, out int _id))
            {
                ushort _realyID = _gvalue.RunTimeID[_id];
                _value = _unit.ActionStateMachine.GValuePool.GetEnum(_groupIndex, _realyID);
                return true;
            }
            _value = 0;
            return false;
        }

        private bool GetEngineGValueDic(ushort _groupIndex, int _index, out EngineGValue _gvalue, out int _id)
        {
            if (EngineGValueDic.TryGetValue(_groupIndex, out _gvalue))
            {
                if (_gvalue.GetRealyID(_index, out _id))
                {
                    ushort _realyID = _gvalue.RunTimeID[_id];
                    EGValueType _type = _gvalue.ValueType[_id];
                    return true;
                }
#if UNITY_EDITOR
                else
                {
                    EngineDebug.LogError($"GV [{_index}] 获取失败， 所属于 [{_groupIndex}] GV组");
                }
#endif
            }
#if UNITY_EDITOR
            else
            {
                EngineDebug.LogError($"GV组 [{_groupIndex}] 获取失败，不存在这个组或者还未加载");
            }
#endif
            _id = 0;
            return false;
        }
    }
}
