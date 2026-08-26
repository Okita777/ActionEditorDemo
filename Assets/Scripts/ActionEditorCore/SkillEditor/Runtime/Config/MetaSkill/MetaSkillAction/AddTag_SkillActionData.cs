using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [SkillActionData(SkillActionType.AddTag)]
    public sealed class AddTag_SkillActionData : SkillActionData
    {
        public TagActionArgs Args = new TagActionArgs();

        public SkillActionType ActionType => SkillActionType.AddTag;

        public SkillActionData Create()
        {
            return new AddTag_SkillActionData();
        }

        public SkillActionData Clone(SkillActionData target)
        {
            AddTag_SkillActionData result = target as AddTag_SkillActionData ?? new AddTag_SkillActionData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}