using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Math_GetGroupLength : BluePrint_Int
    {
        [SerializeReference] protected BluePrint_Value_List m_ReadValue = new GraphEvent_Value_GetGroupValue();
        #region Property

        [EditorGraphProperty("ReadValue(GroupValue)", true, EditorGraphPropertyType.EEPT_GGroupValue, LabelWidth = 150)]
        public BluePrint_Value_List ReadValue
        {
            get { return m_ReadValue; }
            set { m_ReadValue = value; }
        }
        #endregion

        [System.NonSerialized] private int m_ReturnVal;
        //public override void SetListSource(List<PointData> _list)
        //{
        //    m_ReturnVal = _list;
        //}
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_ReadValue.Init(part, _time);
            m_ReturnVal = m_ReadValue.GetLength();
        }
        public override int value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_GetGroupLength GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_GetGroupLength();
                GraphEventG.ReadValue = (BluePrint_Value_List)m_ReadValue.Clone();
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