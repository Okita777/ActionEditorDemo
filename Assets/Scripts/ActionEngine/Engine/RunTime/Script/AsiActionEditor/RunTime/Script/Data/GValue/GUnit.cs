using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GUnit : GValue
    {
        [SerializeField] public byte mSerValue;
        // [SerializeField] public bool mIsLocalPlayer = false;
        public GUnit(byte _value = 0)
        {
            mSerValue = _value;
            mType = true;
        }
        public GUnit(bool _Type)
        {
            mType = _Type;
        }

        //public ActionEngine_Unit GetValue(ActionStatePart part)
        //{
        //    ActionStateMachine mStateMachine = part.ActionStateMachine;
        //    return mStateMachine.GetUnit(mValueGroupIndex, mValueIndex, mSerValue);
        //}
        public TargetUnit GetValue(ActionStatePart part)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            TargetUnit _return = mStateMachine.GetUnit(mValueGroupIndex, mValueIndex, mSerValue);
            return _return;
        }
        public void SetValue(ActionStatePart part, TargetUnit value)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            TargetUnit _old = GetValue(part);

            mStateMachine.SetUnit(mValueGroupIndex, mValueIndex, mSerValue, value);

            mStateMachine.SendChangeMessage_GUnit(this, _old, value);
        }

        public bool IsValid(ActionStatePart part)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            return mStateMachine.GetUnit(mValueGroupIndex, mValueIndex, mSerValue) is not null;
        }
        public (ushort, ushort, byte) GetKey()
        {
            return (mValueGroupIndex, mValueIndex, mSerValue);
        }
        public override GValue Clone()
        {
#if UNITY_EDITOR
            GUnit n = new GUnit(mSerValue);
            n.mType = mType;
            n.mValueIndex = mValueIndex;
            n.mValueGroupIndex = mValueGroupIndex;

            return n;
#endif
            return this;
        }
    }
}