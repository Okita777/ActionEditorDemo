using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class PropWarp : IProperty
    {
        //[System.Serializable]
        //public struct ActionOverrideClip
        //{
        //    public string StateClipName;//AnimatorStateName
        //    public string ClipPath;
        //    public ActionOverrideClip(string _stateClipName, string clipPath)
        //    {
        //        StateClipName = _stateClipName;
        //        ClipPath = clipPath;
        //    }
        //}

        //[System.Serializable]
        //public struct SActionListKey
        //{
        //    public GEnum Key;
        //    public int ActionGroup;
        //    public SActionListKey(GEnum _Key, int _ActionGroup)
        //    {
        //        Key = (GEnum)_Key.Clone();
        //        ActionGroup = _ActionGroup;
        //    }
        //}

        [System.Serializable]
        public struct SActionInfoDic
        {
            public (ushort, ushort, byte) Key;
            public int InfoID;
            public SActionInfoDic((ushort, ushort, byte) _Key, int _InfoID)
            {
                Key = _Key;
                InfoID = _InfoID;
            }
        }

        public int HashCode()
        {
            return this.GetHashCode();
        }

        [SerializeField] protected int m_DefaultAction = 0;
        [SerializeField] protected int m_DefaultPoint;
        [SerializeField] protected int m_ID;
        [SerializeField] protected string m_ModelPath;//模型路径
        [SerializeField] protected EVector3 m_OffsetPos;
        [SerializeField] protected EVector3 m_OffsetRot;
        [SerializeField] protected GEnum m_PropType = new GEnum();
        [SerializeField] protected GValue_Setting m_GValueSetting = new GValue_Setting();
        [SerializeField] protected List<int> m_StartAction = new List<int>();
        //[SerializeField] protected List<GEnum> m_PropTypes = new List<GEnum>() { new GEnum() };
        //[SerializeField] protected List<GEnum> m_PropTypes2 = new List<GEnum>() { };
        ////[SerializeField] protected List<ActionState> m_Actions = new List<ActionState>();
        //[SerializeField] protected List<SActionInfoDic> m_ActionInfoList = new List<SActionInfoDic>();
        //[SerializeField] protected List<ActionOverrideClip> mOverrideClips = new();

        [NonSerialized] protected Dictionary<int, ActionState> m_ActionDict = null;
        [NonSerialized] protected Dictionary<string, int> m_ActionIDDict = null;

        [NonSerialized] protected bool InitDic = false;
        [NonSerialized] public bool UsePropTypes2 = false;
        [NonSerialized] public ActionStateInfo EquaActionStateInfo_Data = null;//当前装备的Clip
        public ActionStateInfo EquaActionStateInfo
        {
            get { return EquaActionStateInfo_Data; }
            set
            {
                InitDic = false;
                EquaActionStateInfo_Data = value;
            }
        }

        public bool GetAction(int _actionID, out ActionState _action)
        {
            InitDicData();
            return m_ActionDict.TryGetValue(_actionID, out _action);
        }
        public bool GetActionID(string _name, out int _actionID)
        {
            InitDicData();
            return m_ActionIDDict.TryGetValue(_name, out _actionID);
        }
        private void InitDicData()
        {
            if (!InitDic)
            {
                m_ActionDict = new Dictionary<int, ActionState>();
                m_ActionIDDict = new Dictionary<string, int>();
                foreach (ActionState _actionState in EquaActionStateInfo.mActionState)
                {
                    m_ActionDict.Add(_actionState.ID, _actionState);
                    m_ActionIDDict.Add(_actionState.Name, _actionState.ID);
                }
                InitDic = true;
            }
        }

        #region MyRegion
        public int ID
        {
            get { return m_ID; }
            set { m_ID = value; }
        }
        //public List<ActionState> Actions
        //{
        //    get { return m_Actions; }
        //    set { m_Actions = value; }
        //}
        // [EditorProperty("道具预制体", EditorPropertyType.EEPT_GameObject)]
        public string ModelPath
        {
            get { return m_ModelPath; }
            set { m_ModelPath = value; }
        }
        [EditorProperty("默认挂点位置", EditorPropertyType.EEPT_CharacteLimbType)]
        public int DefaultPoint
        {
            get { return m_DefaultPoint; }
            set { m_DefaultPoint = value; }
        }
        [EditorProperty("位置偏移", EditorPropertyType.EEPT_Vector3, LabelWidth = 60)]
        public EVector3 OffsetPos
        {
            get { return m_OffsetPos; }
            set { m_OffsetPos = value; }
        }
        [EditorProperty("角度偏移", EditorPropertyType.EEPT_Vector3, LabelWidth = 60)]
        public EVector3 OffsetRot
        {
            get { return m_OffsetRot; }
            set { m_OffsetRot = value; }
        }
        [EditorProperty("道具类型(GEnum)", EditorPropertyType.EEPT_Enum, LabelWidth = 100)]
        public GEnum PropType
        {
            get { return m_PropType; }
            set { m_PropType = value; }
        }
        //[EditorProperty("道具占用槽位(GEnum)", EditorPropertyType.EEPT_EnumList, LabelWidth = 60, Required = true)]
        //public List<GEnum> PropTypes
        //{
        //    get { return m_PropTypes; }
        //    set { m_PropTypes = value; }
        //}
        //[EditorProperty("备用占用槽位(GEnum)(第一占位槽不满足时启用)", EditorPropertyType.EEPT_EnumList, LabelWidth = 60)]
        //public List<GEnum> PropTypes2
        //{
        //    get { return m_PropTypes2; }
        //    set { m_PropTypes2 = value; }
        //}
        [EditorProperty("道具属性", EditorPropertyType.EEPT_GValueSetting)]
        public GValue_Setting GValueSetting
        {
            get { return m_GValueSetting; }
            set { m_GValueSetting = value; }
        }

        [EditorProperty("装备时执行的Action列表", EditorPropertyType.EEPT_ActionList)]
        public List<int> StartAction
        {
            get { return m_StartAction; }
            set { m_StartAction = value; }
        }
        [EditorProperty("默认加载的Action组", EditorPropertyType.EEPT_ActionInfo, LabelWidth = 120)]
        public int DefaultAction
        {
            get { return m_DefaultAction; }
            set { m_DefaultAction = value; }
        }
        //public List<SActionInfoDic> ActionInfoList
        //{
        //    get { return m_ActionInfoList; }
        //    set { m_ActionInfoList = value; }
        //}
        //public List<ActionOverrideClip> OverrideClips
        //{
        //    get { return mOverrideClips; }
        //    set { mOverrideClips = value; }
        //}
        #endregion
        public PropWarp(int _id)
        {
            ID = _id;
        }
    }
}