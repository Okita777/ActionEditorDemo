using System;

namespace AsiSkillEditor.RunTime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class SkillConditionDataAttribute : Attribute
    {
        public SkillConditionType ConditionType { get; }

        public SkillConditionDataAttribute(SkillConditionType conditionType)
        {
            ConditionType = conditionType;
        }
    }
}
