using System;

namespace AsiSkillEditor.RunTime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class SkillConditionRuntimeAttribute : Attribute
    {
        public Type ConditionDataType { get; }

        public SkillConditionRuntimeAttribute(Type conditionDataType)
        {
            ConditionDataType = conditionDataType;
        }
    }
}
