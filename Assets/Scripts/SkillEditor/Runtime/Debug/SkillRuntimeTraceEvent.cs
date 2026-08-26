using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public class SkillRuntimeTraceEvent
    {
        public string TraceType = string.Empty;
        public string NodeId = string.Empty;
        public string MetaSkillId = string.Empty;
        public string PayloadId = string.Empty;
        public float Time;
        public string Message = string.Empty;
    }
}
