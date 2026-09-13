using System.Collections.Generic;

namespace AsiActionEngine.RunTime
{
    public partial class ActionStatePart
    {
        private HashSet<int> interruptedIDs = new HashSet<int>(32);
        private List<ActionInterrupt> _actionInterrupts = new List<ActionInterrupt>();
        public bool DisableAutofill = false;//屏蔽预输入
        public bool DisableInput = false;//屏蔽输入 

        //—— 跳转权重选择（每帧一次，挑选本帧加权随机命中的跳转轨）——
        private bool mHasWeightInterrupt;//当前Action是否装载了含权重条件的跳转轨；切Action时一次性算好，避免每帧扫描
        private ActionInterrupt mSelectedWeightInterrupt;//本帧被加权选中的跳转轨
        private float mWeightSelectionStepTime = float.NaN;//记录选择发生的步时，避免跨步/旁路误命中
        private readonly List<ActionInterrupt> mWeightCandidates = new List<ActionInterrupt>(8);
        private readonly List<int> mWeightCandidateValues = new List<int>(8);

        //裁切保护:被裁掉的保护单帧跳转,延后到事件补触发之后再补判。
        //注意:该列表只按 TryLoadInterrupts 的原始装载顺序追加,禁止再按时间或类型排序。
        private readonly List<ActionInterrupt> mCutProtectInterruptsInLoadOrder = new List<ActionInterrupt>(8);

        //立即跳转(JumpNow):切入瞬间已就绪的立即跳转轨,延后到事件补触发之后再执行,
        //保证被链式立即跳转跳过的中间Action的切入事件先于跳转执行,避免事件丢失。
        //注意:该列表只按 TryLoadInterrupts 的原始装载顺序追加,禁止再按时间或类型排序。
        private readonly List<ActionInterrupt> mJumpNowInterruptsInLoadOrder = new List<ActionInterrupt>(8);

        //条件生命周期：当前Action是否装载了含 Enter/Exit 条件的跳转轨；切Action时一次性算好，避免每帧扫描
        private bool mHasLifecycleInterrupt;

        private bool OnInterrupUpdate(float NextTime)
        {
            //条件的 Enter/Exit 必须先于本帧任何条件判定(含权重预评估)结算
            UpdateInterruptLifecycle(NextTime);

            //每步先确定本帧加权选中的跳转轨，供后续条件判定使用
            ComputeWeightSelection(NextTime);

            bool _isKeyDown = false;
            bool _isKeyUp = false;
            bool _isKeyClick = false;
            //foreach (var VARIABLE in mCurActionInterrupt)
            for (int i = 0; i < mCurActionInterrupt.Count; i++)
            {
                ActionInterrupt VARIABLE = mCurActionInterrupt[i];
                if (VARIABLE.Duration == 0)
                {
                    //单帧打断轨
                    if (IsSingleFrameInterruptReached(VARIABLE, NextTime))
                    {
                        //检查条件  满足后直接跳转
                        if (TryRunInterrupt(VARIABLE, CurrentActionState, out _))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    if (IsInterruptExpired(VARIABLE, ElapsedTime))
                    {
                        //当前时间已经大于当前打断轨最末端的时间，因此不再会执行而销毁
                        // curActionInterrupt.Remove(VARIABLE);
                    }
                    else
                    {
                        if (IsInterruptReadyAt(VARIABLE, ElapsedTime))
                        {
                            //检查条件  满足后直接跳转
                            if (TryRunInterrupt(VARIABLE, CurrentActionState, out _))
                            {
                                return true;
                            }
                        }
                        else if (IsInterruptPreInputAt(VARIABLE, ElapsedTime))
                        {
                            //是否屏蔽预输入
                            if (!DisableAutofill && !DisableInput)
                            {
                                //在预输入轨  在这里决定是否重置按键
                                foreach (var _interruptCondition in VARIABLE.InterruptConditionList)
                                {
                                    if (TryKeepPreInputKey(_interruptCondition, ref _isKeyDown, ref _isKeyUp, ref _isKeyClick))
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //预输入轨没有满足按键的条件  所以回收按键
            if (!_isKeyDown) NowInputDownKey = MotionEngineConst.NondKeyName;
            if (!_isKeyUp) NowInputUpKey = MotionEngineConst.NondKeyName;
            if (!_isKeyClick) NowInputClickKey = MotionEngineConst.NondKeyName;

            return false;
        }

        private bool TryLoadInterrupt(ActionInterrupt interrupt)
        {
            if (interrupt is null)
            {
                return false;
            }

            //延后判定的立即跳转/裁切保护轨也会被 TryRunInterrupt 触发 Enter，因此在分流前先置位
            if (interrupt.HasLifecycleCondition)
                mHasLifecycleInterrupt = true;

            bool isReady = IsInterruptReadyAt(interrupt, ElapsedTime);

            if (interrupt.JumpNow && isReady)
            {
                //立即跳转仅在此收集,真正的跳转延后到事件补触发完成后由 TryRunJumpNowInterrupts 执行,
                //保证被链式立即跳转跳过的中间Action的切入事件先于跳转执行(否则事件丢失)。
                mJumpNowInterruptsInLoadOrder.Add(interrupt);
                return false;
            }

            //裁切保护:被裁掉(触发时间 < 当前裁切起点)的保护单帧,仅收集不当场判定;
            //待事件补触发完成后再由 TryRunCutProtectInterrupts 统一补判,保证事件优先于跳转
            if (interrupt.Duration == 0 && interrupt.CutProtect
                && interrupt.GetRealTriggerTime < ElapsedTime)
            {
                mCutProtectInterruptsInLoadOrder.Add(interrupt);
                return false;
            }

            if (TryGetWeightCondition(interrupt, out _, out _))
                mHasWeightInterrupt = true;

            mCurActionInterrupt.Add(interrupt);
            return false;
        }

        private bool TryLoadInterrupts(List<ActionInterrupt> interrupts)
        {
            if (interrupts is null)
            {
                return false;
            }

            foreach (ActionInterrupt interrupt in interrupts)
            {
                if (TryLoadInterrupt(interrupt))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryLoadInterrupts(List<ActionInterrupt> interrupts, int offsetTriggerTime)
        {
            if (interrupts is null)
            {
                return false;
            }

            foreach (ActionInterrupt interrupt in interrupts)
            {
                if (interrupt is null)
                {
                    continue;
                }

                if (TryLoadInterrupt(interrupt.CreateRuntimeCopy(offsetTriggerTime)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 立即跳转(JumpNow)执行:在事件补触发完成后调用,按原始装载顺序执行切入瞬间已就绪的立即跳转。
        /// 延后执行的目的是让被链式立即跳转跳过的中间Action的切入事件先执行,避免事件丢失。
        /// 语义与原装载期立即跳转一致:命中即阻断;条件满足但跳转未发生(跨层/spawn)或单帧轨不再保留;
        /// 多帧轨条件不满足时回填常规判定列表,后续帧继续按区间判断。
        /// </summary>
        private bool TryRunJumpNowInterrupts()
        {
            for (int i = 0; i < mJumpNowInterruptsInLoadOrder.Count; i++)
            {
                ActionInterrupt interrupt = mJumpNowInterruptsInLoadOrder[i];
                if (TryRunInterrupt(interrupt, CurrentActionState, out bool interruptAccepted))
                {
                    return true;
                }

                if (interruptAccepted || interrupt.Duration == 0)
                {
                    continue;
                }

                //多帧轨且条件未满足:回填常规判定列表,后续帧按区间持续判断
                if (TryGetWeightCondition(interrupt, out _, out _))
                    mHasWeightInterrupt = true;
                mCurActionInterrupt.Add(interrupt);
            }
            return false;
        }

        /// <summary>
        /// 裁切保护跳转补判:在事件补触发完成后调用,对被裁掉的保护单帧跳转按常规装载顺序与条件补判一次。
        /// 语义与常规单帧一致——只判一次,条件不满足即作废(列表在下次切入时清空)。
        /// </summary>
        private bool TryRunCutProtectInterrupts()
        {
            for (int i = 0; i < mCutProtectInterruptsInLoadOrder.Count; i++)
            {
                if (TryRunInterrupt(mCutProtectInterruptsInLoadOrder[i], CurrentActionState, out _))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 条件生命周期结算：按跳转轨是否处于判定窗口，成对驱动条件的 Enter/Exit，窗口内每帧驱动 Update。
        /// 单帧轨只在命中帧处于窗口内(次帧自动 Exit)；Action循环回绕时窗口重新计算，条件会重新进入。
        /// </summary>
        private void UpdateInterruptLifecycle(float nextTime)
        {
            if (!mHasLifecycleInterrupt) return;

            for (int i = 0; i < mCurActionInterrupt.Count; i++)
            {
                ActionInterrupt interrupt = mCurActionInterrupt[i];
                if (!interrupt.HasLifecycleCondition) continue;

                if (!IsInterruptLifecycleActive(interrupt, nextTime))
                {
                    ExitInterruptLifecycle(interrupt, false);
                    continue;
                }

                //Enter 当帧不再 Update，避免刚初始化的运行态被同帧推进
                if (interrupt.IsConditionEntered)
                {
                    UpdateInterruptConditions(interrupt);
                }
                else
                {
                    EnterInterruptLifecycle(interrupt);
                }
            }
        }

        /// <summary>跳转轨本帧是否处于条件判定窗口内。镜像主循环的就绪判定。</summary>
        private bool IsInterruptLifecycleActive(ActionInterrupt interrupt, float nextTime)
        {
            if (interrupt.Duration == 0)
            {
                return IsSingleFrameInterruptReached(interrupt, nextTime);
            }

            return !IsInterruptExpired(interrupt, ElapsedTime) && IsInterruptReadyAt(interrupt, ElapsedTime);
        }

        private void EnterInterruptLifecycle(ActionInterrupt interrupt)
        {
            if (interrupt.IsConditionEntered) return;
            interrupt.IsConditionEntered = true;

            List<IInterruptCondition> conditions = interrupt.InterruptConditionList;
            if (conditions is null) return;

            bool isSingle = interrupt.Duration == 0;
            for (int i = 0; i < conditions.Count; i++)
            {
                conditions[i]?.Enter(this, interrupt, isSingle);
            }
        }

        private void UpdateInterruptConditions(ActionInterrupt interrupt)
        {
            List<IInterruptCondition> conditions = interrupt.InterruptConditionList;
            if (conditions is null) return;

            for (int i = 0; i < conditions.Count; i++)
            {
                conditions[i]?.Update(this, interrupt);
            }
        }

        private void ExitInterruptLifecycle(ActionInterrupt interrupt, bool isInterrupt)
        {
            if (!interrupt.IsConditionEntered) return;
            interrupt.IsConditionEntered = false;

            List<IInterruptCondition> conditions = interrupt.InterruptConditionList;
            if (conditions is null) return;

            for (int i = 0; i < conditions.Count; i++)
            {
                conditions[i]?.Exit(this, interrupt, isInterrupt);
            }
        }

        /// <summary>
        /// 卸载当前Action的所有跳转轨前，统一收尾条件生命周期。
        /// 延后列表中的轨道也可能已经 Enter(装载期由 TryRunInterrupt 触发)，一并收尾；
        /// 重复轨道由 IsConditionEntered 保证只 Exit 一次。
        /// </summary>
        private void ExitAllInterruptLifecycle(bool isInterrupt)
        {
            if (!mHasLifecycleInterrupt) return;

            for (int i = 0; i < mCurActionInterrupt.Count; i++)
            {
                ExitInterruptLifecycle(mCurActionInterrupt[i], isInterrupt);
            }
            for (int i = 0; i < mJumpNowInterruptsInLoadOrder.Count; i++)
            {
                ExitInterruptLifecycle(mJumpNowInterruptsInLoadOrder[i], isInterrupt);
            }
            for (int i = 0; i < mCutProtectInterruptsInLoadOrder.Count; i++)
            {
                ExitInterruptLifecycle(mCutProtectInterruptsInLoadOrder[i], isInterrupt);
            }

            mHasLifecycleInterrupt = false;
        }

        private bool TryRunInterrupt(ActionInterrupt interrupt, ActionState changeSource, out bool interruptAccepted)
        {
            interruptAccepted = false;
            //装载期延后判定(立即跳转/裁切保护)与Action结尾补判不经过 UpdateInterruptLifecycle，
            //在此兜底保证条件先 Enter 再被判定
            if (interrupt.HasLifecycleCondition)
            {
                EnterInterruptLifecycle(interrupt);
            }

            if (!TryCheckInterrupCondition(interrupt, mCurUnit, out ActionState targetAction,
                    out int targetActionID, out int targetActionGroupID))
            {
                return false;
            }

            interruptAccepted = true;
            bool changeResult;
            if (IsTem && interrupt.CrossFadeTime != 0)
            {
                changeResult = OnChangeState(targetActionID, interrupt.CrossFadeTime, interrupt.OffsetTime, changeSource);
            }
            else
            {
                //if (ActionStateMachine.CurUnit == EngineResourcesManager.Instance.Player && targetAction.AnimaLayer == 0)
                //    EngineDebug.LogError($"<color=#ccff00>当前切换到 [{targetAction.Name}] AGID[{ActionStateMachine.GetFirstActionGroupID}]");

                changeResult = OnChangeState(targetAction, interrupt.CrossFadeTime, interrupt.OffsetTime, changeSource, false,
                    targetActionGroupID);
            }

            return changeResult;
        }

        private bool IsSingleFrameInterruptReached(ActionInterrupt interrupt, float nextTime)
        {
            int realTriggerTime = interrupt.GetRealTriggerTime;
            return realTriggerTime >= ElapsedTime && realTriggerTime < nextTime;
        }

        private bool IsInterruptReadyAt(ActionInterrupt interrupt, float currentTime)
        {
            int realTriggerTime = interrupt.GetRealTriggerTime;
            if (interrupt.Duration == 0)
            {
                return currentTime == realTriggerTime;
            }

            int executeTime = realTriggerTime + interrupt.ExecuteTime;
            if (currentTime < executeTime)
            {
                return false;
            }

            return interrupt.Duration < 0 || currentTime <= realTriggerTime + interrupt.Duration;
        }

        private bool IsInterruptPreInputAt(ActionInterrupt interrupt, float currentTime)
        {
            if (interrupt.Duration == 0 || interrupt.ExecuteTime <= 0)
            {
                return false;
            }

            int realTriggerTime = interrupt.GetRealTriggerTime;
            int executeTime = realTriggerTime + interrupt.ExecuteTime;
            if (currentTime < realTriggerTime || currentTime >= executeTime)
            {
                return false;
            }

            return interrupt.Duration < 0 || currentTime <= realTriggerTime + interrupt.Duration;
        }

        private bool IsInterruptExpired(ActionInterrupt interrupt, float currentTime)
        {
            return interrupt.Duration > 0 && interrupt.GetRealTriggerTime + interrupt.Duration < currentTime;
        }

        private bool TryKeepPreInputKey(IInterruptCondition interruptCondition, ref bool isKeyDown, ref bool isKeyUp,
            ref bool isKeyClick)
        {
            if (!TryGetInterruptKeyCondition(interruptCondition, out EInputKeyType inputType, out string checkKeyName))
            {
                return false;
            }

            if (inputType == EInputKeyType.OnDown)
            {
                if (!isKeyDown && NowInputDownKey == checkKeyName)
                    isKeyDown = true;
            }
            else if (inputType == EInputKeyType.OnUp)
            {
                if (!isKeyUp && NowInputUpKey == checkKeyName)
                    isKeyUp = true;
            }
            else
            {
                if (!isKeyClick && NowInputClickKey == checkKeyName)
                    isKeyClick = true;
            }

            return true;
        }

        private bool TryGetInterruptKeyCondition(IInterruptCondition interruptCondition, out EInputKeyType inputType,
            out string checkKeyName)
        {
            if (interruptCondition.InterruptType == -(int)EInterruptTypeInternal.EIT_CheckInput)
            {
                CheckCostomKey checkCostomKey = interruptCondition as CheckCostomKey;
                inputType = checkCostomKey.InputType;
                checkKeyName = checkCostomKey.CheckKeyName;
                return true;
            }

            if (interruptCondition.InterruptType == -(int)EInterruptTypeInternal.EIT_BluePrintKey)
            {
                CheckBluePrintKey checkBluePrintKey = interruptCondition as CheckBluePrintKey;
                inputType = checkBluePrintKey.InputType;
                checkKeyName = checkBluePrintKey.GetCheckKeyName(this);
                return true;
            }

            inputType = EInputKeyType.OnDown;
            checkKeyName = string.Empty;
            return false;
        }

        internal bool TryCheckInterrupCondition(ActionInterrupt _interrupt, ActionEngine_Unit unit,
            out ActionState targetAction, out int targetActionID, out int targetActionGroupID)
        {
            targetAction = null;
            targetActionID = -1;
            targetActionGroupID = -1;

            if (_interrupt is null) return false;

            targetActionID = _interrupt.ActionID;
            if (!ActionStateMachine.TryGetActionStateWithGroup(targetActionID, out targetAction, out targetActionGroupID, CurrentActionState))
            {
                return false;
            }

            SetInterruptTargetAction(targetActionID, targetActionGroupID);
            if (!CheckInterrupCondition(_interrupt.InterruptConditionList, _interrupt.CheckAllCondition, unit, _interrupt))
            {
                return false;
            }

            if (TryGetInterruptTargetActionOverride(out int overrideActionID, out _))
            {
                targetActionID = overrideActionID;
            }

            // 条件检查期间可能修改 FirstActionGroupID，最终跳转目标需要用最新上下文重新解析。
            if (!ActionStateMachine.TryGetActionStateWithGroup(targetActionID, out targetAction, out targetActionGroupID, CurrentActionState))
            {
                return false;
            }

            //bool Debug = (mCurUnit == EngineResourcesManager.Instance.Player) && (_interrupt.ActionID == 100000010);
            //if (Debug) EngineDebug.LogError($"targetAction <color=#ffcc00>[{targetAction.EventList.Count}] " +
            //    $"[{ActionStateMachine.GetFirstActionGroupID}]");
            //if (Debug) EngineDebug.LogError("<color=#ffcc00>开始尝试获取AGC--------------------------");
            //if (Debug) EngineDebug.LogError($"成功获取Action <color=#ccff00>[{targetAction.ID}]");
            return true;
            //finally
            //{
            //    mHasInterruptTargetActionGroupID = previousHasInterruptTargetActionGroupID;
            //    mInterruptTargetActionID = previousInterruptTargetActionID;
            //    mInterruptTargetActionGroupID = previousInterruptTargetActionGroupID;
            //    mHasInterruptTargetActionOverride = previousHasInterruptTargetActionOverride;
            //    mInterruptTargetOverrideActionID = previousInterruptTargetOverrideActionID;
            //    mInterruptTargetOverrideActionGroupID = previousInterruptTargetOverrideActionGroupID;
            //}
        }

        private bool CheckInterrupCondition(List<IInterruptCondition> _conditions, bool _checkAllCondition,
            ActionEngine_Unit unit, ActionInterrupt _owner)
        {
            if (_conditions == null || _conditions.Count == 0) return true;
            if (_checkAllCondition)
            {
                foreach (var VARIABLE in _conditions)
                {
                    if (VARIABLE is CheckJumpWeight _weightCondition)
                    {
                        //自上而下：到达权重条件说明其之前条件已全部通过 → 本轨已参与本帧竞争。
                        //勾选"仅占权重"的轨只消耗权重份额，永不跳转。
                        if (_weightCondition.OnlyFalse)
                            return false;
                        //未被本帧选中即失败；被选中后继续判定其后的条件（权重不是最终裁决）。
                        if (!IsSelectedWeightInterrupt(_owner))
                            return false;
                        continue;
                    }

                    if (!VARIABLE.CheckInterrupt(unit, this))
                        return false;
                }
            }
            else
            {
                foreach (var VARIABLE in _conditions)
                {
                    if (VARIABLE is CheckJumpWeight _weightCondition)
                    {
                        //OR 模式：仅当被选中且非"仅占权重"时，该条件视为满足；否则继续看其它条件。
                        if (!_weightCondition.OnlyFalse && IsSelectedWeightInterrupt(_owner))
                            return true;
                        continue;
                    }

                    if (VARIABLE.CheckInterrupt(unit, this))
                        return true;
                }
            }

            return _checkAllCondition;
        }

        /// <summary>本帧加权选择：在所有"权重条件之前条件均通过"的就绪跳转轨间按权重随机选 1 条。</summary>
        private void ComputeWeightSelection(float nextTime)
        {
            mSelectedWeightInterrupt = null;
            mWeightSelectionStepTime = ElapsedTime;
            //未装载任何权重轨时直接短路，保证不使用此条件时每帧仅一次 bool 判断
            if (!mHasWeightInterrupt) return;

            mWeightCandidates.Clear();
            mWeightCandidateValues.Clear();

            int total = 0;
            for (int i = 0; i < mCurActionInterrupt.Count; i++)
            {
                ActionInterrupt it = mCurActionInterrupt[i];
                if (!IsWeightCandidate(it, nextTime, out int weight)) continue;
                mWeightCandidates.Add(it);
                mWeightCandidateValues.Add(weight);
                total += weight;
            }

            if (total <= 0) return;

            int roll = UnityEngine.Random.Range(0, total);//[0,total)
            int acc = 0;
            for (int i = 0; i < mWeightCandidates.Count; i++)
            {
                acc += mWeightCandidateValues[i];
                if (roll < acc)
                {
                    mSelectedWeightInterrupt = mWeightCandidates[i];
                    break;
                }
            }

#if UNITY_EDITOR
            if (mSelectedWeightInterrupt is not null)
                EngineDebug.Log($"[ActionStatePart] 跳转权重命中 ActionID=[{mSelectedWeightInterrupt.ActionID}] 总权重=[{total}] 候选数=[{mWeightCandidates.Count}]");
#endif
        }

        private bool IsSelectedWeightInterrupt(ActionInterrupt it)
        {
            return mWeightSelectionStepTime == ElapsedTime && it == mSelectedWeightInterrupt;
        }

        /// <summary>判断跳转轨是否为本帧的加权候选，并输出其权重。</summary>
        private bool IsWeightCandidate(ActionInterrupt it, float nextTime, out int weight)
        {
            weight = 0;
            if (it is null) return false;
            if (!TryGetWeightCondition(it, out CheckJumpWeight weightCondition, out int weightIndex)) return false;

            //就绪判定，镜像主循环逻辑
            bool ready = it.Duration == 0
                ? IsSingleFrameInterruptReached(it, nextTime)
                : (!IsInterruptExpired(it, ElapsedTime) && IsInterruptReadyAt(it, ElapsedTime));
            if (!ready) return false;

            //目标Action不可解析的轨主判定必然失败，不允许其占用权重份额；
            //解析成功后写入目标上下文，保证入池评估与主判定看到同一个跳转目标
            //（否则依赖跳转目标的条件与蓝图节点在两次评估中取到不同结果）。
            if (!ActionStateMachine.TryGetActionStateWithGroupNoLog(it.ActionID, out _, out int targetActionGroupID))
                return false;
            SetInterruptTargetAction(it.ActionID, targetActionGroupID);

            //入池资格：仅评估权重条件之前的条件
            if (!ArePrecedingConditionsPassed(it, weightIndex)) return false;

            //权重蓝图与其它轨道蓝图一样需要真实时间上下文，否则依赖时间的节点恒取到 0
            weight = weightCondition.GetWeight(this,
                new ActionMachineTime(0f, ElapsedTime, it.GetRealTriggerTime, it.Duration));
            return weight > 0;
        }

        /// <summary>评估权重条件之前的条件（自上而下）。这些条件须为无副作用纯判定。</summary>
        private bool ArePrecedingConditionsPassed(ActionInterrupt it, int weightIndex)
        {
            List<IInterruptCondition> conditions = it.InterruptConditionList;
            if (!it.CheckAllCondition)
            {
                //OR 模式由 CheckInterrupCondition 自行处理；这里统一视为入池（权重主导）。
                return true;
            }

            for (int i = 0; i < weightIndex; i++)
            {
                if (!conditions[i].CheckInterrupt(mCurUnit, this))
                    return false;
            }
            return true;
        }

        private static bool TryGetWeightCondition(ActionInterrupt it, out CheckJumpWeight condition, out int index)
        {
            condition = null;
            index = -1;
            List<IInterruptCondition> conditions = it.InterruptConditionList;
            if (conditions is null) return false;
            for (int i = 0; i < conditions.Count; i++)
            {
                if (conditions[i] is CheckJumpWeight weightCondition)
                {
                    condition = weightCondition;
                    index = i;
                    return true;
                }
            }
            return false;
        }

        private List<ActionInterrupt> GetInterruptGroup(ActionState _action)
        {
            List<ActionInterruptGroup> _interruptGroups = _action.InterruptGroupList;

            interruptedIDs.Clear();
            _actionInterrupts.Clear();
            if (_interruptGroups is null)
            {
                return _actionInterrupts;
            }

            //当前Action的所有打断组
            foreach (ActionInterruptGroup _interruptGroup in _interruptGroups)
            {
                if (_interruptGroup is null)
                {
                    continue;
                }

                //收集组内Action及其子组
                GetInterruptGroup(_actionInterrupts, _interruptGroup, _interruptGroup.Offset, 0, _action, _action);
            }

            //string _str = "当前所有打断轨";
            //foreach (var item in _actionInterrupts)
            //{
            //    _str += $"\n[{item.ActionID}]";
            //}
            //Debug.LogWarning(_str);

            return _actionInterrupts;
        }

        private void GetInterruptGroup(List<ActionInterrupt> _actionInterrupts, ActionInterruptGroup _interruptGroup, int _triggerTime, int _depth, ActionState _curState, ActionState _sour)
        {
            _depth++;
            //控制检索深度
            if (_depth > 2) return;
            if (_depth > 1)
            {
                if (_curState.AnimaLayer != mActionStateMachine.ActionStateInfo.mGroupLayerID)
                {
                    return;
                }
                //else
                //{
                //    Debug.LogError($"成功识别组套组: layerID[{_curState.AnimaLayer}] GLayer[{mActionStateMachine.ActionStateInfo.mGroupLayerID}] name[{_sour.Name}] 偏移时间<color=#ffcc00>[{_triggerTime}]</color>");
                //}
            }

            //EngineDebug.LogError($"组的索引深度: [<color=#ffcc00>{_depth}</color>]");
            int _actionID = _interruptGroup.ActionID;

            //检查当前添加的Action是否已添加过
            if (interruptedIDs.Contains(_actionID)) return;
            interruptedIDs.Add(_actionID);

            //检查当前所有Action是否存在此ID
            if (ActionStateMachine.GetActionState(_actionID, out ActionState _actionState, _sour))
            {
                List<ActionInterrupt> _targets = _actionState.InterruptList;

                //EngineDebug.LogError($"<color=#ffcc00>跳转组: [{_actionState.Name}]  [{_actionState.ID}]</color> [{_targets.Count}]");

                //添加跳转轨道
                if (_targets is not null)
                {
                    foreach (ActionInterrupt _interrupt in _targets)
                    {
                        if (_interrupt is null || IsInterruptGroupHidden(_interruptGroup, _interrupt))
                        {
                            continue;
                        }

                        int offsetTriggerTime = _triggerTime;
                        if (_interruptGroup.TryGetActionOffset(_interrupt.ActionID, out int _offset))
                        {
                            offsetTriggerTime += _offset;
                        }

                        //if (_interrupt.ActionID == 100002010)
                        //{
                        //    EngineDebug.LogError($"添加skill_001的跳转 跳转时间[{_interrupt.OffsetTriggerTime}] 累计偏移[{_triggerTime}]  当前偏移[{_offset}]");
                        //}
                        _actionInterrupts.Add(_interrupt.CreateRuntimeCopy(offsetTriggerTime));
                        //Debug.Log($"添加轨道ID: [{_interrupt.ActionID}]");
                    }
                }

                //添加跳转组
                if (_actionState.InterruptGroupList is not null)
                {
                    foreach (ActionInterruptGroup VARIABLE in _actionState.InterruptGroupList)
                    {
                        if (VARIABLE is null)
                        {
                            continue;
                        }

                        GetInterruptGroup(_actionInterrupts, VARIABLE, _triggerTime + VARIABLE.Offset, _depth, _actionState, _sour);
                    }
                }
            }
#if UNITY_EDITOR
            else
            {
                EngineDebug.LogError($"组添加失败，Action丢失： {_interruptGroup.ActionID}");
            }
#endif

        }

        private bool IsInterruptGroupHidden(ActionInterruptGroup interruptGroup, ActionInterrupt interrupt)
        {
            return interruptGroup.ActionHide is not null && interruptGroup.ActionHide.Contains(interrupt.ActionID);
        }
    }
}