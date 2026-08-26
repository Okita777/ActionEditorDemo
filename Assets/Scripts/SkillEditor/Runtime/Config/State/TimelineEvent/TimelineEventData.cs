using System;

namespace AsiSkillEditor.RunTime
{
    public interface TimelineEventData
    {
        TimelineEventType EventType { get; }

        object ArgsObject { get; }

        bool SupportsDuration { get; }

        float DefaultDuration { get; }

        TimelineEventData Create();

        TimelineEventData Clone(TimelineEventData target);
    }
}
