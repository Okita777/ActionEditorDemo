namespace AsiActionEngine.RunTime
{
    public partial class ActionStatePart
    {
        public bool IsJumpEnd = false;//仅在这次跳过结尾动画跳转逻辑
        // private int mFinishLayer = 0;

        //返回值决定是否阻断当前动画
        private bool ActionStateCheckEnd()
        {
            if (ElapsedTime > CurrentActionState.TotalTime)
            {
                if (IsTem)
                {
                    if (CurrentActionState.DefaultActionID > -1)
                    {
                        bool loop = CurrentActionState.DefaultActionID == CurrentActionState.ID;
                        //Debug.Log($"Skill跳转 [{loop}] [{CurrentActionState.ID}]");
                        // Debug.Log($"尝试跳转至: {CurrentActionState.DefaultAction}");
                        OnChangeState(CurrentActionState.DefaultActionID, CurrentActionState.MixTime,
                             CurrentActionState.OffsetTime, CurrentActionState, loop);
                        //return true;
                    }
                    else //if(CurrentActionState.DefaultActionID == -1)
                    {
                        //Debug.Log($"Skill结束[{CurrentActionState.DefaultActionID}] [{CurrentActionState.ID}]");
                        ActionStateMachine.StopActionState(this);
                        //return true;
                    }
                    //else
                    //{
                    //    EngineDebug.LogError($"Action在编辑器保存时丢失: {CurrentActionState.DefaultActionID}  [<color=#ffcc00>{mCurUnit.gameObject.name}</color>]");
                    //    //return false;
                    //    ActionStateMachine.StopActionState(this);
                    //}
                    return true;
                }

                //if (!string.IsNullOrEmpty(CurrentActionState.DefaultAction))
                if (CurrentActionState.DefaultActionID > -1)
                {
                    //Debug.Log($"结束后尝试尝试跳转至: {CurrentActionState.DefaultAction}");
                    return OnChangeState(CurrentActionState.DefaultActionID, CurrentActionState.MixTime,
                        CurrentActionState.OffsetTime, CurrentActionState);
                }
                else //if(CurrentActionState.DefaultActionID == -1)
                {
                    //刷新事件
                    OnChangeEvent(CurrentActionState.EventList, ElapsedTime, 0, true);
                    //OnChangeEvent(CurrentActionState.EventList,ElapsedTime,0);
                    //执行单帧跳转逻辑
                    foreach (var VARIABLE in mCurActionInterrupt)
                    {
                        //单帧打断轨
                        if (VARIABLE.Duration == 0 && VARIABLE.GetRealTriggerTime == 0)
                        {
                            //检查条件  满足后直接跳转
                            if (TryRunInterrupt(VARIABLE, CurrentActionState, out _))
                            {
                                return true;
                            }
                        }
                    }

                    ElapsedTime -= CurrentActionState.TotalTime;
                    MixTime = 0;
                    OffsetTime = 0;

                    if (!IsJumpEnd)
                    {
                        if (CurrentActionState.AnimEvent is not null && CurrentActionState.AnimEvent.EventData is not null)
                            CurrentActionState.AnimEvent.EventData.Enter(this, false); //执行动画轨
                    }
                    else
                    {
                        IsJumpEnd = false;
                    }
                }
                //else
                //{
                //    EngineDebug.LogError($"Action在编辑器保存时丢失: {CurrentActionState.DefaultActionID}  [<color=#ffcc00>{mCurUnit.gameObject.name}</color>]");
                //}
            }

            return false;
        }
    }
}