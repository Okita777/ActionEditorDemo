using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;
#if Addressables
#endif

namespace AsiTimeLine.RunTime
{
    public class ActionEngine_SwitchWeapon : MonoBehaviour
    {
        [HideInInspector] public List<int> mWeaponeList = new();
        public KeyCode mSwitchToUp = KeyCode.Z;
        public KeyCode mSwitchToDwon = KeyCode.X;
        public KeyCode mSwitchToNone = KeyCode.C;

        private int mNowWeaponIndex = 0;
        private int mNowWeaponIndex_Last = 0;
        private bool mEquipComplete_Model = true, mEquipComplete_Action = true;
        private Dictionary<int, PropWarp> actionEngine_Props = new();
        //private ActionEngine_Unit mPlayer => ActionEngineManager_Input.Instance.Player;
        private bool isInit = false;
        private int loadConst = 0;

        private bool isNone = false;
        public void Start()
        {
            InitLoadAllWeapon();
        }

        public void Update()
        {
            if (Input.GetKeyDown(mSwitchToUp))
            {
                if (isNone)
                    SwitchID(mWeaponeList[mNowWeaponIndex]);
                else
                    ChangeUp();
                isNone = false;
            }
            else if (Input.GetKeyDown(mSwitchToDwon))
            {
                if (isNone)
                    SwitchID(mWeaponeList[mNowWeaponIndex]);
                else
                    ChangeDwon();
                isNone = false;
            }
            else if (Input.GetKeyDown(mSwitchToNone) && !isNone)
            {
                ActionEngineManager_Input.Instance.WaitPlayerLoad(_player =>
                {
                    if (_player is ActionEngine_Entity _Entity)
                    {
                        //卸载武器
                        if (actionEngine_Props.TryGetValue(mNowWeaponIndex_Last, out PropWarp _warpUnEquip))
                        {
                            //卸载武器
                            _Entity.UnEquipProp(mNowWeaponIndex_Last, _warpUnEquip.PropType.mSerValue);

                            //卸载动作模组
                            _Entity.UnEquipAction(_warpUnEquip.DefaultAction);
                        }
                    }
                    isNone = true;
                });
                mNowWeaponIndex_Last = -1;
            }
        }

        private void ChangeUp()
        {
            if (!mEquipComplete_Model || !mEquipComplete_Action)
            {
                EngineDebug.DisplayDialog("警告", "武器切换失败，上一把还未加载完成", "我知道了");
                return;
            }

            mNowWeaponIndex--;
            if (mNowWeaponIndex < 0)
            {
                mNowWeaponIndex = mWeaponeList.Count - 1;
            }
            SwitchID(mWeaponeList[mNowWeaponIndex]);
        }

        private void ChangeDwon()
        {
            if (!mEquipComplete_Model || !mEquipComplete_Action)
            {
                EngineDebug.DisplayDialog("警告", "武器切换失败，上一把还未加载完成", "我知道了");
                return;
            }

            mNowWeaponIndex++;
            if (mNowWeaponIndex >= mWeaponeList.Count)
            {
                mNowWeaponIndex = 0;
            }
            SwitchID(mWeaponeList[mNowWeaponIndex]);
        }

        private void InitLoadAllWeapon()
        {
            isInit = false;

            loadConst = mWeaponeList.Count;
            Action _loadComplete = () =>
            {
                loadConst--;
                if (loadConst == 0)
                {
                    isInit = true;
                    mNowWeaponIndex = 0;
                    SwitchID(mWeaponeList[mNowWeaponIndex]);
                }
            };

            foreach (int item in mWeaponeList)
            {
                ActionEngineManager_Unit.Instance.GetPropWarp(item, _awrp =>
                {
                    if (!actionEngine_Props.TryAdd(item, _awrp))
                    {
                        EngineDebug.LogError($"加载了相同的武器！！！[{item}]");
                    }
                    _loadComplete();
                });
            }
        }
        private void SwitchID(int _id)
        {
            if (!isInit || !mEquipComplete_Model || !mEquipComplete_Action) return;
            mEquipComplete_Model = false;
            mEquipComplete_Action = false;

            ActionEngineManager_Input.Instance.WaitPlayerLoad(mPlayer =>
            {
                if (mPlayer is ActionEngine_Entity _Entity)
                {
                    //卸载武器
                    if (actionEngine_Props.TryGetValue(mNowWeaponIndex_Last, out PropWarp _warpUnEquip))
                    {
                        //卸载武器
                        _Entity.UnEquipProp(mNowWeaponIndex_Last, _warpUnEquip.PropType.mSerValue);

                        //卸载动作模组
                        _Entity.UnEquipAction(_warpUnEquip.DefaultAction);
                    }

                    //装备武器
                    if (actionEngine_Props.TryGetValue(_id, out PropWarp _warp))
                    {
                        //装备武器
                        _Entity.EquipProp(_id, _warp.PropType.mSerValue, (ECharacteLimbType)_warp.DefaultPoint, _complet =>
                        {
                            Debug.Log("成功装备到挂点");
                            mEquipComplete_Model = true;
                        });

                        //装备动作模组
                        _Entity.EquipActionInfo(_warp.DefaultAction, _loadComplet =>
                        {
                            Debug.Log("成功装备模组");
                            mEquipComplete_Action = true;
                        });
                        mNowWeaponIndex_Last = _id;
                    }
                    else
                    {
                        EngineDebug.LogError("武器加载失败 ID:[<color=#ffcc00>_id</color>]");
                        mEquipComplete_Model = true;
                        mEquipComplete_Action = true;
                    }
                }
                else
                {
                    EngineDebug.LogError("无法切换武器，当前没有玩家或者玩家不为[<color=#ffcc00>ActionEngine_Entity</color>]");
                    mEquipComplete_Model = true;
                    mEquipComplete_Action = true;
                }
            });
        }
    }
}