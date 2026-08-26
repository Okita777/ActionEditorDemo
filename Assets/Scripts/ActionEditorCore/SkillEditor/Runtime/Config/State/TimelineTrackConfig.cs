using System;
using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public class TimelineTrackConfig
    {
        public string TrackId = Guid.NewGuid().ToString("N");
        public TimelineTrackType TrackType = TimelineTrackType.Animation;
        public string DisplayName = "Track";
        public bool IsEnabled = true;

        public List<HitBoxConfig> HitBoxes = new List<HitBoxConfig>();
        public List<BulletConfig> Bullets = new List<BulletConfig>();
        public List<TimelineVfxConfig> VfxClips = new List<TimelineVfxConfig>();
        public List<TimelineAudioConfig> AudioClips = new List<TimelineAudioConfig>();
        public List<TimelineEventConfig> MetaSkillEvents = new List<TimelineEventConfig>();
    }
}
