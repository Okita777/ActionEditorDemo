using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [SkillActionData(SkillActionType.AddToughnessDamage)]
    public sealed class AddToughnessDamage_SkillActionData : SkillActionData
    {
        public DamageActionArgs Args = new DamageActionArgs();

        public SkillActionType ActionType => SkillActionType.AddToughnessDamage;

        public SkillActionData Create()
        {
            return new AddToughnessDamage_SkillActionData();
        }

        public SkillActionData Clone(SkillActionData target)
        {
            AddToughnessDamage_SkillActionData result = target as AddToughnessDamage_SkillActionData ?? new AddToughnessDamage_SkillActionData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}
