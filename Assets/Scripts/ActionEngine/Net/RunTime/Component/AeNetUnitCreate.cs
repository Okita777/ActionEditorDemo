using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEngine;

namespace AsiTimeLine.Net
{
    /// <summary>
    /// 联机场景的单位摆放入口，与单机的 <see cref="UnitCreate"/> 平行存在（后者保持不动，继续服务单机预览）。
    ///
    /// 本组件不在联机路径下自己建体：位姿由 NetworkObject 承载，建体统一由
    /// <see cref="AeNetNpcSpawner"/>（NPC）与 <see cref="AeNetPlayerSpawner"/>（玩家）驱动。
    /// 只有端角色为 Standalone 时才退化为本地直接建体，用于"同一场景两用"的调试便利。
    /// </summary>
    public sealed class AeNetUnitCreate : MonoBehaviour
    {
        [Header("单位")]
        public int UnitWarpID;

        [Header("联网角色")]
        public EAeNetUnitRole Role = EAeNetUnitRole.Npc;

        [Header("单机退化建体用的导航稳定身份源（联机路径不使用，改由 ObjectId 派生）")]
        [SerializeField, Min(1)] private long mNavigationStableSourceId;

#if UNITY_EDITOR
        private void Reset()
        {
            mNavigationStableSourceId =
                NavigationStableUnitIdentity.CreateAuthoringSourceId();
        }
#endif

        /// <summary>
        /// 用 Start 而非 Awake 读取角色：角色可能被启动参数覆盖，
        /// 而覆盖发生在 <c>ActionEngineManager.Init</c> 内，Awake 阶段各组件的先后顺序不确定。
        /// </summary>
        private void Start()
        {
            ActionEngineManager manager = ActionEngineManager_Input.Instance.CreateGameManager();
            if (manager.Role != EActionEngineRole.Standalone) return;

            CreateStandaloneUnit();
        }

        private void CreateStandaloneUnit()
        {
            if (mNavigationStableSourceId <= 0L)
            {
                throw new System.InvalidOperationException(
                    $"AeNetUnitCreate requires Navigation Stable Source ID: {name}, role={Role}.");
            }

            ulong stableUnitId = NavigationStableUnitId.FromPositiveInt64(
                NavigationStableUnitDomain.AuthoringPreview,
                mNavigationStableSourceId);

            EngineDebug.Log(
                $"[AeNetUnitCreate] 单机退化建体 UnitWarpID=[{UnitWarpID}] Role=[{Role}]");
            AeNetUnitFactory.CreateLocalPredict(
                UnitWarpID,
                stableUnitId,
                transform.position,
                transform.rotation,
                null,
                Role == EAeNetUnitRole.Player);
        }
    }
}
