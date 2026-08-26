using System;
using System.Collections.Generic;
using SkillEditor.Preview;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    public readonly struct HitStopRequest
    {
        public readonly GameUnit Unit;
        public readonly float Duration;
        public readonly float TimeScale;
        public readonly int Priority;
        public readonly string SourceId;
        public readonly int Frame;

        public HitStopRequest(GameUnit unit, float duration, float timeScale, int priority, string sourceId, int frame)
        {
            Unit = unit;
            Duration = duration;
            TimeScale = timeScale;
            Priority = priority;
            SourceId = sourceId ?? string.Empty;
            Frame = frame;
        }
    }

    public interface IUnitHitStopService
    {
        void Request(in HitStopRequest request);
        float GetEffectiveTimeScale(GameUnit unit);
        bool IsAffected(GameUnit unit);
        void Clear(GameUnit unit);
    }

    [DefaultExecutionOrder(-150)]
    [DisallowMultipleComponent]
    public sealed class UnitHitStopService : MonoBehaviour, IUnitHitStopService
    {
        private sealed class ActiveRequest
        {
            public string SourceId;
            public int Frame;
            public int Priority;
            public float TimeScale;
            public double ExpiresAt;
        }

        private readonly List<ActiveRequest> _requests = new List<ActiveRequest>();
        private GameUnit _unit;
        private SkillCharacterActionBridge _animationController;
        private ActionEditor.CharacterMotion.CustomCharacterController _characterController;
        private float _effectiveTimeScale = 1f;

        [Header("Observed (Runtime)")]
        [SerializeField] private float _observedEffectiveTimeScale = 1f;
        [SerializeField] private int _observedActiveRequestCount;
        [SerializeField] private string _observedLastSourceId = string.Empty;
        [SerializeField] private float _observedRemainingTime;

        public float EffectiveTimeScale => _effectiveTimeScale;
        public int ActiveRequestCount => _requests.Count;

        private void Awake()
        {
            ResolveDependencies();
            ApplyEffectiveScale(1f);
        }

        private void Update()
        {
            double now = Time.unscaledTimeAsDouble;
            bool changed = false;
            for (int i = _requests.Count - 1; i >= 0; i--)
            {
                if (_requests[i].ExpiresAt <= now)
                {
                    _requests.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
            {
                Recalculate(now);
            }
            else
            {
                UpdateObserved(now);
            }
        }

        private void OnDisable()
        {
            Clear(_unit);
        }

        public void Request(in HitStopRequest request)
        {
            ResolveDependencies();
            if (request.Unit == null || request.Unit != _unit || request.Duration <= 0f)
            {
                return;
            }

            double now = Time.unscaledTimeAsDouble;
            double expiresAt = now + Math.Max(0f, request.Duration);
            float timeScale = Mathf.Clamp01(request.TimeScale);
            ActiveRequest existing = null;
            for (int i = 0; i < _requests.Count; i++)
            {
                ActiveRequest candidate = _requests[i];
                if (candidate.Frame == request.Frame &&
                    string.Equals(candidate.SourceId, request.SourceId, StringComparison.Ordinal))
                {
                    existing = candidate;
                    break;
                }
            }

            if (existing != null)
            {
                existing.Priority = Mathf.Max(existing.Priority, request.Priority);
                existing.TimeScale = Mathf.Min(existing.TimeScale, timeScale);
                existing.ExpiresAt = Math.Max(existing.ExpiresAt, expiresAt);
            }
            else
            {
                _requests.Add(new ActiveRequest
                {
                    SourceId = request.SourceId,
                    Frame = request.Frame,
                    Priority = request.Priority,
                    TimeScale = timeScale,
                    ExpiresAt = expiresAt,
                });
            }

            _observedLastSourceId = request.SourceId;
            Recalculate(now);
        }

        public float GetEffectiveTimeScale(GameUnit unit)
        {
            return unit != null && unit == _unit ? _effectiveTimeScale : 1f;
        }

        public bool IsAffected(GameUnit unit)
        {
            return unit != null && unit == _unit && _requests.Count > 0;
        }

        public void Clear(GameUnit unit)
        {
            if (unit != null && _unit != null && unit != _unit)
            {
                return;
            }

            _requests.Clear();
            _observedLastSourceId = string.Empty;
            _observedRemainingTime = 0f;
            ApplyEffectiveScale(1f);
        }

        public static IUnitHitStopService ResolveOrCreate(GameUnit unit)
        {
            if (unit == null)
            {
                return null;
            }

            GameObject root = unit.UnitObject != null ? unit.UnitObject : unit.gameObject;
            UnitHitStopService service = root.GetComponent<UnitHitStopService>() ??
                root.GetComponentInParent<UnitHitStopService>(true) ??
                root.GetComponentInChildren<UnitHitStopService>(true);
            return service != null ? service : root.AddComponent<UnitHitStopService>();
        }

        private void ResolveDependencies()
        {
            _unit ??= GetComponent<GameUnit>() ?? GetComponentInParent<GameUnit>(true) ?? GetComponentInChildren<GameUnit>(true);
            _animationController ??= GetComponent<SkillCharacterActionBridge>() ??
                GetComponentInParent<SkillCharacterActionBridge>(true) ??
                GetComponentInChildren<SkillCharacterActionBridge>(true);
            _characterController ??= GetComponent<ActionEditor.CharacterMotion.CustomCharacterController>() ??
                GetComponentInParent<ActionEditor.CharacterMotion.CustomCharacterController>(true) ??
                GetComponentInChildren<ActionEditor.CharacterMotion.CustomCharacterController>(true);
        }

        private void Recalculate(double now)
        {
            int highestPriority = int.MinValue;
            float scale = 1f;
            for (int i = 0; i < _requests.Count; i++)
            {
                ActiveRequest request = _requests[i];
                if (request.Priority > highestPriority)
                {
                    highestPriority = request.Priority;
                    scale = request.TimeScale;
                }
                else if (request.Priority == highestPriority)
                {
                    scale = Mathf.Min(scale, request.TimeScale);
                }
            }

            ApplyEffectiveScale(_requests.Count > 0 ? scale : 1f);
            UpdateObserved(now);
        }

        private void ApplyEffectiveScale(float scale)
        {
            _effectiveTimeScale = Mathf.Clamp01(scale);
            _observedEffectiveTimeScale = _effectiveTimeScale;
            _observedActiveRequestCount = _requests.Count;
            ResolveDependencies();
            _animationController?.SetPlaybackScale(_effectiveTimeScale);
            _characterController?.SetLocalTimeScale(_effectiveTimeScale);
        }

        private void UpdateObserved(double now)
        {
            _observedActiveRequestCount = _requests.Count;
            double maximumRemaining = 0d;
            for (int i = 0; i < _requests.Count; i++)
            {
                maximumRemaining = Math.Max(maximumRemaining, _requests[i].ExpiresAt - now);
            }

            _observedRemainingTime = (float)maximumRemaining;
        }
    }
}
