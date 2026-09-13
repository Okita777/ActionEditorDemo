using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GBool : GValue
    {
        [SerializeField] public bool mSerValue;
        public GBool(bool _value = false, bool _mType = false)
        {
            mSerValue = _value;
            mType = _mType;
        }
        public GBool(bool _mType)
        {
            mType = _mType;
        }

        public bool GetValue(ActionStatePart part)
        {
            if (mType) return part.ActionStateMachine.GValuePool.GetBool(mValueGroupIndex, mValueIndex);
            return mSerValue;
        }

        public void SetValue(ActionStatePart part, bool value)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            bool _old = GetValue(part);

            if (mType)
                mStateMachine.GValuePool.SetBool(mValueGroupIndex, mValueIndex, value);
            else
                mSerValue = value;

            mStateMachine.SendChangeMessage_GBool(this, _old, value);
        }

        public (ushort, ushort) GetKey()
        {
            return (mValueGroupIndex, mValueIndex);
        }

        public override GValue Clone()
        {
#if UNITY_EDITOR
            GBool n = new GBool(mSerValue, mType);
            // n.mType = mType;
            n.mValueIndex = mValueIndex;
            n.mValueGroupIndex = mValueGroupIndex;

            return n;
#endif
            return this;

        }
    }
}