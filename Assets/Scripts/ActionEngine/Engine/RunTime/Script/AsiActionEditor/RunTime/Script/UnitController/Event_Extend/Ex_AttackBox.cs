using UnityEngine;

namespace AsiActionEngine.RunTime.Event_Extend
{
    public class Ex_AttackBox : StaticActionLogics
    {
        // public event OnAttackData OnHit; //切换Action时调用
        // public delegate void OnAttackData(bool _isUnit, GameObject _gameObject,Unit _onHiter);

        public override void OnUpdate(ActionStateMachine _actionState)
        {
        }

        public void ExtrudEvent(ActionStatePart _actionStatePart, bool _isUnit, GameObject _gameObject, TargetUnit _onHiter)
        {
            ActionStateMachine _stateMachine = _actionStatePart.ActionStateMachine;
            _stateMachine.EventSystem.RunEvent_OnHit(_isUnit, _gameObject, _onHiter);
        }
    }
}