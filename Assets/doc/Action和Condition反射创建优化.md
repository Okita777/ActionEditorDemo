# Action 和 Condition 反射创建优化设计

## 1. 目标

本设计用于解决当前技能效果树执行过程中，`Action` 和 `Condition` 节点在运行时被频繁反射创建的问题，并给出一套不会破坏现有战斗主逻辑的重构方案。

当前问题不是单纯的“可以做性能优化”，而是加载期职责和执行期职责发生了混淆：

1. `Skill` 已经作为角色持有资产被装配到运行时。
2. 但 `EffectTree` 内部的 `Action` / `Condition` 却仍然在每次执行时临时解析并反射构造。
3. 命中盒、子弹、Buff 这些高频延迟触发载体会不断重复这一过程。
4. 这使得最热执行路径上存在大量不必要的反射构造、临时对象分配和节点重复装配。

本设计的核心目标是：

1. 将 `Action` / `Condition` 的装配前移到 `LoadSkill` 阶段。
2. 让 `EffectTree` 在角色持有技能时就被编译为可执行 runtime graph。
3. 让运行时执行阶段只处理 `Context`、输入数据和结果聚合，不再承担节点 runtime 的临时创建职责。
4. 让命中盒、子弹、Buff 的延迟 effect 统一执行“已加载 effect graph”，而不是在触发时重新构建 effect 节点执行器。
5. 继续保持当前 context 链路不被破坏：`SkillContext -> MetaSkillContext -> EffectResult / ActionContext`。

---

## 2. 当前问题

### 2.1 当前执行模型

当前 `SkillEffectRuntime` 的执行方式，本质上是：

1. 每次执行 effect tree 时，先遍历配置节点。
2. 命中 `Condition` 节点时，通过 `SkillEffectConditionUtility.Evaluate(...)` 创建 condition runtime。
3. 命中 `Action` 节点时，通过 `SkillActionRuntimeFactory.Create(...)` 创建 action runtime。
4. 执行后立即 `Dispose()`。

这意味着 effect tree 当前并不是“预加载好的可执行图”，而更接近“运行时解释执行配置树”。

### 2.2 当前反射行为的真实成本

需要把当前行为拆开看：

1. `ActionRuntime` / `ConditionRuntime` 的类型注册不是每次都做。
2. 首次使用时会扫描程序集并构建 `Type -> RuntimeType` 映射，后续复用该映射。
3. 但每次真正执行节点时，仍然会调用 `Activator.CreateInstance(...)` 创建新的 runtime 实例。

也就是说，当前真正的问题是：

1. 节点实例创建发生在运行期热路径。
2. 该创建路径依赖反射构造。
3. 每次执行 effect tree，都会按实际命中的节点数重复创建 action / condition runtime。

### 2.3 为什么这不是一个可接受的长期结构

这种模式的问题有四个：

1. 性能问题
	- 高频施法、多段命中、子弹、Buff Tick 都会放大节点构造成本。

2. 生命周期问题
	- 技能已被角色持有，但 effect graph 却没有作为加载期资产被稳定装配。

3. 调试与归属问题
	- 节点 runtime 每次临时 new，没有稳定身份，不利于后续做节点级缓存、source lineage 和调试定位。

4. 职责边界问题
	- 执行期本应只负责“带着当前 context 跑已知结构”，不应再承担“解析并构造结构”。

---

## 3. 设计原则

### 3.1 加载期负责装配，执行期负责运行

必须严格区分两个阶段：

1. `Load` 阶段
	- 负责解析配置。
	- 负责创建 effect runtime graph。
	- 负责把 action / condition 装配成可执行节点。

2. `Execute` 阶段
	- 负责提供当前 `SkillContext` / 触发上下文。
	- 负责执行已加载的 runtime graph。
	- 负责把结果合并回当前 metaskill / skill context。

### 3.2 技能持有期是主加载边界

当前角色技能装配入口已经存在于 `SkillPlayerController.Reload()` -> `TryAddRuntimeState(...)` 这条链路中。

因此本设计明确约定：

1. 角色持有某个技能时，应该只装配一次该技能的 effect graph。
2. 技能施放时，不再重新创建 action / condition runtime。
3. 施法实例是动态的，但技能结构不是动态的。

### 3.3 延迟 effect 载体不是新的结构装配点

命中盒、子弹、Buff 都可能在稍后时机触发 effect，但它们只是：

1. effect 的触发载体
2. source context 的传递载体

它们不应成为新的 `Action` / `Condition` 构建时机。

正确语义应该是：

1. 结构在加载期装好。
2. 载体运行时只持有对已加载 effect graph 的引用。
3. 真正触发时，只创建本次触发所需的执行上下文。

### 3.4 保持 context 聚合中心不变

本次优化的目标是替换“节点 runtime 的创建时机”，不是推翻当前已经落地的 context 聚合主链。

因此应保持：

1. `Action` 节点仍然返回标准化结果。
2. `SkillEffectRuntime` 或其等价执行层仍负责统一合并结果。
3. `MetaSkillContext` / `SkillContext` 仍然是正式聚合根。

---

## 4. 新的生命周期模型

### 4.1 技能加载生命周期

建议把技能运行时分为两层：

1. `LoadedSkillRuntime`
	- 角色持有技能时创建一次。
	- 持有技能级静态结构，包括已编译的 metaskill / effect / action / condition graph。

2. `SkillCastRuntime`
	- 每次施法时创建或复用。
	- 持有本次施法的动态上下文与执行状态。

在当前项目语境下，不一定要立即引入这两个准确类名，但语义上必须拆成这两层。

### 4.2 建议的加载链

建议技能装载链明确为：

1. `LoadSkill()`
2. `LoadMetaSkills()`
3. `LoadEffects()`
4. `LoadConditions()`
5. `LoadActions()`

这条链的核心意思不是多几个函数名，而是把“effect graph 编译”变成显式加载阶段。

### 4.3 执行期应剩下什么

执行期只应做以下事情：

1. 取到已加载好的 effect graph。
2. 组装本次执行的动态上下文。
3. 从 root 节点开始执行已装配节点。
4. 收集本次 effect result。
5. 合并进当前 `MetaSkillContext` 和 `SkillContext`。

执行期不应再做：

1. 通过反射寻找节点 runtime 类型。
2. 通过反射构造 action / condition runtime 实例。
3. 在每次命中时重新把配置树解释成执行结构。

---

## 5. 推荐的运行结构

### 5.1 配置对象和运行对象分离

需要把下面两类对象区分开：

1. 配置对象
	- `SkillConfig`
	- `MetaSkillConfig`
	- `SkillEffectConfig`
	- `SkillEffectNodeConfig`
	- `SkillActionConfig`
	- `SkillConditionConfig`

2. 已加载运行对象
	- `LoadedSkillEffectGraph`
	- `LoadedEffectNode`
	- `LoadedActionNode`
	- `LoadedConditionNode`

这里的类名可以调整，但语义必须明确：

1. 配置是静态原始数据。
2. 已加载运行对象是针对执行期优化过的结构。

### 5.2 建议的节点形态

建议 effect graph 被编译为固定节点树，而不是每次通过 `node.NodeType` 再走配置解释。

例如可以抽象为：

1. `LoadedSequenceNode`
2. `LoadedConditionNode`
3. `LoadedActionNode`

每个节点在加载期就完成：

1. 子节点引用绑定
2. action 执行器绑定
3. condition 执行器绑定
4. 节点静态 tag 数据准备

执行期只调用：

1. `Execute(context, lastResult, effectResult)`

### 5.3 Action / Condition 应该预绑定什么

`Action` 和 `Condition` 在加载期应该完成两类绑定：

1. 类型绑定
	- 配置数据类型 -> 对应 runtime 类型 或 对应执行委托

2. 结构绑定
	- 当前节点所需的配置对象
	- 当前节点的静态参数引用

更进一步，推荐最终落点不是“预创建 runtime 实例”，而是“预创建可执行节点对象”。

原因：

1. 如果直接复用一个 runtime 实例，容易把执行期状态残留塞进共享对象。
2. 如果预创建的是不可变节点对象，节点本身可以长期共享。
3. 动态状态继续全部放进 `SkillContext` / 局部执行帧中，更符合当前 context 设计目标。

### 5.4 不建议的做法

本设计不建议简单把当前代码改成：

1. 启动时把所有 `ActionRuntimeBase` 实例直接 new 一遍存起来
2. 执行时重复复用同一 runtime 实例

原因是当前很多 runtime 的设计默认：

1. 构造时接收 config
2. 运行前 bind context
3. 执行后 dispose

这是一种“短生命周期执行器”语义。

如果直接把这类对象硬改成全局单例式复用，很容易引入：

1. context 污染
2. 并发施法污染
3. 延迟 effect 回调污染
4. 上一次执行状态泄漏到下一次执行

因此更稳妥的方向是：

1. 预加载的是无状态或近似无状态的“节点执行结构”
2. 动态执行状态仍由 context 和局部调用栈承担

---

## 6. 命中盒、子弹、Buff 的处理方式

### 6.1 命中盒 OnHitEffect

命中盒不是 effect graph 的拥有者，而是已加载 effect graph 的触发器。

正确处理方式：

1. 技能加载时，把 `HitBoxConfig.OnHitEffect` 编译为已加载 effect graph。
2. `TimelineHitBoxRuntime` 只保存对该已加载 graph 的引用。
3. 命中发生时，构造本次命中的动态上下文：
	- source skill context
	- source metaskill context
	- source action lineage
	- 当前命中目标 / 命中点
4. 用该上下文执行已加载 graph。

不应在命中发生时：

1. 再根据 `OnHitEffect` 配置解释节点
2. 再逐个创建 action / condition runtime

### 6.2 子弹 OnHitEffect / OnSpawnEffect / OnExpireEffect

子弹和命中盒同理。

正确处理方式：

1. 技能加载时，把子弹引用的各类 effect 全部编译好。
2. 子弹实例只引用这些已加载 effect graph。
3. 每次 spawn / hit / expire 时，只创建当前触发上下文并执行对应 graph。

子弹实例本身可以是动态对象，因为它有：

1. 位置
2. 速度
3. 生命周期
4. 当前碰撞结果

但它不应动态创建 effect graph 结构。

### 6.3 Buff OnAdd / OnUpdate / OnRemove / ReactiveEffect

Buff 更应被视为“预加载模板 + 动态实例”的典型场景。

正确处理方式：

1. `BuffConfig` 对应一份可共享的 `LoadedBuffTemplate`
2. 模板在加载时就编译：
	- `OnAddEffect`
	- `OnUpdateEffect`
	- `OnRemoveEffect`
	- 其他 reactive effects
3. `BuffInstance` 运行时只持有：
	- 模板引用
	- 当前宿主
	- 当前层数
	- 剩余时长
	- source lineage

真正触发时：

1. 基于 buff instance 构造当前执行上下文
2. 执行模板里已加载的 effect graph

不应在 Buff Tick 或 Buff OnRemove 时再创建 action / condition runtime。

### 6.4 延迟 effect 的 source lineage

命中盒、子弹、Buff 都不是新的统计根。

因此它们在执行已加载 effect graph 时，必须携带 source lineage，至少包含：

1. 源 `SkillRuntimeId`
2. 源 `SkillId`
3. 源 `MetaSkillId`
4. 源 `EffectId`
5. 源 `Action` 或源 `NodeId`

只有这样，延迟触发结果才能继续正确汇总回：

1. source action
2. source effect
3. source metaskill
4. source skill

---

## 7. 推荐的装配边界

### 7.1 角色持有技能时装配什么

当 `SkillPlayerController.Reload()` 为角色装配技能时，建议完成以下工作：

1. 加载 `SkillConfig`
2. 加载引用到的 `MetaSkillConfig`
3. 为每个 `MetaSkill` 预编译其引用的 `SkillEffectConfig`
4. 为 `StateTimeline` 中引用的 hitbox / bullet effect 预编译 graph
5. 建立 `SkillRuntime` 对这些已加载 graph 的引用表

也就是说，角色拥有某个技能时，该技能相关的大部分 effect 结构应当已经就绪。

### 7.2 Buff 何时装配

Buff 的加载边界可以有两种合理选择：

1. 全局注册期加载
	- 游戏启动或 Buff 注册表初始化时装配所有 Buff 模板。

2. 首次使用时加载并缓存
	- 第一次 `AddBuff(buffId)` 时加载对应模板，后续复用。

两种都可以，但不论选哪种，都不应在每次 tick / trigger 时装配。

### 7.3 执行期什么必须是动态的

以下对象仍然应该是动态的：

1. `SkillContext` 中与本次施法相关的可变状态
2. `MetaSkillContext`
3. 当次 effect 执行结果
4. 命中盒实例
5. 子弹实例
6. Buff 实例
7. 每次触发的临时 target / hitpoint / 时间片数据

也就是说，动态的是“实例态”和“上下文”，不是“节点结构”。

---

## 8. 推荐迁移方案

### 8.1 第一阶段：只替换 effect graph 装配方式

第一阶段不要碰现有 context 聚合逻辑，只做以下事：

1. 增加 effect graph 的加载态结构。
2. 在技能加载时，把 `SkillEffectConfig` 编译为加载态 graph。
3. 让 `SkillEffectRuntime` 改为执行该 graph，而不是按配置即时解释。

这样可以把变更控制在“结构装配”和“节点执行入口”两个面上，不直接冲击战斗语义。

### 8.2 第二阶段：把 hitbox / bullet 接到已加载 graph

第二阶段处理高频延迟 effect：

1. 命中盒 `OnHitEffect`
2. 子弹 `OnSpawnEffect`
3. 子弹 `OnHitEffect`
4. 子弹 `OnExpireEffect`

这一步完成后，高频命中路径将不再反射创建 action / condition。

### 8.3 第三阶段：把 Buff 模板化

第三阶段再处理 Buff：

1. 为 `BuffConfig` 建立模板缓存
2. 预编译 Buff 的 effect graph
3. `BuffInstance` 改为引用模板 + source lineage

之所以放到第三阶段，是因为 Buff 会和 source context 归属链深度耦合，风险比 hitbox / bullet 更高。

### 8.4 第四阶段：清理旧工厂职责

当前 `SkillActionRuntimeFactory` / `SkillConditionRuntimeFactory` 的职责，可以逐步从：

1. 执行期实例创建工厂

迁移为：

1. 加载期节点构建工厂
2. 类型注册与执行器绑定工厂

也就是说，它们仍然可以存在，但用途要前移到加载阶段。

---

## 9. 风险与约束

### 9.1 不要把“预加载”误做成“共享有状态 runtime 实例”

这是本次重构最容易改烂的地方。

如果只是简单把现有 action / condition runtime 实例缓存起来全局复用，风险极高：

1. `mContext` 绑定会被覆盖
2. 多次施法之间会互相污染
3. 子弹 / Buff 延迟触发会串上下文
4. 调试时会出现来源错乱

因此必须缓存“已加载节点结构”或“无状态执行器绑定”，而不是直接缓存当前这套短生命周期 runtime 实例。

### 9.2 不要破坏当前结果聚合链

当前已经落地的：

1. `MetaSkillContext` 生命周期
2. `LastEffectContext` 聚合
3. `AffectedTargets` / `DataContext` 的统一合并

这些都不应因为本次优化被打散。

本次优化只应改变：

1. action / condition 的装配时机
2. effect tree 的执行结构

不应改变：

1. 结果的正式语义
2. context 的归属关系
3. skill / metaskill 的统计根

### 9.3 Buff 要单独谨慎推进

Buff 不只是一个 effect 触发点，还包含：

1. 生命周期
2. 层数
3. 时长刷新
4. source context 恢复

因此 Buff 应当在 hitbox / bullet 稳定后再推进，不建议和主 effect graph 预加载改造一起一次性混改。

---

## 10. 最终目标形态

最终目标应该是下面这套模型：

1. 角色持有技能时，技能被加载为稳定 runtime graph。
2. effect tree 在加载期完成 action / condition 装配。
3. 施法时只创建施法实例和当次上下文。
4. 命中盒、子弹、Buff 只作为延迟触发器，不再负责装配 effect 结构。
5. 所有 effect 执行都沿统一 context 链返回并聚合结果。

可以用一句话概括：

`Skill`、`MetaSkill`、`EffectGraph` 是加载态资产；`Cast`、`HitBox`、`Bullet`、`BuffInstance` 是运行态实例；`Context` 是每次触发的动态执行面。

---

## 11. 结论

当前“每次执行 effect tree 都反射创建 action / condition runtime”的实现方向不对。

正确方向不是简单做一点工厂缓存，而是：

1. 把 effect graph 变成加载期装配的 runtime structure。
2. 把 action / condition 的构建前移到技能持有期或 Buff 模板加载期。
3. 让 hitbox / bullet / Buff 在触发时只执行已加载 graph。
4. 保持当前 context 聚合主链不变。

这套方案既能解决性能问题，也更符合当前项目要推进的运行时分层：

1. 加载期负责结构。
2. 执行期负责上下文。
3. 聚合期负责结果归属。
