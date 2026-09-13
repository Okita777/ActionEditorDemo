using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class CheckGValue : IInterruptCondition
    {
        [SerializeField] private GValue_Ratio mGValue_Ratio = new();

        #region Property
        [EditorProperty("GValue判断列表", EditorPropertyType.EEPT_GValueSRatio)]
        public GValue_Ratio GValue_Ratio
        {
            get { return mGValue_Ratio; }
            set { mGValue_Ratio = value; }
        }
        #endregion

        public int InterruptType => (int)EConditionType.EIT_CheckGValue;
        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            return GValue_Ratio.CheckValue(actionStatePart.ActionStateMachine);
        }

        public IInterruptCondition Clone()
        {
            CheckGValue _check = new CheckGValue();
#if UNITY_EDITOR
            _check.GValue_Ratio = mGValue_Ratio.Clone();
#else
            _check.GValue_Ratio = mGValue_Ratio;
#endif
            return _check;
        }
    }
}