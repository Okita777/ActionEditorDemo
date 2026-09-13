using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;
using UnityEngine.AI;

namespace AsiTimeLine.RunTime
{
    /// <summary>
    /// 占用网格逻辑器（引擎层 <see cref="StaticActionLogics"/>，挂在**目标（暂为本地玩家）** ActionStateMachine 上）：
    /// 由原游戏层 <c>PlayerOccupancyGrid</c> 全部算法内联而来（严格逐环选格 / 标准格图 A\* / 到达才认领）。
    /// 每个目标 SM 各自持有一个实例（= 各自一张网格），天然支持多人各自围环、互不抢格。
    ///
    /// 引擎自包含、**无桥**：<see cref="Event_OccupancyRing"/> 直接 TryGetStaticLogic 取本类并调 <see cref="ResolveNextStep"/>，
    /// 不再经游戏层委托（诊断日志一并删除，避免依赖游戏层 LogUtils）。仅 <c>ExternalMoveAvoidance</c> 因真实依赖游戏层怪群状态仍走委托。
    ///
    /// 生命周期：引擎经 <c>new T()</c> 无参创建 → 首个查询的怪调 <see cref="EnsureGrid"/> 带三环半径惰性建网；
    /// <see cref="OnUpdate"/> 由引擎「每帧首个 TryGetStaticLogic」触发一次（IsEnble 机制），网格每帧只重锚/重采样一次。
    /// </summary>
    public class Ex_OccupancyGrid : StaticActionLogics
    {
        // ── 网格几何（目标相对，格号恒定，建网期预算环归属）──
        // CellSize 略大于怪直径（怪半径 0.3 → 直径 0.6），使「一格容一怪、相邻格不重叠」，claim=1 格即等价一只怪的占位。
        private const float CellSize = 0.65f;
        private const float InvCellSize = 1f / CellSize;
        private const int HalfCells = 12;                       // 覆盖 ±7.8m（含 engage 4.8m + 缓冲）
        private const int GridSize = HalfCells * 2 + 1;         // 25
        private const int CellCount = GridSize * GridSize;      // 625

        private const float MinRadius = 0.3f;                   // 小于此距离的中心格不作为站位目标
        private const float ClaimMigrateThreshold = CellSize * 0.5f + 0.1f; // ≈0.425m 换格滞回：脚下实时占格时，身体在旧占格心此距内不换格，防边界微抖反复改 owner
        private const float NavSampleRadius = CellSize * 0.75f; // 走行性 NavMesh 采样容差
        private const float WalkSampleRadius = 5.0f;            // 只对距目标 ≤此值的格采样可走性(≥engage 4.8+buffer)；更外圈 A* 用不到，置 0 省开销
        // 目标硬碰撞占位：把距目标 ≤此值的格当障碍(walkable=0)。目标本不算障碍 → A* 会给「直穿目标」的最短路 → 怪撞目标卡死。
        // 标成墙后 A* 被逼绕到背面进目标格。取值 < 内环半径(0.7)：只封目标所在中心格、不封内环站位格。
        private const float PlayerBlockRadius = 0.45f;
        private const float RecomputeInterval = 0.2f;           // 走行性重采样节流
        private const float StaleTimeout = 1.0f;                // 选格中的怪超时未查询 → 清理登记（认领态不清，见 Update）
        private const float DiagCost = 1.41421356f;
        // 已认领格叠加的「有限高」cost（准墙）：A\* 找不到零占用路时退化成挤缝，永不封死。
        private const float ClaimedCost = 601f;
        // 邻格膨胀（B）：认领格的「空邻格」叠加的中成本通行费（当前置 0=不膨胀）。
        private const float InflateCost = 0f;

        // 环半径（内→外）：由 EnsureGrid 传入（怪身上 Event_OccupancyRing 的三环参数）。
        private float[] _ringRadii;

        // ── 每格静态属性（目标相对，几何恒定 → 建网期算一次）──
        private readonly sbyte[] _cellRing = new sbyte[CellCount]; // 该格属于第几环；-1 = 不是站位目标（太近/太远）
        private List<int>[] _ringCells;                            // 每环的格号列表，选格时按环遍历（建网期填充）

        // ── 每帧动态状态 ──
        private readonly byte[] _walkable = new byte[CellCount];   // NavMesh 走行性（Update 重采样）
        private readonly ulong[] _owner = new ulong[CellCount];    // 格号 → 认领者 unitId（0=空，到达才写）
        private Vector3 _playerPos;
        private float _sampleY;
        private float _timer;
        private bool _sampledOnce;
        private ulong _navigationChangeSequence;
        private int _claimVersion;                                 // 认领变更计数：任一 Claim/Release 自增，供路径缓存判失效
        private bool _initialized;                                 // 是否已建网（幂等 EnsureGrid）

        private readonly Dictionary<ulong, int> _unitTarget = new Dictionary<ulong, int>(64); // 选中但未到达的目标格
        private readonly Dictionary<ulong, int> _unitClaim = new Dictionary<ulong, int>(64);  // 已到达认领的格
        private readonly Dictionary<ulong, float> _lastSeen = new Dictionary<ulong, float>(64);
        private readonly List<ulong> _staleBuf = new List<ulong>(32);

        // ── A\* 预分配（零稳态 GC）──
        private readonly float[] _gScore = new float[CellCount];
        private readonly int[] _cameFrom = new int[CellCount];
        private readonly int[] _visited = new int[CellCount];      // 访问 stamp，免每次清零
        private readonly int[] _closed = new int[CellCount];
        private int _stamp;
        private readonly int[] _heapCell = new int[CellCount * 8]; // 惰性删除，容量 = 邻接数上限
        private readonly float[] _heapKey = new float[CellCount * 8];
        private int _heapCount;
        private readonly List<int> _pathBuf = new List<int>(CellCount);

        // ── 膨胀标记（B）：认领格的空邻格 = 1（膨胀半墙），随 _claimVersion 惰性重建 ──
        private readonly byte[] _inflated = new byte[CellCount];
        private int _inflateVersion = -1;

        // 当前「活跃」网格（单人下即唯一玩家的网格）：供死亡释放/可视化的静态入口使用。
        private static Ex_OccupancyGrid s_active;

#if UNITY_EDITOR
        // ── 仅 Editor 调试可视化：怪与其目标/认领格共用同一序号(同号=绑定)，并缓存怪位置用于 Scene 连线 ──
        private readonly Dictionary<ulong, int> _debugUnitNumber = new Dictionary<ulong, int>(64);
        private readonly Dictionary<ulong, Vector3> _debugUnitPos = new Dictionary<ulong, Vector3>(64);
        private int _debugNextNumber = 1;
        // 诊断去重：仅在「目标变更 / 停动态翻转」跳变时打日志，压制每帧刷屏（决策频率低但排查成本高）。
        private readonly Dictionary<ulong, int> _debugLastTarget = new Dictionary<ulong, int>(64);
        private readonly Dictionary<ulong, bool> _debugStopped = new Dictionary<ulong, bool>(64);
        private float _debugR0LogTime;
        private static GUIStyle s_debugMonsterStyle, s_debugClaimStyle, s_debugTargetStyle;
#endif

        private static readonly int[] NeighborDx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] NeighborDz = { 0, 0, 1, -1, 1, -1, 1, -1 };

        // ── StaticActionLogics 生命周期 + 惰性建网（替代原 PlayerOccupancyGrid 构造函数）──

        /// 幂等建网：首个查询的怪把三环半径带进来建网；已建则忽略（共享同一张网格）。
        public void EnsureGrid(float[] ringRadii, Vector3 playerPos)
        {
            s_active = this;
            bool navigationReady = SynchronizeNavigationAvailability();
            if (_initialized)
                return;
            _initialized = true;
            _ringRadii = (ringRadii != null && ringRadii.Length > 0) ? ringRadii : new[] { 0.7f, 1.7f, 2.7f };
            _ringCells = new List<int>[_ringRadii.Length];
            for (int r = 0; r < _ringCells.Length; r++)
                _ringCells[r] = new List<int>(64);
            BuildRingLayout();
            if (navigationReady)
                Update(playerPos, 0f); // 建网当帧先采样一次可走性，避免首帧全 0 无法选格
        }

        // 每帧一次（首个 TryGetStaticLogic 触发）：以所在 SM 单位（目标）为锚重采样走行性 + 回收 stale。
        public override void OnUpdate(ActionStateMachine _actionState)
        {
            if (!SynchronizeNavigationAvailability())
                return;
            if (!_initialized || _actionState == null || _actionState.CurUnit == null)
                return;
            s_active = this;
            Update(_actionState.CurUnit.transform.position, Time.deltaTime);
        }

        /// 引擎直调入口（替代原游戏层 OccupancyRingService.Drive）：算「该怪下一步世界点」，并实时占脚下格。
        /// 返回 null = 已站到环站位格（脚下格自己占住）→ 停下交上层攻击；否则返回途经点/直趋玩家点。
        /// attackRange 参数已废弃（占格与攻击距解耦，攻击交上层打断组），保留签名兼容调用方。
        public Vector3? ResolveNextStep(Transform monsterTf, float engageRadius, float attackRange)
        {
            if (!SynchronizeNavigationAvailability())
                return null;
            if (monsterTf == null || !_initialized)
                return null;

            Vector3 selfPos = monsterTf.position;
            Vector3 selfToPlayer = selfPos - _playerPos;
            selfToPlayer.y = 0f;

            // 范围外：直趋玩家接近（本事件不接流场，流场仍归旧 Event_PathFind 路线）。
            float engageSqr = engageRadius * engageRadius;
            if (selfToPlayer.sqrMagnitude > engageSqr)
                return _playerPos;

            ActionEngine_Unit unit = monsterTf.GetComponent<ActionEngine_Unit>();
            ulong unitId = NavigationStableUnitIdentity.Require(unit);

            // 实时占格：进交战范围即每帧占脚下站位格（移出旧格自动释放重占），与攻击距解耦——攻击由上层打断组处理。
            Claim(unitId, selfPos);

            // 选目标格：R0 最近空格（内环优先，满则外溢 R1/R2）。全环无空格 → 直趋玩家兜底。
            if (!TrySelectTarget(unitId, selfPos, out int targetCell))
            {
#if UNITY_EDITOR
                DebugMarkStop(unitId, WorldToCell(selfPos), HorizDist(selfPos, _playerPos), false);
#endif
                return _playerPos;
            }

            // 走到「目标格」本身（脚下即目标）才到位停下——保证怪一路挤到 R0，而非踩到任意外环格就停。
            if (WorldToCell(selfPos) == targetCell)
            {
#if UNITY_EDITOR
                DebugMarkStop(unitId, targetCell, HorizDist(selfPos, _playerPos), true);
#endif
                return null;
            }

            // 未到位：A* 取下一途经点逐段绕已占格推进。
            if (TryGetNextWaypoint(unitId, selfPos, targetCell, out Vector3 waypoint))
            {
#if UNITY_EDITOR
                DebugMarkStop(unitId, WorldToCell(selfPos), HorizDist(selfPos, _playerPos), false);
#endif
                return waypoint;
            }

            // A* 无路（几乎不发生）：直趋玩家兜底。
#if UNITY_EDITOR
            DebugMarkStop(unitId, WorldToCell(selfPos), HorizDist(selfPos, _playerPos), false);
#endif
            return _playerPos;
        }

        // ── 静态入口（供游戏层无需定位 SM 即可释放/可视化，对齐旧 OccupancyRingService 的静态职责）──

        /// 单位死亡：释放其在活跃网格上的认领格。
        public static void ReleaseUnit(ulong unitId) => s_active?.Release(unitId);

        /// 离场：清空活跃网格并断开静态引用（网格实例随玩家 SM 回收）。
        public static void ResetActive()
        {
            s_active?.Clear();
            s_active = null;
        }

        /// 构造期预算每格的环归属（目标相对，几何恒定）：按「离目标距离最接近哪个环半径」归入该环；
        /// 距离 &lt; MinRadius 或 &gt; 最外环 + 半带宽 的格不作为站位目标（_cellRing=-1）。
        private void BuildRingLayout()
        {
            float halfBand = _ringRadii.Length >= 2
                ? (_ringRadii[_ringRadii.Length - 1] - _ringRadii[_ringRadii.Length - 2]) * 0.5f
                : 0.5f;
            float outerCap = _ringRadii[_ringRadii.Length - 1] + halfBand;

            for (int cell = 0; cell < CellCount; cell++)
            {
                int lx = cell % GridSize;
                int lz = cell / GridSize;
                float dx = (lx - HalfCells) * CellSize;
                float dz = (lz - HalfCells) * CellSize;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);

                if (dist < MinRadius || dist > outerCap)
                {
                    _cellRing[cell] = -1;
                    continue;
                }
                int best = 0;
                float bestDelta = Mathf.Abs(dist - _ringRadii[0]);
                for (int r = 1; r < _ringRadii.Length; r++)
                {
                    float delta = Mathf.Abs(dist - _ringRadii[r]);
                    if (delta < bestDelta) { bestDelta = delta; best = r; }
                }
                _cellRing[cell] = (sbyte)best;
                _ringCells[best].Add(cell);
            }
        }

        // ── 每帧：重锚（目标中心）+ 走行性重采样（节流）+ 回收 stale ──
        private void Update(Vector3 playerPos, float deltaTime)
        {
            _playerPos = playerPos;
            _sampleY = playerPos.y;

            _timer += deltaTime;
            if (!_sampledOnce || _timer >= RecomputeInterval)
            {
                _timer = 0f;
                _sampledOnce = true;
                ResampleWalkable();
            }

            // 回收：只清「选格中但超时未查询」的怪的登记；已认领（攻击站定不再查询）的怪跳过——
            // 保住其认领格当墙，认领的释放交给 Release(死亡) 与「怪重新查询=恢复移动」时的自动让位（见 TrySelectTarget）。
            float now = Time.time;
            _staleBuf.Clear();
            foreach (var kv in _lastSeen)
            {
                if (_unitClaim.ContainsKey(kv.Key))
                    continue;
                if (now - kv.Value > StaleTimeout)
                    _staleBuf.Add(kv.Key);
            }
            for (int i = 0; i < _staleBuf.Count; i++)
            {
                ulong u = _staleBuf[i];
                _unitTarget.Remove(u);
                _lastSeen.Remove(u);
            }
        }

        private void ResampleWalkable()
        {
            for (int cell = 0; cell < CellCount; cell++)
            {
                // 可走性 = NavMesh 真实可走，与「是否站位环格(_cellRing)」解耦：环外格虽不作站位目标，
                // 但 A* 必须能路过它们——否则怪在环外(R2~engage)时 A* 起点即硬墙、直接失败退流场（=不绕路）。
                // 例外：目标所在格(≤PlayerBlockRadius)当障碍，逼 A* 绕开目标而非直穿。
                Vector3 w = CellToWorld(cell);
                float distToPlayer = HorizDist(w, _playerPos);
                if (distToPlayer > WalkSampleRadius)
                {
                    _walkable[cell] = 0; // engage 外，A* 路径永不经过，置 0 省采样
                    continue;
                }
                if (distToPlayer <= PlayerBlockRadius)
                {
                    _walkable[cell] = 0; // 目标硬碰撞占位 → 当墙，A* 绕行、禁穿角挡斜切
                    continue;
                }
                _walkable[cell] = NavigationQueryApiRuntime.SamplePosition(
                    w,
                    out _,
                    NavSampleRadius,
                    NavMesh.AllAreas)
                    ? (byte)1
                    : (byte)0;
            }
        }

        /// ② 选目标格：已有目标且仍空 → 沿用（滞回）；否则逐环（内→外）挑「该环里直线最近的空格」。
        /// 返回 false = 全环无空格（整环占满 / 无 NavMesh 落点）→ 调用方回退直趋玩家。
        /// 只负责追击方向目标格；脚下实时占格由 Claim 独立处理，二者解耦。
        public bool TrySelectTarget(ulong unitId, Vector3 selfPos, out int targetCell)
        {
            targetCell = -1;
            if (!SynchronizeNavigationAvailability())
                return false;

            _lastSeen[unitId] = Time.time;
#if UNITY_EDITOR
            _debugUnitPos[unitId] = selfPos; // Scene 调试编号/连线定位
#endif

            // 占脚下格由 Claim 独立实时处理，此处只管「追击方向目标格」，与占格解耦（不再用 _unitClaim 做认领滞回）。

            // 已有目标且仍空 → 沿用（方向滞回，消 churn）。
            bool hasCur = _unitTarget.TryGetValue(unitId, out int cur);
            if (hasCur && IsFreeForUnit(cur, unitId))
            {
                targetCell = cur;
                return true;
            }

            targetCell = SelectNearestFree(selfPos);
            if (targetCell < 0)
            {
                _unitTarget.Remove(unitId);
                return false;
            }
            _unitTarget[unitId] = targetCell;
#if UNITY_EDITOR
            // 诊断①：目标格变更（churn 信号）——只在换目标时打，附带上一目标失效原因，量化横跳频率与成因。
            int prevTarget = _debugLastTarget.TryGetValue(unitId, out int lt) ? lt : -1;
            if (prevTarget != targetCell)
            {
                string reason = !hasCur ? "init"
                    : (_owner[cur] != 0 && _owner[cur] != unitId) ? ("occupied#" + DebugNumberOf(_owner[cur]))
                    : !HasFreeEntrance(cur) ? "noEntrance"
                    : "other";
                EngineDebug.Log(string.Format("[Grid] Target unit=#{0} {1}->{2} ring={3} reason={4}",
                    DebugNumberOf(unitId), prevTarget, targetCell, _cellRing[targetCell], reason));
                _debugLastTarget[unitId] = targetCell;
            }
#endif
            return true;
        }

        /// 目标格对本怪是否「仍可用」——空(或本怪自己占) + 可达(有空邻居入口)。
        /// 加 HasFreeEntrance：keep 滞回时，若目标格因邻居被认领而变成围死死格，立即放弃、下帧重选，
        /// 避免一群怪被 keep 粘死在同一个挤不进的内环格上。
        private bool IsFreeForUnit(int cell, ulong unitId)
        {
            return cell >= 0 && cell < CellCount && _cellRing[cell] >= 0 &&
                   _walkable[cell] == 1 && (_owner[cell] == 0 || _owner[cell] == unitId) &&
                   HasFreeEntrance(cell);
        }

        /// 逐环内→外：第一个有「可达空格」的环里，选离 selfPos 直线最近的。
        /// 可达=空格且至少一个空邻居入口(HasFreeEntrance)——被已认领格/目标墙围死的空格跳过，
        /// 避免一群怪都盯着一个谁也挤不进的内环死格(漏斗死锁)；内环全被围死则本环判空、自动外溢到 R1/R2 铺开包围。
        private int SelectNearestFree(Vector3 selfPos)
        {
#if UNITY_EDITOR
            DebugReportR0IfBlocked();
#endif
            for (int r = 0; r < _ringCells.Length; r++)
            {
                var cells = _ringCells[r];
                int best = -1;
                float bestSqr = float.MaxValue;
                for (int i = 0; i < cells.Count; i++)
                {
                    int cell = cells[i];
                    if (_walkable[cell] != 1 || _owner[cell] != 0 || !HasFreeEntrance(cell))
                        continue;
                    float sqr = (CellToWorld(cell) - selfPos).sqrMagnitude;
                    if (sqr < bestSqr) { bestSqr = sqr; best = cell; }
                }
                if (best >= 0)
                    return best;
            }
            return -1;
        }

        /// 目标格是否有「免费入口」——至少一个可走、未认领、未膨胀的邻居。
        /// 局限：只看静态(walkable/owner/inflated)，识别不了「邻居空但被怪身体动态堵门」——那种残留靠占格误差/超时兜底。
        private bool HasFreeEntrance(int cell)
        {
            int cx = cell % GridSize;
            int cz = cell / GridSize;
            for (int k = 0; k < 8; k++)
            {
                int nx = cx + NeighborDx[k];
                int nz = cz + NeighborDz[k];
                if (nx < 0 || nx >= GridSize || nz < 0 || nz >= GridSize)
                    continue;
                int n = nz * GridSize + nx;
                if (_walkable[n] == 1 && _owner[n] == 0 && _inflated[n] == 0)
                    return true;
            }
            return false;
        }

        /// 实时占脚下格（每帧调用）：脚下格空则占（先释放自己旧占格）；脚下格已被别人占则「共用不抢」——
        /// 该格保持原 owner 当一堵墙，本怪本帧不额外占格（同格两怪算一堵墙，其他怪照常绕开，后期可留意）。
        public void Claim(ulong unitId, Vector3 selfPos)
        {
            if (!SynchronizeNavigationAvailability())
                return;

            int cell = WorldToCell(selfPos);
            if (cell < 0 || cell >= CellCount)
                return;

            // 已占同一格（每帧重复调用）：幂等，不刷 _claimVersion。
            if (_owner[cell] == unitId)
                return;

            // 换格滞回：已占旧格且身体仍在旧格心附近（≤ClaimMigrateThreshold）→ 保持旧格不换，防边界微抖反复改 owner。
            if (_unitClaim.TryGetValue(unitId, out int held) && held != cell &&
                HorizDist(selfPos, CellToWorld(held)) <= ClaimMigrateThreshold)
                return;

            // 身体已移出旧格滞回带：先释放自己的旧占格（无论脚下新格能否占到都要放，实现「离开即释放」）。
            if (_unitClaim.TryGetValue(unitId, out int prev) && prev != cell)
            {
                if (_owner[prev] == unitId)
                    _owner[prev] = 0;
                _unitClaim.Remove(unitId);
                _claimVersion++;
            }

            // 只占站位环格（R0/R1/R2）：脚下是环外格(ring<0，追击途中/外圈) → 不占，避免给别的怪 A* 平白添墙。
            if (_cellRing[cell] < 0)
                return;

            // 脚下格已被别人占：共用不抢——保持原 owner 当墙，本怪不占格（下帧移到空格再占）。
            if (_owner[cell] != 0)
                return;

            // 脚下格空：占领。
            _owner[cell] = unitId;
            _unitClaim[unitId] = cell;
            _unitTarget.Remove(unitId);
            _claimVersion++;
#if UNITY_EDITOR
            // 诊断：占到新脚下格（低频状态跳变）——看怪占的环/格与离玩家距离。
            EngineDebug.Log(string.Format("[Ex_OccupancyGrid] Claim unit=#{0} cell={1} ring={2} dist={3:F2}",
                DebugNumberOf(unitId), cell, _cellRing[cell], HorizDist(selfPos, _playerPos)));
#endif
        }

        /// ③ A\*：从怪当前格搜到目标格，已占格 = +601 墙，返回路径「下一途经点」世界坐标。
        /// 每帧实时算（不缓存）——占格每帧变、缓存命中率低且失效判定成本高，实时 A* 更简单可控。返回 false = 无路（几乎不发生）。
        /// unitId 保留签名兼容调用方（当前实时算不再按怪缓存）。
        public bool TryGetNextWaypoint(ulong unitId, Vector3 selfPos, int targetCell, out Vector3 waypoint)
        {
            waypoint = default;
            if (!SynchronizeNavigationAvailability())
                return false;

            int start = WorldToCell(selfPos);
            if (targetCell < 0 || targetCell >= CellCount)
                return false;

            if (start == targetCell)
            {
                waypoint = SnapToNavMesh(CellToWorld(targetCell));
                return true;
            }

            if (!RunAStar(start, targetCell))
                return false;

            int corner = FirstCorner(start, targetCell);
            waypoint = SnapToNavMesh(CellToWorld(corner));
            return true;
        }

        /// 死亡 / 离场：释放该怪认领格 + 清全部登记。
        public void Release(ulong unitId)
        {
            if (_unitClaim.TryGetValue(unitId, out int cell))
                ReleaseClaim(unitId, cell);
            _unitTarget.Remove(unitId);
            _lastSeen.Remove(unitId);
#if UNITY_EDITOR
            _debugUnitNumber.Remove(unitId);
            _debugUnitPos.Remove(unitId);
            _debugLastTarget.Remove(unitId);
            _debugStopped.Remove(unitId);
#endif
        }

        private void ReleaseClaim(ulong unitId, int cell)
        {
            if (cell >= 0 && cell < CellCount && _owner[cell] == unitId)
                _owner[cell] = 0;
            _unitClaim.Remove(unitId);
            _claimVersion++;
#if UNITY_EDITOR
            // 诊断：释放认领格（状态跳变，低频）——与 Claim 配对，看占格生命周期。
            EngineDebug.Log(string.Format("[Ex_OccupancyGrid] Release unit=#{0} cell={1} ring={2}",
                DebugNumberOf(unitId), cell, (cell >= 0 && cell < CellCount) ? _cellRing[cell] : (sbyte)-1));
#endif
        }

        private static float HorizDist(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        /// 彻底清空（离场/重进场）。
        public void Clear()
        {
            _unitTarget.Clear();
            _unitClaim.Clear();
            _lastSeen.Clear();
#if UNITY_EDITOR
            _debugUnitNumber.Clear();
            _debugUnitPos.Clear();
            _debugLastTarget.Clear();
            _debugStopped.Clear();
#endif
            for (int i = 0; i < CellCount; i++)
            {
                _owner[i] = 0;
                _walkable[i] = 0;
                _inflated[i] = 0;
            }
            _sampledOnce = false;
            _timer = 0f;
            _inflateVersion = -1;
            _heapCount = 0;
            _pathBuf.Clear();
            _claimVersion++;
        }

        public void InvalidateNavigation()
        {
            Clear();
            _navigationChangeSequence =
                NavigationRevisionRuntime.ChangeSequence;
        }

        private void InvalidateNavigationIfChanged()
        {
            ulong currentSequence = NavigationRevisionRuntime.ChangeSequence;
            if (_navigationChangeSequence == currentSequence)
            {
                return;
            }
            Clear();
            _navigationChangeSequence = currentSequence;
        }

        private bool SynchronizeNavigationAvailability()
        {
            InvalidateNavigationIfChanged();
            return NavigationRevisionRuntime.IsQueryReady;
        }

        /// 惰性重建膨胀标记（B）：每个认领格的「空 + 可走」邻格标 1。认领版本未变直接跳过（O(1)）。
        private void RebuildInflationIfDirty()
        {
            if (_inflateVersion == _claimVersion)
                return;
            _inflateVersion = _claimVersion;
            System.Array.Clear(_inflated, 0, CellCount);
            for (int c = 0; c < CellCount; c++)
            {
                if (_owner[c] == 0)
                    continue;
                int cx = c % GridSize;
                int cz = c / GridSize;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + NeighborDx[k];
                    int nz = cz + NeighborDz[k];
                    if (nx < 0 || nx >= GridSize || nz < 0 || nz >= GridSize)
                        continue;
                    int n = nz * GridSize + nx;
                    if (_owner[n] == 0 && _walkable[n] == 1)
                        _inflated[n] = 1;
                }
            }
        }

        // ── 标准格图 A\*（二叉最小堆 + 惰性删除 + 访问 stamp 免清零）──
        private bool RunAStar(int start, int goal)
        {
            RebuildInflationIfDirty();
            _stamp++;
            _heapCount = 0;
            _gScore[start] = 0f;
            _cameFrom[start] = -1;
            _visited[start] = _stamp;
            HeapPush(start, Heuristic(start, goal));

            while (_heapCount > 0)
            {
                int cur = HeapPop();
                if (cur == goal)
                    return true;
                if (_closed[cur] == _stamp)
                    continue;
                _closed[cur] = _stamp;

                int clx = cur % GridSize;
                int clz = cur / GridSize;
                for (int k = 0; k < 8; k++)
                {
                    int nlx = clx + NeighborDx[k];
                    int nlz = clz + NeighborDz[k];
                    if (nlx < 0 || nlx >= GridSize || nlz < 0 || nlz >= GridSize)
                        continue;
                    int n = nlz * GridSize + nlx;
                    if (_walkable[n] == 0 && n != goal)
                        continue; // NavMesh 不可走 = 硬墙（目标格例外，保证有路）
                    if (_closed[n] == _stamp)
                        continue;

                    bool diagonal = k >= 4;
                    // A：禁穿角——对角移动时，两个正交侧格任一「不可走/被认领」则禁止斜切。
                    if (diagonal)
                    {
                        int sideX = clz * GridSize + nlx;   // 横向侧格 (nlx, clz)
                        int sideZ = nlz * GridSize + clx;   // 纵向侧格 (clx, nlz)
                        if (_walkable[sideX] == 0 || _owner[sideX] != 0 ||
                            _walkable[sideZ] == 0 || _owner[sideZ] != 0)
                            continue;
                    }

                    float step = diagonal ? DiagCost : 1f;
                    // B：三档通行费——认领格=准墙(601)、认领格的空邻格=膨胀半墙、空旷格=免费。目标格自身永不加费。
                    float extra = 0f;
                    if (n != goal)
                    {
                        if (_owner[n] != 0)
                            extra = ClaimedCost;
                        else if (_inflated[n] == 1)
                            extra = InflateCost;
                    }
                    float tentative = _gScore[cur] + step + extra;

                    if (_visited[n] != _stamp || tentative < _gScore[n])
                    {
                        _visited[n] = _stamp;
                        _gScore[n] = tentative;
                        _cameFrom[n] = cur;
                        HeapPush(n, tentative + Heuristic(n, goal));
                    }
                }
            }
            return false;
        }

        /// 回溯 A\* 结果，返回 start 之后「第一个拐点」的格号（到该点这一段是直线、途中无转向）。
        private int FirstCorner(int start, int goal)
        {
            _pathBuf.Clear();
            int c = goal;
            int guard = 0;
            while (c != -1 && guard++ <= CellCount)
            {
                _pathBuf.Add(c);
                if (c == start)
                    break;
                c = _cameFrom[c];
            }
            // _pathBuf: goal → ... → start（倒序）。步数 = Count-1，正序索引从末尾往前。
            int n = _pathBuf.Count;
            if (n < 2)
                return goal;

            int p0 = _pathBuf[n - 1];       // start
            int p1 = _pathBuf[n - 2];       // start 的下一步
            int firstDx = StepSign(p1 % GridSize - p0 % GridSize);
            int firstDz = StepSign(p1 / GridSize - p0 / GridSize);
            int corner = p1;
            for (int i = n - 2; i > 0; i--)
            {
                int a = _pathBuf[i];
                int b = _pathBuf[i - 1];
                int dx = StepSign(b % GridSize - a % GridSize);
                int dz = StepSign(b / GridSize - a / GridSize);
                if (dx == firstDx && dz == firstDz)
                    corner = b;
                else
                    break;
            }
            return corner;
        }

        private static int StepSign(int v) => v > 0 ? 1 : (v < 0 ? -1 : 0);

        private float Heuristic(int a, int goal)
        {
            int dx = Mathf.Abs(a % GridSize - goal % GridSize);
            int dz = Mathf.Abs(a / GridSize - goal / GridSize);
            int min = Mathf.Min(dx, dz);
            int max = Mathf.Max(dx, dz);
            return max + (DiagCost - 1f) * min;
        }

        private void HeapPush(int cell, float key)
        {
            if (_heapCount >= _heapCell.Length)
                return; // 溢出保护（理论不可达）
            int i = _heapCount++;
            _heapCell[i] = cell;
            _heapKey[i] = key;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (_heapKey[parent] <= _heapKey[i])
                    break;
                Swap(parent, i);
                i = parent;
            }
        }

        private int HeapPop()
        {
            int top = _heapCell[0];
            _heapCount--;
            if (_heapCount > 0)
            {
                _heapCell[0] = _heapCell[_heapCount];
                _heapKey[0] = _heapKey[_heapCount];
                int i = 0;
                while (true)
                {
                    int l = 2 * i + 1;
                    int r = 2 * i + 2;
                    int smallest = i;
                    if (l < _heapCount && _heapKey[l] < _heapKey[smallest]) smallest = l;
                    if (r < _heapCount && _heapKey[r] < _heapKey[smallest]) smallest = r;
                    if (smallest == i)
                        break;
                    Swap(smallest, i);
                    i = smallest;
                }
            }
            return top;
        }

        private void Swap(int a, int b)
        {
            int tc = _heapCell[a]; _heapCell[a] = _heapCell[b]; _heapCell[b] = tc;
            float tk = _heapKey[a]; _heapKey[a] = _heapKey[b]; _heapKey[b] = tk;
        }

        // ── 坐标换算（目标相对）──
        private Vector3 CellToWorld(int cell)
        {
            int lx = cell % GridSize;
            int lz = cell / GridSize;
            return new Vector3(
                _playerPos.x + (lx - HalfCells) * CellSize,
                _sampleY,
                _playerPos.z + (lz - HalfCells) * CellSize);
        }

        private int WorldToCell(Vector3 world)
        {
            int lx = Mathf.RoundToInt((world.x - _playerPos.x) * InvCellSize) + HalfCells;
            int lz = Mathf.RoundToInt((world.z - _playerPos.z) * InvCellSize) + HalfCells;
            lx = Mathf.Clamp(lx, 0, GridSize - 1);
            lz = Mathf.Clamp(lz, 0, GridSize - 1);
            return lz * GridSize + lx;
        }

        private Vector3 SnapToNavMesh(Vector3 world)
        {
            return NavigationQueryApiRuntime.SamplePosition(
                world,
                out var hit,
                NavSampleRadius * 2f,
                NavMesh.AllAreas)
                ? hit.position
                : world;
        }

#if UNITY_EDITOR
        /// 静态可视化入口（对齐旧 OccupancyRingService.DebugDraw）：画活跃网格。
        public static void DebugDrawActive() => s_active?.DebugDraw();
        public static void DebugDrawLabelsActive() => s_active?.DebugDrawLabels();

        /// 仅 Editor：画认领格（红）与空的站位格（绿），验证由内向外填满与围墙生长。用 Debug.DrawRay，须在 Update 期调用。
        public void DebugDraw()
        {
            if (!_initialized)
                return;
            for (int cell = 0; cell < CellCount; cell++)
            {
                if (_cellRing[cell] < 0 || _walkable[cell] == 0)
                    continue;
                Color c = _owner[cell] != 0 ? Color.red : Color.green;
                Debug.DrawRay(CellToWorld(cell), Vector3.up * 0.4f, c, 0.2f);
            }
        }

        /// 仅 Editor·Scene：怪头顶画黄色编号，其认领格红色编号、目标格绿色编号（同号=绑定），并从怪到格连线。须在 OnDrawGizmos 调用。
        public void DebugDrawLabels()
        {
            if (!_initialized)
                return;
            if (s_debugMonsterStyle == null)
            {
                s_debugMonsterStyle = MakeDebugStyle(Color.yellow);
                s_debugClaimStyle = MakeDebugStyle(Color.red);
                s_debugTargetStyle = MakeDebugStyle(Color.green);
            }

            foreach (var kv in _debugUnitPos)
            {
                if (!_lastSeen.ContainsKey(kv.Key))
                    continue;
                UnityEditor.Handles.Label(kv.Value + Vector3.up * 1.8f, DebugNumberOf(kv.Key).ToString(), s_debugMonsterStyle);
            }

            Vector3 up = Vector3.up * 0.3f;
            foreach (var kv in _unitClaim)
            {
                Vector3 cell = CellToWorld(kv.Value);
                UnityEditor.Handles.Label(cell + Vector3.up * 0.5f, DebugNumberOf(kv.Key).ToString(), s_debugClaimStyle);
                if (_debugUnitPos.TryGetValue(kv.Key, out var p))
                {
                    UnityEditor.Handles.color = Color.red;
                    UnityEditor.Handles.DrawLine(p + up, cell + up);
                }
            }
            foreach (var kv in _unitTarget)
            {
                Vector3 cell = CellToWorld(kv.Value);
                UnityEditor.Handles.Label(cell + Vector3.up * 0.5f, DebugNumberOf(kv.Key).ToString(), s_debugTargetStyle);
                if (_debugUnitPos.TryGetValue(kv.Key, out var p))
                {
                    UnityEditor.Handles.color = Color.green;
                    UnityEditor.Handles.DrawLine(p + up, cell + up);
                }
            }
        }

        private int DebugNumberOf(ulong unitId)
        {
            if (!_debugUnitNumber.TryGetValue(unitId, out int n))
            {
                n = _debugNextNumber++;
                _debugUnitNumber[unitId] = n;
            }
            return n;
        }

        /// 诊断②：停/动态翻转才打一行——看几只怪能稳定「STOP 交攻击」、停在哪个环、是否反复 stop/move 抽搐。
        private void DebugMarkStop(ulong unitId, int cell, float dist, bool stopped)
        {
            bool was = _debugStopped.TryGetValue(unitId, out bool s) && s;
            if (was == stopped)
                return;
            _debugStopped[unitId] = stopped;
            sbyte ring = (cell >= 0 && cell < CellCount) ? _cellRing[cell] : (sbyte)-1;
            EngineDebug.Log(string.Format("[Grid] {0} unit=#{1} cell={2} ring={3} dist={4:F2}",
                stopped ? "STOP" : "MOVE", DebugNumberOf(unitId), cell, ring, dist));
        }

        /// 诊断③：仅当 R0 无任何可选空格时，按 0.5s 节流打一次分项统计——区分「被占满(occupied高)」还是「围死(noEntrance高)」。
        private void DebugReportR0IfBlocked()
        {
            if (_ringCells == null || _ringCells.Length == 0)
                return;
            if (Time.time - _debugR0LogTime < 0.5f)
                return;

            var r0 = _ringCells[0];
            int freeOk = 0, noEntrance = 0, occupied = 0, notWalkable = 0;
            for (int i = 0; i < r0.Count; i++)
            {
                int c = r0[i];
                if (_walkable[c] != 1) { notWalkable++; continue; }
                if (_owner[c] != 0) { occupied++; continue; }
                if (!HasFreeEntrance(c)) { noEntrance++; continue; }
                freeOk++;
            }
            if (freeOk > 0)
                return; // R0 还有可选空格 → 不算被堵，不打

            _debugR0LogTime = Time.time;
            EngineDebug.Log(string.Format("[Grid] R0 blocked total={0} freeOk={1} noEntrance={2} occupied={3} notWalkable={4}",
                r0.Count, freeOk, noEntrance, occupied, notWalkable));
        }

        private static GUIStyle MakeDebugStyle(Color c)
        {
            var s = new GUIStyle { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            s.normal.textColor = c;
            return s;
        }
#endif
    }
}
