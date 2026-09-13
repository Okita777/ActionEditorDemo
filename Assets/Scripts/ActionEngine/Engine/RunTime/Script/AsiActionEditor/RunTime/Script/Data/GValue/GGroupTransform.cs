using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GGroupTransform : GValue
    {
        //[UnityEngine.SerializeField] public float mSerValue;

        public GGroupTransform()
        {
            mType = false;
        }

        public List<Transform> GetValue(ActionStatePart part)
        {
            //if (mType)
            {
                ActionStateMachine mStateMachine = part.ActionStateMachine;
                return mStateMachine.GValuePool.GetGroupTransform(mValueGroupIndex, mValueIndex);
            }
        }

        public void SetValue(ActionStatePart part, List<Transform> value)
        {
            if (value is null)
            {
                Clear(part);
                return;
            }
            //if (mType)
            {
                ActionStateMachine mStateMachine = part.ActionStateMachine;
                mStateMachine.GValuePool.SetGroupTransform(mValueGroupIndex, mValueIndex, value);
            }
        }
        public void Clear(ActionStatePart part)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            mStateMachine.GValuePool.GetGroupTransform(mValueGroupIndex, mValueIndex).Clear();
        }
        public override GValue Clone()
        {
#if UNITY_EDITOR
            GGroupTransform n = new GGroupTransform();
            n.mType = mType;
            n.mValueIndex = mValueIndex;
            n.mValueGroupIndex = mValueGroupIndex;

            return n;
#endif
            return this;
        }
    }
}