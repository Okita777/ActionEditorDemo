using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_SetAnimFloat : IActionEventData
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
        [SerializeField] private EAnimFloatFor mAnimFloatFor = EAnimFloatFor.InputToTransDir;

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
        [EditorProperty("参数来源", EditorPropertyType.EEPT_Enum)]
        public EAnimFloatFor AnimFloatFor
        {
            get { return mAnimFloatFor; }
            set { mAnimFloatFor = value; }
        }

        [EditorProperty("参数过渡速度", EditorPropertyType.EEPT_Float)]
        public float AnimaFloatSpeed
        {
            get { return mAnimaFloatSpeed; }
            set { mAnimaFloatSpeed = value; }
        }
        #endregion
        public int GetEvenType() => (int)EEvenType.EET_SetAnimFloat;
        public IActionEventData Creact() => new Event_SetAnimFloat();

        private float lastAnimFloat = 0;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            lastAnimFloat = 0;
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;

            if (_isSingle)
            {
                if (ValueType == EValueType._1D)
                {
                    _stateMachine.SetAnimatorFloat(mAnimaFloatName, GetValue(_stateMachine));
                }
                else
                {
                    Vector2 _vector2 = GetValue2D(_stateMachine);
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
                    lastAnimFloat = Mathf.Lerp(lastAnimFloat, GetValue(_stateMachine),
                        mAnimaFloatSpeed * _actionTime.Deltatime);
                    _stateMachine.SetAnimatorFloat(mAnimaFloatName, lastAnimFloat);
                }
                else
                {
                    _stateMachine.SetAnimatorFloat(mAnimaFloatName, GetValue(_stateMachine));
                }
            }
            else
            {
                Vector2 _vector2 = GetValue2D(_stateMachine);
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
            Event_SetAnimFloat _event = _eventData as Event_SetAnimFloat;

            _event.ValueType = mValueType;
            _event.AnimaFloatName = mAnimaFloatName;
            _event.AnimaFloatName2 = mAnimaFloatName2;
            _event.AnimFloatFor = mAnimFloatFor;
            _event.AnimaFloatSpeed = mAnimaFloatSpeed;

            return _event;
        }

        private float GetValue(ActionStateMachine _stateMachine)
        {
            if (mAnimFloatFor == EAnimFloatFor.InputToTransDir)
            {
                if (_stateMachine.TryGetStaticLogic(out Ex_InputToTransDir _dir, nameof(Ex_InputToTransDir)))
                {
                    return _dir.GetInputToTransDir;
                }
            }
            else if (mAnimFloatFor == EAnimFloatFor.GroundDir)
            {
                Transform _unitTrans = _stateMachine.CurUnit.transform;
                if (Physics.Raycast(_unitTrans.TransformPoint(0, 0.2f, 0), Vector3.down,
                        out RaycastHit _hit, 1))
                {
                    float _offset = Vector3.SignedAngle(_hit.normal, _unitTrans.forward, _unitTrans.right);
                    _offset -= 90;
                    return _offset;
                }
            }
            return 0;
        }

        private Vector2 GetValue2D(ActionStateMachine _stateMachine)
        {
            if (mAnimFloatFor == EAnimFloatFor.CamOffsetToUnit)
            {
                Quaternion _rotOffset =
                    _stateMachine.CurUnit.transform.rotation * Quaternion.Inverse(_stateMachine.GetCamPointRot());
                return new Vector2(_rotOffset.eulerAngles.x, _rotOffset.eulerAngles.y);
            }
            else if (mAnimFloatFor == EAnimFloatFor.InputToTransDir)
            {
                Vector3 forward = Quaternion.Euler(0, GetValue(_stateMachine), 0) * Vector3.forward;
                return new Vector2(forward.x, forward.z);
                // if (_stateMachine.TryGetStaticLogic(out Ex_InputToTransDir _dir))
                // {
                //     //todo Y轴角度差没做
                //     float Valuer_X = _dir.GetInputToTransDir;
                //     float Valuer_Y = _dir.GetInputToTransDir;
                // }
            }
            else if (mAnimFloatFor == EAnimFloatFor.GroundDir)
            {
                Transform _unitTrans = _stateMachine.CurUnit.transform;
                if (Physics.Raycast(_unitTrans.TransformPoint(0, 0.2f, 0), Vector3.down,
                        out RaycastHit _hit, 1))
                {
                    //todo 地面角度差

                    // float _offset = Vector3.SignedAngle(_hit.normal, _unitTrans.forward, _unitTrans.right);
                    // _offset -= 90;
                    // return _offset;
                }
            }
            return Vector2.up;
        }
    }
}