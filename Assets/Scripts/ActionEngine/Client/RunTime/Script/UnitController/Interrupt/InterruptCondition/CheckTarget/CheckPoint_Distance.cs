using AsiActionEngine.RunTime;

namespace AsiTimeLine.RunTime
{
    public class CheckPoint_Distance : IInterruptCondition
    {
        public int InterruptType { get; }
        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            throw new System.NotImplementedException();
        }

        public IInterruptCondition Clone()
        {
            throw new System.NotImplementedException();
        }
    }
}