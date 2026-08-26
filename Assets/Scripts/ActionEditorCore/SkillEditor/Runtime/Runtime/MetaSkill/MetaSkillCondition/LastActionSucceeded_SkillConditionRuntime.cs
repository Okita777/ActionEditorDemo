namespace AsiSkillEditor.RunTime
{
    [SkillConditionRuntime(typeof(LastActionSucceeded_SkillConditionData))]
    public sealed class LastActionSucceeded_SkillConditionRuntime : SkillConditionRuntimeBase
    {
        public LastActionSucceeded_SkillConditionRuntime(SkillConditionConfig config) : base(config)
        {
        }

        public override bool Evaluate(SkillEffectResult lastResult)
        {
            return lastResult != null && lastResult.HasValue && lastResult.Succeeded;
        }
    }
}
