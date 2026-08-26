# SkillRuntime第三版改动点与实施策略风险评估

## 1. 目标与边界
目标：在不破坏 SkillEvent 组织形式的前提下，实现 MetaSkill 与 State 的第三版关系重构。

硬约束：
- 元技能不等价于 State。
- MetaSkill 固定包含两段 State 字段：SkillStateTimeLineState 与 RecoverySkillStateTimeLineState。
- RecoveryAnim/Recovery 不再作为目标数据结构字段。
- 不再存在独立 MetaSkillTimeline，HitBox/Bullet/Event 由两段 State 的 StateTimeline 承载。
- State 结构不新增 OnAddEffect / OnEndEffect。
- 状态被打断不等于整条技能链终止。
- 状态被打断时不触发 OnEndEffectsTree。
- Skill 与 State 必须建立双向通信机制：Skill 请求切状态，State 回通知技能阶段结果。


## 2. 改动点清单

### 2.1 资源层改动
需修改：
- MetaSkillConfig

改动内容：
- 新增 SkillStateTimeLineState（第一段 State）。
- 新增 RecoverySkillStateTimeLineState（第二段 State）。
- 新增 StatesOfCanInterrupt（可空列表）。
- 明确移除 RecoveryAnim/Recovery 作为目标结构字段（迁移期仅做兼容读取）。

需新增：
- 可选新增 MetaSkillStateBindingConfig 子结构（若决定做结构收敛）。

不改动：
- SkillConfig / SkillLayerConfig / SkillEventConfig。
- StateConfig / StateTimelineConfig 主体结构。


### 2.2 编辑器层改动
需修改：
- MetaSkill Inspector 与对应序列化绘制器。
- 状态打断配置面板交互。

改动内容：
- 在 MetaSkill Inspector 中增加两段 State 绑定区：
  - SkillStateTimeLineState
  - RecoverySkillStateTimeLineState
- 打断配置改为 + 按钮逐条插入。
- 插入位置规则：首 或 插入到指定项之后。
- 保存插入锚点，保证重排一致性。


### 2.3 运行时改动
需修改：
- SkillRuntime
- MetaSkillRuntime
- StateController
- SkillPlayerController（状态构建与增量刷新入口）

改动内容：
- SkillRuntime 释放门禁顺序固化：输入 -> CD -> 资源 -> 状态可打断校验 -> 释放。
- SkillDriven 状态切换接入：基于 MetaSkill 的 SkillStateTimeLineState / RecoverySkillStateTimeLineState 两段状态。
- 在 useMetaSkill 中固定执行顺序：建立技能流上下文 -> OnAddEffect -> 请求 StateController 切到技能 State。
- RecoveryAnim 主语义从运行时主流程移除，后摇统一走 RecoveryState。
- 原 MetaSkillTimeline 执行能力迁入两段 StateTimeline，不再保留独立执行路径。
- 中断语义拆分：StateInterrupted 与 SkillChainTerminated。
- StateController 增加技能 State 结果通知：Entered / Completed / Interrupted / Exited。
- SkillRuntime 消费状态通知后，更新当前 MetaSkill 的结束方式，并决定是否开放下一段连段输入。
- OnEndEffect 触发只保留正常结束路径。


### 2.4 调试与可观测性改动
需新增：
- 关键 trace 事件：
  - SkillBlockedByStateGate
  - SkillStateEntered
  - StateInterrupted
  - MetaSkillInterruptedEnd
  - SkillChainContinuedAfterInterrupt
  - OnEndEffectSkippedByInterrupt

改动内容：
- 统一输出节点、状态、时间轴时间、触发原因。


### 2.5 迁移工具改动
需新增：
- 一次性迁移脚本（Editor 菜单命令）。
- 迁移报告（成功/失败/待人工处理）。

迁移输入：
- 旧 MetaSkill.Recovery。

迁移输出：
- 新 RecoverySkillStateTimeLineState 映射。
- 必要时生成临时 RecoveryState 资源或绑定到既有状态。


## 3. 分阶段实施策略

### Phase 0：基线冻结
产出：
- 冻结第三版规则文档。
- 建立回归样例清单（至少覆盖无状态、单状态、双状态三类技能）。

出口条件：
- 团队确认字段名、语义名、日志名不再变化。


### Phase 1：数据结构与序列化
范围：
- MetaSkillConfig 字段新增与兼容读写。
- ScriptableObject 序列化向后兼容。

关键动作：
- 给新字段提供默认值。
- 保留旧字段并加 Deprecated 注释与迁移标记。

出口条件：
- 老资源可读取，新资源可保存，域重载无报错。


### Phase 2：编辑器交互
范围：
- MetaSkill Inspector 新面板。
- 打断 + 插入优先级配置。

关键动作：
- 插入操作写入稳定排序键。
- 提供重排后可视化校验。

出口条件：
- 同一配置多次打开保存后顺序稳定。


### Phase 3：运行时接线
范围：
- SkillRuntime 状态门禁。
- StateController SkillDriven 接口与语义分离。
- MetaSkillRuntime 中断结束规则调整。

关键动作：
- 先补 SkillFlowContext 与状态通知结构，再改主流程接线。
- 先让 StateController 成为唯一技能 State 生命周期执行中心，再删 MetaSkillRuntime 内部复制逻辑。
- 先加兼容分支，再切默认分支。
- 保留旧逻辑开关用于回滚。

出口条件：
- 三类回归样例全部通过。


### Phase 4：迁移与灰度
范围：
- 迁移脚本执行。
- 迁移报告核验。

关键动作：
- 按目录分批迁移。
- 先在样例资源与测试资源上演练。

出口条件：
- 关键技能迁移成功率达到 100%，无阻断错误。


### Phase 5：清理与收口
范围：
- 清理临时兼容代码。
- 更新文档与测试基线。

出口条件：
- 运行时默认走 V3 路径，兼容开关仅保留一版。


## 4. 风险评估

### 高风险
1. 语义回归风险：状态中断后技能链是否继续
- 影响：连段行为与旧版本可能差异明显。
- 缓解：新增显式 trace，做技能链连续性专项回归。

2. 通信丢失风险：技能 State 被打断后，SkillRuntime 没有准确收到状态结果
- 影响：OnEndEffect 误触发、连段窗口无法提前开放、技能卡死在错误阶段。
- 缓解：为 State -> Skill 通知建立统一结构和日志，并在回归中覆盖 Entered / Completed / Interrupted 三种路径。

3. 迁移风险：旧 Recovery 数据不完整
- 影响：迁移后后摇丢失或时序错误。
- 缓解：迁移报告标记需人工处理项，禁止静默失败。

4. 排序风险：打断插入优先级漂移
- 影响：线上对战/手感不稳定。
- 缓解：保存稳定锚点 + 运行时统一排序 + 回归快照比对。


### 中风险
1. 双路径并存风险（兼容逻辑与V3逻辑）
- 缓解：所有入口统一从策略层分流，避免散落 if 分支。

2. 调试复杂度上升
- 缓解：建立统一 Debug 面板字段与事件字典。


### 低风险
1. 资源字段扩展导致 Inspector 复杂度上升
- 缓解：折叠分组与默认隐藏高级项。


## 5. 测试策略

### 5.1 功能回归矩阵
- Case A：MetaSkill 无状态绑定。
- Case B：MetaSkill 仅 SkillState 绑定。
- Case C：MetaSkill 同时绑定 SkillState + RecoveryState。
- Case D：状态被打断但后续连段继续。
- Case E：状态被打断时 OnEndEffect 不触发。
- Case F：上轨高于下轨、同轨插入顺序生效。
- Case G：MetaSkill1 的 skillState 被打断后，SkillRuntime 能立即开放 MetaSkill2 的 continuation 输入。
- Case H：StateController 对技能 State 的 Entered / Completed / Interrupted 通知与实际状态切换一致。


### 5.2 自动化建议
- PlayMode：技能释放门禁顺序。
- PlayMode：状态中断与技能链分离语义。
- PlayMode：技能 State 中断后，State -> Skill 通知是否准确驱动 continuation 窗口。
- EditMode：资源迁移脚本输入输出校验。


### 5.3 验收指标
- 关键样例技能通过率 100%。
- 迁移后资源人工复核通过率 100%。
- 无新增阻断级运行时异常。


## 6. 回滚策略
- 保留旧 Recovery 逻辑开关直到迁移验证完成。
- 配置层支持按资源粒度回退到旧解释路径。
- 迁移脚本生成前后快照，支持单资源回滚。


## 7. 最终交付物
- 新版字段与序列化实现。
- 编辑器交互改造（含打断插入配置）。
- 运行时接线改造。
- 迁移脚本与迁移报告。
- 回归测试用例与结果记录。
- 文档更新（框架说明、数据结构、实施策略）。