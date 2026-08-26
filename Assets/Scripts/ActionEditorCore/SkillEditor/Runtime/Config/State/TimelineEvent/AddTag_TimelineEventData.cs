using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [TimelineEventData(TimelineEventType.AddTag)]
    public sealed class AddTag_TimelineEventData : TimelineEventData
    {
        public AddTagEventArgs Args = new AddTagEventArgs();

        public TimelineEventType EventType => TimelineEventType.AddTag;

        public object ArgsObject => Args;

        public bool SupportsDuration => true;

        public float DefaultDuration => 0f;

        public TimelineEventData Create()
        {
            return new AddTag_TimelineEventData();
        }

        public TimelineEventData Clone(TimelineEventData target)
        {
            AddTag_TimelineEventData result = target as AddTag_TimelineEventData ?? new AddTag_TimelineEventData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}
