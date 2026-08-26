using System;
using System.Collections.Generic;
using System.Linq;
using ActionEditor.TagSystem;
using ActionEditor.InputSystem;
using ActionEditor.CameraSystem;
using SkillEditor.Preview;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    /// <summary>
    /// 一个输入动作与一个主动技能槽位之间的序列化绑定。
    /// 运行时会根据这条绑定，把输入事件转发给对应槽位上的技能实例。
    /// </summary>
    [Serializable]
    public sealed class SkillSlotBinding
    {
        /// <summary>
        /// 逻辑槽位编号。
        /// 运行时会把它拼成诸如 slot:1 的事件参数，再传给 SkillRuntime。
        /// </summary>
        public int SlotIndex = 1;

        /// <summary>
        /// 从 CharacterInputDriver 中读取的输入动作名。
        /// </summary>
        public string ActionName = string.Empty;

        /// <summary>
        /// 用于加载 SkillConfig 的技能资源名。
        /// </summary>
        public string SkillAssetName = string.Empty;
    }

    /// <summary>
    /// 单位级别的技能运行时总入口。
    ///
    /// 它负责：
    /// 1. 加载主动技能和被动技能。
    /// 2. 合并运行时动态授予的技能。
    /// 3. 构建单位共享的 StateController。
    /// 4. 把输入事件转发给 SkillRuntime。
    /// 5. 每帧推进技能层和状态层的运行。
    /// </summary>
    [Serializable]
    public sealed class SkillPlayerController
    {
        /// <summary>
        /// 默认主目标。
        /// 在每帧 Tick 和事件派发之前，会先写入各个 SkillContext。
        /// </summary>
        [SerializeField] private GameUnit _primaryTarget;

        /// <summary>
        /// 长按判定阈值。
        /// 会同步给 CharacterInputDriver，用于区分短按和长按。
        /// </summary>
        [SerializeField] private float _longPressThreshold = 0.35f;

        /// <summary>
        /// 每个主动技能槽位对应一个运行时包装对象。
        /// 里面持有槽位绑定、SkillContext 和 SkillRuntime。
        /// </summary>
        private readonly List<SkillRuntimeState> _runtimeStates = new List<SkillRuntimeState>();

        /// <summary>
        /// 当前单位装配到运行时的被动技能列表。
        ///
        /// 这里的“状态”不是 StateController 里的状态，而是“当前拥有哪些被动技能”的运行时记录。
        /// 它目前主要承担装配、授予和后续被动逻辑扩展的承载作用。
        /// </summary>
        private readonly List<PassiveSkillState> _passiveStates = new List<PassiveSkillState>();

        private GameObject _owner;
        private CharacterInputDriver _characterInputDriver;

        /// <summary>
        /// 单位共享的状态机。
        /// 它既管理单位原本的状态，也管理技能注入的 execute/recovery 临时状态。
        /// </summary>
        private StateController _stateController;

        /// <summary>
        /// 状态机共享上下文。
        /// 供 StateController 和状态驱动的时间线执行共用。
        /// </summary>
        private SkillContext _stateContext;
        private UnitHitEventHub _unitHitEventHub;
        private IUnitHitStopService _hitStopService;
        private ICameraFeedbackService _cameraFeedbackService;
        private IVfxService _vfxService;
        private IAudioService _audioService;

        /// <summary>
        /// [AICode] 标记当前技能控制器是否已完成初始化，避免 BattleManager 和组件自身生命周期重复初始化。
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// [AICode] 暴露当前 SkillRuntime 引用到的 MetaSkill 集合，供 CharacterStateBuilder 统一拼装技能状态。
        /// </summary>
        public List<MetaSkillConfig> ReferencedMetaSkillConfigs
        {
            get
            {
                List<MetaSkillConfig> result = new List<MetaSkillConfig>();
                HashSet<MetaSkillConfig> visited = new HashSet<MetaSkillConfig>();
                for (int i = 0; i < _runtimeStates.Count; i++)
                {
                    SkillRuntimeState runtimeState = _runtimeStates[i];
                    if (runtimeState?.Runtime == null)
                    {
                        continue;
                    }

                    foreach (MetaSkillConfig metaSkillConfig in runtimeState.Runtime.MetaSkillConfigs)
                    {
                        if (metaSkillConfig != null && visited.Add(metaSkillConfig))
                        {
                            result.Add(metaSkillConfig);
                        }
                    }
                }

                return result;
            }
        }

        public GameUnit PrimaryTarget
        {
            get => _primaryTarget;
            set => _primaryTarget = value;
        }

        public float LongPressThreshold
        {
            get => _longPressThreshold;
            set => _longPressThreshold = Mathf.Max(0f, value);
        }

        /// <summary>
        /// 当前单位在 Reload 期间构建出来的共享状态机。
        /// </summary>
        public StateController StateController => _stateController;

        /// <summary>
        /// [AICode] 由 CharacterBattleManager 注入宿主对象，SkillPlayerController 自身不再依赖 MonoBehaviour 生命周期。
        /// </summary>
        public void Bind(GameObject owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// [AICode] 供 CharacterBattleManager 或组件自身调用的统一初始化入口。
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            if (_owner == null)
            {
                Debug.LogError("SkillPlayerController.Initialize: owner is not bound.");
                return;
            }

            Reload();
            _isInitialized = true;
        }

        /// <summary>
        /// [AICode] 只推进技能运行时；状态机推进由 CharacterBattleManager 独立调度。
        /// </summary>
        public void TickSkillRuntimes(float deltaTime)
        {
            bool canAdvanceRuntime = deltaTime > 0f;
            for (int i = 0; i < _runtimeStates.Count; i++)
            {
                SkillRuntimeState state = _runtimeStates[i];
                if (state == null || state.Runtime == null || state.Binding == null)
                {
                    continue;
                }

                state.Context.PrimaryTarget = _primaryTarget;
                if (canAdvanceRuntime)
                {
                    state.Runtime.Tick(deltaTime);
                }

                if (canAdvanceRuntime)
                {
                    ProcessSkillBindingInput(state);
                }
            }

            if (_stateContext != null)
            {
                _stateContext.PrimaryTarget = _primaryTarget;
            }
        }

        /// <summary>
        /// [AICode] 供 BattleManager 或组件自身调用的统一清理入口。
        /// </summary>
        public void Shutdown()
        {
            for (int i = 0; i < _runtimeStates.Count; i++)
            {
                _runtimeStates[i]?.Runtime?.ExitCasting();
            }

            _stateContext?.CharacterAnimationController?.StopAllStateAnimations(_stateContext);
            _isInitialized = false;
        }

        /// <summary>
        /// 重新构建当前单位的技能运行时视图。
        ///
        /// 处理顺序是：
        /// 1. 清理旧的技能运行时实例。
        /// 2. 从 GameUnit 读取技能槽位。
        /// 3. 合并运行时授予技能。
        /// 4. 重建共享 StateController。
        /// </summary>
        public void Reload()
        {
            if (_owner == null)
            {
                Debug.LogError("SkillPlayerController.Reload: owner is not bound.");
                return;
            }

            ExitAllRuntimeStates();
            _runtimeStates.Clear();
            _passiveStates.Clear();
            _characterInputDriver = ResolveCharacterInputDriver();
            _stateController = null;
            _stateContext = null;

            GameUnit unit = GetComponent<GameUnit>() ?? GetComponentInChildren<GameUnit>(true);
            if (unit == null)
            {
                Debug.LogError("SkillPlayerController.Reload: GameUnit is missing on owner.", _owner);
                return;
            }

            string unitId = unit.UnitId;
            ResolveFeedbackServices(unit);
            BuildActiveRuntimeStates(unit);
            BuildPassiveSkillStates(unit);
            LogDebug($"SkillPlayerController.Reload: source=GameUnit, activeSlots={unit.ActiveSkillSlots?.Count ?? 0}, passiveSlots={unit.PassiveSkillSlots?.Count ?? 0}, runtimeStates={_runtimeStates.Count}, passiveStates={_passiveStates.Count}.");

            BuildStateController(unitId);
        }

        /// <summary>
        /// 通过共享状态机尝试执行一次普通状态切换。
        /// </summary>
        /// <param name="targetStateId">目标状态 Id。只有满足切换规则时才会进入。</param>
        public bool TryChangeState(string targetStateId)
        {
            return _stateController != null && _stateController.TryChangeState(targetStateId);
        }

        /// <summary>
        /// 强制切换到指定状态，并可指定从目标状态时间线的某个时间点开始。
        /// </summary>
        public bool ForceChangeState(string targetStateId, float startTime = 0f)
        {
            return _stateController != null && _stateController.ForceChangeState(targetStateId, startTime);
        }

        /// <summary>
        /// 根据 `GameUnit.ActiveSkillSlots` 构建主动技能运行时。
        /// </summary>
        /// <param name="unit">当前单位配置。</param>
        private void BuildActiveRuntimeStates(GameUnit unit)
        {
            if (unit == null || unit.ActiveSkillSlots == null)
            {
                return;
            }

            for (int i = 0; i < unit.ActiveSkillSlots.Count; i++)
            {
                PreviewActiveSkillSlotConfig slot = unit.ActiveSkillSlots[i];
                if (slot == null)
                {
                    continue;
                }

                SkillSlotBinding binding = new SkillSlotBinding
                {
                    SlotIndex = Mathf.Max(1, slot.SlotIndex),
                    ActionName = slot.ActionName,
                    SkillAssetName = slot.SkillAssetName,
                };
                TryAddRuntimeState(slot.SkillAssetName, binding, SkillCastCategory.Active);
            }
        }

        /// <summary>
        /// 根据 `GameUnit.PassiveSkillSlots` 构建被动技能装配视图。
        ///
        /// 注意这里目前主要是“装配记录”，并不等于已经有完整的被动技能执行流。
        /// </summary>
        /// <param name="unit">当前单位配置。</param>
        private void BuildPassiveSkillStates(GameUnit unit)
        {
            if (unit == null || unit.PassiveSkillSlots == null)
            {
                return;
            }

            for (int i = 0; i < unit.PassiveSkillSlots.Count; i++)
            {
                PreviewPassiveSkillSlotConfig slot = unit.PassiveSkillSlots[i];
                if (slot == null || string.IsNullOrEmpty(slot.SkillAssetName))
                {
                    continue;
                }

                SkillConfig skillConfig = null;
                if (!SkillRuntimeLoadData.Instance.LoadSkill(slot.SkillAssetName, config => skillConfig = config) || skillConfig == null)
                {
                    Debug.LogError($"SkillPlayerController: failed to load passive Skill '{slot.SkillAssetName}'.", _owner);
                    continue;
                }

                if (skillConfig.SkillCategory != SkillCastCategory.Passive)
                {
                    Debug.LogWarning($"SkillPlayerController: Skill '{slot.SkillAssetName}' is equipped in a passive slot but marked as {skillConfig.SkillCategory}.", _owner);
                }

                _passiveStates.Add(new PassiveSkillState
                {
                    SlotIndex = Mathf.Max(1, slot.SlotIndex),
                    DisplayName = slot.DisplayName,
                    SkillAssetName = slot.SkillAssetName,
                    Config = skillConfig,
                });
            }
        }

        /// <summary>
        /// 为一个主动技能槽位创建完整的运行时包装对象。
        ///
        /// 它会负责：
        /// 1. 加载 SkillConfig。
        /// 2. 加载该 Skill 引用到的全部 MetaSkillConfig。
        /// 3. 构建 SkillContext。
        /// 4. 创建 `SkillRuntime` 并加入 `_runtimeStates`。
        /// </summary>
        /// <param name="skillAssetName">要加载的技能资源名。</param>
        /// <param name="binding">槽位绑定信息，包含动作名和槽位编号。</param>
        /// <param name="expectedCategory">期望的技能类别，用于校验主动/被动槽位是否装错类型。</param>
        /// <param name="displayName">运行时显示名；为空时可回退使用资源内名字。</param>
        private void TryAddRuntimeState(string skillAssetName, SkillSlotBinding binding, SkillCastCategory expectedCategory, string displayName = "")
        {
            if (binding == null || string.IsNullOrEmpty(skillAssetName))
            {
                LogDebug("SkillPlayerController: skipped empty slot binding.");
                return;
            }

            SkillConfig skillConfig = null;
            if (!SkillRuntimeLoadData.Instance.LoadSkill(skillAssetName, config => skillConfig = config) || skillConfig == null)
            {
                Debug.LogError($"SkillPlayerController: failed to load Skill '{skillAssetName}' for slot={binding.SlotIndex}, action='{binding.ActionName}', expectedCategory={expectedCategory}.", _owner);
                return;
            }

            if (skillConfig.SkillCategory != expectedCategory)
            {
                Debug.LogWarning($"SkillPlayerController: Skill '{skillAssetName}' is equipped in a {expectedCategory} slot but marked as {skillConfig.SkillCategory}.", _owner);
            }

            Dictionary<string, MetaSkillConfig> metaSkillConfigs = LoadMetaSkillConfigs(skillConfig);
            GameUnit ownerUnit = GetComponent<GameUnit>() ?? GetComponentInChildren<GameUnit>(true);
            SkillContext context = new SkillContext
            {
                Caster = ownerUnit,
                PrimaryTarget = _primaryTarget,
                CombatResolver = ResolveBattleResolver(),
                ResourceService = ResolveResourceService(),
                TagQueryService = ResolveTagQueryService(),
                BuffService = ResolveBuffService(),
                CharacterAnimationController = ResolveCharacterAnimationController(),
                UnitHitEventSource = _unitHitEventHub,
                UnitHitEventPublisher = _unitHitEventHub,
                HitStopService = _hitStopService,
                CameraFeedbackService = _cameraFeedbackService,
                VfxService = _vfxService,
                AudioService = _audioService,
                StateInputSnapshotProvider = CaptureStateInputSnapshot,
                StateHitSnapshotProvider = CaptureStateHitSnapshot,
                StateBeHitSnapshotProvider = CaptureStateBeHitSnapshot,
                BreakValueProvider = CaptureBreakValue,
            };

            _runtimeStates.Add(new SkillRuntimeState
            {
                Binding = binding,
                Context = context,
                Runtime = new SkillRuntime(skillConfig, metaSkillConfigs, context),
                DisplayName = displayName ?? string.Empty,
            });

            LogDebug($"SkillPlayerController: runtime state added, slot={binding.SlotIndex}, action='{binding.ActionName}', skill='{skillAssetName}', metaSkillCount={metaSkillConfigs.Count}.");
        }

        /// <summary>
        /// 为当前单位构建共享状态机。
        ///
        /// 这里会把两类状态合并起来：
        /// 1. 单位原本配置的普通状态。
        /// 2. 技能 MetaSkill 携带的 execute/recovery 临时状态。
        /// </summary>
        /// <param name="unitId">当前单位 Id，用于加载 UnitConfig 和 StateConfig。</param>
        private void BuildStateController(string unitId)
        {
            CharacterStateBuildResult buildResult = CharacterStateBuilder.Build(
                unitId,
                ReferencedMetaSkillConfigs,
                _owner,
                _primaryTarget,
                CaptureStateInputSnapshot,
                CaptureStateHitSnapshot,
                CaptureStateBeHitSnapshot,
                CaptureBreakValue,
                ResolveBattleResolver(),
                ResolveResourceService(),
                ResolveTagQueryService(),
                ResolveBuffService(),
                ResolveCharacterAnimationController());

            if (buildResult == null || buildResult.StateController == null || buildResult.StateContext == null)
            {
                return;
            }

            _stateContext = buildResult.StateContext;
            _stateController = buildResult.StateController;
            _stateContext.UnitHitEventSource = _unitHitEventHub;
            _stateContext.UnitHitEventPublisher = _unitHitEventHub;
            _stateContext.HitStopService = _hitStopService;
            _stateContext.CameraFeedbackService = _cameraFeedbackService;
            _stateContext.VfxService = _vfxService;
            _stateContext.AudioService = _audioService;

            for (int i = 0; i < _runtimeStates.Count; i++)
            {
                if (_runtimeStates[i] == null || _runtimeStates[i].Context == null)
                {
                    continue;
                }

                _runtimeStates[i].Context.StateController = _stateController;
                _runtimeStates[i].Context.CurrentStateConfig = _stateController.GetCurrentState(StateLayerType.Action);
                _runtimeStates[i].Context.CharacterAnimationController ??= _stateContext.CharacterAnimationController;
                _runtimeStates[i].Context.UnitHitEventSource = _unitHitEventHub;
                _runtimeStates[i].Context.UnitHitEventPublisher = _unitHitEventHub;
                _runtimeStates[i].Context.HitStopService = _hitStopService;
                _runtimeStates[i].Context.CameraFeedbackService = _cameraFeedbackService;
                _runtimeStates[i].Context.VfxService = _vfxService;
                _runtimeStates[i].Context.AudioService = _audioService;
            }
        }

        private void ResolveFeedbackServices(GameUnit unit)
        {
            if (unit == null)
            {
                _unitHitEventHub = null;
                _hitStopService = null;
                _cameraFeedbackService = null;
                _vfxService = null;
                _audioService = null;
                return;
            }

            GameObject root = unit.UnitObject != null ? unit.UnitObject : unit.gameObject;
            _unitHitEventHub = root.GetComponent<UnitHitEventHub>() ??
                root.GetComponentInParent<UnitHitEventHub>(true) ??
                root.GetComponentInChildren<UnitHitEventHub>(true);
            _unitHitEventHub ??= root.AddComponent<UnitHitEventHub>();
            _hitStopService = UnitHitStopService.ResolveOrCreate(unit);
            _cameraFeedbackService = CameraFeedbackService.ResolveForLocalPlayer(unit);
            GameFeedbackServiceHost feedbackHost = GameFeedbackServiceHost.ResolveOrCreate();
            _vfxService = feedbackHost.Vfx;
            _audioService = feedbackHost.Audio;
        }

        /// <summary>
        /// 扫描一个 SkillConfig 中所有 layer/node，加载其引用到的 MetaSkillConfig。
        ///
        /// 返回结果以 MetaSkill 资源名为键，避免重复加载同一个 MetaSkill。
        /// </summary>
        /// <param name="skillConfig">要扫描的技能配置。</param>
        private Dictionary<string, MetaSkillConfig> LoadMetaSkillConfigs(SkillConfig skillConfig)
        {
            Dictionary<string, MetaSkillConfig> result = new Dictionary<string, MetaSkillConfig>();
            if (skillConfig == null || skillConfig.Layers == null)
            {
                return result;
            }

            for (int layerIndex = 0; layerIndex < skillConfig.Layers.Count; layerIndex++)
            {
                SkillLayerConfig layer = skillConfig.Layers[layerIndex];
                if (layer == null || layer.MetaSkillNodes == null)
                {
                    continue;
                }

                for (int nodeIndex = 0; nodeIndex < layer.MetaSkillNodes.Count; nodeIndex++)
                {
                    MetaSkillNodeConfig node = layer.MetaSkillNodes[nodeIndex];
                    if (node == null || string.IsNullOrEmpty(node.MetaSkillAssetName) || result.ContainsKey(node.MetaSkillAssetName))
                    {
                        continue;
                    }

                    MetaSkillConfig loadedConfig = null;
                    if (SkillRuntimeLoadData.Instance.LoadMetaSkill(node.MetaSkillAssetName, config => loadedConfig = config) && loadedConfig != null)
                    {
                        result.Add(node.MetaSkillAssetName, loadedConfig);
                    }
                    else
                    {
                        Debug.LogError($"SkillPlayerController: failed to load MetaSkill '{node.MetaSkillAssetName}'.", _owner);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 解析战斗结算服务。
        /// 优先复用角色身上已有的解析器；若没有，则回退到默认实现。
        /// </summary>
        /// okita:直接创建，该类不应该继承monobehaviour
        private IBattleResolver ResolveBattleResolver()
        {
            // 优先复用角色身上已有的战斗解析器；如果没有，再回退到默认实现。
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IBattleResolver resolver)
                {
                    return resolver;
                }
            }

            return new SkillBattleResolver();
        }

        /// <summary>
        /// 解析技能资源服务，用于资源检查和消耗。
        /// </summary>
        private ISkillResourceService ResolveResourceService()
        {
            return SkillResourceServiceUtility.Resolve(_owner);
        }

        /// <summary>
        /// 创建标签查询服务。
        /// </summary>
        private static ITagQueryService ResolveTagQueryService()
        {
            return new TagRuntimeService();
        }

        /// <summary>
        /// 创建 Buff 服务。
        /// </summary>
        private static IBuffService ResolveBuffService()
        {
            return new CharacterBuffService();
        }

        /// <summary>
        /// 解析角色动画控制器实现。
        /// 角色侧可以通过挂接自定义组件来接入不同动画系统。
        /// </summary>
        /// okita:待检查
        private ICharacterAnimationController ResolveCharacterAnimationController()
        {
            // 这里通过组件扫描查找动画控制器实现，目的是允许角色侧用自定义 MonoBehaviour 接入动画系统。
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ICharacterAnimationController controller)
                {
                    return controller;
                }
            }

            return null;
        }

        /// <summary>
        /// 解析输入驱动，并把长按阈值同步给它。
        /// </summary>
        private CharacterInputDriver ResolveCharacterInputDriver()
        {
            CharacterInputDriver driver = GetComponent<CharacterInputDriver>() ?? GetComponentInChildren<CharacterInputDriver>(true);
            if (driver != null)
            {
                driver.LongPressThreshold = _longPressThreshold;
                driver.RebuildProvider();
            }

            return driver;
        }

        /// <summary>
        /// 处理一个主动技能槽位在本帧的输入分发。
        ///
        /// 这里会把输入系统里的动作阶段，转换成 SkillRuntime 可理解的 `SkillEventType`。
        /// </summary>
        /// <param name="state">当前槽位对应的技能运行时包装对象。</param>
        private void ProcessSkillBindingInput(SkillRuntimeState state)
        {
            if (state == null || state.Binding == null || state.Runtime == null)
            {
                return;
            }

            string actionName = ResolveSkillBindingActionName(state.Binding);
            if (_characterInputDriver == null || string.IsNullOrWhiteSpace(actionName) || !_characterInputDriver.HasActionBinding(actionName))
            {
                return;
            }

            string eventArgument = BuildSlotArgument(state.Binding.SlotIndex);

            if (_characterInputDriver.IsActionDown(actionName))
            {
                state.Runtime.Trigger(SkillEventType.PressSkillSlot, eventArgument);
            }

            if (_characterInputDriver.IsActionHoldTick(actionName))
            {
                state.Runtime.Trigger(SkillEventType.HoldSkillSlot, eventArgument);
            }

            if (_characterInputDriver.IsActionLongPressStarted(actionName))
            {
                TriggerSkillInputEvent(state, SkillEventType.CastSkillLong, eventArgument, actionName, "long-start");
            }

            if (_characterInputDriver.IsActionShortReleased(actionName))
            {
                TriggerSkillInputEvent(state, SkillEventType.CastSkillShort, eventArgument, actionName, "short-release");
            }

            if (_characterInputDriver.IsActionUp(actionName))
            {
                state.Runtime.Trigger(SkillEventType.ReleaseSkillSlot, eventArgument);
            }
        }

        /// <summary>
        /// 采集当前帧输入快照，供 StateController 的打断条件和状态判断使用。
        /// </summary>
        /// okita:后面加强理解
        private StateInputSnapshot CaptureStateInputSnapshot()
        {
            CharacterInputFrame inputFrame = _characterInputDriver != null ? _characterInputDriver.CurrentFrame : null;
            if (inputFrame != null)
            {
                StateInputSnapshot frameSnapshot = CreateActionStateSnapshot();
                frameSnapshot.IsMoveInput = inputFrame.HasMoveInput;
                frameSnapshot.IsMoveInputPre = inputFrame.HadMoveInputLastFrame;

                CopyActionNames(inputFrame.HeldActions, frameSnapshot.HeldActions);
                CopyActionNames(inputFrame.DownActions, frameSnapshot.DownActions);
                CopyActionNames(inputFrame.UpActions, frameSnapshot.UpActions);
                PopulateActionPhaseSet(inputFrame, frameSnapshot.ShortReleasedActions, state => state.WasShortReleasedThisFrame);
                PopulateActionPhaseSet(inputFrame, frameSnapshot.LongPressStartedActions, state => state.WasLongPressStartedThisFrame);
                PopulateActionPhaseSet(inputFrame, frameSnapshot.LongPressReleasedActions, state => state.WasLongPressReleasedThisFrame);

                return frameSnapshot;
            }

            return CreateActionStateSnapshot();
        }

        /// <summary>
        /// 解析槽位绑定里的动作名，并做空白裁剪。
        /// </summary>
        private static string ResolveSkillBindingActionName(SkillSlotBinding binding)
        {
            return binding != null ? (binding.ActionName ?? string.Empty).Trim() : string.Empty;
        }

        /// <summary>
        /// 把动作名集合复制到目标集合中，并自动过滤空字符串。
        /// </summary>
        private static void CopyActionNames(IEnumerable<string> source, HashSet<string> destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            foreach (string actionName in source)
            {
                if (!string.IsNullOrWhiteSpace(actionName))
                {
                    destination.Add(actionName);
                }
            }
        }

        /// <summary>
        /// 按指定动作阶段选择器，填充输入快照中的动作集合。
        /// </summary>
        /// <param name="inputFrame">当前输入帧。</param>
        /// <param name="destination">目标动作集合。</param>
        /// <param name="selector">动作状态筛选器，例如“本帧是否短按释放”。</param>
        private static void PopulateActionPhaseSet(CharacterInputFrame inputFrame, HashSet<string> destination, Func<CharacterInputActionState, bool> selector)
        {
            if (inputFrame == null || destination == null || selector == null)
            {
                return;
            }

            AddMatchingActionNames(inputFrame.DownActions, inputFrame, destination, selector);
            AddMatchingActionNames(inputFrame.HeldActions, inputFrame, destination, selector);
            AddMatchingActionNames(inputFrame.UpActions, inputFrame, destination, selector);
        }

        /// <summary>
        /// 在一组动作名中筛出满足指定阶段条件的动作，并加入目标集合。
        /// </summary>
        private static void AddMatchingActionNames(IEnumerable<string> actionNames, CharacterInputFrame inputFrame, HashSet<string> destination, Func<CharacterInputActionState, bool> selector)
        {
            if (actionNames == null)
            {
                return;
            }

            foreach (string actionName in actionNames)
            {
                if (!string.IsNullOrWhiteSpace(actionName) && inputFrame.TryGetActionState(actionName, out CharacterInputActionState actionState) && selector(actionState))
                {
                    destination.Add(actionName);
                }
            }
        }

        /// <summary>
        /// 创建一个空的输入快照对象，并初始化所有动作集合。
        /// </summary>
        private static StateInputSnapshot CreateActionStateSnapshot()
        {
            return new StateInputSnapshot
            {
                HeldActions = new HashSet<string>(StringComparer.Ordinal),
                DownActions = new HashSet<string>(StringComparer.Ordinal),
                UpActions = new HashSet<string>(StringComparer.Ordinal),
                ShortReleasedActions = new HashSet<string>(StringComparer.Ordinal),
                LongPressStartedActions = new HashSet<string>(StringComparer.Ordinal),
                LongPressReleasedActions = new HashSet<string>(StringComparer.Ordinal),
            };
        }

        /// <summary>
        /// 派发一个技能输入事件，并在调试模式下记录触发结果。
        /// </summary>
        /// <param name="state">当前槽位对应的技能运行时包装对象。</param>
        /// <param name="eventType">要发送给 SkillRuntime 的事件类型。</param>
        /// <param name="eventArgument">事件参数，通常包含槽位信息。</param>
        /// <param name="actionName">对应的输入动作名。</param>
        /// <param name="phase">输入阶段标记，仅用于调试日志。</param>
        private void TriggerSkillInputEvent(SkillRuntimeState state, SkillEventType eventType, string eventArgument, string actionName, string phase)
        {
            bool triggered = state.Runtime.Trigger(eventType, eventArgument);
             LogDebug($"SkillPlayerController: input {phase}, slot={state.Binding.SlotIndex}, action='{actionName}', event={eventType}, arg='{eventArgument}', triggered={triggered}, skill='{state.Binding.SkillAssetName}'.");
            if (!triggered)
            {
                Debug.LogWarning($"SkillPlayerController: skill '{state.Binding.SkillAssetName}' trigger failed for event {eventType} arg='{eventArgument}', reason='{state.Runtime.LastTriggerFailureReason}'.", _owner);
            }
        }

        /// <summary>
        /// 采集“本单位最近是否命中过目标”的状态快照。
        ///
        /// 当前实现通过各个 SkillContext 的最近一次 metaskill effect 结果反推命中结果。
        /// </summary>
        private StateHitSnapshot CaptureStateHitSnapshot()
        {
            for (int i = 0; i < _runtimeStates.Count; i++)
            {
                SkillContext context = _runtimeStates[i] != null ? _runtimeStates[i].Context : null;
                SkillEffectResult latestEffectResult = GetLatestEffectResult(context);
                GameUnit hitTarget = null;
                if (latestEffectResult != null && latestEffectResult.AffectedTargets != null && latestEffectResult.AffectedTargets.Count > 0)
                {
                    hitTarget = latestEffectResult.AffectedTargets[0];
                }

                if (context == null || latestEffectResult == null || !latestEffectResult.HasValue || !latestEffectResult.Succeeded || hitTarget == null)
                {
                    continue;
                }

                return new StateHitSnapshot
                {
                    HasHit = true,
                    HitTarget = hitTarget,
                };
            }

            return default(StateHitSnapshot);
        }

        private static SkillEffectResult GetLatestEffectResult(SkillContext context)
        {
            if (context == null)
            {
                return SkillEffectResult.None;
            }

            if (context.CurrentMetaSkillContext != null && context.CurrentMetaSkillContext.LastEffectContext != null)
            {
                return context.CurrentMetaSkillContext.LastEffectContext;
            }

            if (context.LastMetaSkillContext != null && context.LastMetaSkillContext.LastEffectContext != null)
            {
                return context.LastMetaSkillContext.LastEffectContext;
            }

            return SkillEffectResult.None;
        }

        /// <summary>
        /// 采集“本单位本帧是否受击”的状态快照。
        ///
        /// 当前尚未接入正式受击回流，因此先返回默认值。
        /// </summary>
        private StateBeHitSnapshot CaptureStateBeHitSnapshot()
        {
            return default(StateBeHitSnapshot);
        }

        /// <summary>
        /// 采集当前单位的 BreakValue，用于状态打断判定。
        /// </summary>
        private float CaptureBreakValue()
        {
            GameUnit unit = GetComponent<GameUnit>() ?? GetComponentInChildren<GameUnit>(true);
            if (unit != null)
            {
                return unit.GetAttribute(SkillAttributeType.BreakValue);
            }

            SkillAttributeSet attributes = GetComponent<SkillAttributeSet>() ?? GetComponentInChildren<SkillAttributeSet>(true);
            return attributes != null ? attributes.GetAttribute(SkillAttributeType.BreakValue) : 0f;
        }

        /// <summary>
        /// 根据槽位编号生成技能事件参数字符串，例如 `slot:1`。
        /// </summary>
        private static string BuildSlotArgument(int slotIndex)
        {
            return slotIndex > 0 ? $"slot:{slotIndex}" : string.Empty;
        }

        /// <summary>
        /// 让当前全部主动技能运行时退出施法状态。
        /// 一般用于 Reload 或组件停用前的清理。
        /// </summary>
        private void ExitAllRuntimeStates()
        {
            for (int i = 0; i < _runtimeStates.Count; i++)
            {
                _runtimeStates[i]?.Runtime?.ExitCasting();
            }
        }

        private static bool HasActiveSkillSlots(GameUnit unit)
        {
            return unit != null &&
                   unit.ActiveSkillSlots != null &&
                   unit.ActiveSkillSlots.Count > 0;
        }

        /// <summary>
        /// 判断当前单位是否配置了被动技能槽位。
        /// </summary>
        private static bool HasPassiveSkillSlots(GameUnit unit)
        {
            return unit != null &&
                   unit.PassiveSkillSlots != null &&
                   unit.PassiveSkillSlots.Count > 0;
        }

        /// <summary>
        /// 仅在编辑器和开发包中输出调试日志。
        /// </summary>
        private void LogDebug(string message)
        {
            Debug.Log(message, _owner);
        }

        /// <summary>
        /// [AICode] 统一从宿主对象读取组件，避免 SkillPlayerController 继续依赖 MonoBehaviour 继承。
        /// </summary>
        private T GetComponent<T>() where T : Component
        {
            return _owner != null ? _owner.GetComponent<T>() : null;
        }

        /// <summary>
        /// [AICode] 统一从宿主对象的子节点读取组件，维持原有装配方式。
        /// </summary>
        private T GetComponentInChildren<T>(bool includeInactive) where T : Component
        {
            return _owner != null ? _owner.GetComponentInChildren<T>(includeInactive) : null;
        }

        /// <summary>
        /// [AICode] 统一从宿主对象扫描组件，供接口解析逻辑复用。
        /// </summary>
        private T[] GetComponents<T>() where T : Component
        {
            return _owner != null ? _owner.GetComponents<T>() : Array.Empty<T>();
        }

        /// <summary>
        /// 一个主动技能槽位对应的完整运行时包装对象。
        /// 用来把槽位绑定、执行上下文和 SkillRuntime 组织在一起。
        /// </summary>
        private sealed class SkillRuntimeState
        {
            /// <summary>
            /// 槽位绑定信息，例如动作名和槽位编号。
            /// </summary>
            public SkillSlotBinding Binding;

            /// <summary>
            /// 该技能实例独享的执行上下文。
            /// </summary>
            public SkillContext Context;

            /// <summary>
            /// 该槽位对应的技能运行时。
            /// </summary>
            public SkillRuntime Runtime;

            /// <summary>
            /// 运行时显示名。
            /// </summary>
            public string DisplayName;

            /// <summary>
            /// 是否正处于按压中。
            /// 当前代码里预留给更细的按压态处理。
            /// </summary>
            public bool IsPressing;

            /// <summary>
            /// 按压开始时间。
            /// 当前代码里预留给更细的按压时长处理。
            /// </summary>
            public float PressStartTime;
        }

        /// <summary>
        /// 被动技能的运行时装配记录。
        /// 当前主要用于保存被动技能槽位、显示名和资源引用。
        /// </summary>
        private sealed class PassiveSkillState
        {
            /// <summary>
            /// 被动技能槽位编号。
            /// </summary>
            public int SlotIndex;

            /// <summary>
            /// 显示名。
            /// </summary>
            public string DisplayName;

            /// <summary>
            /// 技能资源名。
            /// </summary>
            public string SkillAssetName;

            /// <summary>
            /// 已加载的技能配置。
            /// </summary>
            public SkillConfig Config;

        }
    }
}