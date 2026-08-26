using System;
using System.Collections.Generic;
using ActionEditor.TagSystem;
using ActionEditor.CharacterMotion;
using SkillEditor.Preview;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    /// <summary>
    /// 状态切换请求的来源类型。
    /// 不同来源会影响是否检查中断规则、是否向技能运行时发送完成或中断通知。
    /// </summary>
    public enum StateTransitionRequestType
    {
        Interrupt,
        ExternalTry,
        ExternalForce,
        DefaultNext,
        SkillDriven,
        RecoveryCancel,
    }

    /// <summary>
    /// Recovery 被提前结束的原因。目标状态由发起方决定，原因只负责资格校验和结果追踪。
    /// </summary>
    public enum RecoveryCancelReason
    {
        None,
        Skill,
        Movement,
        HitReaction,
        Forced,
    }

    /// <summary>
    /// 一次状态切换请求的数据包。
    /// StateController 会根据它决定目标状态、起播时间、是否忽略中断规则以及是否携带技能上下文。
    /// </summary>
    public sealed class StateTransitionRequest
    {
        /// <summary>
        /// 切换请求来源类型。
        /// </summary>
        public StateTransitionRequestType RequestType;

        /// <summary>
        /// 源状态 Id；为空时会由 StateController 自动填入当前状态。
        /// </summary>
        public string SourceStateId = string.Empty;

        /// <summary>
        /// 目标状态 Id。
        /// </summary>
        public string TargetStateId = string.Empty;

        /// <summary>
        /// 触发本次切换的中断配置，仅中断切换时通常有值。
        /// </summary>
        public StateInterruptConfig InterruptConfig;

        /// <summary>
        /// 是否忽略当前状态的中断规则。
        /// </summary>
        public bool IgnoreInterruptRules;

        /// <summary>
        /// 进入目标状态后从哪个时间点开始播放，单位为秒。
        /// </summary>
        public float RequestedStartTime;

        /// <summary>
        /// 技能状态切换上下文，用于把 StateController 的回调路由回对应 SkillRuntime。
        /// </summary>
        public SkillTransitionContext SkillTransitionContext;

        /// <summary>
        /// 源状态层提示，用于分层状态机下更快定位请求归属层。
        /// </summary>
        public StateLayerType? SourceLayerHint;

        /// <summary>
        /// 是否是白名单式全局强制切换。
        /// </summary>
        public bool IsGlobalForcedTransition;

        /// <summary>
        /// 当 RequestType 为 RecoveryCancel 时记录取消来源。
        /// </summary>
        public RecoveryCancelReason RecoveryCancelReason;
    }

    /// <summary>
    /// 状态运行时依赖的外部环境。
    /// 包含单位对象、技能上下文、标签服务，以及输入、命中、受击、破防值等快照提供器。
    /// </summary>
    public sealed class StateRuntimeContext
    {
        /// <summary>
        /// 当前状态所属单位或角色对象。
        /// </summary>
        public GameUnit Unit;

        /// <summary>
        /// 技能系统共享上下文。
        /// </summary>
        public SkillContext SkillContext;

        /// <summary>
        /// 标签查询服务，用于状态和中断条件检查。
        /// </summary>
        public ITagQueryService TagQueryService;

        /// <summary>
        /// 输入快照提供器。
        /// </summary>
        public Func<StateInputSnapshot> InputSnapshotProvider;

        /// <summary>
        /// 命中快照提供器。
        /// </summary>
        public Func<StateHitSnapshot> HitSnapshotProvider;

        /// <summary>
        /// 受击快照提供器。
        /// </summary>
        public Func<StateBeHitSnapshot> BeHitSnapshotProvider;

        /// <summary>
        /// 破防值提供器。
        /// </summary>
        public Func<float> BreakValueProvider;

        /// <summary>KCC 最近一次更新产生的运动事实。</summary>
        public Func<CharacterMotionSnapshot> MotionSnapshotProvider;
    }

    /// <summary>
    /// 单次状态中断条件评估上下文。
    /// 条件对象会读取这里的数据来判断中断是否成立。
    /// </summary>
    public sealed class StateInterruptContext
    {
        /// <summary>
        /// 当前 StateController。
        /// </summary>
        public StateController Controller;

        /// <summary>
        /// 状态运行时环境。
        /// </summary>
        public StateRuntimeContext RuntimeContext;

        /// <summary>
        /// 当前正在评估的状态配置。
        /// </summary>
        public StateConfig CurrentStateConfig;

        /// <summary>
        /// 当前正在评估的中断配置。
        /// </summary>
        public StateInterruptConfig InterruptConfig;

        /// <summary>
        /// 输入快照；延迟中断时可能是窗口开启时缓存的输入。
        /// </summary>
        public StateInputSnapshot InputSnapshot;

        /// <summary>
        /// 命中快照。
        /// </summary>
        public StateHitSnapshot HitSnapshot;

        /// <summary>
        /// 受击快照。
        /// </summary>
        public StateBeHitSnapshot BeHitSnapshot;

        /// <summary>
        /// 当前评估时间。
        /// </summary>
        public float ElapsedTime;

        /// <summary>
        /// 上一帧评估时间。
        /// </summary>
        public float PreviousTime;

        /// <summary>
        /// 当前破防值。
        /// </summary>
        public float BreakValue;
    }

    /// <summary>
    /// 一帧输入状态快照。
    /// 用于状态中断条件判断，区分按住、按下、抬起、短按释放和长按等输入。
    /// </summary>
    public struct StateInputSnapshot
    {
        /// <summary>
        /// 当前帧是否存在移动输入。
        /// </summary>
        public bool IsMoveInput;

        /// <summary>
        /// 上一帧是否存在移动输入。
        /// </summary>
        public bool IsMoveInputPre;

        /// <summary>
        /// 当前保持按住的动作集合。
        /// </summary>
        public HashSet<string> HeldActions;

        /// <summary>
        /// 当前帧按下的动作集合。
        /// </summary>
        public HashSet<string> DownActions;

        /// <summary>
        /// 当前帧抬起的动作集合。
        /// </summary>
        public HashSet<string> UpActions;

        /// <summary>
        /// 当前帧短按释放的动作集合。
        /// </summary>
        public HashSet<string> ShortReleasedActions;

        /// <summary>
        /// 当前帧进入长按判定的动作集合。
        /// </summary>
        public HashSet<string> LongPressStartedActions;

        /// <summary>
        /// 当前帧长按释放的动作集合。
        /// </summary>
        public HashSet<string> LongPressReleasedActions;
    }

    /// <summary>
    /// 当前帧命中信息快照。
    /// </summary>
    public struct StateHitSnapshot
    {
        /// <summary>
        /// 当前帧是否命中过目标。
        /// </summary>
        public bool HasHit;

        /// <summary>
        /// 命中的目标对象。
        /// </summary>
        public GameUnit HitTarget;
    }

    /// <summary>
    /// 当前帧受击信息快照。
    /// </summary>
    public struct StateBeHitSnapshot
    {
        /// <summary>
        /// 当前帧是否受到攻击。
        /// </summary>
        public bool WasHit;

        /// <summary>
        /// 攻击来源对象。
        /// </summary>
        public GameUnit Attacker;
    }

    /// <summary>
    /// 单个活动状态的运行时。
    /// 负责推进状态时间线、记录上一帧时间、缓存延迟中断输入，并保存技能切换上下文。
    /// </summary>
    public sealed class ActiveStateRuntime
    {
        /// <summary>
        /// 创建活动状态运行时。
        /// </summary>
        /// <param name="config">状态配置。</param>
        /// <param name="skillContext">技能共享上下文。</param>
        public ActiveStateRuntime(StateConfig config, SkillContext skillContext)
        {
            Config = config;
            TimelineRuntime = new StateTimelineExecutionRuntime(config != null ? config.Timeline : null, skillContext);
            BufferedInputs = new Dictionary<StateInterruptConfig, StateInputSnapshot>();
        }

        /// <summary>
        /// 当前活动状态配置。
        /// </summary>
        public StateConfig Config { get; }

        /// <summary>
        /// 当前状态已经运行的时间。
        /// </summary>
        public float ElapsedTime => TimelineRuntime != null ? TimelineRuntime.ElapsedTime : 0f;

        /// <summary>
        /// 上一帧状态运行时间。
        /// </summary>
        public float PreviousTime { get; private set; }

        /// <summary>
        /// 当前状态时间线运行时。
        /// </summary>
        public StateTimelineExecutionRuntime TimelineRuntime { get; }

        /// <summary>
        /// 延迟中断使用的输入缓存。
        /// key 为中断配置，value 为窗口内捕获的输入快照。
        /// </summary>
        public Dictionary<StateInterruptConfig, StateInputSnapshot> BufferedInputs { get; }

        /// <summary>
        /// 如果当前状态由技能驱动，则这里保存对应的技能切换上下文。
        /// </summary>
        public SkillTransitionContext SkillTransitionContext { get; set; }

        /// <summary>
        /// 当前状态持有的运动策略请求。退出时只释放该版本，避免误删新状态策略。
        /// </summary>
        internal MovementPolicyHandle MovementPolicyHandle { get; set; }

        /// <summary>
        /// 当前状态时间线是否已经完成。
        /// </summary>
        public bool IsCompleted => TimelineRuntime != null && TimelineRuntime.IsCompleted;

        /// <summary>
        /// 重置状态运行时。
        /// </summary>
        /// <param name="startTime">重置后立即推进到的起始时间。</param>
        public void Reset(float startTime = 0f)
        {
            PreviousTime = 0f;
            TimelineRuntime?.Reset();
            if (TimelineRuntime != null && startTime > 0f)
            {
                TimelineRuntime.Tick(startTime);
            }

            BufferedInputs.Clear();
        }

        /// <summary>
        /// 推进当前状态时间线。
        /// </summary>
        /// <param name="deltaTime">本帧推进的时间增量。</param>
        public void Tick(float deltaTime)
        {
            PreviousTime = ElapsedTime;
            TimelineRuntime?.Tick(deltaTime);
        }

        /// <summary>
        /// 结束当前状态时间线。
        /// </summary>
        /// <param name="interrupted">是否以中断方式结束。</param>
        public void End(bool interrupted)
        {
            TimelineRuntime?.End(interrupted);
        }
    }

    /// <summary>
    /// 角色状态机运行时控制器。
    /// 负责进入默认状态、推进当前状态、评估中断、提交状态切换，并向技能系统回发技能状态通知。
    /// </summary>
    public sealed class StateController
    {
        /// <summary>
        /// 状态配置标签写入标签系统时使用的来源标识。
        /// </summary>
        private const string StateTagSourceId = "StateController.ConfigTags";

        /// <summary>
        /// 状态机运行时依赖环境。
        /// </summary>
        private readonly StateRuntimeContext _context;

        /// <summary>
        /// 按 StateId 索引的状态配置表。
        /// </summary>
        private readonly Dictionary<string, StateConfig> _statesById = new Dictionary<string, StateConfig>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 分层活动态运行时容器。
        /// 运行时只维护各层自己的当前活动状态。
        /// </summary>
        private readonly Dictionary<StateLayerType, StateLayerRuntime> _layerRuntimes = new Dictionary<StateLayerType, StateLayerRuntime>();

        private static readonly StateLayerType[] OrderedLayers =
        {
            StateLayerType.Locomotion,
            StateLayerType.Action,
        };

        /// <summary>
        /// 由各层当前状态聚合出的共享控制事实。
        /// 当前阶段先承载 locomotion 可驱动性、输入锁和安全回退诉求。
        /// </summary>
        private readonly StateSharedControlContext _sharedControlContext = new StateSharedControlContext();

        /// <summary>
        /// 汇总所有活动状态的运动策略请求。层优先级由 StateLayerRuntime 提供。
        /// </summary>
        private readonly CharacterMovementPolicyController _movementPolicyController = new CharacterMovementPolicyController();

        /// <summary>
        /// 当前单位统一使用的后摇取消策略。
        /// </summary>
        private readonly UnitRecoveryCancelPolicy _recoveryCancelPolicy;

        /// <summary>
        /// 技能状态进入、完成或被中断时触发的通知事件。
        /// </summary>
        public event Action<SkillStateNotification> SkillStateChanged;

        /// <summary>
        /// 创建状态控制器。
        /// </summary>
        /// <param name="states">可用状态配置集合。</param>
        /// <param name="context">状态机运行时依赖环境。</param>
        /// <param name="layerDefaultStates">各层默认状态配置。</param>
        public StateController(
            IEnumerable<StateConfig> states,
            StateRuntimeContext context,
            IEnumerable<UnitLayerDefaultStateConfig> layerDefaultStates,
            UnitRecoveryCancelPolicy recoveryCancelPolicy = null)
        {
            _context = context ?? new StateRuntimeContext();
            _recoveryCancelPolicy = recoveryCancelPolicy ?? new UnitRecoveryCancelPolicy();
            InitializeLayerRuntimes(layerDefaultStates);

            if (states == null)
            {
                return;
            }

            foreach (StateConfig state in states)
            {
                if (state == null || string.IsNullOrWhiteSpace(state.StateId) || _statesById.ContainsKey(state.StateId))
                {
                    continue;
                }

                _statesById.Add(state.StateId, state);
            }

            ValidateLayerConfiguration();
        }

        /// <summary>
        /// 获取指定层当前活动状态配置。
        /// 第一阶段如果该层没有活动态，则返回 null。
        /// </summary>
        public StateConfig GetCurrentState(StateLayerType layer)
        {
            return _layerRuntimes.TryGetValue(layer, out StateLayerRuntime runtime) && runtime != null && runtime.Current != null
                ? runtime.Current.Config
                : null;
        }

        /// <summary>
        /// 获取指定层当前活动状态 Id。
        /// </summary>
        public string GetCurrentStateId(StateLayerType layer)
        {
            StateConfig state = GetCurrentState(layer);
            return state != null ? state.StateId : string.Empty;
        }

        /// <summary>
        /// 获取指定层当前活动状态已经运行的时间。
        /// </summary>
        public float GetStateElapsedTime(StateLayerType layer)
        {
            return _layerRuntimes.TryGetValue(layer, out StateLayerRuntime runtime) && runtime != null && runtime.Current != null
                ? runtime.Current.ElapsedTime
                : 0f;
        }

            /// <summary>
            /// 判断指定层当前是否处于状态驱动技能的 Recovery 阶段。
            /// </summary>
            public bool IsRecoveryActive(StateLayerType layer)
            {
                return _layerRuntimes.TryGetValue(layer, out StateLayerRuntime runtime) &&
                   IsRecoveryState(runtime != null ? runtime.Current : null);
            }

        /// <summary>
        /// 判断指定层当前安装的技能状态是否归属于给定 SkillRuntime。
        /// </summary>
        public bool IsSkillRuntimeActive(string skillRuntimeId, StateLayerType layer)
        {
            if (string.IsNullOrWhiteSpace(skillRuntimeId) ||
                !_layerRuntimes.TryGetValue(layer, out StateLayerRuntime runtime) ||
                runtime == null ||
                runtime.Current == null ||
                runtime.Current.SkillTransitionContext == null)
            {
                return false;
            }

            return string.Equals(
                runtime.Current.SkillTransitionContext.SkillRuntimeId,
                skillRuntimeId,
                StringComparison.OrdinalIgnoreCase);
        }

            /// <summary>
            /// 判断目标层当前是否允许由新技能取消 Recovery。
            /// 非 Recovery 状态返回 true，表示技能请求继续遵守原有状态规则。
            /// </summary>
            public bool CanRequestSkillTransition(StateLayerType layer)
            {
                if (!_layerRuntimes.TryGetValue(layer, out StateLayerRuntime runtime) || runtime == null || !IsRecoveryState(runtime.Current))
                {
                    return true;
                }

                return _recoveryCancelPolicy.AllowSkillCancel;
            }

        /// <summary>
        /// 获取当前帧聚合后的共享控制事实。
        /// 供后续 locomotion、输入和表现层逐步改造成真正按层消费。
        /// </summary>
        public StateSharedControlContext SharedControlContext => _sharedControlContext;

        /// <summary>
        /// 当前最终生效的状态运动策略。
        /// </summary>
        public StateMovementProfile MovementPolicy => _movementPolicyController.Current;

        /// <summary>
        /// 状态配置只读表。
        /// </summary>
        public IReadOnlyDictionary<string, StateConfig> States => _statesById;

        /// <summary>
        /// 外部尝试切换到指定状态。
        /// 会遵守当前状态的中断窗口和目标状态限制。
        /// </summary>
        /// <param name="targetStateId">目标状态 Id。</param>
        /// <returns>切换是否成功。</returns>
        public bool TryChangeState(string targetStateId)
        {
            return TryChangeState(new StateTransitionRequest
            {
                RequestType = StateTransitionRequestType.ExternalTry,
                TargetStateId = targetStateId,
                IgnoreInterruptRules = false,
                RequestedStartTime = 0f,
            });
        }

        /// <summary>外部强制切换状态，忽略当前状态中断规则。</summary>
        public bool ForceChangeState(string targetStateId, float startTime = 0f)
        {
            return TryChangeState(new StateTransitionRequest
            {
                RequestType = StateTransitionRequestType.ExternalForce,
                TargetStateId = targetStateId,
                IgnoreInterruptRules = true,
                RequestedStartTime = Mathf.Max(0f, startTime),
            });
        }

        /// <summary>
        /// 按完整状态切换请求尝试切换状态。
        /// </summary>
        /// <param name="request">状态切换请求。</param>
        /// <returns>切换是否成功。</returns>
        public bool TryChangeState(StateTransitionRequest request)
        {
            if (request == null)
            {
                return false;
            }

            ActiveStateRuntime sourceActiveState = ResolveSourceActiveStateRuntime(request);
            if (string.IsNullOrWhiteSpace(request.SourceStateId))
            {
                request.SourceStateId = sourceActiveState != null ? sourceActiveState.Config?.StateId ?? string.Empty : string.Empty;
            }

            if (!request.SourceLayerHint.HasValue && sourceActiveState != null && sourceActiveState.Config != null)
            {
                request.SourceLayerHint = sourceActiveState.Config.Layer;
            }

            return CommitTransition(request);
        }

        /// <summary>
        /// 推进状态机。
        /// 会自动进入默认状态、推进各层当前状态、同步主动画，并按优先级处理切换与自然结束。
        /// </summary>
        /// <param name="deltaTime">本帧推进的时间增量。</param>
        public void Tick(float deltaTime)
        {
            if (!HasAnyActiveState() && !TryEnterDefaultState())
            {
                return;
            }

            if (deltaTime <= 0f)
            {
                return;
            }

            EnsureDefaultLayerStates();
            RefreshSharedControlContext();
            ApplySharedLocomotionControl();
            RefreshSharedControlContext();

            // 技能输入在 StateController.Tick 之前处理，因此同帧技能取消天然优先于移动取消。
            // 移动取消又先于状态推进和自然结束，避免后摇末帧先闪回默认态再处理输入。
            TryCancelRecoveryByMovement();

            List<StateLayerRuntime> orderedLayerRuntimes = GetOrderedLayerRuntimes(ascending: false);
            for (int i = 0; i < orderedLayerRuntimes.Count; i++)
            {
                TickLayer(orderedLayerRuntimes[i], deltaTime);
            }

            RefreshSharedControlContext();

            List<StateTransitionRequest> interruptRequests = new List<StateTransitionRequest>(orderedLayerRuntimes.Count);
            for (int i = 0; i < orderedLayerRuntimes.Count; i++)
            {
                StateLayerRuntime layerRuntime = orderedLayerRuntimes[i];
                StateTransitionRequest interruptRequest = layerRuntime != null && layerRuntime.Current != null
                    ? EvaluateInterrupts(layerRuntime.Current)
                    : null;
                if (interruptRequest != null)
                {
                    interruptRequests.Add(interruptRequest);
                }
            }

            HashSet<StateLayerType> transitionedLayers = new HashSet<StateLayerType>();
            for (int i = 0; i < interruptRequests.Count; i++)
            {
                StateTransitionRequest interruptRequest = interruptRequests[i];
                if (CommitTransition(interruptRequest) && interruptRequest.SourceLayerHint.HasValue)
                {
                    transitionedLayers.Add(interruptRequest.SourceLayerHint.Value);
                }
            }

            for (int i = 0; i < orderedLayerRuntimes.Count; i++)
            {
                StateLayerRuntime layerRuntime = orderedLayerRuntimes[i];
                if (layerRuntime != null && !transitionedLayers.Contains(layerRuntime.LayerType))
                {
                    TryProcessLayerNaturalEnd(layerRuntime);
                }
            }
        }

        /// <summary>
        /// 确保所有声明了默认状态的层都拥有可运行的当前状态。
        /// </summary>
        private void EnsureDefaultLayerStates()
        {
            List<StateLayerRuntime> orderedLayerRuntimes = GetOrderedLayerRuntimes(ascending: true);
            for (int i = 0; i < orderedLayerRuntimes.Count; i++)
            {
                StateLayerRuntime layerRuntime = orderedLayerRuntimes[i];
                if (layerRuntime == null || layerRuntime.Current != null || string.IsNullOrWhiteSpace(layerRuntime.DefaultStateId))
                {
                    continue;
                }

                CommitTransition(new StateTransitionRequest
                {
                    RequestType = StateTransitionRequestType.ExternalForce,
                    SourceLayerHint = layerRuntime.LayerType,
                    TargetStateId = layerRuntime.DefaultStateId,
                    IgnoreInterruptRules = true,
                    RequestedStartTime = 0f,
                });
            }
        }

        /// <summary>
        /// 推进指定层当前活动状态时间线。
        /// 没有活动态时会直接跳过，不会自动创建该层默认态。
        /// </summary>
        /// <param name="layerType">要推进的层类型。</param>
        /// <param name="deltaTime">本帧推进的时间增量。</param>
        private void TickLayer(StateLayerRuntime layerRuntime, float deltaTime)
        {
            if (layerRuntime == null || layerRuntime.Current == null)
            {
                return;
            }

            layerRuntime.Current.Tick(deltaTime);
        }

        /// <summary>
        /// 处理指定层当前活动状态的自然结束逻辑。
        /// 会优先尝试状态自身的默认下一状态，其次尝试该层默认态。
        /// </summary>
        /// <param name="layerType">要处理的层类型。</param>
        /// <returns>成功提交自然结束切换时返回 true，否则返回 false。</returns>
        private bool TryProcessLayerNaturalEnd(StateLayerRuntime layerRuntime)
        {
            if (layerRuntime == null || layerRuntime.Current == null)
            {
                return false;
            }

            ActiveStateRuntime activeState = layerRuntime.Current;
            StateLayerType layerType = activeState != null && activeState.Config != null ? activeState.Config.Layer : layerRuntime.LayerType;
            if (!HasReachedNaturalEnd(activeState.Config, activeState.ElapsedTime))
            {
                return false;
            }

            string currentStateId = activeState.Config != null ? activeState.Config.StateId : string.Empty;
            string defaultNextStateId = activeState.Config != null ? activeState.Config.DefaultNextStateId : string.Empty;
            if (string.IsNullOrWhiteSpace(defaultNextStateId))
            {
                defaultNextStateId = ResolveLayerFallbackStateId(activeState);
            }

            if (!string.IsNullOrWhiteSpace(defaultNextStateId))
            {
                bool didTransition = CommitTransition(new StateTransitionRequest
                {
                    RequestType = StateTransitionRequestType.DefaultNext,
                    SourceStateId = currentStateId,
                    SourceLayerHint = activeState.Config != null ? activeState.Config.Layer : (StateLayerType?)null,
                    TargetStateId = defaultNextStateId,
                    IgnoreInterruptRules = true,
                    RequestedStartTime = 0f,
                    SkillTransitionContext = BuildNextSkillTransitionContext(activeState, defaultNextStateId),
                });

                if (didTransition)
                {
                    return true;
                }

                defaultNextStateId = ResolveLayerFallbackStateId(activeState);
                if (!string.IsNullOrWhiteSpace(defaultNextStateId) &&
                    !string.Equals(defaultNextStateId, activeState.Config != null ? activeState.Config.DefaultNextStateId : string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    bool didFallbackTransition = CommitTransition(new StateTransitionRequest
                    {
                        RequestType = StateTransitionRequestType.DefaultNext,
                        SourceStateId = currentStateId,
                        SourceLayerHint = activeState.Config != null ? activeState.Config.Layer : (StateLayerType?)null,
                        TargetStateId = defaultNextStateId,
                        IgnoreInterruptRules = true,
                        RequestedStartTime = 0f,
                        SkillTransitionContext = BuildNextSkillTransitionContext(activeState, defaultNextStateId),
                    });

                    if (didFallbackTransition)
                    {
                        return true;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(ResolveLayerDefaultStateId(layerType)))
            {
                ExitActiveState(activeState, new StateTransitionRequest
                {
                    RequestType = StateTransitionRequestType.DefaultNext,
                    SourceStateId = currentStateId,
                    SourceLayerHint = layerType,
                    IgnoreInterruptRules = true,
                });
                return true;
            }

            if (string.Equals(currentStateId, ResolveLayerDefaultStateId(activeState.Config != null ? activeState.Config.Layer : layerType), StringComparison.OrdinalIgnoreCase))
            {
                activeState.Reset(0f);
                _context.SkillContext?.CharacterAnimationController?.PlayStateAnimation(_context.SkillContext, activeState.Config, null);
            }

            return false;
        }

        /// <summary>
        /// 当 Action 层处于 Recovery 且存在有效移动输入时，直接回到 Action 层默认状态。
        /// </summary>
        private bool TryCancelRecoveryByMovement()
        {
            if (!_recoveryCancelPolicy.AllowMoveCancel ||
                !_layerRuntimes.TryGetValue(StateLayerType.Action, out StateLayerRuntime actionRuntime) ||
                actionRuntime == null ||
                !IsRecoveryState(actionRuntime.Current))
            {
                return false;
            }

            // Recovery 可能仍声明移动锁定，但移动输入本身正是取消意图，
            // 因此这里必须读取原始输入，不能被共享控制结果过滤。
            StateInputSnapshot inputSnapshot = _context.InputSnapshotProvider != null
                ? _context.InputSnapshotProvider()
                : default(StateInputSnapshot);
            if (!inputSnapshot.IsMoveInput)
            {
                return false;
            }

            string defaultStateId = ResolveLayerDefaultStateId(StateLayerType.Action);
            if (string.IsNullOrWhiteSpace(defaultStateId))
            {
                return false;
            }

            return CommitTransition(new StateTransitionRequest
            {
                RequestType = StateTransitionRequestType.RecoveryCancel,
                RecoveryCancelReason = RecoveryCancelReason.Movement,
                SourceStateId = actionRuntime.Current.Config != null ? actionRuntime.Current.Config.StateId : string.Empty,
                SourceLayerHint = StateLayerType.Action,
                TargetStateId = defaultStateId,
                IgnoreInterruptRules = true,
                RequestedStartTime = 0f,
            });
        }

        /// <summary>
        /// 解析活动状态自然结束后的层内回退目标。
        /// 当前优先使用该层默认态；Locomotion 层在必要时回退到全局默认态做兼容兜底。
        /// </summary>
        /// <param name="activeState">已自然结束的活动状态运行时。</param>
        /// <returns>层内回退目标状态 Id；没有合适回退时返回空字符串。</returns>
        private string ResolveLayerFallbackStateId(ActiveStateRuntime activeState)
        {
            if (activeState == null || activeState.Config == null)
            {
                return string.Empty;
            }

            string currentStateId = activeState.Config.StateId;
            string layerDefaultStateId = ResolveLayerDefaultStateId(activeState.Config.Layer);
            if (!string.IsNullOrWhiteSpace(layerDefaultStateId) &&
                !string.Equals(layerDefaultStateId, currentStateId, StringComparison.OrdinalIgnoreCase))
            {
                return layerDefaultStateId;
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取指定层声明的默认状态 Id。
        /// 第一阶段主要用于自然结束后的层内回退与默认态重播。
        /// </summary>
        /// <param name="layerType">目标层类型。</param>
        /// <returns>该层默认状态 Id；未配置时返回空字符串。</returns>
        private string ResolveLayerDefaultStateId(StateLayerType layerType)
        {
            return _layerRuntimes.TryGetValue(layerType, out StateLayerRuntime layerRuntime) && layerRuntime != null
                ? layerRuntime.DefaultStateId ?? string.Empty
                : string.Empty;
        }

        /// <summary>
        /// 将共享控制结果回灌到 locomotion 层。
        /// 当前阶段先消费“强制安全态”这一条显式事实，避免它只停留在聚合结果里却不影响实际状态。
        /// </summary>
        private void ApplySharedLocomotionControl()
        {
            if (!_sharedControlContext.ForceLocomotionSafeState)
            {
                return;
            }

            if (!_layerRuntimes.TryGetValue(StateLayerType.Locomotion, out StateLayerRuntime locomotionRuntime) || locomotionRuntime == null)
            {
                return;
            }

            string defaultStateId = ResolveLayerDefaultStateId(StateLayerType.Locomotion);
            if (string.IsNullOrWhiteSpace(defaultStateId))
            {
                return;
            }

            string currentStateId = locomotionRuntime.Current != null && locomotionRuntime.Current.Config != null
                ? locomotionRuntime.Current.Config.StateId
                : string.Empty;
            if (string.Equals(currentStateId, defaultStateId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            CommitTransition(new StateTransitionRequest
            {
                RequestType = StateTransitionRequestType.ExternalForce,
                SourceStateId = currentStateId,
                SourceLayerHint = StateLayerType.Locomotion,
                TargetStateId = defaultStateId,
                IgnoreInterruptRules = true,
                RequestedStartTime = 0f,
            });
        }

        /// <summary>
        /// 评估当前活动状态可触发的中断，并选出优先级最高的切换请求。
        /// </summary>
        /// <returns>命中的状态切换请求；没有命中时返回 null。</returns>
        private StateTransitionRequest EvaluateInterrupts(ActiveStateRuntime activeState)
        {
            if (activeState == null)
            {
                return null;
            }

            List<StateInterruptConfig> candidates = GetInterruptCandidates(activeState.Config);
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            float evaluationPreviousTime = activeState.PreviousTime;
            float evaluationCurrentTime = activeState.ElapsedTime;
            bool didLoopCycleWrap = TryGetLoopInterruptEvaluationWindow(
                activeState.Config,
                activeState.PreviousTime,
                activeState.ElapsedTime,
                out evaluationPreviousTime,
                out evaluationCurrentTime);

            if (didLoopCycleWrap)
            {
                activeState.BufferedInputs.Clear();
            }

            StateInputSnapshot inputSnapshot = ResolveEffectiveInputSnapshot();
            StateHitSnapshot hitSnapshot = _context.HitSnapshotProvider != null ? _context.HitSnapshotProvider() : default(StateHitSnapshot);
            StateBeHitSnapshot beHitSnapshot = _context.BeHitSnapshotProvider != null ? _context.BeHitSnapshotProvider() : default(StateBeHitSnapshot);
            float breakValue = _context.BreakValueProvider != null ? Mathf.Max(0f, _context.BreakValueProvider()) : 0f;

            StateTransitionRequest bestRequest = null;
            int bestSortOrder = int.MinValue;
            float bestTriggerTime = float.MaxValue;

            for (int index = 0; index < candidates.Count; index++)
            {
                StateInterruptConfig interrupt = candidates[index];
                if (!IsInterruptEligible(interrupt))
                {
                    continue;
                }

                if (!IsInterruptWindowActive(interrupt, evaluationPreviousTime, evaluationCurrentTime, didLoopCycleWrap))
                {
                    continue;
                }
                
                //okita:executetime是啥玩意。。知道了，延迟中断。这个目前不需要
                if (interrupt.ExecuteTime > 0f && evaluationCurrentTime < interrupt.TriggerTime + interrupt.ExecuteTime)
                {
                    BufferInputIfRelevant(activeState, interrupt, inputSnapshot);
                    continue;
                }

                StateInterruptContext interruptContext = new StateInterruptContext
                {
                    Controller = this,
                    RuntimeContext = _context,
                    CurrentStateConfig = activeState.Config,
                    InterruptConfig = interrupt,
                    InputSnapshot = ResolveBufferedInput(activeState, interrupt, inputSnapshot),
                    HitSnapshot = hitSnapshot,
                    BeHitSnapshot = beHitSnapshot,
                    ElapsedTime = evaluationCurrentTime,
                    PreviousTime = evaluationPreviousTime,
                    BreakValue = breakValue,
                };

                if (!CheckInterruptConditions(interrupt, interruptContext))
                {
                    continue;
                }

                if (interrupt.SortOrder > bestSortOrder ||
                    (interrupt.SortOrder == bestSortOrder && interrupt.TriggerTime < bestTriggerTime))
                {
                    bestSortOrder = interrupt.SortOrder;
                    bestTriggerTime = interrupt.TriggerTime;
                    bestRequest = new StateTransitionRequest
                    {
                        RequestType = StateTransitionRequestType.Interrupt,
                        SourceStateId = activeState.Config != null ? activeState.Config.StateId : string.Empty,
                        SourceLayerHint = activeState.Config != null ? activeState.Config.Layer : (StateLayerType?)null,
                        TargetStateId = interrupt.TargetStateId,
                        InterruptConfig = interrupt,
                        IgnoreInterruptRules = false,
                        RequestedStartTime = ResolveTargetStartTime(interrupt, _statesById.TryGetValue(interrupt.TargetStateId, out StateConfig targetState) ? targetState : null),
                        SkillTransitionContext = BuildNextSkillTransitionContext(activeState, interrupt.TargetStateId),
                    };
                }
            }

            return bestRequest;
        }

        /// <summary>
        /// 提交一次状态切换。
        /// 负责校验目标状态、退出旧状态、发送技能状态通知、创建新活动状态并播放动画。
        /// </summary>
        /// <param name="request">状态切换请求。</param>
        /// <returns>切换是否成功提交。</returns>
        private bool CommitTransition(StateTransitionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TargetStateId) || !_statesById.TryGetValue(request.TargetStateId, out StateConfig targetState))
            {
                return false;
            }

            ActiveStateRuntime sourceActiveState = ResolveSourceActiveStateRuntime(request);
            StateConfig sourceStateConfig = sourceActiveState != null ? sourceActiveState.Config : ResolveSourceStateConfig(request);
            if (!IsLayerTransitionAllowed(sourceStateConfig, targetState, request))
            {
                return false;
            }

            if (request.RequestType == StateTransitionRequestType.RecoveryCancel &&
                !CanCancelRecovery(sourceActiveState, request))
            {
                return false;
            }

            if (request.RequestType == StateTransitionRequestType.SkillDriven &&
                sourceActiveState != null &&
                sourceActiveState.SkillTransitionContext != null)
            {
                return false;
            }

            if (!request.IgnoreInterruptRules && sourceActiveState != null && request.RequestType == StateTransitionRequestType.ExternalTry)
            {
                if (!IsTargetAllowedByCurrentInterrupts(request.TargetStateId, sourceActiveState))
                {
                    return false;
                }
            }

            StateConfig previousStateConfig = sourceActiveState != null ? sourceActiveState.Config : null;
            SkillTransitionContext previousSkillTransitionContext = sourceActiveState != null ? sourceActiveState.SkillTransitionContext : null;
            SkillTransitionContext nextSkillTransitionContext = request.SkillTransitionContext ?? BuildNextSkillTransitionContext(sourceActiveState, request.TargetStateId);

            ExitActiveState(sourceActiveState, request);
            ActiveStateRuntime nextActiveState = CreateActiveStateRuntime(targetState, nextSkillTransitionContext, request.RequestedStartTime);
            SyncLayerRuntimeWithActiveState(targetState, nextActiveState);
            EnterActiveState(targetState, nextActiveState, request);

            if (_context.SkillContext != null)
            {
                _context.SkillContext.StateController = this;
            }

            // 先完整安装目标状态，再发送通知。通知回调即使触发技能收尾，也不会观察到层槽位为空的半提交状态。
            if (previousStateConfig != null && previousSkillTransitionContext != null)
            {
                EmitSkillStateChanged(new SkillStateNotification
                {
                    Kind = request.RequestType == StateTransitionRequestType.DefaultNext
                        ? SkillStateNotificationKind.Completed
                        : SkillStateNotificationKind.Interrupted,
                    RequestType = request.RequestType,
                    RecoveryCancelReason = request.RecoveryCancelReason,
                    StateId = previousStateConfig.StateId,
                    SourceStateId = previousStateConfig.StateId,
                    TargetStateId = request.TargetStateId,
                    TransitionContext = previousSkillTransitionContext,
                });
            }

            if (nextSkillTransitionContext != null)
            {
                EmitSkillStateChanged(new SkillStateNotification
                {
                    Kind = SkillStateNotificationKind.Entered,
                    RequestType = request.RequestType,
                    RecoveryCancelReason = request.RecoveryCancelReason,
                    StateId = targetState.StateId,
                    SourceStateId = request.SourceStateId,
                    TargetStateId = targetState.StateId,
                    TransitionContext = nextSkillTransitionContext,
                });
            }

            return true;
        }

        private static bool IsRecoveryState(ActiveStateRuntime activeState)
        {
            return activeState != null &&
                   activeState.SkillTransitionContext != null &&
                   activeState.SkillTransitionContext.PhaseRole == SkillStatePhaseRole.Recovery;
        }

        private bool CanCancelRecovery(ActiveStateRuntime sourceActiveState, StateTransitionRequest request)
        {
            if (!IsRecoveryState(sourceActiveState) || request == null)
            {
                return false;
            }

            switch (request.RecoveryCancelReason)
            {
                case RecoveryCancelReason.Skill:
                    return _recoveryCancelPolicy.AllowSkillCancel &&
                           request.SkillTransitionContext != null &&
                           request.SkillTransitionContext.PhaseRole == SkillStatePhaseRole.Execute;

                case RecoveryCancelReason.Movement:
                    return _recoveryCancelPolicy.AllowMoveCancel;

                case RecoveryCancelReason.HitReaction:
                    return _recoveryCancelPolicy.AllowHitReactionCancel;

                case RecoveryCancelReason.Forced:
                    return _recoveryCancelPolicy.AllowForcedCancel;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 解析本次切换请求的源状态配置。
        /// 优先从当前活动运行时中取，取不到时再回退到请求中携带的状态 Id。
        /// </summary>
        /// <param name="request">状态切换请求。</param>
        /// <returns>源状态配置；无法解析时返回 null。</returns>
        private StateConfig ResolveSourceStateConfig(StateTransitionRequest request)
        {
            ActiveStateRuntime sourceActiveState = ResolveSourceActiveStateRuntime(request);
            if (sourceActiveState != null && sourceActiveState.Config != null)
            {
                return sourceActiveState.Config;
            }

            if (request != null &&
                !string.IsNullOrWhiteSpace(request.SourceStateId) &&
                _statesById.TryGetValue(request.SourceStateId, out StateConfig sourceState))
            {
                return sourceState;
            }

            return null;
        }

        /// <summary>
        /// 解析本次切换请求对应的源活动状态运行时。
        /// 优先使用层提示，再尝试按源状态 Id 匹配；解析不到时返回 null。
        /// </summary>
        /// <param name="request">状态切换请求。</param>
        /// <returns>源活动状态运行时；无法解析时返回 null。</returns>
        private ActiveStateRuntime ResolveSourceActiveStateRuntime(StateTransitionRequest request)
        {
            if (request != null && request.SourceLayerHint.HasValue && _layerRuntimes.TryGetValue(request.SourceLayerHint.Value, out StateLayerRuntime layerRuntime))
            {
                return layerRuntime != null ? layerRuntime.Current : null;
            }

            if (request != null && !string.IsNullOrWhiteSpace(request.SourceStateId))
            {
                ActiveStateRuntime runtime = FindActiveStateRuntimeByStateId(request.SourceStateId);
                if (runtime != null)
                {
                    return runtime;
                }
            }

            return null;
        }

        /// <summary>
        /// 在当前各层活动状态中按状态 Id 查找对应运行时。
        /// 主要用于请求只携带状态 Id，但没有明确层提示时的兼容解析。
        /// </summary>
        /// <param name="stateId">要查找的状态 Id。</param>
        /// <returns>匹配到的活动状态运行时；未命中时返回 null。</returns>
        private ActiveStateRuntime FindActiveStateRuntimeByStateId(string stateId)
        {
            if (string.IsNullOrWhiteSpace(stateId))
            {
                return null;
            }

            foreach (KeyValuePair<StateLayerType, StateLayerRuntime> pair in _layerRuntimes)
            {
                ActiveStateRuntime runtime = pair.Value != null ? pair.Value.Current : null;
                if (runtime != null &&
                    runtime.Config != null &&
                    string.Equals(runtime.Config.StateId, stateId, StringComparison.OrdinalIgnoreCase))
                {
                    return runtime;
                }
            }

            return null;
        }

        private bool IsLayerTransitionAllowed(StateConfig sourceState, StateConfig targetState, StateTransitionRequest request)
        {
            if (targetState == null)
            {
                return false;
            }

            if (sourceState == null)
            {
                return true;
            }

            if (sourceState.Layer == targetState.Layer)
            {
                return true;
            }

            if (request != null && request.IsGlobalForcedTransition)
            {
                return true;
            }

            StateTransitionPolicy transitionPolicy = request != null && request.InterruptConfig != null
                ? request.InterruptConfig.TransitionPolicy
                : StateTransitionPolicy.SameLayerOnly;

            switch (transitionPolicy)
            {
                case StateTransitionPolicy.ForceGlobal:
                    return true;

                case StateTransitionPolicy.AllowWhitelistedCrossLayer:
                    return request != null && request.IsGlobalForcedTransition;

                case StateTransitionPolicy.SameLayerOnly:
                default:
                    return false;
            }
        }

        /// <summary>
        /// 根据目标状态延续当前技能切换上下文。
        /// 只有目标状态属于当前技能的执行段或恢复段时才会生成上下文。
        /// </summary>
        /// <param name="targetStateId">目标状态 Id。</param>
        /// <returns>下一状态使用的技能切换上下文；不属于当前技能时返回 null。</returns>
        private SkillTransitionContext BuildNextSkillTransitionContext(ActiveStateRuntime sourceActiveState, string targetStateId)
        {
            SkillTransitionContext currentSkillTransitionContext = sourceActiveState != null ? sourceActiveState.SkillTransitionContext : null;
            if (currentSkillTransitionContext == null || string.IsNullOrWhiteSpace(targetStateId))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(currentSkillTransitionContext.ExecuteStateId) &&
                string.Equals(currentSkillTransitionContext.ExecuteStateId, targetStateId, StringComparison.OrdinalIgnoreCase))
            {
                return currentSkillTransitionContext.CloneForPhase(SkillStatePhaseRole.Execute, targetStateId);
            }

            if (!string.IsNullOrWhiteSpace(currentSkillTransitionContext.RecoveryStateId) &&
                string.Equals(currentSkillTransitionContext.RecoveryStateId, targetStateId, StringComparison.OrdinalIgnoreCase))
            {
                return currentSkillTransitionContext.CloneForPhase(SkillStatePhaseRole.Recovery, targetStateId);
            }

            return null;
        }

        /// <summary>
        /// 发送技能状态变化通知。
        /// </summary>
        /// <param name="notification">状态变化通知数据。</param>
        private void EmitSkillStateChanged(SkillStateNotification notification)
        {
            SkillStateChanged?.Invoke(notification);
        }

        /// <summary>
        /// 尝试进入默认状态。
        /// </summary>
        /// <returns>是否成功进入默认状态。</returns>
        private bool TryEnterDefaultState()
        {
            EnsureDefaultLayerStates();
            return HasAnyActiveState();
        }

        private bool HasAnyActiveState()
        {
            foreach (KeyValuePair<StateLayerType, StateLayerRuntime> pair in _layerRuntimes)
            {
                if (pair.Value != null && pair.Value.Current != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取指定层的运行时容器；不存在时会立即创建。
        /// </summary>
        /// <param name="layerType">目标层类型。</param>
        /// <returns>对应层的运行时容器。</returns>
        private StateLayerRuntime GetLayerRuntime(StateLayerType layerType)
        {
            return _layerRuntimes.TryGetValue(layerType, out StateLayerRuntime runtime) ? runtime : null;
        }

        private void InitializeLayerRuntimes(IEnumerable<UnitLayerDefaultStateConfig> layerDefaultStates)
        {
            Dictionary<StateLayerType, string> configuredDefaultStates = new Dictionary<StateLayerType, string>();
            if (layerDefaultStates != null)
            {
                foreach (UnitLayerDefaultStateConfig layerDefaultState in layerDefaultStates)
                {
                    if (layerDefaultState == null)
                    {
                        continue;
                    }

                    configuredDefaultStates[layerDefaultState.Layer] = layerDefaultState.DefaultStateId ?? string.Empty;
                }
            }

            for (int i = 0; i < OrderedLayers.Length; i++)
            {
                StateLayerType layerType = OrderedLayers[i];
                _layerRuntimes[layerType] = new StateLayerRuntime
                {
                    LayerType = layerType,
                    Priority = GetLayerPriority(layerType),
                    DefaultStateId = configuredDefaultStates.TryGetValue(layerType, out string defaultStateId)
                        ? defaultStateId
                        : string.Empty,
                };
            }
        }

        private List<StateLayerRuntime> GetOrderedLayerRuntimes(bool ascending = false)
        {
            List<StateLayerRuntime> layerRuntimes = new List<StateLayerRuntime>(_layerRuntimes.Values);
            layerRuntimes.Sort((left, right) => ascending
                ? left.Priority.CompareTo(right.Priority)
                : right.Priority.CompareTo(left.Priority));
            return layerRuntimes;
        }

        private static int GetLayerPriority(StateLayerType layerType)
        {
            switch (layerType)
            {
                case StateLayerType.Action:
                    return 1;
                case StateLayerType.Locomotion:
                    return 0;
                default:
                    return -1;
            }
        }

        private void ValidateLayerConfiguration()
        {
            for (int i = 0; i < OrderedLayers.Length; i++)
            {
                StateLayerType layerType = OrderedLayers[i];
                StateLayerRuntime layerRuntime = GetLayerRuntime(layerType);
                if (layerRuntime == null)
                {
                    throw new InvalidOperationException($"StateController 初始化失败：缺少层运行时配置，layer={layerType}。");
                }

                if (string.IsNullOrWhiteSpace(layerRuntime.DefaultStateId))
                {
                    throw new InvalidOperationException($"StateController 初始化失败：层未配置默认状态，layer={layerType}。");
                }

                if (!_statesById.ContainsKey(layerRuntime.DefaultStateId))
                {
                    throw new InvalidOperationException($"StateController 初始化失败：层默认状态不存在，layer={layerType}, stateId={layerRuntime.DefaultStateId}。");
                }

                StateConfig defaultState = _statesById[layerRuntime.DefaultStateId];
                if (defaultState == null || defaultState.Layer != layerType)
                {
                    throw new InvalidOperationException($"StateController 初始化失败：层默认状态层级不匹配，layer={layerType}, stateId={layerRuntime.DefaultStateId}。");
                }
            }
        }

        /// <summary>
        /// 将新创建的活动状态运行时同步到它所属的层容器。
        /// 同时清空该层待处理请求，确保层槽位与当前活动态一致。
        /// </summary>
        /// <param name="targetState">目标状态配置。</param>
        /// <param name="activeState">目标状态对应的活动运行时。</param>
        private void SyncLayerRuntimeWithActiveState(StateConfig targetState, ActiveStateRuntime activeState)
        {
            if (targetState == null)
            {
                return;
            }

            StateLayerRuntime targetLayerRuntime = GetLayerRuntime(targetState.Layer);
            if (targetLayerRuntime == null)
            {
                throw new InvalidOperationException($"StateController 切换失败：缺少目标层运行时，layer={targetState.Layer}。");
            }

            targetLayerRuntime.Current = activeState;
            targetLayerRuntime.PendingRequest = null;
        }

        /// <summary>
        /// 执行进入状态时的副作用。
        /// 包括添加状态标签、播放并同步状态动画。
        /// </summary>
        /// <param name="state">进入的状态配置。</param>
        /// <param name="activeState">新建好的目标状态运行时。</param>
        /// <param name="request">触发本次进入的切换请求。</param>
        private void EnterActiveState(StateConfig state, ActiveStateRuntime activeState, StateTransitionRequest request)
        {
            ApplyTags(state, state != null ? state.Tags : null, StateTagSourceId);
            if (state != null && activeState != null && state.AffectsLocomotion)
            {
                StateLayerRuntime layerRuntime = GetLayerRuntime(state.Layer);
                activeState.MovementPolicyHandle = _movementPolicyController.Submit(
                    state.StateId,
                    layerRuntime != null ? layerRuntime.Priority : 0,
                    state.MovementProfile ?? StateMovementProfile.CreateDefault());
            }

            bool hasPlayableAnimation = state != null &&
                                        (state.AnimationMode == StateAnimationMode.DirectionalMixer2D ||
                                         !string.IsNullOrWhiteSpace(state.AnimationClipPath));
            if (!hasPlayableAnimation || _context.SkillContext == null)
            {
                return;
            }

            ICharacterAnimationController animationController = _context.SkillContext.CharacterAnimationController;
            animationController?.PlayStateAnimation(_context.SkillContext, state, null);
            if (activeState != null && activeState.ElapsedTime > 0f)
            {
                animationController?.SeekStateAnimation(_context.SkillContext, state, activeState.ElapsedTime);
            }
        }

        /// <summary>
        /// 执行退出当前状态时的副作用。
        /// 包括结束时间线、移除状态标签、停止状态动画。
        /// </summary>
        /// <param name="request">触发本次退出的切换请求。</param>
        private void ExitActiveState(ActiveStateRuntime activeState, StateTransitionRequest request)
        {
            if (activeState == null || activeState.Config == null)
            {
                return;
            }

            bool interrupted = request != null &&
                               (request.RequestType == StateTransitionRequestType.Interrupt ||
                                request.RequestType == StateTransitionRequestType.RecoveryCancel);
            activeState.End(interrupted);
            ClearLayerRuntimeIfMatches(activeState);
            _movementPolicyController.Release(activeState.MovementPolicyHandle);
            activeState.MovementPolicyHandle = default;

            RemoveTags(activeState.Config, activeState.Config.Tags, StateTagSourceId);
            if (_context.SkillContext != null)
            {
                _context.SkillContext.CharacterAnimationController?.StopStateAnimation(
                    _context.SkillContext,
                    activeState.Config,
                    interrupted);
            }
        }

        /// <summary>
        /// 当传入运行时仍占据其所属层槽位时，将该层当前活动态清空。
        /// 如果兼容主引用恰好指向这个运行时，也会同步刷新兼容引用。
        /// </summary>
        /// <param name="activeState">准备清理的活动状态运行时。</param>
        private void ClearLayerRuntimeIfMatches(ActiveStateRuntime activeState)
        {
            if (activeState == null || activeState.Config == null)
            {
                return;
            }

            if (_layerRuntimes.TryGetValue(activeState.Config.Layer, out StateLayerRuntime layerRuntime) &&
                layerRuntime != null &&
                ReferenceEquals(layerRuntime.Current, activeState))
            {
                layerRuntime.Current = null;
                layerRuntime.PendingRequest = null;
            }
        }

        /// <summary>
        /// 创建并初始化一个新的活动状态运行时。
        /// 会绑定技能切换上下文，并按请求起始时间重置时间线。
        /// </summary>
        /// <param name="targetState">目标状态配置。</param>
        /// <param name="skillTransitionContext">要挂到新运行时上的技能切换上下文。</param>
        /// <param name="requestedStartTime">目标状态请求的起始播放时间。</param>
        /// <returns>初始化完成的活动状态运行时。</returns>
        private ActiveStateRuntime CreateActiveStateRuntime(StateConfig targetState, SkillTransitionContext skillTransitionContext, float requestedStartTime)
        {
            ActiveStateRuntime activeState = new ActiveStateRuntime(targetState, _context.SkillContext);
            activeState.SkillTransitionContext = skillTransitionContext;
            activeState.Reset(requestedStartTime);
            return activeState;
        }

        private bool IsTargetAllowedByCurrentInterrupts(string targetStateId, ActiveStateRuntime activeState)
        {
            List<StateInterruptConfig> candidates = GetInterruptCandidates(activeState != null ? activeState.Config : null);
            if (candidates == null || candidates.Count == 0)
            {
                return false;
            }

            float evaluationPreviousTime = activeState != null ? activeState.PreviousTime : 0f;
            float evaluationCurrentTime = activeState != null ? activeState.ElapsedTime : 0f;
            bool didLoopCycleWrap = activeState != null && TryGetLoopInterruptEvaluationWindow(
                activeState.Config,
                activeState.PreviousTime,
                activeState.ElapsedTime,
                out evaluationPreviousTime,
                out evaluationCurrentTime);

            for (int i = 0; i < candidates.Count; i++)
            {
                StateInterruptConfig interrupt = candidates[i];
                if (interrupt != null &&
                    interrupt.IsEnabled &&
                    string.Equals(interrupt.TargetStateId, targetStateId, StringComparison.OrdinalIgnoreCase) &&
                    IsInterruptWindowActive(interrupt, evaluationPreviousTime, evaluationCurrentTime, didLoopCycleWrap))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 从状态配置中收集可用的中断配置。
        /// 优先读取中断轨道；没有轨道结果时兼容旧版平铺中断列表。
        /// </summary>
        /// <param name="stateConfig">状态配置。</param>
        /// <returns>中断候选列表；无时间线时返回 null。</returns>
        private static List<StateInterruptConfig> GetInterruptCandidates(StateConfig stateConfig)
        {
            if (stateConfig == null || stateConfig.Timeline == null)
            {
                return null;
            }

            List<StateInterruptConfig> results = new List<StateInterruptConfig>();
            
            //okita:这里为啥有两个收集result
            if (stateConfig.Timeline.InterruptTracks != null)
            {
                for (int trackIndex = 0; trackIndex < stateConfig.Timeline.InterruptTracks.Count; trackIndex++)
                {
                    StateInterruptTrackConfig track = stateConfig.Timeline.InterruptTracks[trackIndex];
                    if (track == null || !track.IsEnabled || track.Interrupts == null)
                    {
                        continue;
                    }

                    for (int interruptIndex = 0; interruptIndex < track.Interrupts.Count; interruptIndex++)
                    {
                        StateInterruptConfig interrupt = track.Interrupts[interruptIndex];
                        if (interrupt != null)
                        {
                            results.Add(interrupt);
                        }
                    }
                }
            }

            if (results.Count == 0 && stateConfig.Timeline.Interrupts != null)
            {
                for (int i = 0; i < stateConfig.Timeline.Interrupts.Count; i++)
                {
                    StateInterruptConfig interrupt = stateConfig.Timeline.Interrupts[i];
                    if (interrupt != null)
                    {
                        results.Add(interrupt);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 判断中断配置是否具备基本可用条件。
        /// </summary>
        /// <param name="interrupt">中断配置。</param>
        /// <returns>中断启用且目标状态有效时返回 true。</returns>
        private static bool IsInterruptEligible(StateInterruptConfig interrupt)
        {
            return interrupt != null && interrupt.IsEnabled && !string.IsNullOrWhiteSpace(interrupt.TargetStateId);
        }

        /// <summary>
        /// 判断当前评估时间是否处于中断窗口内。
        /// </summary>
        /// <param name="interrupt">中断配置。</param>
        /// <param name="previousTime">上一帧评估时间。</param>
        /// <param name="currentTime">当前帧评估时间。</param>
        /// <param name="didLoopCycleWrap">本帧是否跨过循环边界。</param>
        /// <returns>中断窗口是否激活。</returns>
        private static bool IsInterruptWindowActive(StateInterruptConfig interrupt, float previousTime, float currentTime, bool didLoopCycleWrap)
        {
            if (interrupt == null)
            {
                return false;
            }

            float triggerTime = Mathf.Max(0f, interrupt.TriggerTime);
             
             //okita:这是在干什么，感觉有问题，看不懂
            if (interrupt.Duration == 0f)
            {
                if (didLoopCycleWrap)
                {
                    return triggerTime >= previousTime || triggerTime <= currentTime;
                }

                return triggerTime >= previousTime && triggerTime <= currentTime;
            }

            if (currentTime < triggerTime)
            {
                return false;
            }

            if (interrupt.Duration < 0f)
            {
                return true;
            }

            return currentTime <= triggerTime + interrupt.Duration;
        }

        /// <summary>
        /// 解析中断切入目标状态时的起始播放时间。
        /// </summary>
        /// <param name="interrupt">中断配置。</param>
        /// <param name="targetState">目标状态配置。</param>
        /// <returns>目标状态起始时间，单位为秒。</returns>
        private static float ResolveTargetStartTime(StateInterruptConfig interrupt, StateConfig targetState)
        {
            if (interrupt == null)
            {
                return 0f;
            }

            float startTime = Mathf.Max(0f, interrupt.TargetStartTime);
            if (interrupt.TargetStartTimeUnit != AnimationStartTimeUnit.NormalizedTime)
            {
                return startTime;
            }

            AnimationClip clip = SkillAnimationRuntimeCatalog.LoadClip(targetState != null ? targetState.AnimationClipPath : string.Empty);
            return clip != null ? startTime * Mathf.Max(0f, clip.length) : startTime;
        }

        /// <summary>
        /// 判断状态是否到达自然结束时间。
        /// </summary>
        /// <param name="stateConfig">状态配置。</param>
        /// <param name="elapsedTime">状态已运行时间。</param>
        /// <returns>已运行时间达到自然结束时间时返回 true。</returns>
        private static bool HasReachedNaturalEnd(StateConfig stateConfig, float elapsedTime)
        {
            // 循环状态没有显式默认下一状态时，应持续运行并由中断条件退出。
            // 不能按 Timeline.Duration 重播，否则动画速度倍率不为 1 时，状态重置时间与
            // 动画循环边界不同步，会周期性跳回首帧并产生明显步伐卡顿。
            if (ShouldInterruptWindowRepeatPerLoop(stateConfig))
            {
                return false;
            }

            float endTime = ResolveStateNaturalEndTime(stateConfig);
            return endTime > 0f && elapsedTime >= endTime;
        }

        /// <summary>
        /// 为循环动画计算本帧用于中断检测的循环内时间窗口。
        /// </summary>
        /// <param name="stateConfig">状态配置。</param>
        /// <param name="previousTime">上一帧原始时间。</param>
        /// <param name="currentTime">当前帧原始时间。</param>
        /// <param name="evaluationPreviousTime">输出的循环内上一帧时间。</param>
        /// <param name="evaluationCurrentTime">输出的循环内当前帧时间。</param>
        /// <returns>本帧是否跨过循环边界。</returns>
        private static bool TryGetLoopInterruptEvaluationWindow(StateConfig stateConfig, float previousTime, float currentTime, out float evaluationPreviousTime, out float evaluationCurrentTime)
        {
            evaluationPreviousTime = previousTime;
            evaluationCurrentTime = currentTime;

            if (!ShouldInterruptWindowRepeatPerLoop(stateConfig))
            {
                return false;
            }

            float cycleDuration = ResolveStateNaturalEndTime(stateConfig);
            if (cycleDuration <= 0f)
            {
                return false;
            }
            
            //okita:为啥要比较evaluationCurrentTime < evaluationPreviousTime
            evaluationPreviousTime = Mathf.Repeat(previousTime, cycleDuration);
            evaluationCurrentTime = Mathf.Repeat(currentTime, cycleDuration);
            return currentTime >= cycleDuration && evaluationCurrentTime < evaluationPreviousTime;
        }

        /// <summary>
        /// 判断状态中断窗口是否应按循环动画每轮重复检测。
        /// </summary>
        /// <param name="stateConfig">状态配置。</param>
        /// <returns>状态无默认下一状态且动画循环时返回 true。</returns>
        private static bool ShouldInterruptWindowRepeatPerLoop(StateConfig stateConfig)
        {
            if (stateConfig == null || !string.IsNullOrWhiteSpace(stateConfig.DefaultNextStateId))
            {
                return false;
            }

            // DirectionalMixer2D 是持续型 Locomotion 表现。它没有单一 AnimationClipPath，
            // 不能按 Timeline.Duration（通常只是某个子 Clip 的短时长）自然结束并反复重进状态。
            // 该状态应与普通循环 Locomotion 一样，只通过中断条件退出。
            if (stateConfig.AnimationMode == StateAnimationMode.DirectionalMixer2D)
            {
                return true;
            }

            AnimationClip clip = SkillAnimationRuntimeCatalog.LoadClip(stateConfig.AnimationClipPath);
            return clip != null && clip.isLooping;
        }

        /// <summary>
        /// 解析状态自然结束时间。
        /// 优先使用时间线时长；没有时间线时使用动画长度减去起播时间。
        /// </summary>
        /// <param name="stateConfig">状态配置。</param>
        /// <returns>状态自然结束时间，单位为秒。</returns>
        private static float ResolveStateNaturalEndTime(StateConfig stateConfig)
        {
            if (stateConfig == null)
            {
                return 0f;
            }

            if (stateConfig.Timeline != null && stateConfig.Timeline.Duration > 0f)
            {
                return stateConfig.Timeline.Duration;
            }

            //okita:不知道这里是干啥的，duration小于等于0，就不对了
            AnimationClip clip = SkillAnimationRuntimeCatalog.LoadClip(stateConfig.AnimationClipPath);
            if (clip == null)
            {
                return 0f;
            }

            float clipLength = Mathf.Max(0f, clip.length);
            float startTime = ResolveStateAnimationStartTime(stateConfig, clip);
            return Mathf.Max(0f, clipLength - startTime);
        }

        /// <summary>
        /// 解析状态动画起播时间。
        /// </summary>
        /// <param name="stateConfig">状态配置。</param>
        /// <param name="clip">状态动画剪辑。</param>
        /// <returns>动画起播时间，单位为秒。</returns>
        private static float ResolveStateAnimationStartTime(StateConfig stateConfig, AnimationClip clip)
        {
            if (clip == null)
            {
                return 0f;
            }

            TimelineAnimationConfig animationConfig = stateConfig != null && stateConfig.Timeline != null
                ? stateConfig.Timeline.Animation
                : null;
            float startTime = animationConfig != null ? Mathf.Max(0f, animationConfig.StartTime) : 0f;
            if (animationConfig != null && animationConfig.StartTimeUnit == AnimationStartTimeUnit.NormalizedTime)
            {
                startTime *= Mathf.Max(0f, clip.length);
            }

            return Mathf.Clamp(startTime, 0f, Mathf.Max(0f, clip.length));
        }

        /// <summary>
        /// 检查中断配置上的条件集合。
        /// </summary>
        /// <param name="interrupt">中断配置。</param>
        /// <param name="context">中断评估上下文。</param>
        /// <returns>条件全部或任一满足时返回 true，取决于配置模式。</returns>
        private static bool CheckInterruptConditions(StateInterruptConfig interrupt, StateInterruptContext context)
        {
            if (interrupt == null || interrupt.Conditions == null || interrupt.Conditions.Count == 0)
            {
                return true;
            }

            if (interrupt.CheckAllConditions)
            {
                for (int i = 0; i < interrupt.Conditions.Count; i++)
                {
                    IStateInterruptCondition condition = interrupt.Conditions[i];
                    if (condition != null && !condition.Evaluate(context))
                    {
                        return false;
                    }
                }

                return true;
            }

            for (int i = 0; i < interrupt.Conditions.Count; i++)
            {
                IStateInterruptCondition condition = interrupt.Conditions[i];
                if (condition != null && condition.Evaluate(context))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 为延迟执行的中断缓存输入快照。
        /// </summary>
        /// <param name="interrupt">中断配置。</param>
        /// <param name="snapshot">当前帧输入快照。</param>
        private void BufferInputIfRelevant(ActiveStateRuntime activeState, StateInterruptConfig interrupt, StateInputSnapshot snapshot)
        {
            if (interrupt == null || activeState == null)
            {
                return;
            }

            activeState.BufferedInputs[interrupt] = snapshot;
        }

        /// <summary>
        /// 获取当前帧对状态机真正生效的输入快照。
        /// 当前阶段先让共享控制中的移动输入锁定落到中断评估链上，避免技能覆盖期间仍把移动视为有效输入。
        /// </summary>
        private StateInputSnapshot ResolveEffectiveInputSnapshot()
        {
            StateInputSnapshot inputSnapshot = _context.InputSnapshotProvider != null ? _context.InputSnapshotProvider() : default(StateInputSnapshot);
            if (_sharedControlContext.AllowMoveInput)
            {
                return inputSnapshot;
            }

            inputSnapshot.IsMoveInput = false;
            inputSnapshot.IsMoveInputPre = false;
            return inputSnapshot;
        }

        /// <summary>
        /// 获取中断对应的缓存输入。
        /// </summary>
        /// <param name="interrupt">中断配置。</param>
        /// <param name="fallback">没有缓存时使用的当前输入快照。</param>
        /// <returns>缓存输入或兜底输入。</returns>
        private StateInputSnapshot ResolveBufferedInput(ActiveStateRuntime activeState, StateInterruptConfig interrupt, StateInputSnapshot fallback)
        {
            if (activeState != null && interrupt != null && activeState.BufferedInputs.TryGetValue(interrupt, out StateInputSnapshot snapshot))
            {
                return snapshot;
            }

            return fallback;
        }

        /// <summary>
        /// 将状态配置标签挂到配置载体自己的 RuntimeTagContainer 上。
        /// </summary>
        /// <param name="carrier">标签承载对象，通常是 StateConfig。</param>
        /// <param name="container">要添加的标签容器。</param>
        /// <param name="sourceId">标签来源 Id，用于后续精确移除。</param>
        private static void ApplyTags(object carrier, TagContainer container, string sourceId)
        {
            if (carrier == null || container == null || container.Tags == null)
            {
                return;
            }

            RuntimeTagContainer runtimeTags = ResolveCarrierTagContainer(carrier);
            if (runtimeTags == null)
            {
                return;
            }

            for (int i = 0; i < container.Tags.Count; i++)
            {
                string tag = container.Tags[i];
                if (!string.IsNullOrEmpty(tag))
                {
                    runtimeTags.AddTag(tag, 1, sourceId);
                }
            }
        }

        /// <summary>
        /// 从配置载体自己的 RuntimeTagContainer 上移除状态配置标签。
        /// </summary>
        /// <param name="carrier">标签承载对象，通常是 StateConfig。</param>
        /// <param name="container">要移除的标签容器。</param>
        /// <param name="sourceId">标签来源 Id，需与添加时保持一致。</param>
        private static void RemoveTags(object carrier, TagContainer container, string sourceId)
        {
            if (carrier == null || container == null || container.Tags == null)
            {
                return;
            }

            RuntimeTagContainer runtimeTags = ResolveCarrierTagContainer(carrier);
            if (runtimeTags == null)
            {
                return;
            }

            for (int i = 0; i < container.Tags.Count; i++)
            {
                string tag = container.Tags[i];
                if (!string.IsNullOrEmpty(tag))
                {
                    runtimeTags.RemoveTag(tag, 1, sourceId);
                }
            }
        }

        private static RuntimeTagContainer ResolveCarrierTagContainer(object carrier)
        {
            switch (carrier)
            {
                case RuntimeTagContainer runtimeTagContainer:
                    return runtimeTagContainer;
                case IRuntimeTagContainerOwner runtimeTagOwner:
                    return runtimeTagOwner.RuntimeTags;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 刷新当前帧的共享控制事实。
        /// 先收集各层自己的控制意图，再按统一规则求解共享结果，避免状态遍历和结果写回耦在一起。
        /// </summary>
        private void RefreshSharedControlContext()
        {
            List<StateLayerControlIntent> layerIntents = CollectLayerControlIntents();
            ResolveSharedControlContext(layerIntents, _sharedControlContext);
        }

        /// <summary>
        /// 收集当前各层活动状态声明的控制意图。
        /// 这一层只负责保留“谁想做什么”，不直接修改最终共享结果。
        /// </summary>
        /// <returns>当前各层控制意图列表。</returns>
        private List<StateLayerControlIntent> CollectLayerControlIntents()
        {
            List<StateLayerControlIntent> layerIntents = new List<StateLayerControlIntent>();

            foreach (KeyValuePair<StateLayerType, StateLayerRuntime> pair in _layerRuntimes)
            {
                ActiveStateRuntime runtime = pair.Value != null ? pair.Value.Current : null;
                StateConfig state = runtime != null ? runtime.Config : null;
                if (state == null)
                {
                    continue;
                }

                StateLayerControlIntent layerIntent = new StateLayerControlIntent
                {
                    LayerType = pair.Key,
                    OwnerStateId = state.StateId ?? string.Empty,
                    LocksMoveInput = state.ControlsMovement,
                    LocksLocomotionDrive = state.ControlsMovement,
                    LocksRotationInput = state.ControlsRotation,
                    BlocksLocomotionAnimation = state.BlocksLocomotionAnimation,
                };

                switch (state.LocomotionImpactMode)
                {
                    case LocomotionImpactMode.LockMoveInput:
                        layerIntent.LocksMoveInput = true;
                        break;

                    case LocomotionImpactMode.LockLocomotionDrive:
                        layerIntent.LocksLocomotionDrive = true;
                        break;

                    case LocomotionImpactMode.ForceSafeState:
                        layerIntent.ForcesLocomotionSafeState = true;
                        break;
                }

                layerIntents.Add(layerIntent);
            }

            return layerIntents;
        }

        /// <summary>
        /// 将各层控制意图求解为当前帧共享控制事实。
        /// 当前阶段仍沿用保守求解：任一层显式锁定，就将对应共享事实关闭或置位。
        /// </summary>
        /// <param name="layerIntents">本帧收集到的各层控制意图。</param>
        /// <param name="sharedContext">要写入的共享控制结果。</param>
        private static void ResolveSharedControlContext(List<StateLayerControlIntent> layerIntents, StateSharedControlContext sharedContext)
        {
            if (sharedContext == null)
            {
                return;
            }

            sharedContext.AllowMoveInput = true;
            sharedContext.AllowLocomotionDrive = true;
            sharedContext.AllowRotationInput = true;
            sharedContext.AllowDash = true;
            sharedContext.AllowNextSkill = true;
            sharedContext.UseRootMotion = true;
            sharedContext.ForceLocomotionSafeState = false;
            sharedContext.BlocksLocomotionAnimation = false;

            if (layerIntents == null)
            {
                return;
            }

            for (int index = 0; index < layerIntents.Count; index++)
            {
                StateLayerControlIntent layerIntent = layerIntents[index];
                if (layerIntent == null)
                {
                    continue;
                }

                if (layerIntent.LocksMoveInput)
                {
                    sharedContext.AllowMoveInput = false;
                }

                if (layerIntent.LocksLocomotionDrive)
                {
                    sharedContext.AllowLocomotionDrive = false;
                }

                if (layerIntent.LocksRotationInput)
                {
                    sharedContext.AllowRotationInput = false;
                }

                if (layerIntent.BlocksLocomotionAnimation)
                {
                    sharedContext.BlocksLocomotionAnimation = true;
                }

                if (layerIntent.ForcesLocomotionSafeState)
                {
                    sharedContext.ForceLocomotionSafeState = true;
                }
            }
        }
    }
}
