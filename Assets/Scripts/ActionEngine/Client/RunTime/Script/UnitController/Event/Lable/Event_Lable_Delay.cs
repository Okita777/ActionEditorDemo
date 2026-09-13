using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_Lable_Delay : IActionEventData
    {
        [SerializeField] protected float mDelayTime = 1;
        [SerializeField] protected int mLableName = 0;
        [SerializeField] protected bool mDonece = true;

        #region Property
        [EditorProperty("延时(s)", EditorPropertyType.EEPT_Float)]
        public float DelayTime
        {
            get { return mDelayTime; }
            set { mDelayTime = value; }
        }
        [EditorProperty("标签名", EditorPropertyType.EEPT_ActionLable)]
        public int LableName
        {
            get { return mLableName; }
            set { mLableName = value; }
        }
        [EditorProperty("结束时删除标签", EditorPropertyType.EEPT_Bool)]
        public bool Donece
        {
            get { return mDonece; }
            set { mDonece = value; }
        }
        #endregion

        private float mTiming = 0;

        //public int GetEvenType() => (int)EEvenType.EET_Lable_Delay;
        public int GetEvenType() => 0;

        public IActionEventData Creact() => new Event_Lable_Delay();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            mTiming = 0.0f;
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;

            if (mTiming < mDelayTime)
            {
                mTiming += _actionTime.Deltatime;
                if (mTiming >= mDelayTime)
                {
                    _stateMachine.AddActionLable(mLableName);

                }
            }
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            if (mDonece)
            {
                _stateMachine.RemoveActionLable(mLableName);
            }
        }


        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_Lable_Delay _event = _eventData as Event_Lable_Delay;

            _event.DelayTime = mDelayTime;
            _event.LableName = mLableName;
            _event.Donece = mDonece;

            return _event;
        }
    }
}