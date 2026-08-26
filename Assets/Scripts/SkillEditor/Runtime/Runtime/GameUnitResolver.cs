using System.Collections.Generic;
using ActionEditor.TagSystem;
using SkillEditor.Preview;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    public readonly struct GameUnitTargetInfo
    {
        public readonly GameUnit Unit;
        public readonly GameObject TargetObject;
        public readonly Collider TargetCollider;
        public readonly Vector3 HitPoint;
        public readonly bool HasHitPoint;
        public readonly Vector3 HitNormal;
        public readonly bool HasHitNormal;

        public GameUnitTargetInfo(GameUnit unit, GameObject targetObject, Collider targetCollider, Vector3 hitPoint, bool hasHitPoint, Vector3 hitNormal = default, bool hasHitNormal = false)
        {
            Unit = unit;
            TargetObject = targetObject;
            TargetCollider = targetCollider;
            HitPoint = hitPoint;
            HasHitPoint = hasHitPoint;
            HitNormal = hitNormal;
            HasHitNormal = hasHitNormal;
        }

        public GameObject RootObject => Unit != null ? Unit.gameObject : null;
    }

    public static class GameUnitResolver
    {
        public static GameUnit Resolve(object source)
        {
            return TryResolve(source, out GameUnitTargetInfo targetInfo) ? targetInfo.Unit : null;
        }

        public static bool TryResolve(object source, out GameUnitTargetInfo targetInfo)
        {
            switch (source)
            {
                case GameUnit gameUnit:
                    targetInfo = new GameUnitTargetInfo(gameUnit, gameUnit.gameObject, null, Vector3.zero, false);
                    return true;
                case Collider collider:
                    return TryResolve(collider, out targetInfo);
                case GameObject gameObject:
                    return TryResolve(gameObject, out targetInfo);
                case Component component:
                    if (component is GameUnit componentUnit)
                    {
                        targetInfo = new GameUnitTargetInfo(componentUnit, component.gameObject, null, Vector3.zero, false);
                        return true;
                    }

                    return TryResolve(component.gameObject, out targetInfo);
                default:
                    targetInfo = default;
                    return false;
            }
        }

        public static GameUnit Resolve(GameObject gameObject)
        {
            return TryResolve(gameObject, out GameUnitTargetInfo targetInfo) ? targetInfo.Unit : null;
        }

        public static bool TryResolve(GameObject gameObject, out GameUnitTargetInfo targetInfo)
        {
            if (gameObject == null)
            {
                targetInfo = default;
                return false;
            }

            GameUnit unit = gameObject.GetComponent<GameUnit>() ?? gameObject.GetComponentInParent<GameUnit>(true) ?? gameObject.GetComponentInChildren<GameUnit>(true);
            if (unit == null)
            {
                targetInfo = default;
                return false;
            }

            targetInfo = new GameUnitTargetInfo(unit, gameObject, null, Vector3.zero, false);
            return true;
        }

        public static bool TryResolve(Collider collider, out GameUnitTargetInfo targetInfo)
        {
            return TryResolve(collider, collider != null ? collider.bounds.center : Vector3.zero, out targetInfo);
        }

        public static bool TryResolve(Collider collider, Vector3 referencePoint, out GameUnitTargetInfo targetInfo)
        {
            if (collider == null)
            {
                targetInfo = default;
                return false;
            }

            GameObject targetObject = collider.attachedRigidbody != null ? collider.attachedRigidbody.gameObject : collider.gameObject;
            if (!TryResolve(targetObject, out targetInfo))
            {
                return false;
            }

            Vector3 hitPoint = collider.ClosestPoint(referencePoint);
            targetInfo = new GameUnitTargetInfo(targetInfo.Unit, targetInfo.TargetObject, collider, hitPoint, true);
            return true;
        }

        public static bool TryResolve(RaycastHit hit, out GameUnitTargetInfo targetInfo)
        {
            if (hit.collider == null || !TryResolve(hit.collider, out targetInfo))
            {
                targetInfo = default;
                return false;
            }

            targetInfo = new GameUnitTargetInfo(targetInfo.Unit, targetInfo.TargetObject, targetInfo.TargetCollider, hit.point, true, hit.normal, true);
            return true;
        }
    }
}