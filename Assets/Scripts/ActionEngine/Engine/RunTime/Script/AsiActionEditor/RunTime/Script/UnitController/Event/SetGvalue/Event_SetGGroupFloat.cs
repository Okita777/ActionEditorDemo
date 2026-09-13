using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_SetGGroupFloat : IActionEventData
    {
        [SerializeField] protected GraphEvent_NoValue_GroupFloat m_GraphEvent = new GraphEvent_NoValue_GroupFloat();
        [SerializeField] protected GGroupFloat m_GGroupVal = new GGroupFloat();
        [SerializeField] protected bool m_ExitRun = false;
        [SerializeField] protected GraphEvent_NoValue_GroupFloat m_GraphEvent_Exit = new GraphEvent_NoValue_GroupFloat();
        [SerializeField] protected GraphEvent_NoValue_Unit m_TargetUnit = new GraphEvent_NoValue_Unit();

        #region property
        [EditorProperty("设置逻辑", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_GroupFloat GraphEvent
        {
            get { return m_GraphEvent; }
            set { m_GraphEvent = value; }
        }
        [EditorProperty("设置GV", EditorPropertyType.EEPT_GGroupFloat)]
        public GGroupFloat GGroupVal
        {
            get { return m_GGroupVal; }
            set { m_GGroupVal = value; }
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
        public GraphEvent_NoValue_GroupFloat GraphEvent_Exit
        {
            get
            {
#if UNITY_EDITOR
                if (m_GraphEvent_Exit is null) m_GraphEvent_Exit = new GraphEvent_NoValue_GroupFloat();
#endif
                return m_GraphEvent_Exit;
            }
            set { m_GraphEvent_Exit = value; }
        }
        #endregion

        public int GetEvenType() => -(int)EEvenTypeInternal.EET_SetGGroupFloat;
        [NonSerialized] private ActionEngine_Unit m_Unit;

        public IActionEventData Creact() => new Event_SetGGroupFloat();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            m_Unit = TargetUnit.value(_actionState, EngineResourcesManager.Instance.MachineTime).GetUnit();
            List<float> val = GraphEvent.value(_actionState, new ActionMachineTime(0, 0, 0, 0));
            m_GGroupVal.SetValue(m_Unit.ActionStateMachine.FirstStatePart, val.ToArray());
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            List<float> val = GraphEvent.value(_actionState, _actionTime);
            m_GGroupVal.SetValue(m_Unit.ActionStateMachine.FirstStatePart, val.ToArray());
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            if (ExitRun)
            {
                List<float> val = GraphEvent_Exit.value(_actionState, EngineResourcesManager.Instance.MachineTime);
                m_GGroupVal.SetValue(m_Unit.ActionStateMachine.FirstStatePart, val.ToArray());
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SetGGroupFloat eventCast = _eventData as Event_SetGGroupFloat;
            eventCast.m_GraphEvent = m_GraphEvent.Clone();
            eventCast.m_GGroupVal = (GGroupFloat)m_GGroupVal.Clone();
            eventCast.ExitRun = ExitRun;
            eventCast.GraphEvent_Exit = GraphEvent_Exit.Clone();
            eventCast.TargetUnit = TargetUnit.Clone();
            return eventCast;
        }
    }
}
