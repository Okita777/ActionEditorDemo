using AsiActionEngine.RunTime;
using UnityEngine;
namespace AsiTimeLine.RunTime
{
    public class Ex_InputToTransDir : StaticActionLogics
    {
        private ActionStateMachine mStateMachine;

        public override void OnStart(ActionStateMachine _actionState)
        {
            mStateMachine = _actionState;
        }

        public override void OnUpdate(ActionStateMachine _stateMachine)
        {

        }

        public float GetInputToTransDir
        {
            get
            {
                //Vector3 _dir = Quaternion.Euler(0, mStateMachine.GetCamRot().eulerAngles.y, 0) * mStateMachine.PlayerInputMoveDir;
                Vector3 _dir = mStateMachine.PlayerInputMoveDir_Cam;
                float _angleOffset = Quaternion.LookRotation(_dir).eulerAngles.y - mStateMachine.CurUnit.transform.eulerAngles.y;
                if (_angleOffset > 180) _angleOffset -= 360;
                else if (_angleOffset < -180) _angleOffset += 360;
                return _angleOffset;
            }
        }

        public float GetOffsetAngle(int _offsetAngle = 0)
        {
            Vector3 _dir = Quaternion.Euler(0, mStateMachine.GetCamRot().eulerAngles.y - _offsetAngle, 0) *
                           mStateMachine.PlayerInputMoveDir;
            float _angleOffset = Quaternion.LookRotation(_dir).eulerAngles.y -
                                 mStateMachine.CurUnit.transform.eulerAngles.y;
            if (_angleOffset > 180) _angleOffset -= 360;
            else if (_angleOffset < -180) _angleOffset += 360;
            return _angleOffset;
        }
    }
}