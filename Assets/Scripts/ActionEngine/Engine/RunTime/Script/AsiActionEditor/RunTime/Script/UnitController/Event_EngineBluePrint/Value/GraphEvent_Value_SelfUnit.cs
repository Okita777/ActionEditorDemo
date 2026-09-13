
namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Value_SelfUnit : BluePrint_Unit
    {
        public override bool isValid(ActionStatePart part) => true;

        [System.NonSerialized] private TargetUnit m_IntVal;
        public override TargetUnit value => m_IntVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            m_IntVal = part.ActionStateMachine.CurUnit;
            // base.Init(part, _time);
        }

        public override BluePrint_Value Clone()
        {

            return this;
        }


    }
}