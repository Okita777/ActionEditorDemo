
namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GInt : GValue
    {
        [UnityEngine.SerializeField] public int mSerValue;

        public GInt(int _value = 0, bool _mType = false)
        {
            mSerValue = _value;
            mType = _mType;
        }
        public GInt(bool _mType)
        {
            mType = _mType;
        }

        public int GetValue(ActionStatePart part)
        {
            if (mType)
            {
                ActionStateMachine mStateMachine = part.ActionStateMachine;
                return mStateMachine.GValuePool.GetInt(mValueGroupIndex, mValueIndex);
            }
            return mSerValue;
        }

        public void SetValue(ActionStatePart part, int value)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            int _old = GetValue(part);

            if (mType)
                mStateMachine.GValuePool.SetInt(mValueGroupIndex, mValueIndex, value);
            else
                mSerValue = value;

            mStateMachine.SendChangeMessage_GInt(this, _old, value);
        }
        public (ushort, ushort) GetKey()
        {
            return (mValueGroupIndex, mValueIndex);
        }

        public override GValue Clone()
        {
#if UNITY_EDITOR
            GInt n = new GInt(mSerValue);
            n.mType = mType;
            n.mValueIndex = mValueIndex;
            n.mValueGroupIndex = mValueGroupIndex;
            return n;
#endif
            return this;
        }
    }
}