using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class EX_Update_UnitRot : ActionLogics
    {
        private float lifeTime = 0;
        private ERotType mRotType;
        private int mRotLerp = 12;
        private int mOffsetRotY = 0;
        private int mRotPriority = 0;//越大越优先

        private Quaternion mTargetRot;
        private Transform mTransform => mStateMachine.CurUnit.RootTarget;
        private ActionStateMachine mStateMachine;

        public void SetRot(ERotType mRotType, int mRotLerp, int mOffsetRotY, int mRotPriority, float mTotalTime)
        {
            // if (lifeTime >= 0)
            {
                if (mRotPriority < this.mRotPriority)
                {
                    return;
                }
            }

            lifeTime = mTotalTime;
            this.mRotType = mRotType;
            this.mRotLerp = mRotLerp;
            this.mOffsetRotY = mOffsetRotY;
            this.mRotPriority = mRotPriority;

            mTargetRot = GetTargetRot(mStateMachine, mTransform, mRotType) * Quaternion.Euler(0, mOffsetRotY, 0);
        }
        public void SetRot(Quaternion _mTargetRot, int _mRotLerp, int _mRotPriority, float _mTotalTime)
        {
            // if (lifeTime >= 0)
            {
                if (_mRotPriority < this.mRotPriority)
                {
                    return;
                }
            }
            //Debug.LogWarning($"设置角度: [{_mTargetRot.eulerAngles.y}] ");
            lifeTime = _mTotalTime;
            this.mRotLerp = _mRotLerp;
            this.mRotPriority = _mRotPriority;

            mTargetRot = _mTargetRot;
        }
        public void ResetLifeTime()
        {
            lifeTime = -1f;
        }

        public Quaternion GetTargetRot(ActionStateMachine _stateMachine, Transform _rootTrans, ERotType _RotType)
        {
            if (_RotType == ERotType.Camera)
            {
                return Quaternion.Euler(0, _stateMachine.GetCharacterFor.eulerAngles.y, 0);
            }
            else if (_RotType == ERotType.MoveDir)
            {
                Quaternion _moveDir = Quaternion.LookRotation(_stateMachine.PlayerInputMoveDir, Vector3.up);
                _moveDir *= _stateMachine.GetCamRot();
                return Quaternion.Euler(0, _moveDir.eulerAngles.y, 0);
            }
            else if (_RotType == ERotType.LockToTargetDir)
            {
                // if (_stateMachine.IsLock)
                // {
                //     if (_stateMachine.LockTransform is null)
                //     {
                //         _stateMachine.IsLock = false;
                //     }
                //     else
                //     {
                //         Vector3 _lockDir = _stateMachine.LockTransform.position - _rootTrans.position;
                //         _lockDir.y = 0;
                //         return Quaternion.LookRotation(_lockDir);
                //     }
                // }
            }
            else if (_RotType == ERotType.LookToAttacker)
            {
                Vector3 _lookDir = _stateMachine.AttackerUnit.transform.position -
                                   _rootTrans.position;
                _lookDir.y = 0;
                return Quaternion.LookRotation(_lookDir);
            }
            else if (_RotType == ERotType.LookToPlayer)
            {
                Vector3 _lookDir = ActionEngineManager_Input.Instance.Player.transform.position -
                                   _rootTrans.position;
                _lookDir.y = 0;
                return Quaternion.LookRotation(_lookDir);
            }

            return Quaternion.identity;
        }

        public override void Start(ActionStateMachine _actionState)
        {
            mStateMachine = _actionState;
        }

        public override void Update(ActionStateMachine _actionState, float _deltaTime)
        {
            //if (_actionState.FirstStatePart.IsTem)
            //    return;
            if (lifeTime > 0)
            {
                lifeTime -= _deltaTime;
                OnSetRot(_deltaTime);
            }
        }

        public void OnSetRot(float _deltaTime)
        {
            if (mStateMachine.TryGetLogic(out Ex_Update_CharacterControl _characterControl, nameof(Ex_Update_CharacterControl)))
            {
                if (mRotLerp > 0)
                {
                    _characterControl.SetRot(
                        Quaternion.Lerp(mTransform.rotation, mTargetRot, mRotLerp * _deltaTime),
                        mRotPriority);
                    return;
                }
                _characterControl.SetRot(mTargetRot, mRotPriority);
            }
        }
    }
}