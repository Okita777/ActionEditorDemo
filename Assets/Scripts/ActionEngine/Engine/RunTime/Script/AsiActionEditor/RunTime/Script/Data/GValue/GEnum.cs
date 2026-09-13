using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GEnum : GValue
    {
        [SerializeField] public byte mSerValue;
        public GEnum(byte _value = 0, bool _type = false)
        {
            mSerValue = _value;
            mType = _type;
        }

        public byte GetValue(ActionStatePart part)
        {
            if (mType) return part.ActionStateMachine.GValuePool.GetEnum(mValueGroupIndex, mValueIndex);
            return mSerValue;
        }

        public void SetValue(ActionStatePart part, byte value)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            byte _old = GetValue(part);

            if (mType)
                mStateMachine.GValuePool.SetEnum(mValueGroupIndex, mValueIndex, value);
            else
                mSerValue = value;

            mStateMachine.SendChangeMessage_GEnum(this, _old, value);
        }

        public (ushort, ushort) GetKey()
        {
            return (mValueGroupIndex, mValueIndex);
        }

        public (ushort, ushort, byte) GetEnumKey
        {
            get { return (mValueGroupIndex, mValueIndex, mSerValue); }
        }
        public override GValue Clone()
        {

#if UNITY_EDITOR
            GEnum n = new GEnum(mSerValue);
            n.mType = mType;
            n.mValueIndex = mValueIndex;
            n.mValueGroupIndex = mValueGroupIndex;

            return n;
#endif
            return this;
        }
    }
}