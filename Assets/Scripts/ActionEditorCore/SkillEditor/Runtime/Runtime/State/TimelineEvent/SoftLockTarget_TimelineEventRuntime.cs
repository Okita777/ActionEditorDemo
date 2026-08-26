using System;
using UnityEngine;
using SkillEditor.Preview;
using ActionEditor.CameraSystem;

namespace AsiSkillEditor.RunTime
{
    [TimelineEventRuntime(typeof(SoftLockTarget_TimelineEventData))]
    public sealed class SoftLockTarget_TimelineEventRuntime : TimelineEventRuntimeBase
    {
        private readonly SoftLockTarget_TimelineEventData _data;
        private SkillMotionBridge _motionBridge;
        private Transform _casterTransform;
        private Transform _lockTarget;
        private GameUnit _lockTargetUnit;
        private bool _isLocked;

        public SoftLockTarget_TimelineEventRuntime(TimelineEventConfig config) : base(config)
        {
            _data = mData as SoftLockTarget_TimelineEventData;
        }

        protected override void OnBegin()
        {
            if (_data == null || _data.Args == null)
            {
                throw new InvalidOperationException("SoftLockTarget timeline event data is invalid.");
            }

            if (mContext == null)
            {
                throw new InvalidOperationException("SkillContext is missing.");
            }

            ResolveLockTarget();
            ApplySoftLockRotation();
        }

        protected override void OnTick()
        {
            if (mContext == null || !_isLocked)
            {
                return;
            }
            ApplySoftLockRotation();
        }

        protected override void OnEnd(bool interrupted)
        {
            ClearRuntimeState();
        }

        private void ResolveLockTarget()
        {
            ClearRuntimeState();
            if (_data == null || _data.Args == null || mContext == null ||
                !TimelineMotionBridgeUtility.TryResolveBridge(mContext, out _motionBridge, out _casterTransform))
            {
                return;
            }

            if (TryResolveHardLockTarget(out Transform hardLockTarget, out GameUnit hardLockUnit))
            {
                SetLockTarget(hardLockTarget, hardLockUnit);
                return;
            }

            float radius = Mathf.Max(0f, _data.Args.Radius);
            if (radius <= 0f)
            {
                return;
            }

            Transform referenceTransform = ResolveReferenceTransform();
            Vector3 referenceForward = Vector3.ProjectOnPlane(referenceTransform.forward, Vector3.up);
            if (referenceForward.sqrMagnitude <= Mathf.Epsilon)
            {
                referenceForward = _casterTransform.forward;
            }

            Collider[] colliders = Physics.OverlapSphere(
                _casterTransform.position,
                radius,
                _data.Args.LayerMask,
                QueryTriggerInteraction.Ignore);
            Transform bestTarget = null;
            GameUnit bestUnit = null;
            float minimumAngle = 360f;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider candidateCollider = colliders[i];
                if (candidateCollider == null || candidateCollider.transform == referenceTransform ||
                    candidateCollider.transform.IsChildOf(_casterTransform) ||
                    !GameUnitResolver.TryResolve(candidateCollider, out GameUnitTargetInfo targetInfo) ||
                    targetInfo.Unit == null || targetInfo.Unit == mContext.Caster)
                {
                    continue;
                }

                Transform candidate = targetInfo.Unit.UnitObject != null
                    ? targetInfo.Unit.UnitObject.transform
                    : targetInfo.Unit.transform;
                Vector3 candidateDirection = candidate.position - referenceTransform.position;
                float angle = Vector3.Angle(referenceForward, candidateDirection.normalized);
                if (angle < minimumAngle)
                {
                    minimumAngle = angle;
                    bestTarget = candidate;
                    bestUnit = targetInfo.Unit;
                }
            }

            if (bestTarget != null && minimumAngle < Mathf.Clamp(_data.Args.Angle, 0f, 360f) * 0.5f)
            {
                SetLockTarget(bestTarget, bestUnit);
            }
        }

        private bool TryResolveHardLockTarget(out Transform target, out GameUnit targetUnit)
        {
            target = null;
            targetUnit = null;
            HardLockTargetingController hardLock = _casterTransform.GetComponent<HardLockTargetingController>() ??
                _casterTransform.GetComponentInParent<HardLockTargetingController>(true) ??
                _casterTransform.GetComponentInChildren<HardLockTargetingController>(true);
            if (hardLock == null || !hardLock.IsLocked || hardLock.CurrentTarget == null)
            {
                return false;
            }

            target = hardLock.CurrentTarget.AimPoint;
            targetUnit = hardLock.CurrentTarget.Unit;
            return target != null;
        }

        private Transform ResolveReferenceTransform()
        {
            if (!_data.Args.ReferToCamera)
            {
                return _casterTransform;
            }

            Camera camera = Camera.main;
            return camera != null ? camera.transform : _casterTransform;
        }

        private void SetLockTarget(Transform target, GameUnit targetUnit)
        {
            _lockTarget = target;
            _lockTargetUnit = targetUnit;
            _isLocked = _lockTarget != null;
            if (_isLocked && _lockTargetUnit != null)
            {
                mContext.PrimaryTarget = _lockTargetUnit;
            }
        }

        private void ApplySoftLockRotation()
        {
            if (!_isLocked || _motionBridge == null || _casterTransform == null || _lockTarget == null)
            {
                return;
            }

            Vector3 direction = _lockTarget.position - _casterTransform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            float rotationSpeed = Mathf.Max(0f, _data.Args.RotationSpeed);
            if (rotationSpeed > 0f)
            {
                _motionBridge.SetLookDirection(direction.normalized, rotationSpeed, "SkillEvent.SoftLock", _data.Args.Priority);
            }
            else
            {
                _motionBridge.SetAbsoluteRotation(
                    Quaternion.LookRotation(direction.normalized, Vector3.up),
                    "SkillEvent.SoftLock",
                    _data.Args.Priority);
            }
        }

        private void ClearRuntimeState()
        {
            _motionBridge = null;
            _casterTransform = null;
            _lockTarget = null;
            _lockTargetUnit = null;
            _isLocked = false;
        }

        public override void Dispose()
        {
            ClearRuntimeState();
            base.Dispose();
        }
    }
}
