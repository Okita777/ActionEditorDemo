
namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GGroupString : GValue
    {
        //[UnityEngine.SerializeField] public float mSerValue;

        public GGroupString()
        {
            mType = false;
        }

        public string[] GetValue(ActionStatePart part)
        {
            //if (mType)
            {
                ActionStateMachine mStateMachine = part.ActionStateMachine;
                return mStateMachine.GValuePool.GetGroupString(mValueGroupIndex, mValueIndex);
            }
        }

        public void SetValue(ActionStatePart part, string[] value)
        {
            //if (mType)
            {
                ActionStateMachine mStateMachine = part.ActionStateMachine;
                mStateMachine.GValuePool.SetGroupString(mValueGroupIndex, mValueIndex, value);
            }
        }

        public override GValue Clone()
        {
#if UNITY_EDITOR
            GGroupString n = new GGroupString();
            n.mType = mType;
            n.mValueIndex = mValueIndex;
            n.mValueGroupIndex = mValueGroupIndex;
            return n;
#endif
            return this;
        }
    }
}