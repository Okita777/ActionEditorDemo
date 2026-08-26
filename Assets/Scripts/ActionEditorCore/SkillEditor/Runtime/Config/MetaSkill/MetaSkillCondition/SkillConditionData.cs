using System;

namespace AsiSkillEditor.RunTime
{
    public interface SkillConditionData
    {
        SkillConditionType ConditionType { get; }

        SkillConditionData Create();

        SkillConditionData Clone(SkillConditionData target);
    }
}
