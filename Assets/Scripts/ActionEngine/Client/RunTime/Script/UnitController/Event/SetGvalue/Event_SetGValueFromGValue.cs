using System;
using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [Serializable]
    public class Event_SetGValueFromGValue : IActionEventData
    {
        [SerializeField] private GValue_CopySetting m_GValueSet = new GValue_CopySetting();
        [SerializeField] private GValue_CopySetting m_ExitGValueSet = new GValue_CopySetting();
        [SerializeField] private GraphEvent_NoValue_Unit m_TargetUnit = new GraphEvent_NoValue_Unit();

        [EditorProperty("设置GValue", EditorPropertyType.EEPT_GValueCopySetting)]
        public GValue_CopySetting GValueSet
        {
            get => m_GValueSet;
            set => m_GValueSet = value;
        }

        [EditorProperty("退出时设置GValue", EditorPropertyType.EEPT_GValueCopySetting)]
        public GValue_CopySetting ExitGValueSet
        {
            get => m_ExitGValueSet;
            set => m_ExitGValueSet = value;
        }

        [EditorProperty("被写入GV的目标单位", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Unit TargetUnit
        {
            get
            {
#if UNITY_EDITOR
                if (m_TargetUnit is null) m_TargetUnit = new GraphEvent_NoValue_Unit();
#endif
                return m_TargetUnit;
            }
            set => m_TargetUnit = value;
        }

        public int GetEvenType() => (int)EEvenType.EET_SetGValueFromGValue;

        [NonSerialized] private ActionEngine_Unit m_Unit;

        public IActionEventData Creact() => new Event_SetGValueFromGValue();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            m_Unit = TargetUnit.value(_actionState, EngineResourcesManager.Instance.MachineTime).GetUnit();
            if (m_Unit == null) return;

            m_GValueSet.OnSet(m_Unit.ActionStateMachine.FirstStatePart, _actionState);
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            if (m_Unit == null) return;

            m_ExitGValueSet.OnSet(m_Unit.ActionStateMachine.FirstStatePart, _actionState);
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SetGValueFromGValue _event = _eventData as Event_SetGValueFromGValue;
            _event.m_GValueSet = m_GValueSet.Clone();
            _event.m_ExitGValueSet = m_ExitGValueSet.Clone();
            _event.m_TargetUnit = TargetUnit.Clone();
            return _event;
        }
    }
}
