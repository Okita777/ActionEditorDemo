using ActionEditor.InputSystem;
using AsiSkillEditor.RunTime;
using Cinemachine;
using SkillEditor.Preview;
using UnityEngine;

namespace ActionEditor.CameraSystem
{
    /// <summary>
    /// 基础第三人称相机 Rig。消费统一 Look 输入，维护角色 CameraAnchor 的世界 yaw/pitch，
    /// 并把相机预制体中的 Gameplay VCam 绑定到角色挂点。
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class GameplayCameraRigController : MonoBehaviour, ICameraBasisProvider
    {
        [Header("Camera Groups")]
        [SerializeField] private Transform _defaultGroup;
        [SerializeField] private Transform _lockGroup;
        [SerializeField] private CinemachineVirtualCamera _gameplayCamera;
        [SerializeField] private CinemachineVirtualCamera _lockCamera;

        [Header("Look")]
        [SerializeField, Min(0f)] private float _yawSensitivity = 3f;
        [SerializeField, Min(0f)] private float _pitchSensitivity = 2f;
        [SerializeField] private float _minimumPitch = -35f;
        [SerializeField] private float _maximumPitch = 70f;
        [SerializeField] private bool _invertY;
        [SerializeField] private bool _lookInputEnabled = true;

        [SerializeField] private Transform _mainCameraAnchor;
        [SerializeField] private CharacterInputDriver _inputDriver;
        [SerializeField] private Camera _outputCamera;
        private Transform _followAnchor;
        private Transform _aimAnchor;
        private float _yaw;
        private float _pitch;
        private Vector3 _lastPlanarForward = Vector3.forward;
        private Transform _lockTarget;
        private Vector3 _lockTargetOffset;
        private float _lockViewPivotHeightOffset;
        private Transform _lockAimProxy;
        private CinemachineTransposer _activeLockTransposer;
        private CinemachineTransposer.BindingMode _bindingModeBeforeLock;
        private bool _isLocked;

        public bool IsAvailable => _followAnchor != null && _gameplayCamera != null;
        public bool IsLocked => _isLocked;
        public Vector3 TargetingOrigin => _followAnchor != null ? _followAnchor.position : transform.position;

        public Transform ViewTransform => _outputCamera != null
            ? _outputCamera.transform
            : (_followAnchor != null ? _followAnchor : transform);

        public Vector3 PlanarForward
        {
            get
            {
                UpdatePlanarBasis();
                return _lastPlanarForward;
            }
        }

        public Vector3 PlanarRight
        {
            get
            {
                Vector3 right = Vector3.Cross(Vector3.up, PlanarForward);
                return right.sqrMagnitude > 0.000001f ? right.normalized : Vector3.right;
            }
        }

        public Ray ViewCenterRay
        {
            get
            {
                if (_outputCamera != null)
                {
                    return _outputCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                }

                Transform view = ViewTransform;
                return new Ray(view.position, view.forward);
            }
        }

        public CinemachineVirtualCamera GameplayCamera => _gameplayCamera;
        public Camera OutputCamera => _outputCamera != null ? _outputCamera : Camera.main;

        public bool LookInputEnabled
        {
            get => _lookInputEnabled;
            set => _lookInputEnabled = value;
        }

        private void Awake()
        {
            ResolveRigComponents();
            RestoreSerializedBinding();
        }

        private void OnEnable()
        {
            RestoreSerializedBinding();
        }

        private void OnValidate()
        {
            _yawSensitivity = Mathf.Max(0f, _yawSensitivity);
            _pitchSensitivity = Mathf.Max(0f, _pitchSensitivity);
            if (_minimumPitch > _maximumPitch)
            {
                float value = _minimumPitch;
                _minimumPitch = _maximumPitch;
                _maximumPitch = value;
            }

            ResolveRigComponents();
        }

        private void Update()
        {
            if (IsLocked || !_lookInputEnabled || _inputDriver == null)
            {
                return;
            }

            CharacterInputFrame frame = _inputDriver.CurrentFrame;
            if (frame == null)
            {
                return;
            }

            Vector2 look = frame.LookAxis;
            _yaw += look.x * _yawSensitivity;
            float pitchSign = _invertY ? 1f : -1f;
            _pitch = Mathf.Clamp(_pitch + look.y * _pitchSensitivity * pitchSign, _minimumPitch, _maximumPitch);
        }

        private void LateUpdate()
        {
            if (_followAnchor == null)
            {
                return;
            }

            if (IsLocked && _lockTarget == null)
            {
                ClearLockTarget();
            }

            if (IsLocked)
            {
                if (_lockAimProxy != null)
                {
                    UpdateLockViewPivot();
                }
            }
            else
            {
                Quaternion cameraRotation = Quaternion.Euler(_pitch, _yaw, 0f);
                _followAnchor.rotation = cameraRotation;
                if (_aimAnchor != null && _aimAnchor != _followAnchor)
                {
                    _aimAnchor.rotation = cameraRotation;
                }
            }

            UpdatePlanarBasis();
        }

        /// <summary>将该 Rig 绑定到一个角色。相机预制体中的 Follow/LookAt 可保持为空。</summary>
        public bool Bind(Transform mainCameraAnchor, CharacterInputDriver inputDriver, Camera outputCamera = null)
        {
            ResolveRigComponents();
            if (mainCameraAnchor == null || _gameplayCamera == null)
            {
                return false;
            }

            _mainCameraAnchor = mainCameraAnchor;
            _inputDriver = inputDriver;
            _followAnchor = mainCameraAnchor;
            _aimAnchor = mainCameraAnchor;
            _outputCamera = outputCamera != null ? outputCamera : Camera.main;
            CameraFeedbackService feedbackService = GetComponent<CameraFeedbackService>() ??
                gameObject.AddComponent<CameraFeedbackService>();
            SkillEditor.Preview.GameUnit owner = mainCameraAnchor.GetComponentInParent<SkillEditor.Preview.GameUnit>(true);
            feedbackService.Configure(_outputCamera, owner);

            Vector3 initialForward = _followAnchor.forward;
            if (initialForward.sqrMagnitude <= 0.000001f)
            {
                initialForward = Vector3.forward;
            }

            Vector3 euler = Quaternion.LookRotation(initialForward.normalized, Vector3.up).eulerAngles;
            _yaw = euler.y;
            _pitch = NormalizeSignedAngle(euler.x);
            _pitch = Mathf.Clamp(_pitch, _minimumPitch, _maximumPitch);

            _gameplayCamera.Follow = _followAnchor;
            _gameplayCamera.LookAt = _aimAnchor;
            _gameplayCamera.enabled = true;
            if (_lockCamera != null)
            {
                _lockCamera.Follow = _followAnchor;
                _lockCamera.LookAt = _aimAnchor;
                _lockCamera.enabled = false;
            }

            LateUpdate();
            ConfigureHardLockController();
            return true;
        }

        /// <summary>
        /// 切入 Camera Only 锁定。继续使用 Gameplay VCam，避免切换不同 Lens/Body 配置造成机位突变；
        /// VCam 改为跟随抬高后的玩家视点代理，代理的正方向始终由该视点指向目标。
        /// </summary>
        public bool SetLockTarget(
            Transform aimPoint,
            Vector3 aimOffset = default,
            float viewPivotHeightOffset = 1f)
        {
            ResolveRigComponents();
            if (aimPoint == null || _followAnchor == null || _gameplayCamera == null)
            {
                return false;
            }

            EnsureLockAimProxy();
            _lockTarget = aimPoint;
            _lockTargetOffset = aimOffset;
            _lockViewPivotHeightOffset = viewPivotHeightOffset;
            _isLocked = true;
            UpdateLockViewPivot();
            _gameplayCamera.Follow = _lockAimProxy;
            _gameplayCamera.LookAt = _lockAimProxy;
            EnableLockBindingMode();
            _gameplayCamera.enabled = true;
            if (_lockCamera != null)
            {
                _lockCamera.enabled = false;
            }

            return true;
        }

        /// <summary>回到自由相机，并从实际输出镜头同步 yaw/pitch，避免解除时跳镜头。</summary>
        public void ClearLockTarget()
        {
            if (!IsLocked)
            {
                return;
            }

            SyncAnglesFromOutputCamera();
            RestoreBindingModeAfterLock();
            _isLocked = false;
            _lockTarget = null;
            _lockTargetOffset = Vector3.zero;
            _lockViewPivotHeightOffset = 0f;
            if (_gameplayCamera != null)
            {
                _gameplayCamera.Follow = _followAnchor;
                _gameplayCamera.LookAt = _aimAnchor;
                _gameplayCamera.enabled = true;
            }

            if (_lockCamera != null)
            {
                _lockCamera.enabled = false;
                _lockCamera.LookAt = _aimAnchor;
            }

            if (_followAnchor != null)
            {
                _followAnchor.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }
        }

        public void Unbind()
        {
            ClearLockTarget();
            CameraFeedbackService feedbackService = GetComponent<CameraFeedbackService>();
            feedbackService?.Configure(_outputCamera, null);
            if (_gameplayCamera != null)
            {
                _gameplayCamera.Follow = null;
                _gameplayCamera.LookAt = null;
            }

            if (_lockCamera != null)
            {
                _lockCamera.Follow = null;
                _lockCamera.LookAt = null;
            }

            _mainCameraAnchor = null;
            _inputDriver = null;
            _followAnchor = null;
            _aimAnchor = null;
            _outputCamera = null;
        }

        public bool ValidateConfiguration(out string errorMessage)
        {
            ResolveRigComponents();
            if (_defaultGroup == null)
            {
                errorMessage = "Camera Prefab 根节点下缺少 Default 相机组。";
                return false;
            }

            if (_lockGroup == null)
            {
                errorMessage = "Camera Prefab 根节点下缺少 Lock 相机组。";
                return false;
            }

            if (_gameplayCamera == null)
            {
                errorMessage = "Camera Prefab 的 Default 组下缺少 camera1/CinemachineVirtualCamera。";
                return false;
            }

            if (_lockCamera == null)
            {
                errorMessage = "Camera Prefab 的 Lock 组下缺少 camera1/CinemachineVirtualCamera。";
                return false;
            }

            if (_gameplayCamera.GetCinemachineComponent<CinemachineComposer>() == null)
            {
                errorMessage = "默认 Gameplay VCam 的 Aim 必须配置为 Composer。";
                return false;
            }

            if (_gameplayCamera.GetComponent<CinemachineCollider>() == null)
            {
                errorMessage = "默认 Gameplay VCam 缺少 CinemachineCollider。";
                return false;
            }

            if (_lockCamera.GetCinemachineComponent<CinemachineComposer>() == null)
            {
                errorMessage = "Lock VCam 的 Aim 必须配置为 Composer。";
                return false;
            }

            if (_lockCamera.GetComponent<CinemachineCollider>() == null)
            {
                errorMessage = "Lock VCam 缺少 CinemachineCollider。";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private void ResolveRigComponents()
        {
            _defaultGroup ??= FindDirectChild(transform, "Default");
            _lockGroup ??= FindDirectChild(transform, "Lock");
            _gameplayCamera ??= ResolveGroupCamera(_defaultGroup);
            _lockCamera ??= ResolveGroupCamera(_lockGroup);
        }

        private void RestoreSerializedBinding()
        {
            if (_mainCameraAnchor != null)
            {
                Bind(_mainCameraAnchor, _inputDriver, _outputCamera);
            }
        }

        private static Transform FindDirectChild(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return null;
        }

        private static CinemachineVirtualCamera ResolveGroupCamera(Transform group)
        {
            Transform cameraTransform = FindDirectChild(group, "camera1");
            return cameraTransform != null ? cameraTransform.GetComponent<CinemachineVirtualCamera>() : null;
        }

        private void UpdatePlanarBasis()
        {
            Vector3 candidate = IsLocked && _outputCamera != null
                ? Vector3.ProjectOnPlane(_outputCamera.transform.forward, Vector3.up)
                : Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
            if (candidate.sqrMagnitude > 0.000001f)
            {
                _lastPlanarForward = candidate.normalized;
            }
        }

        private void EnsureLockAimProxy()
        {
            if (_lockAimProxy != null)
            {
                return;
            }

            GameObject proxy = new GameObject("__HardLockAimProxy__");
            proxy.hideFlags = HideFlags.HideAndDontSave;
            proxy.transform.SetParent(transform, false);
            _lockAimProxy = proxy.transform;
        }

        private void UpdateLockViewPivot()
        {
            if (_lockAimProxy == null || _followAnchor == null || _lockTarget == null)
            {
                return;
            }

            Vector3 pivotPosition = _followAnchor.position + Vector3.up * _lockViewPivotHeightOffset;
            Vector3 targetPosition = _lockTarget.TransformPoint(_lockTargetOffset);
            Vector3 targetDirection = targetPosition - pivotPosition;

            _lockAimProxy.SetPositionAndRotation(
                pivotPosition,
                targetDirection.sqrMagnitude > 0.000001f
                    ? Quaternion.LookRotation(targetDirection.normalized, Vector3.up)
                    : _followAnchor.rotation);
        }

        private void EnableLockBindingMode()
        {
            _activeLockTransposer = _gameplayCamera != null
                ? _gameplayCamera.GetCinemachineComponent<CinemachineTransposer>()
                : null;
            if (_activeLockTransposer == null)
            {
                return;
            }

            _bindingModeBeforeLock = _activeLockTransposer.m_BindingMode;
            _activeLockTransposer.m_BindingMode = CinemachineTransposer.BindingMode.LockToTargetNoRoll;
        }

        private void RestoreBindingModeAfterLock()
        {
            if (_activeLockTransposer == null)
            {
                return;
            }

            _activeLockTransposer.m_BindingMode = _bindingModeBeforeLock;
            _activeLockTransposer = null;
        }

        private void SyncAnglesFromOutputCamera()
        {
            Transform output = _outputCamera != null ? _outputCamera.transform : null;
            if (output == null)
            {
                return;
            }

            Vector3 euler = output.rotation.eulerAngles;
            _yaw = euler.y;
            _pitch = Mathf.Clamp(NormalizeSignedAngle(euler.x), _minimumPitch, _maximumPitch);
        }

        private void ConfigureHardLockController()
        {
            GameUnit ownerUnit = _mainCameraAnchor != null
                ? _mainCameraAnchor.GetComponentInParent<GameUnit>(true)
                : null;
            if (ownerUnit == null)
            {
                return;
            }

            UnitConfig unitConfig = SkillRuntimeLoadData.Instance.LoadUnitConfig(ownerUnit.UnitId);
            HardLockTargetingController targeting = GetComponent<HardLockTargetingController>();
            if (targeting == null)
            {
                targeting = gameObject.AddComponent<HardLockTargetingController>();
            }

            targeting.Configure(unitConfig != null ? unitConfig.HardLock : null, ownerUnit, _inputDriver, this, _outputCamera);
        }

        private static float NormalizeSignedAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
