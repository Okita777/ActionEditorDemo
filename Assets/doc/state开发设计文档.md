# State 开发设计文档

## 1. 目标

本文档用于约束第二版 `State` 的第一阶段编辑器开发。

当前目标不是一次性做完整状态系统，而是先把 `State` 作为与 `MetaSkill` 同级的正式资源接入现有第二版资源编辑体系，并保证：

1. `SkillResource` 新增 `State` 资源入口。
2. `State` 资源和 `MetaSkill` 一样，归属于当前 `Unit` 上下文。
3. 点击 `State` 资源后，打开复用 `SkillEditorInspectorWindow` 的详情面板。
4. `State` 的 Inspector 交互风格与 `MetaSkill` 尽量保持一致。
5. `StateTimeline` 的界面、交互、轨道系统、预览链路，严格对齐 `MetaSkillTimeline`。
6. `StateTimeline` 正式引入一条 `打断轨`，作为 `State` 相对 `MetaSkillTimeline` 的核心差异。

这份设计文档只描述第一阶段要做什么、怎么做、做到什么程度算完成。

---

## 2. 设计依据

### 2.1 来自第二版框架文档的硬约束

根据 `SkillRuntime第二版框架说明.md`，当前 `State` 开发必须满足以下原则：

1. `State` 是正式资源，不是临时附属配置。
2. `State` 必须属于当前 `Unit` 资源域。
3. `StateTimeline` 与 `MetaSkillTimeline` 不能做成两套风格完全不同的系统。
4. `State` 允许不配置动画，但如果没有动画，则 Timeline 不允许进入正常编辑预览。
5. 动画桥接继续复用当前 `Animancer` 链路，不另起一套状态动画系统。

### 2.2 来自当前需求的直接约束

当前用户需求明确要求：

1. 资源入口层面，`State` 的结构要和 `MetaSkill` 一致。
2. 点击 `State` 资源，应显示详情窗口，复用已有 Inspector 体系。
3. Inspector 中先做：`stateId`、`stateName`、`stateTag`、`stateAnim`、`transition`、`StateTimeline` 按钮。
4. `StateTimeline` 必须使用 `MetaSkillTimeline` 相同的编辑器界面，不允许另写一套简化版。
5. `StateTimeline` 需要增加 `打断轨`，并且打断条件的设计要严格参考 `AsiActionEditor`。
6. 可以先不做 `打断组` 概念，但必须先把单条 `打断轨` 做出来。

---

## 3. 本阶段范围

### 3.1 要做的内容

1. 新增 `State` 资源类型。
2. 新增 `StateConfig` 数据模型。
3. 新增 `SkillResourceRepository` 中针对 `State` 的加载、创建、复制、保存、显示名逻辑。
4. 让 `SkillResourceEntryWindow` 增加 `State` 标签页，并且只在存在当前 `Active Unit` 时显示对应 unit 下的 `State` 资源。
5. 新增 `SkillEditorInspectorWindow.OpenState(...)` 与 `StateInspectorPanel`。
6. 在 Inspector 中实现 `State` 基础信息编辑。
7. 新增 `StateTimelineEditorWindow`。
8. 让 `StateTimelineEditorWindow` 尽可能直接复用 `MetaSkillTimelineEditorWindow` 的结构、样式和通用轨道交互。
9. 为 `StateTimeline` 增加一条专用 `打断轨`，支持在时间轴上配置目标状态和打断条件。

### 3.2 本阶段明确不做的内容

1. 不做 `StateController`。
2. 不做 `TryChangeState / ForceChangeState` 运行时接口。
3. 不做 `MetaSkill` 与 `State` 的运行时接入。
4. 不做 `MetaSkill changeStateTo`。
5. 不做 `打断组`。
6. 不做每个打断条目的分组锁定、组偏移、组模板联动。
7. 不做完整 `TimelineCore` 抽象重构。

说明：

本阶段的重点是把 `State` 的资源模型和编辑器工作流立住，且不破坏当前 `Skill / MetaSkill` 链路。

---

## 4. 资源模型设计

### 4.1 资源类型枚举

当前 `SkillResourceType` 只有：

- `Unit`
- `Skill`
- `MetaSkill`
- `Buff`

需要新增：

- `State`

新增后，`State` 的定位与 `Skill`、`MetaSkill` 一样，属于 unit-scope 资源。

### 4.2 资源路径设计

当前 unit-scope 资源结构为：

- `Assets/SkillEditor/Data/Units/{UnitId}/Skills/...`
- `Assets/SkillEditor/Data/Units/{UnitId}/MetaSkills/...`

`State` 要保持同样的结构：

- `Assets/SkillEditor/Data/Units/{UnitId}/States/...`

对应编译产物路径：

- `Assets/SkillEditor/Compiled/Units/{UnitId}/States/...`

结论：

`State` 不是全局资源目录，不走 `Assets/SkillEditor/Data/States` 这种独立根路径，而是严格挂在 unit 下。

### 4.3 StateConfig 结构

本阶段建议最小结构如下：

```csharp
[Serializable]
public sealed class StateConfig : IRuntimeTagContainerOwner
{
    public string StateId = "state_001";
    public string StateName = "New State";
    public string AnimationClipPath = string.Empty;
    public TagContainer Tags = new TagContainer();
    public MetaSkillTimelineConfig Timeline = new MetaSkillTimelineConfig();

    [NonSerialized] private RuntimeTagContainer _runtimeTags;
    public RuntimeTagContainer RuntimeTags => _runtimeTags ??= new RuntimeTagContainer();
}
```

说明：

1. `stateId`、`stateName` 是基础字段。
2. `stateAnim` 直接沿用 `AnimationClipPath` 表达。
3. `transition` 不额外单开一套 State 结构，直接使用 `Timeline.Animation`，也就是复用 `MetaSkillTimelineAnimationConfig`。
4. `stateTag` 直接复用 `TagContainer`。
5. `StateTimeline` 的通用轨道条目继续复用 `MetaSkillTrackConfig / HitBoxConfig / BulletConfig / MetaSkillEventConfig`。
6. `StateTimeline` 的宿主配置从纯 `MetaSkillTimelineConfig` 升级为 `StateTimelineConfig`，在保留通用字段的同时新增 `Interrupts`。

### 4.4 为什么不再直接复用 MetaSkillTimelineConfig

原因有三点：

1. 当前需求明确要求和 `MetaSkillTimeline` 严格统一。
2. `MetaSkillTimeline` 已经具备动画、攻击盒、子弹、事件这些第一阶段需要的轨道能力。
3. `State` 现在要正式引入 `打断轨`，而 `MetaSkillTimelineConfig` 本身没有承载这类数据的字段。

结论：

本阶段 `StateTimeline` 的宿主仍然是 `StateConfig`，但 `StateConfig.Timeline` 应演进为 `StateTimelineConfig`：

1. 共享字段命名尽量与 `MetaSkillTimelineConfig` 保持一致。
2. 通用轨道区仍然沿用现有 `Tracks` 结构。
3. `State` 独有的 `打断轨` 单独落在 `Interrupts` 列表中。

---

## 5. 资源窗口接入设计

### 5.1 SkillResourceEntryWindow 改动

需要改动点：

1. `SkillResourceEntryWindow` 新增 `_stateEntries` 列表缓存。
2. 顶部资源类型标签新增 `State` 按钮。
3. `RefreshAssets()` 中，在有 `Active Unit` 时加载当前 unit 的 `State` 列表。
4. `GetActiveEntries()` / `GetAllEntries()` / `ContainsEntry()` 等分支补齐 `State`。
5. 新建、复制、删除、保存流程统一纳入 `State`。
6. 点击 `State` 资源项时，打开 `SkillEditorInspectorWindow.OpenState(entry)`。

### 5.2 Resource Repository 改动

`SkillResourceRepository` 需要补齐：

1. `LoadStates(string unitId = "")`
2. `Create(... SkillResourceType.State ... )`
3. `Duplicate(... SkillResourceType.State ... )`
4. `GetDisplayName(... StateConfig ... )`
5. `BuildRuntimeConfig(... StateConfig ... )`
6. `ResolveCompiledFolder(...)` 中纳入 unit-scope `States`

显示名策略与 MetaSkill 一致：

- 优先显示 `StateName`
- 空时回退 `BaseName`

---

## 6. Inspector 设计

### 6.1 入口形式

沿用当前结构：

- `SkillEditorInspectorWindow.OpenState(entry)`
- `StateInspectorPanel : ISkillEditorInspectorPanel`

这和 `OpenMetaSkill(entry)` / `MetaSkillInspectorPanel` 保持一致。

### 6.2 Inspector 布局

布局保持与 `MetaSkillInspectorPanel` 相同的三段式思路，但收缩到本阶段需求：

#### 第一块：基础信息

字段包括：

1. `stateId`
2. `stateName`
3. `State Tags`
4. `stateAnim`
5. `anim transition`

这里的动画选择交互直接复用 `MetaSkillInspectorPanel.DrawAnimationField(...)` 的做法：

1. HelpBox 显示当前动画名
2. 支持拖拽 AnimationClip
3. 支持“选择 / 清空”按钮
4. 显示当前预览单位动画筛选信息
5. 若 clip 不符合当前 unit 动画筛选规则，给出 warning

#### 第二块：Timeline 入口

标题建议保持和 MetaSkill 同样结构：

- `OnUpdate`

按钮建议命名：

- `StateTimeline`

行为：

1. 若 `AnimationClipPath` 为空，或无法解析到 clip，则按钮 disabled。
2. disabled 时显示提示：`只有配置了 anim，StateTimeline 才允许打开。`
3. 点击后打开 `StateTimelineEditorWindow.OpenForEntry(entry)`。

### 6.3 不做的 Inspector 字段

以下内容本阶段不进 Inspector：

1. `DefaultNextState`
2. 打断条件
3. 状态切换规则列表
4. 运行时状态判定条件

原因：

这些属于第二阶段状态运行时接入内容，不属于当前第一阶段目标。

---

## 7. StateTimeline 设计

### 7.1 核心原则

`StateTimelineEditorWindow` 必须严格对齐 `MetaSkillTimelineEditorWindow`。

这里的“严格对齐”具体含义是：

1. 时间轴区域布局一致。
2. Toolbar 结构一致。
3. 时间头、播放头、滚动区域、详情面板入口一致。
4. 轨道分组一致。
5. 条目 block 的绘制、拖拽、缩放、选中逻辑一致。
6. 详情编辑统一复用已有 Inspector 窗口；攻击盒、子弹、事件、打断点击后都在 Inspector 中编辑，不在 StateTimeline 底部另开一套表单。
7. 场景预览和动画采样逻辑一致。

### 7.2 本阶段允许的差异

仅允许这几类差异：

1. 宿主数据类型从 `MetaSkillConfig` 变成 `StateConfig`。
2. 顶部标题从 `MetaSkillTimeline` 变成 `StateTimeline`。
3. 不显示恢复动画相关内容。
4. 增加一条 `State` 专属的 `打断轨`。

除此之外，不应重新发明另一套 Timeline UI。

### 7.3 推荐实现方式

本阶段不建议手搓一个“长得像”的 StateTimeline 窗口。

推荐做法是：

1. 以 `MetaSkillTimelineEditorWindow` 为基底。
2. 手工抽出宿主相关读写点。
3. 保持大部分绘制、轨道、详情、拖拽、预览逻辑不变。

推荐抽象边界如下：

#### 宿主侧差异点

需要抽象或重写的只有：

1. `Bind(entry)` 时把 `_config` 绑定成 `StateConfig`
2. `LoadClip()` 读取 `StateConfig.AnimationClipPath`
3. 不再读取 `_recoveryClip`
4. `NeedsDurationSync()` / `GetTimelineDuration()` 等从 `StateConfig.Timeline` 取值
5. 保存 dirty 的宿主标记逻辑

#### 可直接复用的部分

直接复用或原样迁移的部分：

1. Toolbar 绘制
2. Timeline Surface 绘制
3. TrackGroup / TrackRow / ItemBlock 绘制
4. 攻击盒、子弹、事件详情编辑
5. Playhead、RangeSlider、Grid、StatusBar
6. SceneView 预览和攻击盒绘制

### 7.4 轨道范围

本阶段 `StateTimeline` 支持的轨道与当前 `MetaSkillTimeline` 一致：

1. 动画轨
2. 攻击盒轨
3. 子弹轨
4. 事件轨
5. 打断轨

本阶段不支持：

1. 摄像机轨
2. 特效轨
3. 音效轨

### 7.5 打断轨设计

`StateTimeline` 的打断轨只做一条固定轨道，不做 `AsiActionEditor` 里的 `打断组` 概念。

这样做的原因：

1. 用户当前需求明确允许先不做组。
2. 当前阶段最重要的是先把“什么时候可打断、能打到哪里、满足什么条件”这三个核心问题做对。
3. 轨道和条目交互仍然必须沿用现有 Timeline + Inspector 的统一编辑模式，避免在 StateTimeline 底部再做一套独立详情面板。

打断轨的基本语义：

1. 这是一条状态内部转移能力轨道。
2. 轨道上的每个 block 表示一个可打断窗口。
3. 每个 block 定义：在什么时间段、满足什么条件、允许切到哪个 `State`。
4. 它只定义“允许打断”的能力，不等于外部系统不能直接切状态。

### 7.6 打断条目数据结构

建议新增：

```csharp
[Serializable]
public sealed class StateTimelineConfig
{
     public float Duration;
     public MetaSkillTimelineAnimationConfig Animation;
     public List<MetaSkillTrackConfig> Tracks = new List<MetaSkillTrackConfig>();
     public List<StateInterruptConfig> Interrupts = new List<StateInterruptConfig>();
}

[Serializable]
public sealed class StateInterruptConfig
{
     public bool IsEnabled = true;
     public string TargetStateId;
     public float TriggerTime;
     public float Duration;
     public int SortOrder;
     public bool CheckAllConditions = true;
     [SerializeReference] public List<IStateInterruptCondition> Conditions = new List<IStateInterruptCondition>();
}
```

字段含义：

1. `TargetStateId`：打断成功后切到哪个状态。对应 `AsiActionEditor.ActionInterrupt.ActionName`。
2. `TriggerTime`：打断窗口起点。
3. `Duration`：打断窗口长度。
4. `SortOrder`：多个窗口同时命中时的优先级。对应 `AsiActionEditor.ActionInterrupt.SortID`。
5. `CheckAllConditions`：条件组合方式；`true = AND`，`false = OR`。对应 `AsiActionEditor.ActionInterrupt.CheckAllCondition`。
6. `Conditions`：条件列表。用 `[SerializeReference]` 保持和 `AsiActionEditor` 一样的可扩展条件体系。

### 7.7 打断时间语义

打断条目的时间语义建议直接对齐当前 Timeline 里已有的持续时间表达：

1. `Duration < 0`：从 `TriggerTime` 开始一直持续到当前 `State` 结束。
2. `Duration = 0`：单帧打断点。
3. `Duration > 0`：正常时间窗。

这样做的好处：

1. 与现有事件/子弹的持续时间直觉一致。
2. 与 `AsiActionEditor` 的打断条持续时间表达一致。
3. 编辑器 block 绘制与拖拽逻辑可以直接沿用现有 item block 语义。

### 7.8 打断条件模型

参考 `AsiActionEditor.IInterruptCondition`，建议新增：

```csharp
public interface IStateInterruptCondition
{
     string GetDisplayName();
     bool Evaluate(StateInterruptContext context);
     IStateInterruptCondition Clone();
}
```

第一版条件类型，优先对齐 `AsiActionEditor` 的高价值条件：

1. `InputActionCondition`
    - 对齐 `CheckCostomKey`
    - 支持按下、抬起、点击、按住、当前按键状态
2. `MoveStateCondition`
    - 对齐 `CheckMoveState`
    - 支持“是否移动输入中”“是否预输入”
3. `BeHitCondition`
    - 对齐 `CheckBeHit`
4. `OnHitCondition`
    - 对齐 `CheckOnHit`
5. `WeightRangeCondition`
    - 对齐 `CheckWeightRange`

设计约束：

1. 一些条件类型天然应限制重复添加，例如 `MoveState`、`BeHit`、`OnHit`、`WeightRange`。
2. 编辑器层面需要像 `AsiActionEditor` 一样，对这类条件做“不可重复添加”的保护。
3. 条件系统保留扩展点，但第一版先只做确实会参与 `State` 切换判定的条件。

### 7.9 打断轨编辑器设计

`StateTimelineEditorWindow` 的打断轨仍然遵守“整体 UI 和 `MetaSkillTimeline` 同构”的原则，但有以下专属行为：

1. 打断轨以固定单行轨道的形式出现在时间轴轨道区。
2. 轨道 block 的拖拽、缩放、选中逻辑与攻击盒/事件 block 尽量一致。
3. block 标题优先显示目标状态名，副标题显示 `SortOrder` 与条件组合方式。
4. 点击 block 后，详情面板显示：
    - `TargetStateId`
    - `TriggerTime`
    - `Duration`
    - `SortOrder`
    - `CheckAllConditions`
    - `Conditions` 列表
5. `TargetStateId` 不允许手填裸字符串，应该使用当前 `Unit` 下已有 `State` 列表做下拉选择。
6. `Conditions` 列表的增删、复制、粘贴、类型切换，参考 `AsiActionEditor` 的 Inspector 结构，而不是塞进 block 本体里编辑。

### 7.10 打断运行时判定约束

虽然本次先写设计文档，不立即实现运行时，但数据和编辑器必须为后续运行时留好约束：

1. 打断轨只参与“当前状态是否允许切到目标状态”的判定。
2. 外部 `ForceChangeState` 不受打断轨限制。
3. 未来的 `TryChangeState(targetStateId)` 可以复用同一套打断判定逻辑。
4. 如果同一帧多个打断条目同时满足条件，应按 `SortOrder` 从高到低选中。
5. 若 `SortOrder` 相同，则按更早定义的条目优先，避免结果不稳定。

### 7.11 第一版明确不做的打断特性

参考 `AsiActionEditor.ActionInterrupt`，以下字段或能力本阶段先不进入 `StateInterruptConfig`：

1. `ExecuteTime`
2. `CrossFadeTime`
3. `OffsetTime`
4. `打断组`
5. 组级偏移、组级锁定、模板继承

原因：

1. 当前 `State` 已经有统一的动画 transition 配置。
2. 当前最重要的是先把“时间窗 + 目标状态 + 条件列表”闭环做出来。
3. 这些字段是否应该做成条目级 override，需要等运行时状态切换接入后再决定。

---

## 8. 动画与预览链路设计

### 8.1 动画来源

`State` 的动画选择和 `MetaSkill` 一样，必须从当前 `Active Unit` 对应的动画目录筛选。

因此直接复用现有：

1. `SkillPreviewUnitSettings.LoadActivePreviewConfig()`
2. `SkillAnimationSelectionUtility`
3. `SkillAnimationPickerWindow`
4. `SkillAnimationReferenceUtility`

### 8.2 Timeline 打开条件

`StateTimeline` 的打开条件与 `MetaSkillTimeline` 完全一致：

1. 有 `AnimationClipPath`
2. 该路径能成功解析到 `AnimationClip`

如果没有动画，则：

1. Inspector 中按钮不可用
2. 不进入 Timeline 编辑器

说明：

框架文档允许 `State` 不配置动画，但当前阶段用户要求 Timeline 先和 `MetaSkillTimeline` 一致，因此先用相同入口限制，后续再决定是否扩展为“无动画也允许进入 Timeline 编辑非动画轨”。

---

## 9. 数据复用与后续演进

### 9.1 第一阶段的数据复用结论

第一阶段先采用以下复用策略：

1. `StateConfig.Tags` 复用 `TagContainer`
2. `State` 动画 transition 配置复用 `MetaSkillTimelineAnimationConfig`
3. `StateTimelineConfig` 复用 `MetaSkillTimelineConfig` 的共享字段语义，但增加 `Interrupts`
4. Timeline 通用轨道条目复用 `MetaSkillTrackConfig / HitBoxConfig / BulletConfig / MetaSkillEventConfig`
5. 打断条件体系参考 `AsiActionEditor.IInterruptCondition`，但命名与上下文改为 `State` 语义

### 9.2 第二阶段的演进方向

等状态运行时正式接入时，再考虑以下扩展：

1. 抽 `TimelineCore`
2. 给打断条目补充可选的过渡 override 字段
3. 增加 `DefaultNextState`
4. 增加 `StateRuntimeContext`
5. 增加 `StateController`

结论：

本阶段不为“未来可能抽象”而提前破坏当前已可工作的 `MetaSkillTimeline` 代码。

---

## 10. 开发顺序

建议严格按以下顺序开发。

### 第一步：资源模型接入

1. 新增 `StateConfig`
2. 新增 `SkillResourceType.State`
3. 新增 `SkillEditorResourcePaths` 中的 `States` 路径常量
4. 扩展 `SkillResourceRepository`

完成标志：

1. `State` 能在当前 `Unit` 下创建、复制、删除、保存
2. `State` 能出现在 Resource 列表中

### 第二步：Inspector 接入

1. `SkillEditorInspectorWindow.OpenState(...)`
2. `StateInspectorPanel`
3. `stateId / stateName / tags / anim / transition / StateTimeline 按钮`

完成标志：

1. 点击 `State` 后能打开 Inspector
2. 动画选择器与 `MetaSkill` 一致
3. 没配动画时按钮不可用

### 第三步：StateTimeline 编辑器接入

1. 新增 `StateTimelineEditorWindow`
2. 复用 `MetaSkillTimeline` 的 UI 与轨道能力
3. 接通 Inspector 中的按钮入口

完成标志：

1. 能打开 `StateTimeline`
2. 可编辑动画、攻击盒、子弹、事件轨
3. 样式和 `MetaSkillTimeline` 对齐

### 第四步：窄验证

1. 创建一个带动画的 `State`
2. Inspector 中成功选择动画
3. 成功打开 `StateTimeline`
4. 能添加攻击盒、子弹、事件并保存
5. 重新打开资源后数据还在

---

## 11. 验收标准

本阶段完成后，应满足以下验收标准：

1. `Resource Entry` 中存在 `State` 标签页。
2. `State` 的创建、复制、删除、保存与 `MetaSkill` 一致。
3. `State` 严格按当前 `Active Unit` 上下文组织。
4. 点击 `State` 后打开 Inspector，而不是其它临时窗口。
5. Inspector 中的动画选择体验与 `MetaSkill` 一致。
6. `StateTimeline` 的界面和交互与 `MetaSkillTimeline` 一致。
7. `StateTimeline` 中存在一条正式可编辑的打断轨。
8. 点击打断条目后，可以配置目标状态和打断条件。
9. 当前已有 `Skill / MetaSkill / Buff / Unit` 功能不被破坏。

---

## 12. 关键实现原则

最后固定三条实现原则，后续开发必须遵守：

1. `StateTimeline` 不允许做成一套“看起来差不多”的简化版窗口，必须与 `MetaSkillTimeline` 严格统一。
2. `State` 的动画选择、transition 配置、Timeline 打开条件，要尽可能直接复用 `MetaSkill` 的现有逻辑。
3. 本阶段先把 `State` 的资源与编辑器形态做对，不提前把运行时状态系统一起硬塞进来。
