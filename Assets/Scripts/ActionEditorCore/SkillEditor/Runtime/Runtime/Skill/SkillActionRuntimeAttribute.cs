using System;

namespace AsiSkillEditor.RunTime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class SkillActionRuntimeAttribute : Attribute
    {
        public Type ActionDataType { get; }

        public SkillActionRuntimeAttribute(Type actionDataType)
        {
            ActionDataType = actionDataType;
        }
    }
}
