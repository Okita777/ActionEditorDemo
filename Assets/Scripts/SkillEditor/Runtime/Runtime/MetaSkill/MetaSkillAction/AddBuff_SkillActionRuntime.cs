using SkillEditor.Preview;

namespace AsiSkillEditor.RunTime
{
    [SkillActionRuntime(typeof(AddBuff_SkillActionData))]
    public sealed class AddBuff_SkillActionRuntime : SkillActionRuntimeBase
    {
        private readonly AddBuff_SkillActionData _data;

        public AddBuff_SkillActionRuntime(SkillActionConfig config) : base(config)
        {
            _data = mData as AddBuff_SkillActionData;
        }

        public override SkillEffectResult Execute(SkillEffectResult lastResult)
        {
            if (_data == null || _data.Args == null)
            {
                return SkillEffectResult.Fail(SkillEffectFailureKind.InvalidData);
            }

            if (mContext == null || mContext.BuffService == null)
            {
                return SkillEffectResult.Fail(SkillEffectFailureKind.MissingService);
            }

            GameUnit target = SkillTargetResolver.Resolve(_data.Args.QueryTarget, mContext);
            if (target == null)
            {
                return SkillEffectResult.Fail(SkillEffectFailureKind.MissingTarget);
            }

            mContext.BuffService.AddBuff(target, _data.Args, mContext);
            ActionContext actionContext = CreateActionContext(target);
            actionContext.DataContext.AddBuff(target, _data.Args.BuffId);
            return SkillEffectResult.Succeed(actionContext);
        }

        private ActionContext CreateActionContext(GameUnit target)
        {
            MetaSkillContext metaSkillContext = mContext != null ? mContext.CurrentMetaSkillContext : null;
            ActionContext actionContext = new ActionContext
            {
                SkillRuntimeId = metaSkillContext != null ? metaSkillContext.SkillRuntimeId : string.Empty,
                SkillId = metaSkillContext != null ? metaSkillContext.SkillId : mContext != null && mContext.SkillConfig != null ? mContext.SkillConfig.SkillId : string.Empty,
                MetaSkillId = metaSkillContext != null ? metaSkillContext.MetaSkillId : mContext != null && mContext.CurrentMetaSkillConfig != null ? mContext.CurrentMetaSkillConfig.MetaSkillId : string.Empty,
                Caster = mContext != null ? mContext.Caster : null,
                PrimaryTarget = mContext != null ? mContext.PrimaryTarget : null,
                HasExecuted = true,
                Succeeded = true,
            };
            if (target != null)
            {
                actionContext.AffectedTargets.Add(target);
            }

            return actionContext;
        }
    }
}
