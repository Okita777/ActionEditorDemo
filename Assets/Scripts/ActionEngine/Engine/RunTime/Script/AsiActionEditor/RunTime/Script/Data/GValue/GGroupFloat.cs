
namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GGroupFloat : GValue
    {
        //[UnityEngine.SerializeField] public float mSerValue;

        public GGroupFloat()
        {
            mType = false;
        }

        public float[] GetValue(ActionStatePart part)
        {
            //if (mType)
            {
                ActionStateMachine mStateMachine = part.ActionStateMachine;
                return mStateMachine.GValuePool.GetGroupFloat(mValueGroupIndex, mValueIndex);
            }
        }

        public void SetValue(ActionStatePart part, float[] value)
        {
            //if (mType)
            {
                ActionStateMachine mStateMachine = part.ActionStateMachine;
                mStateMachine.GValuePool.SetGroupFloat(mValueGroupIndex, mValueIndex, value);
            }
        }

        public override GValue Clone()
        {
#if UNITY_EDITOR
            GGroupFloat n = new GGroupFloat();
            n.mType = mType;
            n.mValueIndex = mValueIndex;
            n.mValueGroupIndex = mValueGroupIndex;

            return n;
#endif
            return this;
        }
    }
}