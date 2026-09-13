using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_TimeScale : IActionEventData
    {
        [SerializeField] protected GFloat m_TimeScale = new GFloat(0.5f);
        [SerializeField] protected byte m_EventType = 0;

        #region Property
        [EditorProperty("时间缩放: ", EditorPropertyType.EEPT_GFloat)]
        public GFloat TimeScale
        {
            get { return m_TimeScale; }
            set { m_TimeScale = value; }
        }
        [EditorProperty("缩放类型: ", EditorPropertyType.EEPT_Enum, EnumNames = new[] { "自身时间", "全局时间", "命中对象时间", "攻击者时间", "命中对象和自身时间", "攻击者和自身时间" })]
        public byte EventType
        {
            get { return m_EventType; }
            set { m_EventType = value; }
        }
        #endregion
        public int GetEvenType() => (int)EEvenType.EET_TimeScale;
        public IActionEventData Creact() => new Event_TimeScale();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            // ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            SetTime(_actionState, m_TimeScale.GetValue(_actionState));
            // _stateMachine.TimeScale = m_TimeScale;
            // _stateMachine.CurAnimator.speed = m_TimeScale;
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            // ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            SetTime(_actionState, 1);
            // _stateMachine.TimeScale = 1;
            // _stateMachine.CurAnimator.speed = 1;
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_TimeScale _event = _eventData as Event_TimeScale;

            _event.TimeScale = (GFloat)m_TimeScale.Clone();
            _event.EventType = m_EventType;

            return _event;
        }

        private void SetTime(ActionStatePart _actionState, float _time)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            if (m_EventType == 0)
            {
                _stateMachine.TimeScale = _time;
                _stateMachine.SetAnimatorSpeed(_time);
            }
            else if (m_EventType == 1)
            {
                Time.timeScale = _time;
            }
            else if (m_EventType == 2)
            {

#if UNITY_EDITOR
                if (_stateMachine.HitUnit == null)
                {
                    EngineDebug.LogError($"无法获取命中者！ 或许已经被销毁\n{_stateMachine.CurUnit.gameObject.name}:   {_actionState.CurrentActionState.Name}");
                }
#endif
                _stateMachine.HitUnit.GetUnit().ActionStateMachine.TimeScale = _time;
                _stateMachine.HitUnit.GetUnit().ActionStateMachine.SetAnimatorSpeed(_time);
            }
            else if (m_EventType == 3)
            {
#if UNITY_EDITOR
                if (_stateMachine.AttackerUnit == null)
                {
                    EngineDebug.LogError($"无法获取命中者！ 或许已经被销毁\n{_stateMachine.CurUnit.gameObject.name}:   {_actionState.CurrentActionState.Name}");
                }
#endif
                _stateMachine.AttackerUnit.ActionStateMachine.TimeScale = _time;
                _stateMachine.AttackerUnit.ActionStateMachine.SetAnimatorSpeed(_time);
            }
            else if (m_EventType == 4)
            {
                _stateMachine.TimeScale = _time;
                _stateMachine.SetAnimatorSpeed(_time);
#if UNITY_EDITOR
                if (_stateMachine.HitUnit == null)
                {
                    EngineDebug.LogError($"无法获取命中者！ 或许已经被销毁\n{_stateMachine.CurUnit.gameObject.name}:   {_actionState.CurrentActionState.Name}");
                }
#endif
                _stateMachine.HitUnit.GetUnit().ActionStateMachine.TimeScale = _time;
                _stateMachine.HitUnit.GetUnit().ActionStateMachine.SetAnimatorSpeed(_time);
            }
            else if (m_EventType == 5)
            {
                _stateMachine.TimeScale = _time;
                _stateMachine.SetAnimatorSpeed(_time);
#if UNITY_EDITOR
                if (_stateMachine.AttackerUnit == null)
                {
                    EngineDebug.LogError($"无法获取命中者！ 或许已经被销毁\n{_stateMachine.CurUnit.gameObject.name}:   {_actionState.CurrentActionState.Name}");
                }
#endif
                _stateMachine.AttackerUnit.ActionStateMachine.TimeScale = _time;
                _stateMachine.AttackerUnit.ActionStateMachine.SetAnimatorSpeed(_time);
            }
        }
    }
}