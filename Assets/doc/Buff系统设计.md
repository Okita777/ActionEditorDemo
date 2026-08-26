# Buff 系统设计

## 1. 目标

建立一个与现有 Skill / MetaSkill 资源体系一致的 Buff 资源系统。

Buff 应满足：

- Buff 作为独立资源可创建、复制、删除、保存
- Buff 运行时挂在 `CharacterObject` 上
- Buff 可定义标签、持续时间、叠层规则、定时更新逻辑
- Buff 的行为通过效果树配置，而不是为每种 Buff 单独写一个 Buff 类

## 2. 核心设计方向

本系统采用“资源配置 + 通用运行时”的设计，不采用“每个 Buff 一种单独代码类”的方案。

也就是说：

- `燃烧`
- `中毒`
- `护盾`
- `攻击强化`

这些都优先作为 Buff 资源存在，由配置决定行为。

原因：

- Buff 种类多
- Buff 参数复杂
- 不同 Buff 的持续时间、叠层、Update 频率、Tag、效果树都可能不同
- 如果每种 Buff 都写代码类，后续维护成本会迅速变高

所以本系统优先保留灵活性，后续再根据需要增加约束和校验。

## 3. 与当前系统的关系

当前项目里已经存在 Buff 的少量占位：

- `IBuffService`
- `AddBuff` / `RemoveBuff` SkillAction
- `HasBuff` SkillCondition
- `BuffActionArgs`
- `BuffConditionArgs`

但这些还只是第一版占位接口，不足以支撑完整 Buff 系统。

例如当前 `BuffActionArgs` 只有：

- `QueryTarget`
- `BuffId`
- `Duration`

这显然还不够表达：

- Buff 资源定义
- 叠层行为
- Update 间隔
- OnAdd / OnUpdate / OnRemove 效果树
- Buff 类型 / Tag / 图标

因此 Buff 系统应视为即将新增的一整套资源与运行时子系统，而不是简单补几个字段。

## 4. 资源入口设计

和 Skill / MetaSkill 一样，在资源入口中增加第三类资源：

- Skill
- MetaSkill
- Buff

即后续 `SkillResourceEntryWindow` 顶部增加一个 Buff 入口按钮，支持：

- 新建 Buff
- 复制 Buff
- 删除 Buff
- 保存 Buff

Buff 资源的体验应尽量与 Skill / MetaSkill 保持一致。

## 5. Buff 配置结构

建议新增：

```csharp
public sealed class BuffConfig
```

### 5.1 基础字段

```csharp
public string BuffId;
public string BuffName;
public TagContainer Tags;
public float Duration;
public bool IsStackable;
public BuffStackMode StackMode;
public BuffType BuffType;
public string IconAssetPath;
```

### 5.2 生命周期效果树

```csharp
public SkillEffectConfig OnAddEffect;
public SkillEffectConfig OnUpdateEffect;
public SkillEffectConfig OnRemoveEffect;
public float UpdateInterval;
```

这里保持和 MetaSkill 类似：

- `OnAdd`：Buff 添加时执行一次
- `OnUpdate`：Buff 生效期间按固定间隔执行
- `OnRemove`：Buff 移除时执行一次

## 6. 建议枚举

### 6.1 BuffStackMode

```csharp
public enum BuffStackMode
{
    None,
    AddStack,
    ExtendDuration,
}
```

含义：

- `None`：不允许叠层，再次添加时忽略或刷新，具体策略后续细化
- `AddStack`：层数增加
- `ExtendDuration`：不增层，只延长持续时间

### 6.2 BuffType

```csharp
public enum BuffType
{
    None,
    FireBuff,
    PoisonBuff,
    ShieldBuff,
    AttackBuff,
    ControlBuff,
}
```

这里的 `BuffType` 是“人为配置的业务分类”，不是自动保证行为正确的代码类型。

## 7. 运行时结构

建议区分两层：

### 7.1 BuffConfig

资源层定义，负责描述一个 Buff 的静态配置。

### 7.2 BuffRuntime / BuffInstance

运行时实例，挂在目标 `CharacterObject` 上，负责记录：

- 对应的 `BuffConfig`
- 当前剩余时间
- 当前层数
- 下次 `OnUpdate` 触发时间
- 添加来源
- 运行时状态

建议模型：

```csharp
public sealed class BuffInstance
{
    public BuffConfig Config;
    public CharacterObject Owner;
    public object Source;
    public float RemainingDuration;
    public int StackCount;
    public float NextUpdateTime;
}
```

## 8. Buff 宿主

Buff 不应直接挂在 `SkillPlayerController`、`SkillAttributeSet` 或裸 `GameObject` 上。

Buff 的宿主应固定为：

- `CharacterObject`

后续 `target` 解析到的，也应优先是 `CharacterObject`。

这意味着：

- `target.GetAllBuff()` 查询的是角色身上的全部 Buff
- `target.GetTags()` 查询时，后续可以把 Buff 贡献的 Tag 聚合进去
- `AddBuffToTarget` 之类 action 最终操作的目标也是 `CharacterObject`

## 9. Buff 容器

建议新增：

```csharp
public sealed class CharacterBuffContainer
```

职责：

- 持有角色身上的全部 Buff 实例
- 处理 Buff 添加、移除、叠层、更新时间推进
- 提供按 Id、Tag、类型查询
- 触发 OnAdd / OnUpdate / OnRemove 效果树

建议接口：

```csharp
IReadOnlyList<BuffInstance> GetAllBuff();
IReadOnlyList<BuffInstance> GetBuffByTags(IReadOnlyList<string> tags);
bool HasBuff(string buffId);
BuffInstance GetBuff(string buffId);
void AddBuff(BuffApplyRequest request);
void RemoveBuff(BuffRemoveRequest request);
void Tick(float deltaTime);
```

## 10. 生命周期设计

### 10.1 Add

当一个 Buff 被施加到 `CharacterObject`：

1. 根据 `BuffId` 找到 `BuffConfig`
2. 根据叠层规则检查是否已有同 Buff
3. 创建或更新 `BuffInstance`
4. 执行 `OnAddEffect`

### 10.2 Update

当 Buff 存在并到达更新时间：

1. 检查 `UpdateInterval`
2. 若间隔合法，则执行 `OnUpdateEffect`
3. 更新下一次触发时间

注意：

- `OnUpdate` 不是每帧执行
- 而是按 `UpdateInterval` 周期执行

这非常适合：

- 燃烧
- 中毒
- 治疗持续效果
- 周期性护盾回复

### 10.3 Remove

当 Buff 结束或被主动移除：

1. 执行 `OnRemoveEffect`
2. 将 Buff 实例从容器中移除

## 11. 叠层规则

### 11.1 不可叠层

如果 `IsStackable == false`：

- 默认同 Buff 只能存在一个实例
- 再次添加时，后续可以支持：
  - 忽略
  - 刷新持续时间
  - 替换来源

这一条后面可以继续细分，但第一版可以先选一个明确规则。

### 11.2 可叠层 + 增加层数

如果 `StackMode == AddStack`：

- 再次添加时增加 `StackCount`
- `Duration` 是否刷新，需要再定一个附加策略

第一版建议：

- 层数增加
- 持续时间刷新到最新配置值

### 11.3 可叠层 + 延长持续时间

如果 `StackMode == ExtendDuration`：

- 不增加层数
- 将剩余时间延长

## 12. OnUpdate 间隔配置

建议字段：

```csharp
public float UpdateInterval = 0f;
```

解释：

- `<= 0`：不执行 `OnUpdate`
- `> 0`：按间隔执行 `OnUpdateEffect`

这样可以覆盖：

- 没有 Update 行为的 Buff
- 周期伤害 Buff
- 周期治疗 Buff

## 13. BuffType 与实际行为不一致的问题

你提到的问题是成立的。

例如：

- 配了 `BuffType = FireBuff`
- 名字和 Tag 看起来也像燃烧
- 但 `OnUpdateEffect` 实际上根本不是持续伤害

在当前“高度灵活配置”的模型下，这种不一致确实会发生。

### 13.1 这是不是致命问题

不是致命问题，但它是一个明确的设计风险。

因为这里的 `BuffType` 本质上只是：

- 分类
- 显示
- 查询
- 业务约定

而不是真正决定行为的代码类。

真正决定行为的仍然是：

- 生命周期配置
- 效果树
- 更新间隔
- Tag
- 叠层规则

### 13.2 我建议的处理方式

我不建议现在回退到“每个 Buff 一个代码类”的方案。

原因还是一样：

- Buff 太多
- 差异参数太多
- 大量 Buff 只是数值、Tag、间隔、叠层规则不同

所以你当前的总体方向是可以接受的。

但建议加一层“弱约束”，而不是强制代码类化。

### 13.3 推荐的改进思路

推荐使用下面的思路来降低风险：

1. `BuffType` 只当分类，不当行为保证。
2. 真正的行为仍由效果树决定。
3. 后续给编辑器增加“Buff 规则校验/警告”，而不是强制硬编码。

例如可以做一些编辑器 warning：

- `FireBuff` 但没有火焰相关 Tag，给出警告
- `FireBuff` 但 `OnUpdateEffect` 为空，给出警告
- `UpdateInterval > 0` 但 `OnUpdateEffect` 为空，给出警告
- `IsStackable == false` 但配置了叠层模式，给出警告

这样既保留灵活性，又避免完全失控。

### 13.4 可选增强：Template/Archetype

如果后面你觉得纯分类还不够，可以再加一个“模板语义层”：

```csharp
public string BuffTemplateId;
```

例如：

- `burning_base`
- `poison_dot`
- `shield_absorb`

然后：

- `BuffType` 负责业务分类
- `BuffTemplateId` 负责编辑器校验和团队约定

但这不是第一版必须项。

## 14. Buff 图标

先保留字段：

```csharp
public string IconAssetPath;
```

这里先不展开实现细节，后续再根据你的补充调整。

## 15. Buff 对外查询能力

后续 `CharacterObject` 应支持：

```csharp
IReadOnlyList<BuffInstance> GetAllBuff();
IReadOnlyList<BuffInstance> GetBuffByTags(IReadOnlyList<string> tagList);
bool HasBuff(string buffId);
```

后续也可以继续扩展：

```csharp
IReadOnlyList<BuffInstance> GetBuffByType(BuffType type);
```

## 16. 与 Tag 的关系

Buff 应该可以携带自己的 Tag。

这些 Tag 的用途包括：

- 查询 Buff
- 与其他技能/条件联动
- 最终参与角色总 Tag 聚合

后续推荐：

- `CharacterObject.GetTags()` 返回角色固有 Tag + Buff Tag 的聚合结果

但第一版 Buff 系统可以先把“Buff 自带 Tag”和“按 Tag 查询 Buff”做通，再逐步接入角色总 Tag 聚合。

## 17. 与效果树的关系

Buff 的 `OnAdd/OnUpdate/OnRemove` 都继续复用现有 `SkillEffectConfig`。

这是合理的，因为：

- 你已经有了效果树编辑器
- Skill / MetaSkill / Buff 三者都可以共享效果树执行框架

因此 Buff 系统不需要重新发明一套效果表达方式。

## 18. 与当前 SkillAction 的关系

当前已有：

- `AddBuff`
- `RemoveBuff`
- `HasBuff`

后续它们应升级为真正基于 Buff 资源系统工作：

- `AddBuff`：通过 `BuffId` 查 `BuffConfig` 并施加到目标 `CharacterObject`
- `RemoveBuff`：从目标 `CharacterObject` 上移除对应 Buff
- `HasBuff`：查询目标 `CharacterObject` 是否存在该 Buff

## 19. 第一阶段建议实现范围

第一阶段建议只做 Buff 基础骨架：

1. Buff 资源类型 `Buff`
2. `BuffConfig`
3. Buff 资源新建/复制/删除/保存
4. `CharacterBuffContainer`
5. `BuffInstance`
6. `IBuffService` 升级为真正可工作的运行时服务
7. `OnAdd/OnUpdate/OnRemove` 执行链

第一阶段先不做：

- 复杂属性修改器系统
- Buff 授予技能
- Buff 驱动技能替换
- Buff UI 图标展示系统
- 强校验模板系统

## 20. 第二阶段建议实现范围

在第一阶段完成后再做：

1. Buff 图标展示
2. Buff 类型校验和警告
3. Buff Tag 聚合到角色 Tag
4. Buff 查询 API 扩展
5. Buff 对属性和技能的高级修饰器

## 21. 设计结论

你的设计思路是成立的，而且适合当前工程：

- Buff 作为独立资源
- Buff 生命周期复用效果树
- Buff 通过配置表达复杂行为
- 不为每种 Buff 单独写代码类

当前这套方案的主要缺点，是 `BuffType` 和实际行为可能不完全一致。

我的建议不是推翻这套方案，而是：

- 第一版先按你的方案落地
- 把 `BuffType` 当成分类信息，不当成强行为约束
- 后续通过编辑器 warning / 校验来降低错误配置风险

这会比一开始就做大量 Buff 代码类更适合你当前这个项目。