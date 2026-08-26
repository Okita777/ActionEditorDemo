namespace AsiSkillEditor.RunTime
{
    [SkillConditionRuntime(typeof(LastActionFailed_SkillConditionData))]
    public sealed class LastActionFailed_SkillConditionRuntime : SkillConditionRuntimeBase
    {
        public LastActionFailed_SkillConditionRuntime(SkillConditionConfig config) : base(config)
        {
        }

        public override bool Evaluate(SkillEffectResult lastResult)
        {
            return lastResult != null && lastResult.HasValue && !lastResult.Succeeded;
        }
    }
}
