using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [TimelineEventData(TimelineEventType.HitAudio)]
    public sealed class HitAudio_TimelineEventData : TimelineEventData
    {
        public HitAudioEventArgs Args = new HitAudioEventArgs();
        public TimelineEventType EventType => TimelineEventType.HitAudio;
        public object ArgsObject => Args;
        public bool SupportsDuration => true;
        public float DefaultDuration => 0.3f;
        public TimelineEventData Create() => new HitAudio_TimelineEventData();
        public TimelineEventData Clone(TimelineEventData target)
        {
            HitAudio_TimelineEventData result = target as HitAudio_TimelineEventData ?? new HitAudio_TimelineEventData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}
