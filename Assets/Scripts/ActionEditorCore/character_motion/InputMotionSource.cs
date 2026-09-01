using UnityEngine;
using ActionEditor.CameraSystem;
using ActionEditor.InputSystem;

namespace ActionEditor.CharacterMotion
{
    public readonly struct CharacterMotionIntent
    {
        public readonly Vector2 MoveAxis;
        public readonly Vector3 DesiredWorldDirection;
        public readonly float InputMagnitude;
        public readonly bool HasMoveInput;

        public CharacterMotionIntent(Vector2 moveAxis, Vector3 desiredWorldDirection)
        {
            MoveAxis = moveAxis;
            DesiredWorldDirection = desiredWorldDirection;
            InputMagnitude = Mathf.Clamp01(moveAxis.magnitude);
            HasMoveInput = InputMagnitude > 0.0001f && desiredWorldDirection.sqrMagnitude > Mathf.Epsilon;
        }
    }

    /// <summary>
    /// 输入运动源。将玩家输入转换为速度与朝向影响。
    /// </summary>
    public sealed class InputMotionSource : MonoBehaviour, IMotionSource
    {
        public bool EnableMove = true;
        public bool EnableRotate = true;

        public Transform CameraTransform;
        [SerializeField] private CharacterInputDriver _inputDriver;
        [SerializeField] private MonoBehaviour _cameraBasisBehaviour;
        [Tooltip("没有输入驱动时使用的兼容输入。运行时通常由 CharacterInputDriver 覆盖。")]
        public Vector2 MoveInput;

        private ICameraBasisProvider _cameraBasisProvider;

        public CharacterMotionIntent CurrentIntent { get; private set; }

        /// <summary>
        /// 按当前输入帧和相机平面基准即时计算运动意图。
        /// 状态中断在 KCC 收集运动源之前执行时，也能读取本帧而不是上一帧的输入方向。
        /// </summary>
        public CharacterMotionIntent ResolveCurrentIntent()
        {
            Vector2 moveAxis = ResolveMoveAxis();
            return new CharacterMotionIntent(moveAxis, ResolveMoveDirection(moveAxis));
        }

        public bool TryGetCameraPlanarBasis(out Vector3 forward, out Vector3 right)
        {
            if (_cameraBasisProvider != null && _cameraBasisProvider.IsAvailable)
            {
                forward = Vector3.ProjectOnPlane(_cameraBasisProvider.PlanarForward, Vector3.up);
                right = Vector3.ProjectOnPlane(_cameraBasisProvider.PlanarRight, Vector3.up);
                if (forward.sqrMagnitude > Mathf.Epsilon && right.sqrMagnitude > Mathf.Epsilon)
                {
                    forward.Normalize();
                    right.Normalize();
                    return true;
                }
            }

            Transform cameraTransform = CameraTransform;
            if (cameraTransform == null)
            {
                Camera mainCamera = Camera.main;
                cameraTransform = mainCamera != null ? mainCamera.transform : null;
            }

            if (cameraTransform != null)
            {
                forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
                if (forward.sqrMagnitude > Mathf.Epsilon)
                {
                    forward.Normalize();
                    right = Vector3.Cross(Vector3.up, forward).normalized;
                    return right.sqrMagnitude > Mathf.Epsilon;
                }
            }

            forward = Vector3.zero;
            right = Vector3.zero;
            return false;
        }

        private void Awake()
        {
            _inputDriver ??= GetComponent<CharacterInputDriver>() ?? GetComponentInChildren<CharacterInputDriver>(true);
            _cameraBasisProvider = _cameraBasisBehaviour as ICameraBasisProvider;
        }

        public void SetCameraBasisProvider(ICameraBasisProvider cameraBasisProvider)
        {
            _cameraBasisProvider = cameraBasisProvider;
            _cameraBasisBehaviour = cameraBasisProvider as MonoBehaviour;
        }

        public void Collect(CharacterVelocity velocity, CharacterRotation rotation, float deltaTime)
        {
            CurrentIntent = ResolveCurrentIntent();

            if (EnableMove)
            {
                Vector3 desiredVelocity = CurrentIntent.HasMoveInput
                    ? CurrentIntent.DesiredWorldDirection * CurrentIntent.InputMagnitude
                    : Vector3.zero;
                velocity.SetDesiredLocomotionVelocity(desiredVelocity, "Input");
            }

            if (EnableRotate && CurrentIntent.HasMoveInput)
            {
                rotation.AddLookDirection(CurrentIntent.DesiredWorldDirection, 0f, "Input");
            }
        }

        private Vector2 ResolveMoveAxis()
        {
            CharacterInputFrame frame = _inputDriver != null ? _inputDriver.CurrentFrame : null;
            return Vector2.ClampMagnitude(frame != null ? frame.MoveAxis : MoveInput, 1f);
        }

        private Vector3 ResolveMoveDirection(Vector2 moveAxis)
        {
            Vector3 input = new Vector3(moveAxis.x, 0f, moveAxis.y);
            if (input.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector3.zero;
            }

            if (TryGetCameraPlanarBasis(out Vector3 forward, out Vector3 right))
            {
                Vector3 basisDirection = forward * input.z + right * input.x;
                if (basisDirection.sqrMagnitude > Mathf.Epsilon)
                {
                    return basisDirection.normalized;
                }
            }

            return input.normalized;
        }
    }
}
