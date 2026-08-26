# SkillRuntime 当前 okita 问题清单

本文档只做问题收集与归类，不在这里直接给出最终代码修改方案。
目标是把当前你已经在代码里标出来的疑问、异议和设计分歧统一收拢，作为后续逐脚本清理的任务索引。

## 1. 当前问题的总体类型

从现有 `okita:` 标注看，问题主要集中在以下几类：

1. 职责越界
   - 某个类承担了超过其定位的职责，尤其是 `SkillPlayerController`、`MetaSkillRuntime`、`StateTimelineExecutionRuntime`。
2. legacy 路径残留
   - 旧版技能槽位、旧版执行习惯、兼容保护逻辑仍在干扰当前架构。
3. 保护性分支过多
   - 大量“兜底继续跑”的写法让真正的配置错误、运行时错误被吞掉，导致问题难暴露。
4. 运行时语义不清
   - 包括 recovery、interrupt、continuation、cancelled 等概念边界不稳定。
5. 输入/状态/技能三套系统边界混杂
   - `SkillRuntime`、`SkillPlayerController`、`StateController` 之间仍有一些控制权重叠。
6. 目标实体模型不统一
   - 当前很多地方仍在直接处理 `GameObject`，而不是稳定落到 `CharacterObject` 这一层。
7. 编辑器/预览遗留入侵运行时
   - 预览单位、旧挂点逻辑、旧武器/插槽读取方式仍影响正式运行时设计。

## 2. 按脚本归类的问题清单

## 2.1 CharacterObject.cs

当前观察：`CharacterObject` 的方向更像“角色实体门面”，但内部又强依赖 `SkillPlayerController` 提供技能相关能力。

现有问题点：

1. 角色身份与技能入口的关系还不够稳定。
   - `CharacterObject` 想代表“这是一个合法角色实体”。
   - 但技能、目标、运行时能力大量仍通过 `SkillPlayerController` 或 `GameObject` 直接访问。
2. `CharacterObject` 目前承担了门面职责，但没有成为全系统唯一实体入口。
   - 属性、buff、tag、技能槽位查询都在这里聚合。
   - 但其他系统并没有强制通过它访问角色能力。
3. `CharacterObjectResolver` 方向是对的，但落地还不彻底。
   - 很多地方虽然最终会 `Resolve`，但系统内仍保留“直接接受 object/GameObject 也能工作”的思路。
4. `CharacterBuffContainer` 和角色本体绑得很紧，但其生命周期仍由 `CharacterObject.Update()` 驱动。
   - 这意味着 `CharacterObject` 已经是单位级运行时宿主，而不是单纯数据门面。

## 2.2 SkillPlayerController.cs

当前观察：这个类实际上已经是单位级战斗运行时装配入口，而不只是“技能播放器”。

现有问题点：

1. 定位膨胀。
   - 它不只负责技能输入和技能运行时。
   - 还负责：
     - 构建全部技能运行时
     - 装配被动技能
     - 合并动态授予技能
     - 构建共享 `StateController`
     - 构建共享 `SkillContext`
     - 每帧推进技能与状态
     - 聚合输入快照/命中快照/BreakValue
2. legacy 技能槽位回退还在。
   - `BuildLegacyRuntimeStates()` 以及预览缺失时回退 `_skillSlots` 的机制仍在。
3. `Reload()` 是大而全重建入口。
   - 可维护性和性能关注点都集中在这里。
4. 运行时服务定位不清。
   - `ResolveBattleResolver()`、`ResolveCharacterActionBridge()` 等服务解析逻辑都放在这里。
5. 它在直接感知 `CharacterObject`。
   - `CaptureBreakValue()` 会主动查 `CharacterObject`。
   - 这说明它已经不是纯技能模块，而是在回头读“角色实体层”。
6. 输入采集、技能装配、状态装配三种职责耦合在一个组件中。

## 2.3 SkillRuntime.cs

当前观察：`SkillRuntime` 已经比之前干净很多，但仍存在一些结构复杂、保护性过强和语义绕的区域。

现有问题点：

1. 自打断与 continuation 逻辑仍偏复杂。
2. 节点切换失败后回滚的流程较绕。
3. 部分空值保护掩盖了真正不该发生的错误。
4. `EnsureStateControllerSubscription()` 这种补偿式设计不够干净。
5. `HandleSkillStateCompleted()` / `HandleSkillStateInterrupted()` 的结构仍可继续统一。
6. `SkillFlowContext` 的使用成本较高，当前阅读负担大。
7. 一些调试黑板、日志和失败原因保留了太多中间层痕迹。

## 2.4 MetaSkillRuntime.cs

当前观察：这个类的主要收缩已经完成，但仍有后续要确认的点。

现有问题点：

1. 当前版本已经收缩成最小壳，这是正确方向。
2. 仍需后续确认非状态驱动 metaskill 是否最终也要进一步收口或拆分。
3. 结束效果命名语义仍建议后续更明确区分：
   - `OnAddEffect`
   - execute 正常结束效果
   - metaskill 外部退出效果

## 2.5 StateController.cs

当前观察：状态控制权已经基本集中，但实现上还有不少你质疑的复杂度点。

现有问题点：

1. 自然结束保护逻辑偏多。
2. 动画同步策略仍未最终定论。
3. `DefaultNext` 的 fallback 机制需要重新核对是否过度保护。
4. interrupt 评估里仍有你认为不需要的“延迟中断”逻辑。
5. `CommitTransition()` 的事件发射流程虽然语义正确，但阅读成本高。
6. 一些 helper 的时间比较和 duration 规则不够直观。
7. 存在潜在对象分配/对象池问题，但这是次级优化，不该先于结构清理。

## 2.6 StateTimelineExecutionRuntime.cs

当前观察：这是当前遗留问题最密集的类之一。

现有问题点：

1. recovery carryover 机制与你当前设计冲突。
2. 仍存在 execute/recovery 手动衔接思想的遗留。
3. `TriggerMetaSkillEvent(...)` 等旧语义残留可疑。
4. 0 时长攻击盒支持与你的配置约束冲突。
5. 挂点解析逻辑放置位置不合理。
6. 目标命中记录仍以 `GameObject` 为核心，而不是实体级抽象。
7. 预览/烘焙相关逻辑仍混入正式运行时判断语义。

## 2.7 SkillContext.cs

当前观察：`SkillContext` 现在是跨技能/状态/效果链路的共享执行上下文，但其中部分字段语义还未最终稳定。

现有问题点：

1. `Caster`、`PrimaryTarget` 等字段仍然是 `object`。
2. `SkillMetaEndReason.Cancelled` 的语义不清晰，当前没有稳定使用场景。
3. `SkillContext` 作为大上下文对象，未来可能需要区分：
   - 角色级上下文
   - 技能级上下文
   - 状态级上下文
   - buff/effect 临时上下文

## 2.8 SkillTargetResolver.cs 与目标解析链

当前观察：这里已经开始往 `CharacterObject` 靠，但还没完全统一。

现有问题点：

1. 当前做法是：优先 `Resolve(CharacterObject)`，失败后回退原始 `object`。
2. 这说明系统仍在容忍“不是角色实体也能作为目标”的执行模型。
3. 如果你的设计目标是“合法角色目标必须体现为 `CharacterObject`”，那这条链路后续应继续收紧。

## 3. 当前最值得优先处理的结构问题

如果按架构影响范围排序，当前最应该优先统一的问题是：

1. `CharacterObject` 与 `SkillPlayerController` 的职责边界
2. 运行时目标实体统一以 `CharacterObject` 为准，而不是 `GameObject/object`
3. `StateTimelineExecutionRuntime` 中 recovery 与 carryover 相关遗留
4. `SkillPlayerController` 的单位级装配职责拆分
5. `SkillRuntime` 中 continuation / rollback / 保护性分支继续收口

## 4. 后续建议的清理顺序

建议按下面顺序逐脚本推进：

1. 先定 `CharacterObject` 与 `SkillPlayerController` 的边界
   - 这是整个“角色实体模型”与“技能系统入口”的总开关。
2. 再统一目标解析与上下文实体语义
   - 明确什么时候必须是 `CharacterObject`，什么时候允许纯 `GameObject`。
3. 再清 `SkillPlayerController`
   - 把不属于“技能控制器”的职责拆出去。
4. 再清 `StateTimelineExecutionRuntime`
   - 去掉和 recovery/carryover 相关的剩余越界逻辑。
5. 最后做 `SkillRuntime` 与 `StateController` 的局部收口
   - 这时很多复杂保护逻辑会自然消失。

## 5. 结论

你现在标出来的问题并不是零散小问题，而是比较集中地指向同一件事：

当前运行时系统正在从“技能驱动的一坨逻辑”过渡到“角色实体 + 技能系统 + 状态系统 + buff系统分层明确”的架构。

所以后面的逐脚本清理，不建议只盯代码风格或单点 bug，而应该持续围绕以下三条主线：

1. 谁才是单位级总入口。
2. 谁才有资格代表一个合法角色实体。
3. 状态、技能、buff、目标解析分别该归谁管。

tip:1.有的地方可以用对象池优化性能
2.黑板变量到底有无必要存在
3.后摇等函数残留
4.一些保护性代码没必要存在
5.零散的问题一点点解决
