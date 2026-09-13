namespace AsiActionEngine.RunTime
{
    /// <summary>
    /// 单位在本进程中的模拟权威角色（由所在 World 的档位赋值，不写死）。
    /// 拆分原 <c>ActionStateMachine.IsLocalClient</c> 一个 bool 承担的多重语义：
    /// 输入采集 / 打断轨 / RootMotion / CC 位移 / 单位旋转。
    ///
    /// 设计见 doc/actengine/ActionEngine_FishNet_ServerAuthority_Blueprint.md §二、
    /// doc/actengine/ActionEngine_FishNet_ServerAuthority.md §四.1。
    /// </summary>
    public enum SimulationAuthority
    {
        /// <summary>客户端本地玩家：跑全套（输入+打断+命中+GValue），可回滚，跑 Animator RootMotion。</summary>
        LocalPredict,

        /// <summary>DS 侧战斗单位：跑输入+打断+命中+GValue，<b>不</b>跑 Animator RootMotion（headless 无 Animator）。</summary>
        ServerAuthoritative,

        /// <summary>客户端上的其它玩家/怪物：被动接受 ChangeAction/位置/GValue 快照，只做插值。</summary>
        RemoteProxy,
    }
}
