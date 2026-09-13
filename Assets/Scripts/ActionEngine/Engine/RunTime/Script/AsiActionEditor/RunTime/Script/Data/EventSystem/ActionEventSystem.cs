using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public class ActionEventSystem
    {
        public ActionEventSystem(ActionEngine_Unit _selfUnit)
        {
            m_curUnit = _selfUnit;
        }

        public event OnAttackData OnHit; //命中时调用
        public event OnBeHitData OnBeHit; //受击时调用
        public event OnDeadData OnDead; //死亡时调用
        //public event OnCreactData OnCreact; //从对象池出生时调用
        public event OnActionEvent OnExecuteActionEvent; //执行事件时调用
        public event OnChangeActionData OnChangeAction; //跳转Action时调用


        public delegate void OnAttackData(ActionEngine_Unit _SelfUnit, bool _isUnit, GameObject _gameObject, TargetUnit _onHiter);
        public delegate void OnBeHitData(ActionEngine_Unit _SelfUnit);
        public delegate void OnDeadData(ActionEngine_Unit _SelfUnit);
        //public delegate void OnCreactData(ActionEngine_Unit _SelfUnit);
        public delegate void OnActionEvent(ActionStatePart _selfPart, ActionEvent _event, EActionEventTriggerType _type);
        public delegate void OnChangeActionData(ActionEngine_Unit _SelfUnit, int _actionID, int _mixTime, int _offsetTime);

        private ActionEngine_Unit m_curUnit;
        #region 事件执行
        public void RunEvent_OnHit(bool _isUnit, GameObject _gameObject, TargetUnit _onHiter)
        {
            OnHit?.Invoke(m_curUnit, _isUnit, _gameObject, _onHiter);
        }
        public void RunEvent_OnHit()
        {
            OnBeHit?.Invoke(m_curUnit);
        }
        public void RunEvent_OnDead()
        {
            m_curUnit.ActionStateMachine.SetDieState(true);
            OnDead?.Invoke(m_curUnit);
        }
        //public void RunEvent_OnCreact()
        //{
        //    OnCreact?.Invoke(m_curUnit);
        //}
        public void RunEvent_ChangeAction(int _actionID, int _mixTime, int _offsetTime)
        {
            OnChangeAction?.Invoke(m_curUnit, _actionID, _mixTime, _offsetTime);
        }
        public void RunEvent_ExecuteActionEvent(ActionStatePart _selfPart, ActionEvent _event, EActionEventTriggerType _type)
        {
            OnExecuteActionEvent?.Invoke(_selfPart, _event, _type);
        }

        public void OnReset()
        {
            OnHit = null;
            OnBeHit = null;
            OnDead = null;
            //OnCreact = null;
            OnChangeAction = null;
            OnExecuteActionEvent = null;
        }
        #endregion

    }
}