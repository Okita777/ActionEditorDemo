namespace AsiSkillEditor.RunTime
{
    // 负责把战斗效果真正落到战斗系统里。
    public interface IBattleResolver
    {
        SkillEffectResult DealDamage(SkillContext context, DamageActionArgs args);
        SkillEffectResult AddToughnessDamage(SkillContext context, DamageActionArgs args);
    }
}
