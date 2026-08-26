using System.Collections.Generic;
using ActionEditor.TagSystem;
using SkillEditor.Preview;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    [System.Serializable]
    public sealed class CharacterBuffContainer
    {
        [System.NonSerialized] private IGameUnit _owner;
        [System.NonSerialized] private List<BuffInstance> _instances;

        public void Bind(IGameUnit owner)
        {
            _owner = owner;
            _instances ??= new List<BuffInstance>();
        }

        public bool HasBuff(string buffId)
        {
            return GetBuff(buffId) != null;
        }

        public bool HasTag(string tag)
        {
            return GetTagCount(tag) > 0;
        }

        public int GetTagCount(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return 0;
            }

            EnsureState();

            int total = 0;
            for (int i = 0; i < _instances.Count; i++)
            {
                BuffInstance instance = _instances[i];
                if (instance != null)
                {
                    total += instance.GetTagCount(tag);
                }
            }

            return total;
        }

        public IReadOnlyList<ICharacterBuff> GetAllBuff()
        {
            EnsureState();

            List<ICharacterBuff> result = new List<ICharacterBuff>(_instances.Count);
            for (int i = 0; i < _instances.Count; i++)
            {
                if (_instances[i] != null)
                {
                    result.Add(_instances[i]);
                }
            }

            return result;
        }

        public IReadOnlyList<ICharacterBuff> GetBuffByTags(IReadOnlyList<string> tags)
        {
            EnsureState();

            List<ICharacterBuff> result = new List<ICharacterBuff>();
            if (tags == null || tags.Count == 0)
            {
                return result;
            }

            for (int i = 0; i < _instances.Count; i++)
            {
                BuffInstance instance = _instances[i];
                if (instance == null || !ContainsAllTags(instance, tags))
                {
                    continue;
                }

                result.Add(instance);
            }

            return result;
        }

        public BuffInstance GetBuff(string buffId)
        {
            if (string.IsNullOrEmpty(buffId))
            {
                return null;
            }

            EnsureState();
            for (int i = 0; i < _instances.Count; i++)
            {
                BuffInstance instance = _instances[i];
                if (instance != null && string.Equals(instance.BuffId, buffId, System.StringComparison.Ordinal))
                {
                    return instance;
                }
            }

            return null;
        }

        public void AddBuff(BuffConfig config, BuffActionArgs args, SkillContext sourceContext)
        {
            if (_owner == null || config == null || string.IsNullOrEmpty(config.BuffId))
            {
                Debug.LogWarning($"CharacterBuffContainer.AddBuff: invalid state owner='{(_owner != null ? _owner.name : "null")}' buffId='{(config != null ? config.BuffId : "null")}'.", _owner != null ? _owner.gameObject : null);
                return;
            }

            EnsureState();
            PreloadBuffEffects(config, sourceContext);

            float duration = ResolveDuration(config, args);
            BuffInstance existing = GetBuff(config.BuffId);
            if (existing == null)
            {
                BuffInstance created = new BuffInstance(config, _owner, sourceContext != null ? sourceContext.Caster : null, duration, 1);
                _instances.Add(created);
                Debug.Log($"CharacterBuffContainer.AddBuff: created buff '{config.BuffId}' on '{_owner.name}' duration={duration:0.###} infinite={created.IsInfiniteDuration} stack=1 updateInterval={config.UpdateInterval:0.###}.", _owner.gameObject);
                ExecuteEffect(created, config.OnAddEffect, sourceContext);
                return;
            }

            ApplyStackRule(existing, config, duration, sourceContext);
            Debug.Log($"CharacterBuffContainer.AddBuff: refreshed buff '{config.BuffId}' on '{_owner.name}' duration={existing.RemainingDuration:0.###} infinite={existing.IsInfiniteDuration} stack={existing.StackCount} mode={config.StackMode}.", _owner.gameObject);
            ExecuteEffect(existing, config.OnAddEffect, sourceContext);
        }

        public void RemoveBuff(string buffId, SkillContext sourceContext)
        {
            if (string.IsNullOrEmpty(buffId))
            {
                Debug.LogWarning("CharacterBuffContainer.RemoveBuff: buffId is empty.", _owner != null ? _owner.gameObject : null);
                return;
            }

            EnsureState();

            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                BuffInstance instance = _instances[i];
                if (instance == null || !string.Equals(instance.BuffId, buffId, System.StringComparison.Ordinal))
                {
                    continue;
                }

                CleanupRevertibleState(instance);
                Debug.Log($"CharacterBuffContainer.RemoveBuff: removing buff '{instance.BuffId}' from '{_owner.name}'.", _owner.gameObject);
                ExecuteEffect(instance, instance.Config != null ? instance.Config.OnRemoveEffect : null, sourceContext);
                _instances.RemoveAt(i);
            }
        }

        public void Tick(float deltaTime)
        {
            EnsureState();

            float clampedDeltaTime = Mathf.Max(0f, deltaTime);
            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                BuffInstance instance = _instances[i];
                if (instance == null)
                {
                    _instances.RemoveAt(i);
                    continue;
                }

                TickUpdate(instance, clampedDeltaTime);

                if (!instance.IsInfiniteDuration)
                {
                    instance.RemainingDuration -= clampedDeltaTime;
                    if (instance.RemainingDuration <= 0f)
                    {
                        Debug.Log($"CharacterBuffContainer.Tick: buff '{instance.BuffId}' expired on '{_owner.name}'.", _owner.gameObject);
                        CleanupRevertibleState(instance);
                        ExecuteEffect(instance, instance.Config != null ? instance.Config.OnRemoveEffect : null, null);
                        _instances.RemoveAt(i);
                    }
                }
            }
        }

        private void EnsureState()
        {
            _instances ??= new List<BuffInstance>();
        }

        private static bool ContainsAllTags(BuffInstance instance, IReadOnlyList<string> tags)
        {
            if (instance == null || tags == null)
            {
                return false;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                if (string.IsNullOrEmpty(tag))
                {
                    continue;
                }

                if (!instance.HasTag(tag))
                {
                    return false;
                }
            }

            return true;
        }

        private static float ResolveDuration(BuffConfig config, BuffActionArgs args)
        {
            float requestedDuration = args != null ? args.Duration : 0f;
            if (requestedDuration > 0f)
            {
                return requestedDuration;
            }

            return config != null ? Mathf.Max(0f, config.Duration) : 0f;
        }

        private static void ApplyStackRule(BuffInstance instance, BuffConfig config, float duration, SkillContext sourceContext)
        {
            if (instance == null || config == null)
            {
                return;
            }

            instance.Source = sourceContext != null ? sourceContext.Caster : instance.Source;

            switch (config.StackMode)
            {
                case BuffStackMode.AddStack:
                    if (config.IsStackable)
                    {
                        instance.StackCount = Mathf.Max(1, instance.StackCount + 1);
                    }

                    if (duration > 0f)
                    {
                        instance.RemainingDuration = Mathf.Max(instance.RemainingDuration, duration);
                        instance.IsInfiniteDuration = false;
                    }
                    break;

                case BuffStackMode.ExtendDuration:
                    if (duration <= 0f)
                    {
                        instance.IsInfiniteDuration = true;
                    }
                    else if (!instance.IsInfiniteDuration)
                    {
                        instance.RemainingDuration += duration;
                    }
                    break;

                case BuffStackMode.None:
                default:
                    instance.StackCount = Mathf.Max(1, instance.StackCount);
                    instance.IsInfiniteDuration = duration <= 0f;
                    instance.RemainingDuration = duration;
                    break;
            }
        }

        private void TickUpdate(BuffInstance instance, float deltaTime)
        {
            if (instance == null || instance.Config == null)
            {
                return;
            }

            float interval = Mathf.Max(0f, instance.Config.UpdateInterval);
            if (interval <= 0f)
            {
                return;
            }

            instance.UpdateElapsedTime += deltaTime;
            while (instance.UpdateElapsedTime >= interval)
            {
                instance.UpdateElapsedTime -= interval;
                Debug.Log($"CharacterBuffContainer.TickUpdate: buff '{instance.BuffId}' triggered OnUpdate on '{_owner.name}' interval={interval:0.###} remaining={instance.RemainingDuration:0.###}.", _owner.gameObject);
                ExecuteEffect(instance, instance.Config.OnUpdateEffect, null);
            }
        }

        private void ExecuteEffect(BuffInstance instance, SkillEffectConfig effectConfig, SkillContext sourceContext)
        {
            if (instance == null || effectConfig == null || _owner == null)
            {
                Debug.LogWarning($"CharacterBuffContainer.ExecuteEffect: skipped effect execution because instance/effect/owner is missing. buff='{(instance != null ? instance.BuffId : "null")}'.", _owner != null ? _owner.gameObject : null);
                return;
            }

            SkillContext context = BuildBuffContext(instance, sourceContext);
            SkillEffectResult result = context.EffectExecutor.Execute(effectConfig, context);
            Debug.Log($"CharacterBuffContainer.ExecuteEffect: buff '{instance.BuffId}' executed effect root='{effectConfig.RootNodeId}' success={(result != null && result.Succeeded)} on '{_owner.name}'.", _owner.gameObject);
        }

        private SkillContext BuildBuffContext(BuffInstance instance, SkillContext sourceContext)
        {
            // [AICode] Buff execution context keeps caster/target semantics as GameUnit.
            GameUnit ownerUnit = _owner as GameUnit;
            GameUnit sourceUnit = sourceContext != null && sourceContext.Caster != null
                ? sourceContext.Caster
                : instance.Source ?? ownerUnit;
            SkillContext context = new SkillContext
            {
                Caster = sourceUnit,
                EquippedWeapon = sourceContext != null ? sourceContext.EquippedWeapon : null,
                PrimaryTarget = ownerUnit,
                ActiveBuffSourceId = instance.RuntimeId,
                RegisterTemporaryContributionTarget = instance.RegisterTemporaryContributionTarget,
                EffectExecutor = sourceContext != null && sourceContext.EffectExecutor != null ? sourceContext.EffectExecutor : new SkillEffectRuntime(),
                BuffService = sourceContext != null && sourceContext.BuffService != null ? sourceContext.BuffService : new CharacterBuffService(),
                TagQueryService = sourceContext != null && sourceContext.TagQueryService != null ? sourceContext.TagQueryService : new TagRuntimeService(),
                ResourceService = sourceContext != null && sourceContext.ResourceService != null ? sourceContext.ResourceService : SkillResourceServiceUtility.Resolve(ownerUnit != null ? (object)ownerUnit : _owner.gameObject),
                CharacterAnimationController = sourceContext != null ? sourceContext.CharacterAnimationController : null,
                CombatResolver = sourceContext != null && sourceContext.CombatResolver != null ? sourceContext.CombatResolver : new SkillBattleResolver(),
                RuntimeObserver = sourceContext != null ? sourceContext.RuntimeObserver : null,
                SkillConfig = sourceContext != null ? sourceContext.SkillConfig : null,
                CurrentMetaSkillConfig = sourceContext != null ? sourceContext.CurrentMetaSkillConfig : null,
                CurrentStateConfig = sourceContext != null ? sourceContext.CurrentStateConfig : null,
                StateController = sourceContext != null ? sourceContext.StateController : null,
                StateInputSnapshotProvider = sourceContext != null ? sourceContext.StateInputSnapshotProvider : null,
                StateHitSnapshotProvider = sourceContext != null ? sourceContext.StateHitSnapshotProvider : null,
                StateBeHitSnapshotProvider = sourceContext != null ? sourceContext.StateBeHitSnapshotProvider : null,
                BreakValueProvider = sourceContext != null ? sourceContext.BreakValueProvider : null,
            };
            return context;
        }

        private static void PreloadBuffEffects(BuffConfig config, SkillContext sourceContext)
        {
            if (config == null || sourceContext == null)
            {
                return;
            }

            if (!(sourceContext.EffectExecutor is SkillEffectRuntime effectRuntime))
            {
                return;
            }

            effectRuntime.Preload(config.OnAddEffect);
            effectRuntime.Preload(config.OnUpdateEffect);
            effectRuntime.Preload(config.OnRemoveEffect);
        }

        private void CleanupRevertibleState(BuffInstance instance)
        {
            if (instance == null || _owner == null || string.IsNullOrEmpty(instance.RuntimeId))
            {
                return;
            }

            IReadOnlyList<GameUnit> contributionTargets = instance.TemporaryContributionTargets;
            if (contributionTargets == null || contributionTargets.Count == 0)
            {
                CleanupContributionTarget(_owner as GameUnit, instance.RuntimeId);
                return;
            }

            for (int i = 0; i < contributionTargets.Count; i++)
            {
                CleanupContributionTarget(contributionTargets[i], instance.RuntimeId);
            }
        }

        private static void CleanupContributionTarget(GameUnit target, string sourceId)
        {
            if (target == null || string.IsNullOrEmpty(sourceId))
            {
                return;
            }

            // [AICode] 直接回收各子系统贡献，避免临时 buff 贡献残留在单位运行时上。
            target.Attributes?.RemoveModifiersFromSource(sourceId);
            target.RuntimeTags.RemoveAllTagsFromSource(sourceId);
        }
    }
}