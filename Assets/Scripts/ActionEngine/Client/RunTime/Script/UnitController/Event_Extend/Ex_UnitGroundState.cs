// using System.Numerics;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class Ex_UnitGroundState : StaticActionLogics
    {
        public bool IsGround = false;
        public override void OnUpdate(ActionStateMachine _actionState)
        {
            Vector3 _startPos = _actionState.CurUnit.transform.TransformPoint(0, 0.1f, 0);
            IsGround = Physics.Raycast(_startPos, Vector3.down, 0.3f);
        }
    }
}