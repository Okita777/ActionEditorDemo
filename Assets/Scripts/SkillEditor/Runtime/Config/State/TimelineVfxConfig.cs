using System;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public sealed class TimelineVfxConfig
    {
        public string VfxId = Guid.NewGuid().ToString("N");
        public string DisplayName = "VFX";
        public bool IsEnabled = true;
        public float TriggerTime;
        public float Duration;
        public string PrefabPath = string.Empty;
        public SkillSocketSourceType SocketSource = SkillSocketSourceType.Character;
        public string AttachPoint = string.Empty;
        public TimelineFollowMode FollowMode = TimelineFollowMode.SpawnAtSocket;
        public SkillVector3 PositionOffset = Vector3.zero;
        public SkillVector3 RotationOffset = Vector3.zero;
        public SkillVector3 Scale = Vector3.one;
        public TimelineVfxMode Mode = TimelineVfxMode.OneShot;
        public TimelineVfxStopMode StopMode = TimelineVfxStopMode.StopEmitting;
        public float TailTimeout = 2f;
        public bool UseUnscaledTime = true;
    }
}