using UnityEngine;

namespace ActionEditor.CharacterMotion
{
    /// <summary>
    /// RootMotion 运动源。收集动画位移/旋转增量并输出为本帧影响。
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class RootMotionSource : MonoBehaviour, IMotionSource
    {
        public bool EnableMove = true;
        public bool EnableRotate = true;
        public float PositionScale = 1f;
        public float RotationScale = 1f;

        private Animator _animator;
        private Vector3 _positionDelta;
        private Quaternion _rotationDelta = Quaternion.identity;

        public bool IsApplyingRootMotion => _animator != null && _animator.applyRootMotion;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnAnimatorMove()
        {
            if (_animator == null || !_animator.applyRootMotion)
            {
                ResetDeltas();
                return;
            }

            _positionDelta += _animator.deltaPosition * PositionScale;
            Quaternion delta = _animator.deltaRotation;
            if (!Mathf.Approximately(RotationScale, 1f))
            {
                delta = Quaternion.Slerp(Quaternion.identity, delta, Mathf.Clamp01(RotationScale));
            }

            _rotationDelta = delta * _rotationDelta;
        }

        public void Collect(CharacterVelocity velocity, CharacterRotation rotation, float deltaTime)
        {
            CollectWeighted(velocity, rotation, deltaTime, 1f, 1f, 1f, 1f);
        }

        public void CollectWeighted(
            CharacterVelocity velocity,
            CharacterRotation rotation,
            float deltaTime,
            float forwardWeight,
            float sideWeight,
            float verticalWeight,
            float rotationWeight,
            bool allowBackwardMotion = true)
        {
            if (_animator == null || !_animator.applyRootMotion)
            {
                ResetDeltas();
                return;
            }

            if (EnableMove && _positionDelta.sqrMagnitude > Mathf.Epsilon)
            {
                float dt = Mathf.Max(0.0001f, deltaTime);
                Vector3 localDelta = transform.InverseTransformDirection(_positionDelta);
                if (!allowBackwardMotion && localDelta.z < 0f)
                {
                    localDelta.z = 0f;
                }

                localDelta.x *= Mathf.Max(0f, sideWeight);
                localDelta.y *= Mathf.Max(0f, verticalWeight);
                localDelta.z *= Mathf.Max(0f, forwardWeight);
                velocity.AddRootMotionVelocity(transform.TransformDirection(localDelta) / dt);
            }

            if (EnableRotate && Quaternion.Angle(Quaternion.identity, _rotationDelta) > 0.001f)
            {
                Quaternion weightedRotation = Quaternion.Slerp(
                    Quaternion.identity,
                    _rotationDelta,
                    Mathf.Clamp01(rotationWeight));
                rotation.AddRotationDelta(weightedRotation, "RootMotion");
            }

            ResetDeltas();
        }

        private void OnDisable()
        {
            ResetDeltas();
        }

        private void ResetDeltas()
        {
            _positionDelta = Vector3.zero;
            _rotationDelta = Quaternion.identity;
        }
    }
}
