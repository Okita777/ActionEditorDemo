using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class CheckBluePrintBool : IInterruptCondition
    {
        [SerializeReference] protected GraphEvent_NoValue_Bool mCheck = new GraphEvent_NoValue_Bool();
        [SerializeField] protected bool mIsValid = true;

        #region Property

        [EditorProperty("判定", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Bool Check
        {
            get { return mCheck; }
            set { mCheck = value; }
        }
        [EditorProperty("蓝图返回为True时", EditorPropertyType.EEPT_Bool)]
        public bool IsValid
        {
            get { return mIsValid; }
            set { mIsValid = value; }
        }
        #endregion

        public int InterruptType => -(int)EInterruptTypeInternal.EIT_BluePrintBool;
        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            return mCheck.value(actionStatePart, new ActionMachineTime()) == mIsValid;
        }

        public IInterruptCondition Clone()
        {
            CheckBluePrintBool _CheckCostomKey = new CheckBluePrintBool();
            _CheckCostomKey.Check = mCheck.Clone();
            _CheckCostomKey.IsValid = mIsValid;

            return _CheckCostomKey;
        }
    }
}