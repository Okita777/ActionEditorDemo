using System;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public class SkillConditionConfig
    {
        [SerializeReference]
        public SkillConditionData Data;

        public SkillConditionType ConditionType => Data != null ? Data.ConditionType : SkillConditionType.None;

        public void CreateData(SkillConditionType type)
        {
            Data = SkillConditionDataFactory.Create(type);
        }

        public void CloneData(SkillConditionData source)
        {
            Data = source == null ? null : source.Clone(Data);
        }

        public void ClearData()
        {
            Data = null;
        }
    }
}
