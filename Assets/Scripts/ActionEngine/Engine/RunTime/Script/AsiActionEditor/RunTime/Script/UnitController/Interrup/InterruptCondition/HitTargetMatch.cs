namespace AsiActionEngine.RunTime
{
    /// <summary>
    /// CheckOnHit / CheckBeHit 共用的目标关系匹配辅助。
    /// 目标含义: 轨道宿主(host)与发起检测的单位(initiator)的关系。
    /// </summary>
    internal static class HitTargetMatch
    {
        public const byte Self = 0;
        public const byte Master = 1;
        public const byte Source = 2;

        /// <summary>
        /// 判断 host 是否符合配置的目标关系。
        /// 自身: host == initiator
        /// 父级: host.GetMaster == initiator
        /// 源:   host.GetSource == initiator
        /// </summary>
        public static bool Match(ActionEngine_Unit host, ActionEngine_Unit initiator, byte type)
        {
            if (host == null || initiator == null) return false;
            switch (type)
            {
                case Self: return host == initiator;
                case Master: return host.GetMaster == initiator;
                case Source: return host.GetSource == initiator;
            }
            return false;
        }
    }
}
