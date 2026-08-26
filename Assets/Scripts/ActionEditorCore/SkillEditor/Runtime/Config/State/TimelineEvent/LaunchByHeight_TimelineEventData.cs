using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [TimelineEventData(TimelineEventType.LaunchByHeight)]
    public sealed class LaunchByHeight_TimelineEventData : TimelineEventData
    {
        public LaunchByHeightEventArgs Args = new LaunchByHeightEventArgs();

        public TimelineEventType EventType => TimelineEventType.LaunchByHeight;
        public object ArgsObject => Args;
        public bool SupportsDuration => false;
        public float DefaultDuration => 0f;

        public TimelineEventData Create()
        {
            return new LaunchByHeight_TimelineEventData();
        }

        public TimelineEventData Clone(TimelineEventData target)
        {
            LaunchByHeight_TimelineEventData result = target as LaunchByHeight_TimelineEventData ?? new LaunchByHeight_TimelineEventData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}
