using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [SkillConditionData(SkillConditionType.HasTag)]
    public sealed class HasTag_SkillConditionData : SkillConditionData
    {
        public TagConditionArgs Args = new TagConditionArgs();

        public SkillConditionType ConditionType => SkillConditionType.HasTag;

        public SkillConditionData Create()
        {
            return new HasTag_SkillConditionData();
        }

        public SkillConditionData Clone(SkillConditionData target)
        {
            HasTag_SkillConditionData result = target as HasTag_SkillConditionData ?? new HasTag_SkillConditionData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}
