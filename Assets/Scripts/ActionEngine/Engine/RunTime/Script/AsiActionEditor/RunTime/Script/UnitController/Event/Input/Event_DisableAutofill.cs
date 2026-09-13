using System;
using AsiActionEngine.RunTime;
using UnityEngine;


namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_DisableAutofill : IActionEventData
    {
        [SerializeField] protected byte m_DisableType = 0;
        #region Property
        [EditorProperty("屏蔽输入类型", EditorPropertyType.EEPT_Enum, EnumNames = new[] { "预输入", "所有输入" })]
        public byte DisableType
        {
            get { return m_DisableType; }
            set { m_DisableType = value; }
        }
        #endregion
        public int GetEvenType() => -(int)EEvenTypeInternal.EET_DisableAutofill;

        [NonSerialized] private bool m_Init;
        public IActionEventData Creact() => new Event_DisableAutofill();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            //m_Init = false;
            _actionState.ReSetKeys(false);
            if (_isSingle)
            {
                //#if UNITY_EDITOR
                //                EngineDebug.LogError($"屏蔽预先输入轨无法单帧工作: [{_actionState.CurrentActionState.Name}]");
                //#endif
                return;
            }

            _actionState.DisableAutofill = true;
            if (m_DisableType == 1)
            {
                _actionState.DisableInput = true;
            }
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            //if (!m_Init)
            {
                //if(_actionTime.TriggerTime == 0)
                {
                    //_actionState.ActionStateMachine.ReSetAllKey(false);
                    _actionState.ReSetKeys(false);
                }
                //m_Init = true;
            }
        }


        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            _actionState.DisableAutofill = false;
            if (m_DisableType == 1)
            {
                _actionState.DisableInput = false;
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_DisableAutofill _event = _eventData as Event_DisableAutofill;
            _event.DisableType = m_DisableType;
            return _event;
        }
    }
}