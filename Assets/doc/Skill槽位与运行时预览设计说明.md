# Skill 槽位与运行时预览设计说明

## 1. 文档目标

本文档只回答两个问题：

- 技能槽位应该如何设计
- 如何基于预览单位实现 `Skill` 的运行时预览

这里的“预览”不是编辑态伪播放，也不是为了测试而临时搭一套逻辑。

这里的预览，本质上是：

- 预览单位提前配置好技能槽位与技能装配
- 进入 Play 模式后，像正式游戏里的角色一样运行技能
- 编辑器只是提供配置入口和运行时观察入口

因此，这套方案的落脚点仍然是正式运行时能力，而不是独立的测试工具。

## 2. 当前现状

### 2.1 已有能力

当前项目已经具备以下基础：

- `Skill -> MetaSkill -> Timeline -> Effect` 的主运行时链路
- `SkillEditor` 图编辑器，可创建 `MetaSkillNode` 与 `Event`
- `SkillRuntime`，可根据事件在 `MetaSkillNode` 间切换
- `MetaSkillRuntime` 与 `MetaSkillTimelineRuntime`
- `SkillPlayerController`，支持最小输入驱动与短按/长按触发
- 预览单位配置 `PreviewUnitConfig`
- 预览单位 Apply 到场景中的运行机制
- `MetaSkill` 预览链路

### 2.2 当前不足

当前仍然缺少以下正式能力：

- `SkillInfo` 还不能配置主动技能/被动技能
- `SkillInfo` 还不能配置技能目标槽位
- `PreviewUnitConfig` 还没有技能槽位配置
- 预览单位还不能装配 `Skill`
- `SkillPlayerController` 还是手填式槽位绑定，不是从角色技能槽系统读取
- 被动技能目前没有正式的数据和装配入口
- `Skill` 还没有像 `MetaSkill` 一样的运行时预览使用路径

## 3. 设计目标

本轮设计目标只有 4 个：

- 为 `Skill` 建立主动/被动技能的正式分类
- 为角色建立主动技能槽位和被动技能槽位
- 让预览单位可以像正式角色一样预先装配技能
- 在 Play 模式下通过预览单位实际运行技能

本轮不追求的内容：

- 被动技能完整触发体系
- 玩家运行时动态换技能 UI
- 完整输入系统重构
- 编辑态直接播放 `Skill`
- 复杂的被动监听器系统

## 4. 技能槽位的核心原则

### 4.1 技能不保存键位，角色槽位保存键位

技能本身不应该直接配置 `Q/E/R/鼠标左键/鼠标右键`。

原因：

- 键位属于角色输入配置，不属于技能本体
- 同一个技能可以被装到不同角色、不同槽位
- 同一个主动技能在不同角色上可能对应不同键位

因此分层应为：

- `SkillConfig` 负责声明：自己是主动还是被动，应该进入哪类槽位
- 角色槽位负责声明：这个槽位映射哪个键位

### 4.2 预览单位配置技能，本质上等同于正式角色装配技能

预览单位不是一个“临时演示对象”。

它应该被理解为：

- 一个静态配置好的角色实例
- 角色身上有主动槽位和被动槽位
- 某些 `Skill` 被预先装配到了这些槽位中

与正式游戏的区别只是：

- 正式游戏里角色技能可能在运行时动态分配
- 预览单位里技能是提前配置好的

运行逻辑应该完全一致。

## 5. Skill 数据设计

### 5.1 新增技能类型

建议在 `SkillConfig` 中增加技能类型字段：

```text
SkillCastCategory
  - Active
  - Passive
```

含义：

- `Active`：需要装配到主动技能槽位，通常由输入触发
- `Passive`：需要装配到被动技能槽位，不直接依赖输入触发

### 5.2 新增技能目标槽位信息

建议在 `SkillConfig` 中增加目标槽位信息：

```text
SkillSlotGroup
  - Active
  - Passive

TargetSlotIndex : int
```

含义：

- `SkillSlotGroup`：技能希望被装到哪一类槽位
- `TargetSlotIndex`：技能默认目标槽位编号

说明：

- 对主动技能，`SkillSlotGroup` 一般为 `Active`
- 对被动技能，`SkillSlotGroup` 一般为 `Passive`
- `TargetSlotIndex` 主要用于默认装配与编辑器提示，不是强制运行时唯一依据

### 5.3 SkillInfo 应新增的配置项

`SkillInfo` 中应新增：

- `SkillType`：主动技能 / 被动技能
- `TargetSlotGroup`：主动槽 / 被动槽
- `TargetSlotIndex`：目标槽位编号

显示逻辑建议：

- 如果是主动技能，则允许配置主动槽位编号
- 如果是被动技能，则允许配置被动槽位编号

## 6. 角色技能槽位设计

### 6.1 主动技能槽位

主动技能槽位用于装配会被输入触发的技能。

建议结构：

```text
ActiveSkillSlotConfig
  - SlotIndex : int
  - DisplayName : string
  - Key : KeyCode
  - SkillAssetName : string
```

字段说明：

- `SlotIndex`：槽位编号，例如 1、2、3、4
- `DisplayName`：用于编辑器显示，例如 `Q槽`、`E槽`、`R槽`
- `Key`：该槽位绑定的键位，例如 `Q/E/R/Mouse0/Mouse1`
- `SkillAssetName`：该槽位当前装配的技能资源名

### 6.2 被动技能槽位

被动技能槽位用于装配不由输入直接触发的技能。

建议结构：

```text
PassiveSkillSlotConfig
  - SlotIndex : int
  - DisplayName : string
  - SkillAssetName : string
```

字段说明：

- `SlotIndex`：被动槽位编号
- `DisplayName`：用于编辑器显示
- `SkillAssetName`：当前装配的被动技能资源名

被动槽位不需要键位配置。

### 6.3 主动与被动的边界

主动技能：

- 装配到主动槽位
- 槽位映射键位
- Play 模式下由输入触发

被动技能：

- 装配到被动槽位
- 不绑定键位
- 运行时由后续事件系统或角色生命周期驱动

本轮先完成被动技能的“数据层与装配层”，不强行把复杂触发器一起做掉。

## 7. 预览单位配置设计

### 7.1 PreviewUnitConfig 应扩展的内容

当前 `PreviewUnitConfig` 只有：

- 动画筛选
- 挂点
- 武器挂载

建议扩展为：

```text
PreviewUnitConfig
  - AnimationSearchRoot
  - AnimationFilterKey
  - MountPoints
  - WeaponBindings
  - ActiveSkillSlots
  - PassiveSkillSlots
```

其中：

- `ActiveSkillSlots`：预览单位的主动技能槽位列表
- `PassiveSkillSlots`：预览单位的被动技能槽位列表

### 7.2 预览单位为什么要持有技能槽位

原因是预览单位要尽量贴近正式运行时角色。

它不仅要提供：

- 模型
- 动画
- 武器

还要提供：

- 技能装配信息
- 主动槽位与键位映射
- 被动槽位装配信息

只有这样，Play 模式下它才能真实承担“角色运行技能”的职责。

### 7.3 预览单位 Inspector 需要新增的内容

在预览单位相关 Inspector 中，需要新增两个区块：

#### 主动技能槽位

每个条目可编辑：

- `SlotIndex`
- `DisplayName`
- `Key`
- `Skill`

#### 被动技能槽位

每个条目可编辑：

- `SlotIndex`
- `DisplayName`
- `Skill`

## 8. 运行时预览实现方案

### 8.1 预览不在编辑态播放，只在 Play 模式运行

本方案不做“编辑状态直接预览 Skill”。

原因：

- 你明确要求一切为了正式运行时服务
- `Skill` 是一套需要输入、槽位、角色装配共同参与的系统
- 编辑态直接硬放一个 `Skill`，容易再次偏离正式运行时边界

因此这里的预览定义为：

- 进入 Play 模式
- 场景中的预览单位实例已经带好技能槽位与技能装配
- 输入驱动主动技能槽位
- 技能按照正式运行时链路执行

### 8.2 运行时 Skill 装配流程

建议运行时流程为：

```text
PreviewUnitConfig
  -> 提供主动槽位 / 被动槽位配置
  -> SkillLoadoutRuntime / SkillPlayerController 读取配置
  -> 加载槽位上装配的 SkillConfig
  -> 预加载这些 Skill 引用到的 MetaSkillConfig
  -> 为每个已装配的主动技能建立 SkillRuntime
  -> Play 模式下监听槽位对应键位
  -> 触发 SkillRuntime
```

### 8.3 主动技能运行逻辑

主动技能运行流程：

```text
玩家按下槽位键位
  -> 根据槽位找到已装配 Skill
  -> 将输入转换为 SkillEventType
  -> SkillRuntime.Trigger(...)
  -> Skill 进入对应 MetaSkillNode
  -> MetaSkillRuntime 播放 Timeline
  -> HitBox / Bullet / Effect 生效
```

短按/长按延续当前设计：

- 短按 -> `CastSkillShort`
- 长按 -> `CastSkillLong`

对应 `EventInfo` 中的 `event` 配置。

### 8.4 被动技能运行逻辑

本轮不实现完整被动触发系统，但运行时至少要完成：

- 识别被动技能槽位中装配的技能
- 运行时建立被动技能的注册数据
- 为后续事件驱动式触发保留入口

也就是说，本轮被动技能完成的是：

- 数据可配置
- 角色可装配
- 运行时可识别

但不在本轮内继续扩展：

- 受击触发
- 进入区域触发
- 击杀触发
- 常驻监听器

### 8.5 与当前 SkillPlayerController 的关系

当前 `SkillPlayerController` 还是“手填槽位”的最小版本。

建议下一步改造方向：

- 去掉手填 `List<SkillSlotBinding>` 的主入口地位
- 让它改为读取 `PreviewUnitConfig.ActiveSkillSlots`
- 让它成为“角色技能槽位驱动器”而不是“临时绑定表”

可以保留一部分内部结构，如：

- `SkillRuntimeState`
- `Reload()`
- 短按/长按判定
- `PrimaryTarget`

但其数据来源必须升级为“角色槽位配置”。

## 9. SkillInfo 与预览功能的关系

### 9.1 SkillInfo 负责定义技能身份

`SkillInfo` 不直接播放技能。

`SkillInfo` 的职责应该是：

- 定义技能基础信息
- 定义技能类型：主动 / 被动
- 定义目标槽位信息
- 提供打开 `SkillEditor` 的入口

### 9.2 预览能力依赖预览单位

`Skill` 级别的预览不应理解为：

- 单独点一个按钮就直接播放一个 Skill

而应理解为：

- 这个 `Skill` 被装到某个预览单位槽位中
- 运行游戏
- 由预览单位通过正式输入与运行时逻辑执行这个 Skill

因此，真正的“Skill 预览”依赖两个前提：

- 预览单位存在
- 预览单位装配了这个 Skill

## 10. 编辑器需要补充的入口

为了支持上述方案，编辑器需要补充以下入口。

### 10.1 SkillInfo

增加：

- `SkillType`
- `TargetSlotGroup`
- `TargetSlotIndex`

### 10.2 预览单位 Inspector

增加：

- 主动技能槽位配置区
- 被动技能槽位配置区
- 每个槽位对应的技能选择下拉框
- 主动槽位的键位选择

### 10.3 运行时观察入口

建议在后续补一个轻量观察区，用来在 Play 模式下显示：

- 当前预览单位主动槽位装配情况
- 当前触发的 `SkillId`
- 当前 `MetaSkillNode`
- 当前 `MetaSkillId`
- 当前 Timeline 时间

这部分应复用现有 `SkillRuntimeDebugBus` 的运行时快照，而不是再造一套独立预览逻辑。

## 11. 本轮建议实现范围

为了控制工作量并保证结果可用，建议本轮编码范围如下：

### 必做

- `SkillConfig` 增加主动/被动类型
- `SkillConfig` 增加目标槽位信息
- `SkillInfo` 增加上述配置项
- `PreviewUnitConfig` 增加主动槽位与被动槽位配置
- 预览单位 Inspector 支持编辑槽位和装配技能
- `SkillPlayerController` 升级为读取预览单位技能槽位
- Play 模式下主动技能可按槽位键位运行

### 本轮只做装配，不做完整触发

- 被动技能槽位可配置
- 被动技能可装配
- 运行时能识别到被动技能存在

### 暂缓

- 被动技能完整事件系统
- 动态换技能
- 完整输入系统改造
- 编辑态直接播放 Skill

## 12. 实施顺序建议

建议按下面顺序编码：

### 第一步

补 `SkillConfig` 和 `SkillInfo`：

- 主动/被动
- 目标槽位

### 第二步

扩展 `PreviewUnitConfig`：

- 主动技能槽位
- 被动技能槽位

### 第三步

扩展预览单位 Inspector：

- 配置槽位
- 配置键位
- 装配 Skill

### 第四步

重构 `SkillPlayerController`：

- 从预览单位槽位读取配置
- 运行主动技能
- 识别被动技能槽位

### 第五步

补运行时观察入口：

- 通过 DebugBus 查看当前正在执行的 Skill

## 13. 预期结果

按以上方案完成后，应达到以下结果：

- `SkillInfo` 能区分主动技能和被动技能
- `SkillInfo` 能配置目标技能槽位
- 预览单位能配置主动技能槽位和被动技能槽位
- 主动槽位能绑定键位
- 预览单位能装配做好了的 `Skill`
- Play 模式下预览单位可以像正式角色一样释放技能
- 这套逻辑可以直接作为正式游戏角色技能装配系统的静态版本

## 14. 一句话总结

本方案的核心不是“给 Skill 做一个额外预览器”，而是把预览单位建设成一个已经装配好技能槽位和技能的正式运行时角色，从而在 Play 模式下用真实逻辑运行 `Skill`。