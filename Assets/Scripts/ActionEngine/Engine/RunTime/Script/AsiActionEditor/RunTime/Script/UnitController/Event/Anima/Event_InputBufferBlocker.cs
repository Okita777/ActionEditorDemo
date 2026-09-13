
namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_InputBufferBlocker : IActionEventData
    {
        //[SerializeField] private string mAnimName = string.Empty;

        #region property
        //public string AnimName
        //{
        //    get { return mAnimName; }
        //    set { mAnimName = value; }
        //}
        #endregion


        public int GetEvenType() => -(int)EEvenTypeInternal.EET_DTD_PlayAnim;
        public IActionEventData Creact() => new Event_PlayAnim();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            if (!_isSingle)
            {

            }
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {

        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_InputBufferBlocker _event = _eventData as Event_InputBufferBlocker;

            //_event.mAnimName = mAnimName;

            return _event;
        }
    }
}