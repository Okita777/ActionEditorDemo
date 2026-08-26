using System;
using System.Collections.Generic;
using ActionEditor.TagSystem;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public sealed class SkillResourceCostConfig
    {
        public SkillCostResourceType ResourceType = SkillCostResourceType.Mana;
        public float Amount = 0f;
    }

    [Serializable]
    public class SkillConfig : IRuntimeTagContainerOwner
    {
        public string SkillId = "skill_001";
        public string SkillName = "New Skill";
        public SkillCastCategory SkillCategory = SkillCastCategory.Active;
        public float Cooldown = 0f;
        public float ComboContinuationTimeout = 0f;
        public List<SkillResourceCostConfig> ResourceCosts = new List<SkillResourceCostConfig>();
        public List<SkillLayerConfig> Layers = new List<SkillLayerConfig>
        {
            new SkillLayerConfig()
        };
        public TagContainer Tags = new TagContainer();

        [NonSerialized] private RuntimeTagContainer _runtimeTags;

        public RuntimeTagContainer RuntimeTags => _runtimeTags ??= new RuntimeTagContainer();
    }
}
