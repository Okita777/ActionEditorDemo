using System;
using SkillEditor.Preview;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    public enum HitReactionType
    {
        Normal = 0,
        Blocked = 1,
        Parried = 2,
        GuardBroken = 3,
        Immune = 4,
    }

    public readonly struct HitResolutionResult
    {
        public readonly bool IsValid;
        public readonly bool EffectSucceeded;
        public readonly HitReactionType ReactionType;

        public HitResolutionResult(bool isValid, bool effectSucceeded, HitReactionType reactionType)
        {
            IsValid = isValid;
            EffectSucceeded = effectSucceeded;
            ReactionType = reactionType;
        }
    }

    public readonly struct UnitHitEvent
    {
        public readonly GameUnit Attacker;
        public readonly GameUnit Defender;
        public readonly string HitBoxId;
        public readonly Vector3 HitPoint;
        public readonly Vector3 HitDirection;
        public readonly bool HasHitPoint;
        public readonly Vector3 HitNormal;
        public readonly bool HasHitNormal;
        public readonly int Frame;
        public readonly SkillContext SkillContext;
        public readonly HitResolutionResult Resolution;

        public UnitHitEvent(
            GameUnit attacker,
            GameUnit defender,
            string hitBoxId,
            Vector3 hitPoint,
            Vector3 hitDirection,
            bool hasHitPoint,
            Vector3 hitNormal,
            bool hasHitNormal,
            int frame,
            SkillContext skillContext,
            HitResolutionResult resolution)
        {
            Attacker = attacker;
            Defender = defender;
            HitBoxId = hitBoxId ?? string.Empty;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            HasHitPoint = hasHitPoint;
            HitNormal = hitNormal;
            HasHitNormal = hasHitNormal;
            Frame = frame;
            SkillContext = skillContext;
            Resolution = resolution;
        }
    }

    public interface IUnitHitEventSource
    {
        event Action<UnitHitEvent> HitConfirmed;
    }

    public interface IUnitHitEventPublisher
    {
        void Publish(in UnitHitEvent hitEvent);
    }

    [DisallowMultipleComponent]
    public sealed class UnitHitEventHub : MonoBehaviour, IUnitHitEventSource, IUnitHitEventPublisher
    {
        public event Action<UnitHitEvent> HitConfirmed;

        public void Publish(in UnitHitEvent hitEvent)
        {
            HitConfirmed?.Invoke(hitEvent);
        }

        private void OnDisable()
        {
            HitConfirmed = null;
        }
    }
}
