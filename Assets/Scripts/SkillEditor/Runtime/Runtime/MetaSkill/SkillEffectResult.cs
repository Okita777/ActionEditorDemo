using System;
using System.Collections.Generic;
using SkillEditor.Preview;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public enum SkillEffectFailureKind
    {
        None,
        InvalidData,
        MissingContext,
        MissingCaster,
        MissingTarget,
        MissingService,
        ConditionFailed,
        ExecutionException,
    }

    [Serializable]
    public sealed class TargetData
    {
        public string UnitId = string.Empty;
        public GameUnit Unit;

        public int TotalDamage;
        public int TotalToughnessDamage;
        public int AddedBuffCount;
        public int RemovedBuffCount;
        public int AddedTagCount;
        public int RemovedTagCount;

        public Dictionary<string, int> AddedBuffCountByBuffId = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> RemovedBuffCountByBuffId = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> AddedTagCountByTag = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> RemovedTagCountByTag = new Dictionary<string, int>(StringComparer.Ordinal);

        public Dictionary<string, int> DamageByType = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> ToughnessDamageByType = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> AttributeDeltaByAttributeType = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    [Serializable]
    public sealed class DataContext
    {
        public int TotalDamage;
        public int TotalToughnessDamage;
        public int TotalAddedBuffCount;
        public int TotalRemovedBuffCount;
        public int TotalAddedTagCount;
        public int TotalRemovedTagCount;

        public Dictionary<string, int> AddedBuffCountByBuffId = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> RemovedBuffCountByBuffId = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> AddedTagCountByTag = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> RemovedTagCountByTag = new Dictionary<string, int>(StringComparer.Ordinal);

        public Dictionary<string, int> DamageByType = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> ToughnessDamageByType = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> AttributeDeltaByAttributeType = new Dictionary<string, int>(StringComparer.Ordinal);

        public Dictionary<string, TargetData> TargetStats = new Dictionary<string, TargetData>(StringComparer.Ordinal);

        public void Merge(DataContext other)
        {
            if (other == null)
            {
                return;
            }

            TotalDamage += other.TotalDamage;
            TotalToughnessDamage += other.TotalToughnessDamage;
            TotalAddedBuffCount += other.TotalAddedBuffCount;
            TotalRemovedBuffCount += other.TotalRemovedBuffCount;
            TotalAddedTagCount += other.TotalAddedTagCount;
            TotalRemovedTagCount += other.TotalRemovedTagCount;

            MergeIntDictionary(AddedBuffCountByBuffId, other.AddedBuffCountByBuffId);
            MergeIntDictionary(RemovedBuffCountByBuffId, other.RemovedBuffCountByBuffId);
            MergeIntDictionary(AddedTagCountByTag, other.AddedTagCountByTag);
            MergeIntDictionary(RemovedTagCountByTag, other.RemovedTagCountByTag);
            MergeIntDictionary(DamageByType, other.DamageByType);
            MergeIntDictionary(ToughnessDamageByType, other.ToughnessDamageByType);
            MergeIntDictionary(AttributeDeltaByAttributeType, other.AttributeDeltaByAttributeType);

            foreach (KeyValuePair<string, TargetData> pair in other.TargetStats)
            {
                if (string.IsNullOrEmpty(pair.Key) || pair.Value == null)
                {
                    continue;
                }

                TargetData targetData = GetOrCreateTargetData(pair.Value.Unit, pair.Key);
                MergeTargetData(targetData, pair.Value);
            }
        }

        public TargetData GetOrCreateTargetData(GameUnit unit, string unitId = null)
        {
            string resolvedUnitId = !string.IsNullOrEmpty(unitId)
                ? unitId
                : unit != null
                    ? unit.UnitId ?? string.Empty
                    : string.Empty;
            if (string.IsNullOrEmpty(resolvedUnitId))
            {
                resolvedUnitId = unit != null ? unit.name : string.Empty;
            }

            if (!TargetStats.TryGetValue(resolvedUnitId, out TargetData targetData) || targetData == null)
            {
                targetData = new TargetData
                {
                    UnitId = resolvedUnitId,
                    Unit = unit,
                };
                TargetStats[resolvedUnitId] = targetData;
            }
            else if (targetData.Unit == null)
            {
                targetData.Unit = unit;
            }

            return targetData;
        }

        public void AddDamage(GameUnit unit, int amount, string damageType)
        {
            if (amount <= 0)
            {
                return;
            }

            TotalDamage += amount;
            AddToDictionary(DamageByType, damageType, amount);

            if (unit == null)
            {
                return;
            }

            TargetData targetData = GetOrCreateTargetData(unit);
            targetData.TotalDamage += amount;
            AddToDictionary(targetData.DamageByType, damageType, amount);
        }

        public void AddToughnessDamage(GameUnit unit, int amount, string damageType)
        {
            if (amount <= 0)
            {
                return;
            }

            TotalToughnessDamage += amount;
            AddToDictionary(ToughnessDamageByType, damageType, amount);

            if (unit == null)
            {
                return;
            }

            TargetData targetData = GetOrCreateTargetData(unit);
            targetData.TotalToughnessDamage += amount;
            AddToDictionary(targetData.ToughnessDamageByType, damageType, amount);
        }

        public void AddBuff(GameUnit unit, string buffId)
        {
            TotalAddedBuffCount++;
            AddToDictionary(AddedBuffCountByBuffId, buffId, 1);
            if (unit == null)
            {
                return;
            }

            TargetData targetData = GetOrCreateTargetData(unit);
            targetData.AddedBuffCount++;
            AddToDictionary(targetData.AddedBuffCountByBuffId, buffId, 1);
        }

        public void RemoveBuff(GameUnit unit, string buffId)
        {
            TotalRemovedBuffCount++;
            AddToDictionary(RemovedBuffCountByBuffId, buffId, 1);
            if (unit == null)
            {
                return;
            }

            TargetData targetData = GetOrCreateTargetData(unit);
            targetData.RemovedBuffCount++;
            AddToDictionary(targetData.RemovedBuffCountByBuffId, buffId, 1);
        }

        public void AddTag(GameUnit unit, string tag, int count)
        {
            int appliedCount = count > 0 ? count : 1;
            TotalAddedTagCount += appliedCount;
            AddToDictionary(AddedTagCountByTag, tag, appliedCount);
            if (unit == null)
            {
                return;
            }

            TargetData targetData = GetOrCreateTargetData(unit);
            targetData.AddedTagCount += appliedCount;
            AddToDictionary(targetData.AddedTagCountByTag, tag, appliedCount);
        }

        public void AddAttributeDelta(GameUnit unit, string attributeType, int amount)
        {
            if (amount == 0)
            {
                return;
            }

            AddToDictionary(AttributeDeltaByAttributeType, attributeType, amount);
            if (unit == null)
            {
                return;
            }

            TargetData targetData = GetOrCreateTargetData(unit);
            AddToDictionary(targetData.AttributeDeltaByAttributeType, attributeType, amount);
        }

        private static void MergeTargetData(TargetData target, TargetData source)
        {
            target.TotalDamage += source.TotalDamage;
            target.TotalToughnessDamage += source.TotalToughnessDamage;
            target.AddedBuffCount += source.AddedBuffCount;
            target.RemovedBuffCount += source.RemovedBuffCount;
            target.AddedTagCount += source.AddedTagCount;
            target.RemovedTagCount += source.RemovedTagCount;

            MergeIntDictionary(target.AddedBuffCountByBuffId, source.AddedBuffCountByBuffId);
            MergeIntDictionary(target.RemovedBuffCountByBuffId, source.RemovedBuffCountByBuffId);
            MergeIntDictionary(target.AddedTagCountByTag, source.AddedTagCountByTag);
            MergeIntDictionary(target.RemovedTagCountByTag, source.RemovedTagCountByTag);
            MergeIntDictionary(target.DamageByType, source.DamageByType);
            MergeIntDictionary(target.ToughnessDamageByType, source.ToughnessDamageByType);
            MergeIntDictionary(target.AttributeDeltaByAttributeType, source.AttributeDeltaByAttributeType);
        }

        private static void MergeIntDictionary(Dictionary<string, int> target, Dictionary<string, int> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            foreach (KeyValuePair<string, int> pair in source)
            {
                AddToDictionary(target, pair.Key, pair.Value);
            }
        }

        private static void AddToDictionary(Dictionary<string, int> dictionary, string key, int amount)
        {
            if (dictionary == null || string.IsNullOrEmpty(key) || amount == 0)
            {
                return;
            }

            if (dictionary.TryGetValue(key, out int existing))
            {
                dictionary[key] = existing + amount;
            }
            else
            {
                dictionary[key] = amount;
            }
        }
    }

    [Serializable]
    public sealed class ActionContext
    {
        public string SkillRuntimeId = string.Empty;
        public string SkillId = string.Empty;
        public string MetaSkillId = string.Empty;
        public string EffectId = string.Empty;
        public string EffectNodeId = string.Empty;
        public string ActionId = string.Empty;
        public SkillActionType ActionType = SkillActionType.None;

        public GameUnit Caster;
        public GameUnit PrimaryTarget;
        public List<GameUnit> AffectedTargets = new List<GameUnit>();

        public bool HasExecuted;
        public bool Succeeded;

        public DataContext DataContext = new DataContext();
    }

    [Serializable]
    public sealed class SkillEffectResult
    {
        public static SkillEffectResult None => new SkillEffectResult();

        public string SkillRuntimeId = string.Empty;
        public string SkillId = string.Empty;
        public string MetaSkillId = string.Empty;
        public string MetaSkillNodeId = string.Empty;
        public string EffectId = string.Empty;
        public string SourceNodeId = string.Empty;

        public GameUnit Caster;
        public GameUnit PrimaryTarget;
        public bool HasValue;
        public bool HasExecuted;
        public bool Succeeded = true;
        public SkillEffectFailureKind FailureKind = SkillEffectFailureKind.None;

        public List<GameUnit> AffectedTargets = new List<GameUnit>();
        public ActionContext CurrentActionContext;
        public ActionContext LastActionContext;
        public DataContext DataContext = new DataContext();

        public static SkillEffectResult Succeed(ActionContext actionContext = null)
        {
            SkillEffectResult result = new SkillEffectResult
            {
                HasValue = true,
                HasExecuted = true,
                Succeeded = true,
                CurrentActionContext = actionContext,
                LastActionContext = actionContext,
            };
            ApplyActionContext(result, actionContext);
            return result;
        }

        public static SkillEffectResult Fail(SkillEffectFailureKind failureKind)
        {
            return new SkillEffectResult
            {
                HasValue = true,
                HasExecuted = true,
                Succeeded = false,
                FailureKind = failureKind,
            };
        }

        public void Merge(ActionContext actionContext)
        {
            CurrentActionContext = actionContext;
            LastActionContext = actionContext;
            HasExecuted = true;
            HasValue = true;
            ApplyActionContext(this, actionContext);
        }

        public void Merge(SkillEffectResult other)
        {
            if (other == null)
            {
                return;
            }

            HasValue |= other.HasValue;
            if (!other.Succeeded)
            {
                Succeeded = false;
                if (FailureKind == SkillEffectFailureKind.None)
                {
                    FailureKind = other.FailureKind;
                }
            }

            if (other.LastActionContext != null)
            {
                LastActionContext = other.LastActionContext;
            }

            if (other.CurrentActionContext != null)
            {
                CurrentActionContext = other.CurrentActionContext;
            }

            HasExecuted |= other.HasExecuted;

            MergeAffectedTargets(AffectedTargets, other.AffectedTargets);
            DataContext.Merge(other.DataContext);
        }

        private static void ApplyActionContext(SkillEffectResult result, ActionContext actionContext)
        {
            if (result == null || actionContext == null)
            {
                return;
            }

            MergeAffectedTargets(result.AffectedTargets, actionContext.AffectedTargets);
            result.DataContext.Merge(actionContext.DataContext);
        }

        private static void MergeAffectedTargets(List<GameUnit> targetList, List<GameUnit> source)
        {
            if (targetList == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                GameUnit unit = source[i];
                if (unit == null || targetList.Contains(unit))
                {
                    continue;
                }

                targetList.Add(unit);
            }
        }
    }
}
