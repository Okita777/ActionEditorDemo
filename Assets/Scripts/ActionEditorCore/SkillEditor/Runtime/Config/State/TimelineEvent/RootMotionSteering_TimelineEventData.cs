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

    [Serializable]
    public sealed class RotationModeOverrideEventArgs
    {
        public StateRotationMode RotationMode = StateRotationMode.MoveDirection;
        public float BlendDuration = 0.15f;
        public float DirectionTurnSpeed = 540f;
    }

    [Serializable]
    [TimelineEventData(TimelineEventType.RotationModeOverride)]
    public sealed class RotationModeOverride_TimelineEventData : TimelineEventData
    {
        public RotationModeOverrideEventArgs Args = new RotationModeOverrideEventArgs();

        public TimelineEventType EventType => TimelineEventType.RotationModeOverride;
        public object ArgsObject => Args;
        public bool SupportsDuration => true;
        public float DefaultDuration => 0.5f;

        public TimelineEventData Create()
        {
            return new RotationModeOverride_TimelineEventData();
        }

        public TimelineEventData Clone(TimelineEventData target)
        {
            RotationModeOverride_TimelineEventData result = target as RotationModeOverride_TimelineEventData ??
                new RotationModeOverride_TimelineEventData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}