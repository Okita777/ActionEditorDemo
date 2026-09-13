
namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GGroupInt : GValue
    {
        //[UnityEngine.SerializeField] public float mSerValue;

        public GGroupInt()
        {
            mType = false;
        }

        public int[] GetValue(ActionStatePart part)
        {
            //if (mType)
            {
                ActionStateMachine mStateMachine = part.ActionStateMachine;
                return mStateMachine.GValuePool.GetGroupInt(mValueGroupIndex, mValueIndex);
            }
        }

        public void SetValue(ActionStatePart part, int[] value)
        {
            //if (mType)
            {
                ActionStateMachine mStateMachine = part.ActionStateMachine;
                mStateMachine.GValuePool.SetGroupInt(mValueGroupIndex, mValueIndex, value);
            }
        }

        public override GValue Clone()
        {
#if UNITY_EDITOR
            GGroupInt n = new GGroupInt();
            n.mType = mType;
            n.mValueIndex = mValueIndex;
            n.mValueGroupIndex = mValueGroupIndex;

            return n;
#endif
            return this;
        }
    }
}