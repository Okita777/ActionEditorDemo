using AsiActionEngine.RunTime;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_DestoryUnit : IActionEventData
    {
        // [SerializeField] protected bool m_runDeadEvent = false;

        // [EditorProperty("执行死亡事件回调", EditorPropertyType.EEPT_Bool)]
        // public bool runDeadEvent
        // {
        //     get { return m_runDeadEvent; }
        //     set { m_runDeadEvent = value; } 
        // }
        public int GetEvenType() => (int)EEvenType.EET_DestoryUnit;

        public IActionEventData Creact() => new Event_DestoryUnit();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            _stateMachine.EventSystem.RunEvent_OnDead();
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            // 故意不在此处统一销毁 Unit：
            //   - 玩家：复活由 BattleDataMgr.OnNotifyRebornData → DestroyPlayer → mainPlayer.Destroy()
            //           或回选角 BattleDataMgr.ResetForReturnToRoleSelect → ReleaseAvatarToPool() 统一掌控；
            //   - 怪物：由协议 RemoveUnit(removeFlag=1) → MonsterObject.PlayDeathAnimation → 延迟 Destroy() 统一掌控。
            // 若在此 Exit 直接 DestoryUnit(CurUnit) 会让 PlayerObject.unit / MonsterObject.m_Unit 变成
            // 指向已还池/可能被复用的 "幽灵引用"，导致 OnPreReleaseAvatar 的灯光恢复 / 被动技能卸载
            // 跑在错误的骨骼上，并触发 ActionEngineManager_Unit.OnDestoryUnit 的 "尝试释放已经释放过的资产" 告警。
            // 经核对（截至 2026-05 全工程 Action JSON），所有引用 Event_DestoryUnit 的 Action（玩家/怪物/Boss/精英）
            // 都属于 Unit 而非 Skill / 投射物 / 特效，因此 Exit 不再兜底销毁是安全的。
            // 技能的自销毁请使用 Event_RemoveSkillToUnit；如确需在此处恢复销毁，必须先排查所有引用 Action。
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_DestoryUnit _event = _eventData as Event_DestoryUnit;
            // _event.runDeadEvent = m_runDeadEvent;
            return _event;
        }
    }
}