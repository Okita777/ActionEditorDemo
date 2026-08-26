using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [SkillActionData(SkillActionType.AddBuff)]
    public sealed class AddBuff_SkillActionData : SkillActionData
    {
        public BuffActionArgs Args = new BuffActionArgs();

        public SkillActionType ActionType => SkillActionType.AddBuff;

        public SkillActionData Create()
        {
            return new AddBuff_SkillActionData();
        }

        public SkillActionData Clone(SkillActionData target)
        {
            AddBuff_SkillActionData result = target as AddBuff_SkillActionData ?? new AddBuff_SkillActionData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}
