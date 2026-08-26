using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [TimelineEventData(TimelineEventType.CameraShake)]
    public sealed class CameraShake_TimelineEventData : TimelineEventData
    {
        public CameraShakeEventArgs Args = new CameraShakeEventArgs();

        public TimelineEventType EventType => TimelineEventType.CameraShake;
        public object ArgsObject => Args;
        public bool SupportsDuration => true;
        public float DefaultDuration => 0.3f;

        public TimelineEventData Create()
        {
            return new CameraShake_TimelineEventData();
        }

        public TimelineEventData Clone(TimelineEventData target)
        {
            CameraShake_TimelineEventData result = target as CameraShake_TimelineEventData ?? new CameraShake_TimelineEventData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}