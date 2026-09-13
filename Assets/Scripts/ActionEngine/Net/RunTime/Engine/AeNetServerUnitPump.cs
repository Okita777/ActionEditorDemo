using System.Collections.Generic;

namespace AsiTimeLine.Net
{
    /// <summary>
    /// 服务端侧单位的每 tick 处理：位姿桥接与 GV 增量下发。
    ///
    /// 挂点必须在 FishNet 的 tick 内、引擎步之后：NetworkTransform 在同一 tick 的 OnPostTick 采样，
    /// 若改在 Unity 的 LateUpdate 桥接，采样到的永远是上一帧的位姿，且采样间隔与引擎步不对齐，
    /// 远端收到的位置增量会忽大忽小。GV 增量沿用同一挂点，保证它与位姿描述的是同一时刻的状态。
    ///
    /// 与 <see cref="AeNetServerIntentPump"/> 的区别是登记范围：意图只针对服务端权威玩家，
    /// 本泵对服务端侧的每个单位都要跑（NPC、服务端权威玩家、以及转发 owner 上报位姿的客户端权威玩家）。
    /// </summary>
    public static class AeNetServerUnitPump
    {
        private static readonly List<AeNetUnit> mUnits = new List<AeNetUnit>();

        public static void Register(AeNetUnit unit)
        {
            if (unit == null || mUnits.Contains(unit)) return;

            mUnits.Add(unit);
        }

        public static void Unregister(AeNetUnit unit)
        {
            if (unit == null) return;

            mUnits.Remove(unit);
        }

        public static void StepOnce()
        {
            // 倒序遍历：处理过程中单位可能因 despawn 自行反注册
            for (int i = mUnits.Count - 1; i >= 0; i--)
            {
                if (i >= mUnits.Count) continue;

                AeNetUnit unit = mUnits[i];
                if (unit == null) continue;

                unit.ServerBridgePose();
                unit.ServerFlushGValueDelta();
            }
        }
    }
}
