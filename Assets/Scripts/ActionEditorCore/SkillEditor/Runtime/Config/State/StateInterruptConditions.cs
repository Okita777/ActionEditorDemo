using System;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    public enum StateKeyTriggerMode
    {
        Down = 0,
        Hold = 1,
        Up = 2,
        ShortRelease = 3,
        LongPressStart = 4,
        LongPressRelease = 5,
        /// <summary>动作当前未被按住的持续状态；不同于仅在释放边沿成立一帧的 Up。</summary>
        Release = 6,
    }

    public enum StateMoveInputMode
    {
        Active = 0,
        Started = 1,
        Stopped = 2,
        Inactive = 3,
    }

    public enum StateGroundingMode
    {
        StableGrounded,
        NotStableGrounded,
        JustLanded,
        JustLeftStableGround,
        WithinCoyoteTime,
    }

    public enum StateMotionValue
    {
        VerticalSpeed,
        PlanarSpeed,
        LandingVerticalSpeed,
    }

    public enum StateFloatComparison
    {
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
    }

    [Serializable]
    public sealed class StateKeyInterruptCondition : IStateInterruptCondition
    {
        public string ActionName = string.Empty;
        public StateKeyTriggerMode TriggerMode = StateKeyTriggerMode.Down;

        public string GetDisplayName()
        {
            return string.IsNullOrWhiteSpace(ActionName)
                ? $"输入动作 ({TriggerMode})"
                : $"输入:{ActionName} ({TriggerMode})";
        }

        public bool Evaluate(StateInterruptContext context)
        {
            if (context == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(ActionName))
            {
                switch (TriggerMode)
                {
                    case StateKeyTriggerMode.Down:
                        return context.InputSnapshot.DownActions != null && context.InputSnapshot.DownActions.Contains(ActionName);
                    case StateKeyTriggerMode.Hold:
                        return context.InputSnapshot.HeldActions != null && context.InputSnapshot.HeldActions.Contains(ActionName);
                    case StateKeyTriggerMode.Up:
                        return context.InputSnapshot.UpActions != null && context.InputSnapshot.UpActions.Contains(ActionName);
                    case StateKeyTriggerMode.ShortRelease:
                        return context.InputSnapshot.ShortReleasedActions != null && context.InputSnapshot.ShortReleasedActions.Contains(ActionName);
                    case StateKeyTriggerMode.LongPressStart:
                        return context.InputSnapshot.LongPressStartedActions != null && context.InputSnapshot.LongPressStartedActions.Contains(ActionName);
                    case StateKeyTriggerMode.LongPressRelease:
                        return context.InputSnapshot.LongPressReleasedActions != null && context.InputSnapshot.LongPressReleasedActions.Contains(ActionName);
                    case StateKeyTriggerMode.Release:
                        return context.InputSnapshot.HeldActions == null || !context.InputSnapshot.HeldActions.Contains(ActionName);
                    default:
                        return false;
                }
            }

            return false;
        }

        public IStateInterruptCondition Clone()
        {
            return new StateKeyInterruptCondition
            {
                ActionName = ActionName,
                TriggerMode = TriggerMode,
            };
        }
    }

    [Serializable]
    public sealed class StateMoveInputInterruptCondition : IStateInterruptCondition
    {
        public StateMoveInputMode MoveInputMode = StateMoveInputMode.Active;
        [Min(0f)] public float MinimumDuration;

        public string GetDisplayName()
        {
            return MinimumDuration > 0f
                ? $"移动输入:{MoveInputMode} 持续 >= {MinimumDuration:0.###}s"
                : $"移动输入:{MoveInputMode}";
        }

        public bool Evaluate(StateInterruptContext context)
        {
            if (context == null)
            {
                return false;
            }

            switch (MoveInputMode)
            {
                case StateMoveInputMode.Started:
                    return context.InputSnapshot.IsMoveInput && !context.InputSnapshot.IsMoveInputPre;
                case StateMoveInputMode.Stopped:
                    return !context.InputSnapshot.IsMoveInput && context.InputSnapshot.IsMoveInputPre;
                case StateMoveInputMode.Inactive:
                    return !context.InputSnapshot.IsMoveInput &&
                           context.InputSnapshot.MoveInputInactiveDuration >= Mathf.Max(0f, MinimumDuration);
                default:
                    return context.InputSnapshot.IsMoveInput &&
                           context.InputSnapshot.MoveInputActiveDuration >= Mathf.Max(0f, MinimumDuration);
            }
        }

        public IStateInterruptCondition Clone()
        {
            return new StateMoveInputInterruptCondition
            {
                MoveInputMode = MoveInputMode,
                MinimumDuration = MinimumDuration,
            };
        }
    }

    /// <summary>
    /// 当角色逻辑前向与当前世界空间输入方向的平面夹角大于阈值时成立。
    /// 没有有效移动输入时不成立，避免静止状态误触发转身。
    /// </summary>
    [Serializable]
    public sealed class CompareCharacterForwardToInputInterruptCondition : IStateInterruptCondition
    {
        [Range(0f, 180f)] public float AngleThreshold = 90f;

        public string GetDisplayName()
        {
            return $"角色前向与输入夹角 > {Mathf.Clamp(AngleThreshold, 0f, 180f):0.###}°";
        }

        public bool Evaluate(StateInterruptContext context)
        {
            StateRuntimeContext runtimeContext = context?.RuntimeContext;
            if (runtimeContext?.CharacterForwardProvider == null || runtimeContext.MoveInputDirectionProvider == null)
            {
                return false;
            }

            Vector3 forward = runtimeContext.CharacterForwardProvider();
            Vector3 inputDirection = runtimeContext.MoveInputDirectionProvider();
            Vector3 up = runtimeContext.Unit != null ? runtimeContext.Unit.transform.up : Vector3.up;
            forward = Vector3.ProjectOnPlane(forward, up);
            inputDirection = Vector3.ProjectOnPlane(inputDirection, up);
            if (forward.sqrMagnitude <= Mathf.Epsilon || inputDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            float angle = Vector3.Angle(forward, inputDirection);
            return angle > Mathf.Clamp(AngleThreshold, 0f, 180f);
        }

        public IStateInterruptCondition Clone()
        {
            return new CompareCharacterForwardToInputInterruptCondition
            {
                AngleThreshold = AngleThreshold,
            };
        }
    }

    [Serializable]
    public sealed class StateHitInterruptCondition : IStateInterruptCondition
    {
        public string GetDisplayName()
        {
            return "命中目标";
        }

        public bool Evaluate(StateInterruptContext context)
        {
            return context != null && context.HitSnapshot.HasHit;
        }

        public IStateInterruptCondition Clone()
        {
            return new StateHitInterruptCondition();
        }
    }

    [Serializable]
    public sealed class StateBeHitInterruptCondition : IStateInterruptCondition
    {
        public string GetDisplayName()
        {
            return "受到命中";
        }

        public bool Evaluate(StateInterruptContext context)
        {
            return context != null && context.BeHitSnapshot.WasHit;
        }

        public IStateInterruptCondition Clone()
        {
            return new StateBeHitInterruptCondition();
        }
    }

    [Serializable]
    public sealed class StateTagInterruptCondition : IStateInterruptCondition
    {
        public string Tag = string.Empty;
        public bool Inverse = false;

        public string GetDisplayName()
        {
            return string.IsNullOrWhiteSpace(Tag)
                ? "Tag 条件"
                : (Inverse ? $"无Tag:{Tag}" : $"Tag:{Tag}");
        }

        public bool Evaluate(StateInterruptContext context)
        {
            if (context == null || context.RuntimeContext == null || context.RuntimeContext.TagQueryService == null || string.IsNullOrWhiteSpace(Tag))
            {
                return false;
            }

            bool hasTag = context.RuntimeContext.TagQueryService.HasTag(context.RuntimeContext.Unit, Tag);
            return Inverse ? !hasTag : hasTag;
        }

        public IStateInterruptCondition Clone()
        {
            return new StateTagInterruptCondition
            {
                Tag = Tag,
                Inverse = Inverse,
            };
        }
    }

    [Serializable]
    public sealed class StateBreakValueInterruptCondition : IStateInterruptCondition
    {
        public float MinimumBreakValue = 0f;

        public string GetDisplayName()
        {
            return $"BreakValue >= {MinimumBreakValue:0.###}";
        }

        public bool Evaluate(StateInterruptContext context)
        {
            return context != null && context.BreakValue >= Mathf.Max(0f, MinimumBreakValue);
        }

        public IStateInterruptCondition Clone()
        {
            return new StateBreakValueInterruptCondition
            {
                MinimumBreakValue = MinimumBreakValue,
            };
        }
    }

    [Serializable]
    public sealed class StateGroundingInterruptCondition : IStateInterruptCondition
    {
        public StateGroundingMode GroundingMode = StateGroundingMode.StableGrounded;
        public float CoyoteTime = 0.1f;

        public string GetDisplayName() => $"接地:{GroundingMode}";

        public bool Evaluate(StateInterruptContext context)
        {
            if (context?.RuntimeContext?.MotionSnapshotProvider == null)
            {
                return false;
            }

            var snapshot = context.RuntimeContext.MotionSnapshotProvider();
            switch (GroundingMode)
            {
                case StateGroundingMode.NotStableGrounded:
                    return !snapshot.IsStableGrounded;
                case StateGroundingMode.JustLanded:
                    return snapshot.JustLanded;
                case StateGroundingMode.JustLeftStableGround:
                    return snapshot.JustLeftStableGround;
                case StateGroundingMode.WithinCoyoteTime:
                    return snapshot.IsStableGrounded || snapshot.TimeSinceStableGrounded <= Mathf.Max(0f, CoyoteTime);
                default:
                    return snapshot.IsStableGrounded;
            }
        }

        public IStateInterruptCondition Clone()
        {
            return new StateGroundingInterruptCondition
            {
                GroundingMode = GroundingMode,
                CoyoteTime = CoyoteTime,
            };
        }
    }

    [Serializable]
    public sealed class StateMotionValueInterruptCondition : IStateInterruptCondition
    {
        public StateMotionValue MotionValue = StateMotionValue.VerticalSpeed;
        public StateFloatComparison Comparison = StateFloatComparison.LessOrEqual;
        public float Threshold;

        public string GetDisplayName() => $"运动:{MotionValue} {Comparison} {Threshold:0.###}";

        public bool Evaluate(StateInterruptContext context)
        {
            if (context?.RuntimeContext?.MotionSnapshotProvider == null)
            {
                return false;
            }

            var snapshot = context.RuntimeContext.MotionSnapshotProvider();
            float value = MotionValue == StateMotionValue.PlanarSpeed
                ? snapshot.PlanarSpeed
                : MotionValue == StateMotionValue.LandingVerticalSpeed
                    ? snapshot.LandingVerticalSpeed
                    : snapshot.VerticalSpeed;
            switch (Comparison)
            {
                case StateFloatComparison.Greater:
                    return value > Threshold;
                case StateFloatComparison.GreaterOrEqual:
                    return value >= Threshold;
                case StateFloatComparison.Less:
                    return value < Threshold;
                default:
                    return value <= Threshold;
            }
        }

        public IStateInterruptCondition Clone()
        {
            return new StateMotionValueInterruptCondition
            {
                MotionValue = MotionValue,
                Comparison = Comparison,
                Threshold = Threshold,
            };
        }
    }
}