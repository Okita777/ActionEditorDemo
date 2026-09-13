using AsiActionEngine.RunTime;
using UnityEngine;


namespace AsiTimeLine.RunTime
{
    public class ActionEngine_UnitWarp : TargetUnit
    {
        [Header("单位组件")] public ActionEngine_Unit mTargetUnit = null;

        public override ActionEngine_Unit GetUnit()
        {
            if (mTargetUnit is not null) mTargetUnit.AgentUnitRoot = gameObject;
            return mTargetUnit;
        }
        //public CharacterController GetController() => mTargetController;
    }
}