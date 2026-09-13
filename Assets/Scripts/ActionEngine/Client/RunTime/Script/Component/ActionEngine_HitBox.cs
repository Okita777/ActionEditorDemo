using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class ActionEngine_HitBox : TargetUnit
    {
        public GameObject target;
        public ActionEngine_Unit MainUnit
        {
            get => mMainUnit;
            set
            {
                target = value.gameObject;
                mMainUnit = value;
            }
        }
        private ActionEngine_Unit mMainUnit;
        public override ActionEngine_Unit GetUnit() => mMainUnit;
    }
}