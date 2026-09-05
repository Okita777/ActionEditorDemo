# Animancer 与 KCC 根运动管线设计

## 1. 目标

本项目以 Animancer 负责动画求值，以 Kinematic Character Controller（KCC）作为角色世界位置和旋转的唯一执行者。

目标链路：

```text
Animancer
  → Animator 求值
  → RootMotionSource.OnAnimatorMove 采集完整 Pose Delta
  → CustomCharacterController 原子消费
  → KCC 执行位移、旋转和碰撞约束
```

必须满足：

1. Animator 不与 KCC 同时写角色根 Transform。
2. 位移与旋转属于同一次 Root Motion 消费，不允许独立清空。
3. 只有成功消费或明确拒绝时才能清除缓存。
4. Root Motion 旋转不经过输入朝向的角速度限制。
5. 地面角色默认只把动画 Yaw 应用到机械胶囊，Pitch/Roll 保留在视觉姿势中。
6. 保留前向、侧向、垂直权重及反向位移过滤。
7. 运行时必须能观察 Root Motion 在采集、消费或丢弃中的状态。

## 2. 所有权

### 2.1 Animancer 与 Animator

负责：

- 播放和混合动画；
- 计算 `Animator.deltaPosition`；
- 计算 `Animator.deltaRotation`。

不负责直接移动正式角色根 Transform。

### 2.2 RootMotionSource

负责：

- 在 `OnAnimatorMove` 中成对采集位移和旋转；
- 将若干 Animator 求值累积为一个 `RootMotionDelta`；
- 通过 `TryConsume` 原子移交数据；
- 记录采集、消费和丢弃原因；
- 在没有 KCC 消费者时报警。

它不直接修改 Transform。

### 2.3 CustomCharacterController

负责：

- 根据当前 `StateMovementProfile` 决定是否接受平移和旋转；
- 对位移执行轴权重和反向过滤；
- 对旋转执行权重及 Yaw-only 过滤；
- 将位移转换为本次 KCC 模拟所需速度；
- 在 `UpdateRotation` 中完整应用动画旋转增量。

### 2.4 KCC

负责：

- 世界位置和旋转的最终写入；
- 碰撞、接地、斜坡和台阶；
- 对 Root Motion 位移进行环境约束。

## 3. 原子 RootMotionDelta

一次可消费数据包含：

```text
RootMotionDelta
├─ PositionDelta
├─ RotationDelta
└─ SampleCount
```

`OnAnimatorMove` 可以在两次 KCC 更新之间执行多次，因此允许累积多个采样；但 KCC 获取时必须一次取得完整快照。

`TryConsume` 成功后才清除 pending 数据。以下情况使用显式 `DiscardPending(reason)`：

- Animator 已关闭 Root Motion；
- 当前状态策略不接受任何 Root Motion 通道；
- 本地时间被冻结；
- 组件禁用或生命周期结束。

## 4. 位移规则

`Animator.deltaPosition` 按世界空间缓存。消费时以当前机械根为基准转换到角色局部空间，应用：

- `RootMotionForwardWeight`；
- `RootMotionSideWeight`；
- `RootMotionVerticalWeight`；
- `AllowBackwardRootMotion`。

之后转换回世界空间并除以本次 KCC `deltaTime`，作为 Root Motion 速度提交。

后续机械根与视觉根分离后，应进一步将每次采样的局部基准固化在采样记录中，避免快速转向期间跨姿态解释累计位移。

## 5. 旋转规则

Root Motion 的 `deltaRotation` 是动画本帧已经确定的旋转增量，不是“希望朝向某方向”。因此：

- `MoveDirection`、`TargetDirection` 等继续使用角速度求解；
- `RootMotion` 直接应用消费到的旋转增量；
- 不使用 `Quaternion.RotateTowards` 截断 Root Motion；
- 默认只提取绕角色 Up 的 Twist/Yaw，避免根骨 Pitch/Roll 驱动胶囊倾斜和左右摆动；
- 特殊动作可关闭 Yaw-only，接受完整旋转。

## 6. 生命周期与失败行为

- 单独挂载 `RootMotionSource` 会让 Animator 显示 `Handled by Script`，但组件不会直接移动角色。
- 如果找不到启用的 `CustomCharacterController`，运行时输出一次明确警告。
- 正式 KCC 角色不回退到 `ApplyBuiltinRootMotion`，避免 Animator 与 KCC 抢写同一 Transform。
- 组件禁用时清理未消费数据。

## 7. 当前阶段与后续阶段

### 第一阶段：核心管线修复

- 原子采集和消费；
- 显式丢弃；
- Root Motion 旋转绕过普通转向限速；
- Yaw-only；
- 运行时观测和消费者报警。

### 第二阶段：机械根与视觉根分离

推荐最终预制体结构：

```text
CharacterRoot (KCC)
└─ VisualRoot (Animator + Animancer + RootMotionSource + 模型)
```

这将从结构上消除 Animator 和 KCC 对同一 Transform 的耦合。实施时需要同步更新挂点、动画桥、相机和预览实例，不在第一阶段直接改动现有预制体。

### 第三阶段：多层动画 Root Motion 所有权

- 为 Action/Locomotion 层建立明确 owner；
- 状态切换时隔离旧动画残留；
- 对关键位移动作支持“视觉淡入、目标 Root Motion 独占”。

## 8. 验收矩阵

| 配置 | 预期 |
|---|---|
| Rotation/XZ 均 Bake | 无机械位移和转向，不抖动 |
| Rotation Bake，XZ 不 Bake | 只有位移 |
| Rotation 不 Bake，XZ Bake | 只有转向 |
| Rotation/XZ 均不 Bake | 位移和转向完整同步 |
| 碰墙 | KCC 阻止穿墙 |
| `AllowBackwardRootMotion=false` | 只过滤局部负 Z 位移 |
| 无消费者 | 报警，不静默伪装为已正常应用 |
| 30/60/120 FPS | 最终位移和旋转结果近似一致 |
