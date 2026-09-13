using System.Collections.Generic;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GGroupPoint : GValue
    {
        //[UnityEngine.SerializeField] public float mSerValue;

        public GGroupPoint()
        {
            mType = false;
        }

        public List<PointData> GetValue(ActionStatePart part)
        {
            //if (mType)
            {
                ActionStateMachine mStateMachine = part.ActionStateMachine;
                return mStateMachine.GValuePool.GetGroupPointData(mValueGroupIndex, mValueIndex);
            }
        }

        public void SetValue(ActionStatePart part, List<PointData> value)
        {
            if (value is null)
            {
                Clear(part);
                return;
            }
            //if (mType)
            {
                ActionStateMachine mStateMachine = part.ActionStateMachine;
                mStateMachine.GValuePool.SetGroupPointData(mValueGroupIndex, mValueIndex, value);
            }
        }
        public void Clear(ActionStatePart part)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            mStateMachine.GValuePool.GetGroupPointData(mValueGroupIndex, mValueIndex).Clear();
        }
        public override GValue Clone()
        {
#if UNITY_EDITOR
            GGroupPoint n = new GGroupPoint();
            n.mType = mType;
            n.mValueIndex = mValueIndex;
            n.mValueGroupIndex = mValueGroupIndex;

            return n;
#endif
            return this;
        }
    }
}