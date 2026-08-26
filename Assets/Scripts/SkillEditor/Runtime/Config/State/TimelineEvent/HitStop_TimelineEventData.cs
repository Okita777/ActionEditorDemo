using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [TimelineEventData(TimelineEventType.HitStop)]
    public sealed class HitStop_TimelineEventData : TimelineEventData
    {
        public HitStopEventArgs Args = new HitStopEventArgs();

        public TimelineEventType EventType => TimelineEventType.HitStop;
        public object ArgsObject => Args;
        public bool SupportsDuration => true;
        public float DefaultDuration => 0.3f;

        public TimelineEventData Create()
        {
            return new HitStop_TimelineEventData();
        }

        public TimelineEventData Clone(TimelineEventData target)
        {
            HitStop_TimelineEventData result = target as HitStop_TimelineEventData ?? new HitStop_TimelineEventData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}