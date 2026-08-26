# SkillRuntime第三版框架说明

## 1. 背景与目标
第三版目标是让Skill与State互相接入，但不破坏既有Skill组织形式和SkillEvent链路。

本版关键修正：
- 元技能不等价于State。
- 只有配置了技能时间线State的元技能，才会在角色装备完成后生成并注册对应tmpState。
- 不再存在独立MetaSkillTimeline；HitBox/Bullet/Event统一由StateTimeline承载。
- State数据结构保持不变，不向State本体添加EffectTreeOnAdd或EffectTreeOnEnd。
- Skill 与 State 通过“技能请求切状态，状态回通知技能结果”的双向通信机制协作。

## 2. 核心概念

### 2.1 Skill与MetaSkill
- Skill用于组织连段和触发关系，组织形式保持不变。
- MetaSkill是Skill节点运行单元，包含释放判定所需元数据与效果树数据。

### 2.2 MetaSkill与State的关系
- MetaSkill并非State。
- MetaSkill可选携带两个State字段：
	- SkillStateTimeLine(State)：技能释放中状态。
	- RecoverySkillStateTimeLine(State)：技能后摇状态。
- 运行时规则：
	- 仅当MetaSkill配置了SkillStateTimeLine(State)时，才生成对应tmpState并参与状态切换。
	- 这些tmpState在技能装备完成后就应被构建并注册到StateController下，而不是在释放瞬间私自运行。
	- 若未配置State，则释放该MetaSkill时不触发状态变化，仅走技能逻辑与效果树。

### 2.3 tmpState
- tmpState是运行时动态State实例，不是资源层新类型。
- tmpState由角色已装配技能的MetaSkill构建并挂到对应角色的StateController之下。
- tmpState一旦进入运行，其生命周期与普通State一致，由StateController统一管理。

### 2.4 Skill 与 State 的通信
- SkillRuntime负责判定技能是否可以释放，并在useMetaSkill时请求进入对应技能State。
- StateController负责执行技能State的运行、打断、自然结束和DefaultNext切换。
- StateController必须把技能State的进入、正常结束、被打断结果回通知给SkillRuntime。
- SkillRuntime根据这些通知决定：是否跳过OnEndEffect、是否开放下一段连段输入、是否终止技能链。

## 3. 数据结构约束

### 3.1 MetaSkill数据结构（第三版）
- metaSkillId
- metaSkillName
- metaSkillTags
- statesOfCanInterrupt
- effectsTreeOnAdd
- effectsTreeOnEnd
- skillStateTimeLine(State)
- recoverySkillStateTimeLine(State)

说明：
- RecoveryAnim从原SkillTimeLine动画配置中移除，改为独立RecoverySkillStateTimeLine(State)。
- 若配置了RecoverySkillStateTimeLine(State)，则形成两段状态链路：释放中State -> 后摇State。
- 第一段与第二段的HitBox/Bullet/Event均由各自StateTimeline执行，不再由独立MetaSkillTimeline执行。

### 3.2 State数据结构
- State结构保持现状不变。
- 不向State结构新增effectsTreeOnAdd或effectsTreeOnEnd字段。

## 4. 编辑器方案

### 4.1 元技能Inspector调整
- 元技能Inspector内直接嵌入两个State编辑区块：
	- SkillStateTimeLine对应释放中State。
	- RecoverySkillStateTimeLine对应后摇State。
- 这两个区块展示State Inspector的完整可编辑项。

### 4.2 自动字段规则
- 在元技能内编辑State时：
	- stateId和stateName不手填，由skillId和skillName派生生成。
	- nextDefaultState不在编辑器显式配置：
		- 若存在RecoveryState，则释放中State的默认后继固定为RecoveryState。
		- RecoveryState默认后继通常配置为idle，但runtime仍按State自身DefaultNextStateId解释。

### 4.3 主动技能打断配置交互规则
- 不使用旧版单下拉配置。
- 使用“+”按钮逐条添加打断配置项。
- 每条项包含：
	- 打断目标状态。
	- 打断优先级插入位置。
- 插入位置下拉规则：
	- 第一项“首”，表示最高优先级。
	- 后续项为该目标状态已有打断轨中的打断项。
	- 选择已有项表示插入到该项之后一个优先级位置。

## 5. 运行时构建

### 5.1 BuildState
- 角色加载时：
	- 读取装配技能。
	- 收集技能下MetaSkill。
	- 收集MetaSkill中配置的State字段并构建tmpState。
- 将这些tmpState注册到StateController的可切换状态集合中。
- 该构建只负责新增/更新自身tmpState，不改写静态State打断轨配置。

### 5.2 增量更新
- 技能装配变化后执行增量更新。
- tmpState唯一键使用unitId + skillId + metaSkillId稳定生成并复用。

## 6. 技能释放流程
释放判定顺序固定：
1. 输入判定。
2. 技能CD判定。
3. MetaSkill资源cost判定。
4. 若MetaSkill含State，判定当前State是否可被该技能打断（按statesOfCanInterrupt）。
5. 全部通过后执行useMetaSkill。
6. useMetaSkill先执行OnAddEffect，再向StateController发起进入对应tmpState的请求。
7. StateController接受请求后，技能State进入正常State运行流程。

说明：
- 当前State可否打断只是“技能能否释放”的一个环节，不是唯一条件。
- SkillRuntime负责发起请求，不负责接管技能State后续的interrupt/default-next逻辑。

## 7. 中断语义

### 7.1 状态被中断
- 中断的是“技能释放中State/后摇State”本身。
- 不是整个Skill链路被强制终止。
- StateController需要把“哪个MetaSkill的哪个State被中断”回通知给SkillRuntime。
- SkillRuntime收到该通知后，要把当前MetaSkill标记为中断结束，并据此开放后续连段技能的继续释放。
- 因此，后续连段技能仍可按既有SkillEvent逻辑继续释放。

### 7.2 被中断时效果树规则
- 当技能State被中断时，不触发OnEndEffectsTree。
- OnEndEffectsTree仅在正常结束路径触发。
- OnAddEffect仍在useMetaSkill启动成功时触发，不受后续是否被中断影响。

### 7.3 打断优先级规则
- 同一状态下，上层打断轨优先级高于下层。
- 同轨内按插入后的顺序评估，越靠前优先级越高。

## 8. Recovery处理
- Recovery抽象为独立State，不是子状态标记。
- 释放中State结束后进入RecoveryState。
- RecoveryState结束后按State自身DefaultNextStateId进入下一个状态；若项目规则默认配置为idle，则表现为回idle。

## 9. Skill逻辑保持项
- SkillEvent逻辑保持不变。
- Skill组织形式保持不变。
- Skill层Layer机制（例如充能型技能）保持不变，本次改版不改其语义。
- 但SkillRuntime需要新增对State回通知的消费逻辑，用于更新MetaSkill结束方式与continuation窗口。

## 10. 主动与被动
- 不再按“主动元技能/被动元技能”二分MetaSkill类型。
- 是否改变状态完全由MetaSkill是否配置State决定。
- 未配置State的MetaSkill可用于仅触发效果树的链路。

## 11. 风险与控制
- 配置迁移风险：旧RecoveryAnim数据需迁移到RecoverySkillStateTimeLine。
控制：提供一次性迁移脚本与迁移报告。

- 运行时语义风险：状态被中断但技能链不断，可能与旧行为预期冲突。
控制：增加调试事件，区分“状态中断”与“技能链终止”。

- 优先级插入风险：插入位置规则实现不一致会导致线上行为漂移。
控制：保存插入基准ID并在运行时统一重排。

## 12. 验收标准
- MetaSkill未配置State时可正常释放且不触发状态切换。
- MetaSkill配置SkillStateTimeLine时能在装备后生成tmpState，并在释放时进入该tmpState。
- MetaSkill配置RecoveryState时可按规则进入后摇State，并按其DefaultNextStateId离开。
- 状态被中断时不触发OnEndEffectsTree。
- 状态被中断不阻断后续连段SkillEvent。
- 状态被中断后，SkillRuntime能收到对应MetaSkill的中断通知并正确开放下一段技能输入。
- 上下轨与插入顺序优先级一致生效。
- BuildState不会污染静态State配置。

## 13. 备注
本文件是第三版实施基线。若后续规则调整，统一追加版本差异段，避免文档与实现分叉。
