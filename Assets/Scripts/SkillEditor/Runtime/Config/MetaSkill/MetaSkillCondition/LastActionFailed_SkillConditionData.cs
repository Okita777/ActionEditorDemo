using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [SkillConditionData(SkillConditionType.LastActionFailed)]
    public sealed class LastActionFailed_SkillConditionData : SkillConditionData
    {
        public ActionResultConditionArgs Args = new ActionResultConditionArgs();

        public SkillConditionType ConditionType => SkillConditionType.LastActionFailed;

        public SkillConditionData Create()
        {
            return new LastActionFailed_SkillConditionData();
        }

        public SkillConditionData Clone(SkillConditionData target)
        {
            LastActionFailed_SkillConditionData result = target as LastActionFailed_SkillConditionData ?? new LastActionFailed_SkillConditionData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}
