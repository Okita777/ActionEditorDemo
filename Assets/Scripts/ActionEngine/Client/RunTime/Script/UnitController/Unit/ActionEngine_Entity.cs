using AsiActionEngine.RunTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using static AsiActionEngine.RunTime.ActionStateMachine;

namespace AsiTimeLine.RunTime
{
    [MovedFrom(true, sourceNamespace: "AsiTimeLine.RunTime", sourceAssembly: "ActionEditor_Ex_RunTime", sourceClassName: "Entity")]
    public partial class ActionEngine_Entity : ActionEngine_Unit
    {
        #region EnumAndStruct
        private enum EquipRequestType
        {
            ActionInfo,
            ActionId,
            ActionIdList,
        }
        private struct ActionEquipRequest
        {
            public EquipRequestType Type;
            public ActionStateInfo ActionInfo;
            public int ActionId;
            public List<int> ActionIdList;
            public Action<bool> LoadComplete;
        }

        public struct SActionPropData
        {
            public int mSlotID;
            public ECharacteLimbType mPoint;
            public SActionPropData(int slot, ECharacteLimbType point)
            {
                mSlotID = slot;
                mPoint = point;
            }
        }
        #endregion

        [HideInInspector] public Dictionary<int, ActionEngine_Prop>  mPropDic_Slot = new Dictionary<int, ActionEngine_Prop>();
        [NonSerialized] private Dictionary<int, int> mDicSlotIDToActionListID = new Dictionary<int, int>();
        [NonSerialized] private int mLoadAction_Number = 0;
        [NonSerialized] private string mLastActionList;
        [NonSerialized] private Action<bool> mLoadAction_loadComplet;
        [NonSerialized] private List<ActionStateInfo> mLoadAction_List = new List<ActionStateInfo>(4);
        [NonSerialized] private bool mEquipActionBusy = false;
        [NonSerialized] private Queue<ActionEquipRequest> mEquipActionQueue = new Queue<ActionEquipRequest>(8);

        public Dictionary<int, int> DicSlotIDToActionListID => mDicSlotIDToActionListID;

        public override void Init()
        {
            base.Init();
            mDicSlotIDToActionListID.Clear();
            mEquipActionBusy = false;
            mEquipActionQueue.Clear();
        }

        public bool TryGetPropToSlotID(int _id, out ActionEngine_Prop _prop)
        {
            return mPropDic_Slot.TryGetValue(mLoadAction_Number, out _prop);
        }

        public bool TryGetSlotIDToActionListID(int _id, out int _slotID)
        {
            if (mDicSlotIDToActionListID.TryGetValue(_id, out int _mySlotID))
            {
                _slotID = _mySlotID;
                return true;
            }
            //if(mDicSlotIDToActionListID.TryGetValue(_id, out string name))
            //{
            //    return mReferSloatID.TryGetValue(name, out _slotID);
            //}
            //else
            //{
            //    EngineDebug.LogError($"获取槽位ID失败, 不存在Action[{_id}]的槽位");
            //}
            _slotID = -1;
            return false;
        }

        /// <summary>
        /// 装备道具(Prop)
        /// </summary>
        /// <param name="propID">道具ID</param>
        /// <param name="slot">槽位ID</param>
        /// <param name="point">武器挂点</param>
        /// <param name="loadComplet">加载完成的回调</param>
        public void EquipProp(int propID, int slot, ECharacteLimbType point, Action<bool> loadComplet)
        {
            ActionEnginLoadData.Instance.LoadInfo(EInfoType.PropWarp, _obj =>
            {
                if (_obj is PropWarp _warp)
                {
                    EngineResourcesManager.Instance.CreactObjToComponent<ActionEngine_Prop>(_warp.ModelPath, _preMode =>
                    {
                        if (_preMode is ActionEngine_Prop _prop)
                        {
                            if (!mPropDic_Slot.TryAdd(slot, _prop))
                            {
                                mPropDic_Slot[slot] = _prop;
                            }
                            if (ActionStateMachine.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig)))
                            {
                                if (_config.HelpPointDic.TryGetValue(point, out Transform _target))
                                {
                                    _prop.transform.SetParent(_target);
                                    _prop.transform.SetPositionAndRotation(_target.position, _target.rotation);
                                }
                            }
                            loadComplet(true);
                        }
                        else
                        {
                            EngineDebug.LogError("加载的不是 ActionEngine_Prop 类型");
                            loadComplet(false);
                        }
                    }, 1, 10, -1, (int)EObjPoolParent.Prop);
                }
            }, propID);
        }
        public void EquipProp(ActionEngine_Prop _prop, SActionPropData _sActionProp)
        {
            int slot = _sActionProp.mSlotID;
            ECharacteLimbType point = _sActionProp.mPoint;
            if (!mPropDic_Slot.TryAdd(slot, _prop))
            {
                mPropDic_Slot[slot] = _prop;
            }
            if (ActionStateMachine.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig)))
            {
                if (_config.HelpPointDic.TryGetValue(point, out Transform _target))
                {
                    _prop.transform.SetParent(_target);
                    _prop.transform.SetPositionAndRotation(_target.position, _target.rotation);
                }
            }
        }

        /// <summary>
        /// 卸载道具(Prop)
        /// </summary>
        /// <param name="propID">道具ID</param>
        /// <param name="slot">槽位ID</param>
        /// <returns></returns>
        public bool UnEquipProp(int propID, int slot)
        {
            if (mPropDic_Slot.TryGetValue(slot, out ActionEngine_Prop _prop))
            {
                ActionEnginLoadData.Instance.LoadInfo(EInfoType.PropWarp, _obj =>
                {
                    if (_obj is PropWarp _warp)
                    {
                        EngineResourcesManager.Instance.RemoveComponent(_warp.ModelPath, _prop);
                    }
                }, propID);
                mPropDic_Slot.Remove(slot);
                return true;
            }
            return false;
        }
        public bool UnEquipProp(int slot)
        {
            return mPropDic_Slot.Remove(slot);
        }

        /// <summary>
        /// 卸载所有道具(Prop)
        /// </summary>
        public void UnAllEquipProp()
        {
            foreach (KeyValuePair<int, ActionEngine_Prop> item in mPropDic_Slot)
            {
                ActionEnginLoadData.Instance.LoadInfo(EInfoType.PropWarp, _obj =>
                {
                    if (_obj is PropWarp _warp)
                    {
                        EngineResourcesManager.Instance.RemoveComponent(_warp.ModelPath, item.Value);
                    }
                }, item.Key);
            }
            mPropDic_Slot.Clear();
        }

        #region 动作装卸
        /// <summary>
        /// 动作装备(Action)
        /// </summary>
        /// <param name="actionID">动作ID</param>
        /// <param name="loadComplet"></param>
        public void EquipActionInfo(int actionID, Action<bool> loadComplet)
        {
            EnqueueEquipAction(new ActionEquipRequest
            {
                Type = EquipRequestType.ActionId,
                ActionId = actionID,
                LoadComplete = loadComplet,
            });
        }

        /// <summary>
        /// 动作装备(Action)
        /// </summary>
        /// <param name="actionIDList">动作ID</param>
        /// <param name="loadComplet"></param>
        public void EquipActionInfo(List<int> actionIDList, Action<bool> loadComplet)
        {
            EnqueueEquipAction(new ActionEquipRequest
            {
                Type = EquipRequestType.ActionIdList,
                ActionIdList = actionIDList == null ? null : new List<int>(actionIDList),
                LoadComplete = loadComplet,
            });
        }


        public void EquipActionInfo(int actionID, int slotID, Action<bool> loadComplet)
        {
            EquipActionListDic(actionID, slotID);
            EnqueueEquipAction(new ActionEquipRequest
            {
                Type = EquipRequestType.ActionId,
                ActionId = actionID,
                LoadComplete = loadComplet,
            });
        }
        public void EquipActionInfo(List<int> actionIDList, List<int> slotID, Action<bool> loadComplet)
        {
            if (actionIDList.Count == slotID.Count)
            {
                for (int i = 0; i < actionIDList.Count; i++)
                {
                    EquipActionListDic(actionIDList[i], slotID[i]);
                }
            }
            else
            {
                string _str = $"技能列表装备错误,技能长度和槽位长度不一致!!";
                EngineDebug.LogError(_str);
                EngineDebug.DisplayDialog("错误!!!", _str, "OK");
            }
            EnqueueEquipAction(new ActionEquipRequest
            {
                Type = EquipRequestType.ActionIdList,
                ActionIdList = actionIDList == null ? null : new List<int>(actionIDList),
                LoadComplete = loadComplet,
            });
        }


        public void EquipActionListDic(int actionID, int slotID)
        {
#if UNITY_EDITOR

#endif
            EngineDebug.Log($"[SkillGvalueMap] {gameObject.name}: <color=#ffcc00>已经将[{actionID}]装备至槽位[{slotID}]</color>");

            //确保一个槽位只有一个技能
            if (mDicSlotIDToActionListID.ContainsValue(slotID))
            {
                int _removeKey = -1;
                foreach (var item in mDicSlotIDToActionListID)
                {
                    if(item.Value == slotID)
                    {
                        _removeKey = item.Key;
                        break;
                    }
                }
                if(_removeKey > -1)
                    mDicSlotIDToActionListID.Remove(_removeKey);
            }

            if (!mDicSlotIDToActionListID.TryAdd(actionID, slotID))
            {
                mDicSlotIDToActionListID[actionID] = slotID;
            }
        }

        /// <summary>
        /// 动作装备
        /// </summary>
        /// <param name="_actionStateInfo"></param>
        public void EquipActionInfo(ActionStateInfo _actionStateInfo)
        {
            EnqueueEquipAction(new ActionEquipRequest
            {
                Type = EquipRequestType.ActionInfo,
                ActionInfo = _actionStateInfo,
            });
        }

        /// <summary>
        /// 动作装备（带槽位ID）
        /// </summary>
        public void EquipActionInfo(ActionStateInfo _actionStateInfo, int slotID)
        {
            EquipActionListDic(_actionStateInfo.ActionGroupID, slotID);
            EnqueueEquipAction(new ActionEquipRequest
            {
                Type = EquipRequestType.ActionInfo,
                ActionInfo = _actionStateInfo,
            });
        }


        /// <summary>
        /// 卸载动作
        /// </summary>
        /// <param name="actionID">动作ID</param>
        /// <returns></returns>
        public bool UnEquipAction(int actionID)
        {
            mDicSlotIDToActionListID.Remove(actionID);
            return ActionStateMachine.UnEquipActionInfo(actionID);
        }
        /// <summary>
        /// 卸载动作
        /// </summary>
        /// <param name="actionID"></param>
        /// <returns></returns>
        public bool UnEquipAction(ActionStateInfo _actionStateInfo)
        {
            mDicSlotIDToActionListID.Remove(_actionStateInfo.ActionGroupID);
            return ActionStateMachine.UnEquipActionInfo(_actionStateInfo);
        }
        /// <summary>
        /// 卸载动作
        /// </summary>
        /// <param name="actionID">动作ID列表</param>
        /// <returns></returns>
        public bool UnEquipAction(List<int> actionID)
        {
            for (int i = 0; i < actionID.Count; i++)
                mDicSlotIDToActionListID.Remove(actionID[i]);
            return ActionStateMachine.UnEquipActionInfo(actionID);
        }

        /// <summary>
        /// 卸载所有动作
        /// </summary>
        public void UnEquipAllAction()
        {
            mDicSlotIDToActionListID.Clear();
            ActionStateMachine.UnEquipAllActionInfo();
        }
        #endregion



        private void EnqueueEquipAction(ActionEquipRequest request)
        {
            mEquipActionQueue.Enqueue(request);
            TryProcessEquipActionQueue();
        }

        private void TryProcessEquipActionQueue()
        {
            if (mEquipActionBusy || mEquipActionQueue.Count == 0)
            {
                return;
            }

            var request = mEquipActionQueue.Dequeue();
            mEquipActionBusy = true;

            switch (request.Type)
            {
                case EquipRequestType.ActionInfo:
                    ProcessEquipActionInfo(request.ActionInfo, request.LoadComplete);
                    break;
                case EquipRequestType.ActionId:
                    ProcessEquipActionId(request.ActionId, request.LoadComplete);
                    break;
                case EquipRequestType.ActionIdList:
                    ProcessEquipActionIdList(request.ActionIdList, request.LoadComplete);
                    break;
            }
        }

        private void FinishEquipActionRequest(Action<bool> loadComplete, bool success)
        {
            loadComplete?.Invoke(success);
            mEquipActionBusy = false;
            TryProcessEquipActionQueue();
        }

        private void ProcessEquipActionInfo(ActionStateInfo actionInfo, Action<bool> loadComplete)
        {
            if (actionInfo == null)
            {
                FinishEquipActionRequest(loadComplete, false);
                return;
            }

            bool finished = false;
            void CompleteOnce(bool success)
            {
                if (finished) return;
                finished = true;
                FinishEquipActionRequest(loadComplete, success);
            }

            bool onEqu = ActionStateMachine.EquipActionInfo(actionInfo, CompleteOnce, out List<SLoadAnimationClip> loadInfo);
            if (onEqu)
            {
                LoadAnimClip(loadInfo);
            }
        }

        private void ProcessEquipActionId(int actionId, Action<bool> loadComplete)
        {
            if (actionId <= 0)
            {
                FinishEquipActionRequest(loadComplete, false);
                return;
            }

            ActionEnginLoadData.Instance.LoadInfo(EInfoType.UnitAction, _obj =>
            {
                if (_obj is ActionStateInfo _info)
                {
                    bool finished = false;
                    void CompleteOnce(bool success)
                    {
                        if (finished) return;
                        finished = true;
                        FinishEquipActionRequest(loadComplete, success);
                    }

                    bool onEqu = ActionStateMachine.EquipActionInfo(_info, CompleteOnce, out List<SLoadAnimationClip> _loadInfo);
                    if (onEqu)
                    {
                        LoadAnimClip(_loadInfo);
                    }
                }
                else
                {
                    FinishEquipActionRequest(loadComplete, false);
                }
            }, actionId);
        }

        private void ProcessEquipActionIdList(List<int> actionIDList, Action<bool> loadComplet)
        {
            if (actionIDList == null || actionIDList.Count == 0)
            {
                FinishEquipActionRequest(loadComplet, false);
                return;
            }

#if UNITY_EDITOR
            mLastActionList = $"\nActionGroupID: [{actionIDList[0]}]";
            for (int i = 1; i < actionIDList.Count; i++)
            {
                mLastActionList += $"\nActionGroupID: [{actionIDList[i]}]";
            }
#endif

            if (mLoadAction_Number > 0)
            {
                FinishEquipActionRequest(loadComplet, false);
                return;
            }

            bool finished = false;
            void CompleteOnce(bool success)
            {
                if (finished) return;
                finished = true;
                FinishEquipActionRequest(loadComplet, success);
            }

            mLoadAction_Number = actionIDList.Count;
            mLoadAction_loadComplet = CompleteOnce;
            mLoadAction_List.Clear();

            foreach (int actionID in actionIDList)
            {
                ActionEnginLoadData.Instance.LoadInfo(EInfoType.UnitAction, _obj =>
                {
                    if (_obj is ActionStateInfo _info)
                    {
                        mLoadAction_List.Add(_info);
                    }
                    mLoadAction_Number--;

                    if (mLoadAction_Number == 0)
                    {
                        bool _onEqu = ActionStateMachine.EquipActionInfo(mLoadAction_List, mLoadAction_loadComplet, out List<SLoadAnimationClip> _loadInfo);
                        if (_onEqu)
                        {
                            LoadAnimClip(_loadInfo);
                        }
                    }
                }, actionID);
            }
        }



        private void LoadAnimClip(List<SLoadAnimationClip> _loadInfo)
        {
            if (Channel == ERuntimeDataChannel.Server)
            {
                return;
            }

            //动画加载实现
            if (_loadInfo.Count > 0)
            {
                foreach (SLoadAnimationClip clip in _loadInfo)
                {
                    // EngineDebug.LogWarning($"动画加载尝试！！ 路径：【<color=#ffcc00>{clip._loadPath}</color>】");
                    EngineResourcesManager.Instance.AsyncLoadObj(clip._loadPath, (_obj) =>
                    {
                        if (_obj is AnimationClip _clip)
                        {
                            //成功加载
                            //Debug.Log($"动画加载成功！！ 路径：【<color=#ffcc00>{clip._loadPath}</color>】");
                            clip.LoadCompleted(_clip);
                        }
                        else
                        {
                            //加载失败  并提示错误信息
                            clip.LoadFailed($"动画加载失败！！ 路径：【<color=#ffcc00>{clip._loadPath}</color>】");
                        }
                    });
                }
            }
            else
            {
                EngineDebug.LogWarning($"【<color=#ffcc00>需要加载的动画数量为零</color>】");
            }
        }
    }
}
