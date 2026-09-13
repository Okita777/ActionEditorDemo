using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class CheckBarrierData : IInterruptCondition
    {
        [SerializeField] private float mCheckHeight_G;
        [SerializeField] private float mCheckHeight_Gm;
        [SerializeField] private float mCheckDis_L;
        [SerializeField] private float mCheckWeight_L;

        #region property
        [EditorProperty("检查高度（大于）", EditorPropertyType.EEPT_Float)]
        public float CheckHeight_G
        {
            get { return mCheckHeight_G; }
            set { mCheckHeight_G = value; }
        }
        [EditorProperty("检查高度（小于）", EditorPropertyType.EEPT_Float)]
        public float CheckHeight_Gm
        {
            get { return mCheckHeight_Gm; }
            set { mCheckHeight_Gm = value; }
        }
        [EditorProperty("检查距离（小于）", EditorPropertyType.EEPT_Float)]
        public float CheckDis_L
        {
            get { return mCheckDis_L; }
            set { mCheckDis_L = value; }
        }
        [EditorProperty("检查厚度（小于）(值小于等于0时不检测厚度)", EditorPropertyType.EEPT_Float, LabelWidth = 260)]
        public float CheckWeight_L
        {
            get { return mCheckWeight_L; }
            set { mCheckWeight_L = value; }
        }
        #endregion

        public int InterruptType => (int)EConditionType.EIT_CheckBarrier;
        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            if (actionStatePart.ActionStateMachine.TryGetStaticLogic(out Ex_BarrierData _barrier, nameof(Ex_BarrierData)))
            {
                // _barrier.mStartHeight = 0.5f;//最低高度
                _barrier.mMaxHeight = mCheckHeight_Gm;//最高高度
                _barrier.mInteractDis = mCheckDis_L;//最远距离
                _barrier.mMaxWeight = mCheckWeight_L;//最厚厚度
                _barrier.OnCheck(actionStatePart.ActionStateMachine);

                if (_barrier.mGetDistance < mCheckDis_L)
                {
                    if (_barrier.mGetHeight > mCheckHeight_G)
                    {
                        if (mCheckWeight_L > 0)
                        {
                            if (_barrier.mGetWeight > 0 && _barrier.mGetWeight < (mCheckWeight_L * mCheckWeight_L))
                            {

                                _barrier.SetInteractPoint();

                                return true;
                            }
                        }
                        else
                        {
                            _barrier.OnCheck(actionStatePart.ActionStateMachine);
                            _barrier.SetInteractPoint();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public IInterruptCondition Clone()
        {
            CheckBarrierData _check = new CheckBarrierData();

            _check.CheckHeight_G = mCheckHeight_G;
            _check.CheckHeight_Gm = mCheckHeight_Gm;
            _check.CheckDis_L = mCheckDis_L;
            _check.CheckWeight_L = mCheckWeight_L;

            return _check;
            // throw new System.NotImplementedException();
        }
    }
}