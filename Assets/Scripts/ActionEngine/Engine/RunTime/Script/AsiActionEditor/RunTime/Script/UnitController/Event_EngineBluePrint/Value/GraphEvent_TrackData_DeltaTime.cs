
namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_TrackData_DeltaTime : BluePrint_Float
    {
        [System.NonSerialized] private float m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            m_ReturnVal = _time.Deltatime;
        }
        public override float value => m_ReturnVal;


        public override BluePrint_Value Clone()
        {
            return this;
        }
    }
}