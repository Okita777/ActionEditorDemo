using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class CheckUnitActionState : IInterruptCondition
    {
        [SerializeField] private int mCheckStateLable = 0;
        [SerializeField] private bool mCheckReverse = true;

        #region property 

        [EditorProperty("检查状态标签", EditorPropertyType.EEPT_ActionLable)]
        public int CheckStateLable
        {
            get { return mCheckStateLable; }
            set { mCheckStateLable = value; }
        }

        [EditorProperty("包含", EditorPropertyType.EEPT_Bool)]
        public bool CheckReverse
        {
            get { return mCheckReverse; }
            set { mCheckReverse = value; }
        }

        #endregion

        public int InterruptType => (int)EConditionType.EIT_CheckActionState;
        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            return actionStatePart.ActionStateMachine.CheckActionLable(mCheckStateLable) == mCheckReverse;
        }

        public IInterruptCondition Clone()
        {
            CheckUnitActionState _actionStateLable = new CheckUnitActionState();

            _actionStateLable.CheckStateLable = mCheckStateLable;
            _actionStateLable.CheckReverse = mCheckReverse;

            return _actionStateLable;
        }
    }
}