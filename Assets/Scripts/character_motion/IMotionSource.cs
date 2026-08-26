using UnityEngine;

namespace ActionEditor.CharacterMotion
{
    /// <summary>
    /// 运动影响源接口。实现者负责在每帧向速度和旋转容器写入自身影响。
    /// </summary>
    public interface IMotionSource
    {
        void Collect(CharacterVelocity velocity, CharacterRotation rotation, float deltaTime);
    }
}
