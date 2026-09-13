
namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Value_GetGroupValue : BluePrint_Value_List
    {
        public override BluePrint_Value Clone()
        {
            return this;
        }

        public override int GetLength() => 0;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
        }
    }
}