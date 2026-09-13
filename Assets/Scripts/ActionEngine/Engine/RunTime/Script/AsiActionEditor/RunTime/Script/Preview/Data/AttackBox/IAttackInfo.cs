using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public interface IAttackInfo
    {
        public IAttackInfo Clone();
        /// <summary>
        /// 受击时调用
        /// </summary>
        /// <param name="_attackInfo">攻击信息</param>
        /// <param name="_stateMachine">自身状态机</param>
        /// <param name="_attacker">攻击者</param>
        /// <param name="_hitPoint">攻击位置</param>
        public void BeHit(IAttackInfo _attackInfo, ActionStateMachine _stateMachine, ActionEngine_Unit _attacker);
        /// <summary>
        /// 命中执行的事件
        /// </summary>
        /// <param name="_attackInfo">攻击参数</param>
        /// <param name="_stateMachine">攻击者的状态机</param>
        /// <param name="_hiter">受击者单位</param>
        public void OnHit(IAttackInfo _attackInfo, ActionStateMachine _stateMachine, TargetUnit _hiter);
        public void OnHitStart(ActionStatePart _statePart);
        public void OnHitReStart(ActionStatePart _statePart);

        public void OnHitUpdate(ActionStatePart _statePart, Vector3 _point, Quaternion _rotate);
        public void OnHitEnd(ActionStatePart _statePart);

    }
}