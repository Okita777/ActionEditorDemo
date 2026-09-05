using UnityEngine;

namespace ActionEditor.CharacterMotion
{
    /// <summary>两次 KCC 更新之间由 Animator 产生的一批完整 Root Motion 增量。</summary>
    public readonly struct RootMotionDelta
    {
        public RootMotionDelta(Vector3 position, Quaternion rotation, int sampleCount)
        {
            Position = position;
            Rotation = rotation;
            SampleCount = sampleCount;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public int SampleCount { get; }
        public bool HasData => SampleCount > 0;
    }

    /// <summary>
    /// RootMotion 运动源。收集动画位移/旋转增量并输出为本帧影响。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class RootMotionSource : MonoBehaviour, IMotionSource
    {
        [Header("Channels")]
        public bool EnableMove = true;
        public bool EnableRotate = true;
        [Tooltip("只把动画绕局部 Y 轴的旋转提交给机械胶囊，过滤根骨 Pitch/Roll。地面角色建议开启。")]
        public bool YawOnly = true;
        public float PositionScale = 1f;
        public float RotationScale = 1f;

        private Animator _animator;
        private Vector3 _positionDelta;
        private Quaternion _rotationDelta = Quaternion.identity;
        private int _sampleCount;
        private bool _warnedMissingConsumer;

        [Header("Observed (Runtime)")]
        [SerializeField] private int _observedCapturedSampleCount;
        [SerializeField] private Vector3 _observedPendingPositionDelta;
        [SerializeField] private Vector3 _observedPendingRotationEuler;
        [SerializeField] private Vector3 _observedConsumedPositionDelta;
        [SerializeField] private Vector3 _observedConsumedRotationEuler;
        [SerializeField] private int _observedLastConsumeFrame = -1;
        [SerializeField] private string _observedLastDiscardReason = string.Empty;

        public bool IsApplyingRootMotion => _animator != null && _animator.applyRootMotion;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            ValidateConsumer();
        }

        private void OnAnimatorMove()
        {
            if (_animator == null || !_animator.applyRootMotion)
            {
                DiscardPending("Animator Root Motion disabled during sampling");
                return;
            }

            _positionDelta += _animator.deltaPosition * PositionScale;
            Quaternion delta = _animator.deltaRotation;
            if (!Mathf.Approximately(RotationScale, 1f))
            {
                delta = Quaternion.Slerp(Quaternion.identity, delta, Mathf.Clamp01(RotationScale));
            }

            _rotationDelta = delta * _rotationDelta;
            _sampleCount++;
            _observedCapturedSampleCount++;
            _observedPendingPositionDelta = _positionDelta;
            _observedPendingRotationEuler = _rotationDelta.eulerAngles;
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
                DiscardPending("Animator Root Motion disabled");
                return;
            }

            if (!EnableMove && !EnableRotate)
            {
                DiscardPending("All Root Motion channels rejected by movement policy");
                return;
            }

            if (!TryConsume(out RootMotionDelta delta))
            {
                return;
            }

            if (EnableMove && delta.Position.sqrMagnitude > Mathf.Epsilon)
            {
                float dt = Mathf.Max(0.0001f, deltaTime);
                Vector3 localDelta = transform.InverseTransformDirection(delta.Position);
                if (!allowBackwardMotion && localDelta.z < 0f)
                {
                    localDelta.z = 0f;
                }

                localDelta.x *= Mathf.Max(0f, sideWeight);
                localDelta.y *= Mathf.Max(0f, verticalWeight);
                localDelta.z *= Mathf.Max(0f, forwardWeight);
                velocity.AddRootMotionVelocity(transform.TransformDirection(localDelta) / dt);
            }

            if (EnableRotate && Quaternion.Angle(Quaternion.identity, delta.Rotation) > 0.001f)
            {
                Quaternion rotationDelta = YawOnly ? ExtractYaw(delta.Rotation) : delta.Rotation;
                Quaternion weightedRotation = Quaternion.Slerp(
                    Quaternion.identity,
                    rotationDelta,
                    Mathf.Clamp01(rotationWeight));
                rotation.AddRotationDelta(weightedRotation, "RootMotion");
            }
        }

        /// <summary>原子取得当前全部动画采样。只有成功取得数据时才清除 pending 缓存。</summary>
        public bool TryConsume(out RootMotionDelta delta)
        {
            if (_sampleCount <= 0)
            {
                delta = default;
                return false;
            }

            delta = new RootMotionDelta(_positionDelta, _rotationDelta, _sampleCount);
            _observedConsumedPositionDelta = delta.Position;
            _observedConsumedRotationEuler = delta.Rotation.eulerAngles;
            _observedLastConsumeFrame = Time.frameCount;
            _observedLastDiscardReason = string.Empty;
            ResetPending();
            return true;
        }

        /// <summary>显式丢弃未消费数据，并保留原因供运行时诊断。</summary>
        public void DiscardPending(string reason)
        {
            if (_sampleCount > 0)
            {
                _observedLastDiscardReason = reason ?? string.Empty;
            }

            ResetPending();
        }

        private static Quaternion ExtractYaw(Quaternion rotation)
        {
            // 提取绕局部 Y 轴的 twist。符号连续，不受 Euler 角 0/360 跳变影响。
            Quaternion yaw = new Quaternion(0f, rotation.y, 0f, rotation.w);
            float magnitude = Mathf.Sqrt(yaw.y * yaw.y + yaw.w * yaw.w);
            if (magnitude <= 0.000001f)
            {
                return Quaternion.identity;
            }

            yaw.y /= magnitude;
            yaw.w /= magnitude;
            return yaw;
        }

        private void ValidateConsumer()
        {
            if (_warnedMissingConsumer || GetComponentInParent<CustomCharacterController>() != null)
            {
                return;
            }

            _warnedMissingConsumer = true;
            Debug.LogWarning(
                "RootMotionSource 已接管 Animator.OnAnimatorMove，但未找到 CustomCharacterController 消费者。" +
                "Root Motion 不会自动应用到 Transform。正式 KCC 角色必须由 CustomCharacterController 消费。",
                this);
        }

        private void OnDisable()
        {
            DiscardPending("RootMotionSource disabled");
        }

        private void ResetPending()
        {
            _positionDelta = Vector3.zero;
            _rotationDelta = Quaternion.identity;
            _sampleCount = 0;
            _observedPendingPositionDelta = Vector3.zero;
            _observedPendingRotationEuler = Vector3.zero;
        }
    }
}
