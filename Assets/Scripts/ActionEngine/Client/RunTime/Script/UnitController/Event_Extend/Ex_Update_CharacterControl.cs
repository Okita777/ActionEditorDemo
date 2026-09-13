using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class Ex_Update_CharacterControl : ActionLogics
    {
        public delegate Vector3 DMoveCorrection(Vector3 _moveVector);
        private readonly int RotConst = 6;

        private bool mIsMove = false;
        private bool mChangeGround_last = false;
        private CharacterController mCharacter
        {
            get
            {
                if (mActionStateMachine == null) return null;
                var curUnit = mActionStateMachine.CurUnit;
                if (curUnit == null || curUnit.RootTarget == null) return null;
                mActionStateMachine.TryGetComponent(out CharacterController _characterController,
                                    nameof(CharacterController), curUnit.RootTarget);
                return _characterController;
            }
        }

        public Vector3 CharacterVelocity;//玩家位移速度
        public Vector3 CharacterMove;//玩家位移
        public DMoveCorrection MoveCorrection = null;

        public float CharacterGravity { get; set; }
        public float PosY { get; set; }

        public bool ChackGround { get; private set; } = false;
        public bool ChackGround_Character { get; private set; } = false;

        private Quaternion[] ChracterRots { get; set; }//玩家旋转，但是不同等级  序列号越大优先级越低
        private bool[] IsRots;
        private ActionStateMachine mActionStateMachine;
        private bool IsTeleprot = false;
        private Vector3 TeleprotTarget = Vector3.zero;

        #region Init
        public override void Start(ActionStateMachine _actionState)
        {
            mActionStateMachine = _actionState;
            mActionStateMachine.CurUnit.AnimatorMoveCallBack = OnAnimatorMove;
            if (mCharacter is not null)
            {
                //数据初始化
                mIsMove = true;
                // mPosY = CurUnit.transform.position.y;
                CharacterVelocity = Vector3.zero;
                ChracterRots = new Quaternion[RotConst];
                IsRots = new bool[RotConst];
            }
            else
            {
                mIsMove = false;
                EngineDebug.LogError($"角色无法位移 [<color=#FFCC00>CharacterController</color>] 获取失败！！");
            }
        }
        #endregion

        #region Update
        public override void Update(ActionStateMachine _actionState, float _delaTime)
        {
            if (_actionState == null) return;
            // RemoteProxy 纯表现，位姿由网络下行接管
            if (_actionState.Authority == SimulationAuthority.RemoteProxy) return;
            // ServerAuthoritative 需显式开启：未开启时位姿由外部上报驱动，跑逻辑位移会与之形成两个位移源
            if (_actionState.Authority == SimulationAuthority.ServerAuthoritative
                && !_actionState.ServerLogicSimulationEnabled) return;
            if (RepelledUpdate(_delaTime)) return;//无位移时不继续执行

            CharacterController character = mCharacter;
            if (character == null) return;
            if (!character.gameObject.activeSelf || !character.enabled) return;


            if (mIsMove)
            {
                //检查角色是否在地面
                var curUnit = _actionState.CurUnit;
                if (curUnit == null || curUnit.transform == null) return;
                ChackGround = Physics.Raycast(curUnit.transform.TransformPoint(0, 0.2f, 0), Vector3.down, 0.3f);

                //浮空时重置重力
                if (mChangeGround_last != ChackGround)
                {
                    if (!ChackGround && PosY < 0)
                    {
                        // Debug.Log("触发");
                        PosY = 0f;
                    }
                    mChangeGround_last = ChackGround;
                }

                //常规重力计算
                PosY -= CharacterGravity * _delaTime;
                PosY = Mathf.Max(PosY, -CharacterGravity * 3);//限制下落的最大加速度
                CharacterVelocity.y += PosY;

                CharacterVelocity *= _delaTime;
                _moveDelta += (CharacterVelocity + CharacterMove);
                //character.Move(CharacterVelocity + CharacterMove);
                ChackGround_Character = character.isGrounded;
                CharacterVelocity = Vector3.zero;//位移后清空速度数据
                CharacterMove = Vector3.zero;//位移后清空位移数据

                bool _isRot = false;
                Quaternion _curRot = Quaternion.identity;

                //数值越大，优先级越高
                for (int i = ChracterRots.Length - 1; i >= 0; i--)
                {
                    if (IsRots[i])
                    {
                        _curRot = ChracterRots[i];
                        _isRot = true;
                        IsRots[i] = false;
                        //Debug.LogWarning($"旋转哦: [{_curRot.eulerAngles.y}]");
                        break;
                    }
                }

                if (_isRot)
                {
                    if (curUnit != null && curUnit.RootTarget != null)
                    {
                        curUnit.RootTarget.rotation = _curRot;
                    }
                }
            }
        }

        Vector3 _moveDelta = Vector3.zero;
        public void OnAnimatorMove(ActionStateMachine _stateMachine, Vector3 _rootDelta, Quaternion _rootRotDelta)
        {
            if (!mIsMove) return;
            //_rootDelta 为 ActionEngine_Unit 存储并传入的 Root 位移（已乘 RootWeight），此处直接叠加至移动
            Vector3 gvacity = _moveDelta.y * Vector3.up;
            _moveDelta.y = 0;
            Vector3 _nowMoveDelta = _moveDelta + _rootDelta;
            if (MoveCorrection is not null) _nowMoveDelta = MoveCorrection(_nowMoveDelta);
            Vector3 _finalMove = Vector3.zero;
            if (IsTeleprot)
            {
                mCharacter.transform.position = TeleprotTarget;
                IsTeleprot = false;
            }
            else
            {
                _finalMove = _nowMoveDelta + gvacity;
                mCharacter.Move(_finalMove);
            }
            //回写最终输出到 CharacterController 的位移向量，供蓝图节点读取
            if (_stateMachine.CurUnit != null) _stateMachine.CurUnit.FinalCharacterMove = _finalMove;
            _moveDelta = Vector3.zero;

            //_rootRotDelta 为 ActionEngine_Unit 传入的 Root 旋转增量，叠加至 RootTarget
            var curUnit = _stateMachine.CurUnit;
            if (curUnit != null && curUnit.RootTarget != null)
            {
                curUnit.RootTarget.rotation = _rootRotDelta * curUnit.RootTarget.rotation;
            }
        }

        public override void LateUpdate(ActionStateMachine _actionState, float _deltaTime)
        {
            // headless（无 Animator）且非 RemoteProxy：帧末提交已累计逻辑位移（含 ServerAuthoritative）。
            if (_actionState == null
                || _actionState.Authority == SimulationAuthority.RemoteProxy
                || _actionState.AnimValid)
            {
                return;
            }

            // 无 Animator 的纯逻辑单位不会收到 Unity 的 OnAnimatorMove，需在帧末主动提交已累计的逻辑位移。
            OnAnimatorMove(_actionState, Vector3.zero, Quaternion.identity);
        }

        private bool RepelledUpdate(float _deltaTime)
        {
            if (mRepelled_E > 0)
            {
                mRepelled_E -= _deltaTime;
                float _property = Mathf.Max(0, mRepelled_E / mRepelled_S);
                Vector3 _nowPos = Vector3.Lerp(mRepelled_DeltaPos_Last, Vector3.zero, _property);
                Vector3 _deltaPos = _nowPos - mRepelled_DeltaPos_Last;
                var character = mCharacter;
                if (character != null)
                {
                    character.Move(_deltaPos);
                }
                mRepelled_DeltaPos_Last = _nowPos;
                if (mRepelled_E <= 0 && mKeepTime <= 0)
                {//继承最终的力度

                }
                return true;
            }

            if (mKeepTime > 0)
            {
                mKeepTime -= _deltaTime;
                return true;
            }
            return false;
        }
        #endregion

        public void SetPosition(Vector3 _pos)
        {
            if (!mIsMove) return;

            CharacterController character = mCharacter;
            if (character == null) return;

            // SetPosition is an authoritative teleport API. Applying only in the next ActionEngine
            // update leaves callers observing the old transform for one frame and can also stall
            // while simulation is intentionally frozen during a stage transition.
            TeleprotTarget = _pos;
            character.transform.position = _pos;
            IsTeleprot = false;
            Physics.SyncTransforms();

            PosY = 0;
            CharacterVelocity = Vector3.zero;//位移后清空速度数据
            CharacterMove = Vector3.zero;//位移后清空位移数据
        }

        public void SetRot(Quaternion _rot, int _priority = 0)
        {
            //EngineDebug.LogWarning($"呼叫旋转: [{_rot.eulerAngles.y}] [{_priority}]");

            // if(!_actionState.IsLocalClient)return;//非本地客户端  不执行
            int _nowPriority = Mathf.Clamp(_priority, 0, RotConst - 1);
            IsRots[_nowPriority] = true;
            ChracterRots[_nowPriority] = _rot;
        }

        public Vector3 GetV3ToInputMove(ActionStateMachine _actionState, Vector3 _mouveDir, Quaternion _referDir)
        {
            return Quaternion.LookRotation(_actionState.PlayerInputMoveDir) * _referDir * _mouveDir;
        }
        public Vector3 GetV3ToInputMove(ActionStateMachine _actionState, Vector3 _mouveDir)
        {
            if (_actionState.PlayerInputMoveDir.sqrMagnitude < 0.0001f)
            {
                return _actionState.CurUnit.transform.TransformDirection(_mouveDir);
            }
            Quaternion _rot = Quaternion.LookRotation(_actionState.PlayerInputMoveDir) *
                              Quaternion.Euler(0, _actionState.GetCharacterFor.eulerAngles.y, 0);
            _mouveDir = _rot * _mouveDir;
            return _mouveDir;
        }

        //击退相关
        private Vector3 mRepelled_Pos_S, mRepelled_Pos_E;
        private Vector3 mRepelled_DeltaPos_Last;
        private float mRepelled_S, mRepelled_E;
        private float mKeepTime;
        public void BeRepelled(Vector3 _finishPos, float _repelledTime, float _keepTime)
        {
            mRepelled_Pos_S = Vector3.zero;
            mRepelled_Pos_E = _finishPos;
            mRepelled_S = _repelledTime;
            mRepelled_E = _repelledTime;
            mKeepTime = _keepTime;

            mRepelled_DeltaPos_Last = mRepelled_Pos_S;
        }
    }
}