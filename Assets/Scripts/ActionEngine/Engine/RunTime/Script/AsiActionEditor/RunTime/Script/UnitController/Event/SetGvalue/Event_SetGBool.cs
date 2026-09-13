using System;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_SetGBool : IActionEventData
    {
        [SerializeField] protected GraphEvent_NoValue_Bool m_GraphEvent = new GraphEvent_NoValue_Bool();
        [SerializeField] protected GValue_SetBool m_SetFloat = new GValue_SetBool();
        [SerializeField] protected bool m_ExitRun = false;
        [SerializeField] protected GraphEvent_NoValue_Bool m_GraphEvent_Exit = new GraphEvent_NoValue_Bool();
        [SerializeField] protected GraphEvent_NoValue_Unit m_TargetUnit = new GraphEvent_NoValue_Unit();
        #region property
        [EditorProperty("设置逻辑(进入时)", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Bool GraphEvent
        {
            get { return m_GraphEvent; }
            set { m_GraphEvent = value; }

        }
        [EditorProperty("设置GV", EditorPropertyType.EEPT_SetGBool)]
        public GValue_SetBool SetFloat
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
        public GraphEvent_NoValue_Bool GraphEvent_Exit
        {
            get
            {
#if UNITY_EDITOR
                if (m_GraphEvent_Exit is null) m_GraphEvent_Exit = new GraphEvent_NoValue_Bool();
#endif
                return m_GraphEvent_Exit;
            }
            set { m_GraphEvent_Exit = value; }
        }
        #endregion
        public int GetEvenType() => -(int)EEvenTypeInternal.EET_SetGBool;

        public IActionEventData Creact() => new Event_SetGBool();
        [NonSerialized] private ActionEngine_Unit m_Unit;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            m_Unit = TargetUnit.value(_actionState, EngineResourcesManager.Instance.MachineTime).GetUnit();
            //if (_isSingle)
            {
                if (m_SetFloat.m_IsSet)
                {
                    bool val = GraphEvent.value(_actionState, EngineResourcesManager.Instance.MachineTime);
                    m_SetFloat.Set(m_Unit.ActionStateMachine.FirstStatePart, val);
                }
            }
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (m_SetFloat.m_IsSet)
            {
                m_SetFloat.Set(m_Unit.ActionStateMachine.FirstStatePart, GraphEvent.value(_actionState, _actionTime));
            }
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)//interruot 是否因打断轨退出  false代表事件自然结束
        {
            if (ExitRun && m_SetFloat.m_IsSet)
            {
                bool val = GraphEvent_Exit.value(_actionState, EngineResourcesManager.Instance.MachineTime);
                m_SetFloat.Set(m_Unit.ActionStateMachine.FirstStatePart, val);
            }
        }


        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SetGBool eventCast = _eventData as Event_SetGBool;
            eventCast.m_GraphEvent = m_GraphEvent.Clone();
            eventCast.m_SetFloat = m_SetFloat.Clone();

            eventCast.ExitRun = ExitRun;
            eventCast.GraphEvent_Exit = GraphEvent_Exit.Clone();
            eventCast.TargetUnit = TargetUnit.Clone();
            return eventCast;
        }
    }
}