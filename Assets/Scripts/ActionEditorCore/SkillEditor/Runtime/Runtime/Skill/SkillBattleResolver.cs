using UnityEngine;
using SkillEditor.Preview;

namespace AsiSkillEditor.RunTime
{
    public sealed class SkillBattleResolver : IBattleResolver
    {
        public SkillEffectResult DealDamage(SkillContext context, DamageActionArgs args)
        {
            if (context == null || args == null)
            {
                return SkillEffectResult.Fail(SkillEffectFailureKind.InvalidData);
            }

            // [AICode] Battle resolution should consume the canonical GameUnit caster directly.
            GameUnit caster = context.Caster;
            if (caster == null || caster.Attributes == null)
            {
                Debug.LogWarning($"[AICode] SkillBattleResolver.DealDamage: caster missing attributes. caster='{(caster != null ? caster.name : "null")}'.");
                return SkillEffectResult.Fail(SkillEffectFailureKind.MissingCaster);
            }

            GameUnit targetUnit = SkillTargetResolver.Resolve(args.QueryTarget, context);
            if (targetUnit == null || targetUnit.Attributes == null)
            {
                Debug.LogWarning($"[AICode] SkillBattleResolver.DealDamage: target missing attributes. queryTarget={args.QueryTarget}, targetUnit='{(targetUnit != null ? targetUnit.name : "null")}'.", caster);
                return SkillEffectResult.Fail(SkillEffectFailureKind.MissingTarget);
            }

            SkillAttributeSet casterAttributes = caster.Attributes;
            SkillAttributeSet targetAttributes = targetUnit.Attributes;

            float sourceValue = casterAttributes.GetAttribute(args.SourceAttribute);
            float ratio = Mathf.Max(0f, args.Ratio);
            float damageValue = Mathf.Max(0f, sourceValue * ratio);
            float beforeHp = targetAttributes.GetAttribute(SkillAttributeType.CurrentHp);
            targetAttributes.ApplyDamage(damageValue);
            float afterHp = targetAttributes.GetAttribute(SkillAttributeType.CurrentHp);
            Debug.Log($"[AICode] SkillBattleResolver.DealDamage: caster='{caster.name}', target='{targetUnit.name}', source={args.SourceAttribute}, sourceValue={sourceValue:0.###}, ratio={ratio:0.###}, damage={damageValue:0.###}, hp={beforeHp:0.###}->{afterHp:0.###}.", targetUnit);

            ActionContext actionContext = CreateActionContext(context, targetUnit);
            int appliedDamage = Mathf.RoundToInt(damageValue);
            actionContext.DataContext.AddDamage(targetUnit, appliedDamage, args.SourceAttribute.ToString());
            return SkillEffectResult.Succeed(actionContext);
        }

        public SkillEffectResult AddToughnessDamage(SkillContext context, DamageActionArgs args)
        {
            if (context == null || args == null)
            {
                return SkillEffectResult.Fail(SkillEffectFailureKind.InvalidData);
            }

            // [AICode] Battle resolution should consume the canonical GameUnit caster directly.
            GameUnit caster = context.Caster;
            if (caster == null || caster.Attributes == null)
            {
                Debug.LogWarning($"[AICode] SkillBattleResolver.AddToughnessDamage: caster missing attributes. caster='{(caster != null ? caster.name : "null")}'.");
                return SkillEffectResult.Fail(SkillEffectFailureKind.MissingCaster);
            }

            GameUnit targetUnit = SkillTargetResolver.Resolve(args.QueryTarget, context);
            if (targetUnit == null || targetUnit.Attributes == null)
            {
                Debug.LogWarning($"[AICode] SkillBattleResolver.AddToughnessDamage: target missing attributes. queryTarget={args.QueryTarget}, targetUnit='{(targetUnit != null ? targetUnit.name : "null")}'.", caster);
                return SkillEffectResult.Fail(SkillEffectFailureKind.MissingTarget);
            }

            SkillAttributeSet casterAttributes = caster.Attributes;
            SkillAttributeSet targetAttributes = targetUnit.Attributes;

            float sourceValue = casterAttributes.GetAttribute(args.SourceAttribute);
            float ratio = Mathf.Max(0f, args.Ratio);
            float toughnessDamage = Mathf.Max(0f, sourceValue * ratio);
            float beforeBreak = targetAttributes.GetAttribute(SkillAttributeType.BreakValue);
            targetAttributes.ApplyToughnessDamage(toughnessDamage);
            float afterBreak = targetAttributes.GetAttribute(SkillAttributeType.BreakValue);
            Debug.Log($"[AICode] SkillBattleResolver.AddToughnessDamage: caster='{caster.name}', target='{targetUnit.name}', source={args.SourceAttribute}, sourceValue={sourceValue:0.###}, ratio={ratio:0.###}, toughness={toughnessDamage:0.###}, break={beforeBreak:0.###}->{afterBreak:0.###}.", targetUnit);

            ActionContext actionContext = CreateActionContext(context, targetUnit);
            int appliedToughnessDamage = Mathf.RoundToInt(toughnessDamage);
            actionContext.DataContext.AddToughnessDamage(targetUnit, appliedToughnessDamage, args.SourceAttribute.ToString());
            return SkillEffectResult.Succeed(actionContext);
        }

        private static ActionContext CreateActionContext(SkillContext context, GameUnit targetUnit)
        {
            MetaSkillContext metaSkillContext = context != null ? context.CurrentMetaSkillContext : null;
            ActionContext actionContext = new ActionContext
            {
                SkillRuntimeId = metaSkillContext != null ? metaSkillContext.SkillRuntimeId : string.Empty,
                SkillId = metaSkillContext != null ? metaSkillContext.SkillId : context != null && context.SkillConfig != null ? context.SkillConfig.SkillId : string.Empty,
                MetaSkillId = metaSkillContext != null ? metaSkillContext.MetaSkillId : context != null && context.CurrentMetaSkillConfig != null ? context.CurrentMetaSkillConfig.MetaSkillId : string.Empty,
                Caster = context != null ? context.Caster : null,
                PrimaryTarget = context != null ? context.PrimaryTarget : null,
                HasExecuted = true,
                Succeeded = true,
            };
            if (targetUnit != null)
            {
                actionContext.AffectedTargets.Add(targetUnit);
            }

            return actionContext;
        }
    }
}