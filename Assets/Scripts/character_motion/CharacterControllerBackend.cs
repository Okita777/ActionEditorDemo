using UnityEngine;

namespace ActionEditor.CharacterMotion
{
    /// <summary>
    /// CharacterController 执行后端。每帧消费统一速度/旋转结果并应用到 Unity CharacterController。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(CharacterVelocity))]
    [RequireComponent(typeof(CharacterRotation))]
    public sealed class CharacterControllerBackend : MonoBehaviour
    {
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private CharacterVelocity _characterVelocity;
        [SerializeField] private CharacterRotation _characterRotation;

        private void Awake()
        {
            _characterController ??= GetComponent<CharacterController>();
            _characterVelocity ??= GetComponent<CharacterVelocity>();
            _characterRotation ??= GetComponent<CharacterRotation>();
        }

        private void Update()
        {
            if (_characterController == null || _characterVelocity == null || _characterRotation == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            _characterController.Move(_characterVelocity.GetFinalVelocity() * deltaTime);
            transform.rotation = _characterRotation.GetFinalRotation(transform.rotation, deltaTime);
        }
    }
}
