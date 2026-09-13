using AsiActionEngine.RunTime;
using UnityEngine;

#if FMOD
using FMODUnity;
#endif

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_SetActionTotal : IActionEventData
    {
        [SerializeField] public GInt m_TotalTime = new GInt(1000);

        [EditorProperty("修改Action总时长(ms)", EditorPropertyType.EEPT_GInt)]
        public GInt TotalTime
        {
            get { return m_TotalTime; }
            set { m_TotalTime = value; }
        }
        public int GetEvenType() => (int)EEvenType.EET_SetActionTotal;

        public IActionEventData Creact() => new Event_SetActionTotal();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            _actionState.CurrentActionState.TotalTime = TotalTime.GetValue(_actionState);
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SetActionTotal _event = _eventData as Event_SetActionTotal;
            _event.TotalTime = (GInt)TotalTime.Clone();
            return _event;
        }
    }
}