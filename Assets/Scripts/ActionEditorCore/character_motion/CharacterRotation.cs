using UnityEngine;

namespace ActionEditor.CharacterMotion
{
    /// <summary>
    /// 角色旋转容器。汇总本帧朝向与旋转增量影响并输出最终旋转。
    /// </summary>
    public sealed class CharacterRotation : MonoBehaviour
    {
        private bool _hasAbsoluteRotation;
        private Quaternion _absoluteRotation = Quaternion.identity;
        private int _absoluteRotationPriority = int.MinValue;

        private bool _hasLookDirection;
        private Vector3 _lookDirection = Vector3.forward;
        private float _lookSharpness = 10f;
        private int _lookDirectionPriority = int.MinValue;

        private Quaternion _rotationDelta = Quaternion.identity;

        [Header("Observed (Runtime)")]
        [SerializeField] private bool _observedHasAbsoluteRotation;
        [SerializeField] private Vector3 _observedAbsoluteEuler;
        [SerializeField] private bool _observedHasLookDirection;
        [SerializeField] private Vector3 _observedLookDirection;
        [SerializeField] private float _observedLookSharpness;
        [SerializeField] private Vector3 _observedRotationDeltaEuler;
        [SerializeField] private Vector3 _observedFinalEuler;

        public Vector3 ObservedFinalEuler => _observedFinalEuler;
        public bool HasLookDirection => _hasLookDirection;
        public Vector3 LookDirection => _lookDirection;
        public bool HasAbsoluteRotation => _hasAbsoluteRotation;
        public Quaternion AbsoluteRotation => _absoluteRotation;
        public Quaternion RotationDelta => _rotationDelta;

        public void ResetFrame()
        {
            _hasAbsoluteRotation = false;
            _absoluteRotation = Quaternion.identity;
            _absoluteRotationPriority = int.MinValue;
            _hasLookDirection = false;
            _lookDirection = Vector3.forward;
            _lookSharpness = 10f;
            _lookDirectionPriority = int.MinValue;
            _rotationDelta = Quaternion.identity;

            _observedHasAbsoluteRotation = false;
            _observedAbsoluteEuler = Vector3.zero;
            _observedHasLookDirection = false;
            _observedLookDirection = Vector3.zero;
            _observedLookSharpness = 0f;
            _observedRotationDeltaEuler = Vector3.zero;
            _observedFinalEuler = Vector3.zero;
        }

        public void AddLookDirection(Vector3 direction, float sharpness, string sourceTag)
        {
            AddLookDirection(direction, sharpness, sourceTag, 0);
        }

        public void AddLookDirection(Vector3 direction, float sharpness, string sourceTag, int priority)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon || (_hasLookDirection && priority < _lookDirectionPriority))
            {
                return;
            }

            _hasLookDirection = true;
            _lookDirection = direction.normalized;
            _lookSharpness = Mathf.Max(0f, sharpness);
            _lookDirectionPriority = priority;

            _observedHasLookDirection = true;
            _observedLookDirection = _lookDirection;
            _observedLookSharpness = _lookSharpness;
        }

        public void AddRotationDelta(Quaternion delta, string sourceTag)
        {
            _rotationDelta = delta * _rotationDelta;
            _observedRotationDeltaEuler = _rotationDelta.eulerAngles;
        }

        public void SetAbsoluteRotation(Quaternion rotation, string sourceTag)
        {
            SetAbsoluteRotation(rotation, sourceTag, 0);
        }

        public void SetAbsoluteRotation(Quaternion rotation, string sourceTag, int priority)
        {
            if (_hasAbsoluteRotation && priority < _absoluteRotationPriority)
            {
                return;
            }

            _absoluteRotation = rotation;
            _hasAbsoluteRotation = true;
            _absoluteRotationPriority = priority;

            _observedHasAbsoluteRotation = true;
            _observedAbsoluteEuler = _absoluteRotation.eulerAngles;
        }

        public Quaternion GetFinalRotation(Quaternion currentRotation, float deltaTime)
        {
            bool useAbsoluteRotation = _hasAbsoluteRotation &&
                (!_hasLookDirection || _absoluteRotationPriority >= _lookDirectionPriority);
            bool useLookDirection = _hasLookDirection &&
                (!_hasAbsoluteRotation || _lookDirectionPriority >= _absoluteRotationPriority);
            Quaternion result = useAbsoluteRotation ? _absoluteRotation : currentRotation;

            if (useLookDirection)
            {
                Quaternion lookRotation = Quaternion.LookRotation(_lookDirection, Vector3.up);
                float t = 1f - Mathf.Exp(-_lookSharpness * Mathf.Max(0f, deltaTime));
                result = Quaternion.Slerp(result, lookRotation, t);
            }

            result = _rotationDelta * result;
            Quaternion normalized = Quaternion.Normalize(result);

            _observedHasAbsoluteRotation = _hasAbsoluteRotation;
            _observedAbsoluteEuler = _absoluteRotation.eulerAngles;
            _observedHasLookDirection = _hasLookDirection;
            _observedLookDirection = _lookDirection;
            _observedLookSharpness = _lookSharpness;
            _observedRotationDeltaEuler = _rotationDelta.eulerAngles;
            _observedFinalEuler = normalized.eulerAngles;

            return normalized;
        }
    }
}
