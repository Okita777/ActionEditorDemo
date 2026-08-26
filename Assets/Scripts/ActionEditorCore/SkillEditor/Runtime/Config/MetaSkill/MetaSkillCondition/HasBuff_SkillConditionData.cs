using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [SkillConditionData(SkillConditionType.HasBuff)]
    public sealed class HasBuff_SkillConditionData : SkillConditionData
    {
        public BuffConditionArgs Args = new BuffConditionArgs();

        public SkillConditionType ConditionType => SkillConditionType.HasBuff;

        public SkillConditionData Create()
        {
            return new HasBuff_SkillConditionData();
        }

        public SkillConditionData Clone(SkillConditionData target)
        {
            HasBuff_SkillConditionData result = target as HasBuff_SkillConditionData ?? new HasBuff_SkillConditionData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}
