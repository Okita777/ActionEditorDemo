using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public sealed class BuffConfig
    {
        public string BuffId = "buff_001";
        public string BuffName = "New Buff";
        public TagContainer Tags = new TagContainer();
        public float Duration = 0f;
        public bool IsStackable;
        public BuffStackMode StackMode = BuffStackMode.None;
        public BuffType BuffType = BuffType.None;
        public string IconAssetPath = string.Empty;
        public float UpdateInterval = 0f;
        public SkillEffectConfig OnAddEffect = new SkillEffectConfig();
        public SkillEffectConfig OnUpdateEffect = new SkillEffectConfig();
        public SkillEffectConfig OnRemoveEffect = new SkillEffectConfig();
    }
}