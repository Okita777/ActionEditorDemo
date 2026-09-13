using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_CharacterAddForce : IActionEventData
    {
        [SerializeField] protected GFloat mForceDir_F = new GFloat();
        [SerializeField] protected GFloat mForceDir_R = new GFloat();
        [SerializeField] protected GFloat mForceDir_U = new GFloat();

        #region Property
        // [EditorProperty("力度施加向量 (前: ", EditorPropertyType.EEPT_Float)]
        // public GFloat ForceDir_F
        // {
        //     get { return mForceDir_F; }
        //     set { mForceDir_F = value; }
        // }
        // [EditorProperty("力度施加向量 (右: ", EditorPropertyType.EEPT_Float)]
        // public GFloat ForceDir_R
        // {
        //     get { return mForceDir_R; }
        //     set { mForceDir_R = value; }
        // }
        [EditorProperty("力度施加向量 (上: ", EditorPropertyType.EEPT_GFloat)]
        public GFloat ForceDir_U
        {
            get { return mForceDir_U; }
            set { mForceDir_U = value; }
        }
        #endregion

        public int GetEvenType() => (int)EEvenType.EET_CharacterAddForce;
        public IActionEventData Creact() => new Event_CharacterAddForce();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;

            if (_stateMachine.TryGetLogic(out Ex_Update_CharacterControl _characterControl, nameof(Ex_Update_CharacterControl)))
            {
                _characterControl.PosY = ForceDir_U.GetValue(_actionState);
            }
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            if (_stateMachine.TryGetLogic(out Ex_Update_CharacterControl _characterControl, nameof(Ex_Update_CharacterControl)))
            {
                _characterControl.PosY = ForceDir_U.GetValue(_actionState);
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_CharacterAddForce _event = _eventData as Event_CharacterAddForce;

            _event.ForceDir_U = (GFloat)mForceDir_U.Clone();

            return _event;
        }
    }
}