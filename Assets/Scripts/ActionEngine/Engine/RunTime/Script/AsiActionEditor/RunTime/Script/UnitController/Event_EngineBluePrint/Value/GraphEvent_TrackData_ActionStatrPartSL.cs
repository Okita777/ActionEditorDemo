
namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_TrackData_ActionStatrPartSL : BluePrint_Int
    {
        [System.NonSerialized] private int m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            m_ReturnVal = part.LoopMax;
        }
        public override int value => m_ReturnVal;

        public override BluePrint_Value Clone()
        {
            return this;
        }
    }
}