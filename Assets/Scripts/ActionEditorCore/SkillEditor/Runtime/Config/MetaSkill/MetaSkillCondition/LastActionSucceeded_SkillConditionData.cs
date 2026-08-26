using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [SkillConditionData(SkillConditionType.LastActionSucceeded)]
    public sealed class LastActionSucceeded_SkillConditionData : SkillConditionData
    {
        public ActionResultConditionArgs Args = new ActionResultConditionArgs();

        public SkillConditionType ConditionType => SkillConditionType.LastActionSucceeded;

        public SkillConditionData Create()
        {
            return new LastActionSucceeded_SkillConditionData();
        }

        public SkillConditionData Clone(SkillConditionData target)
        {
            LastActionSucceeded_SkillConditionData result = target as LastActionSucceeded_SkillConditionData ?? new LastActionSucceeded_SkillConditionData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}
