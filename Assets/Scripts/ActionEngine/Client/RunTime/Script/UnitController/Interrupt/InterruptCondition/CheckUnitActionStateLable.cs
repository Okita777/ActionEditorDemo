using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class CheckUnitActionStateLable : IInterruptCondition
    {
        [SerializeField] private bool mCheckOnHit = false;
        [SerializeField] private int mCheckStateLable = 0;
        [SerializeField] private bool mCheckReverse = true;

        #region property 

        [EditorProperty("检查命中对象标签", EditorPropertyType.EEPT_Bool)]
        public bool CheckOnHit
        {
            get { return mCheckOnHit; }
            set { mCheckOnHit = value; }
        }
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

        public int InterruptType => (int)EConditionType.EIT_CheckActionLable;
        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            ActionStateMachine _stateMachine = actionStatePart.ActionStateMachine;
            if (mCheckOnHit)
            {
                if (_stateMachine.HitUnit is not null)
                {
                    // EngineDebug.LogWarning($"收到受击了 {_stateMachine.OnHitUnit.ActionStateMachine.GetActionLableList.Count}");
                    return _stateMachine.HitUnit.GetUnit().ActionStateMachine.CheckActionLable(mCheckStateLable) == mCheckReverse;
                }
                else
                {
                    return false;
                }
            }
            return _stateMachine.CheckActionLable(mCheckStateLable) == mCheckReverse;
        }

        public IInterruptCondition Clone()
        {
            CheckUnitActionStateLable _actionStateLable = new CheckUnitActionStateLable();

            _actionStateLable.CheckStateLable = mCheckStateLable;
            _actionStateLable.CheckReverse = mCheckReverse;
            _actionStateLable.CheckOnHit = mCheckOnHit;

            return _actionStateLable;
        }
    }
}