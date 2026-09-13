using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GTransform : GValue
    {
        [SerializeField] public byte mSerValue;
        public GTransform(byte _value = 0)
        {
            mSerValue = _value;
            mType = true;
        }
        public GTransform(bool _Type)
        {
            mType = _Type;
        }

        public Transform GetValue(ActionStatePart part)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            return mStateMachine.GetTransform(mValueGroupIndex, mValueIndex, mSerValue);
        }

        public void SetValue(ActionStatePart part, Transform value)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            mStateMachine.SetTransform(mValueGroupIndex, mValueIndex, mSerValue, value);
        }

        public bool IsValid(ActionStatePart part)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            return mStateMachine.GetTransform(mValueGroupIndex, mValueIndex, mSerValue) is not null;
        }

        public override GValue Clone()
        {
#if UNITY_EDITOR
            GTransform n = new GTransform(mSerValue);
            n.mType = mType;
            n.mValueIndex = mValueIndex;
            n.mValueGroupIndex = mValueGroupIndex;

            return n;
#endif
            return this;
        }
    }
}