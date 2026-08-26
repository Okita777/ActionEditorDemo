using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [SkillActionData(SkillActionType.AddAttribute)]
    public sealed class AddAttribute_SkillActionData : SkillActionData
    {
        public AttributeActionArgs Args = new AttributeActionArgs();

        public SkillActionType ActionType => SkillActionType.AddAttribute;

        public SkillActionData Create()
        {
            return new AddAttribute_SkillActionData();
        }

        public SkillActionData Clone(SkillActionData target)
        {
            AddAttribute_SkillActionData result = target as AddAttribute_SkillActionData ?? new AddAttribute_SkillActionData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}