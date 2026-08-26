# SkillRuntime 第四版最小分层状态机数据结构草案

## 1. 文档目标

本文档只定义新的最小数据结构，不涉及运行时代码改法。

目标是服务新的第一版分层设计：

1. 状态层固定为枚举定义。
2. 层独立推进。
3. 层有固定优先级。
4. 动画先支持 Locomotion 与 Action 两层。
5. 暂不引入 abilities 系统。

---

## 2. 对当前数据结构的结论

当前 [Assets/SkillEditor/Runtime/Config/Unit/UnitConfig.cs](Assets/SkillEditor/Runtime/Config/Unit/UnitConfig.cs) 里的 `StateConfig` 混合了三类信息：

1. 状态资源本体
2. 状态层归属
3. 表现输出与控制事实

当前 [Assets/SkillEditor/Runtime/Common/SkillEnums.cs](Assets/SkillEditor/Runtime/Common/SkillEnums.cs) 里的固定枚举也混合了两类概念：

1. 逻辑状态层
2. 动画表现层

在新设计里，这些概念需要拆开。

---

## 3. 第一版需要保留的数据

以下资源字段仍然有效，应继续保留：

### 3.1 UnitConfig

保留：

1. `UnitId`
2. `DisplayName`
3. `PrefabAssetPath`
4. `CameraResourcePath`
5. `AnimationDirectory`
6. `ActiveSkillSlots`
7. `PassiveSkillSlots`

第一版需要调整：

`DefaultStateId` 不再够用，应直接改成“每层默认状态”配置。

### 3.2 StateConfig

保留：

1. `StateId`
2. `StateName`
3. `AnimationClipPath`
4. `DefaultNextStateId`
5. `Timeline`
6. `Tags`

这些字段仍然是状态资源的基础部分。

---

## 4. 第一版新增或重定义的数据结构

## 4.1 固定状态层枚举与优先级定义

第一版直接定义固定枚举层：

```csharp
[Serializable]
public enum StateLayerType
{
  None = 0,
  Locomotion = 1,
  Action = 2,
}
```

并约定固定优先级：

1. `Locomotion` 优先级为 0
2. `Action` 优先级为 1

可以写成工具函数或静态定义：

```csharp
public static int GetPriority(StateLayerType layerType)
{
  switch (layerType)
  {
    case StateLayerType.Action:
      return 1;
    case StateLayerType.Locomotion:
      return 0;
    default:
      return -1;
  }
}
```

说明：

1. 第一版不做动态添加层。
2. 以后如果要加层，直接加枚举和优先级定义。
3. 运行时仍然必须按层列表统一循环，不能按层名写特判分支。

---

## 4.2 UnitConfig 按层默认状态定义

第一版 `UnitConfig` 应直接改成按层配置默认状态。

建议结构：

```csharp
[Serializable]
public sealed class UnitLayerDefaultStateConfig
{
  public StateLayerType Layer = StateLayerType.Locomotion;
  public string DefaultStateId = string.Empty;
}

[Serializable]
public sealed class UnitConfig
{
  public string UnitId = "unit_001";
  public string DisplayName = "New Unit";
  public string PrefabAssetPath = string.Empty;
  public string CameraResourcePath = string.Empty;
  public string AnimationDirectory = string.Empty;
  public List<UnitLayerDefaultStateConfig> LayerDefaultStates = new List<UnitLayerDefaultStateConfig>();
  public List<UnitActiveSkillSlotConfig> ActiveSkillSlots = new List<UnitActiveSkillSlotConfig>();
  public List<UnitPassiveSkillSlotConfig> PassiveSkillSlots = new List<UnitPassiveSkillSlotConfig>();
}
```

说明：

1. 不再使用单一 `DefaultStateId`。
2. `Locomotion` 层必须有默认状态。
3. `Action` 层可以允许默认状态为空。

---

## 4.3 动画层定义：AnimationLayerType

动画层先单独保留固定枚举：

```csharp
[Serializable]
public enum AnimationLayerType
{
    None = 0,
    Locomotion = 1,
    Action = 2,
}
```

说明：

1. 第一版只支持两个动画层。
2. 未来可以再扩展 `UpperBody`、`Additive`。
3. 动画层不等于状态层。

---

## 4.4 状态动画输出定义：StateAnimationProfile

新增状态对动画层的声明：

```csharp
[Serializable]
public sealed class StateAnimationProfile
{
    public AnimationLayerType OutputLayer = AnimationLayerType.Locomotion;
    public bool OverrideLowerLayers = false;
}
```

第一版语义：

1. `Locomotion` 状态通常输出到 `Locomotion`。
2. `Action` 状态通常输出到 `Action`。
3. `OverrideLowerLayers=true` 表示当前动画层会覆盖更低层表现。

第一版里：

1. `Action` 输出默认覆盖 `Locomotion`。
2. 这里只定义表现关系，不定义状态推进关系。

---

## 4.5 StateConfig 重定义建议

新的 `StateConfig` 建议拆成下面这种结构：

```csharp
[Serializable]
public sealed class StateConfig : IRuntimeTagContainerOwner
{
    public string StateId = "state_001";
    public string StateName = "New State";
  public StateLayerType Layer = StateLayerType.Locomotion;

    public string AnimationClipPath = string.Empty;
    public StateAnimationProfile Animation = new StateAnimationProfile();

    public string DefaultNextStateId = string.Empty;
    public StateTimelineConfig Timeline = new StateTimelineConfig();
    public TagContainer Tags = new TagContainer();

    [NonSerialized] private RuntimeTagContainer _runtimeTags;
    public RuntimeTagContainer RuntimeTags => _runtimeTags ??= new RuntimeTagContainer();
}
```

设计理由：

1. 保留固定 `StateLayerType Layer`，符合第一版求稳方向。
2. `Animation` 单独承载表现输出信息。
3. 第一版不引入 `Control` 或 `Abilities` 结构。
4. 先只把层级和动画层关系定义清楚。

---

## 5. 第一版建议移除或废弃的字段

以下字段不符合新设计，应进入废弃列表：

### 5.1 StateLayerType

当前问题：

1. 它容易让运行时直接写成按层名特判分支。

建议：

1. 第一版保留。
2. 但运行时不允许围绕它写死 Tick 分支。
3. 未来加层时再扩枚举和优先级定义。

### 5.2 StatePresentationMode

当前问题：

1. 语义和动画覆盖关系耦合不清。
2. 同时承担了逻辑层与表现层含义。

建议：

1. 第一版设计上废弃。
2. 用 `StateAnimationProfile` 表达输出层和覆盖关系。

### 5.3 StateAnimationSlot

当前问题：

1. 它本质上更接近动画层概念。
2. 但命名和旧状态层设计混在一起。

建议：

1. 第一版设计上替换为 `AnimationLayerType`。
2. 只保留 `Locomotion` / `Action` 两个最小层。

### 5.4 ControlsMovement / ControlsRotation / BlocksLocomotionAnimation / LocomotionImpactMode

当前问题：

1. 命名带旧逻辑痕迹。
2. 里面混合了“控制输出”和“跨层直接影响”两种语义。

建议：

1. 第一版设计上直接移出最小模型。
2. 后续 abilities/tag 方案确定后再重做。

### 5.5 IsLayerDefaultState / IsActionReleaseState / SafeFallbackStateId

当前问题：

1. 强依赖特定层规则。
2. 只服务 Locomotion / Action 特化实现。

建议：

1. 第一版设计上移除。
2. 层默认态应移动到 `UnitConfig.LayerDefaultStates`。

---

## 6. 运行时数据结构重定义建议

## 6.1 LayerRuntimeState

第一版运行时仍然可以保留按层运行时容器，但结构应当更清晰地表达“统一循环推进”。

建议结构：

```csharp
internal sealed class LayerRuntimeState
{
  public StateLayerType Layer = StateLayerType.None;
    public int Priority = 0;
    public string DefaultStateId = string.Empty;
    public ActiveStateRuntime Current;
    public StateTransitionRequest PendingRequest;
}
```

说明：

1. `Layer` 仍是固定枚举。
2. `Priority` 来自统一的层优先级定义。
3. 推进时应先整理层列表，再统一循环 Tick。

---

## 7. 第一版动画运行时数据建议

## 7.1 AnimationLayerRequest

为了让动画桥明确区分状态层和动画层，建议单独定义表现请求：

```csharp
public sealed class AnimationLayerRequest
{
  public StateLayerType SourceLayer = StateLayerType.None;
    public string SourceStateId = string.Empty;
    public AnimationLayerType AnimationLayer = AnimationLayerType.None;
    public string AnimationClipPath = string.Empty;
    public float ElapsedTime = 0f;
    public bool OverrideLowerLayers = false;
}
```

说明：

1. 状态机只产出请求。
2. 动画桥根据多个请求做表现仲裁。
3. 第一版只处理 `Locomotion` 和 `Action`。

---

## 8. 第一版推荐默认配置

第一版建议角色默认定义两层默认状态：

```csharp
LayerDefaultStates:
  - Layer = Locomotion
    DefaultStateId = "idle"

  - Layer = Action
    DefaultStateId = ""
```

典型状态配置：

```csharp
idle:
  Layer = Locomotion
  Animation.OutputLayer = Locomotion
  Animation.OverrideLowerLayers = false

skill_1_cast:
  Layer = Action
  Animation.OutputLayer = Action
  Animation.OverrideLowerLayers = true
```

这正对应你当前想先做通的最小模型。

---

## 9. 本轮数据结构调整结论

本轮新的第一版数据结构重点不是“把功能一次定义完”，而是先完成三件事：

1. 保留固定 `StateLayerType` 枚举，但运行时必须按统一层循环推进。
2. 用 `UnitConfig.LayerDefaultStates` 定义每层默认状态。
3. 用 `AnimationLayerType + StateAnimationProfile` 拆开状态层与动画层。
4. 第一版不引入 `StateControlProfile`、`Abilities` 或控制输出模型。

在这个基础上，后续再加 tag 化 ability、upperbody、additive，结构也不会再打架。
