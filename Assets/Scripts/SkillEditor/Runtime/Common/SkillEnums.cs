using System;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public enum SkillEventType
    {
        PressSkillSlot,
        ReleaseSkillSlot,
        HoldSkillSlot,
        CastingSkill,
        OnHitTarget,
        CastSkillShort,
        CastSkillLong,
        OnMetaSkillEnd,
        OnInterrupted,
    }

    [Serializable]
    public enum SkillConditionMode
    {
        All,
        Any,
    }

    [Serializable]
    public enum SkillConditionType
    {
        None,
        AttributeCompare,
        HasBuff,
        HasTag,
        LastActionSucceeded,
        LastActionFailed,
    }

    [Serializable]
    public enum SkillActionType
    {
        None = 0,
        DealDamage = 1,
        AddToughnessDamage = 2,
        AddAttribute = 3,
        AddTag = 5,
        AddBuff = 6,
        RemoveBuff = 7,
    }

    [Serializable]
    public enum SkillEffectNodeType
    {
        Sequence,
        Condition,
        Action,
    }

    [Serializable]
    public enum SkillAttributeType
    {
        CurrentHp,
        MaxHp,
        Attack,
        BreakValue,
        JumpHeightBonus,
    }

    [Serializable]
    public enum SkillCompareOperator
    {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
    }

    [Serializable]
    public enum AttributeModifyMode
    {
        AddValue,
        AddPercent,
    }

    [Serializable]
    public enum AttributeApplyLifetime
    {
        Permanent,
        TemporaryBuff,
    }

    [Serializable]
    public enum SkillQueryTargetType
    {
        Caster,
        PrimaryTarget,
    }

    [Serializable]
    public enum SkillCastCategory
    {
        Active,
        Passive,
    }

    [Serializable]
    public enum SkillSlotGroup
    {
        Active,
        Passive,
    }

    [Serializable]
    public enum SkillCostResourceType
    {
        Mana,
        Hp,
    }

    [Serializable]
    public enum BuffStackMode
    {
        None,
        AddStack,
        ExtendDuration,
    }

    [Serializable]
    public enum BuffType
    {
        None,
        FireBuff,
        PoisonBuff,
        ShieldBuff,
        AttackBuff,
        ControlBuff,
    }

    [Serializable]
    public enum TimelineTrackType
    {
        Animation,
        HitBox,
        Bullet,
        MetaSkillEvent,
        Vfx,
        Audio,
    }

    [Serializable]
    public enum TimelineEventType
    {
        None = 0,
        SoftLockTarget = 1,
        ApplyForce = 2,
        SetGravity = 3,
        AddTag = 4,
        StateGateControl = 5,
        LaunchByHeight = 6,
        HitStop = 7,
        CameraShake = 8,
        HitVfx = 9,
        HitAudio = 10,
    }

    [Serializable]
    public enum FeedbackTriggerMode
    {
        Immediate = 0,
        OnHit = 1,
    }

    public enum VfxPlaySpace
    {
        World = 0,
        FollowTarget = 1,
    }

    [Serializable]
    public enum TimelineVfxMode
    {
        OneShot = 0,
        Controlled = 1,
    }

    [Serializable]
    public enum TimelineVfxStopMode
    {
        StopEmitting = 0,
        StopAndClear = 1,
    }

    [Serializable]
    public enum TimelineFollowMode
    {
        SpawnAtSocket = 0,
        FollowSocket = 1,
    }

    public enum HitVfxRotationMode
    {
        Identity = 0,
        HitNormal = 1,
        OppositeHitDirection = 2,
        AttackerForward = 3,
        DefenderForward = 4,
    }

    public enum AudioPlaySpace
    {
        TwoD = 0,
        World = 1,
        FollowTarget = 2,
    }

    [Serializable]
    public enum StateLayerType
    {
        None = 0,
        Locomotion = 1,
        Action = 2,
    }

    [Serializable]
    public enum StatePresentationMode
    {
        None = 0,
        FullBodyOverride = 1,
        UpperBodyOverlay = 2,
        AdditiveOverlay = 3,
    }

    [Serializable]
    public enum StateAnimationSlot
    {
        None = 0,
        Locomotion = 1,
        Action = 2,
        UpperBody = 3,
        Additive = 4,
    }

    [Serializable]
    public enum AnimationLayerType
    {
        None = 0,
        Locomotion = 1,
        Action = 2,
        UpperBody = 3,
        Additive = 4,
    }

    [Serializable]
    public enum AnimationBlendMode
    {
        Override = 0,
        Additive = 1,
    }

    [Serializable]
    public enum LocomotionImpactMode
    {
        None = 0,
        KeepCurrentFacts = 1,
        LockMoveInput = 2,
        LockLocomotionDrive = 3,
        ForceSafeState = 4,
    }

    [Serializable]
    public enum StateTranslationMode
    {
        Input = 0,
        RootMotion = 1,
        Hybrid = 2,
        Locked = 3,
    }

    [Serializable]
    public enum StateRotationMode
    {
        MoveDirection = 0,
        TargetDirection = 1,
        RootMotion = 2,
        Locked = 3,
        LimitedTargetDirection = 4,
        KeepCurrent = 5,
        CameraForward = 6,
    }

    [Serializable]
    public enum GateControlType
    {
        MoveInput = 0,
        LocomotionDrive = 1,
        RotationInput = 2,
        Dash = 3,
        SkillCancel = 4,
        NextSkill = 5,
        RootMotion = 6,
    }

    [Serializable]
    public enum GateValueMode
    {
        Enable = 0,
        Disable = 1,
        Override = 2,
    }

    [Serializable]
    public enum StateTransitionPolicy
    {
        SameLayerOnly = 0,
        AllowWhitelistedCrossLayer = 1,
        ForceGlobal = 2,
    }

    [Serializable]
    public enum HitBoxDetectionType
    {
        Capsule = 0,
        Raycast = 1,
    }

    [Serializable]
    public enum SkillSocketSourceType
    {
        Character = 0,
        Weapon = 1,
    }

    [Serializable]
    public enum SkillWeaponType
    {
        None = 0,
        OneHandSword = 1,
        TwoHandSword = 2,
        Bow = 3,
        Staff = 4,
        Spear = 5,
    }

    [Serializable]
    public enum BulletFlightMode
    {
        Direct = 0,
        HomingParabola = 1,
        HomingCurve = 2,
        Parabola = 3,
    }
}
