using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;
using UnityEngine.AI;

namespace AsiTimeLine.RunTime
{
    /// <summary>
    /// 群体宏观寻路结果（P1 #7/#8）：区分「向心途经点」与「环上 slot 真终点」。
    /// AllowContinue=true：向心接近的途经点，允许「接近即提前续路」（避免到点顿挫）；
    /// AllowContinue=false：环上 slot 是真终点，禁止续路，让怪正常走到并停下（到位后 provider 会返回 null 交上层）。
    /// MovementInterrupt：写入 FindPathInterrput 的位移打断类型（0=正常位移，1=无法位移）——
    /// 到位但其 slot 离玩家超出攻击范围（外环待命位）的怪置 1，停在 slot 待命，避免与上层攻击接近径向拔河。
    /// </summary>
    public struct CrowdMacroResult
    {
        public Vector3 Pos;
        public bool AllowContinue;
        public byte MovementInterrupt;
        public CrowdMacroResult(Vector3 pos, bool allowContinue, byte movementInterrupt = 0)
        {
            Pos = pos;
            AllowContinue = allowContinue;
            MovementInterrupt = movementInterrupt;
        }
    }

    /// <summary> 寻路事件：根据起点与目标点计算NavMesh路径并驱动单位沿路径移动 </summary>
    [System.Serializable]
    public class Event_PathFind : IActionEventData, INavigationQueryWorkItem
    {
        [SerializeField] protected float mUpdateInterval = 0.2f;           // 路径刷新间隔(秒)，-1表示不自动刷新
        [SerializeField] protected GGroupPoint mPathPoints = new GGroupPoint();  // 路径点列表(从终点到起点逆序)
        [SerializeField] protected GraphEvent_NoValue_Point mStartPos = new GraphEvent_NoValue_Point();  // 起点(通常为自身位置)
        [SerializeField] protected GraphEvent_NoValue_Point mTargetPos = new GraphEvent_NoValue_Point(); // 目标点
        [SerializeField] protected float mRadius = 0.5f;                   // 到达路径点的判定半径(平方比较用)
        [SerializeField] protected byte mUesType = 0;                      // 移动方式:0模拟输入/1写GValue/2Translate/3Move
        [SerializeField] protected bool mChangeActionToFinish = false;    // 到达终点时是否跳转Action
        [SerializeField] protected bool mSetGvalueToFinish = false;    // 到达终点时是否修改GV
        [SerializeField] protected int mActionID = 0;                     // 到达终点时跳转的ActionID
        [SerializeField] protected GValue_Setting mGvalueSetting = new GValue_Setting(); // 到达终点时修改的GV
        [SerializeField] protected GFloat mGSpeed = new GFloat(1.0f);     // 移动速度倍率
        [SerializeField] protected GPoint mGVelocity = new GPoint();       // 每帧位移向量输出(GValue模式)
        [SerializeField] protected bool mIsDraw = false;                  // 是否在编辑器中绘制路径
        [SerializeField] protected float mSoftRadius = 1.0f;              // 目标点投影到NavMesh的最大搜索半径
        [SerializeField] protected float mTimeOut = -1.0f;//寻路最大持续时间
        [SerializeField] protected GValue_SetEnum mFindPathInterrput = new GValue_SetEnum();
        private const float SameWaypointThreshold = 0.18f;
        private const float NoProgressMoveThreshold = 0.5f;
        private const int SameSolutionUpgradeThreshold = 3;
        private const float RepeatMinElapsedTime = 1.0f;
        private const float EscapeRepathCooldown = 1.0f;
        private const float StuckDirectTargetThreshold = 3.0f;
        private const float RebuildSkipProgressMin = 0.01f;
        private const float RebuildSkipTargetMoveThr = 0.5f;

        /// <summary> 外部卡住解决器（游戏层注册），当寻路卡住时调用，返回避障点或 null </summary>
        /// <remarks> 参数: (selfTransform, selfPos, nextWp) => escapePos or null。需排除自身，与 L5 一致。 </remarks>
        public static Func<Transform, Vector3, Vector3, Vector3?> ExternalStuckResolver;

        /// <summary> 外部移动避障（游戏层注册），在 AutoMove 前修正速度方向以绕开动态障碍 </summary>
        /// <remarks> 参数: (selfTransform, velocity) => adjustedVelocity </remarks>
        public static Func<Transform, Vector3, Vector3> ExternalMoveAvoidance;

        /// <summary> 外部群体宏观寻路（游戏层注册，POE2 Hybrid L0）：大群追玩家时给出「沿流场的宏观下一步点」
        /// 替换寻路终点，返回 null 表示不适用（回退全路径）。是否启用/人数阈值/是否在追玩家等游戏层判断，
        /// 全部由委托实现内部决定，本层只负责「返回非空就用」。 </summary>
        /// <remarks> 参数: (selfTransform, goalPos) => CrowdMacroResult or null。透传 Transform 供游戏层按单位身份分配 slot（P1 #7/#8）。 </remarks>
        public static Func<Transform, Vector3, CrowdMacroResult?> ExternalCrowdMacroTarget;

        /// <summary> 外部玩家宏观寻路（游戏层注册）：玩家点击寻路时给出「绕开怪堆的宏观下一步点」替换终点，
        /// 返回 null 表示不适用（回退纯 NavMesh）。与 <see cref="ExternalCrowdMacroTarget"/> 不同：那张流场 goal=玩家、
        /// 给怪用（故对玩家排除）；这张 goal=点击点，走行性不硬挡、只在 cost 上避怪，专供玩家自身绕怪堆。 </summary>
        /// <remarks> 参数: (selfPos, goalPos=点击点) => macroTarget or null。 </remarks>
        public static Func<Vector3, Vector3, Vector3?> ExternalPlayerCrowdMacroTarget;

        /// <summary> 玩家点击寻路「绕开怪堆」总开关（运行时可切）：默认 false，等价原版纯 NavMesh；
        /// 打开且 <see cref="ExternalPlayerCrowdMacroTarget"/> 已注册时才走软代价场绕行。 </summary>
        public static bool EnablePlayerCrowdAvoid = true;

        /// <summary> 玩家绕怪堆诊断日志节流（调试期用，调好后移除）。 </summary>
        private static float sLastPlayerCrowdLogTime = -999f;
        private const float PlayerCrowdLogInterval = 0.5f;

        #region Property
        [EditorProperty("路径更新间隔", EditorPropertyType.EEPT_Float)]
        public float UpdateInterval
        {
            get { return mUpdateInterval; }
            set { mUpdateInterval = value; }
        }
        [EditorProperty("寻路时效范围(超时退出)", EditorPropertyType.EEPT_Float)]
        public float TimeOut
        {
            get { return mTimeOut; }
            set { mTimeOut = value; }
        }
        [EditorProperty("路径点的组", EditorPropertyType.EEPT_GGroupPoint)]
        public GGroupPoint PathPoints
        {
            get { return mPathPoints; }
            set { mPathPoints = value; }
        }
        [EditorProperty("路径节点半径", EditorPropertyType.EEPT_Float)]
        public float Radius
        {
            get { return mRadius; }
            set { mRadius = value; }
        }
        [EditorProperty("导航网格外最大搜索半径", EditorPropertyType.EEPT_Float)]
        public float SoftRadius
        {
            get { return mSoftRadius; }
            set { mSoftRadius = value; }
        }
        [EditorProperty("自身位置", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Point StartPos
        {
            get { return mStartPos; }
            set { mStartPos = value; }
        }
        [EditorProperty("目标点", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Point TargetPos
        {
            get { return mTargetPos; }
            set { mTargetPos = value; }
        }
        [EditorProperty("移动方案", EditorPropertyType.EEPT_Enum, EnumNames =
            new[] { "模拟移动输入", "将每帧移动向量写入GValue", "Translate(强制位移)", "Move(碰撞检测)" })]
        public byte UesType
        {
            get { return mUesType; }
            set { mUesType = value; }
        }
        [EditorProperty("每帧位移向量", EditorPropertyType.EEPT_GPoint)]
        public GPoint GVelocity
        {
            get { return mGVelocity; }
            set { mGVelocity = value; }
        }
        [EditorProperty("移动速度", EditorPropertyType.EEPT_GFloat)]
        public GFloat GSpeed
        {
            get { return mGSpeed; }
            set { mGSpeed = value; }
        }
        [EditorProperty("结束时设置GV", EditorPropertyType.EEPT_Bool)]
        public bool SetGvalueToFinish
        {
            get { return mSetGvalueToFinish; }
            set { mSetGvalueToFinish = value; }
        }
        [EditorProperty("设置GV", EditorPropertyType.EEPT_GValueSetting)]
        public GValue_Setting GvalueSetting
        {
            get { return mGvalueSetting; }
            set { mGvalueSetting = value; }
        }
        [EditorProperty("结束时跳转Action", EditorPropertyType.EEPT_Bool)]
        public bool ChangeActionToFinish
        {
            get { return mChangeActionToFinish; }
            set { mChangeActionToFinish = value; }
        }
        [EditorProperty("跳转Action", EditorPropertyType.EEPT_Action)]
        public int ActionID
        {
            get { return mActionID; }
            set { mActionID = value; }
        }
        [EditorProperty("路径寻找打断类型", EditorPropertyType.EEPT_SetGEnum)]
        public GValue_SetEnum FindPathInterrput
        {
            get { return mFindPathInterrput; }
            set { mFindPathInterrput = value; }
        }
        [EditorProperty("绘制路径(Editor有效)", EditorPropertyType.EEPT_Bool, LabelWidth = 150)]
        public bool IsDraw
        {
            get { return mIsDraw; }
            set { mIsDraw = value; }
        }
        #endregion

        [NonSerialized] protected float mUpdateIntervalLast;
        [NonSerialized] protected float mSelfDis;
        [NonSerialized] protected bool mIsFinish;
        [NonSerialized] protected bool mHasRepathSnapshot;
        [NonSerialized] protected Vector3 mLastRepathSelfPos;
        [NonSerialized] protected Vector3 mLastRepathNextWp;
        [NonSerialized] protected int mSameSolutionStuckCount;
        [NonSerialized] private float mRepeatStartTime;
        [NonSerialized] private Vector3 mRepeatAnchorSelfPos;
        [NonSerialized] private Vector3 mLastFramePos;
        [NonSerialized] private float mStuckAccumTime;
        [NonSerialized] private float mMoveAccumTime;
        [NonSerialized] private float mTimeOutCheck;
        [NonSerialized] private bool mIsStuckEscaping;
        [NonSerialized] private float mDistToWpAtLastRebuild = float.MaxValue;
        [NonSerialized] private Vector3 mLastRebuildTargetPos;
        [NonSerialized] private Vector3 mCachedValidTargetPos;
        [NonSerialized] private bool mUsingCrowdMacro;          // 本次寻路终点是否被替换为流场宏观点（决定到达后是续路还是停下）
        [NonSerialized] private float mLastMacroContinueTime;   // 上次因接近宏观点而提前续路的时间，用于最小间隔保护
        [NonSerialized] private ulong mNavigationChangeSequence;
        [NonSerialized] private bool mNavigationBlocked;
        [NonSerialized] private ulong mPathQueryStableUnitId;
        [NonSerialized] private ulong mPathQueryOwnerLease;
        [NonSerialized] private bool mPathQueryPending;
        [NonSerialized] private bool mPendingPathIsInitial;
        [NonSerialized] private PointData mPendingPathSelf;
        [NonSerialized] private PointData mPendingPathTarget;
        [NonSerialized] private ActionStatePart mPendingPathActionState;
        [NonSerialized] private ActionMachineTime mPendingPathActionTime;
        private const float ZeroTargetSqrThreshold = 0.01f;
        private const float StuckCheckInterval = 0.3f;
        private const float StuckMoveThreshold = 0.08f;
        private const float CrowdMacroContinueMinGap = 0.05f;   // 提前续路的最小时间间隔，防止宏观点被拉近时每帧重算
        private const float CrowdMacroContinueDistFactor = 3f;  // 距最后一个宏观点 < mRadius×此值 时提前续路，避免走到才续

        /// <summary> 为 true 时输出 Enter / 重复路径计数等详细 Log（排查摇摆、路径抖动时临时打开）。 </summary>
        public static bool EnableVerbosePathFindLog = false;

        /// <summary> 「同一路点 + 无位移进展」日志最小间隔（秒），避免每帧刷屏。 </summary>
        private const float RepeatedSolutionLogInterval = 1.0f;

        /// <summary> Verbose 模式下重复路径日志间隔（秒），仍做节流避免每帧输出。 </summary>
        private const float RepeatedSolutionVerboseLogInterval = 0.12f;

        /// <summary> realtime-stuck 触发后的日志最小间隔（秒）。 </summary>
        private const float RealtimeStuckLogInterval = 0.75f;

        [NonSerialized] private float mLastRepeatedSolutionLogTime = -999f;
        [NonSerialized] private float mLastRealtimeStuckLogTime = -999f;
        [NonSerialized] private float mLastAggressiveEscapeFailLogTime = -999f;

        private const float AggressiveEscapeFailLogInterval = 2.0f;

        // ── DELETE-ME：#7/#8 诊断——观察怪进攻击距离后寻路是否仍在驱动它移动（判定「到位返回 null 交上层」是否成立）──
        public static bool EnablePathMoveDiag = false;
        private const float PathMoveDiagInterval = 0.5f;
        private const float PathMoveDiagNearDist = 3.5f;
        private static readonly Dictionary<ulong, float> s_pathMoveDiagTime = new Dictionary<ulong, float>(64);

        public int GetEvenType() => (int)EEvenType.EET_PathFind;
        public IActionEventData Creact() => new Event_PathFind();

        /// <summary> 进入寻路：校验目标在NavMesh上，若距离足够则计算初始路径 </summary>
        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            //mIsValid = false;
            CancelPendingPathQuery();
            mIsFinish = false;
            mNavigationChangeSequence = NavigationRevisionRuntime.ChangeSequence;
            mNavigationBlocked = false;
            ResetRepathUpgradeState();
            mTimeOutCheck = TimeOut;
            ActionMachineTime MachineTime = EngineResourcesManager.Instance.MachineTime;
            PointData _Self = StartPos.value(_actionState, MachineTime);
            if (!EnsureNavigationReady(_actionState, _Self.pos))
                return;
            PointData _Target = TargetPos.value(_actionState, MachineTime);
            mUpdateIntervalLast = mUpdateInterval;
            mSelfDis = mRadius * mRadius;
            ResetStuckTracking(_Self.pos);
            mDistToWpAtLastRebuild = float.MaxValue;
            if (_Target.pos.sqrMagnitude < ZeroTargetSqrThreshold && mCachedValidTargetPos.sqrMagnitude >= ZeroTargetSqrThreshold)
            {
                _Target = new PointData(mCachedValidTargetPos, _Target.rot);
            }
            else if (_Target.pos.sqrMagnitude >= ZeroTargetSqrThreshold)
            {
                mCachedValidTargetPos = _Target.pos;
            }
            mLastRebuildTargetPos = _Target.pos;
            if ((_Self.pos - _Target.pos).sqrMagnitude > mSelfDis)  // 距离大于半径才需要寻路
            {
                QueueBudgetedPathBuild(
                    _Self,
                    _Target,
                    _actionState,
                    EngineResourcesManager.Instance.MachineTime,
                    true);
                mUpdateIntervalLast = 0f;
                return;
                //else
                //{
                //    //EngineScenceDraw.Text(pos, $"未找到寻路网格({_mainTrans.name})", 20, Color.red);
                //    EngineDebug.LogError($"<color=#ff0000>没有找到寻路网格</color>[{_Target.pos}] [{EngineDebug.DebugActionStatePart(_actionState)}]");
                //    EngineDebug.DrawSphere(_Target.pos, 2.2f, Color.black, 10);
                //}
                //else
                //{
                //    mPathPoints.GetValue(_actionState).Clear();
                //    mIsFinish = true;
                //    StopMove(_actionState);
                //}
            }
            else  // 已在目标半径内，直接完成
            {
                EngineDebug.Log($"[PathFind] Enter already in target radius");
                mPathPoints.GetValue(_actionState).Clear();
                mIsFinish = true;
                StopMove(_actionState);
            }
        }

        /// <summary> 每帧更新：按间隔刷新路径，沿路径点移动，到达终点时停止或跳转Action </summary>
        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            PointData _Self = StartPos.value(_actionState, _actionTime);
            if (!EnsureNavigationReady(_actionState, _Self.pos))
                return;
            PointData _Target = TargetPos.value(_actionState, _actionTime);
            //Debug.DrawRay(_Target.pos, Vector3.up, Color.black, 0.1f);

            if (mTimeOutCheck > 0)
            {
                mTimeOutCheck -= _actionTime.Deltatime;
                if(mTimeOutCheck<= 0)
                {
                    if (!mIsFinish)
                    {
                        OnFinish(_stateMachine, _actionState);
                    }
                }
            }

            if (mPathQueryPending || mUpdateInterval > -1.0f)
            {
                if (!mPathQueryPending)
                {
                    mUpdateIntervalLast -= _actionTime.Deltatime;
                }
                if (mPathQueryPending || mUpdateIntervalLast <= 0f)
                {
                    if (_Target.pos.sqrMagnitude < ZeroTargetSqrThreshold && mCachedValidTargetPos.sqrMagnitude >= ZeroTargetSqrThreshold)
                    {
                        _Target = new PointData(mCachedValidTargetPos, _Target.rot);
                    }
                    else if (_Target.pos.sqrMagnitude >= ZeroTargetSqrThreshold)
                    {
                        mCachedValidTargetPos = _Target.pos;
                    }

                    bool skipRebuild = false;
                    List<PointData> _curPoints = mPathPoints.GetValue(_actionState);
                    // mUpdateInterval<=0 表示用户要求每帧重算以精确跟随移动目标，此时禁用「朝路点前进则跳过重算」的进展优化
                    if (mUpdateInterval > 0f && _curPoints.Count > 0 && !mIsStuckEscaping && mDistToWpAtLastRebuild < float.MaxValue)
                    {
                        float _distToWp = Vector3.Distance(_Self.pos, _curPoints[^1].pos);
                        float _targetMoved = Vector3.Distance(_Target.pos, mLastRebuildTargetPos);
                        if (_distToWp < mDistToWpAtLastRebuild - RebuildSkipProgressMin
                            && _targetMoved < RebuildSkipTargetMoveThr)
                        {
                            skipRebuild = true;
                            mDistToWpAtLastRebuild = _distToWp;
                        }
                    }

                    if (!skipRebuild)
                    {
                        QueueBudgetedPathBuild(
                            _Self,
                            _Target,
                            _actionState,
                            _actionTime,
                            false);
                    }
                    else
                    {
                        CancelPendingPathQuery();
                        mUpdateIntervalLast = Mathf.Max(
                            mUpdateIntervalLast,
                            mUpdateInterval);
                    }
                }
            }

            List<PointData> pointDatas = mPathPoints.GetValue(_actionState);
            if (mPathQueryPending && pointDatas.Count == 0)
            {
                StopMove(_actionState);
                return;
            }
            if (pointDatas.Count > 0)
            {
                CheckRealtimeStuck(_actionState, _Self.pos, _actionTime.Deltatime);

                // 流场宏观点是"途经点"而非真终点：在怪走到它之前就提前续路，避免路点清空后进入停止分支产生的"到点停一下"顿挫。
                // 置 mDistToWpAtLastRebuild=MaxValue 让下次重算绕过「朝路点前进则跳过重算」优化，确保真正续上新宏观点。
                if (mUsingCrowdMacro && Time.time - mLastMacroContinueTime >= CrowdMacroContinueMinGap)
                {
                    float _distToMacro = Vector3.Distance(pointDatas[^1].pos, _Self.pos);
                    if (_distToMacro < mRadius * CrowdMacroContinueDistFactor)
                    {
                        mLastMacroContinueTime = Time.time;
                        mUpdateIntervalLast = 0f;
                        mDistToWpAtLastRebuild = float.MaxValue;
                    }
                }

                Vector3 _offsetPos = pointDatas[^1].pos - _Self.pos;
                mSelfDis = mRadius * mRadius;
                if (_offsetPos.sqrMagnitude < mSelfDis)
                {
                    pointDatas.RemoveAt(pointDatas.Count - 1);
                    mDistToWpAtLastRebuild = float.MaxValue;
                    ResetRepathUpgradeState();
                    ResetStuckTracking(_Self.pos);
                }
                if (pointDatas.Count > 0)
                {
                    if (mIsFinish)
                    {
                        mIsFinish = false;
                    }
                    _offsetPos = pointDatas[^1].pos - _Self.pos;
                    AutoMove(_offsetPos, _actionState, _actionTime.Deltatime);
                }
                else
                {
                    if (!mIsFinish)
                    {
                        OnFinish(_stateMachine, _actionState);
                    }
                }
            } 
            else
            {
                if (!mIsFinish)
                {
                    OnFinish(_stateMachine, _actionState);
                    EngineDebug.LogWarning("意外完成寻路!!!");
                }
                else
                {
                    pointDatas.Clear();
                    ResetRepathUpgradeState();
                    StopMove(_actionState);
                }
            }
        }

        private bool QueueBudgetedPathBuild(
            PointData self,
            PointData target,
            ActionStatePart actionState,
            ActionMachineTime actionTime,
            bool initial)
        {
            EnsurePathQueryOwner(actionState.ActionStateMachine);
            bool wasPending = mPathQueryPending;
            mPendingPathSelf = self;
            mPendingPathTarget = target;
            mPendingPathActionState = actionState;
            mPendingPathActionTime = actionTime;
            if (!wasPending)
            {
                mPendingPathIsInitial = initial;
            }
            NavigationQueryPriority priority = mIsStuckEscaping
                ? NavigationQueryPriority.Stuck
                : IsPlayerUnit(actionState.ActionStateMachine)
                    ? NavigationQueryPriority.Player
                    : NavigationQueryPriority.Normal;
            if (!NavigationQueryBudgetRuntime.Queue(
                    mPathQueryStableUnitId,
                    mPathQueryOwnerLease,
                    priority,
                    mNavigationChangeSequence))
            {
                mPathQueryPending = false;
                ClearPendingPathPayload();
                EngineDebug.LogError(
                    $"[PathFind] rejected stale navigation request unit={actionState.ActionStateMachine.CurUnit.gameObject.name} revision={mNavigationChangeSequence}");
                return false;
            }

            mPathQueryPending = true;
            return true;
        }

        public bool ExecuteNavigationQuery(ulong ownerLease)
        {
            if (ownerLease != mPathQueryOwnerLease ||
                !mPathQueryPending ||
                mPendingPathActionState == null)
            {
                throw new InvalidOperationException(
                    "Event_PathFind received a stale navigation query execution.");
            }

            PointData self = mPendingPathSelf;
            PointData target = mPendingPathTarget;
            ActionStatePart actionState = mPendingPathActionState;
            ActionMachineTime actionTime = mPendingPathActionTime;
            bool initial = mPendingPathIsInitial;
            bool pathUpdated = false;
            try
            {
                if (!actionState.ActionStateMachine.TryGetStaticLogic(
                        out Ex_NavMesh navMesh,
                        nameof(Ex_NavMesh)) ||
                    navMesh == null)
                {
                    throw new InvalidOperationException(
                        "Event_PathFind requires Ex_NavMesh static logic.");
                }
                if (!navMesh.CheckPointToNavMash(
                        target.pos,
                        mSoftRadius,
                        out NavMeshHit hit))
                {
                    if (EnableVerbosePathFindLog)
                    {
                        EngineDebug.LogWarning(
                            $"[PathFind] target off NavMesh sample unit={actionState.ActionStateMachine.CurUnit.gameObject.name} self={self.pos} target={target.pos} softR={mSoftRadius}");
                    }
                    return false;
                }

                target = new PointData(hit.position, target.rot);
                mCachedValidTargetPos = hit.position;
                pathUpdated = UpdatePathData(
                    self,
                    target,
                    actionState,
                    actionTime);
                if (pathUpdated)
                {
                    if (!initial)
                    {
                        TryUpgradeRepeatedSolution(
                            actionState,
                            self.pos,
                            target.pos);
                    }
                    List<PointData> rebuiltPoints = mPathPoints.GetValue(
                        actionState);
                    mDistToWpAtLastRebuild = rebuiltPoints.Count > 0
                        ? Vector3.Distance(self.pos, rebuiltPoints[^1].pos)
                        : float.MaxValue;
                    mLastRebuildTargetPos = target.pos;
                    if (initial && EnableVerbosePathFindLog)
                    {
                        EngineDebug.Log(
                            $"[PathFind] Enter ok self={self.pos} targetProj={target.pos} softR={mSoftRadius} radius²={mSelfDis}");
                    }
                }
                return pathUpdated;
            }
            finally
            {
                mPathQueryPending = false;
                ClearPendingPathPayload();
                mUpdateIntervalLast = Mathf.Max(
                    mUpdateIntervalLast,
                    mUpdateInterval);
            }
        }

        private void EnsurePathQueryOwner(ActionStateMachine stateMachine)
        {
            if (stateMachine == null || stateMachine.CurUnit == null)
            {
                throw new InvalidOperationException(
                    "Event_PathFind requires a current unit before scheduling.");
            }

            ulong stableUnitId = NavigationStableUnitIdentity.Require(
                stateMachine.CurUnit);
            if (mPathQueryOwnerLease != 0UL &&
                mPathQueryStableUnitId == stableUnitId &&
                NavigationQueryBudgetRuntime.IsOwner(
                    stableUnitId,
                    mPathQueryOwnerLease))
            {
                return;
            }

            ReleasePathQueryOwner();
            mPathQueryStableUnitId = stableUnitId;
            mPathQueryOwnerLease = NavigationQueryBudgetRuntime.Register(
                stableUnitId,
                this);
        }

        private void CancelPendingPathQuery()
        {
            if (mPathQueryOwnerLease != 0UL)
            {
                NavigationQueryBudgetRuntime.Cancel(
                    mPathQueryStableUnitId,
                    mPathQueryOwnerLease);
            }
            mPathQueryPending = false;
            ClearPendingPathPayload();
        }

        private void ReleasePathQueryOwner()
        {
            if (mPathQueryOwnerLease != 0UL)
            {
                NavigationQueryBudgetRuntime.Unregister(
                    mPathQueryStableUnitId,
                    mPathQueryOwnerLease);
            }
            mPathQueryStableUnitId = 0UL;
            mPathQueryOwnerLease = 0UL;
            mPathQueryPending = false;
            ClearPendingPathPayload();
        }

        private void ClearPendingPathPayload()
        {
            mPendingPathIsInitial = false;
            mPendingPathSelf = default;
            mPendingPathTarget = default;
            mPendingPathActionState = null;
            mPendingPathActionTime = default;
        }

        private bool EnsureNavigationReady(
            ActionStatePart actionState,
            Vector3 selfPosition)
        {
            ulong currentSequence = NavigationRevisionRuntime.ChangeSequence;
            bool changed = mNavigationChangeSequence != currentSequence;
            bool queryReady = NavigationRevisionRuntime.IsQueryReady;
            if (!changed && (queryReady || mNavigationBlocked))
            {
                return queryReady;
            }
            mNavigationChangeSequence = currentSequence;
            mNavigationBlocked = !queryReady;
            CancelPendingPathQuery();
            mPathPoints.GetValue(actionState).Clear();
            mCachedValidTargetPos = default;
            mLastRebuildTargetPos = default;
            mDistToWpAtLastRebuild = float.MaxValue;
            mUsingCrowdMacro = false;
            mLastMacroContinueTime = 0f;
            mUpdateIntervalLast = 0f;
            mIsFinish = false;
            mIsStuckEscaping = false;
            ResetRepathUpgradeState();
            ResetStuckTracking(selfPosition);
            StopMove(actionState);
            return queryReady;
        }

        private void OnFinish(ActionStateMachine _stateMachine, ActionStatePart _actionState)
        {
                        mIsFinish = true;
                        if (UesType == 0)
                        {
                            _stateMachine.SetMoveInputStop();
                        }
                        else if (UesType == 1)
                        {
                            mGVelocity.SetValue(_actionState, new PointData(Vector3.zero, Quaternion.LookRotation(Vector3.zero)));
                        }
                        if (mSetGvalueToFinish)
                        {
                            mGvalueSetting.OnSet(_actionState.ActionStateMachine);
                        }
                        if (mChangeActionToFinish)
                        {
                            _stateMachine.ChangeAction(mActionID, 0, 0);
                        }
                    }
        private void CheckRealtimeStuck(ActionStatePart _actionState, Vector3 selfPos, float deltaTime)
        {
            List<PointData> pointDatas = mPathPoints.GetValue(_actionState);
            if (pointDatas.Count < 1)
                return;

            mMoveAccumTime += deltaTime;
            float movedDist = Vector3.Distance(selfPos, mLastFramePos);

            if (movedDist > StuckMoveThreshold * deltaTime * 3f)
            {
                mStuckAccumTime = 0f;
                mIsStuckEscaping = false;
                mLastFramePos = selfPos;
                return;
            }

            mStuckAccumTime += deltaTime;
            mLastFramePos = selfPos;

            if (mStuckAccumTime < StuckCheckInterval)
                return;

            Vector3 nextWp = pointDatas[^1].pos;
            float distToWp = Vector3.Distance(selfPos, nextWp);
            _actionState.ActionStateMachine.TryGetStaticLogic(out Ex_NavMesh stuckNavMesh, nameof(Ex_NavMesh));
            bool segReachable = stuckNavMesh.IsSegmentReachableForAgent(selfPos, nextWp, NavMesh.AllAreas);
            mIsStuckEscaping = true;
            float nowRt = Time.time;
            if (EnableVerbosePathFindLog || nowRt - mLastRealtimeStuckLogTime >= RealtimeStuckLogInterval)
            {
                mLastRealtimeStuckLogTime = nowRt;
                EngineDebug.LogWarning(
                    $"[PathFind] realtime-stuck unit={_actionState.ActionStateMachine.CurUnit.gameObject.name} stuckTime={mStuckAccumTime:F2} self={selfPos} nextWp={nextWp} distToWp={distToWp:F2} segReachable={segReachable} wpCount={pointDatas.Count}");
            }

            if (ExternalStuckResolver != null)
            {
                Transform selfTf = _actionState.ActionStateMachine.CurUnit != null
                    ? _actionState.ActionStateMachine.CurUnit.transform
                    : null;
                Vector3? avoidanceWp = ExternalStuckResolver(selfTf, selfPos, nextWp);
                if (avoidanceWp.HasValue)
                {
                    Vector3 escPos = avoidanceWp.Value;
                    Vector3 lookDir = nextWp - escPos;
                    if (lookDir.sqrMagnitude < 0.0001f)
                        lookDir = Vector3.forward;
                    pointDatas.Add(new PointData(escPos, Quaternion.LookRotation(lookDir)));
                    EngineDebug.LogWarning(
                        $"[PathFind] monster-avoid unit={_actionState.ActionStateMachine.CurUnit.gameObject.name} self={selfPos} escape={escPos}");
                    mStuckAccumTime = 0f;
                    return;
                }
            }

            if (TrySkipToReachableWaypoint(pointDatas, selfPos, _actionState))
            {
                EngineDebug.LogWarning(
                    $"[PathFind] realtime-stuck-skip unit={_actionState.ActionStateMachine.CurUnit.gameObject.name} self={selfPos} newNext={pointDatas[^1].pos}");
                mStuckAccumTime = 0f;
                return;
            }

            PointData _Target = TargetPos.value(_actionState, EngineResourcesManager.Instance.MachineTime);
            if (_Target.pos.sqrMagnitude < ZeroTargetSqrThreshold && mCachedValidTargetPos.sqrMagnitude >= ZeroTargetSqrThreshold)
                _Target = new PointData(mCachedValidTargetPos, _Target.rot);
            if (TryDirectPathToTarget(pointDatas, selfPos, _Target.pos, _actionState))
            {
                mStuckAccumTime = 0f;
                return;
            }

            _actionState.ActionStateMachine.TryGetStaticLogic(out Ex_NavMesh navMesh, nameof(Ex_NavMesh));
            if (navMesh.TryGetAggressiveEscapePoint(selfPos, nextWp, _Target.pos, NavMesh.AllAreas, out Vector3 escapePos))
            {
                Vector3 lookDir = nextWp - escapePos;
                if (lookDir.sqrMagnitude < 0.0001f)
                    lookDir = _Target.pos - escapePos;
                if (lookDir.sqrMagnitude < 0.0001f)
                    lookDir = Vector3.forward;

                pointDatas.Add(new PointData(escapePos, Quaternion.LookRotation(lookDir)));
                EngineDebug.LogWarning(
                    $"[PathFind] realtime-stuck-escape unit={_actionState.ActionStateMachine.CurUnit.gameObject.name} self={selfPos} escape={escapePos} nextWp={nextWp}");
            }

            mStuckAccumTime = 0f;
        }

        private void ResetStuckTracking(Vector3 selfPos)
        {
            mLastFramePos = selfPos;
            mStuckAccumTime = 0f;
            mMoveAccumTime = 0f;
        }

        /// <summary> 退出寻路：停止移动 </summary>
        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            //if (!mIsValid) return;
            ReleasePathQueryOwner();
            ResetRepathUpgradeState();
            StopMove(_actionState);
        }

        /// <summary> 停止移动：根据UesType清除输入或速度 </summary>
        private void StopMove(ActionStatePart _actionState)
        {
            if (UesType == 0)
            {
                ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
                _stateMachine.SetMoveInputStop();
            }
            else if (UesType == 1)
            {
                mGVelocity.SetValue(_actionState, new PointData(Vector3.zero, Quaternion.LookRotation(Vector3.zero)));
            }
        }

        /// <summary> 编辑器绘制：在场景中绘制路径点与连线 </summary>
        public void EditorDraw(CharacterConfig characterConfig, ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (IsDraw)
            {
                Color _drawColor = Color.white;
                if (!Application.isPlaying) return;
                if (mIsFinish) return;
                List<PointData> pointDatas = mPathPoints.GetValue(_actionState);
                Transform _mainTrans = _actionState.ActionStateMachine.CurUnit.transform;
                Vector3 pos = _mainTrans.position;
                if (pointDatas.Count < 1)
                {
                    EngineScenceDraw.Text(pos, $"未找到寻路网格({_mainTrans.name})", 20, Color.red);
                    return;
                }
                Vector3 _lastPos = pointDatas[0].pos;
                foreach (PointData item in pointDatas)
                {
                    EngineScenceDraw.Sphere(item.pos, item.rot, Radius, _drawColor);
                    EngineScenceDraw.Line(_lastPos, item.pos, _drawColor);
                    _lastPos = item.pos;
                }
                EngineScenceDraw.Line(_lastPos, pos, _drawColor);
            }
        }

        /// <summary> 按UesType执行移动：0模拟输入/1写GValue/2Translate/3CharacterController.Move </summary>
        private void AutoMove(Vector3 _velocity, ActionStatePart _part, float deltaTime)
        {
            LogPathMoveDiag(_part);
            // 玩家卡死时不关闭 L5：玩家被怪 body 顶住时正需要强侧让绕开，关掉只会直撞；怪物维持原逻辑（卡死时走 L4 逃逸）。
            if (ExternalMoveAvoidance != null && (!mIsStuckEscaping || IsPlayerUnit(_part.ActionStateMachine)))
            {
                Transform selfTf = _part.ActionStateMachine.CurUnit.transform;
                _velocity = ExternalMoveAvoidance(selfTf, _velocity);
            }

            ActionStateMachine _StateMachine = _part.ActionStateMachine;
            if (UesType == 0)
            {
                float _speed = mGSpeed.GetValue(_part);
                Vector3 _moveDir = _velocity.normalized * _speed;
                _StateMachine.SetMoveInput(_moveDir, _moveDir);
            }
            else if (UesType == 1)
            {
                mGVelocity.SetValue(_part, new PointData(_velocity, Quaternion.LookRotation(_velocity)));
            }
            else if (UesType == 2)
            {
                float _speed = mGSpeed.GetValue(_part);
                Transform transform = _StateMachine.CurUnit.transform;
                transform.Translate(_velocity.normalized * _speed * deltaTime);  // 无视碰撞的位移
            }
            else if (UesType == 3)
            {
                float _speed = mGSpeed.GetValue(_part);
                _StateMachine.TryGetLogic(out Ex_Update_CharacterControl _move, nameof(Ex_Update_CharacterControl));
                _move.CharacterVelocity += _velocity * _speed;  // 累加到CharacterController速度
            }
        }

        /// <summary> 调用Ex_NavMesh计算路径并写入mPathPoints </summary>
        private bool UpdatePathData(PointData _s, PointData _e, ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            _stateMachine.TryGetStaticLogic(out Ex_NavMesh _value, nameof(Ex_NavMesh));

            // POE2 Hybrid L0：满足条件时用流场宏观点替换寻路终点，仍走 Ex_NavMesh 做局部 corner/clearance。
            // 判定与降级链由委托实现内部完成；返回 null 即回退原终点（现逻辑，零回归）。
            // 仅对「非玩家」单位启用：流场以玩家为中心且指向玩家，套到玩家自身会把其移动终点「指回自己」而持续抖动。
            PointData _end = _e;
            mUsingCrowdMacro = false;
            bool isPlayer = IsPlayerUnit(_stateMachine);
            if (ExternalCrowdMacroTarget != null && !isPlayer)
            {
                Transform selfTf = _stateMachine.CurUnit != null ? _stateMachine.CurUnit.transform : null;
                if (selfTf != null)
                {
                    CrowdMacroResult? macro = ExternalCrowdMacroTarget(selfTf, _e.pos);
                    byte movementInterrupt = 0;
                    if (macro.HasValue)
                    {
                        _end = new PointData(macro.Value.Pos, _e.rot);
                        // slot 真终点(AllowContinue=false)不置 mUsingCrowdMacro → 不触发下方「接近即续路」，让怪正常到点停下。
                        mUsingCrowdMacro = macro.Value.AllowContinue;
                        movementInterrupt = macro.Value.MovementInterrupt;
                    }
                    // 到位且超攻击范围的怪(interrupt=1)标记「无法位移」停在 slot 待命；其余情况写 0 复原，避免上次的 1 残留。
                    mFindPathInterrput?.Set(_actionState, movementInterrupt);
                }
            }
            else if (EnablePlayerCrowdAvoid && ExternalPlayerCrowdMacroTarget != null && isPlayer)
            {
                // 玩家软代价绕怪：终点仍是点击点，只有当直线被怪挡、流场给出宏观点时才临时改写为宏观点。
                Vector3? macro = ExternalPlayerCrowdMacroTarget(_s.pos, _e.pos);
                if (macro.HasValue)
                {
                    _end = new PointData(macro.Value, _e.rot);
                    mUsingCrowdMacro = true;
                }

                float nowPc = Time.time;
                if (nowPc - sLastPlayerCrowdLogTime >= PlayerCrowdLogInterval)
                {
                    sLastPlayerCrowdLogTime = nowPc;
                    EngineDebug.LogWarning(
                        $"[PlayerCrowd] provider self={_s.pos} goal={_e.pos} macro={(macro.HasValue ? macro.Value.ToString() : "null")} using={mUsingCrowdMacro}");
                }
            }

            if (!_value.SetPath(mPathPoints, _s, _end))
            {
                return false;
            }

            return mPathPoints.GetValue(_actionState).Count > 0;
        }

        /// <summary> 当前寻路单位是否为玩家：玩家不参与 L0 群体流场（否则会被自身流场指回而抖动）。 </summary>
        private static bool IsPlayerUnit(ActionStateMachine _stateMachine)
        {
            if (_stateMachine == null)
                return false;
            var input = ActionEngineManager_Input.Instance;
            return input != null && input.IsPlayer(_stateMachine.CurUnit);
        }

        /// DELETE-ME：#7/#8 诊断。AutoMove 是「寻路真正驱动怪移动」的唯一出口——它仍为某只近距怪触发，
        /// 就说明寻路还在推它（此时若 provider 到位返回 null 会导致重新往玩家冲）。按单位节流、仅近距怪、可关。
        private static void LogPathMoveDiag(ActionStatePart _part)
        {
            if (!EnablePathMoveDiag)
                return;
            var sm = _part != null ? _part.ActionStateMachine : null;
            if (sm == null || sm.CurUnit == null || IsPlayerUnit(sm))
                return;
            var input = ActionEngineManager_Input.Instance;
            var player = input != null ? input.Player : null;
            if (player == null)
                return;

            float dist = Vector3.Distance(sm.CurUnit.transform.position, player.transform.position);
            if (dist > PathMoveDiagNearDist)
                return;

            ulong id = NavigationStableUnitIdentity.Require(sm.CurUnit);
            float now = Time.time;
            if (s_pathMoveDiagTime.TryGetValue(id, out float last) && now - last < PathMoveDiagInterval)
                return;
            s_pathMoveDiagTime[id] = now;
            EngineDebug.LogWarning(
                $"[PathMove] unit={sm.CurUnit.gameObject.name} distToPlayer={dist:F2} 寻路仍在驱动移动（近距）");
        }

        private void TryUpgradeRepeatedSolution(ActionStatePart _actionState, Vector3 selfPos, Vector3 targetPos)
        {
            List<PointData> pointDatas = mPathPoints.GetValue(_actionState);
            if (pointDatas.Count < 1)
            {
                ResetRepathUpgradeState();
                return;
            }

            Vector3 nextWp = pointDatas[^1].pos;
            if (!mHasRepathSnapshot)
            {
                RecordRepathSnapshot(selfPos, _actionState);
                mRepeatAnchorSelfPos = selfPos;
                mRepeatStartTime = Time.time;
                return;
            }

            bool sameWaypoint = (nextWp - mLastRepathNextWp).sqrMagnitude <= SameWaypointThreshold * SameWaypointThreshold;
            bool noProgress = (selfPos - mRepeatAnchorSelfPos).sqrMagnitude <= NoProgressMoveThreshold * NoProgressMoveThreshold;
            if (!sameWaypoint || !noProgress)
            {
                mSameSolutionStuckCount = 0;
                RecordRepathSnapshot(selfPos, _actionState);
                mRepeatAnchorSelfPos = selfPos;
                mRepeatStartTime = Time.time;
                return;
            }

            mSameSolutionStuckCount++;
            mLastRepathNextWp = nextWp;
            float nowRs = Time.time;
            float repeatLogGap = EnableVerbosePathFindLog ? RepeatedSolutionVerboseLogInterval : RepeatedSolutionLogInterval;
            if (nowRs - mLastRepeatedSolutionLogTime >= repeatLogGap)
            {
                mLastRepeatedSolutionLogTime = nowRs;
                float elapsedPreview = nowRs - mRepeatStartTime;
                EngineDebug.LogWarning(
                    $"[PathFind] repeated-solution build unit={_actionState.ActionStateMachine.CurUnit.gameObject.name} count={mSameSolutionStuckCount} elapsed={elapsedPreview:F2}s self={selfPos} nextWp={nextWp} target={targetPos} (同路点+无进展，易与摇摆/反复重算相关)");
            }

            float elapsed = Time.time - mRepeatStartTime;
            if (mSameSolutionStuckCount < SameSolutionUpgradeThreshold || elapsed < RepeatMinElapsedTime)
            {
                return;
            }

            _actionState.ActionStateMachine.TryGetStaticLogic(out Ex_NavMesh _value, nameof(Ex_NavMesh));

            if (TryDirectPathToTarget(pointDatas, selfPos, targetPos, _actionState))
            {
                mSameSolutionStuckCount = 0;
                mHasRepathSnapshot = true;
                mLastRepathSelfPos = selfPos;
                mLastRepathNextWp = pointDatas[^1].pos;
                ResetStuckTracking(selfPos);
                return;
            }

            if (TrySkipToReachableWaypoint(pointDatas, selfPos, _actionState))
            {
                mSameSolutionStuckCount = 0;
                mHasRepathSnapshot = true;
                mLastRepathSelfPos = selfPos;
                mLastRepathNextWp = pointDatas[^1].pos;
                ResetStuckTracking(selfPos);
                EngineDebug.LogWarning(
                    $"[PathFind] skip to reachable wp unit={_actionState.ActionStateMachine.CurUnit.gameObject.name} self={selfPos} newNext={pointDatas[^1].pos} target={targetPos}");
                return;
            }

            if (_value.TryGetAggressiveEscapePoint(selfPos, nextWp, targetPos, NavMesh.AllAreas, out Vector3 escapePos))
            {
                Vector3 lookDir = nextWp - escapePos;
                if (lookDir.sqrMagnitude < 0.0001f)
                    lookDir = targetPos - escapePos;
                if (lookDir.sqrMagnitude < 0.0001f)
                    lookDir = Vector3.forward;

                pointDatas.Add(new PointData(escapePos, Quaternion.LookRotation(lookDir)));
                mUpdateIntervalLast = Mathf.Max(EscapeRepathCooldown, mUpdateInterval);
                EngineDebug.LogWarning(
                    $"[PathFind] aggressive escape injected unit={_actionState.ActionStateMachine.CurUnit.gameObject.name} self={selfPos} oldNext={nextWp} escape={escapePos} target={targetPos} pathCount={pointDatas.Count}");
#if UNITY_EDITOR
                EngineDebug.DrawSphere(escapePos, Radius, Color.cyan, 2.0f);
                EngineDebug.DrawLine(selfPos, escapePos, Color.cyan, 2.0f);
#endif
                mSameSolutionStuckCount = 0;
                mHasRepathSnapshot = true;
                mLastRepathSelfPos = selfPos;
                mLastRepathNextWp = escapePos;
                ResetStuckTracking(selfPos);
                return;
            }

            float nowFail = Time.time;
            if (nowFail - mLastAggressiveEscapeFailLogTime >= AggressiveEscapeFailLogInterval)
            {
                mLastAggressiveEscapeFailLogTime = nowFail;
                EngineDebug.LogError(
                    $"[PathFind] aggressive escape failed unit={_actionState.ActionStateMachine.CurUnit.gameObject.name} self={selfPos} nextWp={nextWp} target={targetPos}");
            }
            RecordRepathSnapshot(selfPos, _actionState);
        }

        private bool TryDirectPathToTarget(List<PointData> pointDatas, Vector3 selfPos, Vector3 targetPos, ActionStatePart _actionState)
        {
            float distToTarget = Vector3.Distance(selfPos, targetPos);
            if (distToTarget > StuckDirectTargetThreshold)
                return false;

            if (!SampleNavigationPosition(
                    selfPos,
                    out NavMeshHit selfHit,
                    0.5f,
                    NavMesh.AllAreas))
                return false;

            _actionState.ActionStateMachine.TryGetStaticLogic(out Ex_NavMesh navMesh, nameof(Ex_NavMesh));
            if (!navMesh.IsSegmentReachableForAgent(selfHit.position, targetPos, NavMesh.AllAreas))
                return false;

            int keepCount = 1;
            if (pointDatas.Count > keepCount)
            {
                pointDatas.RemoveRange(keepCount, pointDatas.Count - keepCount);
            }
            if (EnableVerbosePathFindLog)
            {
                EngineDebug.Log(
                    $"[PathFind] direct path to target unit={_actionState.ActionStateMachine.CurUnit.gameObject.name} self={selfPos} target={targetPos}");
            }
            return true;
        }

        private bool TrySkipToReachableWaypoint(List<PointData> pointDatas, Vector3 selfPos, ActionStatePart _actionState)
        {
            if (pointDatas.Count < 2)
                return false;

            if (!SampleNavigationPosition(
                    selfPos,
                    out NavMeshHit selfHit,
                    0.5f,
                    NavMesh.AllAreas))
                return false;

            _actionState.ActionStateMachine.TryGetStaticLogic(out Ex_NavMesh navMesh, nameof(Ex_NavMesh));
            int lastIdx = pointDatas.Count - 1;
            for (int i = lastIdx - 1; i >= 0; i--)
            {
                if (navMesh.IsSegmentReachableForAgent(selfHit.position, pointDatas[i].pos, NavMesh.AllAreas))
                {
                    int removeCount = lastIdx - i;
                    if (removeCount > 0)
                    {
                        pointDatas.RemoveRange(i + 1, removeCount);
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool SampleNavigationPosition(
            Vector3 position,
            out NavMeshHit hit,
            float maxDistance,
            int areaMask)
        {
            return NavigationQueryApiRuntime.SamplePosition(
                position,
                out hit,
                maxDistance,
                areaMask);
        }

        private void RecordRepathSnapshot(Vector3 selfPos, ActionStatePart _actionState)
        {
            List<PointData> pointDatas = mPathPoints.GetValue(_actionState);
            if (pointDatas.Count < 1)
            {
                ResetRepathUpgradeState();
                return;
            }

            mHasRepathSnapshot = true;
            mLastRepathSelfPos = selfPos;
            mLastRepathNextWp = pointDatas[^1].pos;
        }

        private void ResetRepathUpgradeState()
        {
            mHasRepathSnapshot = false;
            mLastRepathSelfPos = Vector3.zero;
            mLastRepathNextWp = Vector3.zero;
            mSameSolutionStuckCount = 0;
            mRepeatStartTime = 0f;
            mRepeatAnchorSelfPos = Vector3.zero;
        }

        /// <summary> 深拷贝事件配置 </summary>
        public IActionEventData Clone(IActionEventData _eventData)
        {
            if (_eventData == null)
                throw new ArgumentNullException(nameof(_eventData));
            Event_PathFind _event = _eventData as Event_PathFind;
            if (_event == null)
                throw new ArgumentException(
                    "Clone target must be an Event_PathFind.", nameof(_eventData));

            _event.UpdateInterval = mUpdateInterval;
            _event.PathPoints = (GGroupPoint)mPathPoints.Clone();
            _event.StartPos = mStartPos.Clone();
            _event.TargetPos = mTargetPos.Clone();
            _event.Radius = mRadius;
            _event.UesType = mUesType;
            _event.ChangeActionToFinish = mChangeActionToFinish;
            _event.SetGvalueToFinish = mSetGvalueToFinish;

            _event.GvalueSetting = (mGvalueSetting ?? new GValue_Setting()).Clone();
            _event.ActionID = mActionID;
            _event.GSpeed = (GFloat)mGSpeed.Clone();
            _event.GVelocity = (GPoint)mGVelocity.Clone();
            _event.FindPathInterrput = (mFindPathInterrput ?? new GValue_SetEnum()).Clone();
            _event.SoftRadius = SoftRadius;
            _event.IsDraw = mIsDraw;

            return _event;
        }
    }
}
