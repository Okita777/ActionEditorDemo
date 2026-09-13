using System;
using System.Collections.Generic;
using System.Linq;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public partial class ActionEngineUtility
    {
        public static Action<Transform> InteractObjChange;
        /// <summary>
        /// 技能装备  在加载时替换技能按键  加载完毕后执行回调
        /// </summary>
        /// <param name="actionID">ActionInfoID</param>
        /// <param name="_keyName">目标按键名称</param>
        /// <param name="_loadCallback">加载完成的回调,加载失败也会回调并返回false</param>
        public static void ActionInfo_SetCheckKey(int actionID, string _keyName, Action<ActionStateInfo> _loadCallback)
            => OnActionInfo_SetCheckKey(actionID, _keyName, _loadCallback);
        public static ActionStateInfo ActionInfo_SetCheckKey(int actionID, string _keyName, ActionStateInfo _actionInfo)
            => OnActionInfo_SetCheckKey(actionID, _keyName, _actionInfo);

        //public static void E
        //public static Equality
        #region Funtion
        //按键Action
        private static readonly HashSet<string> mReplaceKeyHashSet = new HashSet<string>()
        {
            "Skill_1",
            "Skill_2",
            "Skill_3",
            "Skill_4",
            "Skill_5",
            "Skill_6",
            "Skill_7",
            "Skill_8",
            "Skill_9",
            "Attack",
            "SAttack",
        };
        private static readonly List<string> mReplaceKeyList = new List<string>()
        {
            "Skill_1",
            "Skill_2",
            "Skill_3",
            "Skill_4",
            "Skill_5",
            "Skill_6",
            "Skill_7",
            "Skill_8",
            "Skill_9",
            "Attack",
            "SAttack",
        };
        private static readonly Dictionary<string, int> mReplaceActionDic = new Dictionary<string, int>(){
            { mReplaceKeyList[0],0},
            { mReplaceKeyList[1],1},
            { mReplaceKeyList[2],2},
            { mReplaceKeyList[3],3},
            { mReplaceKeyList[4],4},
            { mReplaceKeyList[5],5},
            { mReplaceKeyList[6],6},
            { mReplaceKeyList[7],7},
            { mReplaceKeyList[8],8},
            { mReplaceKeyList[9],9},
            { mReplaceKeyList[10],10},
        };

        //动画Clip
        private static readonly List<string> mClipList = new List<string>()
        {
            "null_skill_001",
            "null_skill_002",
            "null_skill_003",
            "null_skill_004",
            "null_skill_005",
            "null_skill_006",
            "null_skill_007",
            "null_skill_008",
            "null_skill_009",
            "null_attack_001",
            "null_special_001",
        };

        //ActionID
        private static readonly int[] mReplaceActionID = new[] {
            210611,
            210612,
            210613,
            210614,
            210615,
            210616,
            210617,
            210618,
            210619,
            210603,
            210604,
        };
        //private static readonly int[] mReplaceActionID2 = new[] {
        //    100002010,
        //    100002020,
        //    100002030,
        //    100002040,
        //    100002050,
        //    100002060,
        //    100002070,
        //    100002080,
        //    100002090,
        //    100002100,
        //    100002110,
        //};
        //private static readonly int[] mReplaceActionID3 = new[] {
        //    210611,
        //    210612,
        //    210613,
        //    210614,
        //    210615,
        //    210616,
        //    210617,
        //    210618,
        //    210619,
        //    210603,
        //    210604,
        //};


        private static void OnActionInfo_SetCheckKey(int actionID, string _actionName, Action<ActionStateInfo> _loadCallback)
        {
            ActionEnginLoadData.Instance.LoadInfo(EInfoType.UnitAction, _action =>
            {
                if (_action is ActionStateInfo _actionInfo)
                {
                    //Debug.Log($"<color=#ffcc00>修改Action[{actionID}]的按键为[{_actionName}]</color>");
                    _loadCallback(OnActionInfo_SetCheckKey(actionID, _actionName, _actionInfo));
                }
                else
                {
                    //Debug.Log($"<color=#ff0000>修改Action[{actionID}]的按键失败</color>");
                    _loadCallback(null);
                }
            }, actionID);
        }
        private static ActionStateInfo OnActionInfo_SetCheckKey(int actionID, string _actionName, ActionStateInfo _actionInfo)
        {
            bool _isFind = mReplaceActionDic.TryGetValue(_actionName, out int _index);

            //Debug.LogError($"尝试替换的Action组[{_actionInfo.ActionGroupID}]:{_actionInfo.ActionGroupName}");
            //替换按键行为
            foreach (ActionState _actionState in _actionInfo.mActionState)
            {
                //foreach (ActionInterrupt _actionInterrupt in _actionState.InterruptList)
                //{
                //    foreach (IInterruptCondition _condition in _actionInterrupt.InterruptConditionList)
                //    {
                //        //找到所有的按键检测事件
                //        if (_condition.InterruptType == -(int)EInterruptTypeInternal.EIT_CheckInput)
                //        {
                //            CheckCostomKey _checkCostomKey = _condition as CheckCostomKey;
                //            if (mReplaceKeyHashSet.Contains(_checkCostomKey.CheckKeyName))
                //            {
                //                _checkCostomKey.CheckKeyName = _actionName;
                //                //Debug.LogWarning($"<color=#ffcc00>修改[{_checkCostomKey.CheckKeyName}]按键为[{_actionName}]</color>");
                //            }
                //        }
                //    }
                //}

                //替换State名称
                if (_actionState.AnimEvent is not null && _actionState.AnimEvent.EventData is Event_PlayAnim _anim)
                {
                    //Debug.Log($"<color=#ff0000>尝试变更State名称[{_anim.AnimName}]</color>");
                    foreach (string item in mReplaceKeyList)
                    {
                        if (_anim.AnimName.Contains(item))
                        {
                            string _newName = _anim.AnimName.Replace(item, _actionName);
                            //Debug.Log($"动画(State): [<color=#ffcc00>{_anim.AnimName}</color>] 已替换为 [{_newName}]");
                            _anim.AnimName = _newName;
                            break;
                        }
                    }
                }
            }

            //替换动画资产State名称
            for (int i = 0; i < _actionInfo.mActionOverrideClip.Count; i++)
            {
                ActionOverrideClip _overrideClip = _actionInfo.mActionOverrideClip[i];

                //todo:在正式场景内临时遍历所有对象
                foreach (string nowName in mClipList)
                {
                    if (_overrideClip.StateClipName.Contains(nowName))
                    {
                        string _newName = _overrideClip.StateClipName.Replace(nowName, mClipList[_index]);
                        //Debug.Log($"动画(Clip): [<color=#ffcc00>{_overrideClip.StateClipName}</color>] 已替换为 [{_newName}]: {_overrideClip.ClipPath}");

                        _overrideClip.StateClipName = _newName;
                        break;
                    }
                }
                _actionInfo.mActionOverrideClip[i] = _overrideClip;
            }

            ////替换Action按键行为
            //foreach (ActionState _actionState in _actionInfo.mActionState)
            //{
            //    if (mReplaceActionID.Contains(_actionState.ID))
            //    {
            //        if (_isFind)
            //        {
            //            //Debug.LogError($"<color=#ffcc00>修改Action[{_actionState.ID}]的按键为[{mReplaceActionID[_index]}]</color>");
            //            _actionState.ID = mReplaceActionID[_index];
            //        }
            //        break;
            //    }
            //}

            return _actionInfo;
        }
        #endregion

    }
}