namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GString : GValue
    {
        [UnityEngine.SerializeField] public string mSerValue;

        public GString(string _value = "", bool _mType = false)
        {
            mSerValue = _value;
            mType = _mType;
        }
        public GString(bool _mType)
        {
            mType = _mType;
        }

        public string GetValue(ActionStatePart part)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;

            if (mType) return mStateMachine.GValuePool.GetString(mValueGroupIndex, mValueIndex);
            return mSerValue;
        }

        public void SetValue(ActionStatePart part, string value)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;

            if (mType) mStateMachine.GValuePool.SetString(mValueGroupIndex, mValueIndex, value);
            else mSerValue = value;
        }

        public override GValue Clone()
        {
#if UNITY_EDITOR
            GString n = new GString(mSerValue);
            n.mType = mType;
            n.mValueIndex = mValueIndex;
            n.mValueGroupIndex = mValueGroupIndex;

            return n;
#endif
            return this;
        }
    }
}