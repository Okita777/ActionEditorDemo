using SkillEditor.Preview;
using UnityEngine;

namespace ActionEditor.CameraSystem
{
    /// <summary>声明可被纯视角硬锁定选中的目标及其瞄准点。</summary>
    [DisallowMultipleComponent]
    public sealed class HardLockTarget : MonoBehaviour
    {
        [SerializeField] private Transform _aimPoint;
        [SerializeField] private Vector3 _aimOffset;
        [SerializeField] private bool _lockEnabled = true;
        [SerializeField] private int _priority;

        public Transform AimPoint => _aimPoint != null ? _aimPoint : transform;
        public Vector3 AimOffset => _aimOffset;
        public Vector3 AimPosition => AimPoint.TransformPoint(_aimOffset);
        public bool LockEnabled
        {
            get => _lockEnabled;
            set => _lockEnabled = value;
        }
        public int Priority => _priority;
        public GameUnit Unit => GetComponent<GameUnit>() ?? GetComponentInParent<GameUnit>(true);
    }
}
