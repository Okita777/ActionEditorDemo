using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [TimelineEventData(TimelineEventType.StateGateControl)]
    public sealed class StateGateControl_TimelineEventData : TimelineEventData
    {
        public StateGateControlEventArgs Args = new StateGateControlEventArgs();

        public TimelineEventType EventType => TimelineEventType.StateGateControl;

        public object ArgsObject => Args;

        public bool SupportsDuration => true;

        public float DefaultDuration => 0f;

        public TimelineEventData Create()
        {
            return new StateGateControl_TimelineEventData();
        }

        public TimelineEventData Clone(TimelineEventData target)
        {
            StateGateControl_TimelineEventData result = target as StateGateControl_TimelineEventData ?? new StateGateControl_TimelineEventData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}