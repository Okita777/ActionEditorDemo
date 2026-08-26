using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [TimelineEventData(TimelineEventType.SoftLockTarget)]
    public sealed class SoftLockTarget_TimelineEventData : TimelineEventData
    {
        public SoftLockTargetEventArgs Args = new SoftLockTargetEventArgs();

        public TimelineEventType EventType => TimelineEventType.SoftLockTarget;

        public object ArgsObject => Args;

        public bool SupportsDuration => true;

        public float DefaultDuration => 0.3f;

        public TimelineEventData Create()
        {
            return new SoftLockTarget_TimelineEventData();
        }

        public TimelineEventData Clone(TimelineEventData target)
        {
            SoftLockTarget_TimelineEventData result = target as SoftLockTarget_TimelineEventData ?? new SoftLockTarget_TimelineEventData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}
