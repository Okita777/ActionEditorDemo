using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    /// <summary>
    /// 单位行为
    /// </summary>
    [System.Serializable]
    public class ActionState
    {
        [SerializeField] protected int mID = -1;
        [SerializeField] protected int mTotalTime;//总时长
        [SerializeField] protected int mAnimLenth;//动画长度
        [SerializeField] protected int mAnimaLayer;
        [SerializeField] protected int mMixTime;
        [SerializeField] protected int mOffsetTime;
        [SerializeField] protected int mActionType;
        [SerializeField] protected int mDefaultActionID = -1;
        [SerializeField] protected string mDefaultAction = String.Empty;
        [SerializeField] protected string mName = string.Empty;
        [SerializeField] protected ActionEvent mAnimEvent;
        [SerializeField] protected List<ActionEvent> mEventList = new List<ActionEvent>();//常规Update下更新的事件
        [SerializeField] protected List<ActionEvent> mEventList_Anim = new List<ActionEvent>();//在动画更新之后更新的事件
        [SerializeField] protected List<ActionInterrupt> mInterruptList_BeHit = new List<ActionInterrupt>();//受击时触发的跳转
        [SerializeField] protected List<ActionInterrupt> mInterruptList_OnHit = new List<ActionInterrupt>();//命中时触发的跳转
        [SerializeField] protected List<ActionInterrupt> mInterruptList = new List<ActionInterrupt>();//常规跳转
        [SerializeField] protected List<ActionInterrupt> mInterruptList_E = new List<ActionInterrupt>();//结束时跳转
        [SerializeField] protected List<ActionInterruptGroup> mInterruptGroupList = new List<ActionInterruptGroup>();//跳转组
        //[SerializeField] protected int mRealyLayer = 0;
        #region Properties
        public int ID
        {
            get { return mID; }
            set { mID = value; }
        }

        public string Name
        {
            get { return mName; }
            set { mName = value; }
        }
        public int TotalTime
        {
            get { return mTotalTime; }
            set { mTotalTime = value; }
        }
        public int AnimLenth
        {
            get { return mAnimLenth; }
            set { mAnimLenth = value; }
        }
        public int AnimaLayer
        {
            get { return mAnimaLayer; }
            set { mAnimaLayer = value; }
        }
        //public int RealyLayerID
        //{
        //    get { return mRealyLayer; }
        //    set { mRealyLayer = value; }
        //}
        public int MixTime
        {
            get { return mMixTime; }
            set { mMixTime = value; }
        }
        public int OffsetTime
        {
            get { return mOffsetTime; }
            set { mOffsetTime = value; }
        }
        public int ActionLable
        {
            get { return mActionType; }
            set { mActionType = value; }
        }
        public int DefaultActionID
        {
            get { return mDefaultActionID; }
            set { mDefaultActionID = value; }
        }
        public string DefaultAction
        {
            get { return mDefaultAction; }
            set { mDefaultAction = value; }
        }
        public List<ActionEvent> EventList
        {
            get { return mEventList; }
            set { mEventList = value; }
        }
        public List<ActionEvent> EventList_Anim
        {
            get { return mEventList_Anim; }
            set { mEventList_Anim = value; }
        }
        public List<ActionInterrupt> InterruptList_BeHit
        {
            get { return mInterruptList_BeHit; }
            set { mInterruptList_BeHit = value; }
        }
        public List<ActionInterrupt> InterruptList_OnHit
        {
            get { return mInterruptList_OnHit; }
            set { mInterruptList_OnHit = value; }
        }
        public ActionEvent AnimEvent
        {
            get { return mAnimEvent; }
            set { mAnimEvent = value; }
        }
        public List<ActionInterrupt> InterruptList
        {
            get { return mInterruptList; }
            set { mInterruptList = value; }
        }

        public List<ActionInterruptGroup> InterruptGroupList
        {
            get { return mInterruptGroupList; }
            set { mInterruptGroupList = value; }
        }
        #endregion

        public ActionState Clone()
        {
            ActionState _returnValue = new ActionState();
            _returnValue.ID = ID;
            _returnValue.mTotalTime = mTotalTime;
            _returnValue.mAnimLenth = mAnimLenth;
            _returnValue.mAnimaLayer = mAnimaLayer;
            _returnValue.mMixTime = mMixTime;
            _returnValue.mOffsetTime = mOffsetTime;
            _returnValue.mActionType = mActionType;
            _returnValue.mDefaultActionID = mDefaultActionID;
            _returnValue.mDefaultAction = mDefaultAction;
            _returnValue.mName = mName;
            if (mAnimEvent is not null)
            {
                EngineResourcesManager.Instance.Active_ActionEvent = mAnimEvent;
                _returnValue.mAnimEvent = mAnimEvent.Clone();
            }
            _returnValue.mEventList = new List<ActionEvent>(mEventList.Count);
            _returnValue.mEventList_Anim = new List<ActionEvent>(mEventList_Anim.Count);
            foreach (ActionEvent item in mEventList)
            {
                EngineResourcesManager.Instance.Active_ActionEvent = item;
                _returnValue.mEventList.Add(item.Clone());
            }
            foreach (ActionEvent item in mEventList_Anim)
            {
                EngineResourcesManager.Instance.Active_ActionEvent = item;
                _returnValue.mEventList_Anim.Add(item.Clone());
            }
            _returnValue.mInterruptList_BeHit = mInterruptList_BeHit;
            _returnValue.mInterruptList_OnHit = mInterruptList_OnHit;
            _returnValue.mInterruptList = mInterruptList;
            _returnValue.mInterruptList_E = mInterruptList_E;
            _returnValue.mInterruptGroupList = mInterruptGroupList;
            return _returnValue;
        }
    }


}
