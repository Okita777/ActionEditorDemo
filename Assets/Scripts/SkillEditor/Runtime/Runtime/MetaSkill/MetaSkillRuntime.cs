using ActionEditor.TagSystem;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    /// <summary>
    /// MetaSkill 的最小运行时单元。
    ///
    /// 它只负责：
    /// 1. 维护当前 MetaSkill 的上下文归属。
    /// 2. 进入时执行 OnAddEffect。
    /// 3. execute state 正常结束时执行结束效果。
    /// 4. 结束时清理标签和上下文关联。
    /// </summary>
    public sealed class MetaSkillRuntime
    {
        private const string MetaSkillTagSourceId = "MetaSkillRuntime.ConfigTags";

        private readonly MetaSkillConfig _config;
        private readonly SkillContext _context;

        /// <summary>
        /// [AICode] 标记当前 metaskill 是否由状态系统驱动 execute state 生命周期。
        /// </summary>
        private readonly bool _isStateDriven;

        /// <summary>
        /// [AICode] 标记当前 metaskill 是否已经完成最终收口。
        /// </summary>
        private bool _isCompleted;

        /// <summary>
        /// [AICode] 标记 execute state 正常结束效果是否已经执行过，避免重复触发。
        /// </summary>
        private bool _didApplyExecuteStateEndEffect;

        public MetaSkillRuntime(MetaSkillConfig config, SkillContext context)
        {
            _config = config;
            _context = context ?? new SkillContext();
            _isStateDriven = config != null && config.HasSkillState && _context.StateController != null;
        }

        public MetaSkillConfig Config => _config;

        /// <summary>
        /// [AICode] 收缩后只保留完成态语义，SkillRuntime 用它判断 metaskill 是否已结束。
        /// </summary>
        public bool IsCompleted => _isCompleted;

        /// <summary>
        /// [AICode] 仅为兼容 SkillRuntime 的调试时间读取而保留；状态驱动路径统一由 StateController 提供时间。
        /// </summary>
        public float TimelineTime => 0f;

        public void Enter()
        {
            _isCompleted = false;
            _didApplyExecuteStateEndEffect = false;

            _context.CurrentMetaSkillConfig = _config;
            _context.CurrentStateConfig = _isStateDriven && _config != null ? _config.SkillStateTimeLineState : null;
            _context.CurrentMetaSkillContext = CreateMetaSkillContext();
            ApplyTags(_context.TagQueryService, _config, _config != null ? _config.Tags : null, MetaSkillTagSourceId);

            if (_config != null && _config.OnAddEffect != null)
            {
                ExecuteEffect(_config.OnAddEffect);
            }

            EmitTrace(
                "MetaSkillEnter",
                _config != null ? _config.MetaSkillId : string.Empty,
                _isStateDriven ? "State-driven MetaSkill entered." : "MetaSkill entered.");
        }

        /// <summary>
        /// [AICode] 收缩后 Tick 不再推进 execute/recovery 阶段；生命周期只由 SkillRuntime 和 StateController 驱动。
        /// </summary>
        public void Tick(float deltaTime)
        {
        }

        /// <summary>
        /// [AICode] 外部普通退出入口。
        /// 非状态驱动路径会把 applyExecuteStateEndEffect 视为是否执行 OnEndEffect；
        /// 状态驱动路径只允许由 execute state 正常结束时显式触发结束效果。
        /// </summary>
        public void Exit(bool applyExecuteStateEndEffect = false)
        {
            FinalizeMetaSkill(interrupted: false, applyExecuteStateEndEffect: applyExecuteStateEndEffect);
        }

        /// <summary>
        /// [AICode] execute state 正常结束时由 SkillRuntime 显式调用。
        /// </summary>
        public void CompleteExecuteStateNormally()
        {
            FinalizeMetaSkill(interrupted: false, applyExecuteStateEndEffect: true);
        }

        /// <summary>
        /// [AICode] execute state 被打断时由 SkillRuntime 显式调用。
        /// </summary>
        public void InterruptExecuteState()
        {
            FinalizeMetaSkill(interrupted: true, applyExecuteStateEndEffect: false);
        }

        /// <summary>
        /// [AICode] 统一 metaskill 收口逻辑。
        /// 它不再接管任何状态推进或状态切换，只负责效果结算、标签移除和上下文清理。
        /// </summary>
        private void FinalizeMetaSkill(bool interrupted, bool applyExecuteStateEndEffect)
        {
            if (_isCompleted)
            {
                return;
            }

            if (applyExecuteStateEndEffect && !_didApplyExecuteStateEndEffect && _config != null && _config.OnEndEffect != null)
            {
                ExecuteEffect(_config.OnEndEffect);
                _didApplyExecuteStateEndEffect = true;
            }

            EmitTrace(
                interrupted ? "MetaSkillInterrupted" : "MetaSkillExit",
                _config != null ? _config.MetaSkillId : string.Empty,
                interrupted ? "MetaSkill interrupted." : "MetaSkill completed.");

            RemoveTags(_context.TagQueryService, _config, _config != null ? _config.Tags : null, MetaSkillTagSourceId);

            _isCompleted = true;
            CommitMetaSkillContext();
            _context.CurrentMetaSkillConfig = null;
            _context.CurrentStateConfig = _context.StateController != null ? _context.StateController.GetCurrentState(StateLayerType.Action) : null;
        }

        private MetaSkillContext CreateMetaSkillContext()
        {
            return new MetaSkillContext
            {
                SkillRuntimeId = _context.SkillFlowContext != null ? _context.SkillFlowContext.SkillRuntimeId : string.Empty,
                SkillId = _context.SkillConfig != null ? _context.SkillConfig.SkillId : string.Empty,
                MetaSkillId = _config != null ? _config.MetaSkillId : string.Empty,
                MetaSkillNodeId = _context.SkillFlowContext != null ? _context.SkillFlowContext.CurrentNodeId : string.Empty,
                PhaseRole = _isStateDriven ? SkillStatePhaseRole.Execute : SkillStatePhaseRole.None,
                Caster = _context.Caster,
                PrimaryTarget = _context.PrimaryTarget,
                Succeeded = true,
            };
        }

        private void CommitMetaSkillContext()
        {
            MetaSkillContext currentMetaSkillContext = _context.CurrentMetaSkillContext;
            if (currentMetaSkillContext == null)
            {
                return;
            }

            _context.LastMetaSkillContext = currentMetaSkillContext;
            _context.CurrentMetaSkillContext = null;
        }

        private void ExecuteEffect(SkillEffectConfig effectConfig)
        {
            if (_context.EffectExecutor == null || effectConfig == null)
            {
                return;
            }

            _context.EffectExecutor.Execute(effectConfig, _context);
        }

        private void EmitTrace(string traceType, string payloadId, string message)
        {
            SkillRuntimeDebugBus.PublishTrace(_context, new SkillRuntimeTraceEvent
            {
                TraceType = traceType,
                MetaSkillId = _config != null ? _config.MetaSkillId : string.Empty,
                PayloadId = payloadId,
                Time = _context != null ? _context.DebugTimelineTime : 0f,
                Message = message,
            });
        }

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
    }
}
