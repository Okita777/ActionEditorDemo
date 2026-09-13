using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class CheckGround : IInterruptCondition
    {
        [SerializeField] private bool mIsGround = true;

        #region Property
        [EditorProperty("角色在地面上", EditorPropertyType.EEPT_Bool)]
        public bool IsGround
        {
            get { return mIsGround; }
            set { mIsGround = value; }
        }
        #endregion
        public int InterruptType => (int)EConditionType.EIT_CheckGround;
        private int m_lastPosY;
        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            ActionStateMachine _stateMachine = actionStatePart.ActionStateMachine;
            // if (_stateMachine.TryGetStaticLogic(out Ex_UnitGroundState _groundState2))
            // {
            //     if (_groundState2.IsGround == mIsGround)
            //         return true;
            //     // return _groundState2.IsGround == mIsGround;
            // }


            // if (_stateMachine.TryGetComponent(out CharacterController _controller))
            // {
            //     Debug.Log("地面：" + _controller.isGrounded);
            //     return _controller.isGrounded == mIsGround;
            // }


            if (_stateMachine.TryGetLogic(out Ex_Update_CharacterControl _groundState, nameof(Ex_Update_CharacterControl)))
            {
                // EngineDebug.Log($"奇怪的Debug: {_groundState.ChackGround_Character == mIsGround}");
                return _groundState.ChackGround_Character == mIsGround;
            }
            return false;
        }
        public IInterruptCondition Clone()
        {
            CheckGround _checkGround = new CheckGround();

            _checkGround.IsGround = mIsGround;

            return _checkGround;
        }
    }
}