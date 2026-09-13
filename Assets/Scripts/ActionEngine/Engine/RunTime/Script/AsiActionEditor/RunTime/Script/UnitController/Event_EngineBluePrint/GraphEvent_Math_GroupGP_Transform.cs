using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Math_GroupGP_Transform : BluePrint_Transform
    {
        [SerializeReference] protected BluePrint_GroupTransform m_ListValue = new GraphEvent_GValue_GGTransform();
        [SerializeReference] protected BluePrint_Int m_Index = new GraphEvent_Value_Int();
        #region Property
        [EditorGraphProperty("GroupTransform", true, EditorGraphPropertyType.EEPT_GGroupTransform, LabelWidth = 60)]
        public BluePrint_GroupTransform ListValue
        {
            get { return m_ListValue; }
            set { m_ListValue = value; }
        }
        [EditorGraphProperty("Index", true, EditorGraphPropertyType.EEPT_Int)]
        public BluePrint_Int Index
        {
            get { return m_Index; }
            set { m_Index = value; }
        }
        #endregion

        [System.NonSerialized] private Transform m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            ListValue.Init(part, _time);
            Index.Init(part, _time);
            if (Index.value < ListValue.value.Count)
            {
                m_ReturnVal = ListValue.value[Index.value];
            }
#if UNITY_EDITOR
            else
            {
                string _debug = EngineDebug.DebugActionStatePart(part);
                if (Index.value < 0)
                    EngineDebug.LogWarning($"超出索引，请检查蓝图逻辑 当前Index[{Index.value}]  长度[{ListValue.value.Count}]\n" + _debug);
                else
                    EngineDebug.LogError($"超出索引，请检查蓝图逻辑 当前Index[{Index.value}]  长度[{ListValue.value.Count}]\n" + _debug);
            }
#endif
        }
        public override Transform value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_GroupGP_Transform GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_GroupGP_Transform();
                GraphEventG.ListValue = (BluePrint_GroupTransform)m_ListValue.Clone();
                GraphEventG.Index = (BluePrint_Int)m_Index.Clone();
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