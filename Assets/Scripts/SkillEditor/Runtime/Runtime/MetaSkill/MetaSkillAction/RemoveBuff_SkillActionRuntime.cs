using SkillEditor.Preview;

namespace AsiSkillEditor.RunTime
{
    [SkillActionRuntime(typeof(RemoveBuff_SkillActionData))]
    public sealed class RemoveBuff_SkillActionRuntime : SkillActionRuntimeBase
    {
        private readonly RemoveBuff_SkillActionData _data;

        public RemoveBuff_SkillActionRuntime(SkillActionConfig config) : base(config)
        {
            _data = mData as RemoveBuff_SkillActionData;
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

            mContext.BuffService.RemoveBuff(target, _data.Args, mContext);
            ActionContext actionContext = new ActionContext
            {
                Caster = mContext.Caster,
                PrimaryTarget = mContext.PrimaryTarget,
                HasExecuted = true,
                Succeeded = true,
            };
            actionContext.AffectedTargets.Add(target);
            actionContext.DataContext.RemoveBuff(target, _data.Args.BuffId);
            return SkillEffectResult.Succeed(actionContext);
        }
    }
}
