using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_SetGvalue_Transform : IActionEventData
    {
        [SerializeField] protected GValue_SetTransform m_GValueSetPoint = new GValue_SetTransform();

        #region MyRegion
        // [EditorProperty("是否获取本地玩家", EditorPropertyType.EEPT_Bool)]
        // public bool LocalPlayer
        // {
        //     get { return m_LocalPlayer; } 
        //     set { m_LocalPlayer = value; } 
        // }
        // [EditorProperty("是否获取本地玩家", EditorPropertyType.EEPT_Unit)]
        // public GUnit Unit
        // {
        //     get { return m_Unit; } 
        //     set { m_Unit = value; } 
        // }
        [EditorProperty("目标Transform", EditorPropertyType.EEPT_SetGTransform, Tooltip = "将玩家对象单位设置到此Transform")]
        public GValue_SetTransform GValueSetPoint
        {
            get { return m_GValueSetPoint; }
            set { m_GValueSetPoint = value; }
        }

        #endregion

        public int GetEvenType() => (int)EEvenType.EET_SetGvalue_Transform;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            if (m_GValueSetPoint.m_IsSet)
            {
                if (ActionEngineManager_Input.Instance.Player is not null)
                    m_GValueSetPoint.Set(_actionState, ActionEngineManager_Input.Instance.Player.transform);
            }
        }

        public IActionEventData Creact() => new Event_SetGvalue_Transform();

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SetGvalue_Transform _event = (Event_SetGvalue_Transform)_eventData;
            _event.GValueSetPoint = m_GValueSetPoint;
            return _event;
        }
    }
}