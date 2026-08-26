using System;

namespace AsiSkillEditor.RunTime
{
    /// <summary>
    /// 状态时间线上的一次性音效配置。
    /// 音效在 TriggerTime 播放，并在状态结束或中断后继续自然播放完毕。
    /// </summary>
    [Serializable]
    public sealed class TimelineAudioConfig
    {
        public string AudioId = Guid.NewGuid().ToString("N");
        public string DisplayName = "Audio";
        public bool IsEnabled = true;
        public float TriggerTime;
        public float Duration;
        public string AudioClipPath = string.Empty;
        public string AudioMixerPath = string.Empty;
        public string MixerGroupName = string.Empty;
        public SkillSocketSourceType SocketSource = SkillSocketSourceType.Character;
        public string AttachPoint = string.Empty;
        public AudioPlaySpace Space = AudioPlaySpace.World;
        public float Volume = 1f;
        public float Pitch = 1f;
        public float SpatialBlend = 1f;
        public float MinDistance = 1f;
        public float MaxDistance = 30f;
    }
}
