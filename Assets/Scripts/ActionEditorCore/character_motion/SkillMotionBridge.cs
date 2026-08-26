using UnityEngine;
using ActionEditor.CharacterMotion;

namespace AsiSkillEditor.RunTime
{
    /// <summary>
    /// 技能运动桥接器。技能事件通过该类调用力系统与旋转/速度容器。
    /// </summary>
    public sealed class SkillMotionBridge : MonoBehaviour, IMotionSource
    {
        [SerializeField] private ForceSystem _forceSystem;
        [SerializeField] private CharacterVelocity _characterVelocity;
        [SerializeField] private CharacterRotation _characterRotation;

        private bool _hasAbsoluteVelocity;
        private Vector3 _absoluteVelocity;

        private bool _hasLookDirection;
        private Vector3 _lookDirection;
        private float _lookSharpness = 20f;
        private int _lookPriority = int.MinValue;

        private bool _hasAbsoluteRotation;
        private Quaternion _absoluteRotation = Quaternion.identity;
        private int _absoluteRotationPriority = int.MinValue;

        private void Awake()
        {
            _forceSystem ??= GetComponent<ForceSystem>();
            _characterVelocity ??= GetComponent<CharacterVelocity>();
            _characterRotation ??= GetComponent<CharacterRotation>();
        }

        public ForceHandle ApplyImpulse(Vector3 impulse, string tag = "Skill")
        {
            if (_forceSystem == null)
            {
                return default;
            }

            return _forceSystem.AddImpulse(impulse, tag);
        }

        public ForceHandle ApplyForce(ForceConfig config)
        {
            if (_forceSystem == null)
            {
                return default;
            }

            return _forceSystem.AddForce(config);
        }

        public void SetAbsoluteVelocity(Vector3 velocity, string tag = "Skill")
        {
            _hasAbsoluteVelocity = true;
            _absoluteVelocity = velocity;
        }

        public void SetLookDirection(Vector3 direction, float sharpness = 20f, string tag = "Skill")
        {
            SetLookDirection(direction, sharpness, tag, 0);
        }

        public void SetLookDirection(Vector3 direction, float sharpness, string tag, int priority)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon || (_hasLookDirection && priority < _lookPriority))
            {
                return;
            }

            _hasLookDirection = true;
            _lookDirection = direction.normalized;
            _lookSharpness = Mathf.Max(0f, sharpness);
            _lookPriority = priority;
        }

        public void SetAbsoluteRotation(Quaternion rotation, string tag = "Skill")
        {
            SetAbsoluteRotation(rotation, tag, 0);
        }

        public void SetAbsoluteRotation(Quaternion rotation, string tag, int priority)
        {
            if (_hasAbsoluteRotation && priority < _absoluteRotationPriority)
            {
                return;
            }

            _hasAbsoluteRotation = true;
            _absoluteRotation = rotation;
            _absoluteRotationPriority = priority;
        }

        public void SetGravityEnabled(bool enabled)
        {
            _forceSystem?.EnableGravity(enabled);
        }

        public void SetGravity(Vector3 gravity)
        {
            _forceSystem?.SetGravity(gravity);
        }

        public bool IsGravityEnabled()
        {
            return _forceSystem != null && _forceSystem.GravityEnabled;
        }

        public Vector3 GetGravity()
        {
            return _forceSystem != null ? _forceSystem.Gravity : Physics.gravity;
        }

        public void LaunchByHeight(float targetHeight, float forceUngroundDuration = 0.1f)
        {
            if (_forceSystem == null)
            {
                return;
            }

            CustomCharacterController controller = GetComponent<CustomCharacterController>() ??
                                                   GetComponentInParent<CustomCharacterController>() ??
                                                   GetComponentInChildren<CustomCharacterController>(true);
            Vector3 up = controller != null ? controller.CharacterUp : transform.up;
            float gravityMagnitude = Mathf.Max(0.0001f, -Vector3.Dot(_forceSystem.Gravity, up));
            float launchSpeed = Mathf.Sqrt(2f * gravityMagnitude * Mathf.Max(0f, targetHeight));
            controller?.ForceUnground(forceUngroundDuration);
            _forceSystem.SetVerticalSpeed(up, launchSpeed);
        }

        public void Collect(CharacterVelocity velocity, CharacterRotation rotation, float deltaTime)
        {
            if (velocity == null || rotation == null)
            {
                return;
            }

            if (_hasAbsoluteVelocity)
            {
                velocity.SetAbsoluteVelocity(_absoluteVelocity, "SkillBridge");
                _hasAbsoluteVelocity = false;
                _absoluteVelocity = Vector3.zero;
            }

            if (_hasAbsoluteRotation)
            {
                rotation.SetAbsoluteRotation(_absoluteRotation, "SkillBridge", _absoluteRotationPriority);
                _hasAbsoluteRotation = false;
                _absoluteRotation = Quaternion.identity;
                _absoluteRotationPriority = int.MinValue;
            }

            if (_hasLookDirection)
            {
                rotation.AddLookDirection(_lookDirection, _lookSharpness, "SkillBridge", _lookPriority);
                _hasLookDirection = false;
                _lookDirection = Vector3.zero;
                _lookSharpness = 20f;
                _lookPriority = int.MinValue;
            }
        }
    }
}
