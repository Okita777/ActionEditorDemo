using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{

    [System.Serializable]
    public class Event_Lable : IActionEventData
    {
        [SerializeField] private int mStateLable;
        [SerializeField] private bool mIsRemove;
        [SerializeField] private bool mIsRemoveAll;

        #region property
        [EditorProperty("状态标签", EditorPropertyType.EEPT_ActionLable)]
        public int StateLable
        {
            get { return mStateLable; }
            set { mStateLable = value; }
        }

        [EditorProperty("删除该标签", EditorPropertyType.EEPT_Bool)]
        public bool IsRemove
        {
            get { return mIsRemove; }
            set { mIsRemove = value; }
        }

        [EditorProperty("删除所有标签", EditorPropertyType.EEPT_Bool)]
        public bool IsRemoveAll
        {
            get { return mIsRemoveAll; }
            set { mIsRemoveAll = value; }
        }
        #endregion
        //public int GetEvenType() => (int)EEvenType.EET_Lable;
        public int GetEvenType() => 0;

        public IActionEventData Creact() => new Event_Lable();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            if (mIsRemoveAll)
            {
                _actionState.ActionStateMachine.RemoveAllActionLable();
            }
            else
            {
                if (mIsRemove)
                {
                    _actionState.ActionStateMachine.RemoveActionLable(mStateLable);
                }
                else
                {
                    _actionState.ActionStateMachine.AddActionLable(mStateLable);
                }
            }
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            if (mIsRemove)
            {
                _actionState.ActionStateMachine.AddActionLable(mStateLable);
            }
            else
            {
                _actionState.ActionStateMachine.RemoveActionLable(mStateLable);
            }
        }


        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_Lable _event = _eventData as Event_Lable;

            _event.StateLable = mStateLable;
            _event.IsRemove = mIsRemove;
            _event.IsRemoveAll = mIsRemoveAll;

            return _event;
        }
    }
}