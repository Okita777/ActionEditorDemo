using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class CheckLayerJumpState : IInterruptCondition
    {
        public int InterruptType => -(int)EInterruptTypeInternal.EIT_CheckLayerJumpState;
        [SerializeField] public int mCheckLayer = 0;
        [SerializeField] public bool mIsContains = false;

        #region Property
        [EditorProperty("判断层级", EditorPropertyType.EEPT_ActionLayer)]
        public int CheckLayer
        {
            get { return mCheckLayer; }
            set { mCheckLayer = value; }
        }
        [EditorProperty("包含", EditorPropertyType.EEPT_Bool)]
        public bool IsContains
        {
            get { return mIsContains; }
            set { mIsContains = value; }
        }
        #endregion
        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            if (mCheckLayer < 0) return (actionStatePart.JumpLayerList.Count > 0) == IsContains;
            return actionStatePart.JumpLayerList.Contains(mCheckLayer) == IsContains;
        }

        public IInterruptCondition Clone()
        {
            CheckLayerJumpState _check = new CheckLayerJumpState();
            _check.CheckLayer = mCheckLayer;
            _check.IsContains = mIsContains;
            return _check;
        }
    }
}