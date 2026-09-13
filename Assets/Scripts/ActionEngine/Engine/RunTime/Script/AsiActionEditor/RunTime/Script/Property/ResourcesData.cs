using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class UnitWarpInfo
    {
        public List<UnitWarp> mUnitWarp;
        public UnitWarpInfo(List<UnitWarp> _unitWarps)
        {
            mUnitWarp = _unitWarps;
        }
    }
    [System.Serializable]
    public struct ActionOverrideClip
    {
        public string StateClipName;//AnimatorStateName
        public string ClipPath;
        public ActionOverrideClip(string _stateClipName, string clipPath)
        {
            StateClipName = _stateClipName;
            ClipPath = clipPath;
        }
    }

    [System.Serializable]
    public class ActionStateInfo : IProperty
    {
        public readonly int ActionGroupID = 0;
        public readonly string ActionGroupName = string.Empty;
        public List<ActionState> mActionState;
        public List<int> mStartActionNames;
        public int mLayerCount;
        public int mHitActionID;
        public int mGroupLayerID = -1;
        public List<string> mActionType;
        public List<string> mActionLable;
        public List<ActionOverrideClip> mActionOverrideClip;
        public int[] mLayerOrder = new int[0];//层级启动顺序
        public List<string> mLayerNames;//层级名称（编辑器顺序，索引与 AnimaLayer 对应），用于运行时绘制层级名而非数字
        // 由编辑器保存动作数据时收集；当前仅含 Event_PlayParticle.PartoclePath。
        // 运行时装载完成后读取此列表预热特效对象池，避免首次播放时阻塞在异步加载。
        public List<string> mCollectWarmUp;
        [NonSerialized] private Dictionary<int, ActionState> mDic_Action_ID;
        [NonSerialized] private Dictionary<string, ActionState> mDic_Action_Name;
        [NonSerialized] private bool isInit = false;
        [NonSerialized] private Dictionary<string, AnimationClip> mDic_Anima;

        /// <summary>
        /// 不用调用这个函数,在数据加载出来时就已经第一时间初始化过了
        /// </summary>
        public void Init()
        {
            if (isInit) return;
            mDic_Action_ID = new Dictionary<int, ActionState>(mActionState.Count);
            mDic_Action_Name = new Dictionary<string, ActionState>(mActionState.Count);
            if (mActionOverrideClip is not null)
            {
                mDic_Anima = new Dictionary<string, AnimationClip>(mActionOverrideClip.Count);
                //EngineDebug.LogError("字典初始化了");
            }
            foreach (ActionState action in mActionState)
            {
                mDic_Action_ID.Add(action.ID, action);
                mDic_Action_Name.Add(action.Name, action);
            }
            isInit = true;
        }

        public void SetClip(string _key, AnimationClip _clip)
        {
            Init();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (mActionOverrideClip is not null && mDic_Anima is null)
                    mDic_Anima = new Dictionary<string, AnimationClip>(mActionOverrideClip.Count);
            }
#endif
            if (!mDic_Anima.TryAdd(_key, _clip))
                mDic_Anima[_key] = _clip;
            //EngineDebug.LogWarning($"加载成功：替换：【{_key}】 到【{_clip.name}】");
        }

        public bool TryGetClip(string _key, out AnimationClip _clip)
        {
            return mDic_Anima.TryGetValue(_key, out _clip);
        }

        public bool TryGetAction(int _id, out ActionState _state)
        {
#if UNITY_EDITOR

            if (mDic_Action_ID == null)
            {
                if (!Application.isPlaying)
                {
                    Init();
                    return mDic_Action_ID.TryGetValue(_id, out _state);
                }
                Debug.LogError("<color=#ffcc00>ActionStateInfo未初始化</color>" + GetHashCode() + $"   {isInit}");
                _state = null;
                return false;
            }
#endif
            return mDic_Action_ID.TryGetValue(_id, out _state);
        }

        public bool PerformActionEvent(ActionStatePart _part, int _actionID = 0)
        {
            //int slotID = -1;
            //EngineDebug.LogError($"------------------------------------------------------------------------------------" + 
            //    $"\n[SkillGvalueMap] Apply skillTypeId=执行初始化层级的AGID  [{ActionGroupID}]");
            if (TryGetAction(_actionID, out ActionState _state))
            {
                _part.ActionStateMachine.SetFirstActionGroupID(ActionGroupID);

                //string str = $"[SkillGvalueMap] Apply skillTypeId=尝试执行初始化事件成功!!! [事件数量{_state.EventList.Count}] <color=#ffcc00>[{ActionGroupID}:{ActionGroupName}]</color> [{GetHashCode()}]";
                foreach (ActionEvent _event in _state.EventList)
                {
                    if (_event.Duration == 0)
                    {
                        //str += $"\nEventType: [{_event.EventData.GetType().Name}]";
                        if (_event.Check.value(_part, EngineResourcesManager.Instance.MachineTime))
                        {
                            //str += "  <color=#ccff00>True</color>"; 
                            _event.EventData.Enter(_part, true);
                        }
                        else
                        {
                            //str += "  <color=#ff0000>False</color>";
                        }
                    }
                }
                //EngineDebug.LogError(str);
                return true;
            }
            //else
            //{
            //    string _str = "";
            //    foreach (var _event in mDic_Action_ID)
            //    {
            //        _str += $"\nID: {_event.Key}   Name: {_event.Value.Name}";
            //    }
            //    EngineDebug.LogError($"尝试执行初始化事件<color=#ff0000>失败</color>!!! [{mDic_Action_ID.Count}]{_str}");
            //}
            return false;
    }

        public bool TryGetAction(string _name, out ActionState _state)
            => mDic_Action_Name.TryGetValue(_name, out _state);

        public ActionStateInfo(List<ActionState> _actionStates, int _id, string _DisName)
        {
            //if (string.IsNullOrEmpty(_DisName))
            //{
            //    EngineDebug.LogError($"当前保存的ActionName空了!!!!! 请检查原因");
            //}
            mActionState = _actionStates;
            ActionGroupName = _DisName;
            ActionGroupID = _id;
            mStartActionNames = new List<int>();
            mActionType = new List<string>();
            mActionLable = new List<string>();
            isInit = false;
        }



        public ActionStateInfo Clone()
        {
            EngineResourcesManager.Instance.Active_ActionStateInfo = this;

            ActionStateInfo _newAction = new ActionStateInfo(mActionState, ActionGroupID, ActionGroupName);
            _newAction.mStartActionNames = mStartActionNames;
            _newAction.mLayerCount = mLayerCount;
            _newAction.mHitActionID = mHitActionID;
            _newAction.mActionType = mActionType;
            _newAction.mActionLable = mActionLable;
            _newAction.mActionOverrideClip = mActionOverrideClip;
            _newAction.mLayerOrder = mLayerOrder;
            _newAction.mLayerNames = mLayerNames;
            _newAction.mGroupLayerID = mGroupLayerID;
            _newAction.mCollectWarmUp = mCollectWarmUp;

            List<ActionState> _actionState = new List<ActionState>(mActionState.Count);
            foreach (var actionState in mActionState)
            {
                EngineResourcesManager.Instance.Active_ActionState = actionState;
                _actionState.Add(actionState.Clone());
            }
            _newAction.mActionState = _actionState;

            _newAction.Init();
            return _newAction;
        }
    }
}