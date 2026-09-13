using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class Ex_Custom_Callback : StaticActionLogics
    {
        public enum ECallBackType
        {
            Single,
            Multiple
        }

        /// 键名（如 Skill_1 / Attack）→ 主界面槽索引 5–9；未命中返回 -1。由 调用方传 静态构造注册，避免引擎程序集引用游戏层。
        public static Func<string, int> TryResolveSkillReleaseSlotIndex;

        private readonly Dictionary<int, int> mDic_SlotID = new Dictionary<int, int>();
        private readonly Dictionary<int, Action<int, ECallBackType>> mDic_SkillCallBack = new Dictionary<int, Action<int, ECallBackType>>();
        private readonly Dictionary<int, Action<int, ECallBackType>> mDic_SkillCallBackBySlot = new Dictionary<int, Action<int, ECallBackType>>();

        private static float s_LastFallbackLogTime = -999f;
        private static float s_LastNoMatchLogTime = -999f;
        private const float FallbackLogInterval = 0.75f;
        private const float NoMatchLogInterval = 2f;

        public void EquipSkill(int _slotID, int _ActionGroupID, Action<int, ECallBackType> _callback)
        {
            mDic_SlotID[_ActionGroupID] = _slotID;
            mDic_SkillCallBack[_ActionGroupID] = _callback;
            mDic_SkillCallBackBySlot[_slotID] = _callback;
        }

        public void UnEquipSkill(int _ActionGroupID)
        {
            if (mDic_SlotID.TryGetValue(_ActionGroupID, out int slot))
                mDic_SkillCallBackBySlot.Remove(slot);
            mDic_SlotID.Remove(_ActionGroupID);
            mDic_SkillCallBack.Remove(_ActionGroupID);
        }

        private static string GetFirstSkillInputKeyHint(ActionStatePart _part)
        {
            string nond = MotionEngineConst.NondKeyName;
            if (!string.IsNullOrEmpty(_part.NowInputDownKey) && _part.NowInputDownKey != nond)
                return _part.NowInputDownKey;
            if (!string.IsNullOrEmpty(_part.NowInputClickKey) && _part.NowInputClickKey != nond)
                return _part.NowInputClickKey;
            if (!string.IsNullOrEmpty(_part.NowInputHoldKey) && _part.NowInputHoldKey != nond)
                return _part.NowInputHoldKey;
            return null;
        }

        public void RunEven_SkillCallBack(ActionStatePart _part, ECallBackType _type)
        {
            if (_part.IsTem)
            {
                EngineDebug.LogError("技能释放回调执行错误!!!  不能在技能上执行!!");
                return;
            }

            var stateMachine = _part.ActionStateMachine;
            int curActionGroup = -1;
            bool hasLayerInfo = stateMachine.TryGetCurActionStateInfo(_part.CurrentActionState.AnimaLayer, out curActionGroup);

            if (hasLayerInfo && mDic_SkillCallBack.TryGetValue(curActionGroup, out var cbByGroup))
            {
                cbByGroup(mDic_SlotID[curActionGroup], _type);
                return;
            }

            string keyHint = GetFirstSkillInputKeyHint(_part);
            int slotByKey = -1;
            if (!string.IsNullOrEmpty(keyHint) && TryResolveSkillReleaseSlotIndex != null)
                slotByKey = TryResolveSkillReleaseSlotIndex(keyHint);
            if (slotByKey > 0 &&
                mDic_SkillCallBackBySlot.TryGetValue(slotByKey, out var cbBySlot))
            {
                float t = Time.unscaledTime;
                if (t - s_LastFallbackLogTime >= FallbackLogInterval)
                {
                    s_LastFallbackLogTime = t;
                    EngineDebug.Log($"[SkillReleaseCallback] slot fallback key={keyHint} slot={slotByKey} group={curActionGroup} layerOk={hasLayerInfo} type={_type}");
                }
                cbBySlot(slotByKey, _type);
                return;
            }

            float tn = Time.unscaledTime;
            if (tn - s_LastNoMatchLogTime >= NoMatchLogInterval)
            {
                s_LastNoMatchLogTime = tn;
                EngineDebug.LogWarning($"[SkillReleaseCallback] no match group={curActionGroup} layerOk={hasLayerInfo} key={keyHint ?? "-"} type={_type}");
            }
        }
    }
}
