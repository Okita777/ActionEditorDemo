using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_SetPointData : IActionEventData
    {
        [SerializeField] protected SelectTransform m_SelectTransform = new SelectTransform();
        [SerializeField] protected GValue_SetPoint m_PointData = new GValue_SetPoint();

        #region Property
        [EditorProperty("Transform", EditorPropertyType.EEPT_SelectTransform)]
        public SelectTransform SelectTransform
        {
            get { return m_SelectTransform; }
            set { m_SelectTransform = value; }
        }
        [EditorProperty("设置的目标点数据", EditorPropertyType.EEPT_SetGPoint)]
        public GValue_SetPoint PointData
        {
            get { return m_PointData; }
            set { m_PointData = value; }
        }

        #endregion
        public IActionEventData Creact() => new Event_SetPointData();
        public int GetEvenType() => (int)EEvenType.EET_SetPointData;
        [SerializeField] private bool m_IsValid = false;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            m_IsValid = false;

#if UNITY_EDITOR
            if (m_SelectTransform.Get(_stateMachine) is null)
            {
                EngineDebug.LogError("写入PointData时，Transform为空");
                return;
            }
#endif

            m_PointData.Set(_actionState, m_SelectTransform.Get(_stateMachine));
            m_IsValid = true;
        }

        public void Update(ActionStatePart _actionState, bool _isSingle)
        {
            // ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            if (m_IsValid)
            {
                m_PointData.Set(_actionState, m_SelectTransform.Get(_actionState.ActionStateMachine));
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SetPointData _event = (Event_SetPointData)_eventData;
            _event.SelectTransform = m_SelectTransform.Clone();
            _event.PointData = PointData.Clone();
            return _event;
        }
    }
}