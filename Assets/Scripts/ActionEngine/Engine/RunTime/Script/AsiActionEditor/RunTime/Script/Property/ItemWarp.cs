using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class ItemWarp : IProperty
    {
        //[System.Serializable]
        //public struct ActionOverrideClip
        //{
        //    public int ActionID;
        //    public string ClipPath;
        //    public ActionOverrideClip(int actionID, string clipPath)
        //    {
        //        ActionID = actionID;
        //        ClipPath = clipPath;
        //    }
        //}

        [SerializeField] protected int m_ID;
        [SerializeField] protected string m_ModelPath;//模型路径
        // [SerializeField] protected string m_RuntimeAnimPath;//控制器路径
        [SerializeField] protected List<ActionState> m_Actions = new List<ActionState>();
        [SerializeField] protected GEnum m_ItemType = new GEnum();
        [SerializeField] protected GValue_Setting m_GValueSetting = new GValue_Setting();
        [SerializeField] protected int m_DefaultPoint;
        //[SerializeField] protected List<ActionOverrideClip> mOverrideClips = new ();

        #region MyRegion
        public int ID
        {
            get { return m_ID; }
            set { m_ID = value; }
        }
        public List<ActionState> Actions
        {
            get { return m_Actions; }
            set { m_Actions = value; }
        }
        [EditorProperty("道具预制体", EditorPropertyType.EEPT_GameObject)]
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
        // [EditorProperty("道具替换动画", EditorPropertyType.EEPT_OverAnimator)]
        // public string RuntimeAnimPath
        // {
        //     get { return m_RuntimeAnimPath; }
        //     set { m_RuntimeAnimPath = value; }
        // }
        [EditorProperty("道具类型", EditorPropertyType.EEPT_Enum)]
        public GEnum ItemType
        {
            get { return m_ItemType; }
            set { m_ItemType = value; }
        }

        [EditorProperty("道具属性", EditorPropertyType.EEPT_GValueSetting)]
        public GValue_Setting GValueSetting
        {
            get { return m_GValueSetting; }
            set { m_GValueSetting = value; }
        }
        //public List<ActionOverrideClip> OverrideClips
        //{
        //    get { return mOverrideClips; }
        //    set { mOverrideClips = value; }
        //}
        #endregion
        public ItemWarp(int _id)
        {
            ID = _id;
        }
    }
}