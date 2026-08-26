using System;
using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public class SkillEventEntryConfig
    {
        public SkillEventType EventType = SkillEventType.CastSkillShort;
        public string Argument = string.Empty;
    }

    [Serializable]
    public class SkillEventConfig
    {
        public string EventId = Guid.NewGuid().ToString("N");
        public string FromNodeId = string.Empty;
        public string ToNodeId = string.Empty;
        public SkillConditionMode EventMode = SkillConditionMode.All;
        public List<SkillEventEntryConfig> Events = new List<SkillEventEntryConfig>
        {
            new SkillEventEntryConfig()
        };
        public SkillConditionMode ConditionMode = SkillConditionMode.All;
        public List<SkillConditionConfig> Conditions = new List<SkillConditionConfig>();
    }
}
