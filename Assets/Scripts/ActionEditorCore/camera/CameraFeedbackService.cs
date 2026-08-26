using System;
using System.Collections.Generic;
using AsiSkillEditor.RunTime;
using Cinemachine;
using SkillEditor.Preview;
using UnityEngine;

namespace ActionEditor.CameraSystem
{
    public readonly struct CameraShakeRequest
    {
        public readonly Vector3 Position;
        public readonly Vector3 Direction;
        public readonly float Amplitude;
        public readonly float Frequency;
        public readonly float Duration;
        public readonly string SourceId;
        public readonly int Frame;

        public CameraShakeRequest(
            Vector3 position,
            Vector3 direction,
            float amplitude,
            float frequency,
            float duration,
            string sourceId,
            int frame)
        {
            Position = position;
            Direction = direction;
            Amplitude = amplitude;
            Frequency = frequency;
            Duration = duration;
            SourceId = sourceId ?? string.Empty;
            Frame = frame;
        }
    }

    public interface ICameraFeedbackService
    {
        void RequestShake(in CameraShakeRequest request);
    }

    /// <summary>
    /// 本地玩家相机反馈服务。通过 Cinemachine Impulse 向最终输出相机叠加震动，
    /// 不依赖当前激活的虚拟相机或其 Body 类型。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraFeedbackService : MonoBehaviour, ICameraFeedbackService
    {
        private const int ImpulseChannel = 1;
        private const float ReferenceFrequency = 8f;

        private sealed class RuntimeShakeSignal : SignalSourceAsset
        {
            public float Frequency = ReferenceFrequency;
            public override float SignalDuration => 0f;

            public override void GetSignal(float timeSinceSignalStart, out Vector3 pos, out Quaternion rot)
            {
                float frequency = Mathf.Max(0.01f, Frequency);
                float phase = timeSinceSignalStart * frequency;
                float x = Mathf.PerlinNoise(phase, 11.31f) * 2f - 1f;
                float y = Mathf.PerlinNoise(23.17f, phase) * 2f - 1f;
                float z = Mathf.PerlinNoise(phase + 37.13f, 47.29f) * 2f - 1f;
                pos = new Vector3(x, y, z) * 0.08f;
                rot = Quaternion.Euler(y * 1.2f, x * 1.6f, z * 0.8f);
            }
        }

        private readonly HashSet<string> _sameFrameRequestKeys = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<GameUnit, CameraFeedbackService> ServicesByOwner =
            new Dictionary<GameUnit, CameraFeedbackService>();
        private CinemachineImpulseSource _impulseSource;
        private CinemachineIndependentImpulseListener _listener;
        private RuntimeShakeSignal _signal;
        private Camera _outputCamera;
        private GameUnit _owner;
        private int _requestFrame = -1;

        [Header("Observed (Runtime)")]
        [SerializeField] private string _observedLastSourceId = string.Empty;
        [SerializeField] private float _observedLastAmplitude;
        [SerializeField] private float _observedLastFrequency;
        [SerializeField] private float _observedLastDuration;

        private void Awake()
        {
            EnsureInfrastructure();
        }

        private void OnDestroy()
        {
            UnregisterOwner();
            if (_signal != null)
            {
                Destroy(_signal);
                _signal = null;
            }
        }

        public void Configure(Camera outputCamera, GameUnit owner)
        {
            if (_owner != owner)
            {
                UnregisterOwner();
                _owner = owner;
                if (_owner != null)
                {
                    ServicesByOwner[_owner] = this;
                }
            }

            _outputCamera = outputCamera != null ? outputCamera : Camera.main;
            EnsureInfrastructure();
        }

        public void RequestShake(in CameraShakeRequest request)
        {
            if (request.Amplitude <= 0f || request.Duration <= 0f)
            {
                return;
            }

            EnsureInfrastructure();
            if (_impulseSource == null || _listener == null || _signal == null)
            {
                return;
            }

            if (_requestFrame != request.Frame)
            {
                _requestFrame = request.Frame;
                _sameFrameRequestKeys.Clear();
            }

            string requestKey = request.SourceId ?? string.Empty;
            if (!_sameFrameRequestKeys.Add(requestKey))
            {
                return;
            }

            ConfigureImpulse(request);
            Vector3 direction = request.Direction.sqrMagnitude > 0.000001f
                ? request.Direction.normalized
                : Vector3.down;
            _impulseSource.GenerateImpulseAtPositionWithVelocity(request.Position, direction);

            _observedLastSourceId = request.SourceId;
            _observedLastAmplitude = request.Amplitude;
            _observedLastFrequency = request.Frequency;
            _observedLastDuration = request.Duration;
        }

        public static ICameraFeedbackService ResolveForLocalPlayer(GameUnit unit)
        {
            if (unit == null)
            {
                return null;
            }

            ServicesByOwner.TryGetValue(unit, out CameraFeedbackService service);
            return service;
        }

        private void UnregisterOwner()
        {
            if (_owner != null && ServicesByOwner.TryGetValue(_owner, out CameraFeedbackService service) && service == this)
            {
                ServicesByOwner.Remove(_owner);
            }

            _owner = null;
        }

        private void EnsureInfrastructure()
        {
            _outputCamera = _outputCamera != null ? _outputCamera : Camera.main;
            if (_outputCamera == null || _outputCamera.GetComponent<CinemachineBrain>() == null)
            {
                return;
            }

            _listener = _outputCamera.GetComponent<CinemachineIndependentImpulseListener>();
            if (_listener == null)
            {
                _listener = _outputCamera.gameObject.AddComponent<CinemachineIndependentImpulseListener>();
            }

            _listener.m_ChannelMask = ImpulseChannel;
            _listener.m_Gain = 1f;
            _listener.m_Use2DDistance = false;
            _listener.m_UseLocalSpace = true;
            _listener.m_ReactionSettings = default;

            _impulseSource ??= GetComponent<CinemachineImpulseSource>();
            if (_impulseSource == null)
            {
                _impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
            }

            if (_signal == null)
            {
                _signal = ScriptableObject.CreateInstance<RuntimeShakeSignal>();
                _signal.hideFlags = HideFlags.HideAndDontSave;
            }

            CinemachineImpulseManager.Instance.IgnoreTimeScale = true;
        }

        private void ConfigureImpulse(in CameraShakeRequest request)
        {
            float duration = Mathf.Max(0.01f, request.Duration);
            _signal.Frequency = ReferenceFrequency;
            CinemachineImpulseDefinition definition = _impulseSource.m_ImpulseDefinition ?? new CinemachineImpulseDefinition();
            definition.m_ImpulseChannel = ImpulseChannel;
            definition.m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Legacy;
            definition.m_RawSignal = _signal;
            definition.m_AmplitudeGain = Mathf.Max(0f, request.Amplitude);
            definition.m_FrequencyGain = Mathf.Max(0.01f, request.Frequency) / ReferenceFrequency;
            definition.m_RepeatMode = CinemachineImpulseDefinition.RepeatMode.Loop;
            definition.m_Randomize = true;
            definition.m_TimeEnvelope = new CinemachineImpulseManager.EnvelopeDefinition
            {
                m_AttackTime = duration * 0.1f,
                m_SustainTime = duration * 0.45f,
                m_DecayTime = duration * 0.45f,
                m_ScaleWithImpact = false,
                m_HoldForever = false,
            };
            definition.m_ImpactRadius = 999999f;
            definition.m_DirectionMode = CinemachineImpulseManager.ImpulseEvent.DirectionMode.Fixed;
            definition.m_DissipationDistance = 0f;
            definition.m_PropagationSpeed = 999999f;
            _impulseSource.m_ImpulseDefinition = definition;
        }
    }
}
