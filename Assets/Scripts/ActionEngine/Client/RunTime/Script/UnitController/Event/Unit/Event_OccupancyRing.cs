using System;
using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    /// <summary>
    /// 占格围环事件（自驱动，与 <see cref="Event_PathFind"/> 写法一致）：怪追玩家时不走 NavMesh 全路径，
    /// 而是每帧向「游戏层占用网格给出的下一途经点」移动——实现「实时占脚下格、离开即释放、逐环围圈」。
    ///
    /// 本层只做两件事：① 按间隔向玩家 SM 上的 <see cref="Ex_OccupancyGrid"/> 取下一步世界点；② 用 AutoMove 驱动移动。
    /// 网格生成/选格/A*/认领全在引擎层 <see cref="Ex_OccupancyGrid"/> 内（**无桥**，引擎自包含），返回 null = 到位/无目标 → 停下交上层 AI 攻击。
    /// 与 Event_PathFind 并存：旧寻路事件不动，本事件供追击 Action 单独挂载验证。
    /// </summary>
    [System.Serializable]
    public class Event_OccupancyRing : IActionEventData
    {
        [SerializeField] protected float mUpdateInterval = 0.2f;   // 网格查询间隔(秒)，移动仍每帧执行
        [SerializeField] protected float mRadius = 0.35f;          // 到达途经点判定半径
        [SerializeField] protected byte mUesType = 0;             // 移动方式:0模拟输入/1写GValue/2Translate/3Move
        [SerializeField] protected GFloat mGSpeed = new GFloat(1.0f);
        [SerializeField] protected GPoint mGVelocity = new GPoint();
        [SerializeField] protected float mEngageRadius = 4.8f;     // 交战半径:超出则直趋玩家接近,以内才铺环
        [SerializeField] protected float mAttackRange = 0.7f;      // 攻击距离:进入即就地认领脚下格停下交攻击
        [SerializeField] protected float mRing0 = 0.7f;            // 内环半径(=攻击距离下沿)
        [SerializeField] protected float mRing1 = 1.7f;            // 中环半径
        [SerializeField] protected float mRing2 = 2.7f;            // 外环半径
        [SerializeField] protected bool mIsDraw = false;

        /// <summary> 游戏层注册的动态避障（本事件独立持有，不依赖 Event_PathFind）：AutoMove 前修正速度方向绕开贴身单位。
        /// 参数: (selfTransform, velocity) => adjustedVelocity；未注册则不做避障。 </summary>
        public static Func<Transform, Vector3, Vector3> ExternalMoveAvoidance;

        [NonSerialized] private Vector3? mCachedWaypoint;
        [NonSerialized] private float[] mRingRadii;

        #region Property
        [EditorProperty("网格查询间隔", EditorPropertyType.EEPT_Float)]
        public float UpdateInterval { get { return mUpdateInterval; } set { mUpdateInterval = value; } }
        [EditorProperty("到达途经点半径", EditorPropertyType.EEPT_Float)]
        public float Radius { get { return mRadius; } set { mRadius = value; } }
        [EditorProperty("移动方案", EditorPropertyType.EEPT_Enum, EnumNames =
            new[] { "模拟移动输入", "将每帧移动向量写入GValue", "Translate(强制位移)", "Move(碰撞检测)" })]
        public byte UesType { get { return mUesType; } set { mUesType = value; } }
        [EditorProperty("移动速度", EditorPropertyType.EEPT_GFloat)]
        public GFloat GSpeed { get { return mGSpeed; } set { mGSpeed = value; } }
        [EditorProperty("每帧位移向量", EditorPropertyType.EEPT_GPoint)]
        public GPoint GVelocity { get { return mGVelocity; } set { mGVelocity = value; } }
        [EditorProperty("交战半径(超出直趋玩家)", EditorPropertyType.EEPT_Float)]
        public float EngageRadius { get { return mEngageRadius; } set { mEngageRadius = value; } }
        [EditorProperty("攻击距离(进入即认领停下)", EditorPropertyType.EEPT_Float)]
        public float AttackRange { get { return mAttackRange; } set { mAttackRange = value; } }
        [EditorProperty("内环半径", EditorPropertyType.EEPT_Float)]
        public float Ring0 { get { return mRing0; } set { mRing0 = value; } }
        [EditorProperty("中环半径", EditorPropertyType.EEPT_Float)]
        public float Ring1 { get { return mRing1; } set { mRing1 = value; } }
        [EditorProperty("外环半径", EditorPropertyType.EEPT_Float)]
        public float Ring2 { get { return mRing2; } set { mRing2 = value; } }
        [EditorProperty("绘制途经点(Editor有效)", EditorPropertyType.EEPT_Bool, LabelWidth = 150)]
        public bool IsDraw { get { return mIsDraw; } set { mIsDraw = value; } }
        #endregion

        public int GetEvenType() => (int)EEvenType.EET_OccupancyRing;
        public IActionEventData Creact() => new Event_OccupancyRing();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            mCachedWaypoint = null;
            mRingRadii = new[] { mRing0, mRing1, mRing2 };
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            if (_stateMachine == null || _stateMachine.CurUnit == null)
                return;
            Transform _self = _stateMachine.CurUnit.transform;

            // 实时占格：每帧向网格取下一步（内部每帧占脚下格），不再节流缓存，保证「跑到哪占到哪」。
            if (mRingRadii == null)
                mRingRadii = new[] { mRing0, mRing1, mRing2 };
            mCachedWaypoint = ResolveOccupancyStep(_self);

            if (!mCachedWaypoint.HasValue)
            {
                // 到位 / 无目标：停下，交上层 AI（攻击打断）接管。
                StopMove(_actionState);
                return;
            }

            Vector3 _offset = mCachedWaypoint.Value - _self.position;
            _offset.y = 0f;
            if (_offset.sqrMagnitude <= mRadius * mRadius)
            {
                StopMove(_actionState);
                return;
            }
            AutoMove(_offset, _actionState, _actionTime.Deltatime);
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            mCachedWaypoint = null;
            StopMove(_actionState);
        }

        /// <summary> 直接解析玩家 SM 上的 Ex_OccupancyGrid（无桥）算下一步：找不到玩家/网格则返回 null(停下)。 </summary>
        private Vector3? ResolveOccupancyStep(Transform _self)
        {
            ActionEngine_Unit _player = ActionEngineManager_Input.Instance?.Player;
            if (_player == null)
                return null;
            ActionStateMachine _playerSm = _player.ActionStateMachine;
            if (_playerSm == null)
                return null;
            if (!_playerSm.TryGetStaticLogic(out Ex_OccupancyGrid _logic, nameof(Ex_OccupancyGrid)))
                return null;
            _logic.EnsureGrid(mRingRadii, _player.transform.position);
            return _logic.ResolveNextStep(_self, mEngageRadius, mAttackRange);
        }

        /// <summary> 按UesType执行移动：0模拟输入/1写GValue/2Translate/3CharacterController.Move（对齐 Event_PathFind） </summary>
        private void AutoMove(Vector3 _velocity, ActionStatePart _part, float deltaTime)
        {
            // 本事件独立的动态避障委托（不依赖 Event_PathFind）：怪贴身时侧让绕开，减少 body 互顶。
            if (ExternalMoveAvoidance != null)
            {
                Transform selfTf = _part.ActionStateMachine.CurUnit.transform;
                _velocity = ExternalMoveAvoidance(selfTf, _velocity);
            }

            ActionStateMachine _StateMachine = _part.ActionStateMachine;
            if (mUesType == 0)
            {
                float _speed = mGSpeed.GetValue(_part);
                Vector3 _moveDir = _velocity.normalized * _speed;
                _StateMachine.SetMoveInput(_moveDir, _moveDir);
            }
            else if (mUesType == 1)
            {
                mGVelocity.SetValue(_part, new PointData(_velocity, Quaternion.LookRotation(_velocity)));
            }
            else if (mUesType == 2)
            {
                float _speed = mGSpeed.GetValue(_part);
                Transform transform = _StateMachine.CurUnit.transform;
                transform.Translate(_velocity.normalized * _speed * deltaTime);
            }
            else if (mUesType == 3)
            {
                float _speed = mGSpeed.GetValue(_part);
                _StateMachine.TryGetLogic(out Ex_Update_CharacterControl _move, nameof(Ex_Update_CharacterControl));
                _move.CharacterVelocity += _velocity * _speed;
            }
        }

        private void StopMove(ActionStatePart _actionState)
        {
            if (mUesType == 0)
            {
                _actionState.ActionStateMachine.SetMoveInputStop();
            }
            else if (mUesType == 1)
            {
                mGVelocity.SetValue(_actionState, new PointData(Vector3.zero, Quaternion.LookRotation(Vector3.zero)));
            }
        }

        public void EditorDraw(CharacterConfig characterConfig, ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (!mIsDraw || !Application.isPlaying)
                return;
            if (!mCachedWaypoint.HasValue)
                return;
            Transform _mainTrans = _actionState.ActionStateMachine.CurUnit.transform;
            EngineScenceDraw.Sphere(mCachedWaypoint.Value, Quaternion.identity, mRadius, Color.magenta);
            EngineScenceDraw.Line(_mainTrans.position, mCachedWaypoint.Value, Color.magenta);
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_OccupancyRing _event = _eventData as Event_OccupancyRing;
            _event.UpdateInterval = mUpdateInterval;
            _event.Radius = mRadius;
            _event.UesType = mUesType;
            _event.GSpeed = (GFloat)mGSpeed.Clone();
            _event.GVelocity = (GPoint)mGVelocity.Clone();
            _event.EngageRadius = mEngageRadius;
            _event.AttackRange = mAttackRange;
            _event.Ring0 = mRing0;
            _event.Ring1 = mRing1;
            _event.Ring2 = mRing2;
            _event.IsDraw = mIsDraw;
            return _event;
        }
    }
}
