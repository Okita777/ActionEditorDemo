using System.Collections.Generic;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GGroupUnit : GValue
    {
        //[UnityEngine.SerializeField] public float mSerValue;

        public GGroupUnit()
        {
            mType = false;
        }

        public List<ActionEngine_Unit> GetValue(ActionStatePart part)
        {
            //if (mType)
            {
                ActionStateMachine mStateMachine = part.ActionStateMachine;
                return mStateMachine.GValuePool.GetGroupUnit(mValueGroupIndex, mValueIndex);
            }
        }

        public void SetValue(ActionStatePart part, List<ActionEngine_Unit> value)
        {
            if (value is null)
            {
                Clear(part);
                return;
            }
            //if (mType)
            {
                ActionStateMachine mStateMachine = part.ActionStateMachine;
                mStateMachine.GValuePool.SetGroupUnit(mValueGroupIndex, mValueIndex, value);
            }
        }

        public void Clear(ActionStatePart part)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            mStateMachine.GValuePool.GetGroupUnit(mValueGroupIndex, mValueIndex).Clear();
        }

        public override GValue Clone()
        {
#if UNITY_EDITOR
            GGroupUnit n = new GGroupUnit();
            n.mType = mType;
            n.mValueIndex = mValueIndex;
            n.mValueGroupIndex = mValueGroupIndex;

            return n;
#endif
            return this;
        }
    }
}