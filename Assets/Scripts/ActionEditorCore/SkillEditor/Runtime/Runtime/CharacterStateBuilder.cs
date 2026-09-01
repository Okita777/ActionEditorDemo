using System;
using System.Collections.Generic;
using ActionEditor.TagSystem;
using SkillEditor.Preview;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    public sealed class CharacterStateBuildResult
    {
        public StateController StateController;
        public SkillContext StateContext;
    }

    /// <summary>
    /// [AICode] 角色状态机构建器。
    /// 只负责根据单位静态状态和技能注入状态构建共享 StateController。
    /// </summary>
    public static class CharacterStateBuilder
    {
        public static CharacterStateBuildResult Build(
            string unitId,
            IList<MetaSkillConfig> metaSkillConfigs,
            GameObject owner,
            GameUnit primaryTarget,
            Func<StateInputSnapshot> inputSnapshotProvider,
            Func<StateHitSnapshot> hitSnapshotProvider,
            Func<StateBeHitSnapshot> beHitSnapshotProvider,
            Func<float> breakValueProvider,
            IBattleResolver battleResolver,
            ISkillResourceService resourceService,
            ITagQueryService tagQueryService,
            IBuffService buffService,
            ICharacterAnimationController characterAnimationController)
        {
            if (string.IsNullOrWhiteSpace(unitId) || owner == null)
            {
                return null;
            }

            UnitConfig unitConfig = SkillRuntimeLoadData.Instance.LoadUnitConfig(unitId);
            List<StateConfig> states = SkillRuntimeLoadData.Instance.LoadStatesForUnit(unitId);
            NormalizeStandaloneStateMovementProfiles(states);
            ValidateLayerDefaultStateConfiguration(unitConfig);
            characterAnimationController?.ConfigureAnimationLayers(unitConfig);

            AppendSkillRuntimeStates(states, metaSkillConfigs);
            if (states == null || states.Count == 0)
            {
                return null;
            }

            GameUnit ownerUnit = GameUnitResolver.Resolve(owner);
            ActionEditor.CharacterMotion.CustomCharacterController characterController =
                owner.GetComponent<ActionEditor.CharacterMotion.CustomCharacterController>() ??
                owner.GetComponentInChildren<ActionEditor.CharacterMotion.CustomCharacterController>(true);
            ActionEditor.CharacterMotion.InputMotionSource inputMotionSource = characterController != null
                ? characterController.GetComponent<ActionEditor.CharacterMotion.InputMotionSource>()
                : owner.GetComponentInChildren<ActionEditor.CharacterMotion.InputMotionSource>(true);
            SkillContext stateContext = new SkillContext
            {
                Caster = ownerUnit,
                PrimaryTarget = primaryTarget,
                EffectExecutor = new SkillEffectRuntime(),
                TagQueryService = tagQueryService,
                ResourceService = resourceService,
                BuffService = buffService,
                CombatResolver = battleResolver,
                CharacterAnimationController = characterAnimationController,
                StateInputSnapshotProvider = inputSnapshotProvider,
                StateHitSnapshotProvider = hitSnapshotProvider,
                StateBeHitSnapshotProvider = beHitSnapshotProvider,
                BreakValueProvider = breakValueProvider,
            };

            StateController stateController = new StateController(states, new StateRuntimeContext
            {
                Unit = ownerUnit,
                SkillContext = stateContext,
                TagQueryService = tagQueryService,
                InputSnapshotProvider = inputSnapshotProvider,
                HitSnapshotProvider = hitSnapshotProvider,
                BeHitSnapshotProvider = beHitSnapshotProvider,
                BreakValueProvider = breakValueProvider,
                MotionSnapshotProvider = characterController != null
                    ? () => characterController.MotionSnapshot
                    : null,
                CharacterForwardProvider = characterController != null
                    ? () => characterController.transform.forward
                    : () => owner.transform.forward,
                MoveInputDirectionProvider = inputMotionSource != null
                    ? () => inputMotionSource.ResolveCurrentIntent().DesiredWorldDirection
                    : null,
            },
                unitConfig != null ? unitConfig.LayerDefaultStates : null,
                unitConfig != null ? unitConfig.RecoveryCancel : null);

            return new CharacterStateBuildResult
            {
                StateController = stateController,
                StateContext = stateContext,
            };
        }

        private static void AppendSkillRuntimeStates(List<StateConfig> states, IList<MetaSkillConfig> metaSkillConfigs)
        {
            states ??= new List<StateConfig>();

            HashSet<string> knownStateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < states.Count; index++)
            {
                if (states[index] == null || string.IsNullOrWhiteSpace(states[index].StateId))
                {
                    continue;
                }

                knownStateIds.Add(states[index].StateId);
            }

            if (metaSkillConfigs == null)
            {
                return;
            }

            for (int metaSkillIndex = 0; metaSkillIndex < metaSkillConfigs.Count; metaSkillIndex++)
            {
                MetaSkillConfig metaSkillConfig = metaSkillConfigs[metaSkillIndex];
                NormalizeEmbeddedSkillStateMovementProfile(metaSkillConfig != null ? metaSkillConfig.SkillStateTimeLineState : null);
                NormalizeEmbeddedSkillStateMovementProfile(metaSkillConfig != null ? metaSkillConfig.RecoverySkillStateTimeLineState : null);
                ApplyMetaSkillStateDefaultNextRule(metaSkillConfig);
                TryAppendStateConfig(metaSkillConfig != null ? metaSkillConfig.SkillStateTimeLineState : null, states, knownStateIds);
                TryAppendStateConfig(metaSkillConfig != null ? metaSkillConfig.RecoverySkillStateTimeLineState : null, states, knownStateIds);
            }
        }

        private static void NormalizeStandaloneStateMovementProfiles(IList<StateConfig> states)
        {
            if (states == null)
            {
                return;
            }

            for (int i = 0; i < states.Count; i++)
            {
                StateConfig state = states[i];
                if (state == null || state.MovementProfile != null)
                {
                    continue;
                }

                state.MovementProfile = CreateLegacyCompatibleProfile(state, StateMovementProfile.CreateDefault());
            }
        }

        private static void NormalizeEmbeddedSkillStateMovementProfile(StateConfig state)
        {
            if (state == null || state.MovementProfile != null)
            {
                return;
            }

            // 仅用于没有 MovementProfile 字段的旧 MetaSkill 数据。
            // 新数据始终以状态自身显式保存的策略为准，与 Layer 和本次进入来源无关。
            state.MovementProfile = CreateLegacyCompatibleProfile(state, StateMovementProfile.CreateLocked());
        }

        private static StateMovementProfile CreateLegacyCompatibleProfile(StateConfig state, StateMovementProfile fallback)
        {
            StateMovementProfile profile = fallback ?? StateMovementProfile.CreateDefault();
            if (state == null)
            {
                return profile;
            }

            if (state.ControlsMovement || state.LocomotionImpactMode == LocomotionImpactMode.LockMoveInput ||
                state.LocomotionImpactMode == LocomotionImpactMode.LockLocomotionDrive)
            {
                profile.TranslationMode = StateTranslationMode.Locked;
            }

            if (state.ControlsRotation)
            {
                profile.RotationMode = StateRotationMode.Locked;
            }

            return profile;
        }

        private static void ApplyMetaSkillStateDefaultNextRule(MetaSkillConfig metaSkillConfig)
        {
            if (metaSkillConfig == null || metaSkillConfig.SkillStateTimeLineState == null)
            {
                return;
            }

            StateConfig skillState = metaSkillConfig.SkillStateTimeLineState;
            StateConfig recoveryState = metaSkillConfig.RecoverySkillStateTimeLineState;
            if (recoveryState != null && !string.IsNullOrWhiteSpace(recoveryState.StateId))
            {
                skillState.DefaultNextStateId = recoveryState.StateId;
            }
        }

        private static void TryAppendStateConfig(StateConfig stateConfig, List<StateConfig> states, HashSet<string> knownStateIds)
        {
            if (stateConfig == null || string.IsNullOrWhiteSpace(stateConfig.StateId) || knownStateIds == null || states == null)
            {
                return;
            }

            stateConfig.Timeline ??= new StateTimelineConfig();
            stateConfig.Timeline.Animation ??= new TimelineAnimationConfig();
            stateConfig.Tags ??= new TagContainer();

            if (!knownStateIds.Add(stateConfig.StateId))
            {
                return;
            }

            states.Add(stateConfig);
        }
        private static string ResolveLayerDefaultStateId(UnitConfig unitConfig, StateLayerType layerType)
        {
            if (unitConfig != null && unitConfig.LayerDefaultStates != null)
            {
                for (int i = 0; i < unitConfig.LayerDefaultStates.Count; i++)
                {
                    UnitLayerDefaultStateConfig layerDefaultState = unitConfig.LayerDefaultStates[i];
                    if (layerDefaultState != null && layerDefaultState.Layer == layerType && !string.IsNullOrWhiteSpace(layerDefaultState.DefaultStateId))
                    {
                        return layerDefaultState.DefaultStateId;
                    }
                }
            }

            return string.Empty;
        }

        private static void ValidateLayerDefaultStateConfiguration(UnitConfig unitConfig)
        {
            if (unitConfig == null)
            {
                throw new InvalidOperationException("CharacterStateBuilder 构建失败：UnitConfig 为空。");
            }

            if (unitConfig.LayerDefaultStates == null)
            {
                throw new InvalidOperationException($"CharacterStateBuilder 构建失败：单位未配置分层默认状态，unitId={unitConfig.UnitId}。");
            }

            ValidateLayerDefaultState(unitConfig, StateLayerType.Locomotion);
            ValidateLayerDefaultState(unitConfig, StateLayerType.Action);
        }

        private static void ValidateLayerDefaultState(UnitConfig unitConfig, StateLayerType layerType)
        {
            string defaultStateId = ResolveLayerDefaultStateId(unitConfig, layerType);
            if (string.IsNullOrWhiteSpace(defaultStateId))
            {
                throw new InvalidOperationException($"CharacterStateBuilder 构建失败：层未配置默认状态，unitId={unitConfig.UnitId}, layer={layerType}。");
            }
        }

    }
}
