# Tag系统设计方案 v2（按当前需求重写）

## 1. 目标与边界

本版只解决技能系统内的 Tag 能力，不扩展到网络、存档、跨场景同步。

目标：

1. 统一定义“谁可以挂载 Tag”。
2. 统一定义“谁可以施加 Tag”。
3. 明确 Tag 不传导、不继承。
4. 支持分层命名与计数器。
5. 为后续 Buff 系统直接复用。

---

## 2. 核心概念

### 2.1 Tag

Tag 是挂载在某个载体上的标签，用来表达该载体具备的特性：

1. 能力类特性（例如 ignoreDef）。
2. 状态类特性（例如 state.debuff.stun）。

Tag 本身不执行逻辑，仅提供语义事实。逻辑由系统读取 Tag 后执行。

### 2.2 TagContainer

TagContainer 是标签容器。任何对象只要拥有 TagContainer，就可以成为 Tag 载体。

---

## 3. 载体模型（谁可以挂载 Tag）

第一阶段明确以下载体：

1. 角色
2. 技能
3. 元技能
4. 效果
5. Buff

设计约束：

1. Tag 挂在哪个载体上，只表示该载体拥有该特性。
2. Tag 不自动向其他载体传播。
3. 不存在默认继承关系。

示例：

1. 某 Buff 挂有 100% 命中相关 Tag，不代表角色本体也自动获得该 Tag。
2. 某 Effect 挂有 onhit 类 Tag，不代表技能本体自动有该 Tag。

---

## 4. 元技能相关规则

### 4.1 元技能静态挂载

元技能可以直接挂 Tag，表示该元技能完整生命周期都拥有该特性。

### 4.2 元技能时段挂载

元技能中通过单位事件 AddTag，在 SkillTimeline 的某段时间内为指定目标添加 Tag。

### 4.3 常用事件拆分

像打断窗口这种高频能力，保留独立单位事件，不强制并入通用 AddTag。

建议：

1. AddTag 负责通用加标签。
2. InterruptWindow 作为专用事件保留，内部可映射为 caninterrupt 相关 Tag，但对策划面板仍是独立事件。

---

## 5. 施加模型（谁可以施加 Tag）

第一阶段定义可直接施加 Tag 的来源：

1. 效果
2. Buff

补充规则：

1. 理论上任何调用方只要能调用 TagSystem.AddTagToTarget 都可以施加。
2. 但策划层默认入口先收敛在 Effect 与 Buff，避免失控。

典型例子：

1. 眩晕效果对目标角色施加 state.debuff.stun。
2. 燃烧 Buff 在持续期内对目标角色施加 state.debuff.fire。

---

## 6. Target 语义（施加到谁）

Tag 施加必须显式指定目标载体。

可选目标类型第一阶段建议支持：

1. Character
2. Skill
3. MetaSkill
4. Effect
5. Buff

示例：

1. 常见情况：把 stun、fire 挂到角色。
2. 特殊情况：某技能效果为“该角色当前所有技能获得无视防御”，则目标是该角色持有的技能集合，每个技能容器添加 ignoreDef。

关键点：

1. 目标必须明确。
2. 禁止隐式“顺带给关联对象也加上”。

---

## 7. 命名规范（采用分层命名）

采用类似 gas 的分层命名：domain.subdomain.tag。

建议域：

1. state.debuff.stun
2. state.debuff.fire
3. state.buff.invincible
4. ability.mod.ignoredef
5. skill.window.caninterrupt

好处：

1. 易读。
2. 可分组检索。
3. 支持父级匹配扩展。

---

## 8. 计数器模型（采用）

同一 Tag 在同一载体上使用计数器而非单一布尔。

规则：

1. AddTag 时计数加一（或按 stack 增加）。
2. RemoveTag 时计数减少。
3. 计数大于 0 视为拥有该 Tag。
4. 计数等于 0 才真正移除。

优势：

1. 多来源叠加安全。
2. 不会因为某个来源提前结束而误删其他来源效果。

---

## 9. 生命周期语义

Tag 持续时间统一语义：

1. duration 小于 0：持续到显式移除。
2. duration 等于 0：单帧。
3. duration 大于 0：持续指定时长。

与施加来源关系：

1. Effect 施加的 Tag 由 Effect 生命周期管理。
2. Buff 施加的 Tag 由 Buff 生命周期管理。
3. 独立 AddTag 事件按事件自身 duration 管理。

---

## 10. 挂载与施加严格区分

必须区分两个动作：

1. 挂载 Tag：说明该载体本身属性。
2. 施加 Tag：一个来源对一个目标产生状态影响。

尤其是 Buff 与 Effect：

1. 它们自己可以挂载 Tag（描述自身）。
2. 它们也可以施加 Tag（影响目标）。
3. 这两件事不是一回事，配置与运行时必须拆开。

---

## 11. 接口草案

### 11.1 查询接口

保留现有查询能力并扩展：

1. HasTag(target, tag)
2. GetTagCount(target, tag)
3. HasAnyTag(target, tags)
4. HasAllTags(target, tags)

### 11.2 施加接口

1. AddTagToTarget(source, target, tag, duration, stack, reason)
2. RemoveTagFromTarget(source, target, tag, stack, reason)
3. RemoveTagsBySource(source, target)

### 11.3 运行时推进

1. Tick(deltaTime) 处理到期移除。

---

## 12. 事件与系统对接建议

### 12.1 常用单位事件

1. AddTagEvent（通用）
2. RemoveTagEvent（可选）
3. InterruptWindowEvent（专用保留）

### 12.2 伤害判定

统一伤害入口读取目标 Tag，不在每个伤害 Action 分散判定。

示例：

1. 若目标有 state.buff.ignoredamage，则伤害流程直接返回 ignored。

---

## 13. 最小实施顺序

### Step 1

1. 实现 TagContainerRuntime 与 TagSystemRuntime（角色先接入）。
2. 接入计数器与 duration 过期。
3. 保证 HasTag 条件可跑通。

### Step 2

1. 新增 AddTag 单位事件。
2. 保留并复用 InterruptWindow 专用事件。

### Step 3

1. Buff 施加 Tag。
2. Effect 施加 Tag。
3. 补充 Skill 载体 Tag 施加路径（例如 ignoreDef 到技能集合）。

---

## 14. 本版关键原则（最终确认）

1. Tag 挂载在哪个载体，只表示该载体的特性。
2. Tag 无继承、无传导、无隐式传播。
3. Buff 与 Effect 可挂载 Tag，也可施加 Tag，但两者概念必须拆开。
4. 命名采用分层形式。
5. 存在性判定基于计数器大于 0。

本方案可直接作为 Tag 系统实现与后续 Buff 系统设计基线。