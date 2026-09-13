
namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GFloat : GValue
    {
        [UnityEngine.SerializeField] public float mSerValue;

        public GFloat(float _value = 0.0f, bool _mType = false)
        {
            mSerValue = _value;
            mType = _mType;
        }
        public GFloat(bool _mType)
        {
            mType = _mType;
        }

        public float GetValue(ActionStatePart part)
        {
            if (mType)
            {
                ActionStateMachine mStateMachine = part.ActionStateMachine;
                float _return = mStateMachine.GValuePool.GetFloat(mValueGroupIndex, mValueIndex);
                //if (mValueGroupIndex == 50 && mValueIndex == 22)
                //{
                //    EngineDebug.Log($"获取CD值({mStateMachine.GValuePool.GetHashCode()})(part:{part.GetHashCode()})[{_return}]");
                //}
                return _return;
            }
            return mSerValue;
        }

        public void SetValue(ActionStatePart part, float value)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            float _old = GetValue(part);

            if (mType)
                mStateMachine.GValuePool.SetFloat(mValueGroupIndex, mValueIndex, value);
            else
                mSerValue = value;

            mStateMachine.SendChangeMessage_GFloat(this, _old, value);
        }

        public (ushort, ushort) GetKey()
        {
            return (mValueGroupIndex, mValueIndex);
        }

        public override GValue Clone()
        {
#if UNITY_EDITOR
            GFloat n = new GFloat(mSerValue);
            n.mType = mType;
            n.mValueIndex = mValueIndex;
            n.mValueGroupIndex = mValueGroupIndex;

            return n;
#endif
            return this;
        }
    }
}