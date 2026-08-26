第三版框架主要为了解决 skill 系统和 state 系统的双向通信问题。

先给当前确认后的结论：

1. MetaSkill 不是 State，但 MetaSkill 可以绑定一个 execute state 和一个 recovery state。
2. 这些技能 state 从角色装备技能开始，就应该作为 tmpState 构建并注册到 StateController 下。
3. 技能系统负责决定“能不能 useMetaSkill”，以及在 useMetaSkill 时执行 OnAddEffect 并请求切到对应技能 state。
4. 一旦切进技能 state，后续生命周期完全按普通 State 逻辑运行，由 StateController 统一推进、打断、自然结束和 default next。
5. 状态系统在技能 state 被打断、自然结束、切到下一状态时，必须把结果回通知给技能系统，技能系统再据此调整技能流。

## 1. 当前第三版的核心设计

元技能数据结构：metaSkillId、metaSkillName、metaSkillTags、StatesOfCanInterrupt、EffectsTreeOnAdd、EffectsTreeOnEnd、SkillStateTimeLine(State)、RecoverySkillState(State)。

State 数据结构不变。

之前的 RecoveryAnim 从 SkillTimeLine 里移除。

技能可以拥有两个绑定 State，当然也可以一个都不配；不配时，释放这个 MetaSkill 不会改变角色状态，只执行技能逻辑和效果树。

编辑器上仍然直接嵌入两个 State Inspector 区块，但 runtime 语义必须明确：

1. 编辑器里看起来是“MetaSkill 内嵌编辑 State”。
2. runtime 里这些数据必须被当成 StateController 下的正式 tmpState。
3. nextDefaultState 不由 SkillRuntime 特判硬编码，而是遵守 State 本身配置。
4. 如果存在 recovery state，则 execute state 的默认后继通常指向 recovery state；recovery state 的默认后继通常指向 idle，但本质仍然是 state 配置语义。

## 2. BuildState

这个部分沿用之前正确的思路：根据装配的技能，搜集技能下所有 MetaSkill，再搜集 MetaSkill 绑定的 state，构建并注册对应 tmpState。

关键约束：

1. 这些 tmpState 不是释放瞬间临时 new 出来再私下运行，而是装备完成后就属于 StateController 的可切换状态集合。
2. BuildState 只负责新增、更新和移除这些技能 tmpState，不改写静态 state 的原始打断轨配置。
3. tmpState 保留 StateTags、InterruptTracks、DefaultNextStateId、Timeline 等完整 State 语义。

## 3. 技能释放与状态切换的真实通信机制

这里是第三版必须写清楚的重点。

### 3.1 技能到状态

释放技能仍然走正常技能释放判定，当前 state 能否被打断，只是技能能否释放的一环：

1. 看输入。
2. 看技能是否 CD。
3. 看当前 MetaSkill resource cost 是否足够。
4. 如果当前 MetaSkill 绑定了 SkillStateTimeLineState，则看当前状态是否在 StatesOfCanInterrupt 白名单内。
5. 以上全部通过后，技能系统执行 useMetaSkill。

useMetaSkill 的职责应明确固定为：

1. 建立或激活当前这次技能流上下文。
2. 执行当前 MetaSkill 的 OnAddEffect。
3. 如果该 MetaSkill 绑定了 execute state，则向 StateController 提交一次 `transitionTo(skillState)` 请求。
4. 如果该 MetaSkill 不绑定 state，则仅执行技能逻辑和效果树，不触发状态切换。

也就是说，技能系统驱动“请求进入技能 state”，但它不接管这个 state 后面的运行过程。

### 3.2 状态到技能

一旦进入 skillState，后续就必须走 StateController 的普通 state 管线：

1. StateTimeline 推进。
2. 动画播放。
3. 打断轨判定。
4. default next 判定。
5. 切到 recovery state、idle 或其他普通 state。

但这里不能只有单向驱动，还必须有反向通知：

1. StateController 要能通知技能系统：某个技能 state 已进入。
2. StateController 要能通知技能系统：某个技能 state 正常结束。
3. StateController 要能通知技能系统：某个技能 state 被其他状态打断。
4. 通知里要带上这次状态属于哪个 skill、哪个 MetaSkill、是 execute 还是 recovery。

没有这条反向通知，技能系统就不知道当前 MetaSkill 是正常结束还是被状态中断，也就没法正确处理 OnEndEffect、连段等待和下一段释放。

## 4. 状态被打断后如何影响技能流

这里是这次重新定义后最关键的一条：

技能 state 被打断，打断的是状态表现，不是整个技能系统立即死亡。

正确语义应当是：

1. MetaSkill1 对应的 skillState1 进入后，由 StateController 正常运行。
2. skillState1 途中被其他状态打断。
3. StateController 发出“MetaSkill1 的 skillState 被打断”的通知给技能系统。
4. 技能系统收到后，把 MetaSkill1 标记为“以中断方式结束”。
5. 因为这不是正常结束，所以不触发 MetaSkill1 的 OnEndEffect。
6. 但技能流本身并不必然终止，技能系统应根据连段规则进入“可以继续接收下一段 MetaSkill 输入”的状态。
7. 因此，原本必须等待 MetaSkill1 完整播完才能接 MetaSkill2 的约束，会因为这次 state 中断而提前解除。

换句话说：

1. state 中断会影响当前 MetaSkill 的结束方式。
2. MetaSkill 的结束方式会反过来影响 skill combo 的可衔接时机。
3. 但这不是 SkillRuntime 自己模拟 state 打断，而是 StateController 通知回来以后，SkillRuntime 再改自己的技能流状态。

## 5. OnAddEffect / OnEndEffect 规则

规则固定如下：

1. OnAddEffect 在 useMetaSkill 成功启动时执行。
2. OnEndEffect 只在该 MetaSkill 对应阶段正常结束时执行。
3. 如果 skillState 或 recoveryState 被其他状态打断，则该次结束视为 interrupted end，不触发 OnEndEffect。
4. 如果 MetaSkill 根本不绑定 state，则它的正常完成规则仍由技能系统自身定义，但也必须和是否触发 OnEndEffect 区分清楚。

## 6. continuation 与状态系统的关系

这一条在第三版里必须写死，不然后续实现很容易又退回到 SkillRuntime 手动接管状态切换：

1. MetaSkill 的 execute state 和 recovery state，本质上都是普通 State 语义，不允许因为它们是技能的一部分，就在 runtime 里额外写一套“技能结束后强制切 idle”的特化逻辑。
2. 如果 recovery state 配置了 DefaultNextStateId，那么它必须和普通 State 一样，通过状态系统自己的自然结束流程切到下一个状态。
3. StatesOfCanInterrupt 的职责只用于“能不能从当前外部状态发起这个 MetaSkill 的第一段”，它是技能起手门禁，不应该在同一个 skill 链路内部的 continuation 切段时重复限制后续段。
4. 第一段结束后，如果当前 skill 还允许在规定时间内手动衔接第二段，runtime 应该进入等待 continuation 状态，但这个等待不能继续锁住状态系统本身。
5. 如果在 continuation 时间窗内成功触发下一段，则技能系统重新 useMetaSkill，并再次请求切到下一段对应的技能 state。
6. 如果当前技能 state 被打断，但 continuation 时间窗仍有效，那么下一段依然允许继续触发。

## 7. 主动与被动

主动技能和被动技能不再靠“MetaSkill 类型”二分，而是看是否绑定 state：

1. 绑定 state 的 MetaSkill 会改变角色状态，并和 StateController 双向通信。
2. 不绑定 state 的 MetaSkill 只执行技能逻辑和效果树。
3. 被动触发链路仍可以复用 SkillEvent 与效果树，但不要求切换角色 state。

## 8. 最终结论

我这里要强调的点是：我们没有把 skill 和 state 合并成同一个系统，而是把它们重新接通了。

正确的第三版关系是：

1. skill 保留自己的运行逻辑、节点图、CD、资源、combo 和效果树。
2. state 保留自己的时间轴、打断、默认后继和状态切换控制权。
3. 二者通过明确的双向通信机制协作：
   - skill 发起 useMetaSkill 和切状态请求。
   - state 在状态变化时回通知 skill，影响 MetaSkill 结束方式与后续连段流。

这才是第三版应该锁定的实现方向。
