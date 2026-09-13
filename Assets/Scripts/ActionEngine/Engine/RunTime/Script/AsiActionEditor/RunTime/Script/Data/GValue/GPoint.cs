using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GPoint : GValue
    {
        [SerializeField] public byte mSerValue;
        public GPoint(byte _value = 0)
        {
            mSerValue = _value;
            mType = true;
        }
        public GPoint(bool _type)
        {
            mType = _type;
        }

        public PointData GetValue(ActionStatePart part)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            return mStateMachine.GetPoint(mValueGroupIndex, mValueIndex, mSerValue);
        }

        public void SetValue(ActionStatePart part, PointData value)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            mStateMachine.SetPoint(mValueGroupIndex, mValueIndex, mSerValue, value);
        }

        public void Remove(ActionStatePart part)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            if (mStateMachine.PointDic.ContainsKey((mValueGroupIndex, mValueIndex, mSerValue)))
                mStateMachine.PointDic.Remove((mValueGroupIndex, mValueIndex, mSerValue));
        }

        public bool IsValid(ActionStatePart part)
        {
            ActionStateMachine mStateMachine = part.ActionStateMachine;
            return mStateMachine.PointDic.ContainsKey((mValueGroupIndex, mValueIndex, mSerValue));
        }

        public override GValue Clone()
        {
#if UNITY_EDITOR
            GPoint n = new GPoint(mSerValue);
            n.mType = mType;
            n.mValueIndex = mValueIndex;
            n.mValueGroupIndex = mValueGroupIndex;

            return n;
#endif
            return this;
        }
    }
}