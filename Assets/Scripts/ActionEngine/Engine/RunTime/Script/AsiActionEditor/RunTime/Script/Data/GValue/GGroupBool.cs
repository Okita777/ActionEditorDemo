
namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GGroupBool : GValue
    {
        //[UnityEngine.SerializeField] public float mSerValue;

        public GGroupBool()
        {
            mType = false;
        }

        public bool[] GetValue(ActionStatePart part)
        {
            //if (mType)
            {
                ActionStateMachine mStateMachine = part.ActionStateMachine;
                return mStateMachine.GValuePool.GetGroupBool(mValueGroupIndex, mValueIndex);
            }
        }

        public void SetValue(ActionStatePart part, bool[] value)
        {
            //if (mType)
            {
                ActionStateMachine mStateMachine = part.ActionStateMachine;
                mStateMachine.GValuePool.SetGroupBool(mValueGroupIndex, mValueIndex, value);
            }
        }

        public override GValue Clone()
        {
#if UNITY_EDITOR
            GGroupBool n = new GGroupBool();
            n.mType = mType;
            n.mValueIndex = mValueIndex;
            n.mValueGroupIndex = mValueGroupIndex;

            return n;
#endif
            return this;
        }
    }
}