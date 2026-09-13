using System;
using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_CharacterOnMove : IActionEventData
    {
        [SerializeField] protected GraphEvent_NoValue_Vector3 mNoveVelocity = new GraphEvent_NoValue_Vector3();
        [SerializeField] protected GFloat mLerpSpeed = new GFloat(1);
        [SerializeField] private bool mIsMoveInput = false;
        [SerializeField] protected bool mInitMoveVelocity = false;
        //[SerializeField] protected EngineCurve mAnimCurve = 
        //    new EngineCurve(new AnimationCurve(new[] { new Keyframe(0,0), new Keyframe(0,1)} ));

        #region Property
        [EditorProperty("仅移动输入时位移: ", EditorPropertyType.EEPT_Bool)]
        public bool IsMoveInput
        {
            get { return mIsMoveInput; }
            set { mIsMoveInput = value; }
        }

        [EditorProperty("移动速度: ", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Vector3 NoveVelocity
        {
            get { return mNoveVelocity; }
            set { mNoveVelocity = value; }
        }
        [EditorProperty("仅进入时更新一次 [移动速度] ", EditorPropertyType.EEPT_Bool, LabelWidth = 170)]
        public bool InitMoveVelocity
        {
            get { return mInitMoveVelocity; }
            set { mInitMoveVelocity = value; }
        }
        [EditorProperty("过渡速度: ", EditorPropertyType.EEPT_GFloat)]
        public GFloat LerpSpeed
        {
            get { return mLerpSpeed; }
            set { mLerpSpeed = value; }
        }
        //[EditorProperty("移动曲线: ", EditorPropertyType.EEPT_AnimationCurve)]
        //public EngineCurve AnimCurve
        //{
        //    get { 
        //        if(mAnimCurve is null) mAnimCurve = 
        //                new EngineCurve(new AnimationCurve(new[] { new Keyframe(0, 0), new Keyframe(0, 1) }));
        //        return mAnimCurve; 
        //    }
        //    set { mAnimCurve = value; }
        //}

        #endregion
        public int GetEvenType() => (int)EEvenType.EET_CharacterOnMove;
        public IActionEventData Creact() => new Event_CharacterOnMove();

        [NonSerialized] private Vector3 lerpDir;
        [NonSerialized] private Vector3 _cachedMoveDir;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            //if (ActionEngineManager_Input.Instance.IsPlayer(_actionState.ActionStateMachine.CurUnit))
            //{
            //    Debug.LogWarning($"<color=#ffcc00>进入了移动轨道事件</color>  [{_actionState.AnimaLayer}]  [{_actionState.CurrentActionState.Name}]");
            //}
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            Vector3 dir = mNoveVelocity.value(_actionState, new ActionMachineTime(0, 0, 0, 0));
            _cachedMoveDir = dir;

            if (_isSingle)
            {
                //单帧则瞬间位移
                if (_actionState.IsTem)
                {
                    lerpDir = _actionState.Rot * Vector3.forward;
                    _actionState.SetPrivateVector3(GetHashCode(), lerpDir);
                    _actionState.TransLate(dir);
                }
                else
                {
                    lerpDir = _stateMachine.CurUnit.transform.forward;
                    if (_stateMachine.TryGetLogic(out Ex_Update_CharacterControl _characterControl, nameof(Ex_Update_CharacterControl)))
                        _characterControl.CharacterMove = dir;
                }
            }
            else

            {
                lerpDir = Vector3.zero;
                if (_actionState.IsTem)
                    _actionState.SetPrivateVector3(GetHashCode(), lerpDir);
            }
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {

            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            Vector3 dir = mInitMoveVelocity ? _cachedMoveDir : mNoveVelocity.value(_actionState, _actionTime);
            //if (ActionEngineManager_Input.Instance.IsPlayer(_stateMachine.CurUnit))
            //{
            //    Transform playerTrans = _stateMachine.CurUnit.transform;
            //    Vector3 startPoint = playerTrans.TransformPoint(0, 1, 0);
            //    Debug.DrawRay(startPoint, dir, Color.green); 
            //}
            if (IsMoveInput)
            {
                if (_stateMachine.IsMoveInput)
                {
                    if (_actionState.IsTem)
                    {
                        int _id = GetHashCode();
                        if (LerpSpeed.GetValue(_actionState) > 0.1f)
                            lerpDir = Vector3.Lerp(_actionState.GetPrivateVector3(_id, Vector3.zero), dir, LerpSpeed.GetValue(_actionState) * _actionTime.Deltatime);
                        else if (LerpSpeed.GetValue(_actionState) < -0.1f)
                            lerpDir = GetTrackVelocity(_actionTime, dir);
                        else
                            lerpDir = dir;
                        _actionState.SetPrivateVector3(_id, lerpDir);
                        _actionState.Move(lerpDir);
                    }
                    else
                    {
                        if (LerpSpeed.GetValue(_actionState) > 0.1f)
                            lerpDir = Vector3.Lerp(lerpDir, dir, LerpSpeed.GetValue(_actionState) * _actionTime.Deltatime);
                        else if (LerpSpeed.GetValue(_actionState) < -0.1f)
                            lerpDir = GetTrackVelocity(_actionTime, dir);
                        else
                            lerpDir = dir;
                        if (_stateMachine.TryGetLogic(out Ex_Update_CharacterControl _characterControl, nameof(Ex_Update_CharacterControl)))
                        {
                            _characterControl.CharacterVelocity = lerpDir;
                        }
                    }
                }
            }
            else
            {
                if (_actionState.IsTem)
                {
                    int _id = GetHashCode();
                    if (LerpSpeed.GetValue(_actionState) > 0.1f)
                        lerpDir = Vector3.Lerp(_actionState.GetPrivateVector3(_id, Vector3.zero), dir, LerpSpeed.GetValue(_actionState) * _actionTime.Deltatime);
                    else if (LerpSpeed.GetValue(_actionState) < -0.1f)
                        lerpDir = GetTrackVelocity(_actionTime, dir);
                    else
                        lerpDir = dir;
                    _actionState.SetPrivateVector3(_id, lerpDir);
                    _actionState.Move(lerpDir);
                }
                else
                {
                    if (LerpSpeed.GetValue(_actionState) > 0.1f)
                        lerpDir = Vector3.Lerp(lerpDir, dir, LerpSpeed.GetValue(_actionState) * _actionTime.Deltatime);
                    else if (LerpSpeed.GetValue(_actionState) < -0.1f)
                        lerpDir = GetTrackVelocity(_actionTime, dir);
                    else
                        lerpDir = dir;
                    if (_stateMachine.TryGetLogic(out Ex_Update_CharacterControl _characterControl, nameof(Ex_Update_CharacterControl)))
                        _characterControl.CharacterVelocity = lerpDir;
                }
            }
            //if (ActionEngineManager_Input.Instance.IsPlayer(_actionState.ActionStateMachine.CurUnit))
            //{
            //    _stateMachine.TryGetLogic(out Ex_Update_CharacterControl _characterControl);
            //    float speed = _characterControl.CharacterVelocity.magnitude;
            //    if (speed > 10)
            //        Debug.LogWarning($"<color=#ff0000>移动轨道事件捕捉到异常速度</color>  [{_characterControl.CharacterVelocity.magnitude}]  给予的速度[{dir.magnitude}]  [{_actionState.CurrentActionState.Name}]");
            //}
        }

        // 轨道累计位移模式：把"整条轨道总位移"换算为速度，交由消费端乘 deltaTime 积分
        private Vector3 GetTrackVelocity(ActionMachineTime _actionTime, Vector3 _totalDisplacement)
        {
            if (_actionTime.Duration > 0)
            {
                float _durationSeconds = _actionTime.Duration * MotionEngineConst.TimeDoubling_F;
                return _totalDisplacement / _durationSeconds;
            }
            return Vector3.zero;
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_CharacterOnMove _event = _eventData as Event_CharacterOnMove;
            _event.NoveVelocity = mNoveVelocity.Clone();
            _event.LerpSpeed = (GFloat)mLerpSpeed.Clone();
            _event.IsMoveInput = mIsMoveInput;
            _event.InitMoveVelocity = mInitMoveVelocity;
            return _event;
        }
    }
}