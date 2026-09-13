using System.Collections.Generic;

namespace AsiTimeLine.Net
{
    /// <summary>
    /// 服务端玩家意图的步进驱动。
    ///
    /// 注入必须与引擎步 1:1，因此挂点是引擎步之前而不是 FishNet 的 tick：
    /// 同一引擎步内注入两次，后一次的按键边沿会覆盖前一次尚未被打断系统消费的槽位。
    ///
    /// 专用服务器由 <see cref="AeNetServerTick"/> 每个固定步调用；
    /// Host 下引擎随 Unity 可变 dt 每帧一步，由 <see cref="AeNetBootstrap"/> 每帧调用。
    /// </summary>
    public static class AeNetServerIntentPump
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

        /// <summary>推进一个引擎步：把每个玩家缓冲里的意图注入其服务端单位。</summary>
        public static void StepOnce()
        {
            // 倒序遍历：注入过程中单位可能因 despawn 自行反注册
            for (int i = mUnits.Count - 1; i >= 0; i--)
            {
                if (i >= mUnits.Count) continue;

                mUnits[i]?.ServerApplyIntentStep();
            }
        }
    }
}
