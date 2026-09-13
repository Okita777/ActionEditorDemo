using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_IgnoreLayerCollision : IActionEventData
    {
        // [SerializeField] private byte m_AnimationID = 2;
        // [SerializeField] private int m_AnimationName = null;
        [SerializeField] private byte m_MixTime = 0;
        [SerializeField] private byte m_OffsetTime = 0;

        #region property

        [EditorProperty("目标层级1", EditorPropertyType.EEPT_LayerMask)]
        public byte MixTime
        {
            get { return m_MixTime; }
            set { m_MixTime = value; }
        }
        [EditorProperty("目标层级2", EditorPropertyType.EEPT_LayerMask)]
        public byte OffsetTime
        {
            get { return m_OffsetTime; }
            set { m_OffsetTime = value; }
        }
        #endregion

        public int GetEvenType() => -(int)EEvenTypeInternal.EET_IgnoreLayerCollision;
        public IActionEventData Creact() => new Event_IgnoreLayerCollision();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            // ActionStateMachine stateMachine = _actionState.ActionStateMachine;
            Physics.IgnoreLayerCollision(m_MixTime, m_OffsetTime, true);
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            Physics.IgnoreLayerCollision(m_MixTime, m_OffsetTime, false);
            //Physics.IgnoreCollision()
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_IgnoreLayerCollision _event = _eventData as Event_IgnoreLayerCollision;

            _event.MixTime = m_MixTime;
            _event.OffsetTime = m_OffsetTime;

            return _event;
        }
    }
}