using System.Collections.Generic;
using SkillEditor.Preview;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    internal static class SkillTargetResolver
    {
        public static GameUnit Resolve(SkillQueryTargetType queryTarget, SkillContext context)
        {
            if (context == null)
            {
                Debug.Log("[AICode] SkillTargetResolver.Resolve: context is null.");
                return null;
            }

            GameUnit result;
            switch (queryTarget)
            {
                case SkillQueryTargetType.Caster:
                    // [AICode] Caster is a GameUnit semantic in the runtime context.
                    result = context.Caster;
                    break;

                case SkillQueryTargetType.PrimaryTarget:
                    result = context.PrimaryTarget;
                    break;

                default:
                    result = null;
                    break;
            }

            Debug.Log($"[AICode] SkillTargetResolver.Resolve: queryTarget={queryTarget}, caster='{(context.Caster != null ? context.Caster.name : "null")}', primaryTarget='{(context.PrimaryTarget != null ? context.PrimaryTarget.name : "null")}', resolved='{(result != null ? result.name : "null")}'.", result != null ? result : (Object)context.Caster);
            return result;
        }
    }

    public readonly struct SkillWeightedTargetSelectionResult
    {
        public readonly GameUnit Target;
        public readonly GameObject TargetObject;
        public readonly Collider TargetCollider;
        public readonly float Score;
        public readonly float Distance;
        public readonly float Angle;
        public readonly Vector3 HitPoint;
        public readonly bool HasHitPoint;

        public SkillWeightedTargetSelectionResult(GameUnit target, GameObject targetObject, Collider targetCollider, float score, float distance, float angle, Vector3 hitPoint, bool hasHitPoint)
        {
            Target = target;
            TargetObject = targetObject;
            TargetCollider = targetCollider;
            Score = score;
            Distance = distance;
            Angle = angle;
            HitPoint = hitPoint;
            HasHitPoint = hasHitPoint;
        }
    }

    public static class SkillTargetSelectionUtility
    {
        public static bool TrySelectBestTarget(
            Vector3 origin,
            Vector3 forward,
            float range,
            float angle,
            int layerMask,
            float centerWeight,
            Transform ignoredRoot,
            out SkillWeightedTargetSelectionResult result)
        {
            result = default;
            float clampedRange = Mathf.Max(0f, range);
            if (clampedRange <= 0f)
            {
                return false;
            }

            Vector3 normalizedForward = forward.sqrMagnitude <= Mathf.Epsilon ? Vector3.forward : forward.normalized;
            float halfAngle = Mathf.Clamp(angle, 0f, 360f) * 0.5f;
            float clampedCenterWeight = Mathf.Clamp01(centerWeight);
            Collider[] overlaps = Physics.OverlapSphere(origin, clampedRange, layerMask, QueryTriggerInteraction.Collide);
            if (overlaps == null || overlaps.Length == 0)
            {
                return false;
            }

            HashSet<GameUnit> visitedTargets = new HashSet<GameUnit>();
            float bestScore = float.MinValue;
            GameUnitTargetInfo bestTarget = default;
            float bestDistance = 0f;
            float bestAngle = 0f;

            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider overlap = overlaps[i];
                if (!GameUnitResolver.TryResolve(overlap, out GameUnitTargetInfo candidate) || candidate.Unit == null || !visitedTargets.Add(candidate.Unit))
                {
                    continue;
                }

                Transform candidateTransform = candidate.RootObject.transform;
                if (ignoredRoot != null && (candidateTransform == ignoredRoot || candidateTransform.IsChildOf(ignoredRoot)))
                {
                    continue;
                }

                Vector3 toCandidate = candidateTransform.position - origin;
                float distance = toCandidate.magnitude;
                if (distance <= Mathf.Epsilon || distance > clampedRange)
                {
                    continue;
                }

                float candidateAngle = Vector3.Angle(normalizedForward, toCandidate.normalized);
                if (halfAngle < 180f && candidateAngle > halfAngle)
                {
                    continue;
                }

                float distanceScore = 1f - Mathf.Clamp01(distance / clampedRange);
                float centerScore = halfAngle <= Mathf.Epsilon ? 1f : 1f - Mathf.Clamp01(candidateAngle / halfAngle);
                float finalScore = Mathf.Lerp(distanceScore, centerScore, clampedCenterWeight);
                if (finalScore <= bestScore)
                {
                    continue;
                }

                bestScore = finalScore;
                bestTarget = candidate;
                bestDistance = distance;
                bestAngle = candidateAngle;
            }

            if (bestTarget.Unit == null)
            {
                return false;
            }

            result = new SkillWeightedTargetSelectionResult(bestTarget.Unit, bestTarget.TargetObject, bestTarget.TargetCollider, bestScore, bestDistance, bestAngle, bestTarget.HitPoint, bestTarget.HasHitPoint);
            return true;
        }
    }
}
