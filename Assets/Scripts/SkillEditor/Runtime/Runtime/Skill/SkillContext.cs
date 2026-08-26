using System;
using ActionEditor.TagSystem;
using SkillEditor.Preview;
using System.Collections.Generic;
using System.Linq;

namespace AsiSkillEditor.RunTime
{
    /// <summary>
    /// 资源系统抽象接口。
    /// SkillRuntime 通过它查询和消耗资源，而不直接依赖某个具体组件实现。
    /// </summary>
    public interface ISkillResourceService
    {
        bool HasResource(GameUnit caster, SkillCostResourceType resourceType, float amount);
        bool TryConsumeResource(GameUnit caster, SkillCostResourceType resourceType, float amount);
        float GetResource(GameUnit caster, SkillCostResourceType resourceType);
    }

    /// <summary>
    /// 当前状态驱动 MetaSkill 正处于哪个阶段。
    /// </summary>
    public enum SkillStatePhaseRole
    {
        None,
        Execute,
        Recovery,
    }

    /// <summary>
    /// 当前 MetaSkill 片段结束的原因。
    /// </summary>
    public enum SkillMetaEndReason
    {
        None,
        Normal,
        Interrupted,
        Cancelled,//okita:什么情况会取消
        Timeout,
    }

    /// <summary>
    /// StateController 回传给 SkillRuntime 的通知类型。
    /// </summary>
    public enum SkillStateNotificationKind
    {
        Entered,
        Completed,
        Interrupted,
    }

    /// <summary>
    /// SkillRuntime 与 StateController 共享的可变流程状态。
    ///
    /// 它主要记录：
    /// 1. 当前正在执行的技能节点。
    /// 2. execute/recovery 对应的状态 Id。
    /// 3. 当前激活状态。
    /// 4. continuation 连段窗口是否打开。
    /// </summary>
    public sealed class SkillFlowContext
    {
        public string SkillRuntimeId = string.Empty;
        public string SkillId = string.Empty;
        public string CurrentMetaSkillId = string.Empty;
        public string CurrentNodeId = string.Empty;
        public string ExecuteStateId = string.Empty;
        public StateLayerType ExecuteStateLayer = StateLayerType.None;
        public string RecoveryStateId = string.Empty;
        public StateLayerType RecoveryStateLayer = StateLayerType.None;
        public string ActiveStateId = string.Empty;
        public StateLayerType ActiveStateLayer = StateLayerType.None;
        public SkillStatePhaseRole ActivePhaseRole = SkillStatePhaseRole.None;
        public SkillMetaEndReason LastEndReason = SkillMetaEndReason.None;
        public string LastInterruptSourceStateId = string.Empty;
        public bool IsStateDriven;
        public bool IsAwaitingStateCompletion;
        public bool IsContinuationWindowOpen;

        /// <summary>
        /// 仅清理当前激活状态阶段相关字段，不清除更大范围的技能身份信息。
        /// </summary>
        public void ClearActiveState()
        {
            ActiveStateId = string.Empty;
            ActiveStateLayer = StateLayerType.None;
            ActivePhaseRole = SkillStatePhaseRole.None;
            IsAwaitingStateCompletion = false;
        }
    }

    /// <summary>
    /// 附加在状态切换请求上的技能侧上下文。
    /// StateController 之后会依赖它把状态结果准确回传给所属的 SkillRuntime。
    /// </summary>
    public sealed class SkillTransitionContext
    {
        public string SkillRuntimeId = string.Empty;
        public string SkillId = string.Empty;
        public string MetaSkillId = string.Empty;
        public string NodeId = string.Empty;
        public string ExecuteStateId = string.Empty;
        public StateLayerType ExecuteStateLayer = StateLayerType.None;
        public string RecoveryStateId = string.Empty;
        public StateLayerType RecoveryStateLayer = StateLayerType.None;
        public string ActiveStateId = string.Empty;
        public StateLayerType ActiveStateLayer = StateLayerType.None;
        public SkillStatePhaseRole PhaseRole = SkillStatePhaseRole.None;
        public bool IsContinuation;

        /// <summary>
        /// 复制出另一个阶段用的上下文，同时保留 runtime、skill 和 meta-skill 的身份信息。
        /// </summary>
        /// <param name="phaseRole">复制后的上下文代表哪个阶段。</param>
        /// <param name="activeStateId">该阶段实际激活的状态 Id。</param>
        public SkillTransitionContext CloneForPhase(SkillStatePhaseRole phaseRole, string activeStateId)
        {
            return new SkillTransitionContext
            {
                SkillRuntimeId = SkillRuntimeId,
                SkillId = SkillId,
                MetaSkillId = MetaSkillId,
                NodeId = NodeId,
                ExecuteStateId = ExecuteStateId,
                ExecuteStateLayer = ExecuteStateLayer,
                RecoveryStateId = RecoveryStateId,
                RecoveryStateLayer = RecoveryStateLayer,
                ActiveStateId = activeStateId ?? string.Empty,
                ActiveStateLayer = phaseRole == SkillStatePhaseRole.Recovery ? RecoveryStateLayer : ExecuteStateLayer,
                PhaseRole = phaseRole,
                IsContinuation = IsContinuation,
            };
        }
    }

    /// <summary>
    /// StateController 发给 SkillRuntime 的状态通知。
    /// 用于回传状态进入、正常完成和被打断等结果。
    /// </summary>
    public sealed class SkillStateNotification
    {
        public SkillStateNotificationKind Kind;
        public StateTransitionRequestType RequestType;
        public RecoveryCancelReason RecoveryCancelReason;
        public string StateId = string.Empty;
        public string SourceStateId = string.Empty;
        public string TargetStateId = string.Empty;
        public SkillTransitionContext TransitionContext;
    }

    [Serializable]
    public sealed class MetaSkillContext
    {
        public string SkillRuntimeId = string.Empty;
        public string SkillId = string.Empty;
        public string MetaSkillId = string.Empty;
        public string MetaSkillNodeId = string.Empty;
        public SkillStatePhaseRole PhaseRole = SkillStatePhaseRole.None;

        public GameUnit Caster;
        public GameUnit PrimaryTarget;
        public List<GameUnit> AffectedTargets = new List<GameUnit>();

        public SkillEffectResult CurrentEffectContext;
        public SkillEffectResult LastEffectContext;

        public bool HasExecuted;
        public bool Succeeded = true;

        public DataContext DataContext = new DataContext();
    }

    /// <summary>
    /// 技能系统共享执行上下文。
    ///
    /// 这个对象会在 skill runtime、state runtime、effect、hitbox、bullet 等执行链路之间传递，
    /// 是当前 runtime 层最核心的集成对象之一。
    ///
    /// 它主要承载：
    /// 1. 世界对象引用，例如施法者、武器、当前目标。
    /// 2. 当前 skill/meta-skill/state 的运行态引用。
    /// 3. 各类服务适配器，例如资源、Buff、标签、战斗、动画桥。
    /// 4. 临时黑板数据和调试信息。
    /// </summary>
    public sealed class SkillContext
    {
        /// <summary>
        /// 当前技能的施法者。
        /// 框架内统一使用 GameUnit 表示施法者语义；其他系统要访问实体对象时可经由 UnitObject。
        /// </summary>
        public GameUnit Caster;

        /// <summary>
        /// 当前装备武器的根对象。
        /// 供武器插槽、武器发射点、武器来源时间线逻辑使用。
        /// </summary>
        public object EquippedWeapon;

        /// <summary>
        /// 当前主目标。
        /// 当 effect、hitbox 或 bullet 需要一个默认目标时，会优先使用它。
        /// </summary>
        public GameUnit PrimaryTarget;
        public List<GameUnit> AffectedTargets = new List<GameUnit>();

        /// <summary>
        /// 当前上下文绑定的根 SkillConfig。
        /// </summary>
        public SkillConfig SkillConfig;

        /// <summary>
        /// 当前 SkillRuntime 正在执行的 MetaSkillConfig。
        /// </summary>
        public MetaSkillConfig CurrentMetaSkillConfig;

        /// <summary>
        /// 从当前 runtime 视角看到的激活 StateConfig。
        /// </summary>
        public StateConfig CurrentStateConfig;

        /// <summary>
        /// 单位共享状态机。
        /// 用于状态驱动的技能执行。
        /// </summary>
        public StateController StateController;

        /// <summary>
        /// Skill 与 State 之间共享的流程状态。
        /// 用来桥接 SkillRuntime 和 StateController。
        /// </summary>
        public SkillFlowContext SkillFlowContext;
        public string ActiveBuffSourceId;
        public Action<GameUnit> RegisterTemporaryContributionTarget;

        public MetaSkillContext CurrentMetaSkillContext;
        public MetaSkillContext LastMetaSkillContext;

        public DataContext DataContext = new DataContext();

        public float DebugTimelineTime;
        public string DebugLastTimelineItemType = string.Empty;
        public string DebugLastTimelineItemId = string.Empty;
        public string DebugLastEffectNodeId = string.Empty;

        public ISkillEffectExecutor EffectExecutor;
        public IBuffService BuffService;
        public ITagQueryService TagQueryService;
        public ISkillResourceService ResourceService;
        public ICharacterAnimationController CharacterAnimationController;
        public IBattleResolver CombatResolver;
        public IUnitHitEventSource UnitHitEventSource;
        public IUnitHitEventPublisher UnitHitEventPublisher;
        public IUnitHitStopService HitStopService;
        public ActionEditor.CameraSystem.ICameraFeedbackService CameraFeedbackService;
        public IVfxService VfxService;
        public IAudioService AudioService;
        public ISkillRuntimeObserver RuntimeObserver;
        public Func<StateInputSnapshot> StateInputSnapshotProvider;
        public Func<StateHitSnapshot> StateHitSnapshotProvider;
        public Func<StateBeHitSnapshot> StateBeHitSnapshotProvider;
        public Func<float> BreakValueProvider;
    }
}
