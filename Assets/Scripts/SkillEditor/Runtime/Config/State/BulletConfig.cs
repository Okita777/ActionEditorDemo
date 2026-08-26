using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public class BulletConfig
    {
        public string BulletId = Guid.NewGuid().ToString("N");
        public string DisplayName = "子弹";
        public bool IsEnabled = true;
        public float TriggerTime = 0f;
        public float Duration = 0f;
        public SkillSocketSourceType SocketSource = SkillSocketSourceType.Weapon;
        public string AttachPoint = string.Empty;
        public BulletSpawnArgs SpawnArgs = new BulletSpawnArgs();
        public SkillEffectConfig OnHitEffect = new SkillEffectConfig();
    }
}
