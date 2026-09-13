using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class CheckInputDir : IInterruptCondition
    {
        #region Enum
        public enum EInputDir
        {
            Up,
            Dwon,
            Left,
            Right,
        }
        #endregion

        [SerializeField] protected EInputDir mInputDir = EInputDir.Up;
        [SerializeField] protected bool mIsMoveInput = true;

        #region Property

        [EditorProperty("移动输入方向", EditorPropertyType.EEPT_Enum)]
        public EInputDir InputDir
        {
            get { return mInputDir; }
            set { mInputDir = value; }
        }
        [EditorProperty("移动输入中", EditorPropertyType.EEPT_Bool)]
        public bool IsMoveInput
        {
            get { return mIsMoveInput; }
            set { mIsMoveInput = value; }
        }

        #endregion

        public int InterruptType => (int)EConditionType.EIT_CheckInputDir;
        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            ActionStateMachine _stateMachine = actionStatePart.ActionStateMachine;

            if (_stateMachine.IsMoveInput == mIsMoveInput)
            {
                Vector2 _moveDir = new Vector2(_stateMachine.PlayerInputMoveDir.x, _stateMachine.PlayerInputMoveDir.z);
                float _inputAngle = Vector2.SignedAngle(Vector2.up, _moveDir);
                if (mInputDir == EInputDir.Up)
                {
                    return Mathf.Abs(_inputAngle) < 45;
                }
                else if (mInputDir == EInputDir.Dwon)
                {
                    return Mathf.Abs(_inputAngle) > 135;
                }
                else if (mInputDir == EInputDir.Left)
                {
                    return _inputAngle < -45 && _inputAngle > -135;
                }
                else
                {
                    return _inputAngle > 45 && _inputAngle < 135;
                }
            }

            return false;
        }

        public IInterruptCondition Clone()
        {
            CheckInputDir _check = new CheckInputDir();

            _check.InputDir = mInputDir;
            _check.IsMoveInput = mIsMoveInput;

            return _check;
        }
    }
}