using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_SceneInteractObject : IActionEventData
    {
        [SerializeField] protected byte mEventType = 0;

        #region Property

        [EditorProperty("执行类型", EditorPropertyType.EEPT_Enum, EnumNames = new[] { "缓存交互对象","交互成功时的回调"})]
        public byte EventType
        {
            get { return mEventType; }
            set { mEventType = value; }
        }

        #endregion

        public int GetEvenType() => (int)EEvenType.EET_SceneInteractObject;

        public IActionEventData Creact() => new Event_SceneInteractObject();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine machine = _actionState.ActionStateMachine;
            machine.TryGetStaticLogic(out Ex_InteractObjState _interactObj, nameof(Ex_InteractObjState));
            if(EventType == 0)
            {
                _interactObj.InitData();
            }
            else
            {
                _interactObj.InteractCallBack(machine);
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SceneInteractObject _event = _eventData as Event_SceneInteractObject;
            _event.EventType = EventType;
            return _event;
        }
    }
}
