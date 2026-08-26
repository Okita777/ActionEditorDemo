using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [SkillActionData(SkillActionType.DealDamage)]
    public sealed class DealDamage_SkillActionData : SkillActionData
    {
        public DamageActionArgs Args = new DamageActionArgs();

        public SkillActionType ActionType => SkillActionType.DealDamage;

        public SkillActionData Create()
        {
            return new DealDamage_SkillActionData();
        }

        public SkillActionData Clone(SkillActionData target)
        {
            DealDamage_SkillActionData result = target as DealDamage_SkillActionData ?? new DealDamage_SkillActionData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}
