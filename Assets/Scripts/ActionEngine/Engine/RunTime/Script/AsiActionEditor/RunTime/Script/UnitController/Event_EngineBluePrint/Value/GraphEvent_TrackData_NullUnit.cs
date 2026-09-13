
namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_TrackData_NullUnit : BluePrint_Unit
    {

        [System.NonSerialized] private TargetUnit m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            m_ReturnVal = null;
        }

        public override TargetUnit value => m_ReturnVal;
        public override bool isValid(ActionStatePart part) => false;

        public override BluePrint_Value Clone()
        {
            return this;
        }
    }
}