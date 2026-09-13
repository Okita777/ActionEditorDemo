using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class SkillWarp : IProperty
    {
        [SerializeField] protected int mStartActionIDs = 0;
        [SerializeField] protected int mID;
        [SerializeField] protected GValue_Setting mGvalueSetting = new GValue_Setting();
        [SerializeField] protected ActionStateInfo mActionStateInfo;
        [SerializeField] protected int[] mTags = new int[0];
        [SerializeField] protected GraphEvent_NoValue_Bool mInstanceJudgment = new GraphEvent_NoValue_Bool(true);

        #region Property

        [EditorProperty("技能基础属性", EditorPropertyType.EEPT_GValueSetting)]
        public GValue_Setting GvalueSetting
        {
            get { return mGvalueSetting; }
            set { mGvalueSetting = value; }
        }
        [EditorProperty("技能标签", EditorPropertyType.EEPT_SkillTypeTag)]
        public int[] Tags
        {
            get { return mTags; }
            set { mTags = value; }
        }
        [EditorProperty("初始化时执行的Action", EditorPropertyType.EEPT_SkillAction)]
        public int StartActionIDs
        {
            get { return mStartActionIDs; }
            set { mStartActionIDs = value; }
        }
        [EditorProperty("技能创建有效判定", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Bool InstanceJudgment
        {
            get
            {
#if UNITY_EDITOR
                if (mInstanceJudgment is null) mInstanceJudgment = new GraphEvent_NoValue_Bool(true);
#endif
                return mInstanceJudgment;
            }
            set { mInstanceJudgment = value; }
        }

        public int ID
        {
            get { return mID; }
            set { mID = value; }
        }
        //public string Name
        //{
        //    get { return mName; }
        //    set { mName = value; }
        //}
        public ActionStateInfo ActionStateInfo
        {
            get { return mActionStateInfo; }
            set { mActionStateInfo = value; }
        }
        //public List<ActionState_Skill> ActionStates
        //{
        //    get { return mActionStates; }
        //    set { mActionStates = value; }
        //}

        public void Init()
        {
            mActionStateInfo.Init();
        }
        #endregion
    }
}