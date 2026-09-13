using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace AsiTimeLine.Net
{
    /// <summary>
    /// 联机层的唯一启动入口，按 <see cref="ActionEngineManager.Role"/> 分发。
    ///
    /// 本程序集刻意不使用任何 <c>[RuntimeInitializeOnLoadMethod]</c>：整层只在场景里挂了本组件时才被触碰，
    /// 单机预览场景不放本组件即可保证进程内没有任何网络对象。
    /// </summary>
    public sealed class AeNetBootstrap : MonoBehaviour
    {
        private const string c_LocalAddress = "127.0.0.1";

        [Header("网络管理器（必须显式引用：进程内可能同时存在 Game 层的 NetworkManager，按索引取会拿错实例）")]
        [SerializeField] private NetworkManager m_NetworkManager;

        [Header("连接")]
        [SerializeField] private string m_ServerAddress = c_LocalAddress;
        [SerializeField] private ushort m_Port = 7770;

        [Header("Prefab（空壳 NetworkObject，真实表现体由各端自建）")]
        [SerializeField] private NetworkObject m_PlayerPrefab;
        [SerializeField] private NetworkObject m_NpcPrefab;

        [Header("本机玩家")]
        [SerializeField] private int m_PlayerUnitWarpId;
        [SerializeField] private Transform m_PlayerSpawnPoint;

        [Header("玩家模拟权威。开启后玩家操作以意图上行，服务端跑打断与位移并以其结果为准；" +
                "关闭则退回 owner 客户端权威，服务端只做转发。本项由服务端随 spawn 下发，客户端无需配置。" +
                "NPC 不受影响，恒为服务端权威")]
        [SerializeField] private bool m_PlayerServerAuthority = true;

        private EActionEngineRole mRole;
        private AeNetServerTick mServerTick;
        private AeNetPlayerSpawner mPlayerSpawner;
        private AeNetNpcSpawner mNpcSpawner;
        private bool mServerReady;
        private bool mJoinRequested;
        private bool mHostTickSubscribed;

        /// <summary>
        /// 用 Start 而非 Awake：角色可能被启动参数覆盖，而覆盖发生在 <c>ActionEngineManager.Init</c> 内。
        /// 在 Awake 阶段读取会依赖组件初始化顺序，且会让 Instance getter 抢先创建管理器实例。
        /// </summary>
        private void Start()
        {
            ActionEngineManager manager = ActionEngineManager_Input.Instance.CreateGameManager();
            mRole = manager.Role;

            if (mRole == EActionEngineRole.Standalone)
            {
                // 场景里没有 ActionEngineManager 时 Instance getter 会新建一个，Role 取字段默认值 Standalone，
                // 此时 -server / -host 不会被读取（Init 只在非 Standalone 下解析）。挂了本组件却停用必须留痕，
                // 否则表现为「Play 后什么都没发生」，无从排查。
                EngineDebug.LogWarning(
                    "[AeNetBootstrap] Role=[Standalone]，联机层已停用。" +
                    "联机需在场景中放置 ActionEngineManager 并把 Role 配为 Client / Server / Host");
                enabled = false;
                return;
            }

            if (m_NetworkManager == null)
            {
                EngineDebug.LogError("[AeNetBootstrap] 未配置 NetworkManager，联机启动中止");
                enabled = false;
                return;
            }

            EngineDebug.Log($"[AeNetBootstrap] 启动 Role=[{mRole}] port=[{m_Port}]");

            if (mRole == EActionEngineRole.Server || mRole == EActionEngineRole.Host)
            {
                StartServer();
            }
            if (mRole == EActionEngineRole.Client || mRole == EActionEngineRole.Host)
            {
                StartClient();
            }
        }

        /// <summary>
        /// 只服务 Host：专用服务器的引擎步与意图注入都由 <see cref="AeNetServerTick"/> 挂在网络 tick 上。
        /// Host 下单位仍由 ActionEngineManager 以可变 dt 每帧推进一步，意图必须与之 1:1，
        /// 因此留在 Update 而不是 tick。与 ActionEngineManager 的 Update 无强制先后，最坏晚一帧生效。
        /// </summary>
        private void Update()
        {
            if (mServerTick != null) return;

            if (mServerReady)
            {
                AeNetServerIntentPump.StepOnce();
            }
        }

        /// <summary>
        /// Host 的服务端侧 tick 处理。引擎步虽然是可变 dt，但位姿桥接必须与 NetworkTransform 的采样同 tick，
        /// 否则远端收到的位置增量按 Unity 帧率抖动。
        /// </summary>
        private void OnHostServerTick()
        {
            AeNetServerUnitPump.StepOnce();
        }

        private void OnDestroy()
        {
            if (m_NetworkManager == null) return;

            m_NetworkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
            m_NetworkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
            m_NetworkManager.ClientManager.OnAuthenticated -= OnClientAuthenticated;

            if (mHostTickSubscribed)
            {
                m_NetworkManager.TimeManager.OnTick -= OnHostServerTick;
                mHostTickSubscribed = false;
            }
            mServerTick?.Release();
            mPlayerSpawner?.Unregister();
        }

        private void StartServer()
        {
            if (m_PlayerPrefab == null || m_NpcPrefab == null)
            {
                EngineDebug.LogError("[AeNetBootstrap] 未配置玩家/NPC prefab，服务端启动中止");
                return;
            }

            mPlayerSpawner = new AeNetPlayerSpawner(
                m_NetworkManager,
                m_PlayerPrefab,
                m_PlayerSpawnPoint,
                m_PlayerServerAuthority);
            mNpcSpawner = new AeNetNpcSpawner(m_NetworkManager, m_NpcPrefab);

            // 只有专用服务器接管 tick。Host 进程里同时存在需要逐帧输入响应的本地玩家，
            // 而单位 tick 不区分单位，接管会把本地玩家的输入手感压到 tick 频率（见方案 §四之二）。
            if (mRole == EActionEngineRole.Server)
            {
                mServerTick = AeNetServerTick.Claim(m_NetworkManager.TimeManager);
            }
            else
            {
                // Host 不接管引擎步，但位姿桥接仍要挂到网络 tick 上，否则远端插值拿到的采样点不均匀
                m_NetworkManager.TimeManager.OnTick += OnHostServerTick;
                mHostTickSubscribed = true;
            }

            // 先订阅再起服：本地 socket 的 StartConnection 可能同步触发 Started
            m_NetworkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
            m_NetworkManager.ServerManager.StartConnection(m_Port);
        }

        private void StartClient()
        {
            m_NetworkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            m_NetworkManager.ClientManager.OnAuthenticated += OnClientAuthenticated;

            string address = mRole == EActionEngineRole.Host ? c_LocalAddress : m_ServerAddress;
            m_NetworkManager.ClientManager.StartConnection(address, m_Port);
        }

        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState != LocalConnectionState.Started) return;
            if (mServerReady) return;

            mServerReady = true;
            mPlayerSpawner.Register();
            mNpcSpawner.SpawnSceneNpcs();
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                // 放开幂等锁，重连后需要重新申请玩家体
                mJoinRequested = false;
            }
        }

        /// <summary>
        /// 认证完成而非仅传输层连上才发申请：JoinRequest 默认要求已认证，
        /// 在 Started 时就发会被服务端丢弃。
        /// </summary>
        private void OnClientAuthenticated()
        {
            if (mJoinRequested) return;
            if (m_PlayerUnitWarpId <= 0)
            {
                EngineDebug.LogError("[AeNetBootstrap] 未配置本机玩家 UnitWarpID，无法申请玩家体");
                return;
            }

            mJoinRequested = true;
            m_NetworkManager.ClientManager.Broadcast(
                new AeNetJoinRequest { UnitWarpId = m_PlayerUnitWarpId });
            EngineDebug.Log(
                $"[AeNetBootstrap] 已发送 JoinRequest unitWarpId=[{m_PlayerUnitWarpId}]");
        }
    }
}
