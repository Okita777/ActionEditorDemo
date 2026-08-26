using SkillEditor.Preview;

namespace AsiSkillEditor.RunTime
{
    [SkillActionRuntime(typeof(AddAttribute_SkillActionData))]
    public sealed class AddAttribute_SkillActionRuntime : SkillActionRuntimeBase
    {
        private readonly AddAttribute_SkillActionData _data;

        public AddAttribute_SkillActionRuntime(SkillActionConfig config) : base(config)
        {
            _data = mData as AddAttribute_SkillActionData;
        }

        public override SkillEffectResult Execute(SkillEffectResult lastResult)
        {
            if (_data == null || _data.Args == null)
            {
                return SkillEffectResult.Fail(SkillEffectFailureKind.InvalidData);
            }

            if (mContext == null)
            {
                return SkillEffectResult.Fail(SkillEffectFailureKind.MissingContext);
            }

            GameUnit target = SkillTargetResolver.Resolve(_data.Args.QueryTarget, mContext);
            if (target == null)
            {
                return SkillEffectResult.Fail(SkillEffectFailureKind.MissingTarget);
            }

            SkillAttributeSet attributeSet = target.Attributes;
            if (attributeSet == null)
            {
                return SkillEffectResult.Fail(SkillEffectFailureKind.MissingTarget);
            }

            int valueDelta = UnityEngine.Mathf.RoundToInt(_data.Args.Value);
            if (_data.Args.ApplyLifetime == AttributeApplyLifetime.TemporaryBuff)
            {
                if (string.IsNullOrEmpty(mContext.ActiveBuffSourceId))
                {
                    return SkillEffectResult.Fail(SkillEffectFailureKind.MissingContext);
                }

                attributeSet.AddModifier(_data.Args.AttributeType, _data.Args.ModifyMode, _data.Args.Value, mContext.ActiveBuffSourceId);
                mContext.RegisterTemporaryContributionTarget?.Invoke(target);
                return BuildAttributeResult(target, valueDelta);
            }

            attributeSet.ApplyPermanentChange(_data.Args.AttributeType, _data.Args.ModifyMode, _data.Args.Value);
            return BuildAttributeResult(target, valueDelta);
        }

        private SkillEffectResult BuildAttributeResult(GameUnit target, int valueDelta)
        {
            ActionContext actionContext = new ActionContext
            {
                Caster = mContext.Caster,
                PrimaryTarget = mContext.PrimaryTarget,
                HasExecuted = true,
                Succeeded = true,
            };
            actionContext.AffectedTargets.Add(target);
            actionContext.DataContext.AddAttributeDelta(target, _data.Args.AttributeType.ToString(), valueDelta);
            return SkillEffectResult.Succeed(actionContext);
        }
    }
}