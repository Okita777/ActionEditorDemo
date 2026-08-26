# SkillRuntime 第二版框架说明

## 1. 定位

第二版不再只是技能编辑器。

第二版的定位是：

- 角色 3C 配置器
- 技能、元技能、状态的统一编辑与运行时框架
- AI、玩家、固定技能角色都能使用的通用配置体系

这一版要解决的问题不再是“技能能不能放出来”，而是：

- 单位如何正式拥有技能
- 单位如何正式拥有状态
- 技能系统如何与动作系统、状态系统稳定接入
- AI 如何基于同一套状态与技能接口驱动角色

---

## 2. 第二版核心结论

### 2.1 UnitResource

第二版引入 `UnitResource`。

它表示“某一个单位”的正式配置资源。

它不是技能库，也不是状态库本身，而是单位的装配与运行入口配置。

`UnitResource` 负责：

- 默认状态
- 已装备技能
- 相机资源
- 动画目录
- unit 作用域下的资源入口

说明：

- 技能、元技能、状态依然是单独存在的资源文件
- 这些资源都和某一个 `Unit` 相关联
- 它们表示“这是这个 unit 的资源”
- 是否真正生效，还要看是否被装配
- `State` 只要配置给 `Unit`，就等价于这个单位拥有这套 action/state 能力
- 第二版不再延续“全局 skill 文件夹下直接创建资源”的模式
- 创建 `Skill / MetaSkill / State` 时，必须明确当前归属的 `Unit`
- 编辑器中的资源浏览、创建、选择、引用，都要以当前 `UnitResource` 为上下文
- 没有当前 `Unit` 上下文时，不应继续暴露完整的技能/元技能/状态编辑入口

### 2.2 Skill / MetaSkill / State 的关系

第二版要明确区分三者：

- `Skill`：可装配、可拆卸的技能入口资源
- `MetaSkill`：技能实际执行片段资源
- `State`：角色当前所处状态资源

其中：

- `Skill` 决定角色“能不能用这个技能”
- `MetaSkill` 决定这个技能“具体怎么执行”
- `State` 决定角色“当前处于什么动作状态，以及允许怎样切换”

### 2.3 StateResource 的定位

`StateResource` 表示角色所处状态。

它的核心职责是状态管理，但它必须允许带 `Timeline`。

原因不是因为 `State` 和 `Skill` 混在一起，而是因为：

- 角色一旦处于某个状态
- 就天然会有对应动画、事件、攻击盒、打断、转移等内容

因此：

- `StateResource` 可以不配动画
- 如果不配动画，就没有动画轨
- 但它必须允许存在 `Timeline`
- 并且 `Timeline` 内允许直接挂动画、事件、攻击盒等内容

这意味着：

- `State` 是状态资源
- `StateTimeline` 是状态的时间行为描述
- 它不是技能的附庸

### 2.4 MetaSkillTimeline 与 StateTimeline 的关系

第二版必须尽量保持统一实现思路。

原因：

- `MetaSkillTimeline` 本身就是参考 `AsiActionEditor` 的 `action timeline` 做出来的
- `StateTimeline` 本质上也同样是 action timeline 的表达

所以第二版不应写出两套风格完全不同的时间轴实现。

第二版的原则是：

- 统一 `TimelineCore`
- `MetaSkillTimeline` 与 `StateTimeline` 共用底层轨道系统、节点执行系统、事件执行系统
- 上层只区分宿主资源与运行时上下文不同

允许的统一方式：

- `TimelineCore`
- `MetaSkillRuntimeContext`
- `StateRuntimeContext`

也就是：

- 实现一套核心时间轴框架
- 让 `MetaSkill` 和 `State` 挂在这套核心之上

这样既能保证一致性，也避免后期维护两套几乎相同的系统。

### 2.5 动画桥接策略

第二版状态动画与元技能动画继续沿用当前 `Animancer` 播放链路。

当前结论：

- 播放核心以 `AnimancerComponent` 为主
- `StateTimeline` 与 `MetaSkillTimeline` 共用同一种动画采样、同步、淡入淡出和时间对齐逻辑
- 不为 `State` 再引入一套独立于当前桥接层之外的动画系统

关于 `HybridAnimancerComponent`：

- 它保留为兼容层，而不是第二版状态系统的设计基础
- 当现有角色仍依赖 `AnimatorController` 过渡或历史资源尚未清理时，可以继续通过 `HybridAnimancerComponent` 兜底
- 第二版新链路的目标应是尽量收敛到纯 `AnimancerComponent`

也就是说：

- 架构方向上，第二版默认以 `AnimancerComponent` 为标准形态
- 工程落地上，第一阶段不强制清理所有 `HybridAnimancerComponent` 兼容代码，避免无意义地打断当前可运行链路

---

## 3. 状态系统设计

### 3.1 状态接口

外部状态切换需要两类接口：

- `TryChangeState`
- `ForceChangeState`

含义：

- `TryChangeState`：考虑当前打断条件与转移条件，能切才切
- `ForceChangeState`：无视常规打断限制，直接强制切换

这两类接口都需要对外暴露。

### 3.2 State 的默认去向

一个 `State` 可以配置默认状态去向。

典型例子：

- `CastSkillState` 本身不一定配置动画轨
- 它只表示角色当前进入“施法状态”
- 元技能释放成功后，通过 `changeStateTo` 把角色切进 `CastSkillState`
- 元技能结束后进入后摇
- 若没有其他打断或显式转移，则进入该状态配置的默认去向

因此第二版建议 `StateResource` 支持类似字段：

- `DefaultNextState`
- 或 `FallbackState`

用于表达：

- 当当前状态完成，但没有别的更高优先级转移时，默认应该去哪里

### 3.3 打断轨道

第二版 `StateTimeline` 需要正式引入打断轨道。

打断轨道的职责：

- 描述该状态在什么时间窗口
- 满足什么条件
- 允许被转换到什么状态

注意：

打断轨道只定义“可打断能力”。

它不等于：

- 角色只能用这些方式切状态

因为系统还会暴露外部接口：

- `TryChangeState`
- `ForceChangeState`

所以：

- 打断轨道是状态内部转移能力定义
- 外部接口是外部系统干预入口

### 3.4 State 服务对象

`StateResource` 服务所有单位。

包括：

- 玩家
- AI
- 固定技能角色
- 可变技能角色

第二版不允许把 `State` 只做成 AI 专用系统。

目标是：

- 所有单位都能被正式配置完整 3C

---

## 4. 技能系统与状态系统的接入

### 4.1 技能释放前状态判定

第二版 `MetaSkill` 增加当前状态判定。

含义：

- 元技能释放前要检查当前状态是否允许释放
- 状态正式参与技能释放判定

这样技能系统才真正与状态系统耦合起来。

### 4.2 MetaSkill 的 `changeStateTo`

第二版 `MetaSkill` 增加配置项：

- `changeStateTo`

含义：

- 当元技能释放成功后，角色切换到指定状态
- 不配置则不切换

典型使用方式：

- 普通技能元技能释放成功后切入 `CastSkillState`
- 被动技能不配置该项

### 4.3 技能与状态的关系

建议按下面关系理解：

- `State` 决定角色当前处于什么动作状态
- `Skill` 决定角色是否装配某个技能
- `MetaSkill` 决定技能执行时的实际片段

因此：

- 技能不是状态
- 状态也不是技能
- 但技能执行会受到状态约束
- 技能执行也可以驱动状态变化

---

## 5. UnitResource 设计

第二版 `UnitResource` 不负责保存技能库和状态库。

它只负责正式装配与运行时入口配置。

建议字段：

- `UnitId`
- `DisplayName`
- `DefaultState`
- `EquippedSkills`
- `CameraResource`
- `AnimationDirectory`

同时需要补充两条硬约束：

1. `UnitResource` 不是单纯运行时装配表，它还是编辑器的资源上下文入口。
2. `Skill / MetaSkill / State` 的创建、保存路径、列表展示、引用选择，都必须带上 `Unit` 归属。

说明：

- 技能、元技能、状态依然各自独立成资源
- 它们都属于某一个 unit 的资源集合
- 但只有在 `UnitResource` 中被正式装配时，才进入实际运行链路

编辑器约束：

- 进入技能编辑器后，应先确定当前 `UnitResource`
- 当前 `Unit` 未确定时，只允许进行 unit 选择/创建，不继续展示完整技能编辑内容
- `Skill` 列表只显示当前 `Unit` 的 `Skill`
- `MetaSkill` 列表只显示当前 `Unit` 的 `MetaSkill`
- `State` 列表只显示当前 `Unit` 的 `State`
- 跨 unit 引用默认不开放，后续若要支持，必须作为显式能力处理，而不是默认行为

特殊说明：

- `State` 只要配置给 `Unit`，就相当于给该单位配置了 action
- 技能则只有装配后才可用

---

## 6. TimelineCore 统一方案

### 6.1 统一原则

第二版不允许 `MetaSkillTimeline` 和 `StateTimeline` 采用两套完全不同的底层实现。

统一原则：

- 同一套轨道系统
- 同一套节点调度系统
- 同一套事件执行机制
- 同一套攻击盒、单位事件、特效、音效扩展能力

### 6.2 宿主分离

虽然底层统一，但宿主要区分：

- `MetaSkillTimeline`
- `StateTimeline`

原因：

- `MetaSkill` 是技能执行片段
- `State` 是角色状态资源

两者语义不同，但实现底层要统一。

### 6.3 运行时上下文

建议区分两类上下文：

- `MetaSkillRuntimeContext`
- `StateRuntimeContext`

这样可以做到：

- timeline 内核一致
- 运行时数据来源和可访问接口不同

---

## 7. AI 接入方式

第二版 AI 计划采用 `BehaviorDesigner`。

目标流程：

1. 新建 AI 角色
2. 配置 AI 的 `State`
3. 配置 AI 的 `Skill`
4. 给 AI 角色装配 `Skill`
5. 使用 `BehaviorDesigner` 编排 AI 逻辑

行为树节点只需要调用稳定接口：

- `ChangeStateNode`
- `UseSkillNode`

底层调用：

- `TryChangeState(stateName)`
- `ForceChangeState(stateName)`
- `UseSkill(skillId)`

原则：

- 行为树负责决策
- 状态系统负责状态切换
- 技能系统负责技能执行
- Timeline 负责具体时间行为

---

## 8. 技能资源职责调整

### 8.1 cost 从 Skill 移到 MetaSkill

第二版将 `cost` 从 `Skill` 挪到 `MetaSkill`。

原因：

- `Skill` 是入口资源
- `MetaSkill` 才是具体执行片段
- 实际消耗应与具体执行片段绑定

### 8.2 cd 继续保留在 Skill

`cd` 仍然保留在 `Skill` 中。

当前规则不改：

- 当前第一个 `MetaSkill` 释放成功时进入 CD

这一点暂不调整。

---

## 9. 动作系统接入原则

第二版最大的成败点，是技能系统如何与动作系统接入。

当前原则固定如下：

1. 技能系统不等于动作系统
2. 状态系统是动作系统接入层
3. 技能通过状态系统与动作系统交互
4. 外部系统通过 `TryChangeState / ForceChangeState / UseSkill` 与运行时交互
5. Timeline 统一承载状态和技能的时间行为
6. 状态打断轨道与条件表达，严格参考 `AsiActionEditor`
7. 动画桥接继续复用当前 `Animancer` 链路，不额外派生第二套状态动画方案

换句话说：

- 技能系统不是孤立播放技能的系统
- 它是角色 3C 配置体系中的一部分

---

## 10. 第二版实现顺序

建议实现顺序如下：

### 10.1 第一阶段：资源模型与运行时骨架

- 新增 `UnitResource`
- 让场景预览单位可以正式应用 `UnitResource`
- 编辑器以当前 `Unit` 为上下文显示 `Skill / MetaSkill / State`
- 当前 `Unit` 未确定时，隐藏或禁用相关资源编辑入口
- 保持当前技能预览、技能释放、元技能运行链路不被破坏

第一阶段明确不追求：

- 立即完成整套 `StateResource` 运行时
- 立即抽出完整 `TimelineCore`
- 立即替换现有动画桥接实现

第一阶段的目标不是“把第二版一次性做完”，而是：

- 先建立 `UnitResource` 这个新的顶层资源入口
- 把编辑器和预览链路切到 unit 视角
- 在不破坏现有技能链路的前提下，为后续 `State` 与 AI 接入打地基

### 10.2 第二阶段：状态与技能接入

- 新增 `StateResource`
- 新增 `StateController`
- 明确 `TryChangeState / ForceChangeState`
- 元技能增加状态释放判定
- 元技能增加 `changeStateTo`
- 技能释放链路接入 `StateController`
- 打通 `CastSkillState -> 后摇 -> 默认状态` 的完整链路

### 10.3 第三阶段：Unit 正式装配链路

- 角色正式持有默认状态、装备技能、动画目录、摄像机资源
- 固定技能角色与 AI 角色具备完整装配链路
- 清理第一阶段中的临时兼容入口，收敛到正式 unit 装配流程

### 10.4 第四阶段：AI 接入

- 基于 `BehaviorDesigner` 对接 `UseSkillNode`
- 基于 `BehaviorDesigner` 对接 `ChangeStateNode`
- 让 AI 使用与玩家相同的状态与技能运行时框架

### 10.5 第五阶段：编辑器体验完善

- Unit 视角下查看 Skill / MetaSkill / State
- Action 参数联动显示
- 节点创建位置优化
- Buff、Skill 等资源查找优化
- 摄像机、特效、音效轨道补齐

---

## 11. 当前确认的优化与遗漏清单

### 11.1 资源与列表显示

1. Buff 资源列表显示 `名字（buffId）`
2. 创建 Buff 时可自动填充递增 `buffId`
3. `AddBuffAction` 不再手填 `buffId`，应改为下拉或查询

### 11.2 节点编辑器体验

4. 右键创建 node 时，在鼠标位置创建
5. 左侧按钮创建 node 时，在当前画布视图中心创建

### 11.3 Unit 引入后的联动

6. 元技能动画从当前 `Unit` 对应动画目录取
7. `Skill / MetaSkill / State` 的查看都应以当前选中的 `Unit` 为上下文
8. 新建 `Skill / MetaSkill / State` 时，必须直接落在当前 `Unit` 的资源域下

### 11.4 Timeline 编辑体验

9. 后摇动画光标暂时取消可拖动判定，避免误拖

### 11.5 轨道与运行表现

10. 攻击盒和子弹缺少命中时特效与音效
11. 缺少摄像机轨道
12. 缺少特效轨道
13. 缺少摄像机资源配置项，并要与 `Unit` 关联

### 11.6 参数面板联动

14. `skillEvent` 下方的 `arg` 应根据 `eventType` 动态切换

---

## 12. 第二版开发原则

这一版最重要的原则只有三条：

1. 不再把系统仅仅理解成技能编辑器，而要把它理解成角色 3C 配置器。
2. `StateTimeline` 与 `MetaSkillTimeline` 必须保持统一实现思路，不能做成两套完全不同的系统。
3. `Unit / State / Skill / MetaSkill` 的职责必须清晰分层，但在运行时要通过统一接口整合起来。

补充三条落地原则：

4. 第二版资源入口先从 `UnitResource` 立住，先把“资源属于谁”解决，再谈状态与 AI 扩展。
5. 新系统默认面向 `AnimancerComponent` 收敛，`HybridAnimancerComponent` 只作为兼容层保留。
6. 第一阶段开发必须保护当前已经跑通的技能预览与释放链路，不为了结构理想化而把现有链路打断。

如果第二版按照这个方向推进，那么后续：

- 固定技能玩家角色
- AI 角色
- 可拆卸技能角色
- 动作与技能混合驱动角色

都可以落在同一套配置与运行时框架中。
