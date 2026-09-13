using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_RootWeight : IActionEventData
    {
        [SerializeField] protected GraphEvent_NoValue_Vector3 mRootWeightVal = new GraphEvent_NoValue_Vector3();

        #region Property

        [EditorProperty("Root权重", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Vector3 RootWeightVal
        {
            get
            {
                if (mRootWeightVal is null) mRootWeightVal = new GraphEvent_NoValue_Vector3();
                return mRootWeightVal;
            }
            set { mRootWeightVal = value; }
        }
        #endregion
        public int GetEvenType() => (int)EEvenType.EET_RootWeight;
        public IActionEventData Creact() => new Event_RootWeight();
        [System.NonSerialized] private Vector3 initRootWeight;
        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            initRootWeight = _actionState.ActionStateMachine.RootWeight;
        }
        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            ActionStateMachine _machine = _actionState.ActionStateMachine;
            _machine.RootWeight = mRootWeightVal.value(_actionState, _actionTime);
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            _actionState.ActionStateMachine.RootWeight = initRootWeight;
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_RootWeight newEventData = _eventData as Event_RootWeight;
            newEventData.RootWeightVal = RootWeightVal.Clone();
            return newEventData;
        }
    }
}