using System.Collections.Generic;
using AsiActionEngine.RunTime;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace AsiTimeLine.Net
{
    /// <summary>
    /// 服务端侧的玩家生成器：响应客户端的 <see cref="AeNetJoinRequest"/>，
    /// 为该连接 spawn 一个归其所有的 <see cref="AeNetUnit"/> 空壳。
    /// spawn 之后服务端建 headless 单位跑权威逻辑，owner 客户端另建本地预测体。
    /// </summary>
    public sealed class AeNetPlayerSpawner
    {
        private readonly NetworkManager mNetworkManager;
        private readonly NetworkObject mPlayerPrefab;
        private readonly Transform mSpawnPoint;
        private readonly bool mServerAuthoritative;

        /// <summary>已生成玩家的连接。重复的 JoinRequest 必须幂等，否则一个客户端会拿到多个玩家体。</summary>
        private readonly HashSet<int> mSpawnedClientIds = new HashSet<int>();

        public AeNetPlayerSpawner(
            NetworkManager networkManager,
            NetworkObject playerPrefab,
            Transform spawnPoint,
            bool serverAuthoritative)
        {
            mNetworkManager = networkManager;
            mPlayerPrefab = playerPrefab;
            mSpawnPoint = spawnPoint;
            mServerAuthoritative = serverAuthoritative;
        }

        public void Register()
        {
            mNetworkManager.ServerManager.RegisterBroadcast<AeNetJoinRequest>(OnJoinRequest);
            mNetworkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        public void Unregister()
        {
            mNetworkManager.ServerManager.UnregisterBroadcast<AeNetJoinRequest>(OnJoinRequest);
            mNetworkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            mSpawnedClientIds.Clear();
        }

        private void OnJoinRequest(
            NetworkConnection conn,
            AeNetJoinRequest request,
            Channel channel)
        {
            if (request.UnitWarpId <= 0)
            {
                EngineDebug.LogError(
                    $"[AeNetPlayerSpawner] JoinRequest 缺少 UnitWarpID clientId=[{conn.ClientId}]");
                return;
            }
            if (!mSpawnedClientIds.Add(conn.ClientId))
            {
                EngineDebug.LogWarning(
                    $"[AeNetPlayerSpawner] 忽略重复 JoinRequest clientId=[{conn.ClientId}]");
                return;
            }

            Vector3 position = mSpawnPoint == null ? Vector3.zero : mSpawnPoint.position;
            Quaternion rotation = mSpawnPoint == null ? Quaternion.identity : mSpawnPoint.rotation;

            NetworkObject nob = Object.Instantiate(mPlayerPrefab, position, rotation);
            AeNetUnit netUnit = nob.GetComponent<AeNetUnit>();
            if (netUnit == null)
            {
                EngineDebug.LogError(
                    "[AeNetPlayerSpawner] 玩家 prefab 上缺少 AeNetUnit 组件");
                mSpawnedClientIds.Remove(conn.ClientId);
                Object.Destroy(nob.gameObject);
                return;
            }

            netUnit.ServerWriteSpawnData(
                request.UnitWarpId,
                EAeNetUnitRole.Player,
                mServerAuthoritative);
            mNetworkManager.ServerManager.Spawn(nob, conn);

            EngineDebug.Log(
                $"[AeNetPlayerSpawner] 已生成玩家 clientId=[{conn.ClientId}] " +
                $"unitWarpId=[{request.UnitWarpId}] 服务端权威=[{mServerAuthoritative}]");
        }

        private void OnRemoteConnectionState(
            NetworkConnection conn,
            RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Started) return;

            // FishNet 会自动 despawn 该连接拥有的对象，这里只需放开幂等锁以支持重连
            mSpawnedClientIds.Remove(conn.ClientId);
        }
    }
}
