using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ActionEditor.TagSystem;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    /// <summary>仅用于读取旧 BinaryFormatter 数据；纯视角硬锁定不消费该值。</summary>
    [Serializable, Obsolete("硬锁定固定为 Camera Only；该类型仅保留旧二进制兼容。")]
    public enum HardLockFacingMode
    {
        CameraAndCharacter = 0,
        CameraOnly = 1,
    }

    [Serializable]
    public sealed class UnitActiveSkillSlotConfig
    {
        public int SlotIndex = 1;
        public string DisplayName = "主动技能槽";
        public string ActionName = string.Empty;
        public string SkillId = string.Empty;
    }

    [Serializable]
    public sealed class UnitPassiveSkillSlotConfig
    {
        public int SlotIndex = 1;
        public string DisplayName = "被动技能槽";
        public string SkillId = string.Empty;
    }

    [Serializable]
    public sealed class UnitLayerDefaultStateConfig
    {
        public StateLayerType Layer = StateLayerType.Locomotion;
        public string DefaultStateId = string.Empty;
    }

    [Serializable]
    public enum StateAnimationMode
    {
        SingleClip = 0,
        DirectionalMixer2D = 1,
    }

    /// <summary>
    /// 可同时被 JsonUtility 和 BinaryFormatter 持久化的二维值。
    /// UnityEngine.Vector2 未标记 Serializable，不能直接进入运行时 byte 对象图。
    /// </summary>
    [Serializable]
    public struct SerializableVector2
    {
        public float x;
        public float y;

        public SerializableVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static implicit operator Vector2(SerializableVector2 value)
        {
            return new Vector2(value.x, value.y);
        }

        public static implicit operator SerializableVector2(Vector2 value)
        {
            return new SerializableVector2(value.x, value.y);
        }
    }

    [Serializable]
    public sealed class StateDirectionalMixer2DConfig
    {
        [OptionalField] public string IdleClipPath = string.Empty;
        public string ForwardClipPath = string.Empty;
        public string ForwardRightClipPath = string.Empty;
        public string RightClipPath = string.Empty;
        public string BackRightClipPath = string.Empty;
        public string BackClipPath = string.Empty;
        public string BackLeftClipPath = string.Empty;
        public string LeftClipPath = string.Empty;
        public string ForwardLeftClipPath = string.Empty;

        [OptionalField] public SerializableVector2 IdleThreshold = new SerializableVector2(0f, 0f);
        public SerializableVector2 ForwardThreshold = new SerializableVector2(0f, 1f);
        public SerializableVector2 ForwardRightThreshold = new SerializableVector2(0.7071f, 0.7071f);
        public SerializableVector2 RightThreshold = new SerializableVector2(1f, 0f);
        public SerializableVector2 BackRightThreshold = new SerializableVector2(0.7071f, -0.7071f);
        public SerializableVector2 BackThreshold = new SerializableVector2(0f, -1f);
        public SerializableVector2 BackLeftThreshold = new SerializableVector2(-0.7071f, -0.7071f);
        public SerializableVector2 LeftThreshold = new SerializableVector2(-1f, 0f);
        public SerializableVector2 ForwardLeftThreshold = new SerializableVector2(-0.7071f, 0.7071f);

        public float ParameterSmoothSpeed = 18f;

        public static StateDirectionalMixer2DConfig CreateDefault()
        {
            return new StateDirectionalMixer2DConfig();
        }
    }

    [Serializable]
    public sealed class StateAnimationProfile
    {
        public AnimationLayerType OutputLayer = AnimationLayerType.Locomotion;
        public bool OverrideLowerLayers;
        public float LayerWeight = 1f;
        public float Speed = 1f;
        public bool ApplyRootMotion;
        public bool MatchLocomotionSpeed;
        public float AuthoredMoveSpeed = 6f;
        public float MinLocomotionPlaybackSpeed = 0.85f;
        public float MaxLocomotionPlaybackSpeed = 1.15f;
        public float LocomotionSpeedMatchSharpness = 18f;
        public float LocomotionSpeedMatchDeadZone = 0.01f;
    }

    [Serializable]
    public sealed class UnitLocomotionConfig
    {
        public float MaxMoveSpeed = 7f;
        public float GroundAcceleration = 70f;
        public float GroundDeceleration = 100f;
        public float DirectionChangeAcceleration = 140f;
        [OptionalField] public float HardTurnAngle = 100f;
        [OptionalField] public float HardTurnSpeedRetention;
        public float AirAcceleration = 16f;
        public float MaxAirSpeed = 4f;
        public float TurnSpeed = 1440f;
        [OptionalField] public float AirTurnSpeed = 720f;
        [OptionalField] public float HardTurnSpeed = 2160f;
        [OptionalField] public bool SnapFacingOnHardTurn = true;
        [OptionalField] public int StableGroundLayers = -1;
        public bool EnableGravity = true;
        public float Gravity = 25f;
        public float MaxFallSpeed = 40f;
        public float ExternalVelocityDrag = 4f;
    }

    /// <summary>
    /// 单位级技能后摇取消策略。
    /// Recovery 一旦进入即按此策略全程可取消，不要求每个技能重复配置中断轨道。
    /// </summary>
    [Serializable]
    public sealed class UnitRecoveryCancelPolicy
    {
        public bool AllowSkillCancel = true;
        public bool AllowMoveCancel = true;
        public bool AllowHitReactionCancel = true;
        public bool AllowForcedCancel = true;
    }

    [Serializable]
    public sealed class StateMovementProfile
    {
        public StateTranslationMode TranslationMode = StateTranslationMode.Input;
        public StateRotationMode RotationMode = StateRotationMode.MoveDirection;
        public float InputSpeedMultiplier = 1f;
        public float AccelerationMultiplier = 1f;
        public float DecelerationMultiplier = 1f;
        public float RootMotionForwardWeight = 1f;
        public float RootMotionSideWeight = 1f;
        public float RootMotionVerticalWeight;
        public float RootMotionRotationWeight = 1f;
        public bool AllowBackwardRootMotion = true;
        public float MaxTurnSpeed;
        public bool AllowGravity = true;
        public float AirControl = 1f;

        public static StateMovementProfile CreateDefault()
        {
            return new StateMovementProfile();
        }

        public static StateMovementProfile CreateLocked()
        {
            return new StateMovementProfile
            {
                TranslationMode = StateTranslationMode.Locked,
                RotationMode = StateRotationMode.Locked,
            };
        }
    }

    [Serializable]
    public sealed class UnitAnimationLayerConfig
    {
        public AnimationLayerType Layer = AnimationLayerType.Locomotion;
        public int AnimancerLayerIndex;
        public AnimationBlendMode BlendMode = AnimationBlendMode.Override;
        public string AvatarMaskAssetPath = string.Empty;
        public float DefaultWeight;
    }

    /// <summary>纯视角硬锁定参数。所有 LayerMask 以 int 保存，兼容 JsonUtility 与 BinaryFormatter。</summary>
    [Serializable]
    public sealed class UnitHardLockConfig
    {
        public float SearchRadius = 20f;
        public float HorizontalFanAngle = 120f;
        [OptionalField] public float ViewPivotHeightOffset = 1f;
        public int TargetLayers = -1;
        public int ObstacleLayers;
        public float DistanceWeight = 0.5f;
        public float AngleWeight = 0.5f;
        public float OcclusionUnlockDelay = 0.75f;
        public float UnlockRadius = 25f;
        public string ToggleAction = string.Empty;
        public string SwitchLeftAction = string.Empty;
        public string SwitchRightAction = string.Empty;
        public string SwitchFartherAction = string.Empty;
        public string SwitchNearerAction = string.Empty;
    }

    [Serializable]
    public sealed class UnitConfig
    {
        public string UnitId = "unit_001";
        public string DisplayName = "New Unit";
        public string DefaultStateId = string.Empty;
        public string PrefabAssetPath = string.Empty;
        public string CameraResourcePath = string.Empty;
        [OptionalField] public UnitHardLockConfig HardLock = new UnitHardLockConfig();
    #pragma warning disable CS0618
        [OptionalField, Obsolete("硬锁定固定为 Camera Only；该字段仅保留旧二进制兼容。")]
        public HardLockFacingMode DefaultHardLockFacingMode = HardLockFacingMode.CameraOnly;
    #pragma warning restore CS0618
        public string AnimationDirectory = string.Empty;
        public UnitLocomotionConfig Locomotion = new UnitLocomotionConfig();
        [OptionalField] public UnitRecoveryCancelPolicy RecoveryCancel = new UnitRecoveryCancelPolicy();
        public List<UnitLayerDefaultStateConfig> LayerDefaultStates = new List<UnitLayerDefaultStateConfig>();
        public List<UnitAnimationLayerConfig> AnimationLayers = new List<UnitAnimationLayerConfig>();
        public List<UnitActiveSkillSlotConfig> ActiveSkillSlots = new List<UnitActiveSkillSlotConfig>();
        public List<UnitPassiveSkillSlotConfig> PassiveSkillSlots = new List<UnitPassiveSkillSlotConfig>();
    }

    [Serializable]
    public sealed class StateConfig : IRuntimeTagContainerOwner
    {
        public string StateId = "state_001";
        public string StateName = "New State";
        public string AnimationClipPath = string.Empty;
        [OptionalField] public StateAnimationMode AnimationMode = StateAnimationMode.SingleClip;
        [OptionalField] public StateDirectionalMixer2DConfig DirectionalMixer2D = new StateDirectionalMixer2DConfig();
        public StateAnimationProfile AnimationProfile = new StateAnimationProfile();
        public bool AffectsLocomotion = true;
        public StateMovementProfile MovementProfile = StateMovementProfile.CreateDefault();
        public string DefaultNextStateId = string.Empty;
        public StateTimelineConfig Timeline = new StateTimelineConfig();
        public TagContainer Tags = new TagContainer();

        public StateLayerType Layer = StateLayerType.Locomotion;
        public StatePresentationMode PresentationMode = StatePresentationMode.FullBodyOverride;
        public StateAnimationSlot PrimaryAnimationSlot = StateAnimationSlot.Locomotion;

        public bool ControlsMovement = false;
        public bool ControlsRotation = false;
        public bool BlocksLocomotionAnimation = false;
        public LocomotionImpactMode LocomotionImpactMode = LocomotionImpactMode.None;

        public bool IsLayerDefaultState = false;
        public bool IsActionReleaseState = false;
        public string SafeFallbackStateId = string.Empty;

        [NonSerialized] private RuntimeTagContainer _runtimeTags;

        public RuntimeTagContainer RuntimeTags => _runtimeTags ??= new RuntimeTagContainer();
    }
}