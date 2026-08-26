using System;

namespace AsiSkillEditor.RunTime
{
    public interface SkillActionData
    {
        SkillActionType ActionType { get; }

        SkillActionData Create();

        SkillActionData Clone(SkillActionData target);
    }
}
