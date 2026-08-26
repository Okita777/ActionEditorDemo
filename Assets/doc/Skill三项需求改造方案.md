# Skill 三项需求改造方案

## 1. 目标

本文档只回答当前排期内的 3 件事：

1. 将技能的 cd 与 cost 从 `MetaSkill` 挪到 `Skill`
2. 为连段技能增加“断连后进入 Skill cd”的规则
3. 为 `MetaSkill` 增加“后摇阶段”动画与打断/取消语义

当前原则不变：

- 不为了预览写临时代码
- 不新增脱离正式运行时架构的旁路逻辑
- 编辑器、配置、运行时三层继续保持分离
- 参考 `AsiActionEditor` 的数据保存方式，但不照搬不合适的耦合设计

## 2. 当前实现结论

基于当前代码，先确认几个事实：

### 2.1 cd / cost 当前落点

当前 `cd` 和 `cost` 还定义在：

- `MetaSkillConfig.Cooldown`
- `MetaSkillConfig.Cost`

编辑器入口也还在 `MetaSkillInfo` / `MetaSkillInspector`。

### 2.2 runtime 当前没有正式消费这两个字段

当前运行时主链：

- `SkillPlayerController`
- `SkillRuntime`
- `MetaSkillRuntime`
- `MetaSkillTimelineRuntime`

并没有真正读取 `MetaSkillConfig.Cooldown` 或 `MetaSkillConfig.Cost` 去做：

- 释放前校验
- 释放后扣资源
- 冷却倒计时

这说明第 1 项不是“挪一下 Inspector 文本”这么简单，而是要正式把 `Skill` 级别的资源与冷却规则补进运行时。

### 2.3 当前更合理的职责边界

按需求，`MetaSkill` 是技能中的一个执行段落；`Skill` 才是玩家槽位上装配、真正需要管理 cd 和 cost 的完整技能体。

因此：

- `MetaSkill` 负责执行段逻辑
- `Skill` 负责释放资格、资源消耗、完整技能 cd、连段断连规则

这个边界是正确的，应该落实到 Config 与 Runtime。

## 3. 需求一：cd / cost 从 MetaSkill 挪到 Skill

## 3.1 数据层设计

### 3.1.1 从 `MetaSkillConfig` 移除

移除：

- `Cooldown`
- `Cost`

### 3.1.2 在 `SkillConfig` 增加

建议新增：

```text
SkillConfig
  - Cooldown : float
  - ResourceCosts : List<SkillResourceCostConfig>
```

其中：

```text
SkillResourceCostConfig
  - ResourceType : SkillResourceType
  - Amount : float
```

### 3.1.3 资源类型

第一阶段先支持：

- Mana
- Hp

建议定义：

```text
SkillResourceType
  - Mana
  - Hp
```

保留枚举扩展空间，后续可以继续补：

- Stamina
- Energy
- Rage

## 3.2 Runtime 语义

`Skill` 作为完整技能体，释放规则建议统一为：

### 3.2.1 释放前校验

在尝试从 Entry 进入 Skill 时校验：

- 当前 Skill 是否仍在 cd 中
- 当前 Caster 是否满足全部资源消耗

只要任一不满足，则本次不进入技能。

### 3.2.2 扣除时机

建议在 `Skill` 真正从空闲态进入首个 `MetaSkill` 时扣资源，而不是在切段时扣。

原因：

- 你配置的是一个完整 Skill 的 cost
- 连段中的第 2 段、第 3 段只是同一个 Skill 的延续
- 不能每跳一个 `MetaSkill` 重复扣费

### 3.2.3 cd 起点

建议同样在首次进入 Skill 时记录 cd 起点。

原因：

- 这与第 2 项连段断连规则天然一致
- 也符合“整个 Skill 从第一段开始算 cd”的要求

## 3.3 Runtime 接口建议

当前项目里已有：

- `IBattleResolver`
- `IBuffService`
- `ISkillAttributeSource`

第 1 项不应该再发明一个只给预览用的假接口。建议直接补一层正式资源接口，例如：

```text
ISkillResourceService
  - bool HasResource(GameObject caster, SkillResourceType type, float amount)
  - bool TryConsumeResource(GameObject caster, SkillResourceType type, float amount)
  - float GetResource(GameObject caster, SkillResourceType type)
```

并挂到 `SkillContext`。

这样：

- 预览单位可以给一个正式实现
- 未来正式角色系统也直接接这个接口
- SkillSystem 不需要知道蓝条、血条具体存在哪个组件里

## 3.4 编辑器改造

### 3.4.1 SkillInfo

在 `SkillInfo` / `SkillInspectorPanel` 增加：

- `cd`
- `cost list`

cost list 支持：

- 添加一条资源消耗
- 选择资源类型
- 输入数值
- 删除条目

### 3.4.2 MetaSkillInfo

删除：

- `cd`
- `cost`

这样职责清晰，不再误导配置者把整 Skill 的规则配到单段 `MetaSkill` 上。

## 3.5 兼容旧数据

当前仓库里已有旧的 `MetaSkill` json / byte 资源，因此第 1 项需要考虑兼容：

### 方案

第一阶段只做结构迁移，不自动做复杂升级工具。

策略：

- 新代码读取时忽略 `MetaSkillConfig` 里的旧 `Cooldown/Cost`
- 新建和保存时不再写入这两个字段
- `SkillConfig` 默认 cd 为 0，默认 cost 为空

如果需要，后续再加一个一次性的编辑器迁移工具，把旧 MetaSkill 上的值搬到引用它的 Skill。

当前不建议立即做自动迁移，因为：

- 一个 `MetaSkill` 可能被多个 `Skill` 复用
- 自动迁移会引入业务歧义

## 4. 需求二：连段断连后进入 Skill cd

## 4.1 目标语义

示例：

- 一个 `Skill` 由 `MetaSkill1 -> MetaSkill2 -> MetaSkill3` 组成
- 按下第一段时，整个 Skill cd 就开始计时
- 如果在限定时间内没有接上第二段，则视为断连
- 断连后 Skill 退出，但 cd 不重置，而是继续走“第一段开始时已经启动的 cd”

## 4.2 建议数据落点

不要把这个规则塞进 `MetaSkill`。建议挂在 `Skill` 层，例如：

```text
SkillConfig
  - ComboResetDelay : float
```

含义：

- 从一个连段段落结束后开始计时
- 超过这个时间还没触发下一个有效连段输入，则整个 Skill 断连退出

如果后续要支持“每一段不同断连时间”，再升级到节点级覆盖；当前先做 Skill 级即可。

## 4.3 Runtime 语义

建议在 `SkillRuntime` 中维护：

- Skill 是否已进入 cd
- 首段启动时间
- 当前连段等待截止时间

逻辑：

1. 首次进入 Skill 时：
   - 扣资源
   - 启动 cd
2. 当前 `MetaSkill` 结束后：
   - 若有后续合法连段，则进入“等待下一段输入”状态
   - 若超时仍未接段，则退出 Skill
3. 退出 Skill 时：
   - 不重置 cd 起点

## 4.4 为什么 cd 不应在断连时重新开始

因为你的需求明确是：

- “这个 cd 从开始第一段连段开始计算”

所以断连只是提前结束 Skill，不是重新结算 cd。

## 5. 需求三：MetaSkill 增加后摇阶段

## 5.1 目标语义

一个 `MetaSkill` 分为两个阶段：

### 执行阶段

- 当前已有的 `MetaSkillTimeline`
- 驱动命中盒、子弹、事件、特效等正式技能机制
- 能被其他状态打断
- 不能被自己的后续技能段无条件插断

### 后摇阶段

- 只播放动画表现
- 不再驱动 HitBox / Bullet / Effect
- 整段都允许被其他状态打断
- 允许被任意技能取消

适用场景：

- 连段第 3 段取消第 2 段收刀
- 冲刺取消后摇
- 其他 Action/受击状态打断后摇

## 5.2 数据层建议

建议在 `MetaSkillConfig` 增加一个后摇动画配置，而不是把它塞到当前 `Timeline` 中。

例如：

```text
MetaSkillConfig
  - Recovery

MetaSkillRecoveryConfig
  - AnimationClipPath
  - Duration
  - TransitionConfig...
```

第一阶段可以先简化为：

- `RecoveryAnimationClipPath`
- `RecoveryDuration`

后续再补和执行阶段同级的过渡参数。

## 5.3 Runtime 语义

`MetaSkillRuntime` 建议扩成：

- Execute
- Recovery
- Completed

执行阶段结束后：

- 如果存在 Recovery 配置，则进入 Recovery
- Recovery 只同步动画时间，不执行时间轴内容

打断 / 取消规则：

- 执行阶段：
  - 可被外部状态打断
  - 不允许任意技能直接插断
- 后摇阶段：
  - 可被外部状态打断
  - 可被任意技能取消

## 5.4 与连段的关系

连段切到下一段时，本质上是：

- 如果上一段还在执行阶段，不允许直接切
- 如果上一段已进入后摇，则允许下一段 Skill/MetaSkill 接管

这样既符合“执行动作不能被自己中途切掉”，也符合“后摇可以被技能取消”。

## 6. 实施顺序

当前建议严格按下面顺序做：

### 第一步

完成第 1 项：`cd/cost` 从 `MetaSkill` 迁到 `Skill`

范围包括：

- `SkillConfig`
- `MetaSkillConfig`
- Skill / MetaSkill Inspector
- 资源保存与加载
- runtime 的正式 cd / 资源消耗入口

### 第二步

完成第 2 项：连段断连与 Skill 级 cd 规则

范围包括：

- `SkillRuntime`
- `SkillPlayerController`
- `SkillConfig` 新字段
- Debug snapshot / trace 必要补充

### 第三步

完成第 3 项：后摇阶段

范围包括：

- `MetaSkillConfig`
- `MetaSkillInfo`
- `MetaSkillRuntime`
- 动画桥
- 打断/取消规则

## 7. 本轮先做什么

本轮立刻开始第 1 项，先做以下最小闭环：

1. `SkillConfig` 增加 Skill 级 `Cooldown` 与多资源 `ResourceCosts`
2. `MetaSkillConfig` 移除 `Cooldown/Cost`
3. 编辑器入口迁移到 `SkillInfo`
4. 在 runtime 正式增加 Skill 级 cd / 资源校验与扣除的接口接入点
5. 保证现有 Skill 资源还能被加载，旧 MetaSkill 上残留字段不会阻塞

当前先不做：

- 自动迁移旧数据
- 为预览单独写特殊扣蓝逻辑
- 临时假实现的测试代码

原则是：直接往正式运行时结构上做。