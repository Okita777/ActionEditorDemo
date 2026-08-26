namespace AsiSkillEditor.RunTime
{
    public interface ISkillEffectExecutor
    {
        SkillEffectResult Execute(SkillEffectConfig config, SkillContext context);
    }
}
