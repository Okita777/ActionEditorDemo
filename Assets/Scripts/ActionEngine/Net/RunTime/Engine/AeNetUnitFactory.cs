using System;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEngine;

namespace AsiTimeLine.Net
{
    /// <summary>
    /// 联网单位的唯一建体入口。三种形态对应 <see cref="SimulationAuthority"/> 三态，
    /// 全部走 <c>ActionEngineManager_Unit.CreateIdentifiedUnit</c>，禁止绕过引擎的对象池与稳定身份绑定。
    ///
    /// channel 由本类显式传入而非依赖端角色隐式推导：<c>Server</c> 角色下
    /// <c>ResolveRuntimeChannel</c> 会全局强制提升为 Server，而 <c>Host</c> 下不覆盖，
    /// 两者的创建路径并不一致。
    /// </summary>
    public static class AeNetUnitFactory
    {
        /// <summary>
        /// 本地全模拟单位：跑全套输入 / 打断 / RootMotion / CC。
        /// <paramref name="registerAsPlayer"/> 为 true 时注册为输入操作对象（owner 玩家）；
        /// 单机退化路径下的 NPC 同样需要本形态但不接输入，故留作参数而非硬编码。
        /// </summary>
        public static void CreateLocalPredict(
            int unitWarpId,
            ulong stableUnitId,
            Vector3 position,
            Quaternion rotation,
            Action<ActionEngine_Unit> onCreated,
            bool registerAsPlayer = true)
        {
            CreateCore(
                unitWarpId,
                stableUnitId,
                ERuntimeDataChannel.Local,
                SimulationAuthority.LocalPredict,
                position,
                rotation,
                unit =>
                {
                    if (unit != null && registerAsPlayer)
                    {
                        ActionEngineManager_Input.Instance.ChangePlayer(unit);
                    }
                    onCreated?.Invoke(unit);
                });
        }

        /// <summary>服务端权威单位：headless（无模型 / 无 Animator），跑输入 / 打断 / 命中 / GValue。</summary>
        public static void CreateServerAuthoritative(
            int unitWarpId,
            ulong stableUnitId,
            Vector3 position,
            Quaternion rotation,
            Action<ActionEngine_Unit> onCreated)
        {
            CreateCore(
                unitWarpId,
                stableUnitId,
                ERuntimeDataChannel.Server,
                SimulationAuthority.ServerAuthoritative,
                position,
                rotation,
                onCreated);
        }

        /// <summary>客户端上的远端表现体：需要模型与 Animator，被动接受动作与位姿快照。</summary>
        public static void CreateRemoteProxy(
            int unitWarpId,
            ulong stableUnitId,
            Vector3 position,
            Quaternion rotation,
            Action<ActionEngine_Unit> onCreated)
        {
            CreateCore(
                unitWarpId,
                stableUnitId,
                ERuntimeDataChannel.Local,
                SimulationAuthority.RemoteProxy,
                position,
                rotation,
                onCreated);
        }

        private static void CreateCore(
            int unitWarpId,
            ulong stableUnitId,
            ERuntimeDataChannel channel,
            SimulationAuthority authority,
            Vector3 position,
            Quaternion rotation,
            Action<ActionEngine_Unit> onCreated)
        {
            if (unitWarpId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unitWarpId),
                    $"AeNet unit requires a positive UnitWarpID, got {unitWarpId}.");
            }

            ActionEngineManager_Unit.Instance.CreateIdentifiedUnit(
                unitWarpId,
                stableUnitId,
                target =>
                {
                    ActionEngine_Unit unit = target?.GetUnit();
                    if (unit == null)
                    {
                        EngineDebug.LogError(
                            $"[AeNetUnitFactory] 建体失败 unitWarpId=[{unitWarpId}] channel=[{channel}] authority=[{authority}]");
                        onCreated?.Invoke(null);
                        return;
                    }

                    // Authority 必须在 InitState 播首个动作之前写入，否则首帧走错 gate
                    unit.ActionStateMachine.Authority = authority;
                    // AeNet 的服务端单位完全承担位姿权威，须自行跑逻辑位移与旋转轨；
                    // 既有 DS 路径不开此开关，位姿由客户端上报驱动
                    unit.ActionStateMachine.ServerLogicSimulationEnabled =
                        authority == SimulationAuthority.ServerAuthoritative;
                    unit.transform.SetPositionAndRotation(position, rotation);
                    // 三个朝向源都以出生朝向起步：服务端单位在第一条意图到达前若停留在 identity，
                    // 转身事件会先把它拧到世界 Z+ 再拧回来
                    unit.ActionStateMachine.SetMouseXY(rotation);
                    unit.ActionStateMachine.GetCharacterFor = rotation;
                    unit.ActionStateMachine.SetCamRot(
                        Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));

                    onCreated?.Invoke(unit);
                },
                EUnitType.Entity,
                channel);
        }
    }
}
