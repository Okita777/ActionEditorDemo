# State 运行时设计文档

## 1. 目的

这份文档用于先定义第二版 `State` 的运行时模型，再反推编辑器与打断轨的最终形态。

原因很直接：

1. `State` 的核心不是资源入口，也不是时间轴 UI。
2. `State` 的核心是“状态机如何在运行时切状态”。
3. `打断轨` 本质上是运行时状态转移规则，而不是单纯的编辑器轨道装饰。

因此这里的原则是：

1. 先把运行时切换语义写清楚。
2. 再根据运行时语义校正 `StateTimeline` 的数据结构与编辑器细节。
3. 避免先把编辑器做死，后面为了 runtime 再回头拆数据。

---

## 2. 设计依据

### 2.1 来自当前框架文档的约束

根据 [SkillRuntime第二版框架说明.md](d:/myprojects/unity/projects/ActionEditor/Assets/doc/SkillRuntime第二版框架说明.md)：

1. `State` 需要正式运行时对象，而不是只停留在资源层。
2. 外部必须暴露两类状态切换接口：
   - `TryChangeState`
   - `ForceChangeState`
3. `打断轨道` 只定义状态内部可转移能力。
4. `MetaSkill.changeStateTo` 最终要接入状态系统。
5. `StateTimeline` 与 `MetaSkillTimeline` 必须共享统一实现思路，不能变成两套完全不同的 runtime。

### 2.2 来自 AsiActionEditor 的直接参考

运行时设计主要参考以下实现：

1. [ActionState.cs](d:/myprojects/unity/projects/ActionEditor/Assets/Plugins/AsiActionEditor/Engine/RunTime/Script/AsiActionEditor/RunTime/Script/UnitController/ActionState.cs)
2. [ActionInterrupt.cs](d:/myprojects/unity/projects/ActionEditor/Assets/Plugins/AsiActionEditor/Engine/RunTime/Script/AsiActionEditor/RunTime/Script/UnitController/Interrup/ActionInterrupt.cs)
3. [ActionStateMachine.cs](d:/myprojects/unity/projects/ActionEditor/Assets/Plugins/AsiActionEditor/Engine/RunTime/Script/AsiActionEditor/RunTime/Script/UnitController/ActionStateMachine/ActionStateMachine.cs)
4. [ActionStatePart.TimeLine.Interrup.cs](d:/myprojects/unity/projects/ActionEditor/Assets/Plugins/AsiActionEditor/Engine/RunTime/Script/AsiActionEditor/RunTime/Script/UnitController/ActionStateMachine/Part/ActionStatePart.TimeLine.Interrup.cs)
5. [ActionStatePart.TimeLine.End.cs](d:/myprojects/unity/projects/ActionEditor/Assets/Plugins/AsiActionEditor/Engine/RunTime/Script/AsiActionEditor/RunTime/Script/UnitController/ActionStateMachine/Part/ActionStatePart.TimeLine.End.cs)

从这些实现里可以提炼出最重要的结论：

1. 每个状态自身持有“默认去向 + 打断规则”。
2. 当前状态在 `Update` 过程中持续扫描可用的打断条目。
3. 打断条目命中后，不是直接改字段，而是调用统一的状态切换入口。
4. 状态自然结束后，如果配置了默认去向，则也走统一切换入口。

这套思路必须保留。

---

## 3. 运行时总目标

第二版 `State` runtime 要解决四个问题：

1. 角色当前处于哪个 `State`。
2. 当前 `State` 的动画与时间轴如何推进。
3. 什么时候允许从当前 `State` 转移到别的 `State`。
4. 外部系统如何安全地请求切状态。

对应到系统职责：

1. `StateController` 负责状态切换。
2. `StateTimelineRuntime` 负责状态时间轴推进。
3. `StateInterruptEvaluator` 负责打断条件判定。
4. `SkillCharacterActionBridge` 继续负责动画播放与同步。

---

## 4. 核心原则

### 4.1 内部转移和外部转移必须统一管线

内部打断和外部接口看起来来源不同，但最终都必须走同一条切换管线。

原因：

1. 否则状态退出、事件清理、动画切换、时间轴终止逻辑会出现两套实现。
2. 一旦两套逻辑分叉，后面最容易出现“打断切状态正常，外部切状态残留旧状态 runtime”的问题。

因此：

1. 内部打断命中后，生成 `StateTransitionRequest`。
2. 外部 `TryChangeState / ForceChangeState` 也生成 `StateTransitionRequest`。
3. 最终都由 `StateController.CommitTransition(...)` 处理。

### 4.2 打断轨是运行时规则，不是编辑器附属物

`打断轨` 的本质定义是：

1. 何时可切。
2. 切到哪里。
3. 需要满足哪些条件。

所以编辑器必须服从 runtime 数据模型，而不是 runtime 去迁就一版临时编辑器数据。

### 4.3 先不做打断组，但数据结构要避免再次返工

当前阶段不做 `AsiActionEditor` 的 `打断组`。

但是为了避免再次返工，运行时数据层不能再假设：

1. `StateConfig.Timeline` 永远等于 `MetaSkillTimelineConfig`
2. `打断条目` 只有 `TriggerTime + Duration + TargetStateId`

如果现在就把 schema 收得过窄，后面一旦要补预输入、切换过渡参数，就会再次改 runtime 与编辑器。

---

## 5. 运行时对象模型

### 5.1 总体结构

建议新增以下对象：

1. `StateController`
2. `StateRuntimeContext`
3. `ActiveStateRuntime`
4. `StateTimelineRuntime`
5. `StateTransitionRequest`
6. `StateInterruptContext`

### 5.2 StateController

`StateController` 是单位级服务对象。

建议挂载位置：

1. 与 [SkillPlayerController.cs](d:/myprojects/unity/projects/ActionEditor/Assets/SkillEditor/Runtime/Runtime/Skill/SkillPlayerController.cs) 同级，作为单位 runtime 控制器之一。
2. 与 [SkillCharacterActionBridge.cs](d:/myprojects/unity/projects/ActionEditor/Assets/SkillEditor/Runtime/Runtime/Skill/SkillCharacterActionBridge.cs) 协作，但不把状态切换逻辑塞进动画桥。

职责：

1. 加载当前单位可用的 `StateConfig`。
2. 维护当前激活状态。
3. 提供 `TryChangeState / ForceChangeState`。
4. 每帧推进当前状态 runtime。
5. 统一提交状态切换。

### 5.3 StateRuntimeContext

`StateRuntimeContext` 用于承载状态机运行时所需的共享上下文。

建议包含：

1. 当前单位对象
2. `StateController`
3. `SkillPlayerController`
4. `SkillCharacterActionBridge`
5. 当前输入快照
6. 当前命中/受击快照
7. 当前标签/属性快照
8. 当前技能释放上下文引用

原则：

1. 条件判定读 `Context`
2. 状态切换写 `Controller`
3. 时间轴执行读写 `Context`

### 5.4 ActiveStateRuntime

`ActiveStateRuntime` 表示“当前正在运行的状态实例”。

它不是配置本身，而是配置的运行时实例。

建议字段：

1. `StateConfig Config`
2. `float ElapsedTime`
3. `float PreviousTime`
4. `bool IsEntered`
5. `bool IsExiting`
6. `StateTimelineRuntime TimelineRuntime`
7. `BufferedInterruptInputCache BufferedInputs`

职责：

1. 记录时间推进
2. 驱动 timeline runtime
3. 承载预输入缓存
4. 提供当前状态是否已结束

### 5.5 StateTimelineRuntime

`StateTimelineRuntime` 负责执行 `StateTimeline` 的通用轨道。

建议：

1. 尽量复用 `MetaSkillTimelineRuntime` 现有轨道执行思想。
2. 但宿主改为 `StateConfig` / `StateTimelineConfig`。
3. 它不负责状态切换决策。
4. 它只负责“当前状态自己的轨道行为”。

这点非常重要：

1. `StateTimelineRuntime` 执行 hitbox / bullet / event
2. `StateController` 负责 interrupt 与 state transition

不要把两者混成一个类。

### 5.6 StateTransitionRequest

建议统一定义：

```csharp
public enum StateTransitionRequestType
{
    Interrupt,
    ExternalTry,
    ExternalForce,
    DefaultNext,
    SkillDriven,
}

public sealed class StateTransitionRequest
{
    public StateTransitionRequestType RequestType;
    public string SourceStateId;
    public string TargetStateId;
    public StateInterruptConfig InterruptConfig;
    public bool IgnoreInterruptRules;
    public float RequestedStartTime;
}
```

这样做的意义：

1. 内部打断与外部切换不分家。
2. 切换日志与调试统一。
3. 后续技能、AI、行为树都能复用。

---

## 6. 状态配置运行时建议

### 6.1 StateConfig 需要补的字段

为避免后续返工，建议现在就承认 `StateConfig` 的 runtime 需求不再是极简版本。

建议新增：

1. `DefaultNextStateId`
2. `StateTimelineConfig Timeline`

建议结构：

```csharp
[Serializable]
public sealed class StateConfig : IRuntimeTagContainerOwner
{
    public string StateId;
    public string StateName;
    public string AnimationClipPath;
    public string DefaultNextStateId;
    public StateTimelineConfig Timeline = new StateTimelineConfig();
    public TagContainer Tags = new TagContainer();
}
```

### 6.2 StateTimelineConfig

`StateTimelineConfig` 不是可选优化，而是现在就应该成为 runtime 主 schema。

建议：

```csharp
[Serializable]
public sealed class StateTimelineConfig
{
    public float Duration;
    public MetaSkillTimelineAnimationConfig Animation = new MetaSkillTimelineAnimationConfig();
    public List<MetaSkillTrackConfig> Tracks = new List<MetaSkillTrackConfig>();
    public List<StateInterruptConfig> Interrupts = new List<StateInterruptConfig>();
}
```

原因：

1. 通用轨道可以继续复用已有实现。
2. `Interrupts` 需要成为一等公民，不能继续外挂在别处。

### 6.3 StateInterruptConfig

参考 `AsiActionEditor.ActionInterrupt`，建议核心字段现在就包含：

```csharp
[Serializable]
public sealed class StateInterruptConfig
{
    public bool IsEnabled = true;
    public string TargetStateId;
    public float TriggerTime;
    public float Duration;
    public float ExecuteTime;
    public int SortOrder;
    public bool CheckAllConditions = true;
    public bool UseTransitionOverride;
    public float TransitionDuration;
    public AnimationTransitionTimeUnit TransitionTimeUnit;
    public float TargetStartTime;
    [SerializeReference] public List<IStateInterruptCondition> Conditions = new List<IStateInterruptCondition>();
}
```

这里要特别说明两个字段：

1. `ExecuteTime`
   - 对齐 `AsiActionEditor.ActionInterrupt.ExecuteTime`
   - 表达“预输入命中后，真正允许切出的时间点”
   - 如果现在不保留，后面连招和输入缓存几乎一定返工
2. `UseTransitionOverride`
   - 对齐 `CrossFadeTime / OffsetTime` 的存在意义
   - 当前 `State` 虽然有统一的 `Timeline.Animation`，但不同打断条目常常需要不同切换手感
   - 即使第一版编辑器先不全量暴露，runtime schema 也建议先保留

### 6.4 打断时间语义

建议直接沿用 `AsiActionEditor` 与当前 Timeline 的时间语义：

1. `Duration < 0`
   - 从 `TriggerTime` 开始，一直到当前状态结束
2. `Duration = 0`
   - 单帧打断点
3. `Duration > 0`
   - 普通时间窗

`ExecuteTime` 语义：

1. `ExecuteTime <= 0`
   - 条件一命中即可切
2. `ExecuteTime > 0`
   - 在 `TriggerTime` 之后先进入预输入期
   - 到达 `TriggerTime + ExecuteTime` 才正式允许切出

这部分必须先定死，因为它直接决定编辑器 block 的含义。

---

## 7. 打断条件运行时模型

### 7.1 条件接口

建议定义：

```csharp
public interface IStateInterruptCondition
{
    string GetDisplayName();
    bool Evaluate(StateInterruptContext context);
    IStateInterruptCondition Clone();
}
```

### 7.2 StateInterruptContext

建议包含：

1. 当前单位
2. 当前状态配置
3. 当前状态 elapsed time
4. 当前输入快照
5. 当前命中目标信息
6. 当前受击信息
7. 移动输入信息
8. 标签与属性读取接口

### 7.3 第一版条件类型

第一版直接对齐 `AsiActionEditor` 中最核心的条件：

1. `InputActionCondition`
2. `MoveStateCondition`
3. `BeHitCondition`
4. `OnHitCondition`
5. `WeightRangeCondition`

如果后续需要：

1. 目标距离
2. 目标角度
3. 当前状态标签
4. 受击者状态标签

则继续沿用同样的多态条件扩展方式，不改 evaluator 主流程。

### 7.4 条件组合方式

直接沿用 `AsiActionEditor.ActionInterrupt.CheckAllCondition`：

1. `true` = 所有条件都满足
2. `false` = 任意条件满足

这是 runtime 的固定规则，不应由编辑器自由发明别的组合模式。

---

## 8. StateController 的更新流程

### 8.1 每帧主循环

建议 `StateController.Update(deltaTime)` 流程如下：

1. 收集并冻结当前帧输入/命中/受击快照
2. 如果没有当前状态，则尝试进入默认状态
3. 推进 `ActiveStateRuntime.ElapsedTime`
4. 执行 `StateTimelineRuntime.Update(previousTime, currentTime)`
5. 扫描并求值 `Interrupts`
6. 如果存在命中的内部打断请求，提交最高优先级请求
7. 若没有内部打断，再检查状态是否自然结束
8. 若自然结束且有 `DefaultNextStateId`，提交默认切换请求
9. 若都没有，保持当前状态
10. 清理当前帧 edge 型事件快照

### 8.2 打断扫描顺序

建议：

1. 先筛掉未启用或目标状态无效的打断条目
2. 再判断当前时间是否落入条目窗口
3. 再判断预输入缓存与 `ExecuteTime`
4. 再执行条件求值
5. 收集所有命中条目后按优先级排序

排序规则：

1. `SortOrder` 高的优先
2. 若相同，`TriggerTime` 更早的优先
3. 若仍相同，按配置列表先后顺序稳定取第一个

### 8.3 预输入机制

参考 `AsiActionEditor.ActionStatePart.TimeLine.Interrup.cs`，第一版 runtime 建议保留预输入机制。

语义：

1. 条件可能在窗口开始时就被捕获
2. 但真正切换发生在 `ExecuteTime` 到达后
3. 对输入型条件，需要缓存“当前窗口中命中过的输入”

这样做的必要性：

1. 连招不需要玩家在单帧精确按键
2. 运行、起手、接段这些状态切换都更稳定
3. 后面如果再补，会直接改 runtime 行为，不只是补 UI

建议把这部分做成独立缓存对象，而不是散落在 controller 本体里。

---

## 9. 状态切换语义

### 9.1 统一切换入口

建议暴露：

```csharp
public bool TryChangeState(string targetStateId);
public bool ForceChangeState(string targetStateId, float startTime = 0f);
```

内部统一落到：

```csharp
private bool CommitTransition(StateTransitionRequest request);
```

### 9.2 TryChangeState

`TryChangeState(targetStateId)` 的语义定义：

1. 不无脑切。
2. 仅当当前状态 runtime 在当前时刻允许切到该目标状态时才成功。
3. 目标状态必须存在。
4. 若当前没有状态，可视为初始化进入，允许直接成功。

它本质上是对当前打断规则的一次“外部同规请求”。

### 9.3 ForceChangeState

`ForceChangeState(targetStateId)` 的语义定义：

1. 无视当前打断窗口和条件
2. 只检查目标状态是否存在
3. 仍然走完整的退出/进入/动画切换/时间轴重建流程

它不是直接改 `CurrentStateId`，而是“跳过验证，不跳过切换流程”。

### 9.4 默认去向

当前状态自然结束后：

1. 若 `DefaultNextStateId` 非空，则自动切过去
2. 若为空，则当前状态继续驻留或重入，由状态类型决定

第一版建议规则简单明确：

1. 有 `DefaultNextStateId` 就切
2. 没有则留在当前状态末尾，不自动循环重进

这样比“自动重播动画 + 自动重置 timeline”更稳。

### 9.5 MetaSkill.changeStateTo

未来 `MetaSkill.changeStateTo` 的建议语义：

1. `MetaSkill` 释放成功时，请求状态切到目标 `State`
2. 默认使用 `ForceChangeState`

原因：

1. 这是技能系统显式驱动的状态切换
2. 它不是在询问“当前状态内部是否允许某个自然打断”
3. 如果这里还受当前状态 interrupt 限制，技能和状态会互相打架

---

## 10. 动画与时间轴执行

### 10.1 动画播放职责

状态动画播放继续交给 [SkillCharacterActionBridge.cs](d:/myprojects/unity/projects/ActionEditor/Assets/SkillEditor/Runtime/Runtime/Skill/SkillCharacterActionBridge.cs)。

建议新增 `State` 相关方法，而不是把 `MetaSkill` 方法硬复用到语义混乱：

1. `PlayStateAnimation(...)`
2. `SyncStateAnimation(...)`
3. `StopStateAnimation(...)`

### 10.2 状态切换时的动画规则

状态切换时：

1. 默认使用目标 `State.Timeline.Animation` 的 transition 参数
2. 若打断条目配置了 transition override，则以打断条目为准
3. 动画 sample time 默认从 `0` 开始
4. 若打断条目配置了 `TargetStartTime`，则从该时间点开始采样

### 10.3 StateTimelineRuntime 与通用轨道

第一版建议：

1. `StateTimelineRuntime` 继续支持 hitbox / bullet / unit event
2. 打断判定不放进 `StateTimelineRuntime`
3. `StateTimelineRuntime` 只提供时间推进和轨道执行结果

这样后续如果 `MetaSkillTimelineRuntime` 抽公共 core，也更容易收敛。

---

## 11. 与技能系统的关系

### 11.1 SkillPlayerController 不替代 StateController

`SkillPlayerController` 负责技能槽位、技能 runtime、输入触发技能。

`StateController` 负责状态机本身。

两者关系应为：

1. `SkillPlayerController` 需要读取当前状态
2. `SkillPlayerController` 在技能释放成功时可调用 `ForceChangeState(changeStateTo)`
3. `StateController` 不直接管理技能槽位

### 11.2 技能释放前状态校验

未来技能释放时：

1. 技能侧先检查当前状态是否允许用该技能
2. 若释放成功，再由技能驱动状态切换

也就是说：

1. `UseSkill` 不等于 `ChangeState`
2. 但 `UseSkill` 可能触发 `ChangeState`

---

## 12. 第一版运行时明确不做的内容

为了先把主链路做稳，第一版运行时明确不做：

1. `打断组`
2. 多层状态机
3. limb / upper / script 分层状态
4. 网络同步状态回放
5. 复杂组模板继承

但以下内容建议现在就进 schema：

1. `ExecuteTime`
2. 条件组合方式
3. transition override 开关

因为这些如果现在不留，后面极容易动 runtime 数据结构。

---

## 13. 对编辑器的反向约束

这份 runtime 文档反过来会约束后面的编辑器：

1. 编辑器不能再把 `StateTimeline` 当成单纯的 `MetaSkillTimeline` 宿主替换版
2. `StateTimeline` 必须持有专属 `InterruptTracks / Interrupts` 数据，编辑器侧可以做兼容迁移，但正式编辑入口以 `InterruptTracks` 为准
3. 打断条目详情必须复用现有 `SkillEditorInspectorWindow`，不能在 `StateTimeline` 底部再单独维护一套表单；Inspector 至少要能编辑：
   - `TargetStateId`
   - `TriggerTime`
   - `Duration`
   - `ExecuteTime`
   - `SortOrder`
   - `CheckAllConditions`
   - `Conditions`
4. `TargetStateId` 应从当前 unit 的 state 列表中选择
5. 如果第一版 UI 想收敛复杂度，可以把 transition override 做成折叠高级选项，但不建议 runtime schema 删除

---

## 14. 建议实施顺序

既然现在决定先做 runtime，那么建议顺序改为：

1. 先改数据结构
   - `StateConfig.Timeline` -> `StateTimelineConfig`
   - 新增 `DefaultNextStateId`
   - 新增 `StateInterruptConfig`
   - 新增 `IStateInterruptCondition`
2. 再做 runtime 主链路
   - `StateController`
   - `ActiveStateRuntime`
   - `StateTransitionRequest`
   - `TryChangeState / ForceChangeState`
3. 再接动画与 timeline runtime
   - `StateTimelineRuntime`
   - `SkillCharacterActionBridge` 的 `State` 动画方法
4. 最后回到编辑器
   - 调整 `StateTimelineEditorWindow`
   - 加入打断轨
   - 补齐条件编辑

这个顺序的核心价值是：

1. 数据结构由 runtime 决定
2. 编辑器只负责忠实编辑 runtime 需要的数据

---

## 15. 最终结论

当前第二版 `State` runtime 的推荐结论如下：

1. 运行时先于编辑器推进是对的。
2. `State` 必须引入正式的 `StateController`。
3. `打断轨` 必须被视为运行时状态转移规则。
4. 内部打断、默认去向、外部切状态都必须统一到一条切换提交管线。
5. 为了避免返工，`StateInterruptConfig` 现在就应保留 `ExecuteTime` 与可选 transition override 的能力。
6. 编辑器随后应服从这份 runtime 模型，而不是再以简化版 UI 反向限定 runtime。
