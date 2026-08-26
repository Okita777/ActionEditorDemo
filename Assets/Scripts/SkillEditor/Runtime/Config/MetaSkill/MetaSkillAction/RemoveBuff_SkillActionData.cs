using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [SkillActionData(SkillActionType.RemoveBuff)]
    public sealed class RemoveBuff_SkillActionData : SkillActionData
    {
        public BuffActionArgs Args = new BuffActionArgs();

        public SkillActionType ActionType => SkillActionType.RemoveBuff;

        public SkillActionData Create()
        {
            return new RemoveBuff_SkillActionData();
        }

        public SkillActionData Clone(SkillActionData target)
        {
            RemoveBuff_SkillActionData result = target as RemoveBuff_SkillActionData ?? new RemoveBuff_SkillActionData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}
