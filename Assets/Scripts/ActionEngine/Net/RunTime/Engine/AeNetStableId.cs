using System;
using AsiTimeLine.RunTime;

namespace AsiTimeLine.Net
{
    /// <summary>
    /// 把 FishNet <c>NetworkObject.ObjectId</c> 映射为引擎的导航稳定单位身份。
    ///
    /// domain 同时按"玩法角色"和"本端是服务端还是客户端"分流。这一点在 Host 下是必需的：
    /// 同一进程会为一个 NPC 同时持有 headless 与 RemoteProxy 两份单位，
    /// 若 domain 相同则第二次 <c>NavigationStableUnitIdentity.Bind</c> 会因 ID 重复而抛异常。
    /// </summary>
    public static class AeNetStableId
    {
        public static ulong ForUnit(
            EAeNetUnitRole role,
            bool asServer,
            int networkObjectId)
        {
            NavigationStableUnitDomain domain = ResolveDomain(role, asServer);
            // ObjectId 可以是 0，而 FromPositiveInt64 要求正数
            return NavigationStableUnitId.FromPositiveInt64(
                domain,
                (long)networkObjectId + 1L);
        }

        private static NavigationStableUnitDomain ResolveDomain(
            EAeNetUnitRole role,
            bool asServer)
        {
            switch (role)
            {
                case EAeNetUnitRole.Player:
                    return asServer
                        ? NavigationStableUnitDomain.ServerPlayerRole
                        : NavigationStableUnitDomain.ClientRole;
                case EAeNetUnitRole.Npc:
                    return asServer
                        ? NavigationStableUnitDomain.ServerMonsterAoi
                        : NavigationStableUnitDomain.ClientMonsterAoi;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(role),
                        $"Unhandled AeNet unit role: {role}.");
            }
        }
    }
}
