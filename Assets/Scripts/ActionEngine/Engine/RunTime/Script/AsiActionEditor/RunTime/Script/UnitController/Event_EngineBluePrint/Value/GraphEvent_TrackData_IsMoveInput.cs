
namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_TrackData_IsMoveInput : BluePrint_Bool
    {
        [System.NonSerialized] private bool m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            //m_ReturnVal = _time.CurrentTime * 0.0001f;
            m_ReturnVal = part.ActionStateMachine.IsMoveInput;
            // base.Init(part, _time);
        }
        public override bool value => m_ReturnVal;


        public override BluePrint_Value Clone()
        {
            return this;
        }
    }
}