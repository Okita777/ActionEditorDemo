using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_InteractBarrier : IActionEventData
    {
        [SerializeField] private bool mAlignAngle = false;
        [SerializeField] private int mCharacteLimbType;//ECharacteLimbType
        [SerializeField] private Vector3 mOffsetPos;

        private Vector3 mInteractPoint;
        private Vector3 mInitPos;
        private Quaternion mInteractDir;
        private Quaternion mInitRot;
        private float lastGravity;
        private float deltatime = 1;

        #region Property

        [EditorProperty("对齐角度 ", EditorPropertyType.EEPT_Bool)]
        public bool AlignAngle
        {
            get { return mAlignAngle; }
            set { mAlignAngle = value; }
        }
        [EditorProperty("位置偏移 ", EditorPropertyType.EEPT_Vector3)]
        public Vector3 OffsetPos
        {
            get { return mOffsetPos; }
            set { mOffsetPos = value; }
        }
        [EditorProperty("对齐对象 ", EditorPropertyType.EEPT_CharacteLimbType)]
        public int CharacteLimbType
        {
            get { return mCharacteLimbType; }
            set { mCharacteLimbType = value; }
        }


        #endregion

        //public int GetEvenType() => (int)EEvenType.EET_InteractBarrier;
        public int GetEvenType() => 0;

        public IActionEventData Creact() => new Event_InteractBarrier();
        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;

            if (_stateMachine.TryGetLogic(out Ex_Update_CharacterControl _characterControl, nameof(Ex_Update_CharacterControl)))
            {
                Transform _referTransform = GetInteractPos(_stateMachine);


                mInitPos = _referTransform.position;
                mInitRot = _referTransform.rotation;
                if (_stateMachine.TryGetStaticLogic(out Ex_BarrierData _barrier, nameof(Ex_BarrierData)))
                {
                    mInteractDir = mAlignAngle ? _barrier.mInteractRot : _stateMachine.CurUnit.transform.rotation;
                    mInteractPoint = _barrier.mInteractPoint + mInteractDir * mOffsetPos;
                }
                if (_isSingle)
                {
                    {
                        Vector3 _move = mInteractPoint - _referTransform.position;
                        _characterControl.CharacterMove = _move;
                        _characterControl.SetRot(mInteractDir, 5);
                        // _characterControl.ChracterRots[2] = mInteractDir;
                    }

                    return;
                }
                lastGravity = _characterControl.CharacterGravity;
                _characterControl.CharacterGravity = 0;
                _characterControl.PosY = 0;
            }
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            if (_stateMachine.TryGetLogic(out Ex_Update_CharacterControl _characterControl, nameof(Ex_Update_CharacterControl)))
            {
                Transform _referTransform = GetInteractPos(_stateMachine);

                deltatime = _actionTime.Deltatime;
                float _startTime = _actionTime.CurrentTime - _actionTime.TriggerTime;
                float _proportion = _startTime / _actionTime.Duration;

                Vector3 _targetPoint = Vector3.Lerp(mInitPos, mInteractPoint, _proportion);
                Vector3 _move = _targetPoint - _referTransform.position;
                _characterControl.CharacterMove = _move;
            }
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            if (_stateMachine.TryGetLogic(out Ex_Update_CharacterControl _characterControl, nameof(Ex_Update_CharacterControl)))
            {
                if (_interruot)
                {
                    Transform _referTransform = GetInteractPos(_stateMachine);
                    Vector3 _move = mInteractPoint - _referTransform.position;
                    _characterControl.CharacterMove = _move;
                }
            }
            _characterControl.CharacterGravity = lastGravity;
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_InteractBarrier _event = _eventData as Event_InteractBarrier;

            _event.AlignAngle = mAlignAngle;
            _event.CharacteLimbType = mCharacteLimbType;
            _event.OffsetPos = mOffsetPos;

            return _event;
        }

        public Transform GetInteractPos(ActionStateMachine _stateMachine)
        {
            ECharacteLimbType _interactType = (ECharacteLimbType)mCharacteLimbType;
            if (_stateMachine.TryGetCharacterLimb(_interactType, out Transform _target))
            {
                return _target;
            }

            return _stateMachine.CurUnit.transform;
        }
    }
}