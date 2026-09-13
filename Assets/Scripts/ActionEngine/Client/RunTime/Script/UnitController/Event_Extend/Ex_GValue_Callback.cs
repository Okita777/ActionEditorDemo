using AsiActionEngine.RunTime;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

namespace AsiTimeLine.RunTime
{
    public class Ex_GValue_Callback : StaticActionLogics
    {
        private Dictionary<(ushort, ushort, byte), Action<int, ActionEngine_Unit>> mCallback = new Dictionary<(ushort, ushort, byte), Action<int, ActionEngine_Unit>>();
        private Dictionary<(ushort, ushort, byte), Action<int, int, ActionEngine_Unit>> mCallback_SlotID = new Dictionary<(ushort, ushort, byte), Action<int, int, ActionEngine_Unit>>();


        public void AddAction(GEnum _key, Action<int, ActionEngine_Unit> _value)
        {
            if (!mCallback.TryAdd(_key.GetEnumKey, _value))
            {
                mCallback[_key.GetEnumKey] = _value;
            }
        }

        /// <summary>
        /// 输入子博配置的值和槽位ID
        /// </summary>
        /// <param name="_key"></param>
        /// <param name="_value">第一个Int是子博配置的值, 第二个Int是SlotID, 第三个是目标对象</param>
        public void AddAction(GEnum _key, Action<int, int, ActionEngine_Unit> _value)
        {
            if (!mCallback_SlotID.TryAdd(_key.GetEnumKey, _value))
            {
                mCallback_SlotID[_key.GetEnumKey] = _value;
            }
        }

        public void CallbackInvoke(ActionStatePart _Part, GEnum _key, int _id)
        {
            if (mCallback_SlotID.TryGetValue(_key.GetEnumKey, out Action<int, int, ActionEngine_Unit> _callback_SlotID))
            {
                if (_Part.ActionStateMachine.CurUnit is ActionEngine_Entity _Entity)
                {
                    var _stateMachine = _Part.ActionStateMachine;
                    //if (_stateMachine.TryGetCurActionStateInfo(_Part.CurrentActionState.AnimaLayer, out int _info)
                    //    && _Entity.TryGetSlotIDToActionListID(_info, out int _slotID))
                    int _info = _stateMachine.GetFirstActionGroupID;
                    if (_Entity.TryGetSlotIDToActionListID(_info, out int _slotID))
                    {
                        EngineDebug.GetGEnumNames(_Part, _key, out string GN,out string GVN, out string enumName);
                        EngineDebug.LogWarning($"[SkillGvalueMap] 回调2: Layer[{_Part.CurrentActionState.AnimaLayer}]  AGID[{_info}] 呼叫[<color=#ffcc00>{enumName}</color> ({_id})], " +
                            $"SlotID[<color=#ffcc00>{_slotID}</color>]\n{EngineDebug.DebugActionStatePart(_Part)}");
                        EngineDebug.Log($"[SkillGvalueMap] {_id}  AGID[{_info}]  SlotID{_slotID}  \n[{EngineDebug.DebugActionStatePart(_Part)}]");
                        _callback_SlotID.Invoke(_id, _slotID, _Part.ActionStateMachine.CurUnit);
                    }
                    else
                    {
#if UNITY_EDITOR
                        EngineDebug.GetGEnumNames(_Part, _key, out string _groupName, out string _gvName, out string _enumName);
                        EngineDebug.LogError($"执行GV回调事件时，ActionGroupID [{(_info.ToString())}] 未找到槽位映射 [<color=#ff0000>{_enumName}</color>]");
#endif
                    }
                }
                else
                {
#if UNITY_EDITOR
                    EngineDebug.GetGEnumNames(_Part, _key, out string _groupName2, out string _gvName2, out string _enumName2);
                    EngineDebug.LogError($"执行GV回调事件时，发现当前对象未挂载组件 [<color=#ff0000>ActionEngine_Entity</color>]");
#endif
                }
            }
            else
            {
                if (mCallback.TryGetValue(_key.GetEnumKey, out Action<int, ActionEngine_Unit> _callback))
                {
                    _callback.Invoke(_id, _Part.ActionStateMachine.CurUnit);
                }
                else
                {
#if UNITY_EDITOR    
                    if (Application.isPlaying)
                    {
                        //if (_Part.CurrentActionState.Name == "初始化层级") return;
                        EngineDebug.GetGEnumNames(_Part, _key, out string _groupName, out string _gvName, out string _enumName);
                        EngineDebug.LogError($"执行GV回调事件时，未注册该值 [<color=#ff0000>{_enumName}</color>]  " +
                            $"GroupIndex:[{_key.mValueGroupIndex}]  Index:[{_key.mValueIndex}]  [<color=#ffcc00>{_key.mSerValue}</color>]" +
                            $"\n{EngineDebug.DebugActionStatePart(_Part)}");
                    }
#endif
                }
            }
        }
    }
}