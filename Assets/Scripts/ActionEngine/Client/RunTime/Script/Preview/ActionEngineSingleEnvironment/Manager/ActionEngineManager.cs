using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

#if FMOD
using FMODUnity;
#endif

namespace AsiTimeLine.RunTime
{
    public partial class ActionEngineManager : MonoBehaviour
    {
        private static ActionEngineManager _instance = null;
        // 用于线程安全的锁对象。'volatile' 关键字确保 instance 在多线程间的可见性。
        private static readonly object _lock = new object();
        // 标记应用是否正在退出，防止在退出时创建新实例。
        private static bool _isApplicationQuitting = false;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            if (_instance != null)
            {
                _instance.mExternalUnitTickOwnerId = null;
            }
            _instance = null;
            _isApplicationQuitting = false;
            mIsInit = false;
        }

        [HideInInspector] public Transform mPool_Other;
        [HideInInspector] public Transform mPool_Unit;
        [HideInInspector] public Transform mPool_Prop;
        [HideInInspector] public Transform mPool_Effects;
        [HideInInspector] public Transform mPool_Skills;
        [HideInInspector] public Transform mPool_HitBox;

        [Header("运行环境")]
        [SerializeField] private EActionEngineRole m_Role = EActionEngineRole.Standalone;

        /// <summary>本进程的端角色，联机相关的一切开关都由此派生。</summary>
        public EActionEngineRole Role => m_Role;

        /// <summary>是否参与联机。为 false 时进程内不应存在任何网络对象。</summary>
        public bool IsNetWorked => m_Role != EActionEngineRole.Standalone;

        /// <summary>是否承担服务器职责。Host 同时承担，故也为 true。</summary>
        public bool IsServer =>
            m_Role == EActionEngineRole.Server || m_Role == EActionEngineRole.Host;

        /// <summary>是否承担客户端职责。Host 同时承担，故也为 true。</summary>
        public bool IsClient =>
            m_Role == EActionEngineRole.Client || m_Role == EActionEngineRole.Host;

        /// <summary>
        /// 是否无表现层。仅专用服务器为 true——Host 承担服务器职责但需要相机与鼠标，不算 headless。
        /// 这是原 <c>m_IsServer</c> 承担的第二个语义，现已与 <see cref="IsServer"/> 分离。
        /// </summary>
        public bool IsHeadless => m_Role == EActionEngineRole.Server;

        /// <summary>
        /// 把请求的数据通道解析为实际通道。
        /// 仅专用服务器全局强制提升为 Server（该进程内不存在带模型的 Local 单位）；
        /// Host 不覆盖，由调用方显式决定——这是 Host 能同时持有 Local 玩家与 Server NPC 的前提。
        /// </summary>
        public ERuntimeDataChannel ResolveRuntimeChannel(ERuntimeDataChannel requestedChannel)
        {
            return m_Role == EActionEngineRole.Server
                ? ERuntimeDataChannel.Server
                : requestedChannel;
        }

        /// <summary>
        /// 用启动参数覆盖 Inspector 上的角色。同一场景资产被两端共用时 Inspector 只能存一个值，
        /// 专用服务器与主机靠 <c>-server</c> / <c>-host</c> 在启动时区分。
        /// Standalone 下完全不执行，连命令行数组都不取。
        /// </summary>
        private void ApplyCommandLineRoleOverride()
        {
            if (m_Role == EActionEngineRole.Standalone) return;

            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-server", StringComparison.Ordinal))
                {
                    m_Role = EActionEngineRole.Server;
                    return;
                }
                if (string.Equals(args[i], "-host", StringComparison.Ordinal))
                {
                    m_Role = EActionEngineRole.Host;
                    return;
                }
            }
        }

        public static ActionEngineManager Instance
        {
            get
            {
                // 应用退出时直接返回null，避免创建“幽灵对象”。
                if (_isApplicationQuitting)
                {
                    Debug.LogWarning("[ActionEngineManager] Application is quitting. Instance is already destroyed. Returning null.");
                    return null;
                }

                // 第一次检查：如果实例已存在，避免不必要的锁开销。
                if (CheckNull())
                {
                    lock (_lock) // 加锁确保下方代码块在同一时间只被一个线程执行。
                    {
                        // 第二次检查（双重检查锁定）：进入锁后再次确认实例是否为null。
                        if (CheckNull())
                        {
                            // 1. 尝试在场景中查找已存在的实例
                            _instance = FindAnyObjectByType<ActionEngineManager>();

                            // 2. 如果没找到，则自动创建一个新的GameObject并挂载组件
                            if (CheckNull())
                            {
                                GameObject go = new GameObject("ActionEngineManager");
                                _instance = go.AddComponent<ActionEngineManager>();
                                DontDestroyOnLoad(go); // 确保单例在场景加载时不被销毁
                            }

                            _instance.Init();
                        }
                    }
                }
                return _instance;
            }
        }

        private static bool CheckNull()
        {
#if UNITY_EDITOR
            //消耗稍微高一些  但是更加安全， 在editor时调用  在Runtime中永不销毁就不考虑这个方案，转用更高效的空判断
            return _instance == null;
#endif
            return _instance is null;
        }

#if FMOD
        //仅编辑模式模式绘制用
        [HideInInspector] public EventReference mEventReference;
#endif

        private void Awake()
        {
            // 检查是否已存在一个实例
            if (_instance != null && _instance != this)
            {
                // 如果已存在另一个实例，则销毁这个多余的GameObject本身。
                EngineDebug.LogWarning($"[ActionEngineManager] 一个实例已存在。正在销毁游戏对象上的重复实例: [{gameObject.name}]");
                Destroy(gameObject);
                return;
            }

            // 如果_instance为null，则将当前实例赋值给它。
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject); // 确保正确的实例不被销毁
                Init();
            }
        }

        [HideInInspector] public bool OnUpdateEnble = true;
        private string mExternalUnitTickOwnerId;

        public bool ClaimExternalUnitTickDriver(string ownerId)
        {
            string exactOwnerId = RequireTickOwnerId(ownerId);
            ValidateExternalUnitTickDriverClaim(exactOwnerId);
            if (mExternalUnitTickOwnerId == null)
            {
                mExternalUnitTickOwnerId = exactOwnerId;
                return true;
            }
            return false;
        }

        public void ValidateExternalUnitTickDriverClaim(string ownerId)
        {
            string exactOwnerId = RequireTickOwnerId(ownerId);
            if (mExternalUnitTickOwnerId != null &&
                !string.Equals(
                    mExternalUnitTickOwnerId,
                    exactOwnerId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"ActionEngine unit tick is already owned by '{mExternalUnitTickOwnerId}'.");
            }
        }
        [Header("运行时默认隐藏鼠标")] public bool OnInitHideMouse = true;

        private static bool mIsInit = false;
        private void Init()
        {
#if UNITY_EDITOR
            //Editor下的非运行环境不去初始化
            if (!Application.isPlaying)
                return;
#endif

            //EngineDebug.LogWarning("初始化执行");
            if (mIsInit) return;
            mIsInit = true;
            ApplyCommandLineRoleOverride();
            EngineDebug.Log($"[ActionEngineManager] 初始化运行环境 Role=[{m_Role}]");
            //EngineDebug.LogWarning("初始化执行2");

            if (!IsHeadless)
            {
                ActionEngineManager_Input.Instance.SetMoseDisPlay(!OnInitHideMouse);
            }
            //Transform Pool_Other = new GameObject("Other").transform;

            Transform ActionEngineObjectPool = new GameObject("ActionEngineObjectPool").transform;
            //ActionEngineObjectPool.SetParent(this.transform);

            mPool_Other = new GameObject("Other").transform;
            mPool_Other.SetParent(ActionEngineObjectPool);

            mPool_Unit = new GameObject("Unit").transform;
            mPool_Unit.SetParent(ActionEngineObjectPool);

            mPool_Prop = new GameObject("Prop").transform;
            mPool_Prop.SetParent(ActionEngineObjectPool);

            mPool_Effects = new GameObject("Effects").transform;
            mPool_Effects.SetParent(ActionEngineObjectPool);

            mPool_Skills = new GameObject("Skill").transform;
            mPool_Skills.SetParent(ActionEngineObjectPool);

            mPool_HitBox = new GameObject("HitBox").transform;
            mPool_HitBox.SetParent(ActionEngineObjectPool);

            Dictionary<int, Transform> _objPoolParent = new Dictionary<int, Transform>();
            _objPoolParent.Add((int)EObjPoolParent.Unit, mPool_Unit);
            _objPoolParent.Add((int)EObjPoolParent.Prop, mPool_Prop);
            _objPoolParent.Add((int)EObjPoolParent.Effects, mPool_Effects);
            _objPoolParent.Add((int)EObjPoolParent.Skill, mPool_Skills);
            _objPoolParent.Add((int)EObjPoolParent.HitBox, mPool_HitBox);

            //受击盒对象池初始化
            ActionEngineManager_HitBox.Instance.Init(16, 16, 16, mPool_HitBox);

            //初始化时实现加载方式
            EngineResourcesManager.Instance.Init(new ResourceLoaderFuntion(), mPool_Other, _objPoolParent);

            //初始化公有变量
            ConfigManager.Instance.Init();

            //表头加载（二进制数据）管理器的初始化
            ActionEnginLoadData.Instance.Init();

            ActionEngineManager_GValue.Instance.Init();//GValue数据加载，所有单位必须在此之后加载
            ActionEngineManager_Input.Instance.Init();// 输入相关数据加载，所有单位必须在此之后加载
            ActionEngineManager_Unit.Instance.Init();//单位列表相关数据加载，所有单位必须在此之后加载
        }

        private void Update()
        {
            float _deltaTime = Time.deltaTime;

            /// 资源加载回调必须始终驱动，不受 OnUpdateEnble 控制，否则 CreateUnit 异步回调永远不会触发
            EngineResourcesManager.Instance.Update(_deltaTime);

            if (mExternalUnitTickOwnerId != null || !OnUpdateEnble) return;

            ActionEngineManager_Unit.Instance.Update(_deltaTime);
            ActionEngineManager_Input.Instance.Update(_deltaTime);
        }

        private void LateUpdate()
        {
            if (mExternalUnitTickOwnerId != null || !OnUpdateEnble) return;
            float _deltaTime = Time.deltaTime;
            ActionEngineManager_Unit.Instance.LateUpdate(_deltaTime);
            ActionEngineManager_Input.Instance.LateUpdate(_deltaTime);
        }

        public void Destroy()
        {

        }

        private static string RequireTickOwnerId(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId) ||
                !string.Equals(ownerId, ownerId.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A non-blank, already-normalized tick owner id is required.",
                    nameof(ownerId));
            }
            return ownerId;
        }

        private void EDebug(ActionStateMachine _machine)
        {
            string debug = "";
            int RunConst = 0;
            foreach (ActionStatePart item in _machine.AllActionStatePart)
            {
                if (item.ActionEnble)
                {
                    RunConst++;
                    debug += $"\n [{item.AnimaLayer}] 正常执行";
                }
                else
                {
                    debug += $"\n [{item.AnimaLayer}] 停止运行";
                }
            }
            debug = $"运行层级Debug[{RunConst}]" + debug;
            if (RunConst < 2)
            {
                Debug.LogError(debug);
            }
            else
            {
                Debug.LogWarning(debug);
            }
        }
    }

}
