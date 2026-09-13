using UnityEngine;

namespace AsiActionEngine.RunTime
{
    /// <summary>
    /// 本进程在联机拓扑中承担的角色，是引擎的端角色唯一配置入口。
    ///
    /// 取代原先 <c>m_IsNetWorkProject</c> + <c>m_IsServer</c> 两个 bool：后者能表达
    /// "非联机却是服务端"这类非法组合，且把"承担服务器职责"与"无表现层"两个正交语义
    /// 压在同一个字段上——Host 会同时撕开这两点。
    /// </summary>
    public enum EActionEngineRole
    {
        /// <summary>单机：不加载任何网络框架，等价于历史的非联机环境。</summary>
        [InspectorName("单机")]
        Standalone = 0,

        /// <summary>纯客户端：连接远端专用服务器，本地只跑自身玩家与远端表现体。</summary>
        [InspectorName("客户端")]
        Client = 1,

        /// <summary>专用服务器：headless，无相机 / 无鼠标 / 无 Animator。</summary>
        [InspectorName("专用服务器")]
        Server = 2,

        /// <summary>主机：同进程兼作服务器与客户端，有完整表现层。</summary>
        [InspectorName("主机(服务器+客户端)")]
        Host = 3,
    }
}
