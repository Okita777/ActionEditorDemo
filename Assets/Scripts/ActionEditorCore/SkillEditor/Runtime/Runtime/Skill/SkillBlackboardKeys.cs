namespace AsiSkillEditor.RunTime
{
    public static class SkillBlackboardKeys
    {
        public const string DebugTimelineTime = "Debug.TimelineTime";
        public const string DebugLastTimelineItemType = "Debug.LastTimelineItemType";
        public const string DebugLastTimelineItemId = "Debug.LastTimelineItemId";
        public const string DebugLastEffectNodeId = "Debug.LastEffectNodeId";

        public const string LastSpawnedHitBox = "LastSpawnedHitBox";
        public const string ActiveHitBox = "Active.HitBox";
        public const string ActiveHitBoxId = "Active.HitBoxId";
        public const string LastHitBoxTarget = "HitBox.LastTarget";
        public const string LastHitBoxTargetObject = "HitBox.LastTargetObject";
        public const string LastHitBoxTargetCollider = "HitBox.LastTargetCollider";
        public const string LastHitBoxTargetPoint = "HitBox.LastTargetPoint";
        public const string LastHitBoxTargetCount = "HitBox.LastTargetCount";
        public const string LastHitBoxEffectResult = "HitBox.LastEffectResult";
        public const string LastHitBoxToughnessPlaceholder = "HitBox.LastToughnessPlaceholder";
        public const string LastHitBoxHitStunPlaceholder = "HitBox.LastHitStunPlaceholder";
        public const string LastSpawnedBullet = "LastSpawnedBullet";
        public const string LastBulletSocketTransform = "Bullet.LastSocketTransform";
        public const string LastBulletSpawnPosition = "Bullet.LastSpawnPosition";
        public const string LastBulletSpawnRotation = "Bullet.LastSpawnRotation";
        public const string LastBulletTarget = "Bullet.LastTarget";
        public const string LastBulletTargetObject = "Bullet.LastTargetObject";
        public const string LastBulletTargetCollider = "Bullet.LastTargetCollider";
        public const string LastBulletTargetPoint = "Bullet.LastTargetPoint";
        public const string LastBulletEffectResult = "Bullet.LastEffectResult";
        public const string LastTimelineEvent = "LastTimelineEvent";
        public const string LastTimelineEventType = "LastTimelineEventType";
        public const string LastTimelineEventId = "LastTimelineEventId";
        public const string LastTimelineEventError = "LastTimelineEventError";
        public const string TimelineEventElapsedTime = "TimelineEvent.ElapsedTime";
        public const string TimelineEventNormalizedTime = "TimelineEvent.NormalizedTime";
        public const string TimelineEventSoftLockTargetArgs = "TimelineEvent.SoftLockTarget.Args";
        public const string TimelineEventSoftLockTargetActive = "TimelineEvent.SoftLockTarget.Active";
        public const string TimelineEventSoftLockTargetRadius = "TimelineEvent.SoftLockTarget.Radius";
        public const string TimelineEventSoftLockTargetAngle = "TimelineEvent.SoftLockTarget.Angle";
        public const string TimelineEventSoftLockTargetLayerMask = "TimelineEvent.SoftLockTarget.LayerMask";
        public const string TimelineEventSoftLockTargetRotationSpeed = "TimelineEvent.SoftLockTarget.RotationSpeed";
        public const string TimelineEventApplyForceArgs = "TimelineEvent.ApplyForce.Args";
        public const string TimelineEventApplyForceVector = "TimelineEvent.ApplyForce.Vector";
        public const string TimelineEventApplyForceUseLocalSpace = "TimelineEvent.ApplyForce.UseLocalSpace";
        public const string MetaSkillEventSetGravityArgs = "MetaSkillEvent.SetGravity.Args";
        public const string MetaSkillEventSetGravityEnabled = "MetaSkillEvent.SetGravity.Enabled";
        public const string MetaSkillEventSetGravityVector = "MetaSkillEvent.SetGravity.Vector";
        public const string MetaSkillEventAddTagArgs = "MetaSkillEvent.AddTag.Args";
        public const string MetaSkillEventAddTagTarget = "MetaSkillEvent.AddTag.Target";
        public const string MetaSkillEventAddTagTag = "MetaSkillEvent.AddTag.Tag";
        public const string MetaSkillEventAddTagTags = "MetaSkillEvent.AddTag.Tags";
        public const string MetaSkillEventAddTagStack = "MetaSkillEvent.AddTag.Stack";

        public static string AttributeKey(SkillQueryTargetType queryTarget, SkillAttributeType attributeType)
        {
            return string.Format("Attribute.{0}.{1}", queryTarget, attributeType);
        }
    }
}
