using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{

    /// <summary>
    /// 单位行为逻辑核心处理
    /// </summary>
    public partial class ActionStatePart
    {
        // private float ElapsedTime;//已经经过的时间
        private float mElapsedTime_last;//切换Action时，上一个Action的时间

        private bool mIsTem = false;
        private ActionStatePart mMaster;
        private ActionStateMachine mActionStateMachine;
        //private List<ActionState> mCurrentActionStates = new List<ActionState>();//当前执行的次要事件
        private List<ActionEvent> mCurrentActionEvents = new List<ActionEvent>(MotionEngineConst.MaxEventNumber);//当前正在运行的行为事件
        private List<ActionInterrupt> mCurActionInterrupt = new List<ActionInterrupt>(MotionEngineConst.MaxInteruptNumber);//当前正在执行判断的打断轨
        private List<GameObject> mAllHitObject = new List<GameObject>();
        private bool mIsProcessingInterruptUpdate;
        private bool mHasDeferredLayerChange;
        private ActionState mDeferredLayerAction;
        private ActionState mDeferredLayerChangeSource;
        private int mDeferredLayerMixTime;
        private int mDeferredLayerOffsetTime;
        private bool mDeferredLayerIsLoop;
        private int mDeferredLayerActionGroupID;

        private ActionEngine_Unit mCurUnit => mActionStateMachine.CurUnit;//当前绑定单位

        private List<int> mJumpLayerList = new List<int>();
        public int AnimaLayer => mCurActionLayer;
        public int MixTime { get; private set; }
        public int OffsetTime { get; private set; }

        public void SetInitValue(ActionStateMachine _actionStateMachine, bool isTem = false)
        {
            mActionStateMachine = _actionStateMachine;
            mIsTem = isTem;
        }
        public ActionStatePart(ActionStateMachine _actionStateMachine, int curActionLayer)
        {
            mActionStateMachine = _actionStateMachine;
            mCurActionLayer = curActionLayer;
            ActionEnble = false;
            mIsTem = false;
        }

        public ActionStatePart(ActionStateMachine _actionStateMachine, int curActionLayer, bool isTem, ActionStatePart _master, Vector3 _spawnPos, Quaternion _spawnRot)
        {
            mActionStateMachine = _actionStateMachine;
            mCurActionLayer = curActionLayer;
            ActionEnble = false;
            mIsTem = isTem;
            mMaster = _master;

            Pos = _spawnPos;
            Rot = _spawnRot;
        }

        private bool OnChangeState(string _actionName, int _mixTime, int _offsetTime, ActionState _changeSource)
        {
            if (mActionStateMachine.GetActionIDToName(_actionName, out int _ActionStateID))
            {
                OnChangeState(_ActionStateID, _mixTime, _offsetTime, _changeSource);
            }
            else
            {
                EngineDebug.LogWarning($"Action切换失败，当前数据不存在:{_actionName}");
            }
            return false;
        }

        //返回值为True时阻断当前Action
        private bool OnChangeState(int _actionID, int _mixTime, int _offsetTime, ActionState _changeSource, bool isLoop = false)
        {
            if (ActionStateMachine.TryGetActionStateWithGroup(_actionID, out ActionState _ActionState, out int _actionGroupID, _changeSource))
            {
                if (IsTem)
                {
                    if (_mixTime == 0)
                    {
                        OnChangeState(_ActionState, _mixTime, _offsetTime, _changeSource, isLoop, _actionGroupID);
                        return true;
                    }//阻断当前Action
                    else
                    {
                        ActionStateMachine.SpawnAction(_ActionState, this, Pos, Rot, _actionGroupID);
                        return false;
                    }

                }//跳转为SkillAction

                //int _mRealyLayer = ActionStateMachine.RealyLayer(_ActionState.AnimaLayer);
                //int _mRealyLayer = ActionStateMachine.RealyLayer(_ActionState.AnimaLayerIndex);
                int _mRealyLayer = _ActionState.AnimaLayer;
                if (_changeSource is null && !IsTem)
                    _changeSource = mActionStateMachine.AllActionStatePart[_mRealyLayer].CurrentActionState;
                return OnChangeState(_ActionState, _mixTime, _offsetTime, _changeSource, isLoop, _actionGroupID);
            }
            return false;
        }

        private void OnUpdateState(float _deltatime)
        {
            TryApplyDeferredLayerChange();
            float deltaLength = _deltatime * MotionEngineConst.TimeDoubling;
            float nowElapsedTime = ElapsedTime + deltaLength;
            if (!ActionEnble || CurrentActionState is null)
            {
#if UNITY_EDITOR
                if (ActionEnble && CurrentActionState is null) EngineDebug.LogWarning("ActionStateMachine未工作,Action加载失败");
#endif
                return;
            }

            //位移
            UpdateMove(_deltatime);

            //执行事件
            // PercentTime = mElapsedTime / CurrentActionState.TotalTime;
            OnUpdateEvent(mCurrentActionEvents, _deltatime);

            // LocalPredict + ServerAuthoritative 都跑输入键 / Action 结束 / 打断轨；RemoteProxy 跳过。
            if (mActionStateMachine.Authority != SimulationAuthority.RemoteProxy)
            {
                //检查并更新按钮交互状态
                UpdateInputKey(_deltatime);

                //检查行为是否结束  如果结束后并跳转则无视后续行为
                if (ActionStateCheckEnd())
                {
                    return;
                }

                //检查行为是否跳转  如果跳转则无视后续行为
                mIsProcessingInterruptUpdate = true;
                try
                {
                    if (OnInterrupUpdate(nowElapsedTime))
                    {
                        return;
                    }
                }
                finally
                {
                    mIsProcessingInterruptUpdate = false;
                }
            }

            ElapsedTime += deltaLength;
        }

        private void OnLateUpdateState(float _deltatime)
        {
            OnLateUpdateEvent(mCurrentActionEvents, _deltatime);
        }

        private void OnUpdateAnim(float _deltaTime)
        {

        }

        private bool OnChangeState(ActionState _action, int _mixTime, int _offsetTime, ActionState _changeSource, bool isLoop = false, int _actionGroupID = -1)
        {

            float previousElapsedTime = ElapsedTime;
            CurrentActionState_ChangeSour = _changeSource;

            //开启状态机运行状态
            ActionEnble = true;

            if (!IsTem)
            {
                //切换到自身Action层级时才能往下执行  
                if (_action.AnimaLayer != mCurActionLayer)
                {
                    //int _mRealyLayer = ActionStateMachine.RealyLayer(_action.AnimaLayer);
                    int _mRealyLayer = _action.AnimaLayer;
                    mJumpLayerList.Add(_mRealyLayer);
                    ActionStatePart targetPart = mActionStateMachine.AllActionStatePart[_mRealyLayer];
                    if (mIsProcessingInterruptUpdate)
                    {
                        // 避免跨层目标 Action 的事件嵌套在源层打断判断栈内提前执行。
                        targetPart.DeferLayerChange(_action, _mixTime, _offsetTime, CurrentActionState, isLoop, _actionGroupID);
                    }
                    else
                    {
                        targetPart.ChangeState(_action, _mixTime, _offsetTime, CurrentActionState, _actionGroupID);
                    }

                    //跳转其它层级
                    return false;
                }
            }
            if (_actionGroupID < 0 &&
                ActionStateMachine.TryGetActionStateWithGroup(_action.ID, out _, out int resolvedActionGroupID, _changeSource))
            {
                _actionGroupID = resolvedActionGroupID;
            }
            mActionStateMachine.SetActiveActionGroup(_action, this, _actionGroupID);


            if (_offsetTime < 0)
            {
                if (_changeSource is not null)
                {
                    //int _mRealyLayer = ActionStateMachine.RealyLayer(_changeSource.AnimaLayer);
                    int _mRealyLayer = _changeSource.AnimaLayer;

                    //得到上一个Action百分比
                    float _elapsedTime = mActionStateMachine.AllActionStatePart[_mRealyLayer].ElapsedTime;
                    float _Percentage = _elapsedTime / _changeSource.TotalTime;

                    _offsetTime = (int)(_action.TotalTime * _Percentage);
                }
                else
                {
                    _offsetTime = 0;
                }

            }

            //是否跳过这次动画跳转
            if (_offsetTime + _mixTime >= _action.TotalTime)
            {
                IsJumpEnd = true;
            }
            else
            {
                //结束时不跳过当前结尾动画事件
                IsJumpEnd = false;
            }


            //清空层级
            mJumpLayerList.Clear();

            //执行回调事件
            mActionStateMachine.MachineExecuteChangeEven(_action.ID, _mixTime, _offsetTime);

            ////动画事件层级
            //AnimaLayer = _action.AnimaLayer;

            //动画相关参数
            MixTime = _mixTime;
            OffsetTime = _offsetTime;

            //重置按键 
            ReSetKeys();

            //切换主Action
            CurrentActionState_Last = CurrentActionState;
            CurrentActionState = _action;

            //清空并重新装载新Action的打断轨
            ClearInterrupts();
            //当前Action状态重新计时
            mElapsedTime_last = previousElapsedTime;
            ElapsedTime = OffsetTime;

            //装载阶段只收集打断轨(含立即跳转),不在此执行任何跳转;立即跳转延后到事件之后执行。
            TryLoadInterrupts(_action.InterruptList, 0);
            TryLoadInterrupts(GetInterruptGroup(_action));

            OnChangeEvent(_action.EventList, previousElapsedTime, _offsetTime, isLoop);//先执行事件

            //事件补触发完成后再执行跳转,确保事件优先于跳转(否则链式立即跳转会跳过中间Action的事件)。
            //顺序:先立即跳转(切入瞬间就绪),后裁切保护补判;均严格沿用装载顺序,不做额外排序。
            if (TryRunJumpNowInterrupts()) return true;
            if (TryRunCutProtectInterrupts()) return true;
            //mCurActionInterrupt.AddRange(_action.InterruptList);
            //mCurActionInterrupt.AddRange(GetInterruptGroup(_action));

            //string _str = "当前所有打断轨2";
            //foreach (var item in mCurActionInterrupt)
            //{
            //    _str += $"\n[{item.ActionID}]";
            //}
            //Debug.LogWarning(_str);

            //OnChangeEvent(_action.EventList, ElapsedTime, _offsetTime, isLoop);

            //单独执行动画事件轨道
            if (_action.AnimEvent != null && _action.AnimEvent.EventData != null)
            {
                _action.AnimEvent.EventData.Enter(this, false);
            }

            //成功跳转自身Action
            return true;
        }
        private void OnClearAllEvent()
        {
            mCurrentActionEvents.Clear();
            ClearInterrupts();
        }

        /// <summary>
        /// 卸载当前Action的全部跳转轨。清空前先收尾条件生命周期，保证 Enter 过的条件都能收到 Exit。
        /// part 被销毁、状态机停机或切换Action时调用。
        /// </summary>
        public void ClearInterrupts()
        {
            ExitAllInterruptLifecycle(true);//卸载视为打断退出
            mCurActionInterrupt.Clear();
            mCutProtectInterruptsInLoadOrder.Clear();//裁切保护跳转延后补判列表,随切入重建
            mJumpNowInterruptsInLoadOrder.Clear();//立即跳转延后执行列表,随切入重建
            mHasWeightInterrupt = false;//随打断轨重建，由 TryLoadInterrupt 重新置位
        }

        private void DeferLayerChange(ActionState action, int mixTime, int offsetTime, ActionState changeSource,
            bool isLoop, int actionGroupID)
        {
            mHasDeferredLayerChange = true;
            mDeferredLayerAction = action;
            mDeferredLayerMixTime = mixTime;
            mDeferredLayerOffsetTime = offsetTime;
            mDeferredLayerChangeSource = changeSource;
            mDeferredLayerIsLoop = isLoop;
            mDeferredLayerActionGroupID = actionGroupID;
        }

        private void TryApplyDeferredLayerChange()
        {
            if (!mHasDeferredLayerChange)
            {
                return;
            }

            ActionState action = mDeferredLayerAction;
            int mixTime = mDeferredLayerMixTime;
            int offsetTime = mDeferredLayerOffsetTime;
            ActionState changeSource = mDeferredLayerChangeSource;
            bool isLoop = mDeferredLayerIsLoop;
            int actionGroupID = mDeferredLayerActionGroupID;

            mHasDeferredLayerChange = false;
            mDeferredLayerAction = null;
            mDeferredLayerChangeSource = null;

            if (action is null)
            {
                return;
            }

            OnChangeState(action, mixTime, offsetTime, changeSource, isLoop, actionGroupID);
        }
    }
}
