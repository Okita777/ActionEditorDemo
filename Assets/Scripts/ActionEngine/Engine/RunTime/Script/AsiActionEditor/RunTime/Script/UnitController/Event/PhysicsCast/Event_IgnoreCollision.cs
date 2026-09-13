using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_IgnoreCollision : IActionEventData
    {
        // [SerializeField] private byte m_AnimationID = 2;
        // [SerializeField] private int m_AnimationName = null;
        [SerializeField] private GraphEvent_NoValue_Unit m_Unit_1 = new GraphEvent_NoValue_Unit();
        [SerializeField] private GraphEvent_NoValue_Unit m_Unit_2 = new GraphEvent_NoValue_Unit();

        #region property

        [EditorProperty("目标对象1", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Unit Unit_1
        {
            get { return m_Unit_1; }
            set { m_Unit_1 = value; }
        }
        [EditorProperty("目标对象2", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Unit Unit_2
        {
            get { return m_Unit_2; }
            set { m_Unit_2 = value; }
        }
        #endregion

        public int GetEvenType() => -(int)EEvenTypeInternal.EET_IgnoreCollision;
        public IActionEventData Creact() => new Event_IgnoreCollision();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            // ActionStateMachine stateMachine = _actionState.ActionStateMachine;
            //Physics.IgnoreLayerCollision(m_MixTime, m_OffsetTime, true);
            ActionMachineTime _time = EngineResourcesManager.Instance.MachineTime;
            TargetUnit _unit_01 = Unit_1.value(_actionState, _time);
            TargetUnit _unit_02 = Unit_2.value(_actionState, _time);
            if (_unit_01 is not null && _unit_02 is not null)
            {
                Transform obj_1 = _unit_01.transform;
                Transform obj_2 = _unit_02.transform;
                if (_unit_01.GetUnit().ActionStateMachine.TryGetComponent(out Collider _value1, nameof(Collider), obj_1))
                {
                    if (_unit_02.GetUnit().ActionStateMachine.TryGetComponent(out Collider _value2, nameof(Collider), obj_2))
                    {
                        Physics.IgnoreCollision(_value1, _value2, true);
                    }
                }
            }
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            ActionMachineTime _time = EngineResourcesManager.Instance.MachineTime;
            TargetUnit _unit_01 = Unit_1.value(_actionState, _time);
            TargetUnit _unit_02 = Unit_2.value(_actionState, _time);
            if (_unit_01 is not null && _unit_02 is not null)
            {
                Transform obj_1 = _unit_01.transform;
                Transform obj_2 = _unit_02.transform;
                if (_unit_01.GetUnit().ActionStateMachine.TryGetComponent(out Collider _value1, nameof(Collider), obj_1))
                {
                    if (_unit_02.GetUnit().ActionStateMachine.TryGetComponent(out Collider _value2, nameof(Collider), obj_2))
                    {
                        Physics.IgnoreCollision(_value1, _value2, false);
                    }
                }
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_IgnoreCollision _event = _eventData as Event_IgnoreCollision;

            _event.Unit_1 = m_Unit_1.Clone();
            _event.Unit_2 = m_Unit_2.Clone();

            return _event;
        }
    }
}