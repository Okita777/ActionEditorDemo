using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public sealed class RootMotionSteeringEventArgs
    {
        public float TranslationWeightFrom;
        public float TranslationWeightTo = 1f;
        public float RotationWeightFrom;
        public float RotationWeightTo = 1f;
        public float TranslationSteeringSpeed = 720f;
        public float RotationSteeringSpeed = 540f;
    }

    [Serializable]
    [TimelineEventData(TimelineEventType.RootMotionSteering)]
    public sealed class RootMotionSteering_TimelineEventData : TimelineEventData
    {
        public RootMotionSteeringEventArgs Args = new RootMotionSteeringEventArgs();

        public TimelineEventType EventType => TimelineEventType.RootMotionSteering;
        public object ArgsObject => Args;
        public bool SupportsDuration => true;
        public float DefaultDuration => 0.5f;

        public TimelineEventData Create()
        {
            return new RootMotionSteering_TimelineEventData();
        }

        public TimelineEventData Clone(TimelineEventData target)
        {
            RootMotionSteering_TimelineEventData result = target as RootMotionSteering_TimelineEventData ??
                new RootMotionSteering_TimelineEventData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}