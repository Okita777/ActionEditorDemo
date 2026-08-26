# SkillRuntime第三版关键运行时数据结构

## 1. 文档目的
本文件定义第三版中 Skill / MetaSkill / State 及核心运行时对象的数据结构。

结构描述分为三层：
- 现状字段：当前工程已存在字段。
- V3目标字段：第三版需要新增或重命名的字段。
- 映射规则：如何从现状迁移到目标。


## 2. Skill层

### 2.1 SkillConfig（资源层）
现状字段（已存在）：
- SkillId
- SkillName
- SkillCategory
- Cooldown
- ComboContinuationTimeout
- ResourceCosts
- Layers
- Tags

V3目标字段：
- 保持不变。

映射规则：
- Skill 在 V3 仍是链路组织容器，不承担状态定义。
- SkillEvent 结构与求值逻辑保持不变。


### 2.2 SkillLayerConfig（资源层）
现状字段（已存在）：
- LayerIndex
- DisplayName
- EntryEditorPositionX / EntryEditorPositionY
- ExitEditorPositionX / ExitEditorPositionY
- MetaSkillNodes
- SkillEvents

V3目标字段：
- 保持不变。

映射规则：
- Layer 仍负责连段入口、节点关系与事件边。
- 不把状态迁移到 Layer 级别。


## 3. MetaSkill层

### 3.1 MetaSkillConfig（资源层）
现状字段（已存在）：
- MetaSkillId
- MetaSkillName
- AnimationClipPath
- Recovery（MetaSkillRecoveryConfig）
- OnAddEffect
- OnEndEffect
- Timeline
- Tags

V3目标字段（硬约束）：
- MetaSkillId
- MetaSkillName
- SkillStateTimeLineState（新增，第一段 State 绑定，类型为 StateConfig）
- RecoverySkillStateTimeLineState（新增，第二段 State 绑定，类型为 StateConfig）
- StatesOfCanInterrupt（新增，当前状态门禁白名单，可空）
- OnAddEffect
- OnEndEffect
- Tags

说明（避免 Timeline 歧义）：
- V3 不再存在独立 MetaSkillTimeline。
- SkillStateTimeLineState 内部的 StateTimeline 承载第一段行为轨（HitBox/Bullet/Event）。
- RecoverySkillStateTimeLineState 内部的 StateTimeline 承载第二段行为轨（HitBox/Bullet/Event）。

映射规则：
- 元技能不等价于 State。
- MetaSkill 结构上固定包含两段 State 字段：释放中 State 与后摇 State。
- V3 语义中移除 RecoveryAnim/Recovery 配置，不再作为 MetaSkill 目标结构字段。
- 迁移期仅用于兼容读取旧字段，最终数据结构不再保留 RecoveryAnim。
- OnAddEffect / OnEndEffect 继续归属于 MetaSkill，不下沉到 State。


### 3.2 StateTimeline承载行为轨（资源层）
现状字段（已存在）：
- StateTimelineConfig：Duration、Animation、Tracks、InterruptTracks、Interrupts
- Tracks 当前复用 MetaSkillTrackConfig 数据形态，包含 HitBoxes、Bullets、MetaSkillEvents

V3目标字段：
- 保持 StateTimelineConfig 为唯一行为时间轴容器。

映射规则：
- HitBox/Bullet/Event 能力由 SkillStateTimeLineState 与 RecoverySkillStateTimeLineState 的 StateTimeline 承载。
- 不再维护独立 MetaSkillTimeline 概念。


## 4. State层

### 4.1 StateConfig（资源层）
现状字段（已存在）：
- StateId
- StateName
- AnimationClipPath
- DefaultNextStateId
- Timeline
- Tags

V3目标字段：
- 保持不变（明确不新增 OnAddEffect / OnEndEffect）。

映射规则：
- State 仍是通用状态资源，不承载 MetaSkill 生命周期效果树。


### 4.2 StateTimelineConfig（资源层）
现状字段（已存在）：
- Duration
- Animation
- Tracks
- InterruptTracks
- Interrupts

V3目标字段：
- 保持不变。

映射规则：
- V3 主动打断与状态打断仍依赖 Interrupt 结构。
- 上轨优先于下轨，同轨按插入顺序求值。


## 5. Runtime对象层

### 5.1 SkillContext（运行时共享上下文）
现状字段（已存在）：
- Caster
- EquippedWeapon
- PrimaryTarget
- SkillConfig
- CurrentMetaSkillConfig
- CurrentStateConfig
- StateController
- ActiveBuffSourceId
- ActiveBuffInstance
- LastEffectResult
- Blackboard
- EffectExecutor / BuffService / TagQueryService / ResourceService / CharacterActionBridge / CombatResolver / RuntimeObserver

V3目标字段（建议新增）：
- SkillFlowContext（新增，当前技能链上下文，跨多个MetaSkill/多个技能State持续存在）
- CurrentSkillRuntimeId（可选，用于调试区分并行技能实例）
- CurrentMetaSkillRuntimeId（可选）
- SkillInterruptedByState（可选，调试位）

映射规则：
- SkillContext 保持为技能与状态的协作桥。
- SkillFlowContext 不跟随单个技能State生命周期销毁，避免状态被打断后技能流上下文丢失。
- 额外调试字段只用于诊断，不作为判定源。


### 5.2 SkillRuntime（技能执行器）
现状职责：
- 基于 SkillEvent 在 MetaSkillNode 之间切换。
- 驱动当前 MetaSkillRuntime。
- 处理技能 CD / 资源消耗 / 连段超时。

V3目标职责变化：
- 保持 SkillEvent 组织与跳转逻辑不变。
- 在触发节点前增加状态门禁：
  1) 输入
  2) CD
  3) 资源
  4) 若 MetaSkill 配置了 SkillStateTimeLineState，则校验当前 State 是否允许被该技能打断
  5) 通过后执行 useMetaSkill，并请求 StateController 切到对应技能 State
- useMetaSkill 成功启动时执行 OnAddEffect。
- 消费来自 StateController 的状态变化通知，判断当前 MetaSkill 是正常结束还是被状态中断。
- 当技能 State 被中断时，更新技能流为“MetaSkill interrupted end”，跳过 OnEndEffect，并根据连段规则决定是否开放下一段输入。
- 状态被中断不等价于技能链终止。


### 5.3 MetaSkillRuntime（元技能执行器）
现状职责：
- 执行 OnAddEffect / OnEndEffect。
- 驱动旧版 MetaSkill Timeline/Recovery 语义。

V3目标职责变化：
- 不再执行独立 MetaSkillTimeline。
- SkillStateTimeLineState 与 RecoverySkillStateTimeLineState 的 StateTimeline 负责行为执行。
- RecoveryAnim 语义从 MetaSkillRuntime 主流程移除，后摇由 RecoverySkillStateTimeLineState 承接。
- MetaSkillRuntime 只保留 MetaSkill 级效果树与阶段状态记录，不再复制 StateController 的中断、自然结束、default next 逻辑。
- OnEndEffect 只在正常结束路径触发；被状态中断时不触发。


### 5.4 StateController（状态执行器）
现状职责：
- 维护 CurrentState。
- 执行 Interrupt / ExternalTry / ExternalForce / DefaultNext / SkillDriven 切换。
- 驱动状态动画与状态标签。

V3目标职责变化：
- 接收来自 SkillRuntime 的 SkillDriven 请求。
- 持有由技能装配阶段构建出的技能 tmpState，并将其纳入统一状态集合。
- 在技能 State 进入、正常结束、被打断、切出时，向 SkillRuntime 发出带上下文的状态变化通知。
- 明确区分两类结果：
  - StateInterrupted：仅状态被打断。
  - SkillChainTerminated：技能链被显式终止。
- 外部观察与日志必须能区分上述两类结果。


## 6. V3新增建议结构

### 6.0 SkillFlowContext（新增运行时结构）
建议字段：
- SkillId
- SkillRuntimeId
- CurrentMetaSkillId
- CurrentNodeId
- ActiveStateId
- PhaseRole（Execute / Recovery / None）
- EndReason（Normal / Interrupted / Cancelled / Timeout）
- IsContinuationWindowOpen
- LastInterruptSourceStateId

用途：
- 表示一条技能链当前的运行上下文。
- 让技能 state 被中断后，技能流仍能保留并继续匹配下一段输入。
- 作为 SkillRuntime 与 StateController 之间的关联载体。

### 6.0.1 SkillStateNotification（新增运行时结构）
建议字段：
- SkillId
- MetaSkillId
- StateId
- PhaseRole
- TransitionKind（Entered / Completed / Interrupted / Exited）
- SourceStateId
- TargetStateId
- Timestamp

用途：
- 作为 StateController 回通知 SkillRuntime 的标准消息。
- 让技能系统能区分当前 MetaSkill 是正常结束还是被状态中断。

### 6.1 MetaSkillStateBindingConfig（新增资源子结构）
建议字段：
- SkillStateTimeLineState
- RecoverySkillStateTimeLineState
- AutoLinkRecoveryToIdle（可选，默认 true）

用途：
- 明确承载 MetaSkill 与 State 的绑定关系。
- 减少在 MetaSkillConfig 顶层堆字段的风险。


### 6.2 RuntimeMetaSkillStateBinding（新增运行时结构）
建议字段：
- UnitId
- SkillId
- MetaSkillId
- SkillStateRuntimeId
- RecoveryStateRuntimeId
- CreatedAt
- Version

用途：
- 表示一次角色装配后的动态状态映射。
- 支持增量重建与回滚。


## 7. 迁移映射总表

### 7.1 Recovery迁移
- 旧字段来源：MetaSkillConfig.Recovery.AnimationClipPath + Recovery.Animation。
- 新语义：RecoverySkillStateTimeLineState。
- 迁移方式：
  - 若旧 Recovery 有效，则生成或绑定 RecoveryState。
  - 释放中状态 DefaultNextStateId 自动指向 RecoveryState。
  - RecoveryState 默认后继为 idle。


### 7.2 tmpState生成
- 触发条件：MetaSkill 配置 SkillStateTimeLineState。
- 生成时机：角色装备技能或技能装配发生变化时。
- 唯一键建议：unitId + skillId + metaSkillId + stateRole。
- stateRole 取值：Skill 或 Recovery。


### 7.3 OnEndEffect触发规则
- 正常结束：触发。
- 被打断结束：不触发。

### 7.4 Skill 与 State 通信映射
- Skill -> State：useMetaSkill 后，请求 StateController 切到对应技能 tmpState。
- State -> Skill：技能 tmpState 进入、正常结束、被打断后，StateController 通过 SkillStateNotification 回通知 SkillRuntime。
- SkillRuntime 基于通知更新 SkillFlowContext，并决定是否允许继续接下一段 MetaSkill。


## 8. 数据结构约束
- StateConfig 结构不改形态。
- SkillEvent 结构不变。
- MetaSkill 与 State 为关联关系，不是等价关系。
- 可空绑定必须可表达“只放行为、不切状态”的 MetaSkill。
- SkillFlowContext 生命周期独立于单个技能 State 生命周期。
- OnAddEffect / OnEndEffect 仍属于 MetaSkill，不下沉到 State。


## 9. 实施检查点
- 能表达无状态 MetaSkill。
- 能表达单状态 MetaSkill。
- 能表达双状态（释放中+后摇）MetaSkill。
- 被打断时 OnEndEffect 不触发。
- 状态中断后连段事件仍可继续匹配。
- StateController 能把技能 State 的中断结果准确回通知给 SkillRuntime。