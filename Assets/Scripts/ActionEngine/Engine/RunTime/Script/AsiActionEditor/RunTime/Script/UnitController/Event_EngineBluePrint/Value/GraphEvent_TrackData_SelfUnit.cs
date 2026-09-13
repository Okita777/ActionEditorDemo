
namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_TrackData_SelfUnit : BluePrint_Unit
    {
        [System.NonSerialized] private TargetUnit m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            m_ReturnVal = part.ActionStateMachine.CurUnit;
        }

        public override TargetUnit value => m_ReturnVal;
        public override bool isValid(ActionStatePart part) => true;

        public override BluePrint_Value Clone()
        {
            return this;
        }
    }
}