using UnityEngine;
using ActionEditor.CharacterMotion;
using SkillEditor.Preview;

namespace AsiSkillEditor.RunTime
{
    /// <summary>
    /// [AICode] 角色战斗总管理器。
    /// 它位于 SkillPlayerController 和 StateController 之上，负责统一驱动战斗相关生命周期。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterBattleManager : MonoBehaviour
    {
        [SerializeField] private SkillPlayerController _skillPlayerController;
        [SerializeField] private CustomCharacterController _characterController;
        private GameUnit _unit;
        private IUnitHitStopService _hitStopService;

        public SkillPlayerController SkillPlayerController => _skillPlayerController;

        public StateController StateController => SkillPlayerController != null ? SkillPlayerController.StateController : null;

        private void Reset()
        {
            AutoBind();
        }

        private void Awake()
        {
            AutoBind();
            _skillPlayerController?.Bind(gameObject);
            SkillPlayerController?.Initialize();
            ConfigureLocomotion();
        }

        private void Update()
        {
            float localTimeScale = _hitStopService != null ? _hitStopService.GetEffectiveTimeScale(_unit) : 1f;
            float localDeltaTime = Time.deltaTime * Mathf.Clamp01(localTimeScale);
            SkillPlayerController?.TickSkillRuntimes(localDeltaTime);
            StateController?.Tick(localDeltaTime);
            ApplyStateMovementControl();
        }

        private void OnDisable()
        {
            SkillPlayerController?.Shutdown();
        }

        private void AutoBind()
        {
            if (_skillPlayerController == null)
            {
                _skillPlayerController = new SkillPlayerController();
            }

            _characterController ??= GetComponent<CustomCharacterController>()
                ?? GetComponentInChildren<CustomCharacterController>(true);
            _unit ??= GetComponent<GameUnit>() ?? GetComponentInChildren<GameUnit>(true);
            _hitStopService ??= UnitHitStopService.ResolveOrCreate(_unit);
        }

        private void ApplyStateMovementControl()
        {
            StateSharedControlContext control = StateController != null ? StateController.SharedControlContext : null;
            if (_characterController == null || control == null)
            {
                return;
            }

            _characterController.SetStateControl(
                control.AllowMoveInput,
                control.AllowLocomotionDrive,
                control.AllowRotationInput);
            _characterController.SetMovementPolicy(StateController.MovementPolicy);
        }

        private void ConfigureLocomotion()
        {
            GameUnit unit = _unit ?? GetComponent<GameUnit>() ?? GetComponentInChildren<GameUnit>(true);
            if (_characterController == null || unit == null || string.IsNullOrWhiteSpace(unit.UnitId))
            {
                return;
            }

            UnitConfig unitConfig = SkillRuntimeLoadData.Instance.LoadUnitConfig(unit.UnitId);
            if (unitConfig == null || unitConfig.Locomotion == null)
            {
                Debug.LogError(
                    $"CharacterBattleManager: 无法加载 Unit Locomotion 配置，unitId='{unit.UnitId}'。请在 SkillEditor 中保存 Unit 以重新生成运行时 .byte。",
                    gameObject);
                return;
            }

            _characterController.Configure(unitConfig.Locomotion);
        }
    }
}
