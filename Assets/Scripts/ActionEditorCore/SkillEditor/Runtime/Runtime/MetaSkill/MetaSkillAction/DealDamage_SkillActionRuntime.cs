namespace AsiSkillEditor.RunTime
{
    [SkillActionRuntime(typeof(DealDamage_SkillActionData))]
    public sealed class DealDamage_SkillActionRuntime : SkillActionRuntimeBase
    {
        private readonly DealDamage_SkillActionData _data;

        public DealDamage_SkillActionRuntime(SkillActionConfig config) : base(config)
        {
            _data = mData as DealDamage_SkillActionData;
        }

        public override SkillEffectResult Execute(SkillEffectResult lastResult)
        {
            if (_data == null)
            {
                return SkillEffectResult.Fail(SkillEffectFailureKind.InvalidData);
            }

            if (mContext == null || mContext.CombatResolver == null)
            {
                return SkillEffectResult.Fail(SkillEffectFailureKind.MissingService);
            }

            return mContext.CombatResolver.DealDamage(mContext, _data.Args);
        }
    }
}
