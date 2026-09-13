using System.Collections.Generic;
using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.Graph;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_SetAnimFloatFromBluePrint : IActionEventData
    {
        #region Enum
        public enum EValueType
        {
            _1D,
            _2D
        }
        #endregion

        [SerializeField] private EValueType mValueType = EValueType._1D;
        [SerializeField] private string mAnimaFloatName = string.Empty;
        [SerializeField] private string mAnimaFloatName2 = string.Empty;
        [SerializeField] private float mAnimaFloatSpeed = 7;
        [SerializeField] private GraphEvent_NoValue_Float mFloatValue = CreateDefaultFloatValue();
        [SerializeField] private GraphEvent_NoValue_Vector3 mVector3Value = CreateDefaultVector3Value();

        #region DefaultNodeFactories

        private static GraphEvent_NoValue_Float CreateDefaultFloatValue()
        {
            return new GraphEvent_NoValue_Float()
            {
                BluePrint_Val = new GraphEvent_LocalParam_Read_Float() { ParamIndex = 0 },
                LocalFloatParams = new List<float>() { 0f },
#if UNITY_EDITOR
                m_NodeEdiData = new NodeEdiData()
                {
                    nodeTitle = "浮点值",
                    localParams = new List<BluePrintLocalParamDef>()
                    {
                        new BluePrintLocalParamDef { paramName = "浮点值", paramType = EBluePrintLocalParamType.Float, drawToInspector = true },
                    }
                }
#endif
            };
        }

        // 仅使用 X / Y 值，Z 保持默认常量节点不参与运算
        private static GraphEvent_NoValue_Vector3 CreateDefaultVector3Value()
        {
            return new GraphEvent_NoValue_Vector3()
            {
                BluePrint_Val = new GraphEvent_Make_Vector3()
                {
                    X = new GraphEvent_LocalParam_Read_Float() { ParamIndex = 0 },
                    Y = new GraphEvent_LocalParam_Read_Float() { ParamIndex = 1 },
                },
                LocalFloatParams = new List<float>() { 0f, 0f },
#if UNITY_EDITOR
                m_NodeEdiData = new NodeEdiData()
                {
                    nodeTitle = "二维值(X→参数X, Y→参数Y)",
                    localParams = new List<BluePrintLocalParamDef>()
                    {
                        new BluePrintLocalParamDef { paramName = "X值", paramType = EBluePrintLocalParamType.Float, drawToInspector = true },
                        new BluePrintLocalParamDef { paramName = "Y值", paramType = EBluePrintLocalParamType.Float, drawToInspector = true },
                    }
                }
#endif
            };
        }

        #endregion

        #region property

        [EditorProperty("参数类型", EditorPropertyType.EEPT_Enum)]
        public EValueType ValueType
        {
            get { return mValueType; }
            set { mValueType = value; }
        }

        [EditorProperty("浮点名称_X", EditorPropertyType.EEPT_String)]
        public string AnimaFloatName
        {
            get { return mAnimaFloatName; }
            set { mAnimaFloatName = value; }
        }
        [EditorProperty("浮点名称_Y", EditorPropertyType.EEPT_String)]
        public string AnimaFloatName2
        {
            get { return mAnimaFloatName2; }
            set { mAnimaFloatName2 = value; }
        }
        [EditorProperty("浮点值(蓝图)", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Float FloatValue
        {
            get { return mFloatValue; }
            set { mFloatValue = value; }
        }
        [EditorProperty("二维值(蓝图/仅X,Y)", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Vector3 Vector3Value
        {
            get { return mVector3Value; }
            set { mVector3Value = value; }
        }

        [EditorProperty("参数过渡速度", EditorPropertyType.EEPT_Float)]
        public float AnimaFloatSpeed
        {
            get { return mAnimaFloatSpeed; }
            set { mAnimaFloatSpeed = value; }
        }
        #endregion

        public int GetEvenType() => (int)EEvenType.EET_SetAnimFloatFromBluePrint;
        public IActionEventData Creact() => new Event_SetAnimFloatFromBluePrint();

        //private float lastAnimFloat = 0;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            //lastAnimFloat = 0;
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;

            if (_isSingle)
            {
                if (ValueType == EValueType._1D)
                {
                    _stateMachine.SetAnimatorFloat(mAnimaFloatName, GetValue(_actionState, EngineResourcesManager.Instance.MachineTime));
                }
                else
                {
                    Vector2 _vector2 = GetValue2D(_actionState, EngineResourcesManager.Instance.MachineTime);
                    _stateMachine.SetAnimatorFloat(mAnimaFloatName, _vector2.x);
                    _stateMachine.SetAnimatorFloat(mAnimaFloatName2, _vector2.y);
                }
            }
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            if (ValueType == EValueType._1D)
            {
                if (mAnimaFloatSpeed > 0)
                {
                    float lastAnimFloat = Mathf.Lerp(_stateMachine.GetAnimatorFloat(mAnimaFloatName), GetValue(_actionState, _actionTime),
                        mAnimaFloatSpeed * _actionTime.Deltatime);
                    _stateMachine.SetAnimatorFloat(mAnimaFloatName, lastAnimFloat);
                }
                else
                {
                    _stateMachine.SetAnimatorFloat(mAnimaFloatName, GetValue(_actionState, _actionTime));
                }
            }
            else
            {
                Vector2 _vector2 = GetValue2D(_actionState, _actionTime);
                if (mAnimaFloatSpeed > 0)
                {
                    float _lerpTime = mAnimaFloatSpeed * _actionTime.Deltatime;
                    float _lerpA = Mathf.Lerp(_stateMachine.GetAnimatorFloat(mAnimaFloatName), _vector2.x, _lerpTime);
                    float _lerpB = Mathf.Lerp(_stateMachine.GetAnimatorFloat(mAnimaFloatName2), _vector2.y, _lerpTime);

                    _stateMachine.SetAnimatorFloat(mAnimaFloatName, _lerpA);
                    _stateMachine.SetAnimatorFloat(mAnimaFloatName2, _lerpB);
                }
                else
                {
                    _stateMachine.SetAnimatorFloat(mAnimaFloatName, _vector2.x);
                    _stateMachine.SetAnimatorFloat(mAnimaFloatName2, _vector2.y);
                }
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SetAnimFloatFromBluePrint _event = _eventData as Event_SetAnimFloatFromBluePrint;

            _event.ValueType = mValueType;
            _event.AnimaFloatName = mAnimaFloatName;
            _event.AnimaFloatName2 = mAnimaFloatName2;
            _event.AnimaFloatSpeed = mAnimaFloatSpeed;
            _event.FloatValue = mFloatValue.Clone();
            _event.Vector3Value = mVector3Value.Clone();

            return _event;
        }

        private float GetValue(ActionStatePart _actionState, ActionMachineTime _time)
        {
            return mFloatValue.value(_actionState, _time);
        }

        private Vector2 GetValue2D(ActionStatePart _actionState, ActionMachineTime _time)
        {
            Vector3 _value = mVector3Value.value(_actionState, _time);
            return new Vector2(_value.x, _value.y);
        }
    }
}
