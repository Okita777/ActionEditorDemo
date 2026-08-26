using UnityEngine;

namespace ActionEditor.CharacterMotion
{
    /// <summary>
    /// 角色速度容器。汇总本帧各来源速度影响并输出最终速度。
    /// </summary>
    public sealed class CharacterVelocity : MonoBehaviour
    {
        private Vector3 _desiredLocomotionVelocity;
        private bool _hasDesiredLocomotionVelocity;
        private Vector3 _sourceVelocity;
        private Vector3 _rootMotionVelocity;
        private Vector3 _forceVelocity;
        private Vector3 _absoluteVelocity;
        private bool _hasAbsoluteVelocity;

        [Header("Observed (Runtime)")]
        [SerializeField] private Vector3 _observedDesiredLocomotionVelocity;
        [SerializeField] private bool _observedHasDesiredLocomotionVelocity;
        [SerializeField] private Vector3 _observedSourceVelocity;
        [SerializeField] private Vector3 _observedRootMotionVelocity;
        [SerializeField] private Vector3 _observedForceVelocity;
        [SerializeField] private Vector3 _observedAbsoluteVelocity;
        [SerializeField] private bool _observedHasAbsoluteVelocity;
        [SerializeField] private Vector3 _observedFinalVelocity;

        public Vector3 ObservedFinalVelocity => _observedFinalVelocity;
        public bool HasDesiredLocomotionVelocity => _hasDesiredLocomotionVelocity;
        public bool HasAbsoluteVelocity => _hasAbsoluteVelocity;
        public Vector3 DesiredLocomotionVelocity => _desiredLocomotionVelocity;
        public Vector3 SourceVelocity => _sourceVelocity;
        public Vector3 RootMotionVelocity => _rootMotionVelocity;
        public Vector3 ForceVelocity => _forceVelocity;
        public Vector3 AbsoluteVelocity => _absoluteVelocity;

        public void ResetFrame()
        {
            _desiredLocomotionVelocity = Vector3.zero;
            _hasDesiredLocomotionVelocity = false;
            _sourceVelocity = Vector3.zero;
            _rootMotionVelocity = Vector3.zero;
            _forceVelocity = Vector3.zero;
            _absoluteVelocity = Vector3.zero;
            _hasAbsoluteVelocity = false;

            _observedDesiredLocomotionVelocity = Vector3.zero;
            _observedHasDesiredLocomotionVelocity = false;
            _observedSourceVelocity = Vector3.zero;
            _observedRootMotionVelocity = Vector3.zero;
            _observedForceVelocity = Vector3.zero;
            _observedAbsoluteVelocity = Vector3.zero;
            _observedHasAbsoluteVelocity = false;
            _observedFinalVelocity = Vector3.zero;
        }

        public void SetDesiredLocomotionVelocity(Vector3 value, string sourceTag)
        {
            _desiredLocomotionVelocity = value;
            _hasDesiredLocomotionVelocity = true;
            _observedDesiredLocomotionVelocity = value;
            _observedHasDesiredLocomotionVelocity = true;
        }

        public void AddSourceVelocity(Vector3 value, string sourceTag)
        {
            _sourceVelocity += value;
            _observedSourceVelocity = _sourceVelocity;
        }

        public void AddRootMotionVelocity(Vector3 value)
        {
            _rootMotionVelocity += value;
            _observedRootMotionVelocity = _rootMotionVelocity;
        }

        public void SetRootMotionVelocity(Vector3 value)
        {
            _rootMotionVelocity = value;
            _observedRootMotionVelocity = value;
        }

        public void AddForceVelocity(Vector3 value, string sourceTag)
        {
            _forceVelocity += value;
            _observedForceVelocity = _forceVelocity;
        }

        public void SetAbsoluteVelocity(Vector3 value, string sourceTag)
        {
            _absoluteVelocity = value;
            _hasAbsoluteVelocity = true;
            _observedAbsoluteVelocity = _absoluteVelocity;
            _observedHasAbsoluteVelocity = true;
        }

        public Vector3 GetFinalVelocity()
        {
            Vector3 baseVelocity = _hasAbsoluteVelocity ? _absoluteVelocity : _desiredLocomotionVelocity;
            _observedFinalVelocity = baseVelocity + _sourceVelocity + _rootMotionVelocity + _forceVelocity;
            _observedDesiredLocomotionVelocity = _desiredLocomotionVelocity;
            _observedHasDesiredLocomotionVelocity = _hasDesiredLocomotionVelocity;
            _observedSourceVelocity = _sourceVelocity;
            _observedRootMotionVelocity = _rootMotionVelocity;
            _observedForceVelocity = _forceVelocity;
            _observedAbsoluteVelocity = _absoluteVelocity;
            _observedHasAbsoluteVelocity = _hasAbsoluteVelocity;
            return _observedFinalVelocity;
        }
    }
}
