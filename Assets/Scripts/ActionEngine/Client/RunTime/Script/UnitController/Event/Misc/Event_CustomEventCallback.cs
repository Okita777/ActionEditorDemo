using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    /// <summary>
    /// 自定义事件回调轨道占位：序列化字段与 Enter/Update/Exit 等业务逻辑由项目自行补充。
    /// </summary>
    [System.Serializable]
    public class Event_CustomEventCallback : IActionEventData
    {
        [SerializeField] public byte m_EventType = 0;
        [EditorProperty("回调类型", EditorPropertyType.EEPT_Enum, EnumNames = new[] { "技能执行成功的回调"})]
        public byte EventType
        {
            get { return m_EventType; }
            set { m_EventType = value; }
        }
        public int GetEvenType() => (int)EEvenType.EET_CustomEventCallback;

        public IActionEventData Creact() => new Event_CustomEventCallback();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            Event(_actionState, _isSingle);
        }
        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            Event(_actionState, false);
        }

        private void Event(ActionStatePart _actionState, bool _isSingle)
        {
            if (_actionState.ActionStateMachine.TryGetStaticLogic(out Ex_Custom_Callback _gv, nameof(Ex_Custom_Callback)))
            {
                if (EventType == 0)
                {
                    _gv.RunEven_SkillCallBack(_actionState, _isSingle ? Ex_Custom_Callback.ECallBackType.Single : Ex_Custom_Callback.ECallBackType.Multiple);
                }
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_CustomEventCallback _event = _eventData as Event_CustomEventCallback;
            _event.m_EventType = m_EventType;
            return _event;
        }
    }
}
