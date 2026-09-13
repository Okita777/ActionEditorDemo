
namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_TrackData_ActionStatePartHitP : BluePrint_PointData
    {
        private PointData _return;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            _return = part.HitPoint;
        }
        public override PointData value => _return;

        public override BluePrint_Value Clone()
        {
            return this;
        }
    }
}