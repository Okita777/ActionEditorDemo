# Tag系统设计方案（为Buff系统做前置）

## 1. 目标

当前目标不是一次性做完完整状态系统，而是先把 Tag 做成稳定的基础能力，作为后续 Buff 的判定与联动底座。

本方案遵循现有 SkillRuntime 设计：

1. SkillRuntime 只依赖接口，不依赖具体实现。
2. Tag 先解决“可判定、可增删、可过期、可观察”。
3. Buff 在下一阶段基于 Tag 扩展，不反向破坏 Tag 结构。

---

## 2. 现状与接入点

当前代码里已经有：

1. `ITagQueryService.HasTag(object target, string tag)`
2. `SkillContext.TagQueryService`
3. `HasTag_SkillConditionRuntime` 已通过 `TagQueryService` 判定
4. `IBuffService` 接口与 `AddBuff/RemoveBuff/HasBuff` 动作和条件入口

结论：

- Tag 判定入口已存在，但服务能力过弱（只有 `HasTag`）。
- 现在最需要的是一个真正可运行的 TagRuntimeService 与统一数据模型。

---

## 3. Tag能力边界（第一阶段）

第一阶段只做这些：

1. 查询：目标是否拥有某 Tag。
2. 施加：给目标加 Tag（支持永久和时长）。
3. 移除：从目标移除 Tag。
4. 过期：定时自动移除。
5. 计数：同名 Tag 支持叠层。
6. 观察：能输出调试信息（方便你排问题）。

第一阶段不做这些：

1. 复杂表达式（AND/OR/NOT 组合树）
2. 跨目标传播
3. 网络同步
4. 存档序列化

---

## 4. 数据模型

建议引入运行时结构（仅 Runtime 层）：

```csharp
public enum TagSourceType
{
    Unknown,
    SkillEvent,
    SkillAction,
    Buff,
    System,
}

public sealed class TagInstance
{
    public string TagId;                // 例如: state.airborne, cc.stun, buff.poison
    public int Stack = 1;               // 默认1层
    public float Duration = -1f;        // <0 永久, =0 单帧, >0 持续秒
    public float Elapsed;
    public TagSourceType SourceType;
    public string SourceId;             // 事件ID/技能ID/BuffId
    public object Owner;                // 目标对象引用

    public bool IsExpired => Duration >= 0f && Elapsed >= Duration;
}
```

每个目标维护一个容器：

```csharp
Dictionary<object, Dictionary<string, List<TagInstance>>> _tagsByTarget;
```

说明：

1. 外层按目标分桶。
2. 中层按 `TagId` 分桶。
3. 内层保留实例列表，支持多来源叠加与独立过期。

---

## 5. 命名规范（强约束）

Tag 命名推荐点分命名空间：

1. `state.*`：运动/姿态（`state.airborne`, `state.grounded`）
2. `cc.*`：控制限制（`cc.stun`, `cc.silence`, `cc.root`）
3. `buff.*`：Buff派生标签（`buff.poison`, `buff.burning`）
4. `skill.*`：技能临时标签（`skill.casting`, `skill.recovery`）

规范：

1. 全小写。
2. 不允许空格。
3. 不允许业务同义词并存（例如 stun 和 dizzy 二选一）。

---

## 6. 服务接口设计（建议）

保持 `ITagQueryService` 向后兼容，同时新增可写接口：

```csharp
public interface ITagService : ITagQueryService
{
    void AddTag(object target, string tagId, float duration = -1f, int stack = 1, TagSourceType sourceType = TagSourceType.Unknown, string sourceId = null);
    void RemoveTag(object target, string tagId, int stack = int.MaxValue, string sourceId = null);
    int GetStack(object target, string tagId);
    void Tick(float deltaTime);
}
```

兼容策略：

1. `SkillContext` 继续使用 `TagQueryService` 字段。
2. 若上下文注入的是 `ITagService`，可做强转执行增删。
3. 旧逻辑只查不改，完全不受影响。

---

## 7. 与Skill系统对接

### 7.1 条件侧

`HasTag_SkillConditionRuntime` 保持不变，继续只走 `HasTag`。

### 7.2 动作侧（下一小步）

建议新增两个 SkillActionType：

1. `AddTag`
2. `RemoveTag`

对应 `SkillActionData` + Runtime：

1. `AddTag_SkillActionData/Runtime`
2. `RemoveTag_SkillActionData/Runtime`

这样 Tag 可以被效果树显式驱动，不依赖硬编码。

### 7.3 时间推进

在技能主循环统一调用一次：

1. 在 `SkillRuntime.Tick(deltaTime)` 中调用 `TagService.Tick(deltaTime)`。
2. 只推进一次，避免多次 Tick 导致提前过期。

---

## 8. 与Buff系统的关系（关键）

Buff建议做成“效果容器”，Tag 做“判定信号”。

关系：

1. Buff 创建时可附带若干 Tag（如 `buff.poison`）。
2. Buff 结束时自动移除其来源 Tag（按 `sourceId` 精确移除）。
3. 条件判断优先查 Tag，不直接扫 Buff 列表。

好处：

1. 判定统一，性能稳定。
2. Buff 与非Buff来源（技能事件、系统状态）可共用同一判定语言。
3. 便于做调试面板：一个目标当前有哪些标签、一眼可见。

---

## 9. 生命周期语义（统一约定）

Tag 与你现在事件的时间语义对齐：

1. `duration < 0`：持续直到显式移除。
2. `duration = 0`：单帧标签（下一次 Tick 即过期）。
3. `duration > 0`：按秒持续。

叠层语义：

1. 相同 `tagId` 默认叠层。
2. 查询 `HasTag` 只关心总层数 > 0。
3. `GetStack` 返回总层数。

---

## 10. 调试与可视化建议

建议先做最小调试能力：

1. `SkillBlackboard` 写入最近标签变更（tagId/source/stack）。
2. 在运行时 Inspector 显示当前目标 Tag 列表。
3. 保留最近 N 条 Tag 日志（环形队列）。

重点记录事件：

1. AddTag
2. RemoveTag
3. ExpireTag

---

## 11. 实施顺序（你可以按这个排）

### Step 1（今天可做）

1. 新建 `ITagService` 与 `TagRuntimeService`。
2. 接入 `SkillRuntime.Tick` 的 `TagService.Tick`。
3. 保证 `HasTag` 条件可用。

### Step 2

1. 新增 `AddTag/RemoveTag` SkillAction。
2. 编辑器加两个 Action 配置面板。

### Step 3（开始Buff）

1. Buff实例化时注入/移除来源Tag。
2. Buff条件优先通过Tag判定。

---

## 12. 风险与规避

风险1：对象引用泄漏

1. 目标销毁后若不清理，字典会残留。
2. 规避：在 `Tick` 中剔除空引用目标桶。

风险2：重复 Tick

1. 多处调用 `TagService.Tick` 会导致持续时间翻倍扣减。
2. 规避：只允许 `SkillRuntime` 主循环推进。

风险3：命名污染

1. Tag 字符串随意命名会很快失控。
2. 规避：维护一份 TagRegistry（可先做静态常量类）。

---

## 13. 最终结论

Tag 系统应该先于 Buff 完成，并作为 Buff 的判定和联动底座。

你下一步最划算的落地是：

1. 先把 `ITagService + TagRuntimeService` 跑通。
2. 再补 `AddTag/RemoveTag` 动作。
3. 最后上 Buff，直接复用 Tag 机制。

这样能保证后续 Buff 不是孤岛，而是自然接入当前 SkillRuntime 架构。
