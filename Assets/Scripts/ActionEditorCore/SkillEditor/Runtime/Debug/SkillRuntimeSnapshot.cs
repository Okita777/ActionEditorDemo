using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public class SkillRuntimeSnapshot
    {
        public bool IsCasting;
        public string SkillId = string.Empty;
        public float CooldownRemaining;
        public int LayerIndex = -1;
        public string CurrentNodeId = string.Empty;
        public string CurrentMetaSkillId = string.Empty;
        public float TimelineTime;
        public string LastTimelineItemType = string.Empty;
        public string LastTimelineItemId = string.Empty;
        public string LastEffectNodeId = string.Empty;
    }
}
