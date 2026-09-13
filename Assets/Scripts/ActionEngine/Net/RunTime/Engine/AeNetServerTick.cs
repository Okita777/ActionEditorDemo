using System;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using FishNet.Managing.Timing;
using UnityEngine;

namespace AsiTimeLine.Net
{
    /// <summary>
    /// 专用服务器的 tick 驱动器。接管 <c>ActionEngineManager</c> 的单位 tick 后，
    /// 引擎不再随 Unity 的可变 dt 推进，而与 FishNet 的网络 tick 严格 1:1。
    ///
    /// 不自持累加器：引擎与 FishNet 各跑一套追帧逻辑时，同一网络 tick 会对应 0~2 个引擎步，
    /// 而 NetworkObject 只承载最终位姿，远端收到的位置增量因此忽大忽小，插值再好也平滑不了。
    /// 订阅 <c>TimeManager.OnTick</c> 后每个 tick 恰好一步，采样点与模拟步天然对齐。
    ///
    /// 导航帧时钟必须一并接管：同一 Unity 帧内若跑多个 tick 而帧号不变，
    /// <c>NavigationQueryBudgetRuntime</c> 的每帧预算不会重置，后续步的导航查询会被静默压制。
    /// </summary>
    public sealed class AeNetServerTick
    {
        private const string c_TickOwnerId = "AsiTimeLine.Net.AeNetServerHost";

        /// <summary>引擎步长，恒等于 <c>TimeManager.TickDelta</c>。</summary>
        public float FixedDt { get; }

        private readonly TimeManager mTimeManager;
        private readonly ulong mFrameClockLease;
        private bool mReleased;

        private AeNetServerTick(TimeManager timeManager, float fixedDt, ulong frameClockLease)
        {
            mTimeManager = timeManager;
            FixedDt = fixedDt;
            mFrameClockLease = frameClockLease;
        }

        /// <summary>
        /// 接管进程级的单位 tick 与导航帧时钟。两者都是单 owner，
        /// 因此先各自 Validate 再依次 Claim，避免其中一个已被别的驱动器占用时留下半接管状态。
        /// </summary>
        public static AeNetServerTick Claim(TimeManager timeManager)
        {
            if (timeManager == null) throw new ArgumentNullException(nameof(timeManager));

            float fixedDt = (float)timeManager.TickDelta;
            if (fixedDt <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeManager),
                    $"Server tick requires a positive TickDelta, got {fixedDt}.");
            }

            ActionEngineManager manager = ActionEngineManager.Instance;
            if (manager == null)
            {
                throw new InvalidOperationException(
                    "ActionEngineManager is unavailable; cannot claim the server tick.");
            }

            manager.ValidateExternalUnitTickDriverClaim(c_TickOwnerId);
            NavigationQueryBudgetRuntime.ValidateExternalFrameClockClaim(c_TickOwnerId);

            manager.ClaimExternalUnitTickDriver(c_TickOwnerId);
            ulong frameClockLease =
                NavigationQueryBudgetRuntime.ClaimExternalFrameClock(c_TickOwnerId);

            AeNetServerTick tick = new AeNetServerTick(timeManager, fixedDt, frameClockLease);
            timeManager.OnTick += tick.TickOnce;

            EngineDebug.Log(
                $"[AeNetServerTick] 已接管单位 tick fixedDt=[{fixedDt:F4}] owner=[{c_TickOwnerId}]");
            return tick;
        }

        public void Release()
        {
            if (mReleased) return;
            mReleased = true;

            if (mTimeManager != null) mTimeManager.OnTick -= TickOnce;
        }

        private void TickOnce()
        {
            // 意图注入与引擎步 1:1，必须在推进之前落到状态机上
            AeNetServerIntentPump.StepOnce();

            NavigationQueryBudgetRuntime.AdvanceExternalFrame(mFrameClockLease);

            // 上一步写入的 transform 需要同步进物理引擎，否则本步的 Overlap/Raycast 查询读到旧位置
            Physics.SyncTransforms();

            ActionEngineManager_Unit.Instance.Update(FixedDt);
            ActionEngineManager_Unit.Instance.LateUpdate(FixedDt);

            // 引擎步之后立刻桥接：同一 tick 的 OnPostTick 里 NetworkTransform 采到的就是本步结果
            AeNetServerUnitPump.StepOnce();
        }
    }
}
