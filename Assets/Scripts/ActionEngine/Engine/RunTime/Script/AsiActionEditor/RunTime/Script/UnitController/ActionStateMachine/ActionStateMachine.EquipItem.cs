namespace AsiActionEngine.RunTime
{

    public partial class ActionStateMachine
    {

        //        //#region delegate
        //        //public delegate List<ActionOverrideClip> DloadPropWarpCallback(PropWarp _warp);
        //        //#endregion
        //        //public DloadPropWarpCallback LoadPropWarpCallback = null;


        //        public Dictionary<int, SItemWarp> EquipProps => PropWarp_Type_Dic;

        //        //装备或者替换  武器和动画加载需要客户端完成
        //        private void OnEquipProp(PropWarp warp, ActionStateInfo _info, Action<ActionEngine_Prop> loadComplete, bool _autoUninstall,
        //            out List<SLoadAnimationClip> _loadClips, out Action<ActionEngine_Prop> _loadMode)
        //        {
        //            if (!isOverrideAnimator)
        //            {
        //                //创建OverrideAnimator
        //                if (CurAnimator?.runtimeAnimatorController is not null)
        //                {
        //                    OverrideController = new AnimatorOverrideController(CurAnimator.runtimeAnimatorController);
        //                    OverrideController.name = "ActionEngineOverride";
        //                }
        //                CurAnimator.runtimeAnimatorController = OverrideController;

        //                //初次装备武器时，实例化一次
        //                clipOverrides = new AnimationClipOverrides(OverrideController.overridesCount);
        //                OverrideController.GetOverrides(clipOverrides);

        //                // 优化：构建名称缓存，避免后续频繁的字符串创建和列表遍历
        //                clipOverrides.BuildCache();

        //                //初始化加载参数
        //                mLoadAnimationClips.Clear();
        //                loadClipConst = 0;
        //                loadItemFlish = true;
        //                loadItemModeFlish = true;

        //                //结束初始化
        //                isOverrideAnimator = true;
        //            }
        //            if (!loadItemFlish)
        //            {
        //#if UNITY_EDITOR
        //                EngineDebug.DisplayDialog("道具加载失败!!", "道具加载失败!!因为上一个道具未加载完成,无法立马加载新的道具", "OK");
        //#endif
        //                loadComplete?.Invoke(null);
        //                _loadClips = null;
        //                _loadMode = null;
        //                return;
        //            }

        //            //初始化加载参数
        //            mLoadAnimationClips.Clear();
        //            loadClipConst = 0;
        //            loadItemFlish = false;
        //            loadItemModeFlish = false;

        //            //加载完成后的回调
        //            Action<bool> mLoadComplete = isComplete =>
        //            {
        //                //Debug.Log("初始化武器Action");
        //                //初始化动画
        //                foreach (int actionID in warp.StartAction)
        //                {
        //                    ChangeAction(actionID, 0, 0);
        //                }

        //                //执行回调
        //                loadComplete(isComplete ? loadEngineProp : null);
        //            };

        //            bool _usePropTypes2 = false;
        //            bool _uninstall = false;
        //            _uninstall = CheckUninstall(warp, out _usePropTypes2);
        //            warp.UsePropTypes2 = _usePropTypes2;

        //            List<ActionOverrideClip> _overrideList = _info.mActionOverrideClip;
        //            //武器动画加载 New
        //            if (_autoUninstall)
        //            {
        //                if (_uninstall)
        //                {
        //                    //卸载槽位内占有的武器
        //                    List<GEnum> _listGEnum = _usePropTypes2 ? warp.PropTypes2 : warp.PropTypes;
        //                    foreach (GEnum item in _listGEnum)
        //                    {
        //                        //找到武器，卸载它
        //                        if (PropWarp_Cell_Dic.TryGetValue(item.GetKey, out SItemWarp sItemWarp))
        //                        {
        //                            //删除模型
        //                            EngineResourcesManager.Instance.RemoveComponent(sItemWarp._warp.ModelPath, sItemWarp._prop);

        //                            //删除武器数据
        //                            RemovePropWarp(sItemWarp._warp);

        //                            Debug.Log($"卸载武器: {(sItemWarp._prop ? sItemWarp._prop.gameObject.name : "空")}");
        //                        }
        //                    }
        //                    //清空Override列表
        //                    clipOverrides.ResetOverride();
        //                }

        //                //添加新装备
        //                SetPropWarp(warp, new SItemWarp(warp, null), _usePropTypes2);

        //                if (_uninstall)
        //                {
        //                    //确认卸载了武器 重新装备所有武器 更新整个Override列表
        //                    foreach (var VARIABLE in PropWarp_Type_Dic)
        //                    {
        //                        OnUpdateClipOverrides(VARIABLE.Value._warp, _overrideList, mLoadComplete);
        //                    }
        //                }
        //                else
        //                {
        //                    OnUpdateClipOverrides(warp, _overrideList, mLoadComplete);
        //                }
        //            }
        //            else
        //            {
        //                //直接装上
        //                SetPropWarp(warp, new SItemWarp(warp, null), _usePropTypes2);
        //                OnUpdateClipOverrides(warp, _overrideList, mLoadComplete);
        //            }

        //            //动画片段加载
        //            _loadClips = mLoadAnimationClips;

        //            //道具模型加载
        //            _loadMode = (_obj) =>
        //            {
        //                loadEngineProp = _obj;
        //                if (_obj is null)
        //                {
        //                    loadClipConst = 0;
        //                    loadItemFlish = true;
        //                    loadItemModeFlish = true;

        //                    loadComplete(null);
        //                    return;
        //                }
        //                _obj.mPropWarp = warp;

        //                //更新当前字典的Prop对象
        //                //mEquipItems[PropType] = new SItemWarp(warp, _obj);
        //                SetPropWarp(warp, new SItemWarp(warp, _obj), warp.UsePropTypes2);

        //                loadItemModeFlish = true;
        //                LoadItemCheck(mLoadComplete);
        //            };
        //        }

        //        private bool CheckUninstall(PropWarp propWarp, out bool uninstallType2)
        //        {
        //            bool _isFind = false;

        //#if UNITY_EDITOR
        //            if(propWarp.PropTypes is null)
        //            {
        //                EngineDebug.DisplayDialog("警告", $"严重错误,请在退出运行后重新保存Prop[{propWarp.ID}]", "关闭");
        //            }
        //#endif

        //            //遍历第一个槽位
        //            foreach (GEnum item in propWarp.PropTypes)
        //            {
        //                if (PropWarp_Cell_Dic.ContainsKey(item.GetKey))
        //                {
        //                    _isFind = true;
        //                    break;
        //                }
        //            }
        //            if (!_isFind)
        //            {
        //                //槽位空闲，可直接安装
        //                uninstallType2 = false;
        //                return false;
        //            }
        //            else
        //            {
        //                //当第二个槽位有配置时
        //                if (propWarp.PropTypes2.Count > 0)
        //                {
        //                    _isFind = false;
        //                    foreach (GEnum item in propWarp.PropTypes2)
        //                    {
        //                        if (PropWarp_Cell_Dic.ContainsKey(item.GetKey))
        //                        {
        //                            _isFind = true;
        //                            break;
        //                        }
        //                    }
        //                    if (!_isFind)
        //                    {
        //                        uninstallType2 = true;
        //                        return false;
        //                    }
        //                }
        //            }
        //            uninstallType2 = false;
        //            return true;
        //        }


        //        private void OnUnEquipProp(PropWarp warp, Action<bool>  loadComplete)
        //        {
        //            if (GetPropWarpToType(warp, out SItemWarp _sItemWarp))
        //            {
        //                //删除旧的武器
        //                EngineResourcesManager.Instance.RemoveComponent(_sItemWarp._warp.ModelPath, _sItemWarp._prop);
        //                //if (mEquipItems[warp]._prop is not null)
        //                //{
        //                //    EngineResourcesManager.Instance.RemoveComponent(mEquipItems[warp]._warp.ModelPath,
        //                //        mEquipItems[warp]._prop.transform);
        //                //}

        //                //先清空Override列表
        //                clipOverrides.ResetOverride();

        //                //卸载装备后重新添加
        //                RemovePropWarp(warp);
        //                //mEquipItems.Remove(warp);

        //                //重新装备所有武器
        //                foreach (KeyValuePair<int,SItemWarp> VARIABLE in PropWarp_Type_Dic)
        //                {
        //                    PropWarp _warp = VARIABLE.Value._warp;
        //                    if(_warp.EquaActionStateInfo is not null)
        //                    {
        //                        //if (_warp.EquaIndexID > 0 && _warp.OverrideClips.Count)
        //                            OnUpdateClipOverrides(VARIABLE.Value._warp, _warp.EquaActionStateInfo.mActionOverrideClip, loadComplete);
        //                    }
        //                    //EngineResourcesManager.Instance.AsyncLoadBinary
        //                    //ActionEnginLoadData.Instance.LoadInfo(ActionEnginLoadData.EInfoType.UnitAction, target => { });
        //                    //if (_warp.EquaIndexID>0&& _warp<)
        //                    //OnUpdateClipOverrides(VARIABLE.Value._warp, loadComplete);
        //                }
        //                // return true;
        //            }
        //            // return false;
        //        }
        //        private void OnUnAllEquipProp(Action<bool> loadComplete)
        //        {
        //            //重置所有动画片段
        //            clipOverrides.ResetOverride();

        //            //删除所有装备模型
        //            foreach (KeyValuePair<int, SItemWarp> VARIABLE in PropWarp_Type_Dic)
        //            {
        //                SItemWarp _sWarp = VARIABLE.Value; 
        //                EngineResourcesManager.Instance.RemoveComponent(_sWarp._warp.ModelPath,VARIABLE.Value._prop);
        //            }

        //            //清空装备列表
        //            PropWarp_Type_Dic.Clear();
        //            PropWarp_Cell_Dic.Clear();

        //            //触发动画替换
        //            OverrideController.ApplyOverrides(clipOverrides);

        //            loadComplete(true);
        //        }
        //        //更新AnimatorOverride列表
        //        private void OnUpdateClipOverrides(PropWarp warp, List<ActionOverrideClip> overrideClip, Action<bool> loadComplete)
        //        {
        //            if(overrideClip is null)
        //            {
        //                loadComplete(true);
        //                EngineDebug.LogWarning($"加载的动画数量<color=#ffcc00>为空</color>");
        //                return;
        //            }
        //            EngineDebug.Log($"加载的动画数量：【{overrideClip.Count}】");

        //            //if(LoadPropWarpCallback is null)
        //            //{

        //            //}
        //            //else
        //            //{
        //            //    LoadPropWarpCallback(warp);
        //            //}

        //            //加载武器所有重置动画
        //            foreach (var item in overrideClip)
        //            {
        //                if (!string.IsNullOrEmpty(item.ClipPath))
        //                {
        //                    //当加载资产为空时不做处理
        //                     string clipPath = item.ClipPath + ".anim";//动画片段加载路径
        //                    string overrifeName = item.StateClipName;

        //                    //需要加载的动画片段数量
        //                    loadClipConst++;

        //                    //加载成功
        //                    Action<AnimationClip> _loadComplete = (_clip) =>
        //                    {
        //                        loadClipConst--;
        //                        clipOverrides[overrifeName] = _clip;
        //                        // EngineDebug.Log($"加载成功：替换：【{overrifeName}】 到【{_clip.name}】");
        //                        LoadItemCheck(loadComplete);
        //                    };

        //                    //加载失败
        //                    Action _loadFailed = () =>
        //                    {
        //                        loadClipConst--;
        //                        clipOverrides[overrifeName] = null;
        //                        LoadItemCheck(loadComplete);
        //                    };
        //                    // EngineDebug.Log($"需要加载的动画：【{clipPath}】");
        //                    //动画加载和替换
        //                    mLoadAnimationClips.Add(new SLoadAnimationClip(item.ClipPath, _loadComplete, _loadFailed));
        //                }
        //            }
        //        }

        //        //检查是否完成加载
        //        private void LoadItemCheck(Action<bool> loadComplete)
        //        {
        //            if (loadItemModeFlish && loadClipConst <= 0)
        //            {
        //                //实际动画替换
        //                OverrideController.ApplyOverrides(clipOverrides);
        //                loadItemFlish = true;
        //                loadComplete?.Invoke(true);
        //            }
        //        }


        //#if UNITY_EDITOR
        //        //替换单独某个Action的动画片段 
        //        [Obsolete("仅编辑器调用，RunTime别用这函数")]
        //        public bool UpdateOverrideClip(string actionStateClipName, AnimationClip clip)
        //        {
        //            clipOverrides[actionStateClipName] = clip;
        //            OverrideController.ApplyOverrides(clipOverrides);
        //            return true;
        //        }
        //        [Obsolete("仅编辑器调用，RunTime别用这函数")]
        //        public void SetOverrideClipEnble(bool enable)
        //        {
        //            //重置所有动画
        //            isOverrideAnimator = OverrideController != null;
        //            if (isOverrideAnimator)
        //            {
        //                clipOverrides.SetOverrideEnble(enable);
        //                OverrideController.ApplyOverrides(clipOverrides);
        //            }
        //        }
        //#else
        //        public bool UpdateOverrideClip(string actionStateClipName, AnimationClip clip) { return false;}
        //        public void SetOverrideClipEnble(bool enable) { }
        //#endif

    }
}