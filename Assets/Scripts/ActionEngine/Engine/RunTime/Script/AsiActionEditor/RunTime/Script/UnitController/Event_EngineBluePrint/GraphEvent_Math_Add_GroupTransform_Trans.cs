using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Add_GroupTransform_Trans : BluePrint_GroupTransform, IDynamicInputNode
    {
        [SerializeReference] protected BluePrint_GroupTransform m_InVal = new GraphEvent_GValue_GGTransform();
        [SerializeReference] protected List<BluePrint_Transform> m_Items = new List<BluePrint_Transform>();

        #region Property
        [EditorGraphProperty("GroupTransform", true, EditorGraphPropertyType.EEPT_GGroupTransform, LabelWidth = 60)]
        public BluePrint_GroupTransform ListVal { get => m_InVal; set => m_InVal = value; }
        #endregion

        #region IDynamicInputNode
        public int DynamicInputCount => m_Items.Count;
        public BluePrint_Value GetDynamicInput(int index) => m_Items[index];
        public void SetDynamicInput(int index, BluePrint_Value value) => m_Items[index] = (BluePrint_Transform)value;
        public BluePrint_Value CreateDefaultDynamicInput() => new GraphEvent_BValue_Transform();
        public void AddDynamicInput() => m_Items.Add(new GraphEvent_BValue_Transform());
        public void RemoveDynamicInput(int index) => m_Items.RemoveAt(index);
        #endregion

        [System.NonSerialized] private List<Transform> m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_ReturnVal = EngineResourcesManager.Instance.CreateTransforms();
            m_InVal.Init(part, _time);
            m_ReturnVal.AddRange(m_InVal.value);
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (m_Items[i] != null)
                {
                    m_Items[i].Init(part, _time);
                    if (m_Items[i].value != null)
                        m_ReturnVal.Add(m_Items[i].value);
                }
            }
        }

        public override List<Transform> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_Add_GroupTransform_Trans GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Add_GroupTransform_Trans();
                GraphEventG.ListVal = (BluePrint_GroupTransform)m_InVal.Clone();
                GraphEventG.m_Items = new List<BluePrint_Transform>(m_Items.Count);
                for (int i = 0; i < m_Items.Count; i++)
                    GraphEventG.m_Items.Add(m_Items[i] != null ? (BluePrint_Transform)m_Items[i].Clone() : null);
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
