using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [SkillConditionData(SkillConditionType.AttributeCompare)]
    public sealed class AttributeCompare_SkillConditionData : SkillConditionData
    {
        public AttributeCompareArgs Args = new AttributeCompareArgs();

        public SkillConditionType ConditionType => SkillConditionType.AttributeCompare;

        public SkillConditionData Create()
        {
            return new AttributeCompare_SkillConditionData();
        }

        public SkillConditionData Clone(SkillConditionData target)
        {
            AttributeCompare_SkillConditionData result = target as AttributeCompare_SkillConditionData ?? new AttributeCompare_SkillConditionData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}
