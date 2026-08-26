namespace AsiSkillEditor.RunTime
{
    [SkillActionRuntime(typeof(AddToughnessDamage_SkillActionData))]
    public sealed class AddToughnessDamage_SkillActionRuntime : SkillActionRuntimeBase
    {
        private readonly AddToughnessDamage_SkillActionData _data;

        public AddToughnessDamage_SkillActionRuntime(SkillActionConfig config) : base(config)
        {
            _data = mData as AddToughnessDamage_SkillActionData;
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

            return mContext.CombatResolver.AddToughnessDamage(mContext, _data.Args);
        }
    }
}
