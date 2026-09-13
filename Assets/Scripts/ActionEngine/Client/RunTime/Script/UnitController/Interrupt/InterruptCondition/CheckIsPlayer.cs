using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class CheckIsPlayer : IInterruptCondition
    {
        [SerializeField] private bool mIsPlayer = true;

        #region Property
        [EditorProperty("当前单位为玩家", EditorPropertyType.EEPT_Bool)]
        public bool IsPlayer
        {
            get => mIsPlayer;
            set => mIsPlayer = value;
        }
        #endregion
        public int InterruptType => (int)EConditionType.EIT_CheckIsPlayer;
        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            ActionStateMachine _stateMachine = actionStatePart.ActionStateMachine;

            return ActionEngineManager_Input.Instance.IsPlayer(_stateMachine.CurUnit) == mIsPlayer;
        }
        public IInterruptCondition Clone()
        {
            CheckIsPlayer _checkGround = new CheckIsPlayer();

            _checkGround.IsPlayer = mIsPlayer;

            return _checkGround;
        }
    }
}