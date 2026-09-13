using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class CheckActionState : IInterruptCondition
    {
        [SerializeField] private int mActionID = 0;
        [SerializeField] private bool mInclude = true;

        #region Property
        [EditorProperty("单位行为", EditorPropertyType.EEPT_Action)]
        public int ActionID
        {
            get { return mActionID; }
            set { mActionID = value; }
        }
        [EditorProperty("包含", EditorPropertyType.EEPT_Bool)]
        public bool Include
        {
            get { return mInclude; }
            set { mInclude = value; }
        }
        #endregion

        public int InterruptType => -(int)EInterruptTypeInternal.EIT_CheckActionState;

        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            // bool _isInclude = false;

            ActionStateMachine _actionStateMachine = actionStatePart.ActionStateMachine;
            if (mInclude)
            {
                foreach (ActionStatePart _actionStatePart in _actionStateMachine.AllActionStatePart)
                {
                    if (_actionStatePart.ActionEnble && _actionStatePart.CurrentActionState.ID == mActionID)
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (ActionStatePart _actionStatePart in _actionStateMachine.AllActionStatePart)
            {
                if (_actionStatePart.ActionEnble && _actionStatePart.CurrentActionState.ID == mActionID)
                {
                    return false;
                }
            }
            return true;
        }

        public IInterruptCondition Clone()
        {
            CheckActionState _CheckMoveState = new CheckActionState();
            _CheckMoveState.Include = mInclude;
            _CheckMoveState.ActionID = mActionID;
            return _CheckMoveState;
        }
    }
}