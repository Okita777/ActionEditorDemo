using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Math_Sort_GroupInt : BluePrint_GroupInt
    {
        [SerializeReference] protected BluePrint_GroupInt m_GroupValue = new GraphEvent_GValue_GGInt();
        [SerializeReference] protected BluePrint_Bool m_IsCheckValid = new GraphEvent_Value_Bool(true);
        [SerializeField] protected byte m_SortType = 0;

        #region Property
        [EditorGraphProperty("GroupPoint", true, EditorGraphPropertyType.EEPT_GGroupPoint, LabelWidth = 60)]
        public BluePrint_GroupInt GroupVal
        {
            get { return m_GroupValue; }
            set { m_GroupValue = value; }
        }

        [EditorGraphProperty("筛选条件(Bool)", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 80)]
        public BluePrint_Bool IsCheckValid
        {
            get { return m_IsCheckValid; }
            set { m_IsCheckValid = value; }
        }
        [EditorGraphProperty("排序", false, EditorGraphPropertyType.EEPT_EnumCustom, EnumNames = new[] { "从小到大排序", "从大到小排序" }, LabelWidth = 80)]
        public byte SortType
        {
            get { return m_SortType; }
            set { m_SortType = value; }
        }
        #endregion

        [System.NonSerialized] private List<int> m_ReturnVal = null;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能

            m_ReturnVal = EngineResourcesManager.Instance.CreateInts();
            m_GroupValue.Init(part, _time);
            m_ReturnVal.AddRange(m_GroupValue.value);

            if (SortType == 0) m_ReturnVal.Sort((x, y) => x.CompareTo(y));
            else m_ReturnVal.Sort((x, y) => y.CompareTo(x));
        }
        public override List<int> value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_Sort_GroupInt GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Sort_GroupInt();
                GraphEventG.GroupVal = (BluePrint_GroupInt)m_GroupValue.Clone();
                GraphEventG.IsCheckValid = (BluePrint_Bool)m_IsCheckValid.Clone();
                GraphEventG.SortType = m_SortType;
                GraphEventG.IsNode = IsNode;
                //在保存好文件后重置状态
                ActionSaveFlishEvent.ActionEvent.AddListener(() =>
                {
                    GraphEventG = null;
                });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}