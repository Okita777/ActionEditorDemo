
using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public partial class ActionStateMachine
    {
        private int hitActionID => mActionStateInfo.mHitActionID;
        private List<ActionInterrupt> hitActionInterrup = new List<ActionInterrupt>();
        private ActionEngine_Unit onAttacker = null;

        private bool hasAttacker = false;
        private bool hasHiter = false;
        private bool hasHitobj = false;
        private bool isDie = false;

        /// <summary>
        /// 被击中时调用的函数
        /// </summary>
        /// <param name="_attackInfo">攻击信息</param>
        /// <param name="_attacker">攻击者</param>
        /// <param name="_hitPoint">攻击点位</param>
        private void OnBeHit(IAttackInfo _attackInfo, ActionEngine_Unit _attacker, GValue_Setting _gValueRatio)
        {
            bool changeAction = false;//检查是否有跳转Action

            onAttacker = _attacker;
            hasAttacker = true;
            _gValueRatio.OnSet(this, _attacker.ActionStateMachine);
            List<ActionStatePart> _partList = mAllActionStatePart;

            if (mIsSkill)
            {
                _partList = mAllActionStatePart_Tmp;
                //EngineDebug.LogError($"命中[<color=#ffcc00>{CurUnit.gameObject.name}<color>]");
            }

            bool _isInterrup = false;
            //受击轨道调用
            foreach (var _actionStatePart in _partList)
            {
                if (_actionStatePart.ActionEnble)
                {
                    //EngineDebug.LogWarning($"尝试寻找打断轨道 {_actionStatePart.CurrentActionState.InterruptList_BeHit.Count}");
                    foreach (var _actionInterrupt in _actionStatePart.CurrentActionState.InterruptList_BeHit)
                    {
                        if (CheckValueTrack(_actionInterrupt, (int)_actionStatePart.ElapsedTime))
                        {
                            if (_actionStatePart.TryCheckInterrupCondition(_actionInterrupt, CurUnit,
                                    out _, out int beHitTargetActionID, out _))
                            {
                                //EngineDebug.Log($"成功从打断轨跳受击 {(int)_actionStatePart.ElapsedTime}");
                                _actionStatePart.ChangeState(beHitTargetActionID,
                                    _actionInterrupt.CrossFadeTime,
                                    _actionInterrupt.OffsetTime);
                                _isInterrup = true;
                                break;
                            } //检查条件是否满足
                        }
                    }//所有受击轨道
                }
            }//所有层级

            //扇出到 m_SkillDic 子树: 让 skill 上配置了"父级/源"目标的受击轨能响应本次被击
            FanoutBeHit_ToSkills(CurUnit);

            if (mIsSkill)
            {
                //Debug.LogError($"技能[{CurUnit.gameObject.name}]被[{_attacker.gameObject.name}]命中了");
                EventSystem.RunEvent_OnHit();
                _attackInfo.BeHit(_attackInfo, this, _attacker);
                return;
            }

            //如果没有受击轨道，就走受击Action
            if (!_isInterrup)
            {
                if (mActionStateInfo.TryGetAction(hitActionID, out ActionState _hitActionState))
                {
                    //受击Action
                    //ActionState _hitActionState = mActionStates[_id];

                    //执行受击Action下的事件
                    foreach (ActionEvent _event in _hitActionState.EventList)
                    {
                        if (_event.Duration == 0)
                        {
                            if (_event.Check.value(FirstStatePart, EngineResourcesManager.Instance.MachineTime))
                            {
                                _event.EventData.Enter(FirstStatePart, true);
                            }
                        }
                    }

                    hitActionInterrup.Clear();
                    hitActionInterrup.AddRange(_hitActionState.InterruptList);
                    hitActionInterrup.AddRange(_hitActionState.InterruptList_BeHit);
                    ActionStatePart _curActionStatePart = mAllActionStatePart[_hitActionState.AnimaLayer];
                    //尝试跳转Action
                    foreach (var _actionInterrupt in hitActionInterrup)
                    {
                        if (_curActionStatePart.TryCheckInterrupCondition(_actionInterrupt, CurUnit,
                                out _, out int hitFallbackTargetActionID, out _))
                        {
                            ActionState changedAction = ChangeAction(hitFallbackTargetActionID,
                                _actionInterrupt.CrossFadeTime,
                                _actionInterrupt.OffsetTime);
                            if (changedAction is not null && changedAction.AnimaLayer == _curActionStatePart.AnimaLayer) break;
                        }
                    }

                    //if (_hitActionState.AnimaLayer == 0)
                    //{//跳转层级为0时读取跳转轨道
                    //    hitActionInterrup.AddRange(_hitActionState.InterruptList);
                    //    hitActionInterrup.AddRange(_hitActionState.InterruptList_BeHit);
                    //    ActionStatePart _curActionStatePart = mAllActionStatePart[_hitActionState.AnimaLayer];

                    //    if (hitActionInterrup.Count < 1)
                    //    {
                    //        _curActionStatePart.ChangeState(hitActionName);
                    //    }
                    //    else
                    //    {
                    //        foreach (var _actionInterrupt in hitActionInterrup)
                    //        {
                    //            if (CheckInterrupCondition(_actionInterrupt.InterruptConditionList,
                    //                    _actionInterrupt.CheckAllCondition, _curActionStatePart))
                    //            {
                    //                _curActionStatePart.ChangeState(_actionInterrupt.ActionID,
                    //                    _actionInterrupt.CrossFadeTime,
                    //                    _actionInterrupt.OffsetTime);
                    //                break;
                    //            }
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    ChangeAction(_id,0,0);
                    //}
                }
                else
                {
                    //if (string.IsNullOrEmpty(hitActionID))
                    //{
                    //    EngineDebug.LogWarning($"没有配置受击Action  mode:{CurUnit.name}");
                    //}
                    //else
                    {
                        EngineDebug.LogWarning($"受击Action配置出错 [{hitActionID}]  mode:{CurUnit.name}");
                    }
                }
            }//没有检测到受击打断轨时才执行
            EventSystem.RunEvent_OnHit();
            _attackInfo.BeHit(_attackInfo, this, _attacker);
        }

        /// <summary>
        /// 命中对象时调用的函数
        /// </summary>
        /// <param name="_BeHit">命中的对象</param>
        /// <param name="_BeHitUnit">命中的单位（当命中对象不为单位时，此值为空）</param>
        /// <param name="_hitPoint">命中位置</param>
        private void OnOnHit(IAttackInfo _attackInfo, GameObject _BeHit, TargetUnit _BeHitUnit, ActionStatePart _part)
        {
            //Debug.Log($"命中对象[{_BeHit.name}] [{_BeHit.activeSelf}]");

            OnHitObject = _BeHit;
            hasHitobj = true;
            if (_BeHitUnit is not null)
            {
                hasHiter = true;
                HitUnit = _BeHitUnit;
            }
            _attackInfo.OnHit(_attackInfo, this, _BeHitUnit);

            if (_part is not null)
            {
                CheckHitJump(_part);
            }
            else
            {
                //命中轨道调用 
                foreach (var _actionStatePart in mAllActionStatePart)
                {
                    if (_actionStatePart.ActionEnble)
                    {
                        CheckHitJump(_actionStatePart);
                    }//是否启用
                }//所有层级
            }

            //扇出到 m_SkillDic 子树: 让 skill 上配置了"父级/源"目标的命中轨能响应本次命中
            FanoutOnHit_ToSkills(CurUnit);
        }

        private void CheckHitJump(ActionStatePart _actionStatePart)
        {
            foreach (var _actionInterrupt in _actionStatePart.CurrentActionState.InterruptList_OnHit)
            {
                if (CheckValueTrack(_actionInterrupt, (int)_actionStatePart.ElapsedTime))
                {
                    if (_actionStatePart.TryCheckInterrupCondition(_actionInterrupt, CurUnit,
                            out _, out int targetActionID, out _))
                    {
                        _actionStatePart.ChangeState(targetActionID,
                            _actionInterrupt.CrossFadeTime,
                            _actionInterrupt.OffsetTime, _actionStatePart.CurrentActionState);

                        //EngineDebug.LogError("命中跳转Action: " + _actionInterrupt.ActionID);
                        break;
                    } //检查条件是否满足
                }
            }//所有命中轨道
        }
        private bool CheckValueTrack(ActionInterrupt _actionInterrupt, int _curTime)
        {
            if (_actionInterrupt.Duration < 0)
                return _curTime >= _actionInterrupt.TriggerTime;

            if (_curTime >= _actionInterrupt.TriggerTime)
            {
                if (_curTime <= _actionInterrupt.TriggerTime + _actionInterrupt.Duration)
                {
                    return true;
                }
            }
            return false;
        }
    }
}