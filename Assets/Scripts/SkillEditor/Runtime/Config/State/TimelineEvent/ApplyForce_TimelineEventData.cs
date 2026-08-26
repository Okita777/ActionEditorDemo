using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [TimelineEventData(TimelineEventType.ApplyForce)]
    public sealed class ApplyForce_TimelineEventData : TimelineEventData
    {
        public ApplyForceEventArgs Args = new ApplyForceEventArgs();

        public TimelineEventType EventType => TimelineEventType.ApplyForce;

        public object ArgsObject => Args;

        public bool SupportsDuration => true;

        public float DefaultDuration => 0f;

        public TimelineEventData Create()
        {
            return new ApplyForce_TimelineEventData();
        }

        public TimelineEventData Clone(TimelineEventData target)
        {
            ApplyForce_TimelineEventData result = target as ApplyForce_TimelineEventData ?? new ApplyForce_TimelineEventData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}
