using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_MarkUnitDeathState : IActionEventData
    {
        [SerializeField] private bool m_EnterSetDead = true;
        [SerializeField] private bool m_DieEvent = true;
        [SerializeField] private bool m_ExitSetDead = false;
        [SerializeField] private bool m_ExitResetGValue = false;

        [EditorProperty("进入时设置为死亡(否则存活)", EditorPropertyType.EEPT_Bool, LabelWidth = 200)]
        public bool EnterSetDead
        {
            get => m_EnterSetDead;
            set => m_EnterSetDead = value;
        }
        [EditorProperty("进入时执行死亡回调", EditorPropertyType.EEPT_Bool, LabelWidth = 200)]
        public bool DieEvent
        {
            get => m_DieEvent;
            set => m_DieEvent = value;
        }
        [EditorProperty("离开时设置为死亡(否则存活)", EditorPropertyType.EEPT_Bool, LabelWidth = 200)]
        public bool ExitSetDead
        {
            get => m_ExitSetDead;
            set => m_ExitSetDead = value;
        }
        [EditorProperty("离开时初始化GValue", EditorPropertyType.EEPT_Bool, LabelWidth = 200)]
        public bool ExitResetGValue
        {
            get => m_ExitResetGValue;
            set => m_ExitResetGValue = value;
        }
        public int GetEvenType() => -(int)EEvenTypeInternal.EET_MarkUnitDeathState;
        public IActionEventData Creact() => new Event_MarkUnitDeathState();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            // 存亡状态写入由业务侧补充。
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            _stateMachine.SetDieState(EnterSetDead);
            if(DieEvent) _stateMachine.EventSystem.RunEvent_OnDead();
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            // 存亡状态写入由业务侧补充。
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            _stateMachine.SetDieState(ExitSetDead);
            if (ExitResetGValue)
            {
                _stateMachine.InitGValue(true);
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_MarkUnitDeathState eventData = _eventData as Event_MarkUnitDeathState;
            eventData.m_EnterSetDead = m_EnterSetDead;
            eventData.DieEvent = DieEvent;
            eventData.m_ExitSetDead = m_ExitSetDead;
            eventData.ExitResetGValue = m_ExitResetGValue;
            return eventData;
        }
    }
}
