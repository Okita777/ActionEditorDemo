using System;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class CheckMoveState : IInterruptCondition
    {
        [SerializeField] private bool mIsMove = true;
        [SerializeField] private bool mIsMovePre = false;
        [NonSerialized] private bool mReturnValue = false;
        #region Property
        [EditorProperty("预输入", EditorPropertyType.EEPT_Bool)]
        public bool IsMovePre
        {
            get { return mIsMovePre; }
            set { mIsMovePre = value; }
        }
        [EditorProperty("移动输入中", EditorPropertyType.EEPT_Bool)]
        public bool IsMove
        {
            get { return mIsMove; }
            set { mIsMove = value; }
        }
        #endregion

        public int InterruptType => -(int)EInterruptTypeInternal.EIT_CheckMove;

        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            if (mIsMovePre)
            {
                mReturnValue = (actionStatePart.IsMoveInputPre || actionStatePart.ActionStateMachine.IsMoveInput) == mIsMove;
            }
            else
            {
                mReturnValue = actionStatePart.ActionStateMachine.IsMoveInput == mIsMove;
            }
            //if (EngineResourcesManager.Instance.Player == unit)
            //{
            //    if (mIsMove)
            //    {
            //        EngineDebug.LogError($"移动输入: [<color=#ff{(mReturnValue ? "cc00" : "0000")}>{mReturnValue}</color>]" +
            //            $"  pre:[{actionStatePart.IsMoveInputPre}]  move[{actionStatePart.ActionStateMachine.IsMoveInput}]" +
            //            $"\n" + EngineDebug.DebugActionStatePart(actionStatePart));
            //    }
            //}
            //else
            //{
            //    if (mIsMove)
            //        EngineDebug.LogError($"有非玩家在输入");
            //}
            return mReturnValue;
            return mIsMovePre ? (actionStatePart.IsMoveInputPre || actionStatePart.ActionStateMachine.IsMoveInput) : actionStatePart.ActionStateMachine.IsMoveInput == mIsMove;
        }

        public IInterruptCondition Clone()
        {
            CheckMoveState _CheckMoveState = new CheckMoveState();
            _CheckMoveState.IsMove = IsMove;
            _CheckMoveState.mIsMovePre = mIsMovePre;
            return _CheckMoveState;
        }
    }
}