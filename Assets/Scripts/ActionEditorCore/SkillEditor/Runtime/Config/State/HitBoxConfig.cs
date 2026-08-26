using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public class HitBoxConfig
    {
        public string HitBoxId = Guid.NewGuid().ToString("N");
        public string DisplayName = "攻击盒";
        public bool IsEnabled = true;
        public float TriggerTime = 0f;
        public float Duration = 0f;
        public SkillSocketSourceType SocketSource = SkillSocketSourceType.Weapon;
        public string AttachPoint = string.Empty;
        public HitBoxShapeArgs ShapeArgs = new HitBoxShapeArgs();
        public SkillEffectConfig OnHitEffect = new SkillEffectConfig();
        public HitBoxHitResponseArgs OnHitResponse = new HitBoxHitResponseArgs();
    }
}
