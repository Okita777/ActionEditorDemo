# CharacterObject 与 Buff 系统设计

## 1. 目标

为角色建立一个统一的战斗载体 `CharacterObject`，集中管理所有与战斗机制相关的角色状态与能力，包括：

- 属性
- 技能槽配置
- 运行时技能实例
- Buff 列表
- Tag 集合
- 战斗目标解析入口

后续所有技能、Buff、Tag、属性判定中的 `target`，都应尽量解析到这个 `CharacterObject`，而不是直接对裸 `GameObject`、`MonoBehaviour`、零散组件做逻辑判断。

## 2. 当前现状

当前项目中还没有统一的 `CharacterObject`。

已存在但职责分散的组件：

- `SkillAttributeSet`
  - 管理属性和资源
  - 实现 `ISkillAttributeSource`、`ISkillResourceService`
- `SkillPlayerController`
  - 管理技能输入、技能槽位装载、`SkillRuntime`
  - 当前是“技能播放控制器”，不是完整角色战斗载体
- `ICharacterActionBridge`
  - 管理动作/动画/中断窗口桥接
  - 不是角色战斗数据载体
- `TagRuntimeService`
  - 是独立 Tag 服务，不是角色对象本身
- Buff 系统尚未建立

当前 `SkillContext` 中的：

- `Caster`
- `PrimaryTarget`

本质上只是“任意 object 引用”，并不保证一定是统一的角色战斗对象。

## 3. 核心判断

结论：

- 现在没有一个统一的 `CharacterObject`
- 你描述的职责边界是合理的，且比当前实现更稳定
- Buff 系统应建立在 `CharacterObject` 之上，而不是直接挂在 `SkillPlayerController` 或 `SkillAttributeSet` 上
- Tag 里所谓“给角色挂 tag”，最终也应是给 `CharacterObject` 挂 tag

## 4. CharacterObject 的职责边界

`CharacterObject` 只负责“战斗角色数据与机制聚合”，不直接承担具体动画实现、移动控制、输入读取、编辑器资源管理。

### 4.1 它应该管理的内容

1. 属性系统入口
2. 技能槽配置入口
3. 运行时技能集合入口
4. Buff 集合入口
5. Tag 集合入口
6. 目标对象统一查询入口

### 4.2 它不应该直接承担的内容

1. 角色移动控制
2. 具体动画播放实现
3. 技能编辑器资源读取
4. Timeline/HitBox/Bullet 的底层执行细节

这些系统可以继续独立存在，但 `CharacterObject` 要成为它们的统一宿主。

## 5. 期望接口模型

你给出的使用方式是对的，建议将来收敛成下面这组 API：

```csharp
float attr = target.GetAttribute(SkillAttributeType.Attack);

IReadOnlyList<CharacterSkillSlot> skillConfigs = target.GetSkillConfigs();

IReadOnlyList<ICharacterSkillRuntime> allSkills = target.GetSkills();

IReadOnlyList<ICharacterBuff> buffList = target.GetAllBuff();

IReadOnlyList<string> tags = target.GetTags();

IReadOnlyList<ICharacterBuff> buffs = target.GetBuffByTags(tagList);
```

注意这里的重点不是“函数名”，而是：

- 所有战斗查询都从同一个对象进入
- 不再让外部系统分别找属性组件、技能组件、Buff 容器、Tag 服务

## 6. 推荐结构

建议增加一个统一组件：

```csharp
public sealed class CharacterObject : MonoBehaviour
```

### 6.1 建议字段

```csharp
public sealed class CharacterObject : MonoBehaviour
{
    [SerializeField] private SkillAttributeSet _attributes;
    [SerializeField] private SkillPlayerController _skillPlayer;
    [SerializeField] private CharacterBuffContainer _buffs;
    [SerializeField] private CharacterTagContainer _tags;
    [SerializeField] private SkillCharacterActionBridge _actionBridge;
}
```

说明：

- `SkillAttributeSet` 先复用现有实现
- `SkillPlayerController` 先复用现有实现
- `CharacterBuffContainer` 后续新建
- `CharacterTagContainer` 后续新建，或者让 `CharacterObject` 直接代理 TagService
- `SkillCharacterActionBridge` 继续承担动作桥接

## 7. CharacterObject 的对外能力

### 7.1 属性

```csharp
float GetAttribute(SkillAttributeType type)
bool TrySetAttribute(...)
bool TryApplyDamage(...)
bool TryConsumeResource(...)
```

第一阶段可以只做查询和资源消耗代理，内部直接转发到 `SkillAttributeSet`。

### 7.2 技能槽配置

这里要区分：

- 静态配置的技能槽
- 运行时实际持有的技能

建议拆成两层：

```csharp
IReadOnlyList<CharacterSkillSlot> GetSkillConfigs();
IReadOnlyList<CharacterSkillSlot> GetSkillConfigs(SkillSlotGroup group);
```

`CharacterSkillSlot` 关注的是：

- 主动/被动
- 槽位编号
- 显示名
- 装配的技能资源名

### 7.3 所有技能

这里应返回“当前角色正在持有的技能集合”，包括：

- 静态装配技能
- 运行时动态添加技能
- 后续 Buff 授予技能

建议接口：

```csharp
IReadOnlyList<ICharacterSkillRuntime> GetSkills();
bool AddRuntimeSkill(...);
bool RemoveRuntimeSkill(...);
```

当前 `SkillPlayerController` 里只有 `_runtimeStates` / `_passiveStates` 的内部集合，没有统一对外查询接口。后续应由 `CharacterObject` 统一暴露。

### 7.4 Buff

建议后续新增：

```csharp
public sealed class CharacterBuffContainer
```

职责：

- 持有角色身上的全部 Buff
- 支持增删 Buff
- 支持按 Tag 查询 Buff
- 支持 Buff 生命周期更新

建议接口：

```csharp
IReadOnlyList<ICharacterBuff> GetAllBuff();
IReadOnlyList<ICharacterBuff> GetBuffByTags(IReadOnlyList<string> tags);
bool AddBuff(...);
bool RemoveBuff(...);
```

### 7.5 Tag

角色自身的 Tag 应该是 `CharacterObject` 的一部分，而不是只散落在技能系统里。

建议接口：

```csharp
IReadOnlyList<string> GetTags();
bool HasTag(string tag);
int GetTagCount(string tag);
```

实现上建议区分两类来源：

1. 角色固有 Tag
2. Buff 贡献 Tag

这样后续 `GetTags()` 和 `HasTag()` 才能反映角色的完整战斗状态。

## 8. Buff 与 Tag 的关系

这里建议从一开始就定清楚：

- `CharacterObject` 自己可以有角色级 Tag
- Buff 自己也可以带 Tag
- 角色查询 Tag 时，最终看到的是“角色固有 Tag + Buff 提供 Tag”的聚合结果

这也对应你说的：

- `target.GetTags()`
- `target.GetBuffByTags(tagList)`

因此后续 Tag 查询不能只看一个简单的 `List<string>`，而应考虑来自多个来源。

## 9. Target 解析应该如何变化

当前 `SkillTargetResolver` 直接返回：

- `context.Caster`
- `context.PrimaryTarget`

这会导致后续所有系统都还要自己再去找属性组件、Buff 容器、Tag 容器。

建议后续改成两层：

### 9.1 基础目标引用层

`SkillContext` 里依然保留：

- `Caster`
- `PrimaryTarget`

但约定它们优先指向 `CharacterObject`。

### 9.2 统一解析层

新增类似：

```csharp
public static class CharacterObjectResolver
{
    public static CharacterObject Resolve(object source);
}
```

它的作用不是提供新玩法，而是做“统一入口适配”：

- 如果传入的本来就是 `CharacterObject`，直接返回
- 如果传入的是 `GameObject`，就在这个对象上找 `CharacterObject`
- 如果传入的是 `MonoBehaviour/Component`，就在它所在对象上找 `CharacterObject`
- 如果找不到，返回 `null`

这样后续技能、Buff、Tag、属性系统就不需要分别处理 `GameObject` / `Component` / `CharacterObject` 三套分支，而是先统一解析成角色战斗载体再继续工作。

这样无论传入的是：

- `CharacterObject`
- `GameObject`
- `MonoBehaviour`

都能统一拿到角色战斗载体。

后续所有战斗系统优先基于 `CharacterObject` 工作。

## 10. 与当前系统的关系

### 10.1 SkillAttributeSet

保留，作为 `CharacterObject` 的属性字段。

### 10.2 SkillPlayerController

保留，但职责变成：

- 技能输入驱动器
- 技能运行时控制器

它不再代表完整角色战斗对象，只是 `CharacterObject` 的一个子模块。

### 10.3 ICharacterActionBridge

保留，仍然只负责动作/动画桥接。

### 10.4 Buff 系统

后续新增，但宿主固定为 `CharacterObject`。

### 10.5 Tag 系统

独立系统保留，但角色级 Tag 查询入口收敛到 `CharacterObject`。

## 11. 第一阶段建议落地范围

先不要一口气把所有系统重写成 `CharacterObject` 强依赖。

建议第一阶段只做“统一入口”和“兼容现有实现”：

1. 新建 `CharacterObject` 组件
2. 挂住现有 `SkillAttributeSet`
3. 挂住现有 `SkillPlayerController`
4. 预留 `BuffContainer`
5. 预留 `TagContainer` / `TagService` 入口
6. 新增 `CharacterObjectResolver`
7. 后续让 `SkillContext.Caster/PrimaryTarget` 尽量传 `CharacterObject`

这样改动最小，但方向正确。

## 12. 第二阶段建议落地范围

在第一阶段完成后，再做 Buff 系统本体：

1. `CharacterBuffContainer`
2. `ICharacterBuff`
3. Buff 生命周期管理
4. Buff 对属性/Tag/技能的修饰
5. `GetBuffByTags(tagList)`

## 13. 当前不做的内容

本轮设计先不做：

- `AddTagToTarget`
- `AddTagToTargetSkill`
- Buff 授予技能
- Buff 驱动技能替换
- Tag 聚合缓存优化

这些应该建立在 `CharacterObject` 统一入口已经落地之后再做。

## 14. 建议的最小接口草案

```csharp
public interface ICharacterObject
{
    float GetAttribute(SkillAttributeType attributeType);
    IReadOnlyList<CharacterSkillSlot> GetSkillConfigs();
    IReadOnlyList<CharacterSkillSlot> GetSkillConfigs(SkillSlotGroup group);
    IReadOnlyList<ICharacterSkillRuntime> GetSkills();
    IReadOnlyList<ICharacterBuff> GetAllBuff();
    IReadOnlyList<string> GetTags();
    IReadOnlyList<ICharacterBuff> GetBuffByTags(IReadOnlyList<string> tags);
}
```

这里先作为设计接口，不要求第一版全部实现完。

## 15. 设计结论

你当前的方向是对的：

- 所有战斗相关角色特性都应由统一角色战斗对象管理
- 找 target，本质上就是找 `CharacterObject`
- 属性、技能、Buff、Tag 都应从这个对象统一查询

当前工程还没有这个统一对象，所以 Buff 系统开工前，先把 `CharacterObject` 这一层立起来是正确顺序。

下一步建议：

1. 先实现 `CharacterObject` 最小壳子，只做属性/技能/动作桥接聚合
2. 再补 `CharacterObjectResolver`
3. 然后再开始 `BuffContainer` 与 `IBuffService` 设计