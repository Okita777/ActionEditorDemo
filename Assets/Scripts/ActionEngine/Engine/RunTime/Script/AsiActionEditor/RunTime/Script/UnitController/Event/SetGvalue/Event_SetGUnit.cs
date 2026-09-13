using System;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_SetGUnit : IActionEventData
    {
        [SerializeField] protected GraphEvent_NoValue_Unit m_GraphEvent = new GraphEvent_NoValue_Unit();
        [SerializeField] protected GValue_SetUnit m_SetFloat = new GValue_SetUnit();
        [SerializeField] protected bool m_ExitRun = false;
        [SerializeField] protected GraphEvent_NoValue_Unit m_GraphEvent_Exit = new GraphEvent_NoValue_Unit();
        [SerializeField] protected GraphEvent_NoValue_Unit m_TargetUnit = new GraphEvent_NoValue_Unit();
        #region property
        [EditorProperty("设置逻辑", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Unit GraphEvent
        {
            get { return m_GraphEvent; }
            set { m_GraphEvent = value; }

        }
        [EditorProperty("设置GV", EditorPropertyType.EEPT_SetGUnit)]
        public GValue_SetUnit SetFloat
        {
            get { return m_SetFloat; }
            set { m_SetFloat = value; }

        }
        [EditorProperty("被设置GV的目标单位", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Unit TargetUnit
        {
            get
            {
#if UNITY_EDITOR
                if (m_TargetUnit is null) m_TargetUnit = new GraphEvent_NoValue_Unit();
#endif
                return m_TargetUnit;
            }
            set { m_TargetUnit = value; }

        }
        [EditorProperty("执行退出时的设置蓝图", EditorPropertyType.EEPT_Bool, LabelWidth = 120)]
        public bool ExitRun
        {
            get { return m_ExitRun; }
            set { m_ExitRun = value; }
        }
        [EditorProperty("设置逻辑(退出时)", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Unit GraphEvent_Exit
        {
            get
            {
#if UNITY_EDITOR
                if (m_GraphEvent_Exit is null) m_GraphEvent_Exit = new GraphEvent_NoValue_Unit();
#endif
                return m_GraphEvent_Exit;
            }
            set { m_GraphEvent_Exit = value; }
        }
        #endregion
        public int GetEvenType() => -(int)EEvenTypeInternal.EET_SetGUnit;
        [NonSerialized] private TargetUnit m_Unit;

        public IActionEventData Creact() => new Event_SetGUnit();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            m_Unit = TargetUnit.value(_actionState, EngineResourcesManager.Instance.MachineTime);
            if (m_Unit is not null)
            {
                TargetUnit val = GraphEvent.value(_actionState, new ActionMachineTime(0, 0, 0, 0));
                m_SetFloat.Set(m_Unit.GetUnit().ActionStateMachine.FirstStatePart, val);
            }
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (m_Unit is not null)
            {
                m_SetFloat.Set(m_Unit.GetUnit().ActionStateMachine.FirstStatePart, GraphEvent.value(_actionState, _actionTime));
            }
        }
        public void Exit(ActionStatePart _actionState, bool _interruot)//interruot 是否因打断轨退出  false代表事件自然结束
        {
            if (ExitRun && m_Unit is not null)
            {
                TargetUnit val = GraphEvent_Exit.value(_actionState, EngineResourcesManager.Instance.MachineTime);
                m_SetFloat.Set(m_Unit.GetUnit().ActionStateMachine.FirstStatePart, val);
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SetGUnit eventCast = _eventData as Event_SetGUnit;
            eventCast.m_GraphEvent = m_GraphEvent.Clone();
            eventCast.m_SetFloat = m_SetFloat.Clone();
            eventCast.ExitRun = ExitRun;
            eventCast.GraphEvent_Exit = GraphEvent_Exit.Clone();
            eventCast.TargetUnit = TargetUnit.Clone();
            return eventCast;
        }
    }
}