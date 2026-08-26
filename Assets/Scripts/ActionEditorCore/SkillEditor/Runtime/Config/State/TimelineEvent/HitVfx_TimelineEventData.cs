using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    [TimelineEventData(TimelineEventType.HitVfx)]
    public sealed class HitVfx_TimelineEventData : TimelineEventData
    {
        public HitVfxEventArgs Args = new HitVfxEventArgs();
        public TimelineEventType EventType => TimelineEventType.HitVfx;
        public object ArgsObject => Args;
        public bool SupportsDuration => true;
        public float DefaultDuration => 0.3f;
        public TimelineEventData Create() => new HitVfx_TimelineEventData();
        public TimelineEventData Clone(TimelineEventData target)
        {
            HitVfx_TimelineEventData result = target as HitVfx_TimelineEventData ?? new HitVfx_TimelineEventData();
            result.Args = SkillDataFactoryUtility.CloneSerializable(Args, result.Args);
            return result;
        }
    }
}
