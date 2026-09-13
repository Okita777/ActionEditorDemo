using System.Collections.Generic;
//#if UNITY_EDITOR
//using UnityEditor.PackageManager;
//#endif

namespace AsiActionEngine.RunTime
{
    public partial class ActionStatePart
    {
        private bool mActionEnble = false;
        private List<ActionEvent> mTargetActionEven = new List<ActionEvent>(MotionEngineConst.MaxEventNumber); //目标事件列表
        //private List<ActionEvent> mActionEvenInit = new List<ActionEvent>(MotionEngineConst.MaxEventNumber); //已经初始化过的事件
        private List<ActionEvent> mActionEvenLoop = new List<ActionEvent>(MotionEngineConst.MaxEventNumber); //循环事件
        private HashSet<ActionEvent> mActionEvenLoopHash = new HashSet<ActionEvent>(MotionEngineConst.MaxEventNumber);
        private HashSet<ActionEvent> mActionEvenInitHash = new HashSet<ActionEvent>(MotionEngineConst.MaxEventNumber);
        private Dictionary<ActionEvent, ActionEvent> mFindInher = new Dictionary<ActionEvent, ActionEvent>(MotionEngineConst.ActionPoolMaxSize);
        private ActionMachineTime mMachineTime = new ActionMachineTime();

        private void OnUpdateEvent(List<ActionEvent> _ActionEvent, float _deltatime)
        {
            mMachineTime.Deltatime = ActionStateMachine.DeltaTime;
            mMachineTime.CurrentTime = ElapsedTime;

            for (int i = 0; i < _ActionEvent.Count; i++)
            {
                ActionEvent _actionEvent = _ActionEvent[i];
                if (ElapsedTime >= _actionEvent.TriggerTime)
                {
#if UNITY_EDITOR
                    if (_actionEvent.EventData == null)
                    {
                        EngineDebug.LogError(string.Format(
                            "Action出现空事件!! 出现问题的单位: {0}\nActionName: {1}  触发时间: {2} ",
                            mCurUnit.name,
                            CurrentActionState.Name,
                            _actionEvent.TriggerTime
                        ));
                        return;
                    }
#endif
                    //todo: 执行Bool蓝图判断
                    if (!EventCheck_Enter(_actionEvent, mMachineTime))
                    {
                        //EngineDebug.Log("清除事件");
                        //触发轨道  蓝图为非时退出轨道
                        _ActionEvent.Remove(_actionEvent);
                        i--;
                    }
                    else
                    {
                        if (_actionEvent.Duration == 0)
                        {
                            //if (_actionEvent.Check.value(this, new ActionMachineTime
                            //        (_deltatime, ElapsedTime, _actionEvent.TriggerTime, _actionEvent.Duration)))
                            //if(EventCheck_Enter(_actionEvent, mMachineTime))
                            {
                                _actionEvent.EventData.Enter(this, true);
                                CallBack_Even(_actionEvent, EActionEventTriggerType.Trigger);

                            }
                            _ActionEvent.Remove(_actionEvent);
                            i--;
                        } //单帧直接执行后踢出列表
                        else if (_actionEvent.Duration > 0)
                        {
                            if (!mActionEvenInitHash.Contains(_actionEvent))
                            {
                                //进入前先尝试事件继承：当本事件勾选了 EditorInheritable && Inheritable，
                                //且存在一个与之首尾相接、同类型且仍在运行的前序事件时，直接复用前序运行态，
                                //而不是重新 Enter。以此覆盖"后继在列表中排在前序之前"的顺序，避免继承丢失。
                                ActionEvent _pre = (_actionEvent.EditorInheritable && _actionEvent.Inheritable)
                                    ? FindInheritPredecessor(_ActionEvent, _actionEvent)
                                    : null;

                                if (_pre != null)
                                {
                                    //复用前序运行态实例 + 本事件配置，本事件被标记为已初始化(跳过 Enter)
                                    InheritHandoff(_pre, _actionEvent);

                                    //前序退役：交接而非退出，从列表移除并维护循环索引
                                    int _preIndex = _ActionEvent.IndexOf(_pre);
                                    if (_preIndex >= 0)
                                    {
                                        _ActionEvent.RemoveAt(_preIndex);
                                        if (_preIndex < i) i--;
                                    }

                                    //交接当帧驱动一次 Update，保持表现连续
                                    EventCheck_Update(_actionEvent, mMachineTime);
                                }
                                else if (EventCheck_Enter(_actionEvent, mMachineTime))
                                {
                                    //常规进入：未找到可继承前序，正常执行 Enter
                                    mActionEvenInitHash.Add(_actionEvent);
                                    _actionEvent.EventData.Enter(this, false);
                                    CallBack_Even(_actionEvent, EActionEventTriggerType.Enter);
                                }
                                else
                                {
                                    _ActionEvent.Remove(_actionEvent);
                                }//进入轨道时判断一次

                            } //如果没有初始化过  那就先初始化
                            else
                            {
                                if (ElapsedTime < _actionEvent.TriggerTime + _actionEvent.Duration)
                                {
                                    EventCheck_Update(_actionEvent, mMachineTime);
                                    //_actionEvent.EventData.Update(this, new ActionMachineTime
                                    //    (_deltatime, ElapsedTime, _actionEvent.TriggerTime, _actionEvent.Duration));
                                }
                                else
                                {
                                    //事件结束：整表查找可继承本事件运行态的后继(不再只看 i+1)
                                    //命中则把运行态交接给后继(不 Exit/不 DestoryAction)，否则正常收尾退出
                                    ActionEvent _succ = FindInheritSuccessor(_ActionEvent, _actionEvent);
                                    if (_succ != null)
                                    {
                                        InheritHandoff(_actionEvent, _succ);
                                    } //把运行态交接给同类型首尾相接的后继
                                    else
                                    {
                                        _actionEvent.EventData.Exit(this, false);
                                        CallBack_Even(_actionEvent, EActionEventTriggerType.Exit);
                                        mActionStateMachine.DestoryAction(_actionEvent);
                                        mActionEvenInitHash.Remove(_actionEvent);
                                    } //无可继承后继：正常退出并归还对象池

                                    _ActionEvent.Remove(_actionEvent);
                                    i--;
                                }
                            }
                        } //有限帧
                        else
                        {
                            if (!mActionEvenInitHash.Contains(_actionEvent))
                            {
                                if (EventCheck_Enter(_actionEvent, mMachineTime))
                                {
                                    //mActionEvenInit.Add(_actionEvent);
                                    mActionEvenInitHash.Add(_actionEvent);
                                    _actionEvent.EventData.Enter(this, false);
                                    CallBack_Even(_actionEvent, EActionEventTriggerType.Enter);
                                }
                                else
                                {
                                    _ActionEvent.Remove(_actionEvent);
                                }//进入轨道时判断一次
                                //mActionEvenInit.Add(_actionEvent);
                                //_actionEvent.EventData.Enter(this, false);
                            } //如果没有初始化过  那就先初始化
                            else
                            {
                                EventCheck_Update(_actionEvent, mMachineTime);
                                //_actionEvent.EventData.Update(this, new ActionMachineTime
                                //    (_deltatime, ElapsedTime, _actionEvent.TriggerTime, _actionEvent.Duration));
                            }
                        } //无限帧
                    } //判断事件条件是否满足
                } //当前时间处于事件触发时间之后
            }
        }

        /// <summary>
        /// 事件继承交接：把"前序事件 _from"的运行态无缝交给"后继事件 _to"。
        /// 复用 _from 的 EventData 运行对象(保留其非序列化运行时状态)，并用 _to 自己的配置
        /// 覆盖该对象，达到"看似完整事件、但外部参数已切换"的延续效果。
        /// 交接后 _to 标记为已初始化(跳过 Enter)，_from 仅退役而不执行 Exit。
        /// 注意：本方法不负责把 _from 移出运行列表，调用方需自行移除并维护循环索引。
        /// </summary>
        private void InheritHandoff(ActionEvent _from, ActionEvent _to)
        {
            //Clone 语义：a.Clone(b) 把 a 的序列化配置写入 b 并返回 b。
            //此处让 _to 的配置写入 _from 的运行对象，再赋回 _to —— 保留运行态、应用新参数。
            _to.EventData = _to.EventData.Clone(_from.EventData);
            mActionEvenInitHash.Add(_to);      //后继跳过 Enter，直接进入 Update 延续
            mActionEvenInitHash.Remove(_from); //前序退役(运行态所有权已转移，不归还对象池)
        }

        /// <summary>
        /// 结束交接：为"刚结束的前序事件 _from"在整张运行表中查找可继承它的后继。
        /// 条件：同类型、勾选 EditorInheritable && Inheritable、尚未初始化、且首尾相接
        /// (后继 TriggerTime == 前序结束帧)。整型帧比较稳定，且不再受列表相邻性约束。
        /// </summary>
        private ActionEvent FindInheritSuccessor(List<ActionEvent> _eventList, ActionEvent _from)
        {
            int _endFrame = _from.TriggerTime + _from.Duration;
            for (int i = 0; i < _eventList.Count; i++)
            {
                ActionEvent _c = _eventList[i];
                if (_c == _from) continue;
                if (!_c.EditorInheritable || !_c.Inheritable) continue; //门控：两标记齐全
                if (mActionEvenInitHash.Contains(_c)) continue;         //已进入则不再继承
                if (_c.EventData.GetEvenType() != _from.EventData.GetEvenType()) continue;
                if (_c.TriggerTime != _endFrame) continue;              //首尾相接
                return _c;
            }
            return null;
        }

        /// <summary>
        /// 进入交接：为"即将进入的后继事件 _to"查找可被其继承的前序运行态。
        /// 用于覆盖"后继在列表中排在前序之前"的顺序，避免后继先独立 Enter 导致继承丢失。
        /// 条件：同类型、已初始化(运行中/本帧刚结束)、且首尾相接(前序结束帧 == 后继 TriggerTime)。
        /// </summary>
        private ActionEvent FindInheritPredecessor(List<ActionEvent> _eventList, ActionEvent _to)
        {
            for (int i = 0; i < _eventList.Count; i++)
            {
                ActionEvent _c = _eventList[i];
                if (_c == _to) continue;
                if (!mActionEvenInitHash.Contains(_c)) continue;        //必须是运行态才能让出
                if (_c.EventData.GetEvenType() != _to.EventData.GetEvenType()) continue;
                if (_c.TriggerTime + _c.Duration != _to.TriggerTime) continue; //首尾相接
                return _c;
            }
            return null;
        }

        private void OnLateUpdateEvent(List<ActionEvent> _ActionEvent, float _deltatime)
        {
            //foreach (ActionEvent VARIABLE in _ActionEvent)
            for (int i = 0; i < _ActionEvent.Count; i++)
            {
                ActionEvent VARIABLE = _ActionEvent[i];
                //if (!VARIABLE.IsLateUpdate) continue;
                VARIABLE.EventData.LateUpdate(this, new ActionMachineTime
                    (_deltatime, ElapsedTime, VARIABLE.TriggerTime, VARIABLE.Duration));
                VARIABLE.IsLateUpdate = false;
            }
            // _ActionEvent.EventData.Update(this, _deltatime);
        }


        //private List<ActionEvent> _returnEvents = new List<ActionEvent>(MotionEngineConst.MaxEventNumber);
        //private List<ActionEvent> _EnterEvents = new List<ActionEvent>(MotionEngineConst.MaxEventNumber);

        private List<ActionEvent> OnChangeEvent(List<ActionEvent> _targetActionEvent, float _currentTime,
             float _offsetTime, bool _loop = false)
        {
            //mCurrentActionEvents 是还在跑的事件
            //_targetActionEvent 是要切换的目标事件
            //mActionEvenInit、mActionEvenInitHash 标记为已经初始化的事件，不需要再次执行进入

            //逻辑衔接 其1：当_loop为true时，对比新的目标事件，将同类型剔除并保留当前事件。
            //逻辑衔接 其2：当事件勾选事件继承时，寻找同类型事件并继承参数，并且不执行enter和exit。

            //先初始化当前时间
            mMachineTime.Deltatime = ActionStateMachine.DeltaTime;
            mMachineTime.CurrentTime = _offsetTime;
            int _offsetTimeInt = (int)_offsetTime;

            //裁剪目标事件列表，剔除无法执行的事件
            mTargetActionEven.Clear();
            mTargetActionEven.AddRange(_targetActionEvent);
            for (int i = mTargetActionEven.Count - 1; i >= 0; i--)
            {
                ActionEvent _event = mTargetActionEven[i];
                if (_event.Duration > -1)
                {
                    if ((_event.TriggerTime + _event.Duration) < _offsetTimeInt)
                    {
                        //裁切保护:被裁掉的单帧保留,稍后在第五步补触发一次
                        if (_event.Duration == 0 && _event.CutProtect) continue;
                        mTargetActionEven.RemoveAt(i);
                    }
                }
            }

            //if (_loop)
            {
                //初始化
                mActionEvenLoop.Clear();
                mActionEvenLoopHash.Clear();
                mFindInher.Clear();

                //1、剔除所有范围外的事件和单帧事件
                for (int i = mCurrentActionEvents.Count - 1; i >= 0; i--)
                {
                    if (mCurrentActionEvents[i].Duration == 0
                        || mCurrentActionEvents[i].TriggerTime > _currentTime
                    )
                    {
                        mCurrentActionEvents.RemoveAt(i);
                    }
                }

                //2、将筛选后的事件加入待循环列表
                foreach (ActionEvent _event in mCurrentActionEvents)
                {
                    mActionEvenLoop.Add(_event);
                    mActionEvenLoopHash.Add(_event);
                }

                //3、清空当前事件，以便后续事件列表更新进来
                mCurrentActionEvents.Clear();
                mActionEvenInitHash.Clear();

                //if (!IsTem && AnimaLayer == 0)
                //{
                //    string _debug = $"<color=#ffcc00>[In] 当前事件列表[{mActionEvenLoop.Count}]</color>";
                //    foreach (ActionEvent item in mActionEvenLoop)
                //    {
                //        _debug += $"\n Type[{item.EventData.GetEvenType()}]" +
                //            $"  EdiInher[{item.EditorInheritable}]" +
                //            $"  Inher[{item.Inheritable}]" +
                //            $"  ";
                //    }
                //    _debug += "\n\n目标事件集";
                //    foreach (ActionEvent item in mTargetActionEven)
                //    {
                //        bool _isRun = _offsetTimeInt >= item.TriggerTime;
                //        _debug += $"\n Type[{item.EventData.GetEvenType()}]" +
                //            $"  EdiInher[{item.EditorInheritable}]" +
                //            $"  Inher[{item.Inheritable}]" +
                //            $"  IsRun[{_isRun}]";
                //    }
                //    Debug.LogWarning(_debug + $"\nobj[{ActionStateMachine.CurUnit.gameObject.name}]");
                //}

                //string _forDebug = "筛选至字典过程";
                //4、再次筛选，执行事件收尾
                for (int i = 0; i < mActionEvenLoop.Count; i++)
                {
                    bool _isExit = true;
                    ActionEvent _nowEvent = mActionEvenLoop[i];
                    //_forDebug += $"\nType[{_nowEvent.EventData.GetEvenType()}]";
                    foreach (ActionEvent _event in mTargetActionEven)
                    {
                        bool _isRun = _offsetTimeInt >= _event.TriggerTime;
                        //_forDebug += $"\n       Type[{_event.EventData.GetEvenType()}]  isRun[{_isRun}]";

                        if (_isRun)
                        {
                            //_forDebug += $"  EdiInher[{_event.EditorInheritable}]";

                            if (!IsTem)//只有技能才能直接通过事件类型判断循环 _event.EditorInheritable || 
                            {
                                if (!mFindInher.ContainsKey(_event))
                                {
                                    //_forDebug += $"  <color=#ffcc00>content3</color>";

                                    //是否有继承
                                    //if (_event.Inheritable)
                                    {
                                        if (_event.EventData.GetEvenType() == _nowEvent.EventData.GetEvenType())
                                        {
                                            //_forDebug += $"<color=#ff0000>  Find</color>";

                                            mFindInher.Add(_event, _nowEvent);

                                            //门控对齐第⑤步真正的继承条件(EditorInheritable && Inheritable)：
                                            //仅勾 Inheritable 而非 EditorInheritable 的(历史/异常)数据不应被保留，
                                            //否则会跳过 Exit 造成残留或后续重复 Enter
                                            if (_event.EditorInheritable && _event.Inheritable)//未继承的 依旧处理下退出事件
                                                _isExit = false;
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //_forDebug += $"  <color=#ffcc00>非继承事件判断</color>";

                                //是否有循环
                                if (mActionEvenLoopHash.Contains(_event))
                                {
                                    //_forDebug += "  IsLoop";
                                    _isExit = false;
                                    break;
                                }
                            }
                        }
                    }
                    if (_isExit)
                    {
                        //_forDebug += $"\n       <color=#ff0000>Exit</color>";

                        _nowEvent.EventData.Exit(this, true);
                        CallBack_Even(_nowEvent, EActionEventTriggerType.Exit);
                        mActionStateMachine.DestoryAction(_nowEvent);
                        mActionEvenLoop.RemoveAt(i);
                        mActionEvenLoopHash.Remove(_nowEvent);
                        i--;
                    }
                }
                //if (!IsTem && AnimaLayer == 0)
                //    Debug.LogWarning(_forDebug);

                //if (!IsTem && AnimaLayer == 0)
                //{
                //    string _debug = $"<color=#ffcc00>[Check] 当前事件列表[{mFindInher.Count}]</color>";
                //    foreach (ActionEvent item in mFindInher.Keys)
                //    {
                //        _debug += $"\n Type[{item.EventData.GetType().Name}][{item.EventData.GetEvenType()}] obj[{ActionStateMachine.CurUnit.gameObject.name}]";
                //    }
                //    Debug.LogError(_debug);
                //}
                //if (!IsTem && AnimaLayer == 0)
                //{
                //    string _debug = $"<color=#ffcc00>[Check] 目标事件列表[{mTargetActionEven.Count}]</color>";
                //    foreach (ActionEvent item in mTargetActionEven)
                //    {
                //        _debug += $"\n Type[{item.EventData.GetType().Name}][{item.EventData.GetEvenType()}] obj[{ActionStateMachine.CurUnit.gameObject.name}]";
                //    }
                //    Debug.LogError(_debug);
                //}
                //5、执行事件,并添加可update事件。
                //裁切保护单帧不单独排序,严格沿用 mTargetActionEven 的原始列表顺序执行。
                //foreach (ActionEvent _event in mTargetActionEven)
                for (int i = 0; i < mTargetActionEven.Count; i++)
                {
                    ActionEvent _event = mTargetActionEven[i];
                    if (_event.Duration == 0)
                    {
                        //检测到单帧
                        //裁切保护:被裁掉(TriggerTime < 裁切点)的保护单帧也在切入瞬间补触发一次
                        if (_event.TriggerTime == _offsetTimeInt
                            || (_event.CutProtect && _event.TriggerTime < _offsetTimeInt))
                        {

                            //判断并执行
                            if (EventCheck_Enter(_event, mMachineTime))
                            {
                                _event.EventData.Enter(this, true);
                                CallBack_Even(_event, EActionEventTriggerType.Trigger);
                            }
                        }
                        else
                        {
                            mCurrentActionEvents.Add(_event);
                        }
                    }
                    else
                    {
                        bool isRunEven = _offsetTimeInt >= _event.TriggerTime;
                        if (!isRunEven)
                        {
                            if (_event.EditorInheritable)
                            {
                                ActionEvent _newEvent = mActionStateMachine.CreactAction(_event, _currentTime, CurrentActionState.Name);
                                mCurrentActionEvents.Add(_newEvent);
                            }
                            else
                            {
                                mCurrentActionEvents.Add(_event);
                            }
                        }
                        else
                        {
                            if (_event.EditorInheritable)
                            {
                                //bool _isFindInheritable = false;//是否有找到可继承事件

                                //事件属性为事件继承
                                if (_event.Inheritable && mFindInher.TryGetValue(_event, out ActionEvent _nowEvent))
                                {
                                    _nowEvent.CloneTo(_event);
                                    _event.EventData.Clone(_nowEvent.EventData);
                                    EventCheck_Update(_nowEvent, mMachineTime);
                                    //if (!IsTem && AnimaLayer == 0)
                                    //    Debug.LogWarning($"\n Type1[{_event.EventData.GetEvenType()}]实际[{_nowEvent.EventData.GetEvenType()}] obj[{ActionStateMachine.CurUnit.gameObject.name}]");
                                    mCurrentActionEvents.Add(_nowEvent);
                                    mActionEvenInitHash.Add(_nowEvent);
                                }
                                else
                                {
                                    //未找到可继承事件时新建事件,先判断条件再决定是否要执行里面的东西
                                    if (EventCheck_Enter(_event, mMachineTime))
                                    {
                                        ActionEvent _newEvent;
                                        //if (mFindInher.ContainsKey(_event))
                                        //    _newEvent = _event;
                                        //else
                                        _newEvent = mActionStateMachine.CreactAction(_event, _currentTime, CurrentActionState.Name);

                                        _newEvent.EventData.Enter(this, false);
                                        CallBack_Even(_newEvent, EActionEventTriggerType.Enter);
                                        EventCheck_Update(_newEvent, mMachineTime);
                                        //if (!IsTem && AnimaLayer == 0)
                                        //    Debug.LogWarning($"\n Type2[{_newEvent.EventData.GetEvenType()}] obj[{ActionStateMachine.CurUnit.gameObject.name}]");
                                        mCurrentActionEvents.Add(_newEvent);
                                        mActionEvenInitHash.Add(_newEvent);
                                    }

                                    //ActionEvent _newEvent = mActionStateMachine.CreactAction(_event);
                                    //if (EventCheck_Enter(_newEvent, mMachineTime))
                                    //{
                                    //    _newEvent.EventData.Enter(this, false);
                                    //    CallBack_Even(_newEvent, EActionEventTriggerType.Enter);
                                    //    EventCheck_Update(_newEvent, mMachineTime);
                                    //    mCurrentActionEvents.Add(_newEvent);
                                    //    mActionEvenInitHash.Add(_newEvent);
                                    //}
                                }
                            }
                            else
                            {
                                //检查非事件继承对象,常规判断
                                if (mActionEvenLoopHash.Contains(_event))
                                {
                                    //鉴定为循环
                                    EventCheck_Update(_event, mMachineTime);
                                    //if (!IsTem && AnimaLayer == 0)
                                    //    Debug.LogWarning($"\n Type3[{_event.EventData.GetEvenType()}] obj[{ActionStateMachine.CurUnit.gameObject.name}]");
                                    mCurrentActionEvents.Add(_event);
                                    mActionEvenInitHash.Add(_event);

                                    ////剔除已执行过的逻辑
                                    //mActionEvenLoop.Remove(_event);
                                    //mActionEvenLoopHash.Remove(_event);
                                }
                                else
                                {
                                    //非循环且非事件继承，直接执行初始化
                                    if (EventCheck_Enter(_event, mMachineTime))
                                    {
                                        _event.EventData.Enter(this, false);
                                        CallBack_Even(_event, EActionEventTriggerType.Enter);
                                        //if (!IsTem && AnimaLayer == 0)
                                        //    Debug.LogWarning($"\n Type4[{_event.EventData.GetEvenType()}] obj[{ActionStateMachine.CurUnit.gameObject.name}]");
                                        mCurrentActionEvents.Add(_event);
                                        mActionEvenInitHash.Add(_event);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return mCurrentActionEvents;
        }


        //        private List<ActionEvent> OnChangeEvent(List<ActionEvent> _targetActionEvent, float _currentTime,
        //            float _offsetTime, bool _loop = false)
        //        {
        //            //仅经历过初始化后的事件可以往下继承， 清理初始化事件列表
        //            _returnEvents.Clear();
        //            _EnterEvents.Clear();
        //            mActionEvenLoop.Clear();
        //            mActionEvenLoopHash.Clear();
        //            if (_loop) // && IsTem
        //            {
        //                //mActionEvenInit.Clear();
        //                //mActionEvenInitHash.Clear();

        //                foreach (ActionEvent _event in mCurrentActionEvents) //mCurrentActionEvents 有坑  角色实例化继承的事件后  哈希和自己会对不上
        //                {
        //                    if (_event.TriggerTime <= 0 &&
        //                        (_event.Duration >= CurrentActionState.TotalTime || _event.Duration < 0))
        //                    {
        //                        mActionEvenLoop.Add(_event);
        //                        mActionEvenLoopHash.Add(_event);
        //                    }
        //                }

        //                foreach (ActionEvent _event in _targetActionEvent) //mCurrentActionEvents 有坑  角色实例化继承的事件后  哈希和自己会对不上
        //                {
        //                    if (_event.TriggerTime <= 0 &&
        //                        (_event.Duration >= CurrentActionState.TotalTime || _event.Duration < 0))
        //                    {
        //                        //mActionEvenLoop.Add(_event);
        //                        mActionEvenLoopHash.Add(_event);
        //                    }
        //                }
        //                //for (int i = mActionEvenInit.Count -1; i >=0; i--)
        //                //{

        //                //}
        //                //if (AnimaLayer == 0 && !IsTem)
        //                //{
        //                //    string _debug = $"循环事件 [{mActionEvenLoop.Count}]";
        //                //    foreach (ActionEvent _event in mActionEvenLoop)
        //                //    {
        //                //        _debug += $"\n[{_event.EventData.GetType()}]  [{_event.GetHashCode()}]";
        //                //    }
        //                //    Debug.LogWarning(_debug);
        //                //}

        //                //if (AnimaLayer == 0 && !IsTem)
        //                //{
        //                //    string _debug = $"所有事件 [{mCurrentActionEvents.Count}]";
        //                //    foreach (ActionEvent _event in mCurrentActionEvents)
        //                //    {
        //                //        _debug += $"\n[{_event.EventData.GetType()}]  [{_event.GetHashCode()}]  IsTem[{_event.IsTem}]";
        //                //    }
        //                //    Debug.LogWarning(_debug);
        //                //}
        //            }

        //            mCurrentActionEvents.Clear();
        //            mCurrentActionEvents.AddRange(mActionEvenInit);
        //            mActionEvenInit.Clear();
        //            mActionEvenInitHash.Clear();


        //            //string _debug1 = $"执行事件 [{_targetActionEvent.Count}]";
        //            //遍历新事件列表
        //            for (int i = 0; i < _targetActionEvent.Count; i++)
        //            {
        //                ActionEvent _nextEvent = _targetActionEvent[i];
        //                if (mActionEvenLoopHash.Contains(_nextEvent)) continue; //循环事件不计入
        //                //_debug1 += $"\n[{_nextEvent.EventData.GetType()}]  [{_nextEvent.GetHashCode()}]";

        //                //if (_loop && _nextEvent.TriggerTime == 0 && _nextEvent.Duration == 0) continue;
        //                bool _isRun = _offsetTime >= _nextEvent.TriggerTime;
        //                if (_isRun)
        //                {
        //                    //todo: 执行Bool蓝图判断
        //                    if (EventCheck_Enter(_nextEvent, ActionStateMachine.DeltaTime, ElapsedTime))
        //                    {
        //                        if (_nextEvent.Duration > 0)
        //                        {
        //                            int _EvenEndTime = _nextEvent.TriggerTime + _nextEvent.Duration;
        //                            _isRun = _EvenEndTime > _offsetTime;
        //                        } //有限范围帧将可能跳过
        //                        else if (_nextEvent.Duration == 0)
        //                        {
        //                            if (_nextEvent.TriggerTime == _offsetTime)
        //                            {
        //                                _EnterEvents.Add(_nextEvent);
        //                            } //直接执行事件 不加入列表

        //                            _isRun = false;
        //                        } //单帧

        //                        if (_isRun)
        //                        {
        //                            bool _findEvent = false;
        //                            foreach (ActionEvent _currentEnve in mCurrentActionEvents)
        //                            {
        //                                if (mActionEvenLoopHash.Contains(_currentEnve)) continue; //循环事件不计入

        //                                //找到了同类型事件
        //                                if (_nextEvent.EventData.GetEvenType() == _currentEnve.EventData.GetEvenType())
        //                                {
        //                                    //复制上一个Action的事件到当前列表
        //                                    ActionEvent _event = _currentEnve;
        //                                    //if (_nextEvent.EditorInheritable)
        //                                    //{
        //                                    //    _event = mActionStateMachine.CreactAction(_nextEvent);
        //                                    //    //Debug.LogWarning($"创建的技能:[{_event.EventData.GetType()}] [{_event.EventData.GetHashCode()}] ");
        //                                    //}

        //                                    //启用了事件继承 那就复用当前事件
        //                                    if (_nextEvent.Inheritable)
        //                                    {
        //                                        if (!_event.IsTem)
        //                                        {
        //                                            _event = mActionStateMachine.CreactAction(_nextEvent);
        //#if UNITY_EDITOR
        //                                            Debug.LogWarning(
        //                                                $"警告！！ 出现一个未实例化的事件对象继承 [{CurrentActionState.Name}] [{_event.EventData.GetType()}]  D:[{_currentEnve.Duration}]");
        //#endif
        //                                        }

        //                                        //Debug.LogWarning(
        //                                        //    $"切换Action继承: {_nextEvent.EventData.GetType()}" + "\n" +
        //                                        //    $"[{_nextEvent.EventData.GetHashCode()}]当前的对象参数： " + _nextEvent.Duration + "\n" +
        //                                        //    $"[{_currentEnve.EventData.GetHashCode()}]上一个对象参数： " + _currentEnve.Duration + "\n" +
        //                                        //    ""
        //                                        //);

        //                                        _event.TriggerTime = _nextEvent.TriggerTime;
        //                                        _event.Duration = _nextEvent.Duration;
        //                                        _nextEvent.EventData.Clone(_event.EventData);

        //                                        mActionEvenInit.Add(_event);
        //                                        mActionEvenInitHash.Add(_event);
        //                                        _returnEvents.Add(_event);

        //                                        //_currentEnve.EventData
        //                                        ////_event.TriggerTime = _currentEnve.TriggerTime;
        //                                        ////_event.Duration = _currentEnve.Duration;
        //                                        //_event.EventData = _nextEvent.EventData.Clone(_currentEnve.EventData);
        //                                        //mActionEvenInit.Add(_event);
        //                                        //mActionEvenInitHash.Add(_event);
        //                                        //mCurrentActionEvents.Remove(_currentEnve);
        //                                        //Debug.LogWarning(
        //                                        //    $"切换Action继承: {_nextEvent.EventData.GetType()}" + "\n" +
        //                                        //    $"[{_nextEvent.EventData.GetHashCode()}]当前的对象参数： " + _nextEvent.Duration + "\n" +
        //                                        //    $"[{_currentEnve.EventData.GetHashCode()}]上一个对象参数： " + _currentEnve.Duration + "\n" +
        //                                        //    ""
        //                                        //);
        //                                    }
        //                                    else
        //                                    {
        //                                        //没有启用继承  执行该事件的退出函数  并销毁
        //                                        _currentEnve.EventData.Exit(this, true); //没有继承到对象  执行退出
        //                                        CallBack_Even(_currentEnve, EActionEventTriggerType.Exit);


        //                                        if (_nextEvent.EditorInheritable)
        //                                        {
        //                                            //可能产生继承, 实例化新函数去继承事件
        //                                            _event = mActionStateMachine.CreactAction(_nextEvent);
        //                                            _event.TriggerTime = _nextEvent.TriggerTime;
        //                                            _event.Duration = _nextEvent.Duration;
        //                                        }
        //                                        else
        //                                        {
        //                                            //不会产生继承，直接获取
        //                                            _event = _nextEvent;
        //                                        }

        //                                        _returnEvents.Add(_event); //添加到update
        //                                        _EnterEvents.Add(_event); //收集起来执行进入函数
        //                                        mActionEvenInit.Add(_event); //避免重复执行初始化
        //                                        mActionEvenInitHash.Add(_event); //避免重复执行初始化
        //                                        //mCurrentActionEvents.Remove(_currentEnve);//后面加的,按绝对顺序继承
        //                                        // EngineDebug.Log($"拒接继承: {_nextEvent.EventData.GetType()}");
        //                                    }

        //                                    mCurrentActionEvents.Remove(_currentEnve);

        //                                    //if (_currentEnve.EventData.GetEvenType() == 10)
        //                                    //{
        //                                    //    string _debug = "当前事件剩余列表: " + _nextEvent.EventData.GetEvenType();
        //                                    //    foreach (var a in mCurrentActionEvents)
        //                                    //    {
        //                                    //        _debug += $"\nEvent: [<color=#ffcc00>{a.EventData.GetEvenType()}</color>]";
        //                                    //    }
        //                                    //    EngineDebug.LogWarning(_debug);
        //                                    //}

        //                                    //_returnEvents.Add(_event);
        //                                    _findEvent = true;
        //                                    break;
        //                                } //找到同类型的轨道后继承变量
        //                            }

        //                            //未找到可继承事件 
        //                            if (!_findEvent)
        //                            {
        //                                ActionEvent _event = _nextEvent;

        //                                //检查到当前事件允许继承  就直接创建新的
        //                                if (_nextEvent.EditorInheritable)
        //                                {
        //                                    _event = mActionStateMachine.CreactAction(_nextEvent);
        //                                    //Debug.LogWarning($"实例化演示事件[{_event.EventData.GetType()}]: " + _event.EventData.GetHashCode());
        //                                }

        //                                _returnEvents.Add(_event); //添加到update
        //                                _EnterEvents.Add(_event); //收集起来执行进入函数
        //                                mActionEvenInit.Add(_event); //避免重复执行初始化
        //                                mActionEvenInitHash.Add(_event); //避免重复执行初始化
        //                            }
        //                        }
        //                    }
        //                } //只判断进入触发时间范围的事件
        //                else
        //                {
        //                    if (_nextEvent.EditorInheritable)
        //                    {
        //                        _returnEvents.Add(mActionStateMachine.CreactAction(_nextEvent));
        //                    }
        //                    else
        //                    {
        //                        _returnEvents.Add(_nextEvent);
        //                    }
        //                } //未进入触发范围  仅加入列表
        //            }
        //            //if (AnimaLayer == 0 && !IsTem && _loop) Debug.LogWarning(_debug1);

        //            //将剩下的旧事件列表都执行一下退出函数
        //            int tt = 0;
        //            foreach (ActionEvent _actionEvent in mCurrentActionEvents)
        //            {
        //                if (mActionEvenLoopHash.Contains(_actionEvent)) continue; //循环事件不计入

        //                _actionEvent.editorcheck = tt;
        //                _actionEvent.EventData.Exit(this, true);
        //                CallBack_Even(_actionEvent, EActionEventTriggerType.Exit);
        //                tt++;
        //            }

        //            //执行完退出函数后再执行初始函数
        //            foreach (ActionEvent _actionEvent in _EnterEvents)
        //            {
        //                bool isSingle = _actionEvent.Duration == 0;
        //                _actionEvent.EventData.Enter(this, isSingle);
        //                CallBack_Even(_actionEvent, isSingle ? EActionEventTriggerType.Trigger : EActionEventTriggerType.Enter);
        //            }

        //            foreach (ActionEvent item in _returnEvents)
        //            {
        //                item.IsLateUpdate = false;
        //            }

        //            mCurrentActionEvents.Clear();
        //            mCurrentActionEvents.AddRange(_returnEvents);
        //            mActionEvenInit.AddRange(mActionEvenLoop); //避免循环事件重复执行
        //            foreach (var VARIABLE in mActionEvenLoopHash)
        //                mActionEvenInitHash.Add(VARIABLE);
        //            mCurrentActionEvents.AddRange(mActionEvenLoop); //重新拾回循环事件
        //            return mCurrentActionEvents;
        //        }

        private bool EventCheck_Enter(ActionEvent _event, ActionMachineTime _time)
        {
            if (_event.CheckType == 0)
            {
                //ActionMachineTime _time = new ActionMachineTime(ActionStateMachine.DeltaTime,
                //    ElapsedTime, _event.TriggerTime, _event.Duration);

                _time.TriggerTime = _event.TriggerTime;
                _time.Duration = _event.Duration;
                return _event.Check.value(this, _time);
            }
            else
            {
                return true;
            }
        }

        private bool EventCheck_Update(ActionEvent _event, ActionMachineTime _time)
        {
            _time.TriggerTime = _event.TriggerTime;
            _time.Duration = _event.Duration;
            if (_event.CheckType == 0)
            {
                //ActionMachineTime _time = new ActionMachineTime(ActionStateMachine.DeltaTime,
                //    ElapsedTime, _event.TriggerTime, _event.Duration);
                _event.EventData.Update(this, _time);
                _event.IsLateUpdate = true;
                return true;
            }
            else
            {
                //ActionMachineTime _time = new ActionMachineTime(ActionStateMachine.DeltaTime,
                //    ElapsedTime, _event.TriggerTime, _event.Duration);
                bool _isRun = _event.Check.value(this, _time);
                if (_isRun)
                {
                    _event.EventData.Update(this, _time);
                    _event.IsLateUpdate = true;
                }
                return _isRun;
            }
        }

        private void CallBack_Even(ActionEvent _event, EActionEventTriggerType _type)
        {
            mActionStateMachine.EventSystem.RunEvent_ExecuteActionEvent(this, _event, _type);
        }
    }
}