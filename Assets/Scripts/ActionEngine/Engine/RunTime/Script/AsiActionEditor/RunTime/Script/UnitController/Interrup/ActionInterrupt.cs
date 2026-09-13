using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class ActionInterrupt : IProperty
    {
        [NonSerialized] protected int mOffsetTriggerTime;//记录轨道组偏移的
        [NonSerialized] protected bool mHasLifecycleCondition;//本轨是否存在需要 Enter/Exit 运行态的条件，装载时算一次
        [NonSerialized] protected bool mIsConditionEntered;//条件生命周期是否已进入，保证 Enter/Exit 成对

        //[SerializeField] protected GInt mGTriggerTime = new GInt();
        //[SerializeField] protected GInt mGDuration = new GInt();
        //[SerializeField] protected GInt mGExecuteTime = new GInt();
        //[SerializeField] protected GInt mGCrossFadeTime = new GInt();
        //[SerializeField] protected GInt mGOffsetTime = new GInt();

        [SerializeField] protected int mActionID;
        [SerializeField] protected int mTriggerTime;
        [SerializeField] protected int mDuration;
        [SerializeField] protected int m_ExecuteTime;
        [SerializeField] protected bool m_JumpNow = false;//是否立即跳转
        [SerializeField] protected bool m_CutProtect = false;//裁切保护:单帧被跳入裁切掉时仍补判一次

        //[SerializeField] protected byte mAnimLayer;

        // [SerializeField] protected int m_SortID;
        [SerializeField] protected bool m_CheckAllCondition = true;
        [SerializeField] protected int m_CrossFadeTime;
        [SerializeField] protected int m_OffsetTime;
        [SerializeReference] protected List<IInterruptCondition> m_InterruptConditionList = new List<IInterruptCondition>();

        public int GetRealTriggerTime => mOffsetTriggerTime + mTriggerTime;
        #region Properties
        public int OffsetTriggerTime
        {
            get => mOffsetTriggerTime;
            set => mOffsetTriggerTime = value;
        }
        //public GInt GTriggerTime
        //{
        //    get { return mGTriggerTime; }
        //    set { mGTriggerTime = value; }
        //}

        //public GInt GDuration
        //{
        //    get { return mGDuration; }
        //    set { mGDuration = value; }
        //}
        //public GInt GExecuteTime
        //{
        //    get { return mGExecuteTime; }
        //    set { mGExecuteTime = value; }
        //}
        //public GInt GCrossFadeTime
        //{
        //    get { return mGCrossFadeTime; }
        //    set { mGCrossFadeTime = value; }
        //}
        //public GInt GOffsetTime
        //{
        //    get { return mGOffsetTime; }
        //    set { mGOffsetTime = value; }
        //}
        public int TriggerTime
        {
            get { return mTriggerTime; }
            set { mTriggerTime = value; }
        }

        public bool JumpNow
        {
            get { return m_JumpNow; }
            set { m_JumpNow = value; }
        }

        public bool CutProtect
        {
            get { return m_CutProtect; }
            set { m_CutProtect = value; }
        }

        public int Duration
        {
            get { return mDuration; }
            set { mDuration = value; }
        }

        public int ExecuteTime
        {
            get { return m_ExecuteTime; }
            set { m_ExecuteTime = value; }
        }

        public int ActionID
        {
            get { return mActionID; }
            set { mActionID = value; }
        }

        // public int SortID
        // {
        //     get { return m_SortID; }
        //     set { m_SortID = value; }
        // }

        public bool CheckAllCondition
        {
            get { return m_CheckAllCondition; }
            set { m_CheckAllCondition = value; }
        }

        public int CrossFadeTime
        {
            get { return m_CrossFadeTime; }
            set { m_CrossFadeTime = value; }
        }

        public int OffsetTime
        {
            get { return m_OffsetTime; }
            set { m_OffsetTime = value; }
        }

        public List<IInterruptCondition> InterruptConditionList
        {
            get { return m_InterruptConditionList; }
            set { m_InterruptConditionList = value; }
        }

        /// <summary>本轨是否需要驱动条件的 Enter/Exit。仅运行时副本(CreateRuntimeCopy)会置位。</summary>
        public bool HasLifecycleCondition => mHasLifecycleCondition;

        /// <summary>条件生命周期是否已进入，由 ActionStatePart 维护。</summary>
        public bool IsConditionEntered
        {
            get { return mIsConditionEntered; }
            set { mIsConditionEntered = value; }
        }

        #endregion

        public ActionInterrupt Clone()
        {
            ActionInterrupt _actionInterrupt = new ActionInterrupt();
            //_actionInterrupt.mGTriggerTime = mGTriggerTime;
            //_actionInterrupt.mGDuration = mGDuration;
            _actionInterrupt.mTriggerTime = mTriggerTime;
            _actionInterrupt.mDuration = mDuration;
            _actionInterrupt.m_ExecuteTime = m_ExecuteTime;
            _actionInterrupt.mActionID = mActionID;
            _actionInterrupt.JumpNow = m_JumpNow;
            _actionInterrupt.CutProtect = m_CutProtect;
            //_actionInterrupt.mAnimLayer = mAnimLayer;

            // _actionInterrupt.m_SortID = m_SortID;
            _actionInterrupt.m_CheckAllCondition = m_CheckAllCondition;
            _actionInterrupt.m_CrossFadeTime = m_CrossFadeTime;
            _actionInterrupt.m_OffsetTime = m_OffsetTime;
            _actionInterrupt.m_InterruptConditionList = m_InterruptConditionList;

            return _actionInterrupt;
        }

        public ActionInterrupt CreateRuntimeCopy(int offsetTriggerTime)
        {
            ActionInterrupt _actionInterrupt = Clone();
            _actionInterrupt.OffsetTriggerTime = offsetTriggerTime;
            _actionInterrupt.PrepareRuntimeConditions();
            return _actionInterrupt;
        }

        /// <summary>
        /// 装载期处理条件运行态：只有声明 NeedRuntimeState 的条件才 Clone 出独立实例，
        /// 其余条件继续与配置数据共享引用，不产生额外分配。
        /// </summary>
        private void PrepareRuntimeConditions()
        {
            mHasLifecycleCondition = false;
            mIsConditionEntered = false;
            if (m_InterruptConditionList is null) return;

            for (int i = 0; i < m_InterruptConditionList.Count; i++)
            {
                if (m_InterruptConditionList[i] is null || !m_InterruptConditionList[i].NeedRuntimeState) continue;
                mHasLifecycleCondition = true;
                break;
            }

            if (!mHasLifecycleCondition) return;

            List<IInterruptCondition> _runtimeConditions = new List<IInterruptCondition>(m_InterruptConditionList.Count);
            for (int i = 0; i < m_InterruptConditionList.Count; i++)
            {
                IInterruptCondition _condition = m_InterruptConditionList[i];
                _runtimeConditions.Add(_condition is not null && _condition.NeedRuntimeState ? _condition.Clone() : _condition);
            }
            m_InterruptConditionList = _runtimeConditions;
        }
    }
}