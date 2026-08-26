using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [TimelineEventData(TimelineEventType.SetGravity)]
    public sealed class SetGravity_TimelineEventData : TimelineEventData
    {
        public GravityEventArgs Args = new GravityEventArgs();

        public TimelineEventType EventType => TimelineEventType.SetGravity;

        public object ArgsObject => Args;

        public bool SupportsDuration => true;

        public float DefaultDuration => 0f;

        public TimelineEventData Create()
        {
            return new SetGravity_TimelineEventData();
        }

        public TimelineEventData Clone(TimelineEventData target)
        {
            SetGravity_TimelineEventData result = target as SetGravity_TimelineEventData ?? new SetGravity_TimelineEventData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}
