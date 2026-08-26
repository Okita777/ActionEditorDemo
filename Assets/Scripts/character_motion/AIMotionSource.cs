using UnityEngine;

namespace ActionEditor.CharacterMotion
{
    /// <summary>
    /// AI 运动源。将 AI 的期望方向与速度转换为速度与朝向影响。
    /// </summary>
    public sealed class AIMotionSource : MonoBehaviour, IMotionSource
    {
        public bool EnableMove = true;
        public bool EnableRotate = true;

        public Vector3 DesiredDirection;
        public float DesiredSpeed = 4f;
        public float RotationSharpness = 10f;

        public void Collect(CharacterVelocity velocity, CharacterRotation rotation, float deltaTime)
        {
            if (DesiredDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Vector3 direction = DesiredDirection.normalized;
            if (EnableMove)
            {
                velocity.SetDesiredLocomotionVelocity(direction * Mathf.Max(0f, DesiredSpeed), "AI");
            }

            if (EnableRotate)
            {
                rotation.AddLookDirection(direction, RotationSharpness, "AI");
            }
        }
    }
}
