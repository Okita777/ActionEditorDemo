using System;

namespace AsiSkillEditor.RunTime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class TimelineEventDataAttribute : Attribute
    {
        public TimelineEventType EventType { get; }

        public TimelineEventDataAttribute(TimelineEventType eventType)
        {
            EventType = eventType;
        }
    }
}
