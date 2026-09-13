namespace AsiActionEngine.RunTime
{
    // Runtime 数据分通道：同一份编辑器数据可按通道导出不同过滤程度的副本，
    // 加载时按通道选择对应文件；本地通道无后缀，等价于历史完整数据。
    public enum ERuntimeDataChannel
    {
        Local = 0, // 本地：无后缀
        Remote,    // 远端：_remote
        Server,    // 服务器：_server
    }

    public static class RuntimeDataChannel
    {
        public const string SuffixLocal = "";
        public const string SuffixRemote = "_remote";
        public const string SuffixServer = "_server";

        public static string GetChannelSuffix(ERuntimeDataChannel _channel)
        {
            switch (_channel)
            {
                case ERuntimeDataChannel.Remote: return SuffixRemote;
                case ERuntimeDataChannel.Server: return SuffixServer;
                default: return SuffixLocal;
            }
        }
    }
}
