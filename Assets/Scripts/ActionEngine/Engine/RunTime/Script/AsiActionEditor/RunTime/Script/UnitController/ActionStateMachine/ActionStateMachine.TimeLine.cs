using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public partial class ActionStateMachine
    {
        //public int ActiveActionGroupID { get => mCurGetStateInfo; set => mCurGetStateInfo = value; }

        private ActionStateInfo mActionStateInfo;
        private bool mIsSkill = false;
        private bool IsMoveInputValue = false;
        private bool mAnimValid = false;
        private bool mIsInitState = false;
        private float mSetSpeedDuration = -1;
        private float mTimeScale = 1;//行为状态机时间膨胀

        //private Dictionary<int, ActionState> mActionStates = new Dictionary<int, ActionState>();//当前角色的所有行为
        //private Dictionary<string, int> mActionStateID = new Dictionary<string, int>();//角色行为的ID
        private Dictionary<int, int> mActionStateInfoDic = new Dictionary<int, int>();
        private List<ActionStatePart> mAllActionStatePart;//所有并行执行的Action
        private List<ActionStatePart> mAllActionStatePart_Tmp = new List<ActionStatePart>(4);//所有临时并行执行的Action
        private List<Action<string>> mDwonAction = new List<Action<string>>();
        private int mCurGetStateInfo = -1;
        private int ActivActionGroupID = 0;
        private int[] mActionInfoID;
        private int mFirstActionGroupID = -1;

        public int RealyLayer(int id) { return mActionStateInfo.mLayerOrder[id]; }
        public void SetFirstActionGroupID(int actionGroupID)
        {
            mFirstActionGroupID = actionGroupID;
            //if (CurUnit == EngineResourcesManager.Instance.Player)
            //    EngineDebug.LogError($"[SkillGvalueMap] 设置AG:[{mFirstActionGroupID}]");
        }
        public int GetFirstActionGroupID => mFirstActionGroupID;

        //public int RealyLayer(int id) { return id; }
        public void Init()
        {
            if (mEquipActionInfoList.Count > 0)
            {
                //清空所有ActionList
                OnUnEquipAllActionInfo();
            }
            ClearActionGroupContext();

            ////清理既有的事件和所有ActionStatePart
            //foreach (ActionStatePart item in mAllActionStatePart_Tmp)
            //{
            //    item.ResetState();
            //}

            SetDieState(false);//状态为非死亡
            EventSystem.OnReset();//清空所有回调
            ClearGValueOnChangeEvent();//清空所有GValue的监控
            ReSetAllKey(true);//清空所有按键
            CleartComponent_Pool();//清空所有获取的组件，避免空引用


        }

        public ActionStateMachine(
            ActionEngine_Unit unit,
            Animator _animtor,
            ActionStateInfo _actionStateInfo,
            Dictionary<ushort, EngineGValue> _engineGValue,//EngineEquation
            Dictionary<ushort, EngineEquation> _engineEquation,
            GValue_Setting _setting
        )
        {
            mActionStateInfo = _actionStateInfo;
            IsCheckBluePrintCon = true;
            EventSystem ??= new ActionEventSystem(unit);

            GValueInit(_engineGValue, _engineEquation);
            //Init();
            InitGValue_Setting = _setting;
            CurUnit = unit;
            mIsSkill = CurUnit is ActionEngine_Skill;

            mAnimValid = _animtor != null;
            CurAnimator = _animtor;

#if UNITY_EDITOR
            //技能Editor预览时创建的Action
            if (!Application.isPlaying && _actionStateInfo is null)
            {
                //if (_actionStateInfo.mLayerCount < 1)
                mAllActionStatePart = new List<ActionStatePart>();
                mActionInfoID = new int[0];
                return;
            }
#endif

            //foreach (var VARIABLE in _actionStateInfo.mActionState)
            //{
            //    if (!mActionStates.TryAdd(VARIABLE.ID, VARIABLE))
            //    {
            //        EngineDebug.LogError($"Action字典初始化失败,出现同ID:{VARIABLE.ID}");
            //    }

            //    if (!mActionStateID.TryAdd(VARIABLE.Name, VARIABLE.ID))
            //    {
            //        EngineDebug.LogError($"Action字典初始化失败,出现同名:{VARIABLE.Name}");
            //    }
            //}

            if (mActionStateInfo.mLayerOrder is null || mActionStateInfo.mLayerOrder.Length != _actionStateInfo.mLayerCount)
            {
#if UNITY_EDITOR
                Debug.LogWarning("层级排序有误，已重新生成");
#endif

                mActionStateInfo.mLayerOrder = new int[_actionStateInfo.mLayerCount];
                for (int i = 0; i < _actionStateInfo.mLayerCount; i++)
                {
                    mActionStateInfo.mLayerOrder[i] = i;
                }
            }
            mActionInfoID = new int[_actionStateInfo.mLayerCount];
            ResetActionInfoID();
            mAllActionStatePart = new List<ActionStatePart>(_actionStateInfo.mLayerCount);
            for (int i = 0; i < _actionStateInfo.mLayerCount; i++)
            {
                mAllActionStatePart.Add(new ActionStatePart(this, i));
            }

            if (_actionStateInfo.mLayerCount > 0) FirstStatePart = mAllActionStatePart[0];
            unit.SetActionStateMachine(this);
            //InitGValue();

            Init_EventPool();
            //Init();
#if UNITY_EDITOR
            //非运行状态下不在初始化时切换Action
            if (!Application.isPlaying) return;
#endif
        }

        private void OnReSetAllKey(bool _resetHoldKey)
        {
            if (mAllActionStatePart is null) return;
            foreach (ActionStatePart _part in mAllActionStatePart)
            {
                _part.ReSetKeys(_resetHoldKey);
            }
        }

        private void ResetActionInfoID()
        {
            if (mActionInfoID is null) return;
            for (int i = 0; i < mActionInfoID.Length; i++)
            {
                mActionInfoID[i] = -1;
            }
        }

        private void ClearActionGroupContext()
        {
            mCurGetStateInfo = -1;
            //mFirstActionGroupID = -1;
            mActionStateInfoDic.Clear();
            ResetActionInfoID();

            if (mAllActionStatePart is not null)
            {
                foreach (ActionStatePart part in mAllActionStatePart)
                {
                    part.ClearActionGroupContext();
                }
            }

            if (mAllActionStatePart_Tmp is not null)
            {
                foreach (ActionStatePart part in mAllActionStatePart_Tmp)
                {
                    part.ClearActionGroupContext();
                }
            }
        }

        /// <summary>
        /// 初始化GValue
        /// </summary>
        /// <param name="_setInitState">是否标记为 [已初始化] </param>
        public void InitGValue(bool _setInitState = false)
        {
            GValuePool.ResetAllValue();
            InitGValue_Setting.OnSet(this);
            mIsInitState = _setInitState;
        }

        public void InitState(List<int> _initAction)
        {
            mIsInitState = true;
            //EngineDebug.Log("初始化ActionStatePart逻辑");
            foreach (int _startAction in _initAction)
            {
                ChangeAction(_startAction, 0, 0);
            }
        }
        public void InitState(string _initAction)
        {
            mIsInitState = true;
            //EngineDebug.Log("初始化ActionStatePart逻辑");
            ChangeAction(_initAction, 0, 0);
        }

        public void InitState(List<int> _initAction, ActionStatePart _master, Vector3 _pos, Quaternion _rot)
        {
            mIsInitState = true;
            foreach (int _startAction in _initAction)
            {
                OnSpawnAction(_startAction, _master, _pos, _rot);
            }
        }
        public void InitState(string _initAction, ActionStatePart _master, Vector3 _pos, Quaternion _rot)
        {
            mIsInitState = true;
            OnSpawnAction(_initAction, _master, _pos, _rot);
        }
        public ActionStatePart InitState(int _initAction, ActionStatePart _master, Vector3 _pos, Quaternion _rot)
        {
            mIsInitState = true;
            return OnSpawnAction(_initAction, _master, _pos, _rot);
        }
        public void InitState()
        {
            if (mIsInitState) return;
            mIsInitState = true;

            //Debug.Log("初始化状态机");
            //播放默认动画
            if (mActionStateInfo.mStartActionNames.Count > 0)
            {
                foreach (int _startAction in mActionStateInfo.mStartActionNames)
                {
                    ChangeAction(_startAction, 0, 0);
                }
            }
            else
            {
                //Debug.LogError("寻找默认动画: " + mActionStateInfo.mStartActionNames);

                bool _isFind = false;
                foreach (ActionState action in mActionStateInfo.mActionState)
                {
                    if (action.AnimaLayer == 0)
                    {
                        _isFind = true;
                        ChangeAction(action.Name, 0, 0);
                        break;
                    }
                }

                if (!_isFind)
                {
                    ChangeAction(mActionStateInfo.mActionState[0].Name, 0, 0);
                }
            }
        }
        /// <summary>
        /// 注册按下行为的回调
        /// </summary>
        /// <param name="_action"></param>
        public void AddDwonAction(Action<string> _action)
        {
            mDwonAction.Add(_action);
        }

        private void OnUpdateState(float _deltatime)
        {
            SetSpeedUpdate(_deltatime);
            _deltatime *= mTimeScale;
            DeltaTime = _deltatime;

            //重新排序
            foreach (int index in mActionStateInfo.mLayerOrder)
            {
                mAllActionStatePart[index].OnUpdate(_deltatime);
            }
            //foreach (ActionStatePart VARIABLE in mAllActionStatePart)
            //{
            //    VARIABLE.OnUpdate(_deltatime);
            //}

            //这个是临时申请的行为状态列表
            for (int i = mAllActionStatePart_Tmp.Count - 1; i >= 0; i--)
            {
                if (mAllActionStatePart_Tmp.Count > i)
                    mAllActionStatePart_Tmp[i].OnUpdate(_deltatime);
            }

            Update_Extend(_deltatime);
        }

        private void OnLateUpdateState(float _deltatime)
        {
            //重新排序
            foreach (int index in mActionStateInfo.mLayerOrder)
            {
                mAllActionStatePart[index].OnLateUpdate(_deltatime);
            }
            //foreach (ActionStatePart VARIABLE in mAllActionStatePart)
            //{
            //    VARIABLE.OnLateUpdate(_deltatime);
            //}

            //这个是临时申请的行为状态列表
            for (int i = mAllActionStatePart_Tmp.Count - 1; i >= 0; i--)
            {
                mAllActionStatePart_Tmp[i].OnLateUpdate(_deltatime);
            }

            LateUpdate_Extend(_deltatime);
        }

        //设置输入
        private void OnSetMoveInput(Vector3 _move, Vector3 _transToCam)
        {
            PlayerInputMoveDir = _move;
            PlayerInputMoveDir_Cam = _transToCam;
            IsMoveInput = true;
            //IsMoveInputPre = true;
        }
        private void OnSetHeadRot(Quaternion _rot)
        {
            mMouseXY = _rot;
        }
        private void OnSetMoveInputStop()
        {
            IsMoveInput = false;
        }

        private void OnSetSpeed(float _speed, float _duration)
        {
            if (_duration > 0)
            {
                mSetSpeedDuration = _duration;
                mTimeScale = _speed;
                SetAnimatorSpeed(_speed);

            }
        }

        private void SetSpeedUpdate(float _deltatime)
        {
            if (mSetSpeedDuration > 0)
            {
                mSetSpeedDuration -= _deltatime;
                if (mSetSpeedDuration <= 0)
                {
                    mTimeScale = 1;
                    SetAnimatorSpeed(1);
                }
            }
        }

        // 注册按键按下
        private void OnSetKeyDown(string _keyName, int _actionGroupID)
        {
            InputKeyObserved?.Invoke(EActionInputKind.KeyDown, _keyName, _actionGroupID);
            ActivActionGroupID = _actionGroupID;
            foreach (var VARIABLE in mAllActionStatePart)
            {
                if (VARIABLE.ActionEnble)
                {
                    VARIABLE.SetKeyDown(_keyName);
                }
            }
        }
        // 注册按键抬起
        private void OnSetKeyUp(string _keyName)
        {
            InputKeyObserved?.Invoke(EActionInputKind.KeyUp, _keyName, 0);
            foreach (var _action in mDwonAction)
            {
                _action(_keyName);
            }
            foreach (var VARIABLE in mAllActionStatePart)
            {
                if (VARIABLE.ActionEnble)
                {
                    VARIABLE.SetKeyUp(_keyName);
                }
            }
        }

        private void OnSendKeyDown(string _keyName, int _inputType)
        {
            InputKeyObserved?.Invoke(EActionInputKind.SendKey, _keyName, _inputType);
            //按下
            if (_inputType == 0)
            {
                foreach (var VARIABLE in mAllActionStatePart)
                {
                    if (VARIABLE.ActionEnble)
                    {
                        VARIABLE.NowInputDownKey = _keyName;
                    }
                }
            }
            //抬起
            else if (_inputType == 1)
            {
                foreach (var VARIABLE in mAllActionStatePart)
                {
                    if (VARIABLE.ActionEnble)
                    {
                        VARIABLE.NowInputUpKey = _keyName;
                        // VARIABLE.SetKeyUp(_keyName);
                        //重置长按按钮
                        // if (_keyName == NowInputHoldKey)
                        // {
                        //     NowInputHoldKey = MotionEngineConst.NondKeyName;
                        // }
                    }
                }
            }
            //点击
            else if (_inputType == 2)
            {
                foreach (var VARIABLE in mAllActionStatePart)
                {
                    if (VARIABLE.ActionEnble)
                    {
                        VARIABLE.NowInputClickKey = _keyName;
                    }
                }
            }
            //长按
            else if (_inputType == 3)
            {
                foreach (var VARIABLE in mAllActionStatePart)
                {
                    if (VARIABLE.ActionEnble)
                    {
                        VARIABLE.NowInputHoldKey = _keyName;
                    }
                }
            }
        }

        private ActionState OnGetActionState(string _name, ActionState _sour = null)
        {
            if (OnGetActionState(_name, out ActionState _action, out _, _sour)) return _action;
            return null;
        }

        private bool OnGetActionIDToName(string _actionName, out int _ActionStateID)
        {
            //从武器中寻找Action优先替换
            foreach (ActionStateInfo _action in mEquipActionInfoList)
            {
                if (_action.TryGetAction(_actionName, out ActionState _state))
                {
                    _ActionStateID = _state.ID;
                    return true;
                }
            }

            //基础Action
            if (mActionStateInfo.TryGetAction(_actionName, out ActionState _state2))
            {
                _ActionStateID = _state2.ID;
                return true;
            }

            _ActionStateID = 0;
            EngineDebug.LogError($"不存在这个Action: {_actionName}");
            return false;
        }

        //所有获取ActionState的入口
        private bool OnGetActionState(string _actionName, out ActionState _action, ActionState _sour = null)
            => OnGetActionState(_actionName, out _action, out _, _sour);

        private bool OnGetActionState(string _actionName, out ActionState _action, out int _actionGroupID, ActionState _sour = null)
        {
            if (OnGetActionIDToName(_actionName, out int _ActionStateID))
                return OnGetActionState(_ActionStateID, out _action, out _actionGroupID, _sour);

            EngineDebug.LogError($"[<color=#ffcc00>{(_sour is null ? "外部跳转" : _sour.Name)}</color>]不存在这个Action: {_actionName}  [{CurUnit.gameObject}]可以尝试重新保存\n原因：可能在保存Action时，丢失跳转对象，被赋予默认ID 0");
            _action = null;
            _actionGroupID = -1;
            return false;
        }

        private bool OnGetActionState(int _actionID, out ActionState _action, ActionState _sour = null)
            => OnGetActionState(_actionID, out _action, out _, _sour);

        internal bool TryGetActionStateWithGroupNoLog(int _actionID, out ActionState _action, out int _actionGroupID)
        {
            _actionGroupID = -1;

            //优先从目标ActionGroup中查询Action.
            if (mFirstActionGroupID > -1)
            {
                //找到对应的ActionGroup
                foreach (ActionStateInfo _state in mEquipActionInfoList)
                {
                    if (_state.ActionGroupID == mFirstActionGroupID)
                    {
                        if (_state.TryGetAction(_actionID, out _action))
                        {
                            _actionGroupID = _state.ActionGroupID;
                            //mCurGetStateInfo = _actionGroupID;
                            return true;
                        }
                        break;
                    }
                }
            }

            foreach (ActionStateInfo _state in mEquipActionInfoList)
            {
                if (_state.TryGetAction(_actionID, out _action))
                {
                    _actionGroupID = _state.ActionGroupID;
                    return true;
                }
            }

            if (mActionStateInfo.TryGetAction(_actionID, out _action))
            {
                _actionGroupID = mActionStateInfo.ActionGroupID;
                return true;
            }

            _action = null;
            return false;
        }

        //public string testDebug = "";
        private bool OnGetActionState(int _actionID, out ActionState _action, out int _actionGroupID, ActionState _sour = null)
        {
            _actionGroupID = -1;
            //if(_actionID == -10)
            //{
            //    EngineDebug.LogError($"<color=#ff0000>Action丢失!!</color> [<color=#ffcc00>{(_sour is null ? "外部跳转" : _sour.Name)}</color>]" +
            //        $"不存在这个Action: {_actionID}  [{CurUnit.gameObject}]可以尝试重新保存\n原因：可能在保存Action时，丢失跳转对象");
            //    _action = null;
            //    return false;
            //} 14
            //testDebug = "";
            //优先从目标ActionGroup中查询Action.
            if (mFirstActionGroupID > -1)
            {
                //找到对应的ActionGroup
                //string str = $"当前持有的AG: [{mEquipActionInfoList.Count}]";
                foreach (ActionStateInfo _state in mEquipActionInfoList)
                {
                    //str += $"\nAG:{_state.ActionGroupID}";
                    if (_state.ActionGroupID == mFirstActionGroupID)
                    {
                        if (_state.TryGetAction(_actionID, out _action))
                        {
                            //if (_actionID == 100000010)
                            //{
                            //    if (CurUnit == EngineResourcesManager.Instance.Player)
                            //        EngineDebug.LogError($"<color=#ffcc00>目标AG[{mFirstActionGroupID}]成功跳转[{_actionID}]");
                            //}
                            //if (mFirstActionGroupID == 100000000)
                            //{
                            //    EngineDebug.LogError($"<color=#ccff00>已找到[{mFirstActionGroupID}]的AG");
                            //}
                            //testDebug += $"在 <color=#ccff00>[{mFirstActionGroupID}]</color> 中找到";
                            _actionGroupID = _state.ActionGroupID;
                            mCurGetStateInfo = _actionGroupID;
                            return true;
                        }
                        //else if(_actionID == 100000010)
                        //{
                        //    if (CurUnit == EngineResourcesManager.Instance.Player)
                        //        EngineDebug.LogError($"<color=#ccff00>目标AG[{mFirstActionGroupID}]不存在ActionID[{_actionID}]");
                        //}
                        break;
                    }
                }
                //testDebug += $"找不到 <color=#ccff00>[{mFirstActionGroupID}]</color>";

                //if (_actionID == 100000010)
                //    EngineDebug.LogError(str);
                //if(mFirstActionGroupID == 100000000)
                //{
                //    EngineDebug.LogError($"<color=#ccff00>未找到[{mFirstActionGroupID}]的AG");
                //}
            }

            //从武器中寻找Action优先替换
            foreach (ActionStateInfo _state in mEquipActionInfoList)
            {
                if (_state.TryGetAction(_actionID, out _action))
                {
                    _actionGroupID = _state.ActionGroupID;
                    mCurGetStateInfo = _actionGroupID;
                    //testDebug += $"在 <color=#ccff00>常规顺序</color> 中找到  [{mFirstActionGroupID}]";

                    return true;
                }
            }

            //基础Action
            if (mActionStateInfo.TryGetAction(_actionID, out _action))
            {
                // EngineDebug.Log($"使用角色的Action：[{_action.Name}]");
                _actionGroupID = mActionStateInfo.ActionGroupID;
                mCurGetStateInfo = _actionGroupID;
                //testDebug += $"在 <color=#ccff00>角色身上</color> 中找到";

                return true;
            }

            EngineDebug.LogError($"[<color=#ffcc00>{(_sour is null ? "外部跳转" : _sour.Name)}</color>]不存在这个Action: {_actionID}  [{CurUnit.gameObject}]可以尝试重新保存\n原因：可能在保存Action时，丢失跳转对象，被赋予默认ID 0");
            return false;
        }

        private int OnGetActionInfoIDToLayer(ActionStatePart _part)
        {
            if (_part is null || mActionInfoID is null) return -1;
            if (_part.TryGetActiveActionGroupID(out int actionGroupID)) return actionGroupID;
            if (_part.AnimaLayer < 0 || _part.AnimaLayer >= mActionInfoID.Length) return -1;
            return mActionInfoID[_part.AnimaLayer];
        }

        private void OnChangeAction(string _name, int _mixTime, int _offsetTime)
        {

            if (!OnGetActionState(_name, out ActionState _actionState, out int _actionGroupID)) return;

            //SetActionStateInfo(_actionState);

            int _mRealyLayer = _actionState.AnimaLayer;
            ActionState _sour = mAllActionStatePart[_mRealyLayer].CurrentActionState;
            mAllActionStatePart[_mRealyLayer].ChangeState(_actionState, _mixTime, _offsetTime, _sour, _actionGroupID);
        }
        private ActionState OnChangeAction(int _id, int _mixTime, int _offsetTime)
        {

            if (OnGetActionState(_id, out ActionState _actionState, out int _actionGroupID))
            {
                if (_actionState is null) return null;

                //SetActionStateInfo(_actionState);

                //if(CurUnit is _Entity.TryGetSlotIDToActionListID(_info.ActionGroupID, out int _slotID))
                //{

                //}
                //if (CurUnit == EngineResourcesManager.Instance.Player)
                //{
                //    EngineDebug.LogError($"Player当前切换 [{_actionState.AnimaLayer}] 到 [{mCurGetStateInfo.ActionGroupID}]");
                //    //EngineDebug.LogError($"当前切换 [{mCurGetStateInfo.ActionGroupID}] 到 [{mCurGetStateInfo.ActionGroupID}]");
                //} 


                int _mRealyLayer = _actionState.AnimaLayer;
                ActionState _sour = mAllActionStatePart[_mRealyLayer].CurrentActionState;
                mAllActionStatePart[_mRealyLayer].ChangeState(_actionState, _mixTime, _offsetTime, _sour, _actionGroupID);
            }
            return _actionState;
        }

        public void SetActiveActionGroup(ActionState _actionState, ActionStatePart _part, int _actionGroupID)
        {
            if (_actionState is null || _part is null) return;
            mCurGetStateInfo = _actionGroupID;
            _part.SetActiveActionGroupID(_actionGroupID);
            if (_actionState.AnimaLayer >= 0 && mActionInfoID is not null && _actionState.AnimaLayer < mActionInfoID.Length)
            {
                mActionInfoID[_actionState.AnimaLayer] = _actionGroupID;
            }

            if (!mActionStateInfoDic.TryAdd(_actionState.AnimaLayer, _actionGroupID))
                mActionStateInfoDic[_actionState.AnimaLayer] = _actionGroupID;
            //if (CurUnit == EngineResourcesManager.Instance.Player)
            //    EngineDebug.LogError($"当前切换 [{_actionState.AnimaLayer}] 到 [{_actionGroupID}]" +
            //        $"\n[{EngineDebug.DebugActionStatePart(_part)}]");
        }

        //为Action生成ActionStatePart 独立执行
        private void OnSpawnAction(ActionState _actionState, ActionStatePart _master, Vector3 _pos, Quaternion _rot, int _actionGroupID = -1)
        {
            if (_actionState is null) return;
            ActionStatePart _part = new ActionStatePart(this, 0, true, _master, _pos, _rot);
            FirstStatePart = _part;

            _part.ChangeState(_actionState, 0, 0, null, _actionGroupID);
            mAllActionStatePart_Tmp.Add(_part);
        }
        private void OnSpawnAction(string _name, ActionStatePart _master, Vector3 _pos, Quaternion _rot)
        {
            if (!OnGetActionState(_name, out ActionState _actionState, out int _actionGroupID)) return;
            ActionStatePart _part = new ActionStatePart(this, 0, true, _master, _pos, _rot);
            FirstStatePart = _part;

            _part.ChangeState(_actionState, 0, 0, null, _actionGroupID);
            mAllActionStatePart_Tmp.Add(_part);
        }
        private ActionStatePart OnSpawnAction(int _id, ActionStatePart _master, Vector3 _pos, Quaternion _rot)
        {
            if (OnGetActionState(_id, out ActionState _actionState, out int _actionGroupID))
            {
                if (_actionState is null) return null;
                ActionStatePart _part = new ActionStatePart(this, 0, true, _master, _pos, _rot);
                FirstStatePart = _part;

                _part.ChangeState(_actionState, 0, 0, null, _actionGroupID);
                mAllActionStatePart_Tmp.Add(_part);

                //EngineDebug.LogWarning($"生成 State:[<color=#ffcc00>{_part.GetHashCode()}</color>]  Event:[{_actionState.GetHashCode()}]");

                return _part;
            }
            return null;
        }

        private void OnStopActionState(ActionStatePart _part)
        {
            if (_part.IsTem)
            {
                //是临时创建的ActionState  需要特别处理
                DestoryPart(_part);
                if (mAllActionStatePart_Tmp.Count < 1)
                {
                    if (mAllActionStatePart.Count < 1)
                    {
                        //销毁当前单位
                        EventSystem.RunEvent_OnDead();
                        //EngineDebug.Log("在这里尝试销毁Unit了啊");
                        EngineResourcesManager.Instance.DestoryUnit(CurUnit);
                    }
                }
            }
            else
            {
                _part.SetActionEnble(false);
            }
        }

        public void DestoryPart(ActionStatePart _part)
        {
            //退出一下事件
            foreach (ActionEvent _event in _part.CurrentActionEvents)
            {
                _event.EventData.Exit(_part, true);
            }
            _part.CurrentActionEvents.Clear();
            _part.ClearInterrupts();//含跳转条件的生命周期收尾
            _part.ClearActionGroupContext();
            mAllActionStatePart_Tmp.Remove(_part);
        }
        // private void OnSetToClient(bool _isLocal)
        // {//设置为客户端  屏蔽部分逻辑的执行
        //     foreach (var VARIABLE in mAllActionStatePart)
        //     {
        //         VARIABLE.IsLocalClient = _isLocal;
        //     }
        // }
        public void MachineExecuteChangeEven(int _id, int _mixTime, int _offsetTime)
        {
            EventSystem.RunEvent_ChangeAction(_id, _mixTime, _offsetTime);
        }
    }
}