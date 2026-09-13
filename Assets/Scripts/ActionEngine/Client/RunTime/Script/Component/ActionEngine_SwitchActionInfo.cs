using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class ActionEngine_SwitchActionInfo : MonoBehaviour
    {
        [HideInInspector] public List<int> ActionInfoID = new List<int>();
        [HideInInspector] public int mSwitchID = -1, mSwitchID_Last = -1;
        [Header("切换至上一个模组")] public KeyCode Switch_Up = KeyCode.Z;
        [Header("切换至下一个模组")] public KeyCode Switch_Dwon = KeyCode.C;
        [Header("卸载当前模组")] public KeyCode Switch_UnEqu = KeyCode.X;


        private ActionEngine_Unit mPlayer;
        private bool mInit = false;
        private void Start()
        {
            ActionEngineManager_Input.Instance.WaitPlayerLoad((ActionEngine_Unit _unit) =>
            {
                mPlayer = _unit;
                mInit = true;
            });
        }
        // Update is called once per frame
        void Update()
        {
            if (ActionInfoID.Count < 1 || !mInit) return;

            if (Input.GetKeyDown(Switch_Up))
            {
                mSwitchID--;
                if (mSwitchID < 0) mSwitchID = ActionInfoID.Count - 1;
            }
            if (Input.GetKeyDown(Switch_Dwon))
            {
                mSwitchID++;
                if (mSwitchID >= ActionInfoID.Count) mSwitchID = 0;
            }
            if (Input.GetKeyDown(Switch_UnEqu))
            {
                if (mSwitchID < 0) return;
                if (mPlayer.ActionStateMachine.UnEquipActionInfo(ActionInfoID[mSwitchID]))
                {
                    mSwitchID = -1;
                    mSwitchID_Last = -1;
                }
                return;
            }

            if (mSwitchID_Last != mSwitchID)
            {
                //尝试卸载上一个ActionInfo
                if (mSwitchID_Last > -1 && !mPlayer.ActionStateMachine.UnEquipActionInfo(ActionInfoID[mSwitchID_Last]))
                {
                    //EngineDebug.LogError($"卸载失败 [<color=#ffcc00>{ActionInfoID[mSwitchID_Last]}</color>]");
                }

                //尝试装备最新ActionInfo
                ActionEnginLoadData.Instance.LoadInfo(EInfoType.UnitAction, target =>
                {
                    if (target is ActionStateInfo _info)
                    {
                        bool _onEqu = mPlayer.ActionStateMachine.EquipActionInfo(_info, _complet => { }, out List<ActionStateMachine.SLoadAnimationClip> _loadInfo);
                        if (_onEqu)
                        {
                            //动画加载实现
                            if (_loadInfo.Count > 0)
                            {
                                foreach (ActionStateMachine.SLoadAnimationClip clip in _loadInfo)
                                {
                                    // EngineDebug.LogWarning($"动画加载尝试！！ 路径：【<color=#ffcc00>{clip._loadPath}</color>】");
                                    EngineResourcesManager.Instance.AsyncLoadObj(clip._loadPath, (_obj) =>
                                    {
                                        if (_obj is AnimationClip _clip)
                                        {
                                            //成功加载
                                            // EngineDebug.LogWarning($"动画加载成功！！ 路径：【<color=#ffcc00>{clip._loadPath}</color>】");
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
                }, ActionInfoID[mSwitchID]);
                mSwitchID_Last = mSwitchID;
            }

            if (Input.GetKeyDown(Switch_UnEqu))
            {
            }
        }
    }
}
