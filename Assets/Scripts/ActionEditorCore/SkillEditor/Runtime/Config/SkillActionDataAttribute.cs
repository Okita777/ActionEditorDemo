using System;

namespace AsiSkillEditor.RunTime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class SkillActionDataAttribute : Attribute
    {
        public SkillActionType ActionType { get; }

        public SkillActionDataAttribute(SkillActionType actionType)
        {
            ActionType = actionType;
        }
    }
}
