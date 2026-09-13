
namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_LoopIndex : BluePrint_Int
    {
        [System.NonSerialized] private int _loopIndex;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            _loopIndex = part.ActionStateMachine.BluePrint_LoopIndex;
        }

        public override int value => _loopIndex;

        public override BluePrint_Value Clone()
        {
            return this;
        }
    }
}