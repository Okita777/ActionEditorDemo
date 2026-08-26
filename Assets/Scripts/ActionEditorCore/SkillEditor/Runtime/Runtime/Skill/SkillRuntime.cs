using System;
using System.Collections.Generic;
using ActionEditor.TagSystem;
using SkillEventEntryConfig = AsiSkillEditor.RunTime.SkillEventEntryConfig;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    /// <summary>
    /// 单个技能实例的核心运行时。
    ///
    /// 它主要负责：
    /// 1. 管理当前技能正在运行的 layer 和 node。
    /// 2. 处理输入事件在技能图上的匹配和跳转。
    /// 3. 处理技能开始释放前的冷却、资源、状态门禁检查。
    /// 4. 管理 MetaSkill 的进入、完成、中断和 continuation 窗口。
    /// 5. 在需要时请求 StateController 切入技能状态，并消费状态机回传的结果。
    ///
    /// 可以把它理解为“技能图调度器”，而不是时间线具体执行器。
    /// 时间线内部的事件、攻击盒、子弹执行由 StateTimelineExecutionRuntime 负责。
    /// </summary>
    public sealed class SkillRuntime
    {
        /// <summary>
        /// 技能图入口伪节点 Id，用于匹配从入口开始的事件边。
        /// </summary>
        private const string EntryNodeId = "__entry__";

        /// <summary>
        /// 技能图出口伪节点 Id，命中后会退出当前技能施法。
        /// </summary>
        private const string ExitNodeId = "__exit__";

        /// <summary>
        /// 技能配置标签写入标签系统时使用的来源标识。
        /// </summary>
        private const string SkillTagSourceId = "SkillRuntime.ConfigTags";

        /// <summary>
        /// 当前技能的根配置，提供 layer、事件边、冷却、资源消耗和标签等规则。
        /// </summary>
        private readonly SkillConfig _skillConfig;

        /// <summary>
        /// 当前技能可访问的 MetaSkill 配置表，键通常是 MetaSkill 资源名。
        /// </summary>
        private readonly Dictionary<string, MetaSkillConfig> _metaSkillConfigs;

        /// <summary>
        /// 当前技能实例共享的运行时上下文，贯穿效果执行、状态机、资源系统和调试黑板。
        /// </summary>
        private readonly SkillContext _context;

        /// <summary>
        /// 当前 SkillRuntime 实例的唯一标识。
        /// StateController 回发技能状态通知时，会依靠它把通知路由回正确的 SkillRuntime。
        /// </summary>
        private readonly string _runtimeInstanceId = System.Guid.NewGuid().ToString("N");

        /// <summary>
        /// 当前活动 layer 下标。
        /// 对于分层技能，它决定下一次从哪一层开始尝试进入。
        /// </summary>
        private int _activeLayerIndex;

        /// <summary>
        /// 当前正在执行的 layer。
        /// 为空通常表示技能当前处于空闲，尚未进入任何 layer。
        /// </summary>
        private SkillLayerConfig _currentLayer;

        /// <summary>
        /// 当前节点。
        /// 它决定 SkillEvent 的 fromNode 以及当前这段技能链处在什么位置。
        /// </summary>
        private MetaSkillNodeConfig _currentNode;

        /// <summary>
        /// 当前运行中的 MetaSkillRuntime。
        /// 对于非状态驱动路径，它直接推进 execute/recovery；
        /// 对于状态驱动路径，它更多承担阶段组织和效果触发的职责。
        /// </summary>
        private MetaSkillRuntime _currentMetaSkillRuntime;

        /// <summary>
        /// 当前已订阅的 StateController。
        /// 用于接收技能状态进入、完成和中断通知。
        /// </summary>
        private StateController _observedStateController;

        /// <summary>
        /// 最近一次 Trigger 失败原因。
        /// 主要用于调试、日志和 UI 观察。
        /// </summary>
        private string _lastTriggerFailureReason = string.Empty;

        /// <summary>
        /// 当前是否处于等待下一段连段输入的窗口中。
        /// </summary>
        private bool _isAwaitingContinuation;

        /// <summary>
        /// continuation 窗口开始的时间点。
        /// 与 SkillConfig.ComboContinuationTimeout 配合使用，用于判定连段超时。
        /// </summary>
        private float _continuationAwaitStartTime = -1f;

        /// <summary>
        /// 每个 layer 的冷却起始时间。
        /// 这让同一个技能的不同逻辑层可以独立进入冷却。
        /// </summary>
        private readonly List<float> _layerCooldownStartTimes = new List<float>();

        /// <summary>
        /// 创建一个技能运行时实例。
        /// </summary>
        /// <param name="skillConfig">技能根配置，定义 layer、冷却、资源消耗等全局规则。</param>
        /// <param name="metaSkillConfigs">本技能图内会引用到的 MetaSkill 配置集合。</param>
        /// <param name="context">本技能实例共享的执行上下文。</param>
        public SkillRuntime(
            SkillConfig skillConfig,
            Dictionary<string, MetaSkillConfig> metaSkillConfigs,
            SkillContext context)
        {
            _skillConfig = skillConfig;
            _metaSkillConfigs = metaSkillConfigs ?? new Dictionary<string, MetaSkillConfig>();
            _context = context ?? new SkillContext();

            if (_context.EffectExecutor == null)
            {
                _context.EffectExecutor = new SkillEffectRuntime();
            }

            if (_context.EffectExecutor is SkillEffectRuntime runtimeEffectExecutor)
            {
                PreloadSkillEffects(runtimeEffectExecutor);
            }

            _context.SkillConfig = _skillConfig;
            _context.SkillFlowContext ??= new SkillFlowContext();
            _context.SkillFlowContext.SkillId = _skillConfig != null ? _skillConfig.SkillId : string.Empty;
            _context.SkillFlowContext.SkillRuntimeId = _runtimeInstanceId;
            EnsureLayerCooldownState();
        }

        private void PreloadSkillEffects(SkillEffectRuntime effectRuntime)
        {
            if (effectRuntime == null || _metaSkillConfigs == null)
            {
                return;
            }

            foreach (MetaSkillConfig metaSkillConfig in _metaSkillConfigs.Values)
            {
                PreloadMetaSkillEffects(effectRuntime, metaSkillConfig);
            }
        }

        private static void PreloadMetaSkillEffects(SkillEffectRuntime effectRuntime, MetaSkillConfig metaSkillConfig)
        {
            if (effectRuntime == null || metaSkillConfig == null)
            {
                return;
            }

            effectRuntime.Preload(metaSkillConfig.OnAddEffect);
            effectRuntime.Preload(metaSkillConfig.OnEndEffect);
            PreloadTimelineEffects(effectRuntime, metaSkillConfig.GetExecuteTimeline());
            PreloadTimelineEffects(effectRuntime, metaSkillConfig.GetRecoveryTimeline());
        }

        private static void PreloadTimelineEffects(SkillEffectRuntime effectRuntime, StateTimelineConfig timeline)
        {
            if (effectRuntime == null || timeline == null || timeline.Tracks == null)
            {
                return;
            }

            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                TimelineTrackConfig track = timeline.Tracks[trackIndex];
                if (track == null)
                {
                    continue;
                }

                if (track.HitBoxes != null)
                {
                    for (int hitBoxIndex = 0; hitBoxIndex < track.HitBoxes.Count; hitBoxIndex++)
                    {
                        HitBoxConfig hitBox = track.HitBoxes[hitBoxIndex];
                        if (hitBox != null)
                        {
                            effectRuntime.Preload(hitBox.OnHitEffect);
                        }
                    }
                }

                if (track.Bullets != null)
                {
                    for (int bulletIndex = 0; bulletIndex < track.Bullets.Count; bulletIndex++)
                    {
                        BulletConfig bullet = track.Bullets[bulletIndex];
                        if (bullet != null)
                        {
                            effectRuntime.Preload(bullet.OnHitEffect);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 当前所在的技能图节点。
        /// </summary>
        public MetaSkillNodeConfig CurrentNode => _currentNode;

        /// <summary>
        /// 当前运行时持有的全部 MetaSkill 配置。
        /// </summary>
        public IEnumerable<MetaSkillConfig> MetaSkillConfigs => _metaSkillConfigs != null ? _metaSkillConfigs.Values : Array.Empty<MetaSkillConfig>();

        /// <summary>
        /// 当前活动 layer 下标；赋值时会自动限制在有效 layer 范围内。
        /// </summary>
        public int ActiveLayerIndex
        {
            get => _activeLayerIndex;
            set => _activeLayerIndex = NormalizeLayerIndex(value);
        }

        /// <summary>
        /// 当前活动 layer 剩余冷却时间。
        /// </summary>
        public float CooldownRemaining => GetCooldownRemaining(GetSnapshotLayerIndex());

        /// <summary>
        /// 最近一次 Trigger 失败原因。
        /// </summary>
        public string LastTriggerFailureReason => _lastTriggerFailureReason;

        /// <summary>
        /// 从指定 layer 和起始节点显式进入施法状态。
        /// 这个入口更像是“外部强制启动技能图”，而不是正常输入触发入口。
        /// </summary>
        /// <param name="layerIndex">要进入的 layer 下标。</param>
        /// <param name="startNodeId">起始节点 Id；为空时会使用 layer 的默认起始节点。</param>
        public void EnterCasting(int layerIndex = 0, string startNodeId = null)
        {
            EnsureStateControllerSubscription();
            EnsureLayerCooldownState();
            if (_skillConfig == null || _skillConfig.Layers == null || _skillConfig.Layers.Count <= layerIndex)
            {
                return;
            }

            _activeLayerIndex = NormalizeLayerIndex(layerIndex);
            _context.SkillConfig = _skillConfig;
            SkillLayerConfig layer = _skillConfig.Layers[_activeLayerIndex];
            MetaSkillNodeConfig startNode = ResolveStartNode(layer, startNodeId);
            if (!TryGetMetaSkillConfig(startNode, out MetaSkillConfig metaSkillConfig))
            {
                return;
            }

            if (!TryBeginSkillCast(metaSkillConfig, _activeLayerIndex))
            {
                PublishSnapshot(false);
                return;
            }

            _currentLayer = layer;
            ResetSkillAggregationContext();
            if (_context.CharacterAnimationController == null)
            {
                _context.CharacterAnimationController = ResolveCharacterAnimationController(_context.Caster != null ? _context.Caster.UnitObject : null);
            }

            ApplyTags(_context.TagQueryService, _skillConfig, _skillConfig.Tags, SkillTagSourceId);
            SwitchToNode(startNode, metaSkillConfig, false, -1f);
            PublishSnapshot(true);
        }

        /// <summary>
        /// 退出当前技能施法。
        /// 这个入口用于对外部暴露一个简单的结束动作；是否推进到下一层由内部版本决定。
        /// </summary>
        public void ExitCasting()
        {
            ExitCasting(false);
        }

        /// <summary>
        /// 统一的技能退出逻辑。
        ///
        /// 它负责：
        /// 1. 退出当前 MetaSkillRuntime。
        /// 2. 清理 continuation 状态。
        /// 3. 清空当前 node/layer 与当前 MetaSkill 引用。
        /// 4. 重置 SkillFlowContext。
        /// 5. 退出动画桥施法状态。
        /// 6. 在满足条件时推进到下一层。
        /// </summary>
        /// <param name="advanceLayer">是否在退出后把活动 layer 指针推进到下一层。</param>
        private void ExitCasting(bool advanceLayer)
        {
            EnsureStateControllerSubscription();
            int completedLayerIndex = _activeLayerIndex;
            _currentMetaSkillRuntime?.Exit();
            _currentMetaSkillRuntime = null;
            _isAwaitingContinuation = false;
            _continuationAwaitStartTime = -1f;
            _currentNode = null;
            _currentLayer = null;
            _context.CurrentMetaSkillConfig = null;
            _context.CurrentMetaSkillContext = null;
            ResetSkillFlowContext();
            RemoveTags(_context.TagQueryService, _skillConfig, _skillConfig.Tags, SkillTagSourceId);
            if (advanceLayer && _skillConfig != null && _skillConfig.Layers != null && _skillConfig.Layers.Count > 0)
            {
                _activeLayerIndex = (NormalizeLayerIndex(completedLayerIndex) + 1) % _skillConfig.Layers.Count;
                PublishTrace("SkillLayerAdvanced", _skillConfig.SkillId, $"Next cast will start from layer {_activeLayerIndex}.");
            }

            PublishSnapshot(false);
        }

        /// <summary>
        /// 将技能配置标签挂到指定载体上。
        /// </summary>
        /// <param name="tagQueryService">标签查询服务；必须同时实现 ITagService 才能写入标签。</param>
        /// <param name="carrier">标签承载对象，通常是 SkillConfig。</param>
        /// <param name="container">要添加的标签容器。</param>
        /// <param name="sourceId">标签来源 Id，用于后续精确移除。</param>
        private static void ApplyTags(ITagQueryService tagQueryService, object carrier, TagContainer container, string sourceId)
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
        /// 从指定载体上移除技能配置标签。
        /// </summary>
        /// <param name="tagQueryService">标签查询服务；必须同时实现 ITagService 才能移除标签。</param>
        /// <param name="carrier">标签承载对象，通常是 SkillConfig。</param>
        /// <param name="container">要移除的标签容器。</param>
        /// <param name="sourceId">标签来源 Id，需与添加时保持一致。</param>
        private static void RemoveTags(ITagQueryService tagQueryService, object carrier, TagContainer container, string sourceId)
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
        /// 推进当前技能运行。
        ///
        /// 它主要处理两件事：
        /// 1. continuation 超时检查。
        /// 2. 当前 MetaSkillRuntime 的 Tick 和完成收尾。
        /// </summary>
        /// <param name="deltaTime">本帧推进的时间增量。</param>
        public void Tick(float deltaTime)
        {
            EnsureStateControllerSubscription();
            if (_isAwaitingContinuation && HasContinuationTimedOut())
            {
                if (_context.SkillFlowContext != null)
                {
                    _context.SkillFlowContext.LastEndReason = SkillMetaEndReason.Timeout;
                    _context.SkillFlowContext.IsContinuationWindowOpen = false;
                }

                PublishTrace("SkillComboTimeout", _currentNode != null ? _currentNode.NodeId : string.Empty, "Combo continuation timed out.");
                ExitCasting();
                return;
            }

            // [AICode] 配置了技能状态的 MetaSkill 不再由 SkillRuntime 推进内部阶段，状态生命周期完全交给 StateController。
            if (_currentMetaSkillRuntime != null &&
                (_context.SkillFlowContext == null || !_context.SkillFlowContext.IsStateDriven))
            {
                _currentMetaSkillRuntime.Tick(deltaTime);
            }

            if (_currentMetaSkillRuntime != null && _currentMetaSkillRuntime.IsCompleted)
            {
                if (_context.SkillFlowContext == null || !_context.SkillFlowContext.IsStateDriven)
                {
                    CompleteCurrentMetaSkill(false, "MetaSkill completed.");
                }
            }
            
            //okita:关于黑板变量，考虑它存在的必要性
            _context.DebugTimelineTime = GetActiveTimelineTime();
            PublishSnapshot(_currentMetaSkillRuntime != null || _currentNode != null);
        }

        /// <summary>
        /// 技能的标准输入入口。
        /// 外部输入最终都会转换为 SkillEventType 并从这里进入技能图匹配。
        /// </summary>
        /// <param name="eventType">输入事件类型，例如短按、长按、按下、抬起。</param>
        /// <param name="eventArgument">事件参数，通常带槽位信息，例如 slot:1。</param>
        /// <returns>是否成功匹配到一条技能图边并完成处理。</returns>
        public bool Trigger(SkillEventType eventType, string eventArgument = "")
        {
            _lastTriggerFailureReason = string.Empty;
            return TriggerInternal(eventType, eventArgument, true);
        }

        /// <summary>
        /// 技能图的核心触发处理函数。
        ///
        /// 它负责：
        /// 1. 选择 entry layer。
        /// 2. 在当前节点对应的 layer 中匹配 SkillEvent。
        /// 3. 处理切到 Exit 节点或下一个 MetaSkill 节点。
        /// 4. 记录失败原因，供调试和观察使用。
        /// </summary>
        /// <param name="eventType">当前输入事件类型。</param>
        /// <param name="eventArgument">事件附加参数。</param>
        /// <param name="allowRetryFromEntry">是否允许按 entry 逻辑重试；主要用于内部递归或补偿场景。</param>
        private bool TriggerInternal(SkillEventType eventType, string eventArgument, bool allowRetryFromEntry)
        {
            EnsureLayerCooldownState();
            if (_skillConfig == null || _skillConfig.Layers == null || _skillConfig.Layers.Count == 0)
            {
                _lastTriggerFailureReason = "SkillConfigMissing";
                return false;
            }
            
            //okita:不能被其他技能打断
            if (ShouldBlockSelfInterrupt(eventType))
            {
                _lastTriggerFailureReason = "SelfInterruptBlocked";
                PublishTrace("SkillSelfInterruptBlocked", _currentNode != null ? _currentNode.NodeId : string.Empty, "Current MetaSkill is still executing and cannot be interrupted by the same skill.");
                return false;
            }
            
            //okita:我不理解currentlayer为啥会是null,如果是null一定是不对的
            if (_currentLayer == null)
            {
                if (!TryPrepareEntryLayer(eventType, eventArgument))
                {
                    return false;
                }

                if (_currentLayer == null)
                {
                    int safeLayerIndex = NormalizeLayerIndex(_activeLayerIndex);
                    _currentLayer = _skillConfig.Layers[safeLayerIndex];
                    _activeLayerIndex = safeLayerIndex;
                    _context.SkillConfig = _skillConfig;
                }
            }

            if (_currentLayer == null || _currentLayer.SkillEvents == null)
            {
                _lastTriggerFailureReason = "SkillLayerEventsMissing";
                return false;
            }

            string fromNodeId = _currentNode != null ? _currentNode.NodeId : EntryNodeId;

            for (int i = 0; i < _currentLayer.SkillEvents.Count; i++)
            {
                SkillEventConfig skillEvent = _currentLayer.SkillEvents[i];
                if (skillEvent == null ||
                    skillEvent.FromNodeId != fromNodeId ||
                    !EvaluateSkillEvent(skillEvent, eventType, eventArgument))
                {
                    continue;
                }

                if (skillEvent.ToNodeId == ExitNodeId)
                {
                    ExitCasting(ShouldAdvanceLayerOnExit(eventType));
                    return true;
                }

                MetaSkillNodeConfig nextNode = FindNode(_currentLayer, skillEvent.ToNodeId);
                if (nextNode == null)
                {   
                    //okita:这个节点找不到的话，说明技能图配置有问题了，直接报错不继续了，现在的错误被保护的太好了，很难发现问题
                    _lastTriggerFailureReason = $"TargetNodeMissing:{skillEvent.ToNodeId}";
                    return false;
                }

                if (!TryGetMetaSkillConfig(nextNode, out MetaSkillConfig metaSkillConfig))
                {   
                    //oktia:和上面一样
                    _lastTriggerFailureReason = $"MetaSkillConfigMissing:{nextNode.MetaSkillAssetName}";
                    return false;
                }

                if (_currentNode == null)
                {
                    if (!TryBeginSkillCast(metaSkillConfig, _activeLayerIndex))
                    {
                        PublishSnapshot(false);
                        return false;
                    }

                    if (_context.CharacterAnimationController == null)
                    {   
                        //okita:caster后续需要直接改成具体的unit,而不是一个object
                        _context.CharacterAnimationController = ResolveCharacterAnimationController(_context.Caster != null ? _context.Caster.UnitObject : null);
                    }
                    
                    //okita:这里看起来问题很大，好像正好写反了
                    if (!ShouldUseStateDrivenMetaSkill(metaSkillConfig))
                    {
                    }
                }
                //okita:有currentnode,currentmetaskillruntime还可以为null?
                else if (_currentMetaSkillRuntime == null)
                {
                    if (_context.CharacterAnimationController == null)
                    {
                        _context.CharacterAnimationController = ResolveCharacterAnimationController(_context.Caster != null ? _context.Caster.UnitObject : null);
                    }

                    if (!ShouldUseStateDrivenMetaSkill(metaSkillConfig))
                    {
                    }
                }

                bool wasAwaitingContinuation = _isAwaitingContinuation;
                float previousContinuationAwaitStartTime = _continuationAwaitStartTime;
                if (!SwitchToNode(nextNode, metaSkillConfig, wasAwaitingContinuation, previousContinuationAwaitStartTime))
                {
                    return false;
                }

                PublishTrace("SkillTransition", nextNode.NodeId, "Skill node switched.");
                PublishSnapshot(true);
                return true;
            }

            _lastTriggerFailureReason = $"NoTransitionMatched:from={fromNodeId},event={eventType},arg={eventArgument}";
            return false;
        }

        /// <summary>
        /// 切换到下一个技能节点。
        ///
        /// 这里是技能图层面的“节点切换”，不是状态机层面的状态切换。
        /// 如果下一个 MetaSkill 配置为状态驱动，则这里会进一步请求 StateController 切状态。
        /// </summary>
        /// <param name="nextNode">目标节点。</param>
        /// <param name="metaSkillConfig">目标节点绑定的 MetaSkill 配置。</param>
        /// <param name="previousAwaitingContinuation">进入本次切换前，是否正处于 continuation 等待状态。</param>
        /// <param name="previousContinuationAwaitStartTime">切换前 continuation 窗口的起始时间。</param>
        private bool SwitchToNode(
            MetaSkillNodeConfig nextNode,
            MetaSkillConfig metaSkillConfig,
            bool previousAwaitingContinuation,
            float previousContinuationAwaitStartTime)
        {   
            // [AICode] 状态驱动节点先确认 StateController 接受切换，再提交 SkillRuntime 本地现场，避免失败后回滚。
            if (nextNode == null || metaSkillConfig == null)
            {   
                return false;
            }

            if (ShouldUseStateDrivenMetaSkill(metaSkillConfig))
            {
                if (!TryStartStateDrivenMetaSkill(nextNode, metaSkillConfig, previousAwaitingContinuation))
                {
                    if (_context.StateController != null &&
                        _context.StateController.IsRecoveryActive(metaSkillConfig.SkillStateTimeLineState.Layer) &&
                        !string.IsNullOrWhiteSpace(_lastTriggerFailureReason) &&
                        _lastTriggerFailureReason.StartsWith("StateTransitionRejected:", StringComparison.Ordinal))
                    {
                        PublishTrace("RecoveryCancelRejected", metaSkillConfig.MetaSkillId, "Recovery remains active because the replacement execute state was rejected.");
                        return false;
                    }

                    if (previousAwaitingContinuation &&
                        !string.IsNullOrWhiteSpace(_lastTriggerFailureReason) &&
                        _lastTriggerFailureReason.StartsWith("StateTransitionRejected:", StringComparison.Ordinal))
                    {
                        _isAwaitingContinuation = true;
                        _continuationAwaitStartTime = previousContinuationAwaitStartTime;
                        PublishTrace("SkillContinuationPreserved", _currentNode != null ? _currentNode.NodeId : string.Empty, "Continuation window remains active because the requested follow-up skill state is still blocked by another active skill state.");
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(_lastTriggerFailureReason))
                    {
                        _lastTriggerFailureReason = $"SkillStateRequestFailed:{metaSkillConfig.MetaSkillId}";
                    }

                    PublishTrace("SkillStateRequestFailed", metaSkillConfig.MetaSkillId, "SkillState request was rejected by StateController.");
                    ExitCasting();
                    return false;
                }

                _isAwaitingContinuation = false;
                _continuationAwaitStartTime = -1f;
                _currentNode = nextNode;
                _context.CurrentMetaSkillConfig = metaSkillConfig;
                return true;
            }

            _currentMetaSkillRuntime?.Exit();
            _currentMetaSkillRuntime = null;
            _isAwaitingContinuation = false;
            _continuationAwaitStartTime = -1f;
            _currentNode = nextNode;
            _context.CurrentMetaSkillConfig = metaSkillConfig;
            UpdateCurrentMetaSkillFlowIdentity(nextNode, metaSkillConfig);
            _currentMetaSkillRuntime = new MetaSkillRuntime(metaSkillConfig, _context);
            _currentMetaSkillRuntime.Enter();
            return true;
        }

        /// <summary>
        /// 根据节点上记录的资源名查找 MetaSkill 配置。
        /// </summary>
        /// <param name="node">技能图节点。</param>
        /// <param name="metaSkillConfig">找到的 MetaSkill 配置。</param>
        /// <returns>是否成功找到可用配置。</returns>
        private bool TryGetMetaSkillConfig(MetaSkillNodeConfig node, out MetaSkillConfig metaSkillConfig)
        {
            metaSkillConfig = null;
            return node != null &&
                   !string.IsNullOrEmpty(node.MetaSkillAssetName) &&
                   _metaSkillConfigs.TryGetValue(node.MetaSkillAssetName, out metaSkillConfig) &&
                   metaSkillConfig != null;
        }

        /// <summary>
        /// 发布技能快照。
        ///
        /// snapshot 表示“某一时刻的整体状态截面”，
        /// 常用于调试面板、观察器或运行时状态采样。
        /// </summary>
        /// <param name="isCasting">当前是否处于施法状态。</param>
        private void PublishSnapshot(bool isCasting)
        {
            SkillRuntimeDebugBus.PublishSnapshot(_context, new SkillRuntimeSnapshot
            {
                IsCasting = isCasting,
                SkillId = _skillConfig != null ? _skillConfig.SkillId : string.Empty,
                CooldownRemaining = CooldownRemaining,
                LayerIndex = _currentLayer != null ? _currentLayer.LayerIndex : -1,
                CurrentNodeId = _currentNode != null ? _currentNode.NodeId : string.Empty,
                CurrentMetaSkillId = _context.CurrentMetaSkillConfig != null ? _context.CurrentMetaSkillConfig.MetaSkillId : string.Empty,
                TimelineTime = GetActiveTimelineTime(),
                LastTimelineItemType = _context.DebugLastTimelineItemType,
                LastTimelineItemId = _context.DebugLastTimelineItemId,
                LastEffectNodeId = _context.DebugLastEffectNodeId,
            });
        }

        /// <summary>
        /// 发布一条技能调试轨迹。
        ///
        /// trace 表示“运行过程中的一条事件记录”，
        /// 比如节点切换、状态请求失败、continuation 超时等。
        /// </summary>
        /// <param name="traceType">轨迹类型。</param>
        /// <param name="payloadId">与本条轨迹关联的节点、状态或配置 Id。</param>
        /// <param name="message">附加说明信息。</param>
        private void PublishTrace(string traceType, string payloadId, string message)
        {
            SkillRuntimeDebugBus.PublishTrace(_context, new SkillRuntimeTraceEvent
            {
                TraceType = traceType,
                NodeId = _currentNode != null ? _currentNode.NodeId : string.Empty,
                MetaSkillId = _context.CurrentMetaSkillConfig != null ? _context.CurrentMetaSkillConfig.MetaSkillId : string.Empty,
                PayloadId = payloadId,
                Time = GetActiveTimelineTime(),
                Message = message,
            });
        }

        /// <summary>
        /// 确保当前 SkillRuntime 已正确订阅到所属的 StateController。
        ///
        /// 这样当技能状态进入、完成或被打断时，SkillRuntime 才能收到回调并更新自己的流程状态。
        /// </summary>
        /// okita:这种ensure的设计，我认为是不需要不应该出现的
        private void EnsureStateControllerSubscription()
        {
            if (_context.StateController == _observedStateController)
            {
                return;
            }

            if (_observedStateController != null)
            {
                _observedStateController.SkillStateChanged -= OnSkillStateChanged;
            }

            _observedStateController = _context.StateController;
            if (_observedStateController != null)
            {
                _observedStateController.SkillStateChanged += OnSkillStateChanged;
            }
        }

        /// <summary>
        /// 处理来自 StateController 的状态通知。
        ///
        /// 这里只接收属于当前 SkillRuntime 实例的通知，
        /// 然后再按进入、完成、中断分别更新技能流程。
        /// </summary>
        /// <param name="notification">状态机回发的技能状态通知。</param>
        private void OnSkillStateChanged(SkillStateNotification notification)
        {
            if (notification == null || notification.TransitionContext == null)
            {
                return;
            }

            if (!string.Equals(notification.TransitionContext.SkillRuntimeId, _runtimeInstanceId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            SkillFlowContext flowContext = _context.SkillFlowContext;
            if (flowContext == null)
            {
                return;
            }

            switch (notification.Kind)
            {
                case SkillStateNotificationKind.Entered:
                    flowContext.ActiveStateId = notification.StateId;
                    flowContext.ActivePhaseRole = notification.TransitionContext.PhaseRole;
                    flowContext.IsAwaitingStateCompletion = true;
                    flowContext.IsContinuationWindowOpen = false;
                    if (_context.CurrentMetaSkillContext != null)
                    {
                        _context.CurrentMetaSkillContext.PhaseRole = notification.TransitionContext.PhaseRole;
                    }

                    PublishTrace("SkillStateEntered", notification.StateId, $"Skill state '{notification.StateId}' entered as {notification.TransitionContext.PhaseRole}.");
                    break;

                case SkillStateNotificationKind.Completed:
                    HandleSkillStateCompleted(notification, flowContext);
                    break;

                case SkillStateNotificationKind.Interrupted:
                    HandleSkillStateInterrupted(notification, flowContext);
                    break;
            }
        }

        //okita:下面这两个函数完成的内容相似，应该合并

        /// <summary>
        /// 处理技能状态正常完成通知。
        /// </summary>
        /// <param name="notification">状态机回发的完成通知。</param>
        /// <param name="flowContext">当前技能流程上下文。</param>
        private void HandleSkillStateCompleted(SkillStateNotification notification, SkillFlowContext flowContext)
        {
            if (notification == null || flowContext == null)
            {
                return;
            }

            if (notification.TransitionContext.PhaseRole == SkillStatePhaseRole.Execute &&
                notification.TransitionContext.RecoveryStateLayer != StateLayerType.None &&
                !string.IsNullOrWhiteSpace(flowContext.RecoveryStateId) &&
                string.Equals(notification.TargetStateId, flowContext.RecoveryStateId, StringComparison.OrdinalIgnoreCase))
            {
                // [AICode] execute -> recovery 只结算 execute state 对应的 metaskill，不提前结束整个技能流程。
                _currentMetaSkillRuntime?.CompleteExecuteStateNormally();
                _currentMetaSkillRuntime = null;
                flowContext.ActiveStateId = notification.TargetStateId;
                flowContext.ActiveStateLayer = notification.TransitionContext.RecoveryStateLayer;
                flowContext.ActivePhaseRole = SkillStatePhaseRole.Recovery;
                flowContext.IsAwaitingStateCompletion = true;
                PublishTrace("SkillStateCompleted", notification.StateId, "Execute state completed and transitioned into recovery state.");
                return;
            }

            CompleteCurrentMetaSkill(false, $"Skill state '{notification.StateId}' completed normally.");
        }

        /// <summary>
        /// 处理技能状态被其他状态中断的通知。
        /// </summary>
        /// <param name="notification">状态机回发的中断通知。</param>
        /// <param name="flowContext">当前技能流程上下文。</param>
        private void HandleSkillStateInterrupted(SkillStateNotification notification, SkillFlowContext flowContext)
        {
            if (notification == null || flowContext == null)
            {
                return;
            }

            flowContext.LastInterruptSourceStateId = notification.TargetStateId;
            if (notification.RequestType == StateTransitionRequestType.RecoveryCancel)
            {
                HandleRecoveryCancelled(notification, flowContext);
                return;
            }

            CompleteCurrentMetaSkill(true, $"Skill state '{notification.StateId}' was interrupted by '{notification.TargetStateId}'.");
        }

        /// <summary>
        /// Recovery 取消只结束后摇流程，不重复结算已经正常完成的 Execute，也不触发旧技能的 OnMetaSkillEnd 跳转。
        /// </summary>
        private void HandleRecoveryCancelled(SkillStateNotification notification, SkillFlowContext flowContext)
        {
            bool continuesInSameRuntime = _context.StateController != null &&
                                          _context.StateController.IsSkillRuntimeActive(_runtimeInstanceId, StateLayerType.Action);

            flowContext.LastEndReason = SkillMetaEndReason.Cancelled;
            flowContext.IsAwaitingStateCompletion = false;
            flowContext.IsContinuationWindowOpen = false;
            flowContext.ClearActiveState();
            PublishTrace("RecoveryCancelled", notification.StateId,
                $"Recovery was cancelled and transitioned to '{notification.TargetStateId}'.");

            if (continuesInSameRuntime)
            {
                // TryStartStateDrivenMetaSkill 仍持有本次目标节点局部变量，返回后会建立新的 FlowContext。
                // 此处只清理旧 Recovery，不退出整个 SkillRuntime，避免移除仍需沿用的技能标签与图上下文。
                _isAwaitingContinuation = false;
                _continuationAwaitStartTime = -1f;
                return;
            }

            if (CanAwaitManualContinuation(_currentNode))
            {
                // RecoveryCancel 无论来自移动、其他技能、受击还是强制状态，
                // 都只取消角色的后摇表现，不重置技能图进度。
                // 保留 currentNode/currentLayer 并开放 continuation，下一次合法输入仍从当前段连接后续段。
                EnterContinuationWindow(false, flowContext);
                return;
            }

            ExitCasting(false);
        }

        /// <summary>
        /// 判断 MetaSkill 是否应交给 StateController 驱动。
        /// </summary>
        /// <param name="metaSkillConfig">待执行的 MetaSkill 配置。</param>
        /// <returns>是否使用状态机驱动路径。</returns>
        private bool ShouldUseStateDrivenMetaSkill(MetaSkillConfig metaSkillConfig)
        {
            return metaSkillConfig != null && metaSkillConfig.HasSkillState && _context.StateController != null;
        }

        /// <summary>
        /// 尝试启动状态机驱动的 MetaSkill。
        /// 会初始化流程上下文、执行 OnAddEffect，并向 StateController 发起切状态请求。
        /// </summary>
        /// <param name="metaSkillConfig">要启动的 MetaSkill 配置。</param>
        /// <returns>状态请求是否被接受。</returns>
        private bool TryStartStateDrivenMetaSkill(MetaSkillNodeConfig nextNode, MetaSkillConfig metaSkillConfig, bool isContinuation)
        {   
            EnsureStateControllerSubscription();
            if (_context.StateController == null || metaSkillConfig == null || metaSkillConfig.SkillStateTimeLineState == null)
            {
                _lastTriggerFailureReason = metaSkillConfig == null
                    ? "MetaSkillConfigNull"
                    : (_context.StateController == null
                        ? "StateControllerMissing"
                        : "ExecuteStateMissing");
                return false;
            }

            SkillTransitionContext transitionContext = CreateSkillTransitionContext(nextNode, metaSkillConfig, isContinuation);
            StateLayerType executeLayer = metaSkillConfig.SkillStateTimeLineState.Layer;
            bool cancelsRecovery = _context.StateController.IsRecoveryActive(executeLayer);
            bool didEnterState = _context.StateController.TryChangeState(new StateTransitionRequest
            {
                RequestType = cancelsRecovery
                    ? StateTransitionRequestType.RecoveryCancel
                    : StateTransitionRequestType.SkillDriven,
                RecoveryCancelReason = cancelsRecovery
                    ? RecoveryCancelReason.Skill
                    : RecoveryCancelReason.None,
                SourceStateId = _context.StateController.GetCurrentStateId(executeLayer),
                SourceLayerHint = executeLayer,
                TargetStateId = transitionContext.ExecuteStateId,
                IgnoreInterruptRules = false,
                RequestedStartTime = 0f,
                SkillTransitionContext = transitionContext,
            });

            if (!didEnterState)
            {
                _lastTriggerFailureReason = $"StateTransitionRejected:{transitionContext.ExecuteStateId}";
                return false;
            }

            _currentMetaSkillRuntime = new MetaSkillRuntime(metaSkillConfig, _context);
            _currentMetaSkillRuntime.Enter();
            BeginStateDrivenFlow(nextNode, metaSkillConfig, transitionContext);

            PublishTrace("SkillStateRequested", transitionContext.ExecuteStateId, "MetaSkill requested state-driven execution.");
            return true;
        }

        /// <summary>
        /// 创建传递给 StateController 的技能切状态上下文。
        /// </summary>
        /// <param name="metaSkillConfig">当前 MetaSkill 配置。</param>
        /// <returns>包含技能、节点、执行状态与恢复状态信息的切换上下文。</returns>
        private SkillTransitionContext CreateSkillTransitionContext(MetaSkillNodeConfig nextNode, MetaSkillConfig metaSkillConfig, bool isContinuation)
        {
            return new SkillTransitionContext
            {
                SkillRuntimeId = _runtimeInstanceId,
                SkillId = _skillConfig != null ? _skillConfig.SkillId : string.Empty,
                MetaSkillId = metaSkillConfig != null ? metaSkillConfig.MetaSkillId : string.Empty,
                NodeId = nextNode != null ? nextNode.NodeId : string.Empty,
                ExecuteStateId = metaSkillConfig != null && metaSkillConfig.SkillStateTimeLineState != null ? metaSkillConfig.SkillStateTimeLineState.StateId : string.Empty,
                ExecuteStateLayer = metaSkillConfig != null && metaSkillConfig.SkillStateTimeLineState != null ? metaSkillConfig.SkillStateTimeLineState.Layer : StateLayerType.None,
                RecoveryStateId = metaSkillConfig != null && metaSkillConfig.RecoverySkillStateTimeLineState != null ? metaSkillConfig.RecoverySkillStateTimeLineState.StateId : string.Empty,
                RecoveryStateLayer = metaSkillConfig != null && metaSkillConfig.RecoverySkillStateTimeLineState != null ? metaSkillConfig.RecoverySkillStateTimeLineState.Layer : StateLayerType.None,
                ActiveStateId = metaSkillConfig != null && metaSkillConfig.SkillStateTimeLineState != null ? metaSkillConfig.SkillStateTimeLineState.StateId : string.Empty,
                ActiveStateLayer = metaSkillConfig != null && metaSkillConfig.SkillStateTimeLineState != null ? metaSkillConfig.SkillStateTimeLineState.Layer : StateLayerType.None,
                PhaseRole = SkillStatePhaseRole.Execute,
                IsContinuation = isContinuation,
            };
        }

        /// <summary>
        /// 初始化状态机驱动 MetaSkill 的流程上下文。
        /// </summary>
        /// <param name="metaSkillConfig">当前 MetaSkill 配置。</param>
        /// <param name="transitionContext">本次状态切换上下文。</param>
        private void BeginStateDrivenFlow(MetaSkillNodeConfig nextNode, MetaSkillConfig metaSkillConfig, SkillTransitionContext transitionContext)
        {
            SkillFlowContext flowContext = _context.SkillFlowContext ??= new SkillFlowContext();
            flowContext.SkillRuntimeId = _runtimeInstanceId;
            flowContext.SkillId = _skillConfig != null ? _skillConfig.SkillId : string.Empty;
            flowContext.CurrentMetaSkillId = metaSkillConfig != null ? metaSkillConfig.MetaSkillId : string.Empty;
            flowContext.CurrentNodeId = nextNode != null ? nextNode.NodeId : string.Empty;
            flowContext.ExecuteStateId = transitionContext != null ? transitionContext.ExecuteStateId : string.Empty;
            flowContext.ExecuteStateLayer = transitionContext != null ? transitionContext.ExecuteStateLayer : StateLayerType.None;
            flowContext.RecoveryStateId = transitionContext != null ? transitionContext.RecoveryStateId : string.Empty;
            flowContext.RecoveryStateLayer = transitionContext != null ? transitionContext.RecoveryStateLayer : StateLayerType.None;
            flowContext.ActiveStateId = transitionContext != null ? transitionContext.ActiveStateId : string.Empty;
            flowContext.ActiveStateLayer = transitionContext != null ? transitionContext.ActiveStateLayer : StateLayerType.None;
            flowContext.ActivePhaseRole = transitionContext != null ? transitionContext.PhaseRole : SkillStatePhaseRole.None;
            flowContext.LastEndReason = SkillMetaEndReason.None;
            flowContext.LastInterruptSourceStateId = string.Empty;
            flowContext.IsStateDriven = true;
            flowContext.IsAwaitingStateCompletion = true;
            flowContext.IsContinuationWindowOpen = false;
        }

        private void UpdateCurrentMetaSkillFlowIdentity(MetaSkillNodeConfig nextNode, MetaSkillConfig metaSkillConfig)
        {
            SkillFlowContext flowContext = _context.SkillFlowContext ??= new SkillFlowContext();
            flowContext.SkillRuntimeId = _runtimeInstanceId;
            flowContext.SkillId = _skillConfig != null ? _skillConfig.SkillId : string.Empty;
            flowContext.CurrentNodeId = nextNode != null ? nextNode.NodeId : string.Empty;
            flowContext.CurrentMetaSkillId = metaSkillConfig != null ? metaSkillConfig.MetaSkillId : string.Empty;
        }

        private void ResetSkillAggregationContext()
        {
            _context.AffectedTargets.Clear();
            _context.DataContext = new DataContext();
            _context.CurrentMetaSkillContext = null;
            _context.LastMetaSkillContext = null;
        }

        /// <summary>
        /// 清空当前技能流程上下文中的 MetaSkill 与状态阶段信息。
        /// </summary>
        private void ResetSkillFlowContext()
        {
            SkillFlowContext flowContext = _context.SkillFlowContext;
            if (flowContext == null)
            {
                return;
            }

            flowContext.CurrentMetaSkillId = string.Empty;
            flowContext.CurrentNodeId = string.Empty;
            flowContext.ExecuteStateId = string.Empty;
            flowContext.ExecuteStateLayer = StateLayerType.None;
            flowContext.RecoveryStateId = string.Empty;
            flowContext.RecoveryStateLayer = StateLayerType.None;
            flowContext.LastEndReason = SkillMetaEndReason.None;
            flowContext.LastInterruptSourceStateId = string.Empty;
            flowContext.IsStateDriven = false;
            flowContext.IsContinuationWindowOpen = false;
            flowContext.ClearActiveState();
        }

        /// <summary>
        /// 统一完成当前 MetaSkill 的收尾逻辑。
        /// 根据是否被中断决定是否执行 OnEndEffect、是否开启 continuation 窗口，以及是否退出施法。
        /// </summary>
        /// <param name="interrupted">当前 MetaSkill 是否以中断方式结束。</param>
        /// <param name="traceMessage">用于调试轨迹的说明信息。</param>
        private void CompleteCurrentMetaSkill(bool interrupted, string traceMessage)
        {
            SkillFlowContext flowContext = _context.SkillFlowContext;
            MetaSkillConfig completedMetaSkillConfig = _currentMetaSkillRuntime != null
                ? _currentMetaSkillRuntime.Config
                : _context.CurrentMetaSkillConfig;
            string completedMetaSkillId = !string.IsNullOrWhiteSpace(flowContext != null ? flowContext.CurrentMetaSkillId : string.Empty)
                ? flowContext.CurrentMetaSkillId
                : (completedMetaSkillConfig != null ? completedMetaSkillConfig.MetaSkillId : string.Empty);
            if (string.IsNullOrWhiteSpace(completedMetaSkillId) && completedMetaSkillConfig == null)
            {
                return;
            }

            if (flowContext != null)
            {
                flowContext.LastEndReason = interrupted ? SkillMetaEndReason.Interrupted : SkillMetaEndReason.Normal;
                flowContext.IsAwaitingStateCompletion = false;
                flowContext.ActiveStateId = string.Empty;
                flowContext.ActiveStateLayer = StateLayerType.None;
                flowContext.ActivePhaseRole = SkillStatePhaseRole.None;
            }

            if (_currentMetaSkillRuntime != null)
            {
                // [AICode] 状态驱动路径下，由 SkillRuntime 按 execute state 的最终结果显式通知 MetaSkillRuntime 收口。
                if (flowContext != null && flowContext.IsStateDriven)
                {
                    if (interrupted)
                    {
                        _currentMetaSkillRuntime.InterruptExecuteState();
                    }
                    else if (!_currentMetaSkillRuntime.IsCompleted)
                    {
                        _currentMetaSkillRuntime.CompleteExecuteStateNormally();
                    }
                }
                else
                {
                    _currentMetaSkillRuntime.Exit(!interrupted);
                }

                _currentMetaSkillRuntime = null;
            }

            PublishTrace(interrupted ? "MetaSkillInterruptedEnd" : "MetaSkillCompleted", completedMetaSkillId, traceMessage);

            if (!Trigger(SkillEventType.OnMetaSkillEnd))
            {
                if (CanAwaitManualContinuation(_currentNode))
                {
                    EnterContinuationWindow(interrupted, flowContext);
                }
                else
                {
                    ExitCasting(!interrupted);
                }

                return;
            }

            if (flowContext != null)
            {
                flowContext.IsContinuationWindowOpen = false;
            }
        }

        private SkillEffectResult GetLatestEffectResult()
        {
            if (_context.CurrentMetaSkillContext != null && _context.CurrentMetaSkillContext.LastEffectContext != null)
            {
                return _context.CurrentMetaSkillContext.LastEffectContext;
            }

            if (_context.LastMetaSkillContext != null && _context.LastMetaSkillContext.LastEffectContext != null)
            {
                return _context.LastMetaSkillContext.LastEffectContext;
            }

            return SkillEffectResult.None;
        }

        /// <summary>
        /// 获取当前活跃时间线的播放时间。
        /// 非状态驱动路径取 MetaSkillRuntime 时间；状态驱动路径取 StateController 当前状态时间。
        /// </summary>
        /// <returns>当前时间线时间；无活跃时间线时返回 0。</returns>
        private float GetActiveTimelineTime()
        {
            if (_currentMetaSkillRuntime != null &&
                (_context.SkillFlowContext == null || !_context.SkillFlowContext.IsStateDriven))
            {
                return _currentMetaSkillRuntime.TimelineTime;
            }

            if (_context.StateController == null || _context.SkillFlowContext == null || string.IsNullOrWhiteSpace(_context.SkillFlowContext.ActiveStateId))
            {
                return 0f;
            }

            return string.Equals(_context.StateController.GetCurrentStateId(_context.SkillFlowContext.ActiveStateLayer), _context.SkillFlowContext.ActiveStateId, StringComparison.OrdinalIgnoreCase)
                ? _context.StateController.GetStateElapsedTime(_context.SkillFlowContext.ActiveStateLayer)
                : 0f;
        }

        /// <summary>
        /// 进入等待手动连段输入的 continuation 窗口。
        /// </summary>
        /// <param name="interrupted">上一段是否因中断结束。</param>
        /// <param name="flowContext">当前技能流程上下文。</param>
        private void EnterContinuationWindow(bool interrupted, SkillFlowContext flowContext)
        {
            _context.CurrentMetaSkillConfig = null;
            _isAwaitingContinuation = true;
            _continuationAwaitStartTime = Time.time;
            if (flowContext != null)
            {
                flowContext.IsContinuationWindowOpen = true;
            }

            PublishTrace(
                interrupted ? "SkillChainContinuedAfterInterrupt" : "SkillAwaitContinuation",
                _currentNode != null ? _currentNode.NodeId : string.Empty,
                interrupted
                    ? "Skill state was interrupted; combo continuation window is now open."
                    : "MetaSkill completed and is waiting for a manual continuation.");
        }

        /// <summary>
        /// 判断一条技能图事件边是否被当前输入与条件命中。
        /// </summary>
        /// <param name="skillEvent">要检测的事件边配置。</param>
        /// <param name="eventType">当前输入事件类型。</param>
        /// <param name="eventArgument">当前输入事件参数。</param>
        /// <returns>事件与条件是否全部满足。</returns>
        private bool EvaluateSkillEvent(SkillEventConfig skillEvent, SkillEventType eventType, string eventArgument)
        {
            if (!MatchesEventEntries(skillEvent, eventType, eventArgument))
            {
                return false;
            }

            if (skillEvent.Conditions == null || skillEvent.Conditions.Count == 0)
            {
                return true;
            }
            
            //okita:这里condition暂且保留
            if (skillEvent.ConditionMode == SkillConditionMode.Any)
            {
                SkillEffectResult latestEffectResult = GetLatestEffectResult();
                for (int i = 0; i < skillEvent.Conditions.Count; i++)
                {
                    if (SkillEffectConditionUtility.Evaluate(skillEvent.Conditions[i], _context, latestEffectResult))
                    {
                        return true;
                    }
                }

                return false;
            }

            SkillEffectResult lastEffectResult = GetLatestEffectResult();
            for (int i = 0; i < skillEvent.Conditions.Count; i++)
            {
                if (!SkillEffectConditionUtility.Evaluate(skillEvent.Conditions[i], _context, lastEffectResult))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断事件边上的事件列表是否匹配当前输入。
        /// 支持 Any/All 两种匹配模式；事件列表为空时使用默认事件规则。
        /// </summary>
        /// <param name="skillEvent">事件边配置。</param>
        /// <param name="eventType">当前输入事件类型。</param>
        /// <param name="eventArgument">当前输入事件参数。</param>
        /// <returns>事件列表是否匹配。</returns>
        private static bool MatchesEventEntries(SkillEventConfig skillEvent, SkillEventType eventType, string eventArgument)
        {
            if (skillEvent == null)
            {
                return false;
            }

            if (skillEvent.Events == null || skillEvent.Events.Count == 0)
            {
                return MatchesDefaultSkillEvent(skillEvent, eventType, eventArgument);
            }

            if (skillEvent.EventMode == SkillConditionMode.Any)
            {
                for (int i = 0; i < skillEvent.Events.Count; i++)
                {
                    if (MatchesEventEntry(skillEvent.Events[i], eventType, eventArgument))
                    {
                        return true;
                    }
                }

                return false;
            }

            for (int i = 0; i < skillEvent.Events.Count; i++)
            {
                if (!MatchesEventEntry(skillEvent.Events[i], eventType, eventArgument))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断单个事件条目是否匹配当前输入。
        /// </summary>
        /// <param name="entry">事件条目配置。</param>
        /// <param name="eventType">当前输入事件类型。</param>
        /// <param name="eventArgument">当前输入事件参数。</param>
        /// <returns>事件类型一致，且参数为空或完全一致时返回 true。</returns>
        private static bool MatchesEventEntry(SkillEventEntryConfig entry, SkillEventType eventType, string eventArgument)
        {
            return entry != null &&
                   entry.EventType == eventType &&
                   (string.IsNullOrEmpty(entry.Argument) || string.Equals(entry.Argument, eventArgument));
        }

        /// <summary>
        /// 使用旧版默认规则匹配事件边。
        /// 从 Entry 出发默认匹配短按施法，非 Entry 出发默认匹配 MetaSkill 结束。
        /// </summary>
        /// <param name="skillEvent">事件边配置。</param>
        /// <param name="eventType">当前输入事件类型。</param>
        /// <param name="eventArgument">当前输入事件参数；默认规则暂不使用该参数。</param>
        /// <returns>是否命中默认事件。</returns>
        private static bool MatchesDefaultSkillEvent(SkillEventConfig skillEvent, SkillEventType eventType, string eventArgument)
        {
            SkillEventType defaultEventType = skillEvent.FromNodeId == EntryNodeId
                ? SkillEventType.CastSkillShort
                : SkillEventType.OnMetaSkillEnd;

            return defaultEventType == eventType;
        }

        /// <summary>
        /// 尝试开始一次技能施法。
        /// 只有从空节点进入第一段时才会检查冷却、资源和状态门禁，并启动 layer 冷却。
        /// </summary>
        /// <param name="metaSkillConfig">即将进入的第一段 MetaSkill 配置。</param>
        /// <param name="layerIndex">本次施法所属的 layer 下标。</param>
        /// <returns>是否允许开始施法。</returns>
        private bool TryBeginSkillCast(MetaSkillConfig metaSkillConfig, int layerIndex)
        {
            if (_currentNode != null)
            {
                return true;
            }

            if (!CanBeginSkillCast(metaSkillConfig, layerIndex, out string failureReason, out float cooldownRemaining))
            {   
                //okita:写这么多傻逼日志是干啥
                _lastTriggerFailureReason = failureReason;
                if (failureReason.StartsWith("CooldownBlocked:", StringComparison.Ordinal))
                {
                    PublishTrace("SkillCooldownBlocked", _skillConfig != null ? _skillConfig.SkillId : string.Empty, $"Layer {layerIndex} is still on cooldown for {cooldownRemaining:0.###}s.");
                }
                else if (string.Equals(failureReason, "ResourceRequirementBlocked", StringComparison.Ordinal))
                {
                    PublishTrace("SkillCostBlocked", _skillConfig != null ? _skillConfig.SkillId : string.Empty, "Skill resource requirement not met.");
                }

                return false;
            }

            if (!ConsumeResources())
            {
                _lastTriggerFailureReason = "ResourceConsumeBlocked";
                PublishTrace("SkillCostBlocked", _skillConfig != null ? _skillConfig.SkillId : string.Empty, "Skill resource consumption failed.");
                return false;
            }

            SetLayerCooldownStartTime(layerIndex, Time.time);
            return true;
        }

        /// <summary>
        /// 检查本次施法是否满足开始条件。
        /// </summary>
        /// <param name="metaSkillConfig">即将进入的 MetaSkill 配置。</param>
        /// <param name="layerIndex">要检查冷却的 layer 下标。</param>
        /// <param name="failureReason">失败原因，供调试或 UI 展示。</param>
        /// <param name="cooldownRemaining">当前 layer 剩余冷却时间。</param>
        /// <returns>冷却、资源和状态门禁是否全部通过。</returns>
        private bool CanBeginSkillCast(MetaSkillConfig metaSkillConfig, int layerIndex, out string failureReason, out float cooldownRemaining)
        {
            cooldownRemaining = GetCooldownRemaining(layerIndex);
            if (cooldownRemaining > 0f)
            {
                failureReason = $"CooldownBlocked:{cooldownRemaining:0.###}";
                return false;
            }

            if (metaSkillConfig != null &&
                metaSkillConfig.SkillStateTimeLineState != null &&
                _context.StateController != null &&
                !_context.StateController.CanRequestSkillTransition(metaSkillConfig.SkillStateTimeLineState.Layer))
            {
                failureReason = "RecoverySkillCancelBlocked";
                return false;
            }

            if (!HasRequiredResources())
            {
                failureReason = "ResourceRequirementBlocked";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        /// <summary>
        /// 检查施法者是否拥有技能所需资源。
        /// </summary>
        /// <returns>资源足够或技能没有资源消耗时返回 true。</returns>
        private bool HasRequiredResources()
        {
            if (_skillConfig == null || _skillConfig.ResourceCosts == null || _skillConfig.ResourceCosts.Count == 0)
            {
                return true;
            }

            if (_context.ResourceService == null)
            {
                return false;
            }

            for (int i = 0; i < _skillConfig.ResourceCosts.Count; i++)
            {
                SkillResourceCostConfig cost = _skillConfig.ResourceCosts[i];
                if (cost == null || cost.Amount <= 0f)
                {
                    continue;
                }

                if (!_context.ResourceService.HasResource(_context.Caster, cost.ResourceType, cost.Amount))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 消耗技能配置声明的资源。
        /// </summary>
        /// <returns>全部资源扣除成功，或技能没有资源消耗时返回 true。</returns>
        private bool ConsumeResources()
        {
            if (_skillConfig == null || _skillConfig.ResourceCosts == null || _skillConfig.ResourceCosts.Count == 0)
            {
                return true;
            }

            if (_context.ResourceService == null)
            {   
                //okita:这种没service的情况，怎么可能发生，写的啥，这里需要直接做报错处理，或者直接不处理，暴露问题
                return false;
            }

            for (int i = 0; i < _skillConfig.ResourceCosts.Count; i++)
            {
                SkillResourceCostConfig cost = _skillConfig.ResourceCosts[i];
                if (cost == null || cost.Amount <= 0f)
                {
                    continue;
                }

                if (!_context.ResourceService.TryConsumeResource(_context.Caster, cost.ResourceType, cost.Amount))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 计算指定 layer 的冷却结束时间。
        /// </summary>
        /// <param name="layerIndex">要查询的 layer 下标。</param>
        /// <returns>冷却结束的 Time.time 时间点；未进入冷却时返回 0。</returns>
        private float GetCooldownEndTime(int layerIndex)
        {
            EnsureLayerCooldownState();
            if (_skillConfig == null)
            {
                return 0f;
            }

            int safeLayerIndex = NormalizeLayerIndex(layerIndex);
            float cooldownStartTime = _layerCooldownStartTimes[safeLayerIndex];
            if (cooldownStartTime < 0f)
            {
                return 0f;
            }

            return cooldownStartTime + Mathf.Max(0f, _skillConfig.Cooldown);
        }

        /// <summary>
        /// 获取指定 layer 的剩余冷却。
        /// </summary>
        /// <param name="layerIndex">要查询的 layer 下标。</param>
        /// <returns>剩余冷却秒数。</returns>
        private float GetCooldownRemaining(int layerIndex)
        {
            return Mathf.Max(0f, GetCooldownEndTime(layerIndex) - Time.time);
        }

        /// <summary>
        /// 在空闲状态下为本次输入选择可进入的起始 layer。
        /// 会从当前活动 layer 开始向后扫描，跳过仍在冷却但事件匹配的 layer。
        /// </summary>
        /// <param name="eventType">当前输入事件类型。</param>
        /// <param name="eventArgument">当前输入事件参数。</param>
        /// <returns>是否成功准备入口 layer；没有匹配入口时也会返回 true，交给后续逻辑处理。</returns>
        private bool TryPrepareEntryLayer(SkillEventType eventType, string eventArgument)
        {   
            //okita:这个函数的调用时机比较奇怪
            //okita:理解了，就设置个layer还有其他参数。。。感觉写的好复杂
            if (_currentNode != null || _currentLayer != null)
            {
                return true;
            }

            if (!IsSelfInputEvent(eventType))
            {
                return true;
            }

            int startLayerIndex = NormalizeLayerIndex(_activeLayerIndex);
            bool foundMatchingEntry = false;
            float minBlockedCooldown = float.MaxValue;

            for (int offset = 0; offset < _skillConfig.Layers.Count; offset++)
            {
                int candidateLayerIndex = (startLayerIndex + offset) % _skillConfig.Layers.Count;
                SkillLayerConfig candidateLayer = _skillConfig.Layers[candidateLayerIndex];
                if (!TryGetEntryTransition(candidateLayer, eventType, eventArgument, out MetaSkillNodeConfig nextNode, out MetaSkillConfig metaSkillConfig))
                {
                    continue;
                }

                foundMatchingEntry = true;
                if (!CanBeginSkillCast(metaSkillConfig, candidateLayerIndex, out string failureReason, out float cooldownRemaining))
                {
                    if (failureReason.StartsWith("CooldownBlocked:", StringComparison.Ordinal))
                    {
                        minBlockedCooldown = Mathf.Min(minBlockedCooldown, cooldownRemaining);
                        continue;
                    }

                    _lastTriggerFailureReason = failureReason;
                    return false;
                }

                _activeLayerIndex = candidateLayerIndex;
                _currentLayer = candidateLayer;
                _context.SkillConfig = _skillConfig;
                if (candidateLayerIndex != startLayerIndex)
                {
                    PublishTrace("SkillLayerAutoSelected", nextNode != null ? nextNode.NodeId : string.Empty, $"Selected layer {candidateLayerIndex} for the next cast.");
                }

                return true;
            }

            if (foundMatchingEntry && minBlockedCooldown < float.MaxValue)
            {
                _lastTriggerFailureReason = $"CooldownBlocked:{minBlockedCooldown:0.###}";
                PublishTrace("SkillCooldownBlocked", _skillConfig != null ? _skillConfig.SkillId : string.Empty, $"No layer is ready. Earliest remaining cooldown is {minBlockedCooldown:0.###}s.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 查找某个 layer 中从入口节点出发、能被当前输入命中的第一条跳转。
        /// </summary>
        /// <param name="layer">候选 layer。</param>
        /// <param name="eventType">当前输入事件类型。</param>
        /// <param name="eventArgument">当前输入事件参数。</param>
        /// <param name="nextNode">命中的目标节点。</param>
        /// <param name="metaSkillConfig">目标节点绑定的 MetaSkill 配置。</param>
        /// <returns>是否找到可进入的入口跳转。</returns>
        private bool TryGetEntryTransition(
            SkillLayerConfig layer,
            SkillEventType eventType,
            string eventArgument,
            out MetaSkillNodeConfig nextNode,
            out MetaSkillConfig metaSkillConfig)
        {
            nextNode = null;
            metaSkillConfig = null;
            if (layer == null || layer.SkillEvents == null)
            {
                return false;
            }

            for (int i = 0; i < layer.SkillEvents.Count; i++)
            {
                SkillEventConfig skillEvent = layer.SkillEvents[i];
                if (skillEvent == null ||
                    skillEvent.FromNodeId != EntryNodeId ||
                    !EvaluateSkillEvent(skillEvent, eventType, eventArgument) ||
                    skillEvent.ToNodeId == ExitNodeId)
                {
                    continue;
                }

                nextNode = FindNode(layer, skillEvent.ToNodeId);
                if (nextNode == null)
                {
                    _lastTriggerFailureReason = $"TargetNodeMissing:{skillEvent.ToNodeId}";
                    return false;
                }

                if (!TryGetMetaSkillConfig(nextNode, out metaSkillConfig))
                {
                    _lastTriggerFailureReason = $"MetaSkillConfigMissing:{nextNode.MetaSkillAssetName}";
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 判断退出当前技能时是否应推进到下一层。
        /// </summary>
        /// <param name="eventType">触发退出的事件类型。</param>
        /// <returns>正常完成 MetaSkill 后由 OnMetaSkillEnd 触发退出时返回 true。</returns>
        private bool ShouldAdvanceLayerOnExit(SkillEventType eventType)
        {
            return _currentNode != null &&
                   eventType == SkillEventType.OnMetaSkillEnd &&
                   _context.SkillFlowContext != null &&
                   _context.SkillFlowContext.LastEndReason == SkillMetaEndReason.Normal;
        }

        /// <summary>
        /// 同步 layer 冷却数组长度，确保它与技能配置中的 layer 数量一致。
        /// </summary>
        /// //okita:不明白为啥这样写
        private void EnsureLayerCooldownState()
        {
            int layerCount = _skillConfig != null && _skillConfig.Layers != null
                ? _skillConfig.Layers.Count
                : 0;

            while (_layerCooldownStartTimes.Count < layerCount)
            {
                _layerCooldownStartTimes.Add(-1f);
            }

            if (_layerCooldownStartTimes.Count > layerCount)
            {
                _layerCooldownStartTimes.RemoveRange(layerCount, _layerCooldownStartTimes.Count - layerCount);
            }

            _activeLayerIndex = NormalizeLayerIndex(_activeLayerIndex);
        }

        /// <summary>
        /// 将 layer 下标限制在当前技能配置的有效范围内。
        /// </summary>
        /// <param name="layerIndex">原始 layer 下标。</param>
        /// <returns>安全的 layer 下标；没有 layer 时返回 0。</returns>
        private int NormalizeLayerIndex(int layerIndex)
        {
            if (_skillConfig == null || _skillConfig.Layers == null || _skillConfig.Layers.Count == 0)
            {
                return 0;
            }

            return Mathf.Clamp(layerIndex, 0, _skillConfig.Layers.Count - 1);
        }

        /// <summary>
        /// 获取用于快照和冷却展示的安全 layer 下标。
        /// </summary>
        /// <returns>当前活动 layer 的规范化下标。</returns>
        private int GetSnapshotLayerIndex()
        {
            return NormalizeLayerIndex(_activeLayerIndex);
        }

        /// <summary>
        /// 记录指定 layer 的冷却开始时间。
        /// </summary>
        /// <param name="layerIndex">要设置冷却的 layer 下标。</param>
        /// <param name="startTime">冷却开始的 Time.time 时间点。</param>
        private void SetLayerCooldownStartTime(int layerIndex, float startTime)
        {
            EnsureLayerCooldownState();
            _layerCooldownStartTimes[NormalizeLayerIndex(layerIndex)] = startTime;
        }

        /// <summary>
        /// 判断当前节点结束后是否存在手动输入触发的后续连段。
        /// </summary>
        /// <param name="node">当前节点。</param>
        /// <returns>存在非 OnMetaSkillEnd 的后续事件边时返回 true。</returns>
        private bool CanAwaitManualContinuation(MetaSkillNodeConfig node)
        {
            if (node == null || _currentLayer == null || _currentLayer.SkillEvents == null)
            {
                return false;
            }

            for (int i = 0; i < _currentLayer.SkillEvents.Count; i++)
            {
                SkillEventConfig skillEvent = _currentLayer.SkillEvents[i];
                if (skillEvent == null || skillEvent.FromNodeId != node.NodeId || skillEvent.Events == null)
                {
                    continue;
                }

                for (int eventIndex = 0; eventIndex < skillEvent.Events.Count; eventIndex++)
                {
                    SkillEventEntryConfig entry = skillEvent.Events[eventIndex];
                    if (entry != null && entry.EventType != SkillEventType.OnMetaSkillEnd)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 判断当前输入是否应该被视为同技能自打断并阻止。
        /// </summary>
        /// <param name="eventType">当前输入事件类型。</param>
        /// <returns>当前 MetaSkill 尚不可被同技能输入打断时返回 true。</returns>
        /// okita:这里感觉写的太复杂：
        private bool ShouldBlockSelfInterrupt(SkillEventType eventType)
        {
            if (!IsSelfInputEvent(eventType))
            {
                return false;
            }

            if ((eventType == SkillEventType.CastSkillShort || eventType == SkillEventType.CastSkillLong) &&
                IsManualContinuationInputAllowed())
            {
                return false;
            }

            // Execute 阶段继续阻止普通自打断；Recovery 阶段允许输入进入技能门禁，
            // 最终由 StateController 的单位级 RecoveryCancelPolicy 决定是否接受。
            SkillFlowContext flowContext = _context.SkillFlowContext;
            if (flowContext != null && flowContext.IsStateDriven)
            {
                if (flowContext.ActivePhaseRole == SkillStatePhaseRole.Recovery &&
                    _context.StateController != null &&
                    !_context.StateController.CanRequestSkillTransition(flowContext.ActiveStateLayer))
                {
                    return true;
                }

                return flowContext.IsAwaitingStateCompletion &&
                       flowContext.ActivePhaseRole == SkillStatePhaseRole.Execute;
            }

            return _currentMetaSkillRuntime != null && !_currentMetaSkillRuntime.IsCompleted;
        }

        /// <summary>
        /// 判断当前是否允许手动连段输入穿过自打断保护。
        /// </summary>
        /// <returns>continuation 窗口开启且不在等待状态完成时返回 true。</returns>
        /// okita:没看懂，这个flowcontext是啥，需求很简单：metaskill的skillstate没结束就不能被打断
        private bool IsManualContinuationInputAllowed()
        {
            if (!_isAwaitingContinuation)
            {
                return false;
            }

            SkillFlowContext flowContext = _context.SkillFlowContext;
            if (flowContext == null)
            {
                return true;
            }

            return flowContext.IsContinuationWindowOpen && !flowContext.IsAwaitingStateCompletion;
        }

        /// <summary>
        /// 判断事件类型是否属于本技能槽位输入。
        /// </summary>
        /// <param name="eventType">事件类型。</param>
        /// <returns>短按、长按、按下、抬起、按住或施法中输入返回 true。</returns>
        private static bool IsSelfInputEvent(SkillEventType eventType)
        {
            switch (eventType)
            {
                case SkillEventType.CastSkillShort:
                case SkillEventType.CastSkillLong:
                case SkillEventType.PressSkillSlot:
                case SkillEventType.ReleaseSkillSlot:
                case SkillEventType.HoldSkillSlot:
                case SkillEventType.CastingSkill:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 判断 continuation 等待窗口是否已经超时。
        /// </summary>
        /// <returns>配置了超时时间且当前等待时间超过限制时返回 true。</returns>
        private bool HasContinuationTimedOut()
        {
            float timeout = _skillConfig != null ? Mathf.Max(0f, _skillConfig.ComboContinuationTimeout) : 0f;
            return timeout > 0f &&
                   _continuationAwaitStartTime >= 0f &&
                   Time.time - _continuationAwaitStartTime >= timeout;
        }

        /// <summary>
        /// 解析显式进入施法时使用的起始节点。
        /// </summary>
        /// <param name="layer">目标 layer。</param>
        /// <param name="startNodeId">指定起始节点 Id；为空或找不到时使用第一个节点。</param>
        /// <returns>解析到的起始节点；layer 无节点时返回 null。</returns>
        private static MetaSkillNodeConfig ResolveStartNode(SkillLayerConfig layer, string startNodeId)
        {
            if (layer == null || layer.MetaSkillNodes == null || layer.MetaSkillNodes.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(startNodeId))
            {
                MetaSkillNodeConfig node = FindNode(layer, startNodeId);
                if (node != null)
                {
                    return node;
                }
            }

            return layer.MetaSkillNodes[0];
        }

        /// <summary>
        /// 在指定 layer 中按节点 Id 查找技能图节点。
        /// </summary>
        /// <param name="layer">要查找的 layer。</param>
        /// <param name="nodeId">目标节点 Id。</param>
        /// <returns>找到的节点；未找到时返回 null。</returns>
        private static MetaSkillNodeConfig FindNode(SkillLayerConfig layer, string nodeId)
        {
            if (layer == null || layer.MetaSkillNodes == null)
            {
                return null;
            }

            for (int i = 0; i < layer.MetaSkillNodes.Count; i++)
            {
                MetaSkillNodeConfig node = layer.MetaSkillNodes[i];
                if (node != null && node.NodeId == nodeId)
                {
                    return node;
                }
            }

            return null;
        }

        /// <summary>
        /// 从任意来源对象解析角色动画控制器。
        /// </summary>
        /// <param name="source">可能是 GameObject 或 Component 的来源对象。</param>
        /// <returns>找到的 ICharacterAnimationController；没有时返回 null。</returns>
        private static ICharacterAnimationController ResolveCharacterAnimationController(object source)
        {
            if (source is GameObject gameObject)
            {
                return ResolveCharacterAnimationController(gameObject);
            }

            if (source is Component component)
            {
                return ResolveCharacterAnimationController(component.gameObject);
            }

            return null;
        }

        /// <summary>
        /// 从 GameObject 上查找实现 ICharacterAnimationController 的组件。
        /// </summary>
        /// <param name="gameObject">要查找的 GameObject。</param>
        /// <returns>第一个实现 ICharacterAnimationController 的 MonoBehaviour；没有时返回 null。</returns>
        private static ICharacterAnimationController ResolveCharacterAnimationController(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return null;
            }

            MonoBehaviour[] behaviours = gameObject.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ICharacterAnimationController controller)
                {
                    return controller;
                }
            }

            return null;
        }
    }
}
