using System;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_ChangeUnitLayer : IActionEventData
    {
        [SerializeField] protected int m_UnitLayer;

        #region Property
        [EditorProperty("锁定层级: ", EditorPropertyType.EEPT_LayerMask)]
        public int UnitLayer
        {
            get { return m_UnitLayer; }
            set { m_UnitLayer = value; }
        }
        #endregion
        [NonSerialized] private int m_LocalUnitLayer;

        public int GetEvenType() => (int)EEvenType.EET_ChangeUnitLayer;
        public IActionEventData Creact() => new Event_ChangeUnitLayer();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            GameObject _gameObject = _actionState.ActionStateMachine.CurUnit.gameObject;
            m_LocalUnitLayer = _gameObject.layer;
            _gameObject.layer = m_UnitLayer;
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            GameObject _gameObject = _actionState.ActionStateMachine.CurUnit.gameObject;
            _gameObject.layer = m_LocalUnitLayer;
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_ChangeUnitLayer _event = _eventData as Event_ChangeUnitLayer;
            _event.UnitLayer = m_UnitLayer;
            return _event;
        }
    }
}