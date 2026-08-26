using SkillEditor.Preview;

namespace AsiSkillEditor.RunTime
{
    [SkillConditionRuntime(typeof(HasBuff_SkillConditionData))]
    public sealed class HasBuff_SkillConditionRuntime : SkillConditionRuntimeBase
    {
        private readonly HasBuff_SkillConditionData _data;

        public HasBuff_SkillConditionRuntime(SkillConditionConfig config) : base(config)
        {
            _data = mData as HasBuff_SkillConditionData;
        }

        public override bool Evaluate(SkillEffectResult lastResult)
        {
            if (_data == null || _data.Args == null || mContext == null || mContext.BuffService == null)
            {
                return false;
            }

            GameUnit target = SkillTargetResolver.Resolve(_data.Args.QueryTarget, mContext);
            return target != null && mContext.BuffService.HasBuff(target, _data.Args.BuffId);
        }
    }
}
