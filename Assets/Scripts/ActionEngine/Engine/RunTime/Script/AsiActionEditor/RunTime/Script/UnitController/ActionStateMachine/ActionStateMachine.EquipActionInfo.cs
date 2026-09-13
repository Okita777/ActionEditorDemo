using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public partial class ActionStateMachine
    {
        #region PrivateClass
        private class AnimationClipOverrides : List<KeyValuePair<AnimationClip, AnimationClip>>
        {
            // 优化：缓存动画名称到索引的映射，避免每次都遍历列表和访问 .name 属性
            private Dictionary<string, int> _nameToIndexCache = new Dictionary<string, int>();
            public AnimationClipOverrides(int capacity) : base(capacity)
            {
                _nameToIndexCache = new Dictionary<string, int>(capacity);
            }
            //#if UNITY_EDITOR
            private bool isReset = false;
            private Dictionary<int, KeyValuePair<AnimationClip, AnimationClip>> _pairs = new Dictionary<int, KeyValuePair<AnimationClip, AnimationClip>>();
            //#endif

            // 优化：构建名称缓存，只在初始化时调用一次
            public void BuildCache()
            {
                _nameToIndexCache.Clear();
                for (int i = 0; i < Count; i++)
                {
                    if (this[i].Key != null)
                    {
                        // 只访问一次 .name 属性并缓存，避免 Unity 每次都创建新字符串
                        _nameToIndexCache[this[i].Key.name] = i;
                    }
                }
            }

            public AnimationClip this[string name]
            {
                get
                {
                    // 优化：使用缓存查找，O(1) 复杂度，避免遍历和字符串创建
                    if (_nameToIndexCache.TryGetValue(name, out int index))
                    {
                        return this[index].Value;
                    }
                    return null;
                }
                set
                {
                    // 优化：使用缓存查找，避免 FindIndex 遍历整个列表
                    if (_nameToIndexCache.TryGetValue(name, out int index))
                    {
                        KeyValuePair<AnimationClip, AnimationClip> pair =
                            new KeyValuePair<AnimationClip, AnimationClip>(this[index].Key, value);
                        //#if UNITY_EDITOR
                        if (!isReset)
                        {
                            if (!_pairs.TryAdd(index, pair))
                                _pairs[index] = pair;
                        }
                        //#endif

                        this[index] = pair;
                    }
                }
            }

#if UNITY_EDITOR
            public void SetOverrideEnble(bool _enble)
            {
                isReset = true;

                if (!_enble)
                {
                    // EngineDebug.Log("重置");
                    foreach (var VARIABLE in _pairs)
                    {
                        int _id = VARIABLE.Key;
                        this[_id] = new KeyValuePair<AnimationClip, AnimationClip>(this[_id].Key, null);
                    }
                }
                else
                {
                    // EngineDebug.Log("恢复");
                    foreach (var VARIABLE in _pairs)
                    {
                        this[VARIABLE.Key] = VARIABLE.Value;
                    }
                }

                isReset = false;
            }
#endif
            public void ResetOverride()
            {
                isReset = true;
                foreach (var VARIABLE in _pairs)
                {
                    int _id = VARIABLE.Key;
                    this[_id] = new KeyValuePair<AnimationClip, AnimationClip>(this[_id].Key, null);
                }
                _pairs.Clear();
                isReset = false;
            }
        }
        #endregion

        #region Struct
        public struct SLoadAnimationClip
        {
            public string _loadPath;
            private Action<AnimationClip> LoadClip;
            private Action LoadClipFailed;

            public SLoadAnimationClip(string loadPath, Action<AnimationClip> loadClip, Action loadClipFailed)
            {
                _loadPath = loadPath;
                LoadClip = loadClip;
                LoadClipFailed = loadClipFailed;
            }

            public void LoadCompleted(AnimationClip clip)
            {
                LoadClip(clip);
            }

            public void LoadFailed(string error = "")
            {
                LoadClipFailed();
#if UNITY_EDITOR
                if (string.IsNullOrEmpty(error))
                {
                    EngineDebug.LogError($"动画加载失败！！ 路径：【<color=#ffcc00>{_loadPath}</color>】");
                }
                else
                {
                    EngineDebug.LogError(error);
                }
#endif
            }
        }
        //public struct SItemWarp
        //{
        //    public PropWarp _warp;
        //    public ActionEngine_Prop _prop;

        //    public SItemWarp(PropWarp warp, ActionEngine_Prop gameObject)
        //    {
        //        _warp = warp;
        //        _prop = gameObject;
        //    }
        //}
        #endregion

        private bool isOverrideAnimator = false;
        private AnimatorOverrideController OverrideController = null;
        private List<ActionStateInfo> mEquipActionInfoList = new List<ActionStateInfo>(8);
        private List<ActionOverrideClip> mOverrideClip = null;
        private HashSet<ActionStateInfo> mEquipActionInfoList_HashSet = new HashSet<ActionStateInfo>(8);
        private HashSet<string> mCheckAnimClip = null;

        private int loadClipConst = 0;
        private bool loadItemModeFlish = true;
        private bool loadItemFlish = true;
        private AnimationClipOverrides clipOverrides;
        private List<SLoadAnimationClip> mLoadAnimationClips = new List<SLoadAnimationClip>();//所有要加载的AnimtionClip
        //private Dictionary<string, AnimationClip> mAnimationClipDic;//State所对应的Clip资产
        private List<ActionStateInfo> mActionInfoTransList = null;
        //private ActionEngine_Prop loadEngineProp = null;
        private bool OnEquipActionInfo(ActionStateInfo _info, Action<bool> _loadComplete, out List<SLoadAnimationClip> _loadClips)
        {
            //EngineDebug.LogError("装备AG");
            ActionStatePart _part = EngineResourcesManager.Instance.GetPreActionStatePart();
            _part.SetInitValue(this, false);

            SetActiveActionGroup(_part.CurrentActionState, _part, _info.ActionGroupID);
            //_info.PerformActionEvent(_part);//执行第0层逻辑

            if (!isOverrideAnimator)
            {
                mCheckAnimClip = new HashSet<string>(64);
                mOverrideClip = new List<ActionOverrideClip>(64);
                //mAnimationClipDic = new Dictionary<string, AnimationClip>(64);
                mActionInfoTransList = new List<ActionStateInfo>(1);
                mActionInfoTransList.Add(_info);

                //无动画机（如服务端逻辑单位）时跳过动画覆写机构建，避免空引用
                if (AnimValid)
                {
                    //创建OverrideAnimator
                    if (CurAnimator?.runtimeAnimatorController is not null)
                    {
                        OverrideController = new AnimatorOverrideController(CurAnimator.runtimeAnimatorController);
                        OverrideController.name = "ActionEngineOverride";
                    }
                    CurAnimator.runtimeAnimatorController = OverrideController;

                    //初次装备武器时，实例化一次
                    clipOverrides = new AnimationClipOverrides(OverrideController.overridesCount);
                    OverrideController.GetOverrides(clipOverrides);

                    // 优化：构建名称缓存，避免后续频繁的字符串创建和列表遍历
                    clipOverrides.BuildCache();
                }

                //初始化加载参数
                mLoadAnimationClips.Clear();
                loadClipConst = 0;
                loadItemFlish = true;
                loadItemModeFlish = true;

                //结束初始化
                isOverrideAnimator = true;
            }

            if (!loadItemFlish)
            {
#if UNITY_EDITOR
                string _str = "动作模组加载失败!!因为上一个 动作模组 未加载完成,无法立马加载新的 动作模组";
                EngineDebug.DisplayDialog("动作模组加载失败!!", _str, "OK");
                EngineDebug.LogError(_str);
#endif
                _loadComplete?.Invoke(false);

                _loadClips = null;
                //_loadMode = null;
                return false;
            }

            //检查是否已经装备过这个动作模组
            if (mEquipActionInfoList_HashSet.Contains(_info))
            {
#if UNITY_EDITOR
                EngineDebug.LogWarning($"已装备过此Action [<color=#ffcc00>{_info.ActionGroupID}</color>]");
#endif

                SetFirstActionGroupID(_info.ActionGroupID);
                _part.SetActiveActionGroupID(_info.ActionGroupID);
                _loadComplete?.Invoke(true);
                _info.PerformActionEvent(_part);//执行第0层逻辑

                _loadClips = null;
                loadItemFlish = true;
                return false;
            }

            //初始化加载参数
            mLoadAnimationClips.Clear();
            mOverrideClip.Clear();
            mCheckAnimClip.Clear();
            loadClipConst = 0;
            loadItemFlish = false;
            loadItemModeFlish = false;

            mEquipActionInfoList.Add(_info);
            mEquipActionInfoList_HashSet.Add(_info);
            mEquipActionInfoList.Sort((y, x) => (x.ActionGroupID.CompareTo(y.ActionGroupID)));//ID数字越大,优先级越高

            mActionInfoTransList[0] = _info;

            //注册每个动画片段的加载回调
            OnUpdateClipOverrides(mActionInfoTransList, isComplete =>
            {
                //EngineDebug.LogError("初始化武器Action");

                //Debug.Log("初始化武器Action");
                //初始化动画
                //foreach (int actionID in warp.StartAction)
                //{
                //    ChangeAction(actionID, 0, 0);
                //}

                //完成加载
                loadItemFlish = true;

                //执行回调
                SetFirstActionGroupID(_info.ActionGroupID);
                _part.SetActiveActionGroupID(_info.ActionGroupID);
                _loadComplete(isComplete);//在回调中注册技能槽位
                _info.PerformActionEvent(_part);//执行第0层逻辑

                ResetAllAnimatorAnim();//重新排列动画
            });

            //返回目前需求加载的动画片段
            _loadClips = mLoadAnimationClips;
            return true;
        }
        private bool OnEquipActionInfo(List<ActionStateInfo> _info, Action<bool> _loadComplete, out List<SLoadAnimationClip> _loadClips)
        {
            ActionStatePart _part = EngineResourcesManager.Instance.GetPreActionStatePart();
            _part.SetInitValue(this, false);
            _part.CurrentActionState.AnimaLayer = 0;

            //string _str2 = $"<color=#ff0000>Action装备列表: [{_info.Count}]</color>";
            //foreach (ActionStateInfo actionInfo in _info)
            //{
            //    _str2 += $"\n{actionInfo.ActionGroupID}";
            //}
            //Debug.Log(_str2);

            if (!isOverrideAnimator)
            {
                mCheckAnimClip = new HashSet<string>(64);
                mOverrideClip = new List<ActionOverrideClip>(64);
                //mAnimationClipDic = new Dictionary<string, AnimationClip>(64);
                mActionInfoTransList = new List<ActionStateInfo>(1);
                mActionInfoTransList.Add(_info[0]);

                //无动画机（如服务端逻辑单位）时跳过动画覆写机构建，避免空引用
                if (AnimValid)
                {
                    //创建OverrideAnimator
                    if (CurAnimator?.runtimeAnimatorController is not null)
                    {
                        OverrideController = new AnimatorOverrideController(CurAnimator.runtimeAnimatorController);
                        OverrideController.name = "ActionEngineOverride";
                    }
                    CurAnimator.runtimeAnimatorController = OverrideController;

                    //初次装备武器时，实例化一次
                    clipOverrides = new AnimationClipOverrides(OverrideController.overridesCount);
                    OverrideController.GetOverrides(clipOverrides);

                    // 优化：构建名称缓存，避免后续频繁的字符串创建和列表遍历
                    clipOverrides.BuildCache();
                }

                //初始化加载参数
                mLoadAnimationClips.Clear();
                loadClipConst = 0;
                loadItemFlish = true;
                loadItemModeFlish = true;

                //结束初始化
                isOverrideAnimator = true;
            }

            if (!loadItemFlish)
            {
#if UNITY_EDITOR
                string _str = "动作模组加载失败!!因为上一个 动作模组 未加载完成,无法立马加载新的 动作模组";
                EngineDebug.DisplayDialog("动作模组加载失败!!", _str, "OK");
                EngineDebug.LogError(_str);
#endif
                _loadComplete?.Invoke(false);
                _loadClips = null;
                //_loadMode = null;
                return false;
            }

            bool _isEquip = false;
            //检查是否已经装备过这个动作模组
            foreach (ActionStateInfo item in _info)
            {
                if (!mEquipActionInfoList_HashSet.Contains(item))
                {
                    SetFirstActionGroupID(item.ActionGroupID);
                    SetActiveActionGroup(_part.CurrentActionState, _part, item.ActionGroupID);
                    item.PerformActionEvent(_part);//初始化事件

                    _isEquip = true;
                    mEquipActionInfoList.Add(item);
                    mEquipActionInfoList_HashSet.Add(item);
                }
#if UNITY_EDITOR
                else
                {
                    EngineDebug.LogWarning($"已装备过此Action [<color=#ffcc00>{item.ActionGroupID}</color>]");

                }
#endif
            }

            if (!_isEquip)
            {
                _loadComplete?.Invoke(true);
                _loadClips = null;
                loadItemFlish = true;
                return false;
            }


            //初始化加载参数
            mLoadAnimationClips.Clear();
            mOverrideClip.Clear();
            mCheckAnimClip.Clear();
            loadClipConst = 0;
            loadItemFlish = false;
            loadItemModeFlish = false;

            //mEquipActionInfoList.AddRange(_info);
            //mEquipActionInfoList_HashSet.AddRange(_info);
            mEquipActionInfoList.Sort((y, x) => (x.ActionGroupID.CompareTo(y.ActionGroupID)));//ID数字越大,优先级越高

            //注册每个动画片段的加载回调
            OnUpdateClipOverrides(_info, isComplete =>
            {
                //Debug.Log("初始化武器Action");
                //初始化动画
                //foreach (int actionID in warp.StartAction)
                //{
                //    ChangeAction(actionID, 0, 0);
                //}
                //完成加载
                loadItemFlish = true;

                //执行回调
                _loadComplete(isComplete);
                ResetAllAnimatorAnim();//重新排列动画
            });

            //返回目前需求加载的动画片段
            _loadClips = mLoadAnimationClips;
            return true;
        }

        private bool OnUnEquipActionInfo(ActionStateInfo _info)
        {
            //bool _isPlayer = CurUnit == EngineResourcesManager.Instance.Player;
            //if (_isPlayer) Debug.LogError($"尝试卸载ActionInfo2 [{_info.ActionGroupID}]");
            if (mEquipActionInfoList.Remove(_info))
            {
                mEquipActionInfoList_HashSet.Remove(_info);
                ClearActionGroupContext();
                ResetAllAnimatorAnim();
                return true;
            }
            return false;
        }
        private bool OnUnEquipAllActionInfo()
        {
            //bool _isPlayer = CurUnit == EngineResourcesManager.Instance.Player;
            //if (_isPlayer) Debug.LogError($"尝试卸载所有ActionInfo");
            mEquipActionInfoList.Clear();
            mEquipActionInfoList_HashSet.Clear();
            ClearActionGroupContext();
            ResetAllAnimatorAnim();
            return true;
        }
        private bool OnUnEquipActionInfo(int _actionGroupID)
        {
            //bool _isPlayer = CurUnit == EngineResourcesManager.Instance.Player;
            //if (_isPlayer) Debug.LogError($"尝试卸载ActionInfo [{_actionGroupID}]");
            //EngineDebug.LogError($"尝试卸载ActionInfo [{_actionGroupID}]");
            for (int i = 0; i < mEquipActionInfoList.Count; i++)
            {
                ActionStateInfo _actionInfo = mEquipActionInfoList[i];
                if (_actionInfo.ActionGroupID == _actionGroupID)
                {
                    mEquipActionInfoList.Remove(_actionInfo);
                    mEquipActionInfoList_HashSet.Remove(_actionInfo);
                    ClearActionGroupContext();
                    ResetAllAnimatorAnim();
                    //EngineDebug.Log($"<color=#ffcc00>成功卸载ActionInfo</color> [{_actionGroupID}]");
                    return true;
                }
            }
            return false;
        }
        private bool OnUnEquipActionInfo(List<ActionStateInfo> _info)
        {
            bool isFind = false;
            foreach (ActionStateInfo item in _info)
            {
                if (mEquipActionInfoList.Remove(item))
                {
                    mEquipActionInfoList_HashSet.Remove(item);
                    isFind = true;
                }
            }

            if (isFind)
            {
                ClearActionGroupContext();
                ResetAllAnimatorAnim();
            }
            return isFind;
        }

        private bool OnUnEquipActionInfo(List<int> _actionGroupID)
        {
            bool isFind = false;
            foreach (int removeInfoID in _actionGroupID)
            {
                for (int i = 0; i < mEquipActionInfoList.Count; i++)
                {
                    ActionStateInfo _actionInfo = mEquipActionInfoList[i];
                    if (_actionInfo.ActionGroupID == removeInfoID)
                    {
                        mEquipActionInfoList.Remove(_actionInfo);
                        mEquipActionInfoList_HashSet.Remove(_actionInfo);
                        isFind = true;
                        break;
                    }
                }
            }

            if (isFind)
            {
                ClearActionGroupContext();
                ResetAllAnimatorAnim();
            }
            return isFind;
        }
        private void ResetAllAnimatorAnim()
        {
            //无动画机（如服务端逻辑单位）时无覆写机可重排，直接跳过，避免空引用
            if (!AnimValid) return;
            bool _isPlayer = CurUnit == EngineResourcesManager.Instance.Player;
            //if (_isPlayer) Debug.Log("<color=#ff0000>重新排序动画资产</color>");
            //Debug.LogError("重新排序动画资产");
            mCheckAnimClip.Clear();
            //重置整个动画覆写数据
            clipOverrides.ResetOverride();
            foreach (ActionStateInfo actionInfo in mEquipActionInfoList)
            {
                if (actionInfo.mActionOverrideClip is not null)
                {
                    //string _str = $"Action覆写列表:[{actionInfo.ActionGroupID}]";
                    foreach (ActionOverrideClip overrideClip in actionInfo.mActionOverrideClip)
                    {
                        string _stateName = overrideClip.StateClipName;
                        if (mCheckAnimClip.Add(_stateName))
                        {
                            if (actionInfo.TryGetClip(_stateName, out AnimationClip _clip))
                            {
                                clipOverrides[overrideClip.StateClipName] = _clip;
                                //_str += $"\n[{_stateName}]:{_clip.name} Clip:<color=#ffcc00>{overrideClip.ClipPath}</color>";
                            }
                            else
                            {
                                clipOverrides[overrideClip.StateClipName] = null;
                                //_str += $"\n[{_stateName}]:<color=#ff0000>Null</color>";
                            }
                        }
                    }
                    //Debug.LogError(_str);
                    //if (_isPlayer) Debug.Log(_str);
                }
            }
            OverrideController.ApplyOverrides(clipOverrides);
        }

        private void OnUpdateClipOverrides(List<ActionStateInfo> actionInfoList, Action<bool> loadComplete)
        {
            foreach (ActionStateInfo actionInfo in actionInfoList)
            {
                List<ActionOverrideClip> overrideClip = actionInfo.mActionOverrideClip;
                if (overrideClip.Count == 0)
                {
                    loadComplete(true);
                    //EngineDebug.LogWarning($"加载的动画数量<color=#ffcc00>为空</color>");
                    return;
                }
                //EngineDebug.LogWarning($"加载的动画数量：【{overrideClip.Count}】");

                //加载武器所有重置动画
                foreach (ActionOverrideClip item in overrideClip)
                {
                    if (!string.IsNullOrEmpty(item.ClipPath))
                    {
                        //当加载资产为空时不做处理
                        //string clipPath = item.ClipPath + ".anim";//动画片段加载路径
                        string overrifeName = item.StateClipName;
                        ActionStateInfo _selfActionInfo = actionInfo;

                        //需要加载的动画片段数量
                        loadClipConst++;

                        //加载成功
                        Action<AnimationClip> _loadComplete = (_clip) =>
                        {
                            loadClipConst--;
                            clipOverrides[overrifeName] = _clip;
                            //if (!mAnimationClipDic.TryAdd(overrifeName, _clip))
                            //    mAnimationClipDic[overrifeName] = _clip;
                            _selfActionInfo.SetClip(overrifeName, _clip);

                            //if(overrifeName == "clip_char_player_wp_sword_towhand_skill_lv1_002_1")
                            //{
                            //Debug.Log($"<color=#ff0000>加载成功：替换：【{overrifeName}】 到【{_clip.name}】</color>");
                            //}
                            //EngineDebug.LogWarning($"加载成功：替换：【{overrifeName}】 到【{_clip.name}】");
                            LoadActionInfoCheck(loadComplete);
                        };

                        //加载失败
                        Action _loadFailed = () =>
                        {
                            loadClipConst--;
                            clipOverrides[overrifeName] = null;
                            LoadActionInfoCheck(loadComplete);
                        };

                        // EngineDebug.Log($"需要加载的动画：【{clipPath}】");
                        //动画加载和替换
                        mLoadAnimationClips.Add(new SLoadAnimationClip(item.ClipPath, _loadComplete, _loadFailed));
                    }
                }
            }
        }
        //检查是否完成加载
        private void LoadActionInfoCheck(Action<bool> loadComplete)
        {
            if (loadClipConst <= 0)
            {
                //实际动画替换
                OverrideController.ApplyOverrides(clipOverrides);
                loadItemFlish = true;
                loadComplete?.Invoke(true);
            }
        }

#if UNITY_EDITOR
        //替换单独某个Action的动画片段 
        [Obsolete("仅编辑器调用，RunTime别用这函数")]
        public bool UpdateOverrideClip(string actionStateClipName, AnimationClip clip)
        {
            clipOverrides[actionStateClipName] = clip;
            OverrideController.ApplyOverrides(clipOverrides);
            return true;
        }
        [Obsolete("仅编辑器调用，RunTime别用这函数")]
        public void SetOverrideClipEnble(bool enable)
        {
            //重置所有动画
            isOverrideAnimator = OverrideController != null;
            if (isOverrideAnimator)
            {
                clipOverrides.SetOverrideEnble(enable);
                OverrideController.ApplyOverrides(clipOverrides);
            }
        }
#else
                public bool UpdateOverrideClip(string actionStateClipName, AnimationClip clip) { return false;}
                public void SetOverrideClipEnble(bool enable) { }
#endif
    }
}