using AsiActionEngine.RunTime;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

namespace AsiTimeLine.Net
{
    /// <summary>
    /// 服务端侧的 NPC 生成器：扫描场景里角色为 Npc 的 <see cref="AeNetUnitCreate"/> 摆点，
    /// 为每个摆点 spawn 一个无 owner 的 <see cref="AeNetUnit"/> 空壳。
    ///
    /// 建体本身不在这里做：headless 单位的导航稳定身份由 <c>NetworkObject.ObjectId</c> 派生，
    /// 而 ObjectId 只在 Spawn 之后有效，所以服务端与客户端都在各自的 OnStart 回调里建体。
    /// 好处是不再依赖作者手工维护唯一的 sourceId，两端建体路径也完全对称。
    /// </summary>
    public sealed class AeNetNpcSpawner
    {
        private readonly NetworkManager mNetworkManager;
        private readonly NetworkObject mNpcPrefab;

        public AeNetNpcSpawner(NetworkManager networkManager, NetworkObject npcPrefab)
        {
            mNetworkManager = networkManager;
            mNpcPrefab = npcPrefab;
        }

        public void SpawnSceneNpcs()
        {
            // InstanceID 排序保证同一场景每次扫描顺序一致，ObjectId 分配因此可复现
            AeNetUnitCreate[] entries = Object.FindObjectsByType<AeNetUnitCreate>(
                FindObjectsSortMode.InstanceID);

            int spawned = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                AeNetUnitCreate entry = entries[i];
                if (entry.Role != EAeNetUnitRole.Npc) continue;

                if (entry.UnitWarpID <= 0)
                {
                    EngineDebug.LogError(
                        $"[AeNetNpcSpawner] 摆点缺少 UnitWarpID name=[{entry.name}]");
                    continue;
                }

                NetworkObject nob = Object.Instantiate(
                    mNpcPrefab,
                    entry.transform.position,
                    entry.transform.rotation);
                AeNetUnit netUnit = nob.GetComponent<AeNetUnit>();
                if (netUnit == null)
                {
                    EngineDebug.LogError("[AeNetNpcSpawner] NPC prefab 上缺少 AeNetUnit 组件");
                    Object.Destroy(nob.gameObject);
                    continue;
                }

                // NPC 恒为服务端权威：没有 owner 客户端可以承担模拟
                netUnit.ServerWriteSpawnData(entry.UnitWarpID, EAeNetUnitRole.Npc, true);
                mNetworkManager.ServerManager.Spawn(nob);
                spawned++;
            }

            EngineDebug.Log($"[AeNetNpcSpawner] 已生成场景 NPC 数量=[{spawned}]");
        }
    }
}
