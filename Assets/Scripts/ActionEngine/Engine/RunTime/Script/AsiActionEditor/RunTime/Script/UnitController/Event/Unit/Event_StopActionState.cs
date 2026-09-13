using System;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_StopActionState : IActionEventData
    {
        // [SerializeField] private int m_HelpPointID = 0;
        // [SerializeField] private bool m_IsActive = false;

        #region property
        //[EditorProperty("显隐对象", EditorPropertyType.EEPT_Enum)]
        //public int HelpPointID
        //{
        //    get { return m_HelpPointID; }
        //    set { m_HelpPointID = value; }
        //}
        // [EditorProperty("显示", EditorPropertyType.EEPT_Bool)]
        // public bool IsActive
        // {
        //     get { return m_IsActive; }
        //     set { m_IsActive = value; }
        // }
        #endregion

        [NonSerialized] private bool defaultActive = false;
        public int GetEvenType() => -(int)EEvenTypeInternal.EET_StopActionState;
        public IActionEventData Creact() => new Event_StopActionState();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            if (_isSingle)
            {
                _actionState.ActionStateMachine.StopActionState(_actionState);
            }
        }
        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            _actionState.ActionStateMachine.StopActionState(_actionState);
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_StopActionState _event = _eventData as Event_StopActionState;

            return _event;
        }
    }
}