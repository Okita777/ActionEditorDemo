using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public struct SkillVector3
    {
        public float x;
        public float y;
        public float z;

        public SkillVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public SkillVector3(Vector3 value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
        }

        public void SetValue(Vector3 value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
        }

        public Vector3 GetValue()
        {
            return new Vector3(x, y, z);
        }

        public static implicit operator Vector3(SkillVector3 value)
        {
            return value.GetValue();
        }

        public static implicit operator SkillVector3(Vector3 value)
        {
            return new SkillVector3(value);
        }
    }

    [Serializable]
    public class TagContainer
    {
        public List<string> Tags = new List<string>();

        public bool HasTag(string tag)
        {
            return !string.IsNullOrEmpty(tag) && Tags.Contains(tag);
        }
    }

    [Serializable]
    public class AttributeCompareArgs
    {
        public SkillQueryTargetType QueryTarget = SkillQueryTargetType.Caster;
        public SkillAttributeType AttributeType = SkillAttributeType.CurrentHp;
        public SkillCompareOperator CompareOperator = SkillCompareOperator.Greater;
        public float Value = 0f;
    }

    [Serializable]
    public class BuffConditionArgs
    {
        public SkillQueryTargetType QueryTarget = SkillQueryTargetType.PrimaryTarget;
        public string BuffId = string.Empty;//todo
    }

    [Serializable]
    public class TagConditionArgs
    {
        public SkillQueryTargetType QueryTarget = SkillQueryTargetType.PrimaryTarget;
        public string Tag = string.Empty;
    }

    [Serializable]
    public class ActionResultConditionArgs
    {
        // 预留给后续扩展：如果以后效果树中一个 Sequence 有多个 Action，
        // 可以通过索引回看上一个或指定 Action 的执行结果。
        public int ActionIndex = -1;
    }

    [Serializable]
    public class DamageActionArgs
    {
        public SkillQueryTargetType QueryTarget = SkillQueryTargetType.PrimaryTarget;
        public SkillAttributeType SourceAttribute = SkillAttributeType.Attack;
        public string ActionId = "damage";
        public string Description = string.Empty;
        public float Ratio = 1f;
    }

    [Serializable]
    public class AttributeActionArgs
    {
        public SkillQueryTargetType QueryTarget = SkillQueryTargetType.PrimaryTarget;
        public SkillAttributeType AttributeType = SkillAttributeType.Attack;
        public AttributeModifyMode ModifyMode = AttributeModifyMode.AddValue;
        public AttributeApplyLifetime ApplyLifetime = AttributeApplyLifetime.Permanent;
        public float Value = 0f;
    }

    [Serializable]
    public class BuffActionArgs
    {
        public SkillQueryTargetType QueryTarget = SkillQueryTargetType.PrimaryTarget;
        public string BuffId = string.Empty;
        public float Duration = 0f;
    }

    [Serializable]
    public class TagActionArgs
    {
        public SkillQueryTargetType QueryTarget = SkillQueryTargetType.PrimaryTarget;
        public List<string> Tags = new List<string>();
        public int Stack = 1;
        public AttributeApplyLifetime ApplyLifetime = AttributeApplyLifetime.TemporaryBuff;
    }

    [Serializable]
    public class SoftLockTargetEventArgs
    {
        public float Radius = 6f;
        public float Angle = 120f;
        public int LayerMask = ~0;
        public float RotationSpeed = 12f;
        [OptionalField] public bool ReferToCamera = true;
        [OptionalField] public int Priority = 1;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            ReferToCamera = true;
            Priority = 1;
        }
    }

    [Serializable]
    public sealed class HitStopEventArgs
    {
        public FeedbackTriggerMode TriggerMode = FeedbackTriggerMode.OnHit;
        public float AttackerDuration = 0.05f;
        public float DefenderDuration = 0.07f;
        public float AttackerTimeScale = 0f;
        public float DefenderTimeScale = 0f;
        public bool AffectAttacker = true;
        public bool AffectDefender = true;
        public bool TriggerOncePerEvent = true;
        public bool MergeSameFrameHits = true;
        public int Priority = 0;
    }

    [Serializable]
    public sealed class CameraShakeEventArgs
    {
        public FeedbackTriggerMode TriggerMode = FeedbackTriggerMode.OnHit;
        public float Amplitude = 0.35f;
        public float Frequency = 8f;
        public float ShakeDuration = 0.12f;
        public SkillVector3 Direction = new Vector3(0f, 0f, -1f);
        public bool UseHitDirection = true;
        public bool TriggerOncePerEvent = true;
        public bool MergeSameFrameHits = true;
    }

    [Serializable]
    public sealed class HitVfxEventArgs
    {
        public FeedbackTriggerMode TriggerMode = FeedbackTriggerMode.OnHit;
        public string PrefabPath = string.Empty;
        public VfxPlaySpace Space = VfxPlaySpace.World;
        public HitVfxRotationMode RotationMode = HitVfxRotationMode.HitNormal;
        public SkillVector3 PositionOffset = Vector3.zero;
        public SkillVector3 RotationOffset = Vector3.zero;
        public SkillVector3 Scale = Vector3.one;
        public float Lifetime = 1f;
        public bool UseUnscaledTime = true;
        public bool TriggerOncePerEvent = true;
        public bool MergeSameFrameHits = true;
    }

    [Serializable]
    public sealed class HitAudioEventArgs
    {
        public FeedbackTriggerMode TriggerMode = FeedbackTriggerMode.OnHit;
        public string AudioClipPath = string.Empty;
        public string AudioMixerPath = string.Empty;
        public string MixerGroupName = string.Empty;
        public AudioPlaySpace Space = AudioPlaySpace.World;
        public float Volume = 1f;
        public float Pitch = 1f;
        public float SpatialBlend = 1f;
        public float MinDistance = 1f;
        public float MaxDistance = 30f;
        public bool TriggerOncePerEvent = true;
        public bool MergeSameFrameHits = true;
    }

    [Serializable]
    public class ApplyForceEventArgs
    {
        public SkillVector3 Force = new Vector3(0f, 0f, 3f);
        public bool UseLocalSpace = true;
        public bool OverrideRecoveryAnimation;
    }

    [Serializable]
    public class GravityEventArgs
    {
        public bool EnableGravity = true;
        public bool OverrideGravityVector = true;
        public SkillVector3 Gravity = new Vector3(0f, -9.81f, 0f);
        public bool OverrideRecoveryAnimation;
    }

    [Serializable]
    public class LaunchByHeightEventArgs
    {
        public float TargetHeight = 1.6f;
        public bool UseHeightBonusAttribute = true;
        public SkillAttributeType HeightBonusAttribute = SkillAttributeType.JumpHeightBonus;
        public float AttributeScale = 1f;
        public float ForceUngroundDuration = 0.1f;
    }

    [Serializable]
    public class AddTagEventArgs
    {
        public List<string> Tags = new List<string>();
        public int Stack = 1;
    }

    [Serializable]
    public class StateGateControlEventArgs
    {
        public GateControlType GateType = GateControlType.MoveInput;
        public GateValueMode ValueMode = GateValueMode.Disable;
        public bool Value = false;
        public string SourceToken = string.Empty;
        public string Notes = string.Empty;
    }

    [Serializable]
    public class HitBoxShapeArgs
    {
        public HitBoxDetectionType DetectionType = HitBoxDetectionType.Capsule;
        public SkillVector3 Center = Vector3.zero;
        public SkillVector3 Rotation = Vector3.zero;
        public SkillVector3 Size = new Vector3(1f, 0.2f, 0f);
        public int BakeCount = 0;
        public List<HitBoxBakedPart> BakedParts = new List<HitBoxBakedPart>();
        public float HitInterval = 0f;
        public int HitLayerMask = ~0;

        public Vector3 OffsetPosition
        {
            get => Center;
            set => Center = value;
        }

        public Vector3 OffsetRotation
        {
            get => Rotation;
            set => Rotation = value;
        }

        public Vector3 Scale
        {
            get => Size;
            set => Size = value;
        }
    }

    [Serializable]
    public class HitBoxBakedPart
    {
        public SkillVector3 StartPos = Vector3.zero;
        public SkillVector3 Direction = Vector3.forward;
        public float TriggerTime = 0f;
    }

    [Serializable]
    public class HitBoxHitResponseArgs
    {
        public float ToughnessDamage = 0f;
        public float HitStunDuration = 0f;
        public string HitStunTag = string.Empty;
    }

    [Serializable]
    public class BulletSpawnArgs
    {
        public string BulletType = string.Empty;
        public string BulletPrefabPath = string.Empty;
        public SkillVector3 PositionOffset = Vector3.zero;
        public SkillVector3 RotationOffset = Vector3.zero;
        public BulletFlightMode FlightMode = BulletFlightMode.Direct;
        public int SpawnCount = 1;
        public float Speed = 12f;
        public float MaxLifetime = 3f;
        public int HitLayerMask = ~0;
        public float CollisionRadius = 0.1f;
        public BulletParabolaArgs Parabola = new BulletParabolaArgs();
        public BulletTrackingArgs Tracking = new BulletTrackingArgs();
    }

    [Serializable]
    public class BulletParabolaArgs
    {
        public float InitialVerticalSpeed = 4f;
        public float Gravity = 9.8f;
    }

    [Serializable]
    public class BulletTrackingArgs
    {
        public float SearchRange = 5f;
        public float SearchAngle = 60f;
        public float CenterWeight = 0.5f;
        public float Acceleration = 0f;
        public float StraightDistance = 1f;
        public float CurveStrength = 1f;
        public float CurveLateralOffset = 1.5f;
        public float CurveVerticalOffset = 0.75f;
        public float CurveOscillation = 1.25f;
        public float LaunchYawRange = 45f;
        public float LaunchPitchRange = 25f;
    }
}
