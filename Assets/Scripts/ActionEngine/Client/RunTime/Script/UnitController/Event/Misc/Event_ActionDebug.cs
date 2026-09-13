using System.Collections.Generic;
#if UNITY_EDITOR
using AsiActionEngine.Editor;
#endif
using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;
using UnityEngine.Serialization;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_ActionDebug : IActionEventData
    {
        [SerializeField] protected byte mDebugType;
        [SerializeField] protected string mDebugName = "Even";
        [SerializeField] protected bool mOnlyDebugPlayer = false;
        [SerializeField] protected GInt mGIntTest = new GInt(12);
        [SerializeField] protected GFloat mGFloatTest2 = new GFloat(23);
        [SerializeField] protected GEnum mEnumname = new GEnum(0);
        [SerializeField] protected GTransform mGTransform = new GTransform();
        [SerializeField] protected GValue_Setting mGintTest3 = new GValue_Setting();
        [SerializeField] protected GValue_Ratio mGintTest4 = new GValue_Ratio();
        [SerializeField] protected EVector3 mEVector3 = new EVector3();
        [SerializeField] protected GValue_SetUnit mUnitPos = new GValue_SetUnit(false);
        [SerializeField] protected GValue_SetTransform mTransPos = new GValue_SetTransform(false);
        [SerializeField] protected GValue_SetPoint mPointPos = new GValue_SetPoint(false);
        [SerializeField] protected GValue_SetFloat mSetFloat = new GValue_SetFloat(false);
        [SerializeField] protected GValue_SetInt mSetInt = new GValue_SetInt(false);
        [SerializeField] protected GValue_SetBool mSetBool = new GValue_SetBool(false);
        [SerializeField] protected GValue_SetEnum mSetEnum = new GValue_SetEnum(false);

        [SerializeField] protected GGroupBool mGroupBool = new GGroupBool();
        [SerializeField] protected GGroupInt mGroupInt = new GGroupInt();
        [SerializeField] protected GGroupFloat mGroupFloat = new GGroupFloat();
        [SerializeField] protected GGroupString mGroupString = new GGroupString();
        [SerializeField] protected GGroupUnit mGroupUnit = new GGroupUnit();
        [SerializeField] protected GGroupPoint mGroupPoint = new GGroupPoint();
        [SerializeField] protected GGroupTransform mGroupTransform = new GGroupTransform();
        [SerializeField] protected GGroupString_Select mGroupString_Select = new GGroupString_Select();

        [FormerlySerializedAs("mEvent_Vector3")][SerializeField] protected GraphEvent_NoValue_Vector3 mEventNoValueVector3 = new GraphEvent_NoValue_Vector3();

        #region property
        [EditorProperty("Debug类型", EditorPropertyType.EEPT_Enum, EnumNames = new[] { "普通(log)", "警告(warning)", "报错(error)", "弹窗" })]
        public byte DebugType
        {
            get { return mDebugType; }
            set { mDebugType = value; }
        }
        [EditorProperty("仅操作对象执行Debug", EditorPropertyType.EEPT_Bool, LabelWidth = 120)]
        public bool OnlyDebugPlayer
        {
            get { return mOnlyDebugPlayer; }
            set { mOnlyDebugPlayer = value; }
        }
        [EditorProperty("String数组", EditorPropertyType.EEPT_GGroupString_Select)]
        public GGroupString_Select GroupString_Select
        {
            get { return mGroupString_Select; }
            set { mGroupString_Select = value; }
        }

        [EditorProperty("Trans数组", EditorPropertyType.EEPT_GGroupTransform)]
        public GGroupTransform GroupTransform
        {
            get { return mGroupTransform; }
            set { mGroupTransform = value; }
        }

        [EditorProperty("Unit位置绘制", EditorPropertyType.EEPT_SetGUnit)]
        public GValue_SetUnit UnitPos
        {
            get { return mUnitPos; }
            set { mUnitPos = value; }
        }
        [EditorProperty("Trans位置绘制", EditorPropertyType.EEPT_SetGTransform)]
        public GValue_SetTransform TransPos
        {
            get { return mTransPos; }
            set { mTransPos = value; }
        }
        [EditorProperty("Point位置绘制", EditorPropertyType.EEPT_SetGPoint)]
        public GValue_SetPoint PointPos
        {
            get { return mPointPos; }
            set { mPointPos = value; }
        }
        [EditorProperty("GFloat输出", EditorPropertyType.EEPT_SetGFloat)]
        public GValue_SetFloat SetFloat
        {
            get { return mSetFloat; }
            set { mSetFloat = value; }
        }
        [EditorProperty("GInt输出", EditorPropertyType.EEPT_SetGInt)]
        public GValue_SetInt SetInt
        {
            get { return mSetInt; }
            set { mSetInt = value; }
        }
        [EditorProperty("GBool输出", EditorPropertyType.EEPT_SetGBool)]
        public GValue_SetBool SetBool
        {
            get { return mSetBool; }
            set { mSetBool = value; }
        }
        [EditorProperty("GEnum输出", EditorPropertyType.EEPT_SetGEnum)]
        public GValue_SetEnum SetEnum
        {
            get { return mSetEnum; }
            set { mSetEnum = value; }
        }
        [EditorProperty("Debug标题", EditorPropertyType.EEPT_String)]
        public string DebugName
        {
            get { return mDebugName; }
            set { mDebugName = value; }
        }

        // [EditorProperty("参数测试1", EditorPropertyType.EEPT_Int)]
        // public GInt GIntTest
        // {
        //     get { return mGIntTest; }
        //     set { mGIntTest = value; }
        // }

        // [EditorProperty("参数测试2", EditorPropertyType.EEPT_Float)]
        // public GFloat GFloatTest2
        // {
        //     get { return mGFloatTest2; }
        //     set { mGFloatTest2 = value; }
        // }
        //
        // [EditorProperty("参数测试3", EditorPropertyType.EEPT_GValueSetting)]
        // public GValue_Setting GintTest3
        // {
        //     get { return mGintTest3; }
        //     set { mGintTest3 = value; }
        // }
        // [EditorProperty("参数测试4", EditorPropertyType.EEPT_GValueSRatio)]
        // public GValue_Ratio GintTest4
        // {
        //     get { return mGintTest4; }
        //     set { mGintTest4 = value; }
        // }
        // [EditorProperty("参数测试5", EditorPropertyType.EEPT_Enum, EnumNames = new []{"选项1","选项2","选项3"})]
        // public GEnum Enumname
        // {
        //     get { return mEnumname; }
        //     set { mEnumname = value; }
        // }
        // [EditorProperty("获取Trans", EditorPropertyType.EEPT_GTransform)]
        // public GTransform GTransform
        // {
        //     get { return mGTransform; }
        //     set { mGTransform = value; }
        // }
        // [EditorProperty("获取Trans", EditorPropertyType.EEPT_GraphValue)]
        // public GraphEvent_NoValue_Vector3 EventNoValueVector3
        // {
        //     get { return mEventNoValueVector3; }
        //     set { mEventNoValueVector3 = value; }
        // }
        #endregion

        public int GetEvenType() => (int)EEvenType.EET_ActionDebug;
        public IActionEventData Creact() => new Event_ActionDebug();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
#if UNITY_EDITOR
            if (OnlyDebugPlayer)
            {
                if (_actionState.ActionStateMachine.CurUnit.GetSource != ActionEngineManager_Input.Instance.Player)
                {
                    return;
                }
            }
            string _debugCon = "";
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            if (_stateMachine == null || _stateMachine.CurUnit == null)
            {
                return;
            }
            var curUnitPos = _stateMachine.CurUnit.transform.position;
            if (_isSingle)
            {
                if (mUnitPos.m_IsSet)
                {
                    if (mUnitPos.m_Value.IsValid(_actionState))
                    {
                        var targetUnit = mUnitPos.m_Value.GetValue(_actionState).GetUnit();
                        if (targetUnit != null)
                        {
                            Vector3 _pos = targetUnit.transform.position;
                            EngineScenceDraw.Sphere(_pos, Quaternion.identity, 1, Color.red, 1);
                            EngineScenceDraw.Line(curUnitPos, _pos, Color.red, 1);
                        }
                    }
                }

                if (mTransPos.m_IsSet)
                {
                    if (mTransPos.m_Value.IsValid(_actionState))
                    {
                        var targetTrans = mTransPos.m_Value.GetValue(_actionState);
                        if (targetTrans != null)
                        {
                            Vector3 _pos = targetTrans.position;
                            EngineScenceDraw.Sphere(targetTrans.position, Quaternion.identity, 0.8f, Color.green, 1);
                            EngineScenceDraw.Line(curUnitPos, _pos, Color.green, 1);
                        }
                    }
                    else
                    {
                        _debugCon = "未获取";
                    }
                }

                if (mPointPos.m_IsSet)
                {
                    if (mPointPos.m_Value.IsValid(_actionState))
                    {
                        Vector3 _pos = mPointPos.m_Value.GetValue(_actionState).pos;
                        EngineScenceDraw.Sphere(mPointPos.m_Value.GetValue(_actionState).pos, Quaternion.identity, 0.6f, Color.blue, 1);
                        EngineScenceDraw.Line(curUnitPos, _pos, Color.blue, 1);
                    }
                }

                if (SetFloat.m_IsSet || SetInt.m_IsSet || SetBool.m_IsSet)
                {
                    //string _debugCon = $"<color=#ffcc00>ActionEventDebug</color>  [{mDebugName}]";
                    if (SetFloat.m_IsSet)
                    {
                        _debugCon += $"\nDebugGFloat: {SetFloat.m_Value.GetValue(_actionState)}";
                    }

                    if (SetInt.m_IsSet)
                    {
                        _debugCon += $"\nDebugGInt: {SetInt.m_Value.GetValue(_actionState)}  GroupIndex[{SetInt.m_Value.mValueGroupIndex}]  ValueIndex[{SetInt.m_Value.mValueIndex}]";
                        _debugCon += $"\n当前注册的监控回调数量: [{_stateMachine.m_OnGIntChanged.Count}]";
                    }

                    if (SetBool.m_IsSet)
                    {
                        _debugCon += $"\nDebugGBool: {SetBool.m_Value.GetValue(_actionState)}";
                    }

                    if (SetEnum.m_IsSet)
                    {
                        int enumID = SetEnum.m_Value.mValueIndex;
                        EditorEngineGValue _gv = AsiActionEngine.Editor.ResourcesWindow.Instance.GetGValueToID(SetEnum.m_Value.mValueGroupIndex);
                        _debugCon += $"\nDebugGEnum: {_gv.EnumNames[enumID].names[SetEnum.m_Value.GetValue(_actionState)]}";
                    }

                    //DebugLog(_debugCon);
                }

                // return;
                if (mGintTest4.GValue_RatioPart.Count > 0)
                {
                    if (mGintTest4.CheckValue(_stateMachine))
                    {
                        _debugCon += "\n通过判断";
                    }
                    else
                    {
                        _debugCon += "\n未通过判断";
                    }
                }

                // if(mGTransform.)
                // EngineDebug.Log(mGTransform.value.name);
                string _eventDebug = "";
                for (int i = 0; i < _actionState.CurrentActionEvents.Count; i++)
                {
                    ActionEvent _actionEvent = _actionState.CurrentActionEvents[i];
                    _eventDebug += $"  {i}、 {_actionEvent.EventData.GetType().Name}  D:{_actionEvent.Duration}\n";
                }

                //Debug当前状态下的所有可跳转轨道
                string _interruptDebug = "";
                for (int i = 0; i < _actionState.CurActionInterrupt.Count; i++)
                {
                    ActionInterrupt _actionInterrupt = _actionState.CurActionInterrupt[i];
                    string str = "";
                    if(_stateMachine.TryGetActionState(_actionInterrupt.ActionID, out ActionState _state))
                        str = $"  {i}、 {_state.Name}[<color=#ffcc00>{_actionInterrupt.ActionID}</color>]\n";
                    else
                        str = $"  {i}、 <color=#ff0000>找不到对应Action</color>[<color=#ffcc00>{_actionInterrupt.ActionID}</color>]\n";
                    _interruptDebug += str;
                }

                string _actionLable = "";
                List<int> _ActionLableList = _actionState.ActionStateMachine.GetActionLableList;
                for (int i = 0; i < _ActionLableList.Count; i++)
                {
                    _actionLable += $"{i}: {_actionState.ActionStateMachine.GetActionLable(_ActionLableList[i])}\n";
                }

                //DebugLog("mGFloatTest2: " + mGFloatTest2.GetValue(_actionState));

                DebugLog(
                    "ActionDebug" +
                    $"<color=#FFCC00>{mDebugName}</color>\n" +
                    _debugCon +
                    //$"\nDebug信息来自 {_actionState.mCurActionLayer} 层级\n" +
                    $"\nDebug信息来自 [{EngineDebug.DebugActionStatePart(_actionState)}]\n" +
                    $"Action状态层为 {_actionState.GetActionType()} \n" +
                    $"当前层级时间 {_actionState.ElapsedTime} ms\n" +
                    $"事件数量: {_actionState.CurrentActionEvents.Count} \n" +
                    _eventDebug + "\n" +
                    $"打断轨数量: {_actionState.CurActionInterrupt.Count}\n" +
                    _interruptDebug + "\n" +
                    //$"当前状态标签数量：{_ActionLableList.Count}\n" +
                    _actionLable + "\n\n"
                );
            }
#endif
        }

        private void DebugLog(string _str)
        {
            if (mDebugType == 0)
            {
                EngineDebug.Log(_str);
            }
            else if (mDebugType == 1)
            {
                EngineDebug.LogWarning(_str);
            }
            else if (mDebugType == 2)
            {
                EngineDebug.LogError(_str);
            }
            else if (mDebugType == 3)
            {
                EngineDebug.DisplayDialog("EnginDebug事件弹窗", _str, "确认");
            }
        }
        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;


            if (mUnitPos.m_IsSet)
            {
                if (mUnitPos.m_Value.IsValid(_actionState))
                {
                    Vector3 _pos = mUnitPos.m_Value.GetValue(_actionState).GetUnit().transform.position;
                    EngineScenceDraw.Sphere(_pos, Quaternion.identity, 1, Color.red);
                    EngineScenceDraw.Line(_stateMachine.CurUnit.transform.position, _pos, Color.red);
                }
            }

            if (mTransPos.m_IsSet)
            {
                if (mTransPos.m_Value.IsValid(_actionState))
                {
                    Vector3 _pos = mTransPos.m_Value.GetValue(_actionState).position;
                    EngineScenceDraw.Sphere(mTransPos.m_Value.GetValue(_actionState).position, Quaternion.identity, 0.8f, Color.green);
                    EngineScenceDraw.Line(_stateMachine.CurUnit.transform.position, _pos, Color.green);
                }
                else
                {
                    EngineDebug.Log("未获取");
                }
            }

            if (mPointPos.m_IsSet)
            {
                if (mPointPos.m_Value.IsValid(_actionState))
                {
                    Vector3 _pos = mPointPos.m_Value.GetValue(_actionState).pos;
                    EngineScenceDraw.Sphere(mPointPos.m_Value.GetValue(_actionState).pos, Quaternion.identity, 0.6f, Color.blue);
                    EngineScenceDraw.Line(_stateMachine.CurUnit.transform.position, _pos, Color.blue);
                }
            }

            if (SetFloat.m_IsSet || SetInt.m_IsSet || SetBool.m_IsSet)
            {
                string _debugCon = $"<color=#ffcc00>ActionEventDebug</color>  [{mDebugName}]";

                if (SetFloat.m_IsSet)
                {
                    _debugCon += $"\nDebugGFloat: {SetFloat.m_Value.GetValue(_actionState)}";
                }
                if (SetInt.m_IsSet)
                {
                    _debugCon += $"\nDebugGInt: {SetInt.m_Value.GetValue(_actionState)}";
                }
                if (SetBool.m_IsSet)
                {
                    _debugCon += $"\nDebugGBool: {SetBool.m_Value.GetValue(_actionState)}";
                }
                if (SetEnum.m_IsSet)
                {
                    int enumID = SetEnum.m_Value.mValueIndex;
#if UNITY_EDITOR
                    EditorEngineGValue _gv =
                        AsiActionEngine.Editor.ResourcesWindow.Instance.GetGValueToID(SetEnum.m_Value.mValueGroupIndex);
                    _debugCon += $"\nDebugGEnum: {_gv.EnumNames[enumID].names[SetEnum.m_Value.GetValue(_actionState)]}";
#endif
                }
                //EngineDebug.Log(_debugCon);
                DebugLog(
                    "ActionDebug" +
                    $"<color=#FFCC00>{mDebugName}</color>\n" +
                    _debugCon +
                    //$"\nDebug信息来自 {_actionState.mCurActionLayer} 层级\n" +
                    $"\nDebug信息来自 [{EngineDebug.DebugActionStatePart(_actionState)}]\n" +
                    $"Action状态层为 {_actionState.GetActionType()} \n" +
                    $"当前层级时间 {_actionState.ElapsedTime} ms\n" +
                    $"事件数量: {_actionState.CurrentActionEvents.Count} \n" +
                    //_eventDebug + "\n" +
                    $"打断轨数量: {_actionState.CurActionInterrupt.Count}\n"// +
                                                                       //_interruptDebug + "\n" +
                                                                       //$"当前状态标签数量：{_ActionLableList.Count}\n" +
                                                                       //_actionLable + "\n\n"
                );
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_ActionDebug actionDebug = _eventData as Event_ActionDebug;
            actionDebug.DebugName = mDebugName;
            // actionDebug.Enumname = mEnumname;
            // actionDebug.GFloatTest2 = (GFloat)mGFloatTest2.Clone();
            actionDebug.mGintTest4 = mGintTest4.Clone();
            actionDebug.mGintTest3 = mGintTest3.Clone();
            actionDebug.mTransPos = mTransPos.Clone();
            actionDebug.mUnitPos = mUnitPos.Clone();
            actionDebug.mPointPos = mPointPos.Clone();

            actionDebug.mOnlyDebugPlayer = mOnlyDebugPlayer;
            actionDebug.SetBool = mSetBool.Clone();
            actionDebug.SetFloat = mSetFloat.Clone();
            actionDebug.SetInt = mSetInt.Clone();
            actionDebug.SetEnum = mSetEnum.Clone();
            actionDebug.DebugType = mDebugType;
            // actionDebug.EventNoValueVector3 = (GraphEvent_NoValue_Vector3)mEventNoValueVector3.Clone();
            return actionDebug;
        }
    }
}