using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public enum EAngleType
    {
        SelfForward,
        InputDir,
        AttackerForward,
        OnHitForward,
        CamForward,
        GTransForward,
        LookToGTrans,
        LookToAttacker,
        LookToOnHit,

    }

    public class Ex_GetAngleToType : StaticActionLogics
    {
        public Vector3 Get(ActionStateMachine _stateMachine, EAngleType _angleType, Transform _selectTransform)
        {
            if (_angleType == EAngleType.AttackerForward)
            {
                return (_stateMachine.AttackerUnit.transform.forward);
            }

            if (_angleType == EAngleType.OnHitForward)
            {
                return (_stateMachine.HitUnit.GetUnit().transform.forward);
            }

            if (_angleType == EAngleType.InputDir)
            {
                return Quaternion.Euler(0, _stateMachine.GetCamRot().eulerAngles.y, 0) * _stateMachine.PlayerInputMoveDir;
            }

            if (_angleType == EAngleType.SelfForward)
            {
                return (_stateMachine.CurUnit.transform.forward);
            }

            if (_angleType == EAngleType.CamForward)
            {
                return Quaternion.Euler(0, _stateMachine.GetCamRot().eulerAngles.y, 0) * Vector3.forward;
            }

            if (_angleType == EAngleType.GTransForward)
            {
                Transform _transform = _selectTransform;
                if (_transform is null)
                {
                    EngineDebug.LogWarning("Ex_GetAngleToType Warning ‘No transform found’ ");
                    return Vector3.forward;
                }
                return _transform.forward;
            }
            if (_angleType == EAngleType.LookToGTrans)
            {
                Transform _transform = _selectTransform;
                if (_transform is null)
                {
                    EngineDebug.LogWarning("Ex_GetAngleToType Warning ‘No transform found’ ");
                    return Vector3.forward;
                }

                return _transform.position - _stateMachine.CurUnit.transform.position;
            }
            if (_angleType == EAngleType.LookToAttacker)
            {
                return _stateMachine.AttackerUnit.transform.position - _stateMachine.CurUnit.transform.position;
            }

            if (_angleType == EAngleType.LookToOnHit)
            {
                return _stateMachine.HitUnit.GetUnit().transform.position - _stateMachine.CurUnit.transform.position;
            }

            return Vector3.forward;
        }

        public Vector3 Get(ActionStateMachine _stateMachine, EAngleType _angleType, SelectTransform _selectTransform)
        {
            return Get(_stateMachine, _angleType, _selectTransform.Get(_stateMachine));
        }

        public float GetOffsetAngle(ActionStateMachine _stateMachine, EAngleType _forAngleType, EAngleType _toAngleType,
            float _offsetAngle, SelectTransform _refer, SelectTransform _target)
        {
            Vector3 _forAngle = Quaternion.Euler(0, _offsetAngle, 0) * Get(_stateMachine, _forAngleType, _refer);
            Vector3 _toAngle = Get(_stateMachine, _toAngleType, _target);
            _forAngle.y = 0;
            _toAngle.y = 0;
            float _angleOffset = Quaternion.FromToRotation(_forAngle, _toAngle).eulerAngles.y;
            if (_angleOffset > 180) _angleOffset -= 360;

            // Vector3 _forAngleL = Quaternion.Euler(0, _offsetAngle - 22.5f, 0) * Get(_stateMachine, _forAngleType);
            // Vector3 _forAngleR = Quaternion.Euler(0, _offsetAngle + 22.5f, 0) * Get(_stateMachine, _forAngleType);
            // Vector3 _pos = _stateMachine.CurUnit.transform.TransformPoint(0, 1, 0);
            // Debug.DrawRay(_pos, _forAngleL, Color.red);
            // Debug.DrawRay(_pos, _forAngleR, Color.red);
            // Debug.DrawRay(_pos, _toAngle, Color.green);
            // Debug.Log("差值: " + _angleOffset);

            return _angleOffset;
        }

        public override void OnUpdate(ActionStateMachine _actionState)
        {
        }
    }
}