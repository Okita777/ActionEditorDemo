namespace AsiSkillEditor.RunTime
{
    public interface ISkillAttributeSource
    {
        float GetAttribute(SkillAttributeType attributeType);
        void ApplyDamage(float amount);
        void ApplyToughnessDamage(float amount);
    }
}