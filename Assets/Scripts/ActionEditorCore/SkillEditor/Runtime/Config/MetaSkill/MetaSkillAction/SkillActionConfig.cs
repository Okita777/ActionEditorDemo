using System;
using ActionEditor.TagSystem;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public class SkillActionConfig : IRuntimeTagContainerOwner
    {
        [SerializeReference]
        public SkillActionData Data;
        public TagContainer Tags = new TagContainer();

        [NonSerialized] private RuntimeTagContainer _runtimeTags;

        public RuntimeTagContainer RuntimeTags => _runtimeTags ??= new RuntimeTagContainer();

        public SkillActionType ActionType => Data != null ? Data.ActionType : SkillActionType.None;

        public void CreateData(SkillActionType type)
        {
            Data = SkillActionDataFactory.Create(type);
        }

        public void CloneData(SkillActionData source)
        {
            Data = source == null ? null : source.Clone(Data);
        }

        public void ClearData()
        {
            Data = null;
        }
    }
}
