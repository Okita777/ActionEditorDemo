using System;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class ActionEvent : IProperty
    {
        [SerializeField] protected GInt mGTriggerTime = new GInt();
        [SerializeField] protected GInt mGDuration = new GInt();
        [SerializeField] protected int mTriggerTime;
        [SerializeField] protected int mDuration;
        [SerializeField] protected bool mForceInit = true;
        [SerializeField] protected bool mInheritable = false;
        [SerializeField] protected bool mEditorInheritable = false;//上下轨是否可继承
        [SerializeReference] protected GraphEvent_NoValue_Bool mCheck = new GraphEvent_NoValue_Bool(true);
        [SerializeField] protected byte mCheckType = 0;
        [SerializeField] protected bool mCutProtect = false;//裁切保护:单帧被跳入裁切掉时仍补触发一次
        [NonSerialized] public bool IsLateUpdate = false;
        [NonSerialized] public bool IsTem = false;
        [NonSerialized] public byte TemID = 0;
        [NonSerialized] public int editorcheck = 0;

        [SerializeReference] private IActionEventData mActionEventData;

        [EditorProperty("事件条件", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Bool Check
        {
            get
            {
#if UNITY_EDITOR
                if (mCheck is null)
                {
                    mCheck = new GraphEvent_NoValue_Bool(true);
                    EngineDebug.LogWarning("尝试获取事件Bool蓝图时，返回空!!");
                }
#endif
                return mCheck;
            }
            set { mCheck = value; }
        }

        public ActionEvent CloneTo(ActionEvent _action)//上一帧的
        {
            // ActionEvent _ActionEvent = new ActionEvent();
            // ActionEvent _ActionEvent = this;
            GTriggerTime = _action.GTriggerTime;
            GDuration = _action.GDuration;
            TriggerTime = _action.TriggerTime;
            Duration = _action.Duration;
            ForceInit = _action.ForceInit;
            Inheritable = _action.Inheritable;
            CheckType = _action.mCheckType;
            EditorInheritable = _action.EditorInheritable;
            CutProtect = _action.CutProtect;
            Check = _action.Check.Clone();

            //mActionEventData = mActionEventData.Clone(_actionEventData);//复制_actionEventData下的参数

            return this;
        }

        public ActionEvent Clone()
        {
            EngineResourcesManager.Instance.Active_ActionEventData = mActionEventData;

            ActionEvent _returnValue = new ActionEvent();
            _returnValue.GTriggerTime = GTriggerTime;
            _returnValue.GDuration = GDuration;
            _returnValue.TriggerTime = TriggerTime;
            _returnValue.Duration = Duration;
            _returnValue.ForceInit = ForceInit;
            _returnValue.Inheritable = Inheritable;
            _returnValue.CheckType = mCheckType;
            _returnValue.EditorInheritable = EditorInheritable;
            _returnValue.CutProtect = CutProtect;
            _returnValue.Check = Check.Clone();
            if (mActionEventData is not null)
                _returnValue.mActionEventData = mActionEventData.Clone(mActionEventData.Creact());
            return _returnValue;
        }

        #region Properties
        public GInt GTriggerTime
        {
            get { return mGTriggerTime; }
            set { mGTriggerTime = value; }
        }

        public GInt GDuration
        {
            get { return mGDuration; }
            set { mGDuration = value; }
        }
        public int TriggerTime
        {
            get { return mTriggerTime; }
            set { mTriggerTime = value; }
        }

        public int Duration
        {
            get { return mDuration; }
            set
            {
                //if (IsTem)
                //{
                //    EngineDebug.LogWarning("正常修改");
                //}
                //else
                //{
                //    EngineDebug.LogError("谁在动我!!");
                //}

                mDuration = value;
            }
        }

        public bool Inheritable
        {
            get { return mInheritable; }
            set { mInheritable = value; }
        }

        public bool EditorInheritable
        {
            get { return mEditorInheritable; }
            set { mEditorInheritable = value; }
        }

        public bool ForceInit
        {
            get { return mForceInit; }
            set { mForceInit = value; }
        }
        public byte CheckType
        {
            get { return mCheckType; }
            set { mCheckType = value; }
        }
        public bool CutProtect
        {
            get { return mCutProtect; }
            set { mCutProtect = value; }
        }
        public IActionEventData EventData
        {
            get { return mActionEventData; }
            set { mActionEventData = value; }
        }
        #endregion
    }
}
