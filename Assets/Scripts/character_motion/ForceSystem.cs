using System.Collections.Generic;
using UnityEngine;

namespace ActionEditor.CharacterMotion
{
    /// <summary>
    /// 力系统。负责维护重力与各类力实例生命周期，并输出每帧速度增量。
    /// </summary>
    public sealed class ForceSystem : MonoBehaviour
    {
        private sealed class ForceInstance
        {
            public readonly int Id;
            public readonly ForceConfig Config;
            public float Elapsed;
            public bool ConsumedImpulse;

            public ForceInstance(int id, ForceConfig config)
            {
                Id = id;
                Config = config;
            }

            public bool IsCompleted
            {
                get
                {
                    if (Config.Type == ForceType.Impulse)
                    {
                        return ConsumedImpulse;
                    }

                    return Config.Duration > 0f && Elapsed >= Config.Duration;
                }
            }

            public Vector3 Tick(float deltaTime)
            {
                if (Config.Type == ForceType.Impulse)
                {
                    if (ConsumedImpulse)
                    {
                        return Vector3.zero;
                    }

                    ConsumedImpulse = true;
                    return ResolveDirection() * Mathf.Max(0f, Config.Magnitude);
                }

                float curveScale = 1f;
                if (Config.Type == ForceType.Curve && Config.Curve != null)
                {
                    float t = Config.Duration > 0f
                        ? Mathf.Clamp01(Elapsed / Config.Duration)
                        : 1f;
                    curveScale = Config.Curve.Evaluate(t);
                }

                Vector3 delta = ResolveDirection() * (Mathf.Max(0f, Config.Magnitude) * curveScale * deltaTime);
                Elapsed += deltaTime;
                return delta;
            }

            private Vector3 ResolveDirection()
            {
                if (Config.Direction.sqrMagnitude <= Mathf.Epsilon)
                {
                    return Vector3.zero;
                }

                return Config.Direction.normalized;
            }
        }

        [SerializeField] private bool _gravityEnabled;
        [SerializeField] private Vector3 _gravity = new Vector3(0f, -9.81f, 0f);
        [SerializeField, Min(0f)] private float _maxFallSpeed = 40f;
        [SerializeField, Min(0f)] private float _externalVelocityDrag = 4f;

        private readonly List<ForceInstance> _forces = new List<ForceInstance>();
        private int _nextId = 1;
        private Vector3 _persistentVelocity;
        private bool _applyGravityThisFrame = true;
        private bool _isStableGrounded;
        private Vector3 _characterUp = Vector3.up;

        public bool GravityEnabled => _gravityEnabled;
        public Vector3 Gravity => _gravity;
        public Vector3 CurrentVelocity => _persistentVelocity;

        public void Configure(bool gravityEnabled, float gravityMagnitude, float maxFallSpeed, float externalVelocityDrag)
        {
            _gravityEnabled = gravityEnabled;
            _gravity = -_characterUp * Mathf.Max(0f, gravityMagnitude);
            _maxFallSpeed = Mathf.Max(0f, maxFallSpeed);
            _externalVelocityDrag = Mathf.Max(0f, externalVelocityDrag);
        }

        public void EnableGravity(bool enabled)
        {
            _gravityEnabled = enabled;
        }

        public void SetGravity(Vector3 gravity)
        {
            _gravity = gravity;
        }

        public ForceHandle AddImpulse(Vector3 impulse, string tag = "Skill")
        {
            ForceConfig config = new ForceConfig
            {
                Type = ForceType.Impulse,
                Direction = impulse.sqrMagnitude > Mathf.Epsilon ? impulse.normalized : Vector3.zero,
                Magnitude = impulse.magnitude,
                Duration = 0f,
                Tag = tag,
            };

            return AddForce(config);
        }

        public ForceHandle AddForce(ForceConfig config)
        {
            if (config == null)
            {
                return default;
            }

            ForceConfig copied = new ForceConfig
            {
                Type = config.Type,
                Direction = config.Direction,
                Magnitude = config.Magnitude,
                Duration = config.Duration,
                Tag = config.Tag,
                Curve = config.Curve,
            };

            ForceInstance instance = new ForceInstance(_nextId++, copied);
            _forces.Add(instance);
            return new ForceHandle(instance.Id);
        }

        public void RemoveForce(int forceId)
        {
            for (int i = _forces.Count - 1; i >= 0; i--)
            {
                if (_forces[i].Id == forceId)
                {
                    _forces.RemoveAt(i);
                    return;
                }
            }
        }

        public void ClearForcesByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }

            for (int i = _forces.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_forces[i].Config.Tag, tag))
                {
                    _forces.RemoveAt(i);
                }
            }
        }

        public void SetGravityApplyThisFrame(bool applyGravity)
        {
            _applyGravityThisFrame = applyGravity;
        }

        public void SetGroundingState(bool isStableGrounded, Vector3 characterUp)
        {
            _isStableGrounded = isStableGrounded;
            _characterUp = characterUp.sqrMagnitude > Mathf.Epsilon ? characterUp.normalized : Vector3.up;
        }

        public void Tick(float deltaTime)
        {
            deltaTime = Mathf.Max(0f, deltaTime);

            if (_isStableGrounded)
            {
                float verticalSpeed = Vector3.Dot(_persistentVelocity, _characterUp);
                if (verticalSpeed < 0f)
                {
                    _persistentVelocity -= _characterUp * verticalSpeed;
                }
            }

            if (_gravityEnabled && _applyGravityThisFrame)
            {
                _persistentVelocity += _gravity * deltaTime;
            }

            for (int i = _forces.Count - 1; i >= 0; i--)
            {
                ForceInstance instance = _forces[i];
                _persistentVelocity += instance.Tick(deltaTime);
                if (instance.IsCompleted)
                {
                    _forces.RemoveAt(i);
                }
            }

            ClampFallingSpeed();

            if (_externalVelocityDrag > 0f)
            {
                Vector3 verticalVelocity = Vector3.Project(_persistentVelocity, _characterUp);
                Vector3 planarVelocity = _persistentVelocity - verticalVelocity;
                planarVelocity *= 1f / (1f + _externalVelocityDrag * deltaTime);
                _persistentVelocity = planarVelocity + verticalVelocity;
            }

            _applyGravityThisFrame = true;
        }

        public Vector3 GetVelocityDelta(float deltaTime)
        {
            return _persistentVelocity;
        }

        public void SetVerticalSpeed(Vector3 up, float speed)
        {
            Vector3 normalizedUp = up.sqrMagnitude > Mathf.Epsilon ? up.normalized : Vector3.up;
            float currentVerticalSpeed = Vector3.Dot(_persistentVelocity, normalizedUp);
            _persistentVelocity += normalizedUp * (speed - currentVerticalSpeed);
        }

        public void ResetVelocity()
        {
            _persistentVelocity = Vector3.zero;
        }

        private void ClampFallingSpeed()
        {
            if (_maxFallSpeed <= 0f)
            {
                return;
            }

            float verticalSpeed = Vector3.Dot(_persistentVelocity, _characterUp);
            if (verticalSpeed >= -_maxFallSpeed)
            {
                return;
            }

            _persistentVelocity += _characterUp * (-_maxFallSpeed - verticalSpeed);
        }
    }
}
