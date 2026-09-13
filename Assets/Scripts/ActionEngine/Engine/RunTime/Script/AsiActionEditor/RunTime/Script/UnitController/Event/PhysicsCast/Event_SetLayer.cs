using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_SetLayer : IActionEventData
    {
        [SerializeField] private byte m_EnterLayer = 0;
        [SerializeField] private byte m_ExitLayer = 0;

        #region property

        [EditorProperty("进入时设置层级", EditorPropertyType.EEPT_LayerMask)]
        public byte EnterLayer
        {
            get { return m_EnterLayer; }
            set { m_EnterLayer = value; }
        }
        [EditorProperty("离开时设置层级", EditorPropertyType.EEPT_LayerMask)]
        public byte ExitLayer
        {
            get { return m_ExitLayer; }
            set { m_ExitLayer = value; }
        }
        #endregion

        public int GetEvenType() => -(int)EEvenTypeInternal.EET_SetLayer;
        public IActionEventData Creact() => new Event_SetLayer();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine stateMachine = _actionState.ActionStateMachine;
            stateMachine.CurUnit.gameObject.layer = m_EnterLayer;
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            ActionStateMachine stateMachine = _actionState.ActionStateMachine;
            stateMachine.CurUnit.gameObject.layer = m_ExitLayer;
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SetLayer _event = _eventData as Event_SetLayer;

            _event.EnterLayer = m_EnterLayer;
            _event.ExitLayer = m_ExitLayer;

            return _event;
        }
    }
}