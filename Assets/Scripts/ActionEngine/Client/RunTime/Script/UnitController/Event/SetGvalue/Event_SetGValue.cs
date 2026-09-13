using System;
using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_SetGValue : IActionEventData
    {
        [SerializeField] protected GValue_Setting mGValueSet = new GValue_Setting();
        [SerializeField] protected GValue_Setting mExitGValueSet = new GValue_Setting();
        [SerializeField] protected GraphEvent_NoValue_Unit m_TargetUnit = new GraphEvent_NoValue_Unit();
        [SerializeField] protected bool m_UpdateValue = false;

        [EditorProperty("设置GValue", EditorPropertyType.EEPT_GValueSetting)]
        public GValue_Setting GValueSet
        {
            get { return mGValueSet; }
            set { mGValueSet = value; }
        }
        [EditorProperty("   每帧设置GValue", EditorPropertyType.EEPT_Bool)]
        public bool UpdateValue
        {
            get { return m_UpdateValue; }
            set { m_UpdateValue = value; }
        }
        [EditorProperty("退出时设置GValue", EditorPropertyType.EEPT_GValueSetting)]
        public GValue_Setting ExitGValueSet
        {
            get { return mExitGValueSet; }
            set { mExitGValueSet = value; }
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
        public int GetEvenType() => (int)EEvenType.EET_SetGValue;
        [NonSerialized] private ActionEngine_Unit m_Unit;
        public IActionEventData Creact() => new Event_SetGValue();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            m_Unit = TargetUnit.value(_actionState, EngineResourcesManager.Instance.MachineTime).GetUnit();

            //Debug.LogWarning("进入设置");
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            mGValueSet.OnSet(m_Unit.ActionStateMachine, _stateMachine);
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (!m_UpdateValue) return;
            m_Unit = TargetUnit.value(_actionState, EngineResourcesManager.Instance.MachineTime).GetUnit();
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            mGValueSet.OnSet(m_Unit.ActionStateMachine, _stateMachine);
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            m_Unit = TargetUnit.value(_actionState, EngineResourcesManager.Instance.MachineTime).GetUnit();
            mExitGValueSet.OnSet(m_Unit.ActionStateMachine, _stateMachine);
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SetGValue _event = _eventData as Event_SetGValue;
            _event.GValueSet = mGValueSet.Clone();
            _event.ExitGValueSet = mExitGValueSet.Clone();
            _event.UpdateValue = m_UpdateValue;
#if UNITY_EDITOR
            if (m_TargetUnit is null)
            {
                m_TargetUnit = new GraphEvent_NoValue_Unit();
            }
#endif
            _event.TargetUnit = TargetUnit.Clone();
            return _event;
        }
    }
}