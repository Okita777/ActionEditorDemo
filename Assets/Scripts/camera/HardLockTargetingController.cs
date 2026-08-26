using System.Collections.Generic;
using ActionEditor.InputSystem;
using AsiSkillEditor.RunTime;
using SkillEditor.Preview;
using UnityEngine;

namespace ActionEditor.CameraSystem
{
    /// <summary>
    /// Camera Only 硬锁定。只筛选目标并驱动 GameplayCameraRigController，绝不参与角色运动与状态。
    /// </summary>
    [DefaultExecutionOrder(-40)]
    [DisallowMultipleComponent]
    public sealed class HardLockTargetingController : MonoBehaviour
    {
        private const int InitialOverlapCapacity = 32;
        private const int MaximumOverlapCapacity = 512;
        private const int InitialRaycastCapacity = 8;

        private readonly List<HardLockTarget> _candidates = new List<HardLockTarget>(32);
        private readonly Dictionary<GameUnit, HardLockTarget> _candidateUnits = new Dictionary<GameUnit, HardLockTarget>();
        private Collider[] _overlapResults = new Collider[InitialOverlapCapacity];
        private RaycastHit[] _raycastResults = new RaycastHit[InitialRaycastCapacity];

        private UnitHardLockConfig _config = new UnitHardLockConfig();
        private GameUnit _ownerUnit;
        private CharacterInputDriver _inputDriver;
        private GameplayCameraRigController _cameraRig;
        private Camera _outputCamera;
        private HardLockTarget _currentTarget;
        private float _occludedDuration;
        private bool _hasLock;

        public bool IsLocked => _hasLock;
        public HardLockTarget CurrentTarget => _currentTarget;

        public void Configure(
            UnitHardLockConfig config,
            GameUnit ownerUnit,
            CharacterInputDriver inputDriver,
            GameplayCameraRigController cameraRig,
            Camera outputCamera = null)
        {
            _config = config ?? new UnitHardLockConfig();
            _ownerUnit = ownerUnit;
            _inputDriver = inputDriver;
            _cameraRig = cameraRig;
            _outputCamera = outputCamera != null ? outputCamera : Camera.main;
            if (IsLocked && !IsTargetValid(_currentTarget, false, false))
            {
                ClearLock();
            }
        }

        private void OnDisable()
        {
            ClearLock();
        }

        private void Update()
        {
            CharacterInputFrame frame = _inputDriver != null ? _inputDriver.CurrentFrame : null;
            if (frame == null || _cameraRig == null || _ownerUnit == null)
            {
                return;
            }

            if (frame.IsActionDown(_config.ToggleAction))
            {
                if (IsLocked)
                {
                    ClearLock();
                }
                else
                {
                    TryLockBestTarget();
                }

                return;
            }

            if (!IsLocked)
            {
                return;
            }

            if (!ValidateCurrentTarget())
            {
                ClearLock();
                return;
            }

            if (frame.IsActionDown(_config.SwitchLeftAction))
            {
                TrySwitchHorizontal(false);
            }
            else if (frame.IsActionDown(_config.SwitchRightAction))
            {
                TrySwitchHorizontal(true);
            }
            else if (frame.IsActionDown(_config.SwitchFartherAction))
            {
                TrySwitchDistance(true);
            }
            else if (frame.IsActionDown(_config.SwitchNearerAction))
            {
                TrySwitchDistance(false);
            }
        }

        public bool TryLockBestTarget()
        {
            CollectCandidates();
            HardLockTarget best = null;
            float bestScore = float.PositiveInfinity;
            Vector3 origin = OwnerPosition;
            Vector3 cameraForward = _cameraRig != null ? _cameraRig.PlanarForward : _ownerUnit.transform.forward;
            float searchRadius = Mathf.Max(0.001f, _config.SearchRadius);
            float halfFan = Mathf.Max(0.001f, _config.HorizontalFanAngle * 0.5f);

            for (int i = 0; i < _candidates.Count; i++)
            {
                HardLockTarget candidate = _candidates[i];
                Vector3 planarDirection = Vector3.ProjectOnPlane(candidate.AimPosition - origin, Vector3.up);
                float distance = planarDirection.magnitude;
                float angle = Vector3.Angle(cameraForward, planarDirection);
                float score = _config.DistanceWeight * (distance / searchRadius) +
                    _config.AngleWeight * (angle / halfFan) - candidate.Priority;
                if (score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return SetLock(best);
        }

        public void ClearLock()
        {
            _hasLock = false;
            _currentTarget = null;
            _occludedDuration = 0f;
            _cameraRig?.ClearLockTarget();
        }

        private bool SetLock(HardLockTarget target)
        {
            if (target == null || _cameraRig == null || !IsTargetValid(target, true, true))
            {
                return false;
            }

            if (!_cameraRig.SetLockTarget(
                target.AimPoint,
                target.AimOffset,
                _config.ViewPivotHeightOffset))
            {
                return false;
            }

            _currentTarget = target;
            _hasLock = true;
            _occludedDuration = 0f;
            return true;
        }

        private bool ValidateCurrentTarget()
        {
            if (!IsTargetValid(_currentTarget, false, false))
            {
                return false;
            }

            if (IsOccluded(_currentTarget))
            {
                _occludedDuration += Time.deltaTime;
                return _occludedDuration <= Mathf.Max(0f, _config.OcclusionUnlockDelay);
            }

            _occludedDuration = 0f;
            return true;
        }

        private void TrySwitchHorizontal(bool switchRight)
        {
            Camera camera = ResolveOutputCamera();
            if (camera == null)
            {
                return;
            }

            CollectCandidates();
            Vector3 currentViewport = camera.WorldToViewportPoint(_currentTarget.AimPosition);
            HardLockTarget best = null;
            float bestDelta = float.PositiveInfinity;
            for (int i = 0; i < _candidates.Count; i++)
            {
                HardLockTarget candidate = _candidates[i];
                if (candidate == _currentTarget)
                {
                    continue;
                }

                Vector3 viewport = camera.WorldToViewportPoint(candidate.AimPosition);
                if (viewport.z <= 0f)
                {
                    continue;
                }

                float delta = viewport.x - currentViewport.x;
                if ((switchRight && delta <= 0f) || (!switchRight && delta >= 0f))
                {
                    continue;
                }

                float absoluteDelta = Mathf.Abs(delta);
                if (absoluteDelta < bestDelta)
                {
                    best = candidate;
                    bestDelta = absoluteDelta;
                }
            }

            if (best != null)
            {
                SetLock(best);
            }
        }

        private void TrySwitchDistance(bool farther)
        {
            CollectCandidates();
            float currentDistance = Vector3.Distance(OwnerPosition, _currentTarget.AimPosition);
            HardLockTarget best = null;
            float bestDelta = float.PositiveInfinity;
            for (int i = 0; i < _candidates.Count; i++)
            {
                HardLockTarget candidate = _candidates[i];
                if (candidate == _currentTarget)
                {
                    continue;
                }

                float delta = Vector3.Distance(OwnerPosition, candidate.AimPosition) - currentDistance;
                if ((farther && delta <= 0.001f) || (!farther && delta >= -0.001f))
                {
                    continue;
                }

                float absoluteDelta = Mathf.Abs(delta);
                if (absoluteDelta < bestDelta)
                {
                    best = candidate;
                    bestDelta = absoluteDelta;
                }
            }

            if (best != null)
            {
                SetLock(best);
            }
        }

        private void CollectCandidates()
        {
            _candidates.Clear();
            _candidateUnits.Clear();
            int count = OverlapTargets(OwnerPosition, Mathf.Max(0f, _config.SearchRadius));
            for (int i = 0; i < count; i++)
            {
                Collider collider = _overlapResults[i];
                _overlapResults[i] = null;
                if (collider == null)
                {
                    continue;
                }

                HardLockTarget target = collider.GetComponent<HardLockTarget>() ??
                    collider.GetComponentInParent<HardLockTarget>(true);
                GameUnit unit = target != null ? target.Unit : null;
                if (target == null || unit == null || unit == _ownerUnit ||
                    !IsTargetValid(target, true, true))
                {
                    continue;
                }

                if (!_candidateUnits.TryGetValue(unit, out HardLockTarget current) || target.Priority > current.Priority)
                {
                    _candidateUnits[unit] = target;
                }
            }

            foreach (KeyValuePair<GameUnit, HardLockTarget> pair in _candidateUnits)
            {
                _candidates.Add(pair.Value);
            }
        }

        private int OverlapTargets(Vector3 center, float radius)
        {
            while (true)
            {
                int count = Physics.OverlapSphereNonAlloc(
                    center,
                    radius,
                    _overlapResults,
                    _config.TargetLayers,
                    QueryTriggerInteraction.Collide);
                if (count < _overlapResults.Length || _overlapResults.Length >= MaximumOverlapCapacity)
                {
                    return count;
                }

                _overlapResults = new Collider[Mathf.Min(_overlapResults.Length * 2, MaximumOverlapCapacity)];
            }
        }

        private bool IsTargetValid(HardLockTarget target, bool requireSearchConstraints, bool requireUnoccluded)
        {
            if (target == null || !target.isActiveAndEnabled || !target.LockEnabled || target.Unit == null || target.Unit == _ownerUnit)
            {
                return false;
            }

            if (target.Unit.Attributes != null && target.Unit.GetAttribute(SkillAttributeType.CurrentHp) <= 0f)
            {
                return false;
            }

            Vector3 offset = target.AimPosition - OwnerPosition;
            float maxRadius = requireSearchConstraints ? _config.SearchRadius : _config.UnlockRadius;
            if (offset.sqrMagnitude > Mathf.Max(0f, maxRadius) * Mathf.Max(0f, maxRadius))
            {
                return false;
            }

            if (requireSearchConstraints)
            {
                Vector3 planarOffset = Vector3.ProjectOnPlane(offset, Vector3.up);
                Vector3 forward = _cameraRig != null ? _cameraRig.PlanarForward : _ownerUnit.transform.forward;
                if (planarOffset.sqrMagnitude <= 0.000001f ||
                    Vector3.Angle(forward, planarOffset) > Mathf.Clamp(_config.HorizontalFanAngle * 0.5f, 0f, 180f))
                {
                    return false;
                }
            }

            return !requireUnoccluded || !IsOccluded(target);
        }

        private bool IsOccluded(HardLockTarget target)
        {
            Vector3 origin = _cameraRig != null ? _cameraRig.TargetingOrigin : OwnerPosition;
            Vector3 direction = target.AimPosition - origin;
            float distance = direction.magnitude;
            if (distance <= 0.001f || _config.ObstacleLayers == 0)
            {
                return false;
            }

            int count = Physics.RaycastNonAlloc(
                origin,
                direction / distance,
                _raycastResults,
                distance,
                _config.ObstacleLayers,
                QueryTriggerInteraction.Ignore);
            if (count == _raycastResults.Length)
            {
                _raycastResults = new RaycastHit[_raycastResults.Length * 2];
                count = Physics.RaycastNonAlloc(origin, direction / distance, _raycastResults, distance,
                    _config.ObstacleLayers, QueryTriggerInteraction.Ignore);
            }

            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = _raycastResults[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                GameUnit hitUnit = GameUnitResolver.Resolve(hitCollider);
                if (hitUnit != _ownerUnit && hitUnit != target.Unit)
                {
                    return true;
                }
            }

            return false;
        }

        private Camera ResolveOutputCamera()
        {
            if (_outputCamera == null)
            {
                _outputCamera = Camera.main;
            }

            return _outputCamera;
        }

        private Vector3 OwnerPosition => _ownerUnit != null ? _ownerUnit.transform.position : transform.position;
    }
}
