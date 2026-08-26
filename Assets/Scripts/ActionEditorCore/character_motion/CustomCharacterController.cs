using System.Collections.Generic;
using AsiSkillEditor.RunTime;
using KinematicCharacterController;
using UnityEngine;

namespace ActionEditor.CharacterMotion
{
    /// <summary>KCC 在最近一次运动更新后产生的只读运动事实。</summary>
    public struct CharacterMotionSnapshot
    {
        public bool IsStableGrounded;
        public bool FoundAnyGround;
        public bool JustLanded;
        public bool JustLeftStableGround;
        public Vector3 ActualVelocity;
        public float PlanarSpeed;
        public float VerticalSpeed;
        public float LandingVerticalSpeed;
        public float TimeSinceStableGrounded;
    }

    /// <summary>
    /// KCC 自定义控制器。实现 ICharacterController，并将统一运动结果写入 KCC。
    /// </summary>
    [RequireComponent(typeof(KinematicCharacterMotor))]
    [RequireComponent(typeof(CharacterVelocity))]
    [RequireComponent(typeof(CharacterRotation))]
    [RequireComponent(typeof(InputMotionSource))]
    public sealed class CustomCharacterController : MonoBehaviour, ICharacterController
    {
        [Header("Source Switches")]
        public bool AllowInputMove = true;
        public bool AllowInputRotate = true;
        public bool AllowAIMove = true;
        public bool AllowAIRotate = true;
        public bool AllowRootMotionMove = true;
        public bool AllowRootMotionRotate = true;
        public bool ApplyGravityWhenStableGrounded;

        [Header("Locomotion")]
        [Min(0f)] public float GroundAcceleration = 30f;
        [Min(0f)] public float GroundDeceleration = 40f;
        [Min(0f)] public float AirAcceleration = 8f;
        [Min(0f)] public float MaxAirSpeed = 4f;
        [Min(0f)] public float DirectionChangeAcceleration = 180f;
        [Range(45f, 180f)] public float HardTurnAngle = 100f;
        [Range(0f, 1f)] public float HardTurnSpeedRetention;
        [Min(0f)] public float HardTurnSpeed = 2160f;
        public bool SnapFacingOnHardTurn = true;

        [Header("Observed (Runtime)")]
        [SerializeField] private float _observedBaseMoveSpeed;
        [SerializeField] private float _observedStateSpeedMultiplier = 1f;
        [SerializeField] private Vector3 _observedTargetVelocity;
        [SerializeField] private Vector3 _observedSolvedLocomotionVelocity;
        [SerializeField] private bool _observedHardTurn;
        [SerializeField] private bool _observedStableGrounded;

        [SerializeField] private KinematicCharacterMotor _motor;
        [SerializeField] private CharacterVelocity _characterVelocity;
        [SerializeField] private CharacterRotation _characterRotation;
        [SerializeField] private ForceSystem _forceSystem;
        [SerializeField] private InputMotionSource _inputMotionSource;
        [SerializeField] private MonoBehaviour[] _sourceBehaviours;

        private readonly List<IMotionSource> _motionSources = new List<IMotionSource>();
        private Vector3 _locomotionVelocity;
        private bool _stateAllowsMoveInput = true;
        private bool _stateAllowsLocomotionDrive = true;
        private bool _stateAllowsRotationInput = true;
        private StateMovementProfile _movementProfile = StateMovementProfile.CreateDefault();
        private float _baseTurnSpeed = 1080f;
        private float _airTurnSpeed = 720f;
        private float _baseMoveSpeed = 6f;
        private bool _wasStableGrounded;
        private float _timeSinceStableGrounded;
        private CharacterMotionSnapshot _motionSnapshot;
        private float _localTimeScale = 1f;

        public Vector3 CharacterUp => _motor != null ? _motor.CharacterUp : transform.up;
        public CharacterMotionSnapshot MotionSnapshot => _motionSnapshot;
        public float LocalTimeScale => _localTimeScale;

        public void SetLocalTimeScale(float scale)
        {
            _localTimeScale = Mathf.Clamp01(scale);
        }

        public float ActualPlanarSpeed
        {
            get
            {
                Vector3 up = _motor != null ? _motor.CharacterUp : Vector3.up;
                Vector3 velocity = _motor != null ? _motor.BaseVelocity : _locomotionVelocity;
                return Vector3.ProjectOnPlane(velocity, up).magnitude;
            }
        }

        /// <summary>
        /// 当前 Locomotion 求解器希望驱动角色达到的平面速度。
        /// 与碰撞后的实际速度分离，供动画步频匹配使用，避免撞墙时 Run 被降速。
        /// </summary>
        public float LocomotionDrivePlanarSpeed
        {
            get
            {
                Vector3 up = _motor != null ? _motor.CharacterUp : Vector3.up;
                return Vector3.ProjectOnPlane(_locomotionVelocity, up).magnitude;
            }
        }

        public Vector3 LocomotionDriveVelocity
        {
            get
            {
                Vector3 up = _motor != null ? _motor.CharacterUp : Vector3.up;
                return Vector3.ProjectOnPlane(_locomotionVelocity, up);
            }
        }

        /// <summary>
        /// 供 Locomotion Mixer 使用的运动速度。CameraForward 状态按相机平面基准表达，
        /// 因而角色尚在快速转身时，S 也会立即稳定对应局部 Back，而不会短暂播放 Forward。
        /// </summary>
        public Vector3 LocomotionAnimationLocalVelocity
        {
            get
            {
                Vector3 driveVelocity = LocomotionDriveVelocity;
                if (_movementProfile != null && _movementProfile.RotationMode == StateRotationMode.CameraForward &&
                    TryGetCameraPlanarBasis(out Vector3 cameraForward, out Vector3 cameraRight))
                {
                    return new Vector3(
                        Vector3.Dot(driveVelocity, cameraRight),
                        0f,
                        Vector3.Dot(driveVelocity, cameraForward));
                }

                return transform.InverseTransformDirection(driveVelocity);
            }
        }

        private void Awake()
        {
            ResolveComponents();

            if (GetComponent<InputMotionSource>() == null)
            {
                gameObject.AddComponent<InputMotionSource>();
            }

            if (_motor != null)
            {
                _motor.CharacterController = this;
            }

            RefreshSources();
        }

        private void ResolveComponents()
        {
            _motor ??= GetComponent<KinematicCharacterMotor>();
            _characterVelocity ??= GetComponent<CharacterVelocity>();
            _characterRotation ??= GetComponent<CharacterRotation>();
            _forceSystem ??= GetComponent<ForceSystem>();
            _inputMotionSource ??= GetComponent<InputMotionSource>();
        }

        public void RefreshSources()
        {
            _motionSources.Clear();
            if (_sourceBehaviours != null && _sourceBehaviours.Length > 0)
            {
                for (int i = 0; i < _sourceBehaviours.Length; i++)
                {
                    if (_sourceBehaviours[i] is IMotionSource source)
                    {
                        _motionSources.Add(source);
                    }
                }

                return;
            }

            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IMotionSource source)
                {
                    _motionSources.Add(source);
                }
            }
        }

        public void SetStateControl(bool allowMoveInput, bool allowLocomotionDrive, bool allowRotationInput)
        {
            _stateAllowsMoveInput = allowMoveInput;
            _stateAllowsLocomotionDrive = allowLocomotionDrive;
            _stateAllowsRotationInput = allowRotationInput;
        }

        public void SetMovementPolicy(StateMovementProfile profile)
        {
            _movementProfile = profile ?? StateMovementProfile.CreateDefault();
        }

        public void Configure(UnitLocomotionConfig config)
        {
            if (config == null)
            {
                return;
            }

            // CharacterBattleManager 和本组件的 Awake 顺序不固定。
            // 配置入口不能假设本组件的 Awake 已经完成，否则 ForceSystem 可能仍为空，
            // 导致 Unit 的重力、最大下落速度等配置静默失效并保留 Prefab 默认值。
            ResolveComponents();

            GroundAcceleration = Mathf.Max(0f, config.GroundAcceleration);
            GroundDeceleration = Mathf.Max(0f, config.GroundDeceleration);
            DirectionChangeAcceleration = Mathf.Max(0f, config.DirectionChangeAcceleration);
            HardTurnAngle = config.HardTurnAngle > 0f
                ? Mathf.Clamp(config.HardTurnAngle, 45f, 180f)
                : 100f;
            HardTurnSpeedRetention = Mathf.Clamp01(config.HardTurnSpeedRetention);
            HardTurnSpeed = config.HardTurnSpeed > 0f ? config.HardTurnSpeed : 2160f;
            SnapFacingOnHardTurn = config.HardTurnAngle <= 0f || config.SnapFacingOnHardTurn;
            AirAcceleration = Mathf.Max(0f, config.AirAcceleration);
            MaxAirSpeed = Mathf.Max(0f, config.MaxAirSpeed);
            _baseTurnSpeed = Mathf.Max(0f, config.TurnSpeed);
            // 旧 JSON / Binary 数据中没有 AirTurnSpeed 时按地面转速迁移，避免升级后空中无法转向。
            _airTurnSpeed = config.AirTurnSpeed > 0f
                ? config.AirTurnSpeed
                : _baseTurnSpeed;
            _baseMoveSpeed = Mathf.Max(0f, config.MaxMoveSpeed);
            _observedBaseMoveSpeed = _baseMoveSpeed;
            if (_motor != null)
            {
                // 旧角色预制体曾把 StableGroundLayers 序列化为 0，导致 KCC 永远判定为空中，
                // 所有地面速度参数失效并被 MaxAirSpeed 钳制。0 在正式角色配置中按旧数据迁移处理。
                int stableGroundLayers = config.StableGroundLayers != 0
                    ? config.StableGroundLayers
                    : Physics.DefaultRaycastLayers;
                _motor.StableGroundLayers = stableGroundLayers;
            }

            _forceSystem?.Configure(
                config.EnableGravity,
                config.Gravity,
                config.MaxFallSpeed,
                config.ExternalVelocityDrag);
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
            _characterVelocity.ResetFrame();
            _characterRotation.ResetFrame();

            if (_localTimeScale <= 0f)
            {
                return;
            }

            deltaTime *= _localTimeScale;

            StateMovementProfile profile = _movementProfile ?? StateMovementProfile.CreateDefault();
            bool allowsInputTranslation = profile.TranslationMode == StateTranslationMode.Input ||
                profile.TranslationMode == StateTranslationMode.Hybrid;
            bool allowsRootTranslation = profile.TranslationMode == StateTranslationMode.RootMotion ||
                profile.TranslationMode == StateTranslationMode.Hybrid;
            bool allowsDirectionRotation = profile.RotationMode == StateRotationMode.MoveDirection ||
                profile.RotationMode == StateRotationMode.TargetDirection ||
                profile.RotationMode == StateRotationMode.LimitedTargetDirection;
            bool allowsRootRotation = profile.RotationMode == StateRotationMode.RootMotion;

            bool rootMotionOwnsMove = false;
            bool rootMotionOwnsRotation = false;
            for (int i = 0; i < _motionSources.Count; i++)
            {
                if (_motionSources[i] is RootMotionSource rootSource && rootSource.IsApplyingRootMotion)
                {
                    rootMotionOwnsMove |= allowsRootTranslation && profile.TranslationMode == StateTranslationMode.RootMotion &&
                        AllowRootMotionMove && rootSource.EnableMove;
                    rootMotionOwnsRotation |= allowsRootRotation && AllowRootMotionRotate && rootSource.EnableRotate;
                }
            }

            for (int i = 0; i < _motionSources.Count; i++)
            {
                IMotionSource source = _motionSources[i];
                if (source == null)
                {
                    continue;
                }

                if (source is InputMotionSource inputSource)
                {
                    bool originalMove = inputSource.EnableMove;
                    bool originalRotate = inputSource.EnableRotate;
                    inputSource.EnableMove = originalMove
                        && AllowInputMove
                        && _stateAllowsMoveInput
                        && _stateAllowsLocomotionDrive
                        && allowsInputTranslation
                        && !rootMotionOwnsMove;
                    inputSource.EnableRotate = originalRotate
                        && AllowInputRotate
                        && _stateAllowsRotationInput
                        && allowsDirectionRotation
                        && !rootMotionOwnsRotation;
                    inputSource.Collect(_characterVelocity, _characterRotation, deltaTime);
                    if (_characterVelocity.HasDesiredLocomotionVelocity)
                    {
                        float speedMultiplier = Mathf.Max(0f, profile.InputSpeedMultiplier);
                        _characterVelocity.SetDesiredLocomotionVelocity(
                            _characterVelocity.DesiredLocomotionVelocity * (_baseMoveSpeed * speedMultiplier),
                            "Input.SpeedPolicy");
                        _observedStateSpeedMultiplier = speedMultiplier;
                    }
                    ApplyHybridInputAxisMask(profile);
                    inputSource.EnableMove = originalMove;
                    inputSource.EnableRotate = originalRotate;
                    continue;
                }

                if (source is AIMotionSource aiSource)
                {
                    bool originalMove = aiSource.EnableMove;
                    bool originalRotate = aiSource.EnableRotate;
                    aiSource.EnableMove = originalMove
                        && AllowAIMove
                        && _stateAllowsLocomotionDrive
                        && allowsInputTranslation
                        && !rootMotionOwnsMove;
                    aiSource.EnableRotate = originalRotate
                        && AllowAIRotate
                        && _stateAllowsRotationInput
                        && allowsDirectionRotation
                        && !rootMotionOwnsRotation;
                    aiSource.Collect(_characterVelocity, _characterRotation, deltaTime);
                    ApplyHybridInputAxisMask(profile);
                    aiSource.EnableMove = originalMove;
                    aiSource.EnableRotate = originalRotate;
                    continue;
                }

                if (source is RootMotionSource rootMotionSource)
                {
                    bool originalMove = rootMotionSource.EnableMove;
                    bool originalRotate = rootMotionSource.EnableRotate;
                    rootMotionSource.EnableMove = originalMove && AllowRootMotionMove && allowsRootTranslation;
                    rootMotionSource.EnableRotate = originalRotate && AllowRootMotionRotate && allowsRootRotation;
                    rootMotionSource.CollectWeighted(
                        _characterVelocity,
                        _characterRotation,
                        deltaTime,
                        profile.RootMotionForwardWeight,
                        profile.RootMotionSideWeight,
                        profile.RootMotionVerticalWeight,
                        profile.RootMotionRotationWeight,
                        profile.AllowBackwardRootMotion);
                    rootMotionSource.EnableMove = originalMove;
                    rootMotionSource.EnableRotate = originalRotate;
                    continue;
                }

                source.Collect(_characterVelocity, _characterRotation, deltaTime);
            }

            ApplyCameraForwardFacing(profile);
            RedirectRootMotionToMoveDirection(profile);

            if (_forceSystem != null)
            {
                bool isStableGrounded = _motor != null && _motor.GroundingStatus.IsStableOnGround;
                bool applyGravityThisFrame = profile.AllowGravity && (ApplyGravityWhenStableGrounded || !isStableGrounded);
                _forceSystem.SetGroundingState(isStableGrounded, _motor != null ? _motor.CharacterUp : Vector3.up);
                _forceSystem.SetGravityApplyThisFrame(applyGravityThisFrame);
                _forceSystem.Tick(deltaTime);
                _characterVelocity.AddForceVelocity(_forceSystem.GetVelocityDelta(deltaTime), "ForceSystem");
            }
        }

        private void ApplyCameraForwardFacing(StateMovementProfile profile)
        {
            if (profile == null || profile.RotationMode != StateRotationMode.CameraForward ||
                !AllowInputRotate || !_stateAllowsRotationInput ||
                !TryGetCameraPlanarBasis(out Vector3 cameraForward, out _))
            {
                return;
            }

            Vector3 up = _motor != null ? _motor.CharacterUp : Vector3.up;
            cameraForward = Vector3.ProjectOnPlane(cameraForward, up);
            if (cameraForward.sqrMagnitude > Mathf.Epsilon)
            {
                _characterRotation.AddLookDirection(cameraForward.normalized, 0f, "MovementPolicy.CameraForward");
            }
        }

        private bool TryGetCameraPlanarBasis(out Vector3 forward, out Vector3 right)
        {
            _inputMotionSource ??= GetComponent<InputMotionSource>();
            if (_inputMotionSource != null)
            {
                return _inputMotionSource.TryGetCameraPlanarBasis(out forward, out right);
            }

            forward = Vector3.zero;
            right = Vector3.zero;
            return false;
        }

        public void ForceUnground(float duration)
        {
            _motor?.ForceUnground(Mathf.Max(0f, duration));
        }

        private void ApplyHybridInputAxisMask(StateMovementProfile profile)
        {
            if (profile == null || profile.TranslationMode != StateTranslationMode.Hybrid ||
                !_characterVelocity.HasDesiredLocomotionVelocity)
            {
                return;
            }

            Vector3 localVelocity = transform.InverseTransformDirection(_characterVelocity.DesiredLocomotionVelocity);
            localVelocity.x *= 1f - Mathf.Clamp01(profile.RootMotionSideWeight);
            localVelocity.y *= 1f - Mathf.Clamp01(profile.RootMotionVerticalWeight);
            localVelocity.z *= 1f - Mathf.Clamp01(profile.RootMotionForwardWeight);
            _characterVelocity.SetDesiredLocomotionVelocity(
                transform.TransformDirection(localVelocity),
                "MovementPolicy.HybridInput");
        }

        private void RedirectRootMotionToMoveDirection(StateMovementProfile profile)
        {
            if (profile == null || profile.TranslationMode != StateTranslationMode.RootMotion ||
                profile.RotationMode != StateRotationMode.MoveDirection ||
                _characterVelocity.RootMotionVelocity.sqrMagnitude <= 0.000001f ||
                !_characterRotation.HasLookDirection)
            {
                return;
            }

            Vector3 up = _motor != null ? _motor.CharacterUp : Vector3.up;
            Vector3 rootMotionVelocity = _characterVelocity.RootMotionVelocity;
            Vector3 planarRootMotion = Vector3.ProjectOnPlane(rootMotionVelocity, up);
            Vector3 desiredDirection = Vector3.ProjectOnPlane(_characterRotation.LookDirection, up);
            if (planarRootMotion.sqrMagnitude <= 0.000001f || desiredDirection.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            Vector3 redirectedPlanarVelocity = desiredDirection.normalized * planarRootMotion.magnitude;
            Vector3 verticalVelocity = Vector3.Project(rootMotionVelocity, up);
            _characterVelocity.SetRootMotionVelocity(redirectedPlanarVelocity + verticalVelocity);
        }

        public void PostGroundingUpdate(float deltaTime)
        {
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            if (_motor == null)
            {
                return;
            }

            bool isStableGrounded = _motor.GroundingStatus.IsStableOnGround;
            Vector3 up = _motor.CharacterUp;
            Vector3 actualVelocity = _motor.BaseVelocity;
            float verticalSpeed = Vector3.Dot(actualVelocity, up);
            bool justLanded = !_wasStableGrounded && isStableGrounded;
            _timeSinceStableGrounded = isStableGrounded
                ? 0f
                : _timeSinceStableGrounded + Mathf.Max(0f, deltaTime);

            _motionSnapshot = new CharacterMotionSnapshot
            {
                IsStableGrounded = isStableGrounded,
                FoundAnyGround = _motor.GroundingStatus.FoundAnyGround,
                JustLanded = justLanded,
                JustLeftStableGround = _wasStableGrounded && !isStableGrounded,
                ActualVelocity = actualVelocity,
                PlanarSpeed = Vector3.ProjectOnPlane(actualVelocity, up).magnitude,
                VerticalSpeed = verticalSpeed,
                LandingVerticalSpeed = justLanded ? _motionSnapshot.VerticalSpeed : 0f,
                TimeSinceStableGrounded = _timeSinceStableGrounded,
            };
            _wasStableGrounded = isStableGrounded;
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_characterRotation == null || _localTimeScale <= 0f)
            {
                return;
            }

            Vector3 up = _motor != null ? _motor.CharacterUp : Vector3.up;
            bool isStableGrounded = _motor != null && _motor.GroundingStatus.IsStableOnGround;
            Quaternion targetRotation = _characterRotation.HasAbsoluteRotation
                ? _characterRotation.AbsoluteRotation
                : currentRotation;
            // 地面急转负责高速折返；空中只按独立空中转速渐进转向，避免复用地面瞬时折返手感。
            bool hardTurn = isStableGrounded && IsHardTurn(_locomotionVelocity, _characterVelocity.DesiredLocomotionVelocity);
            if (_characterRotation.HasLookDirection)
            {
                Vector3 lookDirection = Vector3.ProjectOnPlane(_characterRotation.LookDirection, up);
                if (lookDirection.sqrMagnitude > 0.0001f)
                {
                    targetRotation = Quaternion.LookRotation(lookDirection.normalized, up);
                }
            }

            targetRotation = _characterRotation.RotationDelta * targetRotation;
            float baseTurnSpeed = isStableGrounded ? _baseTurnSpeed : _airTurnSpeed;
            float stateTurnLimit = _movementProfile != null ? Mathf.Max(0f, _movementProfile.MaxTurnSpeed) : baseTurnSpeed;
            float maxTurnSpeed = stateTurnLimit > 0f ? Mathf.Min(baseTurnSpeed, stateTurnLimit) : baseTurnSpeed;
            if (hardTurn)
            {
                maxTurnSpeed = Mathf.Max(maxTurnSpeed, HardTurnSpeed);
            }

            deltaTime *= _localTimeScale;
            currentRotation = hardTurn && SnapFacingOnHardTurn
                ? targetRotation
                : Quaternion.RotateTowards(currentRotation, targetRotation, maxTurnSpeed * Mathf.Max(0f, deltaTime));
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_characterVelocity == null)
            {
                return;
            }

            if (_localTimeScale <= 0f)
            {
                currentVelocity = Vector3.zero;
                return;
            }

            deltaTime *= _localTimeScale;

            if (_characterVelocity.HasAbsoluteVelocity)
            {
                _locomotionVelocity = _characterVelocity.AbsoluteVelocity;
                currentVelocity = (_locomotionVelocity + _characterVelocity.ForceVelocity) * _localTimeScale;
                return;
            }

            StateTranslationMode translationMode = _movementProfile != null
                ? _movementProfile.TranslationMode
                : StateTranslationMode.Input;
            if (translationMode == StateTranslationMode.Locked || translationMode == StateTranslationMode.RootMotion)
            {
                _locomotionVelocity = Vector3.zero;
            }

            Vector3 targetVelocity = _characterVelocity.HasDesiredLocomotionVelocity
                ? _characterVelocity.DesiredLocomotionVelocity
                : Vector3.zero;
            _observedTargetVelocity = targetVelocity;
            SolveLocomotionVelocity(targetVelocity, deltaTime);
            _observedSolvedLocomotionVelocity = _locomotionVelocity;

            currentVelocity = _locomotionVelocity
                + _characterVelocity.SourceVelocity
                + _characterVelocity.RootMotionVelocity
                + _characterVelocity.ForceVelocity;
            currentVelocity *= _localTimeScale;
        }

        private void SolveLocomotionVelocity(Vector3 targetVelocity, float deltaTime)
        {
            Vector3 up = _motor != null ? _motor.CharacterUp : Vector3.up;
            bool isStableGrounded = _motor != null && _motor.GroundingStatus.IsStableOnGround;
            _observedStableGrounded = isStableGrounded;
            if (isStableGrounded)
            {
                Vector3 groundNormal = _motor.GroundingStatus.GroundNormal;
                _locomotionVelocity = _motor.GetDirectionTangentToSurface(_locomotionVelocity, groundNormal)
                    * _locomotionVelocity.magnitude;

                Vector3 inputRight = Vector3.Cross(targetVelocity, up);
                Vector3 groundDirection = Vector3.Cross(groundNormal, inputRight);
                targetVelocity = groundDirection.sqrMagnitude > Mathf.Epsilon
                    ? groundDirection.normalized * targetVelocity.magnitude
                    : Vector3.zero;

                SolveResponsiveGroundVelocity(targetVelocity, deltaTime);
                return;
            }

            Vector3 planarVelocity = Vector3.ProjectOnPlane(_locomotionVelocity, up);
            Vector3 planarTarget = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(targetVelocity, up), MaxAirSpeed);
            // MaxAirSpeed 只限制空中输入能主动产生的速度，不应吃掉奔跑起跳继承的动量。
            if (planarTarget.sqrMagnitude > Mathf.Epsilon && planarVelocity.magnitude > planarTarget.magnitude)
            {
                planarTarget = planarTarget.normalized * planarVelocity.magnitude;
            }
            else if (planarTarget.sqrMagnitude <= Mathf.Epsilon)
            {
                planarTarget = planarVelocity;
            }
            planarVelocity = Vector3.MoveTowards(
                planarVelocity,
                planarTarget,
                Mathf.Max(0f, AirAcceleration) * Mathf.Max(0f, _movementProfile.AirControl) * Mathf.Max(0f, deltaTime));
            _locomotionVelocity = planarVelocity;
        }

        private void SolveResponsiveGroundVelocity(Vector3 targetVelocity, float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            float currentSpeed = _locomotionVelocity.magnitude;
            float targetSpeed = targetVelocity.magnitude;
            if (targetSpeed <= 0.0001f)
            {
                currentSpeed = Mathf.MoveTowards(
                    currentSpeed,
                    0f,
                    GroundDeceleration * Mathf.Max(0f, _movementProfile.DecelerationMultiplier) * dt);
                _locomotionVelocity = currentSpeed > 0.0001f
                    ? _locomotionVelocity.normalized * currentSpeed
                    : Vector3.zero;
                _observedHardTurn = false;
                return;
            }

            Vector3 targetDirection = targetVelocity / targetSpeed;
            bool hardTurn = IsHardTurn(_locomotionVelocity, targetVelocity);
            _observedHardTurn = hardTurn;
            if (hardTurn)
            {
                // 高速 ARPG 折返：不旋转旧速度向量。直接清除旧方向及横向分量，
                // 只按配置保留沿新方向的速度，避免滑行和圆弧。
                currentSpeed *= HardTurnSpeedRetention;
            }

            float acceleration = hardTurn
                ? DirectionChangeAcceleration * Mathf.Max(0f, _movementProfile.AccelerationMultiplier)
                : targetSpeed > currentSpeed
                    ? GroundAcceleration * Mathf.Max(0f, _movementProfile.AccelerationMultiplier)
                    : GroundDeceleration * Mathf.Max(0f, _movementProfile.DecelerationMultiplier);
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * dt);

            // 方向直接服从当前输入，速度标量独立平滑。自由移动不制造转弯半径。
            _locomotionVelocity = targetDirection * currentSpeed;
        }

        private bool IsHardTurn(Vector3 current, Vector3 target)
        {
            if (current.sqrMagnitude <= 0.0001f || target.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            float threshold = Mathf.Cos(Mathf.Clamp(HardTurnAngle, 0f, 180f) * Mathf.Deg2Rad);
            return Vector3.Dot(current.normalized, target.normalized) <= threshold;
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void ProcessHitStabilityReport(
            Collider hitCollider,
            Vector3 hitNormal,
            Vector3 hitPoint,
            Vector3 atCharacterPosition,
            Quaternion atCharacterRotation,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
}
