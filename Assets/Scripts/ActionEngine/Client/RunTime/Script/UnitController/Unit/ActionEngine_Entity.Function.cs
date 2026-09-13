using AsiActionEngine.RunTime;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace AsiTimeLine.RunTime
{
    public partial class ActionEngine_Entity : ActionEngine_Unit
    {
        //todo: 重新实现武器模型和动画组的加载

        // private List<ActionStateMachine.SLoadAnimationClip> m_LoadClips = new List<ActionStateMachine.SLoadAnimationClip>();
        // private Action<Prop> m_LoadMode;

        //private void OnEquipProp(PropWarp warp, Action<ActionEngine_Prop> _loadComplete)
        //{
        //    ActionEnginLoadData.Instance.LoadInfo(EInfoType.UnitAction, target =>
        //    {
        //        if (target is ActionStateInfo _info)
        //        {
        //            warp.EquaActionStateInfo = _info;
        //            // EngineDebug.LogWarning("加载出的东西是空的啊啊啊啊");

        //            Debug.LogWarning($"尝试加载武器: {(_info.mActionOverrideClip is null ? "<color=#ff0000>空</color>" : $"[<color=#ffcc00>{_info.mActionOverrideClip.Count}</color>]")}  PropID:[{warp.ID}]  ActionID:[{_info.ActionGroupID}]");
        //            //客户端实现异步加载
        //            ActionStateMachine.EquipProp(warp, _info, _loadComplete, true, out List<ActionStateMachine.SLoadAnimationClip> m_LoadClips, out Action<ActionEngine_Prop> m_LoadMode);

        //            //模型加载实现
        //            EngineResourcesManager.Instance.CreactObjToComponent<ActionEngine_Prop>(warp.ModelPath, component =>
        //            {
        //                if (component is ActionEngine_Prop _gameObject)
        //                {
        //                    m_LoadMode(_gameObject);
        //                }
        //                else if (component is null)
        //                {
        //                    // EngineDebug.LogWarning("加载出的东西是空的啊啊啊啊");
        //                    m_LoadMode(null);
        //                }
        //                else
        //                {
        //                    m_LoadMode(null);
        //                }
        //            }, 1, 20, -1, (int)EObjPoolParent.Prop);

        //            //动画加载实现
        //            if (m_LoadClips.Count > 0)
        //            {
        //                foreach (ActionStateMachine.SLoadAnimationClip clip in m_LoadClips)
        //                {
        //                    // EngineDebug.LogWarning($"动画加载尝试！！ 路径：【<color=#ffcc00>{clip._loadPath}</color>】");
        //                    EngineResourcesManager.Instance.AsyncLoadObj(clip._loadPath, (_obj) =>
        //                    {
        //                        if (_obj is AnimationClip _clip)
        //                        {
        //                            //成功加载
        //                            // EngineDebug.LogWarning($"动画加载成功！！ 路径：【<color=#ffcc00>{clip._loadPath}</color>】");
        //                            clip.LoadCompleted(_clip);
        //                        }
        //                        else
        //                        {
        //                            //加载失败  并提示错误信息
        //                            clip.LoadFailed($"动画加载失败！！ 路径：【<color=#ffcc00>{clip._loadPath}</color>】");
        //                        }
        //                    });
        //                }
        //            }
        //            else
        //            {
        //                EngineDebug.LogWarning($"【<color=#ffcc00>需要加载的动画数量为零</color>】");
        //            }
        //        }
        //    }, warp.DefaultAction);
        //}

        //private void OnUnEquipProp(PropWarp warp, Action<bool> _loadComplete)
        //{
        //    ActionStateMachine.UnEquipProp(warp, _loadComplete);
        //}

        //private void OnUnAllEquipProp(Action<bool> _loadComplete)
        //{
        //    ActionStateMachine.UnAllEquipProp(_loadComplete);
        //}

        private void OnSetPropToDefaultPoint(ActionEngine_Prop _prop)
        {
            if (ActionStateMachine.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig)))
            {
                if (_config.HelpPointDic.TryGetValue((ECharacteLimbType)_prop.mPropWarp.DefaultPoint,
                        out Transform _target))
                {
                    _prop.transform.SetParent(_target);
                    EVector3 pos = _prop.mPropWarp.OffsetPos;
                    EVector3 rot = _prop.mPropWarp.OffsetRot;
                    _prop.transform.SetLocalPositionAndRotation(pos.GetValue(), Quaternion.Euler(rot.GetValue()));
                    _prop.transform.localScale = Vector3.one;
                }
                else
                {
#if UNITY_EDITOR
                    EngineDebug.LogError("武器挂载失败，挂载对象的【CharacterConfig】组件上没有配置 " +
                                         $"{(ECharacteLimbType)_prop.mPropWarp.DefaultPoint} 挂点");
#endif
                }
            }
            else
            {
#if UNITY_EDITOR
                EngineDebug.LogError("武器挂载失败，挂载对象没有配置【CharacterConfig】组件");
#endif
            }
        }
    }
}
