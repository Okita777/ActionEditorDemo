using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public class GVector3 : GValue
    {
        public Vector3 mSerValue;

        public GVector3(Vector3 _value)
        {
            mSerValue = _value;
        }

        public Vector3 GetValue(ActionStatePart part)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;

            if (mType) return mStateMachine.GValuePool.GetVector3(mValueGroupIndex, mValueIndex);
            return mSerValue;
        }

        public void SetValue(ActionStatePart part, Vector3 value)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;

            if (mType) mStateMachine.GValuePool.SetVector3(mValueGroupIndex, mValueIndex, value);
            else mSerValue = value;
        }

        public override GValue Clone()
        {
            GVector3 n = new GVector3(mSerValue);
            n.mType = mType;
            n.mValueIndex = mValueIndex;
            return n;
        }
    }
}