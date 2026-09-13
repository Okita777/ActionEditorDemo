using System.Collections.Generic;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;

namespace AsiTimeLine.Net
{
    /// <summary>
    /// 联网单位的网络行为。玩家与 NPC 共用本脚本，靠 spawn 数据里的角色分支。
    ///
    /// NetworkObject 本身是空壳，只承载身份与位姿；真实表现体是各端自建的 <see cref="ActionEngine_Unit"/>。
    /// 服务端侧与客户端侧的单位是两个独立字段：Host 进程会同时持有两者
    /// （headless 跑权威逻辑 + 表现体做本地预测/远端表现），这也是 Host 不需要任何特判分支的原因。
    ///
    /// 玩家与 NPC 的服务端路径完全一致，唯一差别是玩家的状态机由上行意图驱动。
    /// </summary>
    public sealed class AeNetUnit : NetworkBehaviour
    {
        /// <summary>
        /// 预测偏差小于此值不校正。对账虽然按 tick 对齐，仍有两项固有偏差：
        /// owner 记录的是「应用该 tick 输入之前」的位置而服务端算的是之后，差一个 tick 的位移；
        /// 以及 owner 可变 dt 与服务端固定步长的积分差异。阈值需要能容纳这两项。
        /// </summary>
        private const float c_PoseIgnoreDistance = 0.3f;

        /// <summary>预测偏差超过此值直接一次性补偿。到这个量级说明预测已经失效，继续平滑只会让偏差持续可见。</summary>
        private const float c_PoseSnapDistance = 2.5f;

        /// <summary>中间区间每帧消化待补偿误差的比例。</summary>
        private const float c_PoseBlendRate = 0.2f;

        /// <summary>误差消化到此值以下即视为归零，避免浮点残差让偏移量永远消不完。</summary>
        private const float c_PoseCorrectionEpsilon = 0.001f;

        /// <summary>预测位置历史容量。30Hz 下约 2 秒，足以覆盖任何可接受的 RTT。</summary>
        private const int c_PoseHistoryCapacity = 64;

        private readonly SyncVar<AeNetUnitSpawnData> mSpawn =
            new SyncVar<AeNetUnitSpawnData>();

        /// <summary>
        /// 各动画层的最新动作快照，按 AnimaLayer 索引。
        /// 实时广播走 <see cref="ObserversApplyAction"/>，本列表只负责给"建体尚未完成"和
        /// "中途加入"的观察者补齐当前分层状态，因此客户端不订阅它的 OnChange，避免与实时通道重复播放。
        /// </summary>
        private readonly SyncList<AeNetActionData> mLayerActions =
            new SyncList<AeNetActionData>();

        private readonly AeNetIntentQueue mIntentQueue = new AeNetIntentQueue();
        private readonly AeNetInputCollector mInputCollector = new AeNetInputCollector();

        private readonly AeNetGValueSync mServerGValueSync = new AeNetGValueSync();
        private readonly AeNetGValueSync mClientGValueSync = new AeNetGValueSync();

        /// <summary>
        /// 服务端单位建好之前到达的快照请求。客户端建体与服务端建体都是异步的，
        /// 谁先完成不确定；请求先到就必须留着，否则该客户端会永远停在空快照上，
        /// 只能靠后续增量补，而增量补不出那些从未再改过的初始值。
        /// </summary>
        private readonly List<NetworkConnection> mPendingSnapshotRequests =
            new List<NetworkConnection>();

        private ActionEngine_Unit mServerUnit;
        private ActionEngine_Unit mClientUnit;
        private bool mClientUnitReady;
        private bool mIntentPumpRegistered;
        private bool mUnitPumpRegistered;
        private bool mTickSubscribed;
        private bool mOwnerActionSubscribed;

        /// <summary>
        /// 是否已允许应用下行 GV 增量。服务端权威下要等全量快照落地才打开：
        /// 快照之前到达的增量描述的是本端还没有基线的状态，应用它们只会拼出一份残缺值。
        /// </summary>
        private bool mGValueGateOpen;

        /// <summary>客户端权威模式下 owner 上报的位姿，由服务端写入 NetworkObject 后广播。</summary>
        private Vector3 mReportedPosition;
        private Quaternion mReportedRotation;
        private bool mHasReportedPose;

        /// <summary>
        /// owner 的预测位置历史，按 tick 取模寻址。权威回包带着它被消费的客户端 tick，
        /// 只有拿同一 tick 的本地位置作差，得到的才是真实预测偏差；
        /// 拿「当前权威位置」减「当前本地位置」会把整个 RTT 计入偏差，把本地预测拉回滞后位置。
        /// </summary>
        private readonly uint[] mPoseHistoryTicks = new uint[c_PoseHistoryCapacity];
        private readonly Vector3[] mPoseHistoryPositions = new Vector3[c_PoseHistoryCapacity];
        private readonly bool[] mPoseHistoryValid = new bool[c_PoseHistoryCapacity];

        /// <summary>待消化的位置误差。按帧比例加到本地位置上，而不是把位置拉向权威值。</summary>
        private Vector3 mPendingCorrection;

        public EAeNetUnitRole UnitRole => mSpawn.Value.Role;
        public ActionEngine_Unit ServerUnit => mServerUnit;
        public ActionEngine_Unit ClientUnit => mClientUnit;

        /// <summary>本单位的模拟权威是否在服务端。两端读同一份 spawn 数据，不存在各自解释的空间。</summary>
        private bool ServerAuthoritative => mSpawn.Value.ServerAuthoritative;

        /// <summary>服务端在 <c>ServerManager.Spawn</c> 之前写入，随 spawn 下发给所有观察者。</summary>
        public void ServerWriteSpawnData(int unitWarpId, EAeNetUnitRole role, bool serverAuthoritative)
        {
            mSpawn.Value = new AeNetUnitSpawnData
            {
                UnitWarpId = unitWarpId,
                Role = role,
                ServerAuthoritative = serverAuthoritative,
            };
        }

        #region 生命周期

        public override void OnStartServer()
        {
            base.OnStartServer();

            // 两种权威模式都要桥接位姿：服务端权威时源头是 headless 单位，
            // 客户端权威时源头是 owner 上报的位姿，NetworkObject 的写入者始终只有服务端
            AeNetServerUnitPump.Register(this);
            mUnitPumpRegistered = true;

            // 客户端权威单位在服务端不建体：服务端只转发 owner 上报的位姿与动作，
            // 建一个不跑逻辑的空壳只会给 NetworkObject 多出一个位姿写入者
            if (!ServerAuthoritative) return;

            CreateServerUnit();

            if (mSpawn.Value.Role != EAeNetUnitRole.Player) return;

            AeNetServerIntentPump.Register(this);
            mIntentPumpRegistered = true;
        }

        public override void OnStopServer()
        {
            if (mIntentPumpRegistered)
            {
                AeNetServerIntentPump.Unregister(this);
                mIntentPumpRegistered = false;
            }
            if (mUnitPumpRegistered)
            {
                AeNetServerUnitPump.Unregister(this);
                mUnitPumpRegistered = false;
            }
            mIntentQueue.Clear();
            mPendingSnapshotRequests.Clear();
            ReleaseServerUnit();
            base.OnStopServer();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            CreateClientUnit();

            if (!base.IsOwner) return;

            // 两种模式都需要 tick：服务端权威时上行意图，客户端权威时上行位姿
            base.TimeManager.OnTick += OnClientTick;
            mTickSubscribed = true;
        }

        public override void OnStopClient()
        {
            if (mTickSubscribed)
            {
                // 关闭流程中 TimeManager 可能已被销毁
                if (base.TimeManager != null) base.TimeManager.OnTick -= OnClientTick;
                mTickSubscribed = false;
            }
            mInputCollector.Detach();
            ReleaseClientUnit();
            base.OnStopClient();
        }

        #endregion

        #region 建体与回收

        private void CreateServerUnit()
        {
            AeNetUnitSpawnData spawn = mSpawn.Value;
            if (spawn.UnitWarpId <= 0)
            {
                EngineDebug.LogError(
                    $"[AeNetUnit] 服务端 spawn 数据缺少 UnitWarpID objectId=[{base.ObjectId}]");
                return;
            }

            ulong stableUnitId = AeNetStableId.ForUnit(spawn.Role, true, base.ObjectId);
            AeNetUnitFactory.CreateServerAuthoritative(
                spawn.UnitWarpId,
                stableUnitId,
                transform.position,
                transform.rotation,
                OnServerUnitCreated);
        }

        private void OnServerUnitCreated(ActionEngine_Unit unit)
        {
            if (unit == null) return;

            // 建体是异步的，回调到达时对象可能已 despawn
            if (!base.IsServerInitialized)
            {
                DestroyUnit(unit);
                return;
            }

            mServerUnit = unit;
            unit.ActionStateMachine.EventSystem.OnChangeAction += OnServerUnitActionChanged;

            // 只有服务端权威单位才会走到这里建体，服务端就是它 GV 的唯一真源
            mServerGValueSync.Attach(unit, true);
            ServerFlushPendingSnapshotRequests();
        }

        private void CreateClientUnit()
        {
            AeNetUnitSpawnData spawn = mSpawn.Value;
            if (spawn.UnitWarpId <= 0)
            {
                EngineDebug.LogError(
                    $"[AeNetUnit] 客户端 spawn 数据缺少 UnitWarpID objectId=[{base.ObjectId}]");
                return;
            }

            ulong stableUnitId = AeNetStableId.ForUnit(spawn.Role, false, base.ObjectId);
            if (base.IsOwner)
            {
                AeNetUnitFactory.CreateLocalPredict(
                    spawn.UnitWarpId,
                    stableUnitId,
                    transform.position,
                    transform.rotation,
                    OnClientUnitCreated);
            }
            else
            {
                AeNetUnitFactory.CreateRemoteProxy(
                    spawn.UnitWarpId,
                    stableUnitId,
                    transform.position,
                    transform.rotation,
                    OnClientUnitCreated);
            }
        }

        private void OnClientUnitCreated(ActionEngine_Unit unit)
        {
            if (unit == null) return;

            if (!base.IsClientInitialized)
            {
                DestroyUnit(unit);
                return;
            }

            mClientUnit = unit;
            mClientUnitReady = true;

            // 收集方只有真源那一端：服务端权威时客户端（含 owner 预测体）纯接收，
            // 本地预测过程中产生的 GV 写入不上行，否则预测值会反过来污染权威值
            bool collectGValue = !ServerAuthoritative && base.IsOwner;
            mClientGValueSync.Attach(unit, collectGValue);
            RequestGValueSnapshotIfNeeded();

            if (base.IsOwner)
            {
                if (ServerAuthoritative)
                {
                    // 只挂本地预测体。服务端单位被注入意图时同样会触发输入观察事件，
                    // 挂错对象会把注入结果当作新输入再次上行
                    mInputCollector.Attach(unit);
                }
                else
                {
                    // 客户端权威：owner 的状态机就是动作真源，结果上报给服务端转发
                    unit.ActionStateMachine.EventSystem.OnChangeAction += OnOwnerUnitActionChanged;
                    mOwnerActionSubscribed = true;
                }
                return;
            }

            ReplayLayerSnapshots();
        }

        private void ReleaseServerUnit()
        {
            mServerGValueSync.Detach();
            if (mServerUnit == null) return;

            ActionEventSystem events = mServerUnit.ActionStateMachine?.EventSystem;
            if (events != null)
            {
                events.OnChangeAction -= OnServerUnitActionChanged;
            }
            DestroyUnit(mServerUnit);
            mServerUnit = null;
        }

        private void ReleaseClientUnit()
        {
            mClientUnitReady = false;
            mPendingCorrection = Vector3.zero;
            mGValueGateOpen = false;
            mClientGValueSync.Detach();
            System.Array.Clear(mPoseHistoryValid, 0, mPoseHistoryValid.Length);
            if (mClientUnit == null) return;

            if (mOwnerActionSubscribed)
            {
                ActionEventSystem ownerEvents = mClientUnit.ActionStateMachine?.EventSystem;
                if (ownerEvents != null)
                {
                    ownerEvents.OnChangeAction -= OnOwnerUnitActionChanged;
                }
                mOwnerActionSubscribed = false;
            }

            if (ReferenceEquals(ActionEngineManager_Input.Instance.Player, mClientUnit))
            {
                ActionEngineManager_Input.Instance.ChangePlayer(null);
            }
            DestroyUnit(mClientUnit);
            mClientUnit = null;
        }

        private static void DestroyUnit(ActionEngine_Unit unit)
        {
            // 应用退出阶段引擎已停机，此时只需丢引用；DestoryUnit 内部会 Clear 稳定身份绑定
            if (ActionEngineManager.Instance == null) return;

            ActionEngineManager_Unit.Instance.DestoryUnit(unit);
        }

        #endregion

        #region Owner 上行

        private void OnClientTick()
        {
            if (!base.IsOwner) return;

            if (ServerAuthoritative)
            {
                if (!mInputCollector.IsAttached) return;

                uint tick = base.TimeManager.LocalTick;
                ServerSubmitIntent(mInputCollector.Collect(tick));
                RecordPredictedPose(tick);
                return;
            }

            if (mClientUnit == null) return;

            Transform source = mClientUnit.transform;
            ServerReportPose(source.position, source.rotation);

            if (mClientGValueSync.TryTakeDelta(out AeNetGValueBatch batch))
            {
                ServerRelayGValues(batch);
            }
        }

        /// <summary>
        /// 记录本 tick 上行意图时的预测位置。tick 回调发生在帧最早（FishNet 的 NetworkReaderLoop），
        /// 此时本帧引擎尚未推进，记下的是「应用该 tick 输入之前」的位置——与服务端算出的「之后」差一个 tick，
        /// 这项系统偏差由 <see cref="c_PoseIgnoreDistance"/> 容纳。
        /// </summary>
        private void RecordPredictedPose(uint tick)
        {
            if (mClientUnit == null) return;

            int slot = (int)(tick % c_PoseHistoryCapacity);
            mPoseHistoryTicks[slot] = tick;
            mPoseHistoryPositions[slot] = mClientUnit.transform.position;
            mPoseHistoryValid[slot] = true;
        }

        private bool TryGetPredictedPose(uint tick, out Vector3 position)
        {
            int slot = (int)(tick % c_PoseHistoryCapacity);
            if (!mPoseHistoryValid[slot] || mPoseHistoryTicks[slot] != tick)
            {
                position = default;
                return false;
            }

            position = mPoseHistoryPositions[slot];
            return true;
        }

        /// <summary>
        /// 走可靠通道：按键边沿是一次性事件，丢一个就是玩家按了技能没反应。
        /// 移动与视角虽然每 tick 重发、丢了可自愈，但与按键同在一个包里，不单独降级。
        /// </summary>
        [ServerRpc]
        private void ServerSubmitIntent(AeNetIntentData intent)
        {
            // 客户端权威模式下服务端没有该单位的状态机，收到意图只能是模式不一致
            if (!ServerAuthoritative) return;

            mIntentQueue.Enqueue(intent);
        }

        /// <summary>
        /// 客户端权威模式下 owner 每 tick 上报位姿。走不可靠通道：位姿是全量状态，
        /// 丢包由下一 tick 自愈，排队重传只会让位置追着历史走。
        /// </summary>
        [ServerRpc]
        private void ServerReportPose(
            Vector3 position,
            Quaternion rotation,
            Channel channel = Channel.Unreliable)
        {
            // 服务端权威模式下位姿只能由服务端状态机产出，客户端上报一律拒绝
            if (ServerAuthoritative) return;

            mReportedPosition = position;
            mReportedRotation = rotation;
            mHasReportedPose = true;
        }

        /// <summary>
        /// 由 <see cref="AeNetServerIntentPump"/> 在每个引擎步之前调用，注入本步意图。
        /// 注入只落输入，动作由服务端自己的打断系统判定后产出。
        /// </summary>
        public void ServerApplyIntentStep()
        {
            if (mServerUnit == null) return;

            mIntentQueue.ApplyTick(mServerUnit.ActionStateMachine);
        }

        #endregion

        #region 动作同步

        private void OnServerUnitActionChanged(
            ActionEngine_Unit unit,
            int actionId,
            int mixTime,
            int offsetTime)
        {
            ServerBroadcastAction(BuildActionData(unit, actionId, mixTime, offsetTime));
        }

        /// <summary>客户端权威模式下，owner 的本地状态机产出动作后上报服务端转发。</summary>
        private void OnOwnerUnitActionChanged(
            ActionEngine_Unit unit,
            int actionId,
            int mixTime,
            int offsetTime)
        {
            ServerRelayAction(BuildActionData(unit, actionId, mixTime, offsetTime));
        }

        [ServerRpc]
        private void ServerRelayAction(AeNetActionData data)
        {
            // 服务端权威模式下动作只能由服务端自己的打断系统产出，客户端上报一律拒绝
            if (ServerAuthoritative) return;

            ServerBroadcastAction(data);
        }

        /// <summary>
        /// 不排除 owner：owner 的本地预测结果需要与权威结果对账，
        /// 排除掉就等于 owner 永远收不到自己的权威动作，预测偏差无法收敛。
        /// </summary>
        [ObserversRpc]
        private void ObserversApplyAction(AeNetActionData data)
        {
            ApplyActionToClientUnit(data);
        }

        private void ServerBroadcastAction(AeNetActionData data)
        {
            WriteLayerSnapshot(data);
            ObserversApplyAction(data);
        }

        /// <summary>
        /// 按 AnimaLayer 写入分层快照。单槽位 SyncVar 会让上层动作在同帧覆盖主层 idle/walk，
        /// 导致中途加入的观察者卡在错误动作，因此这里按层各存一份。
        /// </summary>
        private void WriteLayerSnapshot(AeNetActionData data)
        {
            if (data.AnimaLayer < 0) return;

            while (mLayerActions.Count <= data.AnimaLayer)
            {
                mLayerActions.Add(default);
            }
            mLayerActions.Set(data.AnimaLayer, data, false);
        }

        private void ApplyActionToClientUnit(AeNetActionData data)
        {
            // 建体尚未完成时丢弃：完成后会用 mLayerActions 一次性补齐各层当前状态
            if (!mClientUnitReady || mClientUnit == null) return;
            if (data.ActionId <= 0) return;

            if (base.IsOwner)
            {
                // 客户端权威：owner 是动作的产出方，下行只是自己的回声
                if (!ServerAuthoritative) return;

                // 预测正确时不重播：权威回包与本地预测同一动作，重播会把已进行的动作切回起点
                if (IsLocalActionMatching(data)) return;
            }

            mClientUnit.ActionStateMachine.ChangeAction(
                data.ActionId,
                data.MixTime,
                data.OffsetTime);
        }

        private bool IsLocalActionMatching(in AeNetActionData data)
        {
            return mClientUnit.ActionStateMachine.TryGetCurActionStateInfo(
                       data.AnimaLayer,
                       out int currentActionId)
                   && currentActionId == data.ActionId;
        }

        private void ReplayLayerSnapshots()
        {
            List<AeNetActionData> snapshots = mLayerActions.Collection;
            if (snapshots == null) return;

            for (int i = 0; i < snapshots.Count; i++)
            {
                AeNetActionData data = snapshots[i];
                if (data.ActionId <= 0) continue;

                mClientUnit.ActionStateMachine.ChangeAction(
                    data.ActionId,
                    data.MixTime,
                    data.OffsetTime);
            }
        }

        private static AeNetActionData BuildActionData(
            ActionEngine_Unit unit,
            int actionId,
            int mixTime,
            int offsetTime)
        {
            int animaLayer = 0;
            ActionStateMachine machine = unit?.ActionStateMachine;
            if (machine != null &&
                machine.TryGetActionState(actionId, out ActionState state) &&
                state != null)
            {
                animaLayer = state.AnimaLayer;
            }

            return new AeNetActionData
            {
                ActionId = actionId,
                MixTime = mixTime,
                OffsetTime = offsetTime,
                AnimaLayer = animaLayer,
            };
        }

        #endregion

        #region GValue 同步

        /// <summary>
        /// 由 <see cref="AeNetServerUnitPump"/> 在引擎步之后调用，把本 tick 的 GV 变更广播出去。
        /// 攒到 tick 末尾统一发而不是写一次发一包：一个动作事件里连改数条 GV 是常态。
        /// </summary>
        public void ServerFlushGValueDelta()
        {
            if (!base.IsSpawned) return;
            if (!mServerGValueSync.TryTakeDelta(out AeNetGValueBatch batch)) return;

            ObserversApplyGValues(batch);
        }

        /// <summary>
        /// 不排除 owner：服务端权威下 owner 的预测体同样以服务端结果为准，
        /// 排除掉它等于让 owner 的 GV 永远停在自己的预测值上。
        /// </summary>
        [ObserversRpc]
        private void ObserversApplyGValues(AeNetGValueBatch batch)
        {
            // 快照尚未落地时丢弃：快照本身携带的就是最新值，先应用增量只会被随后的快照覆盖
            if (!mGValueGateOpen) return;

            mClientGValueSync.ApplyBatch(batch);
        }

        [ServerRpc]
        private void ServerRelayGValues(AeNetGValueBatch batch)
        {
            // 服务端权威下 GV 只能由服务端状态机产出，客户端上报一律拒绝
            if (ServerAuthoritative) return;

            ObserversApplyGValues(batch);
        }

        /// <summary>
        /// 建体完成后拉一份全量基线。由客户端主动请求而不是服务端在 spawn 时推送：
        /// 两端建体都是异步的，spawn 那一刻收方往往还没有状态机可写。
        /// </summary>
        private void RequestGValueSnapshotIfNeeded()
        {
            // 客户端权威单位的真源是 owner 自己的状态机，服务端不持有可供快照的池。
            // 中途加入的观察者只能从下一次变更开始跟上
            if (!ServerAuthoritative)
            {
                mGValueGateOpen = true;
                return;
            }

            ServerRequestGValueSnapshot();
        }

        /// <summary>
        /// 非 owner 的观察者同样需要基线，因此不要求所有权。
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void ServerRequestGValueSnapshot(NetworkConnection sender = null)
        {
            if (!ServerAuthoritative) return;
            if (sender == null || !sender.IsValid) return;

            if (!mServerGValueSync.IsAttached)
            {
                mPendingSnapshotRequests.Add(sender);
                return;
            }

            ServerSendSnapshotTo(sender);
        }

        private void ServerFlushPendingSnapshotRequests()
        {
            for (int i = 0; i < mPendingSnapshotRequests.Count; i++)
            {
                NetworkConnection conn = mPendingSnapshotRequests[i];
                if (conn == null || !conn.IsValid) continue;

                ServerSendSnapshotTo(conn);
            }
            mPendingSnapshotRequests.Clear();
        }

        private void ServerSendSnapshotTo(NetworkConnection conn)
        {
            // 池为空时也要回：请求方靠这个回包开闸，不回它就永远收不到后续增量
            mServerGValueSync.TryBuildSnapshot(out AeNetGValueBatch batch);
            TargetApplyGValueSnapshot(conn, batch);
        }

        [TargetRpc]
        private void TargetApplyGValueSnapshot(NetworkConnection conn, AeNetGValueBatch batch)
        {
            mGValueGateOpen = true;
            mClientGValueSync.ApplyBatch(batch);
        }

        #endregion

        #region 位姿桥接

        /// <summary>
        /// 由 <see cref="AeNetServerUnitPump"/> 在引擎步之后、NetworkTransform 采样之前调用。
        ///
        /// NetworkObject 始终只有服务端一个写入者：服务端权威时源头是 headless 单位，
        /// 客户端权威时源头是 owner 上报的位姿。两种模式共用同一个 NetworkTransform 配置，
        /// 因为 FishNet 不支持运行时切换其权威归属。
        /// </summary>
        public void ServerBridgePose()
        {
            if (!base.IsSpawned) return;

            if (mServerUnit != null)
            {
                Transform source = mServerUnit.transform;
                transform.SetPositionAndRotation(source.position, source.rotation);
                ServerSendOwnerReconcile(source.position);
                return;
            }

            if (mHasReportedPose)
            {
                transform.SetPositionAndRotation(mReportedPosition, mReportedRotation);
            }
        }

        /// <summary>
        /// 给 owner 回一份带 tick 的权威位置供对账。不复用 NetworkTransform 的 sendToOwner：
        /// 那条通道给出的是插值后的显示值且不带 tick，无法定位它对应哪一次输入。
        /// </summary>
        private void ServerSendOwnerReconcile(Vector3 position)
        {
            // 只有服务端权威玩家存在预测方需要对账：NPC 没有预测，客户端权威玩家自己就是真源
            if (mSpawn.Value.Role != EAeNetUnitRole.Player) return;
            if (!mIntentQueue.HasConsumed) return;

            NetworkConnection owner = base.Owner;
            if (!owner.IsValid) return;

            TargetApplyAuthoritativePose(owner, mIntentQueue.LastConsumedTick, position);
        }

        /// <summary>
        /// 走不可靠通道：权威位置是全量状态，丢包由下一 tick 自愈，重传只会让对账追着历史走。
        /// </summary>
        [TargetRpc]
        private void TargetApplyAuthoritativePose(
            NetworkConnection conn,
            uint consumedClientTick,
            Vector3 position,
            Channel channel = Channel.Unreliable)
        {
            if (mClientUnit == null) return;
            if (!TryGetPredictedPose(consumedClientTick, out Vector3 predicted)) return;

            Vector3 error = position - predicted;
            float distance = error.magnitude;

            if (distance <= c_PoseIgnoreDistance)
            {
                mPendingCorrection = Vector3.zero;
                return;
            }

            // 补偿的是误差增量而不是把位置设成权威值：权威值属于 consumedClientTick 那一刻，
            // 直接赋值会把角色拖回若干 tick 之前的位置
            if (distance >= c_PoseSnapDistance)
            {
                mClientUnit.transform.position += error;
                mPendingCorrection = Vector3.zero;
                return;
            }

            mPendingCorrection = error;
        }

        /// <summary>
        /// 客户端侧的表现体位姿。服务端侧的桥接不在这里做：
        /// NetworkTransform 在 tick 的 OnPostTick 采样，LateUpdate 写入永远慢一帧且采样间隔不均。
        /// </summary>
        private void LateUpdate()
        {
            if (!base.IsSpawned) return;
            if (mClientUnit == null) return;

            if (base.IsOwner)
            {
                // 客户端权威：owner 自身即位姿真源，下行只是自己的回声
                if (ServerAuthoritative) ReconcileOwnerPose();
                return;
            }

            // 远端表现体：RemoteProxy 不跑逻辑位移，直接接受权威位姿不会冲突
            mClientUnit.transform.SetPositionAndRotation(
                transform.position,
                transform.rotation);
        }

        /// <summary>
        /// 逐帧消化待补偿误差。加的是偏移量而不是把位置拉向某个权威值，
        /// 本地预测的速度感因此完全不受影响——这是「移动即时」与「结果以服务端为准」能同时成立的原因。
        ///
        /// 只处理位置，不动朝向：两端跑同一份旋转轨且输入相同，朝向不会持续发散，
        /// 强行拉回反而会让镜头与角色打架。
        /// </summary>
        private void ReconcileOwnerPose()
        {
            if (mPendingCorrection.sqrMagnitude
                <= c_PoseCorrectionEpsilon * c_PoseCorrectionEpsilon)
            {
                mPendingCorrection = Vector3.zero;
                return;
            }

            Vector3 step = mPendingCorrection * c_PoseBlendRate;
            mClientUnit.transform.position += step;
            mPendingCorrection -= step;
        }

        #endregion
    }
}
