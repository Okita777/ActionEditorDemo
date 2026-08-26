using UnityEngine;

namespace ActionEditor.CameraSystem
{
    /// <summary>向移动、锁定与瞄准系统提供稳定的只读视角事实。</summary>
    public interface ICameraBasisProvider
    {
        bool IsAvailable { get; }
        Transform ViewTransform { get; }
        Vector3 PlanarForward { get; }
        Vector3 PlanarRight { get; }
        Ray ViewCenterRay { get; }
    }
}
