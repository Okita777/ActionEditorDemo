# Skill 动画切换到 Animancer 更新说明

## 1. 文档目的

本文档只回答一件事：

- 当前 `Skill -> MetaSkill` 的运行时动画播放，从自写 `Playable` 方案切换到 `Animancer` 时，哪些部分需要替换，哪些部分不需要动，以及准备如何替换。

本文档是一次“更新说明”，不是重写整套技能系统。

目标是：

- 停止继续扩展当前自写 `Playable` 动画桥
- 明确 Animancer 接入范围
- 明确编辑器、运行时、数据层分别要不要改
- 明确后续实现步骤

## 2. 当前结论

当前项目里，技能运行时主链已经打通到：

- `SkillPlayerController` 能根据输入触发 `Skill`
- `SkillRuntime` 能根据 `SkillEvent` 切到目标 `MetaSkill`
- `MetaSkillRuntime` 能进入目标 `MetaSkill`
- `MetaSkillConfig.AnimationClipPath` 能解析出正确的 `AnimationClip`

当前没有稳定打通的是：

- 将这个 `AnimationClip` 稳定地作用到角色最终显示姿态

也就是说，问题已经不是：

- 数据有没有存对
- `Skill` / `MetaSkill` 有没有切过去
- 动画资源有没有拿到

而是：

- 当前自写 `PlayableGraph` 方案没有稳定接管角色最终姿态

因此，本次更新决定：

- 运行时动画播放切换到 `Animancer`
- 保留 `Skill -> MetaSkill -> Timeline` 逻辑主链
- 只替换“动画桥”这一层

## 3. 不变的部分

以下内容不需要因为接入 Animancer 而推翻：

### 3.1 技能编辑器与图数据

- `SkillConfig`
- `MetaSkillConfig`
- `SkillEvent`
- `MetaSkillNode`
- `MetaSkillTimeline`
- `HitBox`
- `Effect`

这些编辑器数据仍然是当前正式数据源。

### 3.2 数据存储方式

仍然保留：

- 编辑器配置保存为 `json`
- 运行时加载使用 `.byte`

对应目录不变：

- `Assets/SkillEditor/Data/Skills`
- `Assets/SkillEditor/Data/MetaSkills`
- `Assets/SkillEditor/Compiled/Skills`
- `Assets/SkillEditor/Compiled/MetaSkills`

### 3.3 动画字段本身

`MetaSkillConfig.AnimationClipPath` 继续保留，不改字段名。

原因：

- 当前编辑器已经围绕它构建了选择、拖拽、筛选和显示逻辑
- 当前它已经能稳定定位到目标 `AnimationClip`
- 现在要替换的是“拿到 clip 以后怎么播”，不是“怎么存 clip”

### 3.4 技能运行时主链

以下主链保持不变：

- `SkillPlayerController`
- `SkillRuntime`
- `MetaSkillRuntime`
- `MetaSkillTimelineRuntime`

Animancer 不负责驱动技能逻辑。

Animancer 只负责：

- 角色动画表现层

## 4. 需要替换的部分

### 4.1 运行时动画桥

当前文件：

- `Assets/SkillEditor/Runtime/Runtime/Skill/SkillCharacterActionBridge.cs`

当前状态：

- 这是一个自写的 `PlayableGraph` 动画桥
- 它自己创建 `AnimationClipPlayable`
- 自己连接 `AnimationPlayableOutput`
- 自己尝试把 clip 推给角色 `Animator`

本次替换方向：

- 保留 `SkillCharacterActionBridge` 这个组件名和对外职责
- 但内部实现从“手写 PlayableGraph”切到“Animancer 驱动”

替换后职责变为：

- 找到角色上的 `AnimancerComponent`
- 从 `MetaSkillConfig.AnimationClipPath` 解析 `AnimationClip`
- 调 Animancer 播放该 clip
- 根据 `MetaSkillRuntime` 的时间同步动画状态
- 在技能退出时停止或切回基础状态

### 4.2 动画播放接口

当前接口：

- `ICharacterActionBridge.PlayMetaSkillAnimation(...)`
- `ICharacterActionBridge.SyncMetaSkillAnimation(...)`
- `ICharacterActionBridge.StopMetaSkillAnimation(...)`

对应文件：

- `Assets/SkillEditor/Runtime/Interfaces/ICharacterActionBridge.cs`

处理方式：

- 接口本身保留，不改调用点
- 由 `SkillCharacterActionBridge` 的实现改为基于 Animancer

这样可以保证：

- `MetaSkillRuntime` 不需要重写
- 逻辑调用点不需要大面积扩散改动

### 4.3 当前自写 Playable 细节逻辑

以下逻辑将被移除或废弃：

- `PlayableGraph` 手动创建
- `AnimationClipPlayable` 手动创建
- `AnimationPlayableOutput` 手动连接
- `LateUpdate()` 中手动 `Evaluate`
- 手动维护 `_skillAnimationGraph`
- 手动维护 `_skillAnimationPlayable`
- 手动维护 `_pendingAnimationTime`

它们属于当前失败方案的内部细节，不再继续保留和扩展。

### 4.4 运行时动画目录是否保留

当前相关文件：

- `Assets/SkillEditor/Runtime/Runtime/Skill/SkillAnimationRuntimeCatalog.cs`
- `Assets/SkillEditor/Runtime/Runtime/Skill/SkillAnimationCatalog.cs`
- `Assets/SkillEditor/Editor/SkillResourceRepository.cs` 中的 `RebuildAnimationCatalog()`

处理方式：

- 第一阶段先保留，不立即删除
- Animancer 接入时仍然可以继续复用“根据 `AnimationClipPath` 找到 `AnimationClip`”这条资源解析链

原因：

- Animancer 负责“播放”
- 它不替你决定“怎么从当前配置串找到 clip”

也就是说：

- 资源解析层可以先不动
- 表现播放层替换为 Animancer

后续如果确认 `SkillAnimationRuntimeCatalog` 没有存在必要，再决定是否收敛

## 5. 编辑器预览是否需要变化

### 5.1 MetaSkill 编辑器里的动画选择

当前文件：

- `Assets/SkillEditor/Editor/MetaSkillInfoWindow.cs`
- `Assets/SkillEditor/Editor/SkillEditorInspectorWindow.cs`
- `Assets/SkillEditor/Editor/SkillAnimationReferenceUtility.cs`
- `Assets/SkillEditor/Editor/SkillAnimationPickerWindow.cs`
- `Assets/SkillEditor/Editor/SkillAnimationSelectionUtility.cs`

处理方式：

- 不需要改

原因：

- 这些编辑器逻辑解决的是“如何挑选并保存正确的 AnimationClip 引用”
- 不是“运行时如何播放”

### 5.2 MetaSkillTimeline 编辑器预览

当前编辑器时间轴预览，本质上是编辑器态可视化，不是正式 runtime。

处理方式：

- 第一阶段不改

原因：

- 当前更高优先级是 Play 模式下真实播放技能动画
- 编辑器态预览可以继续沿用现有方式

### 5.3 预览单位配置界面

当前文件：

- `Assets/SkillEditor/Editor/PreviewUnitConfigEditor.cs`
- `Assets/SkillEditor/Editor/SkillPreviewUnitInspectorWindow.cs`

处理方式：

- 不需要因为 Animancer 改技能槽位 UI

可能需要新增的仅是：

- 预览单位 prefab 需要挂 `AnimancerComponent`
- 在检查逻辑中，把“是否存在 Animator”升级为“是否存在 Animator + AnimancerComponent”

## 6. Runtime 要如何改成 Animancer 播放

### 6.1 最小替换原则

不改这一层：

- `SkillPlayerController`
- `SkillRuntime`
- `MetaSkillRuntime` 的调用结构

只改这一层：

- `SkillCharacterActionBridge`

### 6.2 目标结构

建议角色上具备：

- `Animator`
- `AnimancerComponent`
- `SkillCharacterActionBridge`

其中：

- `AnimancerComponent` 负责真正播放 clip
- `SkillCharacterActionBridge` 负责把技能运行时请求转成 Animancer 播放命令

### 6.3 建议播放流程

当 `MetaSkillRuntime.Enter()` 被调用时：

1. 读取当前 `MetaSkillConfig.AnimationClipPath`
2. 解析出 `AnimationClip`
3. 调用 `AnimancerComponent` 播放这个 clip
4. 拿到 Animancer 返回的 state
5. 将该 state 标记为“当前技能动画状态”

当 `MetaSkillRuntime.Tick()` 被调用时：

1. 根据技能 timeline 时间，同步 Animancer state 的时间
2. 技能时间依然是主时钟
3. 动画只是表现层跟随，不反向驱动技能逻辑

当 `MetaSkillRuntime.Exit()` 被调用时：

1. 停止当前技能动画 state
2. 或切回基础角色状态

### 6.4 为什么仍然要保留“技能时间轴是主时钟”

因为当前项目的核心设计是：

- `HitBox`
- `Effect`
- `MetaSkillEvent`

都由 `MetaSkillTimelineRuntime` 驱动。

这意味着：

- 动画必须从属于技能 timeline
- 不能反过来让动画状态机决定技能逻辑时机

Animancer 接入后，也仍然遵守这个原则。

## 7. 需要替换的具体代码点

### 7.1 运行时桥实现

需要替换：

- `Assets/SkillEditor/Runtime/Runtime/Skill/SkillCharacterActionBridge.cs`

替换内容：

- 从手写 PlayableGraph 切换到 Animancer 播放

### 7.2 预览单位有效性检查

建议补充检查：

- `Assets/SkillEditor/Editor/SkillEditorInspectorWindow.cs`
- `Assets/SkillEditor/Editor/SkillPreviewUnitInspectorWindow.cs`

替换内容：

- 原本只检查 Animator
- 更新为检查 Animator + AnimancerComponent

### 7.3 运行时动画播放依赖组件

如果当前预览单位 prefab 还没挂 Animancer，需要：

- 在预览单位 prefab 上补挂 `AnimancerComponent`

这一步是场景/资源层操作，不是技能数据层操作。

## 8. 第一阶段不做的事情

本次切换到 Animancer，第一阶段先不做：

- 不改 `MetaSkillConfig.AnimationClipPath` 字段结构
- 不改 `Skill` / `MetaSkill` 数据模型
- 不改 `MetaSkillTimeline` 编辑器预览方式
- 不改 `Effect` / `HitBox` / `MetaSkillEvent` 的运行逻辑
- 不改被动技能逻辑
- 不改技能槽位逻辑
- 不引入新的动画状态配置资源

也就是说：

- 这次只替换播放桥，不重构整套系统

## 9. 建议实施顺序

### 第一步

把 `SkillCharacterActionBridge` 改成 Animancer 实现。

验收标准：

- `Q` 触发后，预览单位能播放目标 `MetaSkill` 动画

### 第二步

补充预览单位检查逻辑。

验收标准：

- 没有 `AnimancerComponent` 的 prefab，会在编辑器里被明确提示

### 第三步

根据实际运行情况，决定是否保留 `SkillAnimationRuntimeCatalog`。

验收标准：

- 动画引用解析链稳定、可解释、可维护

## 10. 最终原则

本次更新不推翻技能系统。

本次更新只把“技能动画播放桥”从自写 Playable 替换成 Animancer。

整体原则是：

- 数据层不动
- 技能逻辑层不动
- 时间轴逻辑不动
- 编辑器配置入口基本不动
- 只替换运行时动画表现层

这样可以以最小改动，先把“技能动画在 Play 模式下真实播放出来”这件事情做成。

## 11. 追加说明：动画过渡不能只靠简单 Play

当前如果只是：

- `Animancer.Play(clip)`

那只能证明“能播”，但还不能满足正式技能系统需求。

原因是正式技能动画至少还要覆盖：

- 切入时的过渡时长
- 过渡时长的解释方式
- 混合模式
- Root Motion
- IK

因此，后续 runtime 设计不能停留在“直接播 clip”，而是要把动画播放参数变成 `MetaSkillTimeline` 的正式数据。

## 12. 过渡配置应该放在哪

### 12.1 当前问题

当前 `MetaSkillTimelineConfig` 只有：

- `Duration`
- `Tracks`

而当前编辑器 Timeline 顶部本来就有一条 Animation 行，但这个行还没有正式的“动画表现配置数据”。

所以现在的问题不是 Animancer API 不够，而是：

- `MetaSkillTimeline` 还没有用于描述动画过渡策略的数据结构

### 12.2 建议落点

建议在 `MetaSkillTimelineConfig` 下新增一个明确的动画配置对象，例如：

```text
MetaSkillTimelineConfig
	- Duration
	- Animation
	- Tracks

MetaSkillTimelineAnimationConfig
	- TransitionDuration
	- TransitionTimeUnit
	- FadeMode
	- StartTime
	- StartTimeUnit
	- RootMotionPolicy
	- ApplyAnimatorIK
	- ApplyFootIK
```

这里的重点是：

- `AnimationClipPath` 仍然放在 `MetaSkillConfig`
- `MetaSkillTimeline.Animation` 负责描述“怎么进入这段动画、怎么混、怎么处理运动和 IK”

也就是说：

- `MetaSkillConfig.AnimationClipPath` 解决“播哪个 clip”
- `MetaSkillTimeline.Animation` 解决“这个 clip 以什么方式进入和驱动”

### 12.3 建议字段语义

建议先引入以下概念：

```text
AnimationTransitionTimeUnit
	- FixedSeconds
	- NormalizedDuration

AnimationStartTimeUnit
	- FixedSeconds
	- NormalizedTime

AnimationRootMotionPolicy
	- KeepAnimatorSetting
	- ForceEnable
	- ForceDisable

AnimationToggleMode
	- KeepDefault
	- ForceEnable
	- ForceDisable
```

说明：

- `TransitionDuration`：过渡时长数值
- `TransitionTimeUnit`：这个数值按秒解释，还是按动画归一化时长解释
- `FadeMode`：映射到 Animancer 的 `FadeMode`
- `StartTime`：技能动画起播点
- `StartTimeUnit`：按秒还是按归一化时间解释
- `RootMotionPolicy`：是否覆盖角色当前的 `Animator.applyRootMotion`
- `ApplyAnimatorIK`：是否覆盖该技能动画状态的 `Animator IK`
- `ApplyFootIK`：是否覆盖该技能动画状态的 `Foot IK`

### 12.4 为什么过渡配置要挂在 Timeline，而不是挂在 MetaSkill 根上

因为这组数据本质上属于：

- 动画在这段技能时间轴里的表现方式

而不是：

- 这个 MetaSkill 资源的静态标识信息

另外，从编辑器结构上看，`MetaSkillTimeline` 已经有单独的 Animation 行，因此把过渡和混合配置挂在 Timeline 侧，后续 UI 也更自然。

## 13. Runtime 里要怎么用这些过渡配置

### 13.1 基本策略

后续 `SkillCharacterActionBridge` 不应该再直接调用最简单的：

- `Play(clip)`

而应该变成：

1. 读取 `MetaSkillConfig.AnimationClipPath`
2. 读取 `MetaSkillTimeline.Animation`
3. 计算实际 fade duration
4. 选择对应的 `FadeMode`
5. 调用 Animancer 播放并得到 state
6. 设置 state 的起播时间
7. 设置 IK / FootIK / RootMotion 策略
8. 在后续 Tick 中继续用技能 timeline 同步动画时间

### 13.2 过渡时长的换算规则

如果：

- `TransitionTimeUnit = FixedSeconds`

则：

- 直接把 `TransitionDuration` 当秒数使用

如果：

- `TransitionTimeUnit = NormalizedDuration`

则：

- `actualFadeDuration = clip.length * TransitionDuration`

这样设计的原因是：

- 技能作者有时想精确写秒数
- 有时想写“过渡占动画长度的百分比”

### 13.3 FadeMode 的处理

Animancer 已经提供 `FadeMode`，至少包括：

- `FixedSpeed`
- `FixedDuration`
- `FromStart`
- `NormalizedSpeed`
- `NormalizedDuration`
- `NormalizedFromStart`

因此运行时不应该重新发明一套过渡算法，而应该：

- `MetaSkillTimeline.Animation.FadeMode` 直接映射到 Animancer 的 `FadeMode`

### 13.4 时间同步和过渡并不冲突

当前技能系统的主时钟仍然是：

- `MetaSkillTimelineRuntime`

这条原则不变。

即使技能动画用了过渡，后续仍然应该在 `SyncMetaSkillAnimation(...)` 中：

- 把动画 state 的时间同步到技能 timeline 时间

换句话说：

- 过渡只影响权重变化
- 技能 timeline 仍然决定动画采样时间

这样可以保证：

- 命中框
- 特效
- 事件

仍然和技能时间轴对齐，而不是被动画自己带着跑。

## 14. Root Motion 和 IK 的方案

### 14.1 Root Motion 不能直接“全局打开就算完”

Root Motion 的风险在于：

- 角色平时可能由移动控制器驱动
- 技能期间某些动作需要动画位移
- 还有些技能动作只要姿态，不要位移

所以不能简单地把角色的 `Animator.applyRootMotion` 永久打开。

建议方案是：

- 把 Root Motion 是否生效，作为 `MetaSkillTimeline.Animation` 的配置
- 技能进入时按策略切换
- 技能退出时恢复角色原本设置

### 14.2 Root Motion 的策略建议

建议第一阶段先支持这三种：

- `KeepAnimatorSetting`
- `ForceEnable`
- `ForceDisable`

后续如果角色移动系统需要更严格控制，再增加：

- `ManualConsume`

也就是：

- 不让 Animator 直接推动 Transform
- 而是由角色移动组件读取 `deltaPosition / deltaRotation` 后自行消费

### 14.3 IK 的策略建议

Animancer 本身支持：

- `ApplyAnimatorIK`
- `ApplyFootIK`

所以技能动画层建议也做成可配，而不是写死。

建议第一阶段：

- `KeepDefault`
- `ForceEnable`
- `ForceDisable`

适用方式：

- 技能 state 创建后，根据配置给该 state 或 layer 设置 IK 开关

### 14.4 这意味着什么

这意味着未来的技能动画桥，不只是“播 clip”，而是要成为一个真正的表现桥，负责：

- 过渡
- 时间同步
- Root Motion 策略切换
- IK 策略切换

## 15. AnimatorController 和 Animancer 共存问题

### 15.1 直接混用会有问题

如果角色身上的 `Animator` 仍然挂着传统 `AnimatorController`，同时技能又想用独立的 `AnimancerComponent.Play(clip)` 来播，确实会有问题。

问题不在于 Animancer 不能播，而在于：

- 你会得到两套不同的动画驱动思路
- 一套是 AnimatorController 自己在播
- 一套是 Animancer 想接管同一个 Animator 的输出

如果不统一到一张图里，后果通常是：

- 谁在最终驱动姿态不稳定
- 基础待机/移动和技能动作切换关系不清楚
- Root Motion 来源不清楚
- 参数与状态同步会越来越乱

所以正式方案不能是：

- “角色平时继续靠外部 AnimatorController 播”
- “技能来了再临时用一个独立 Animancer 抢输出”

### 15.2 正式方案：统一到 Animancer 图里

如果角色本来就依赖 AnimatorController 做基础移动/待机/受击等动画，建议采用：

- `HybridAnimancerComponent`

原因是它本身就是为这种情况准备的：

- 主体播放一个 `RuntimeAnimatorController`
- 同时允许播放独立 `AnimationClip`

这样最终结构变成：

- 角色所有动画仍然统一由 Animancer 管理
- 基础 Controller 只是变成 Animancer 图里的一个状态来源
- 技能 clip 也是在同一张图里混入

这才是可维护的共存方式。

### 15.3 建议的角色侧分流策略

建议分成两类角色：

#### A. 没有基础 AnimatorController 的角色

使用：

- `AnimancerComponent`

适合：

- 纯技能预览角色
- 动画逻辑还没形成正式 controller 的测试载体

#### B. 已经依赖 AnimatorController 的正式角色

使用：

- `HybridAnimancerComponent`

适合：

- 平时跑待机 / 移动 / 转身 / 受击的角色
- 技能只是其中一个附加表现层的正式角色

### 15.4 对技能桥的影响

这意味着 `SkillCharacterActionBridge` 后续不应该只假设：

- “找到一个 `AnimancerComponent` 就播放”

而应该升级成：

- 优先兼容 `HybridAnimancerComponent`
- 如果存在基础 Controller，则先确保它由 Hybrid 作为基础状态管理
- 技能动画再从同一套 Animancer 图里切入或混入

### 15.5 我对这个问题的最终结论

结论很明确：

- `AnimatorController` 和技能动画不能长期以“两套独立播放系统”并行驱动同一个 Animator
- 正式方案必须统一到 Animancer 这张图里
- 如果角色原本有 controller，就用 `HybridAnimancerComponent`
- 如果角色没有 controller，普通 `AnimancerComponent` 就够

## 16. 后续实现上的调整

基于上面的要求，后续 runtime 改造不能停在当前这一步。

接下来真正要补的是：

1. 给 `MetaSkillTimelineConfig` 增加动画过渡配置对象
2. 在 `MetaSkillTimelineEditorWindow` 的 Animation 行暴露这些配置
3. 在 runtime 中按配置计算 fade duration 和 fade mode
4. 给技能动画接入 Root Motion / IK 策略切换
5. 把角色侧兼容从 `AnimancerComponent` 升级为 `AnimancerComponent / HybridAnimancerComponent` 双通道支持

## 17. 额外记录

`Cost` 字段当前语义也不够稳定。

后续更合理的方向是：

- 不再只是一个裸 `float`
- 而是“资源类型 + 数值”的组合

例如：

- 蓝量消耗
- 血量消耗
- 怒气消耗

这件事本次先不展开实现，但设计上需要记住，它和技能正式数据模型有关，后面应单独收口。

## 18. 范围收窄：当前只做动画过渡

根据最新要求，当前阶段先明确收窄范围：

- 只做技能动画过渡配置
- 先不做 IK
- 先不做 Root Motion

因此，上一节里关于：

- `RootMotionPolicy`
- `ApplyAnimatorIK`
- `ApplyFootIK`

都暂时不进入本轮实现。

当前这一轮真正要落地的数据，收敛为：

```text
MetaSkillTimelineAnimationConfig
	- TransitionDuration
	- TransitionTimeUnit
	- FadeMode
	- StartTime
	- StartTimeUnit
```

其中最核心的是：

- `TransitionDuration`
- `TransitionTimeUnit`
- `FadeMode`

## 19. 当前阶段的过渡配置方案

### 19.1 建议保留的字段

建议当前只保留以下两组时间语义：

```text
AnimationTransitionTimeUnit
	- FixedSeconds
	- NormalizedDuration

AnimationStartTimeUnit
	- FixedSeconds
	- NormalizedTime
```

说明：

- `TransitionDuration` 决定过渡时长数值
- `TransitionTimeUnit` 决定这个数值按秒解释还是按动画长度比例解释
- `FadeMode` 直接映射 Animancer 的 `FadeMode`
- `StartTime` 决定技能动画从哪里起播
- `StartTimeUnit` 决定起播时间是秒还是归一化时间

### 19.2 Runtime 的使用方式

后续 `SkillCharacterActionBridge` 里，技能切入时不再只是：

- `Play(clip)`

而应该按以下流程：

1. 读取 `MetaSkillConfig.AnimationClipPath`
2. 读取 `MetaSkillTimeline.Animation`
3. 计算实际 fade duration
4. 选择对应 `FadeMode`
5. 播放技能 clip 并拿到 state
6. 设置 state 的起播时间
7. 进入后续 timeline 时间同步

### 19.3 当前阶段不扩展的点

本轮先不引入：

- 技能期间 IK 开关
- 技能期间 Root Motion 策略
- 技能期间多层混合策略
- 技能结束后的复杂恢复策略

原因不是这些不重要，而是：

- 当前最高优先级是把“技能动画切入时的过渡”做成正式配置，并让 runtime 真正消费这组配置

## 20. 关于 AnimatorController 和 Animancer 的兼容结论

### 20.1 你现在的约束是什么

你当前的要求不是：

- Controller 和技能动画长期同时并行播放

而是：

- 平时角色可以由 AnimatorController 驱动普通动画
- 当技能触发时，中断当前普通动画
- 然后播放技能动画
- 技能动画由 Animancer 播

这个约束比“长期双系统并行混播”简单很多。

### 20.2 Animancer 是否能兼容带 Controller 的 Animator

可以，但前提不是“让外部 AnimatorController 和一个独立 Animancer 各管各的”。

更稳的做法是：

- 让 Animancer 接管这个 Animator 的最终播放图
- 把原本的 `RuntimeAnimatorController` 作为 Animancer 图里的基础状态来源

当前导入的 Animancer 包里已经有现成类型：

- `HybridAnimancerComponent`

它的用途就是：

- 主体播放一个 `RuntimeAnimatorController`
- 需要时再切到单独的 `AnimationClip`

也就是说，对“有 Controller 的角色”来说，Animancer 不是不能兼容，而是应该通过 `HybridAnimancerComponent` 去兼容。

### 20.3 对你这个技能场景的具体解释

在你的场景里，更准确的理解应该是：

- 角色平时处于基础 Controller 状态
- 技能触发时，由技能桥请求 Animancer 中断当前基础状态
- 用 Animancer 切入技能 clip
- 技能结束后，再回到基础 Controller 状态

这里的关键是：

- 中断的是“Animancer 图中的基础 Controller 状态”
- 不是让外部 AnimatorController 和技能 Animancer 分别去抢同一个 Animator

### 20.4 当前方案的最终结论

所以，当前方案应当明确分成两类：

#### A. 纯预览 / 没有基础 Controller 的角色

使用：

- `AnimancerComponent`

#### B. 有基础 AnimatorController 的正式角色

使用：

- `HybridAnimancerComponent`

技能播放策略是：

- 平时播放基础 Controller
- 技能触发时切断当前基础状态
- 按 `MetaSkillTimeline.Animation` 的过渡配置切入技能动画
- 技能结束后回到基础 Controller

### 20.5 对当前 runtime bridge 的含义

这意味着 `SkillCharacterActionBridge` 后续还要再收敛一次：

- 不是只支持普通 `AnimancerComponent`
- 而是要优先支持“有 Controller 的角色走 `HybridAnimancerComponent`”

也就是说，当前那版“最小 Animancer 接线”只是第一步；正式方案里，角色如果有基础 Controller，就应该切到 Hybrid 路线。

## 21. 更新后的实施顺序

基于最新范围，后续建议顺序更新为：

1. 给 `MetaSkillTimelineConfig` 增加 `Animation` 过渡配置对象
2. 在 `MetaSkillTimelineEditorWindow` 的 Animation 行暴露过渡配置
3. 把 runtime bridge 改成消费 `TransitionDuration / TransitionTimeUnit / FadeMode / StartTime`
4. 给角色侧补 `AnimancerComponent / HybridAnimancerComponent` 的分流支持
5. 技能结束时恢复到基础 Controller 或默认状态

当前明确不进入这一轮的内容：

1. Root Motion
2. IK
3. `Cost` 资源类型化