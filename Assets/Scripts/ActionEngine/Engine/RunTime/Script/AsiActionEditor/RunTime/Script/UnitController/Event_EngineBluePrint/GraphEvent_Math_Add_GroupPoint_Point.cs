using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Add_GroupPoint_Point : BluePrint_GroupPointData, IDynamicInputNode
    {
        [SerializeReference] protected BluePrint_GroupPointData m_InVal = new GraphEvent_GValue_GGPoint();
        [SerializeReference] protected List<BluePrint_PointData> m_Items = new List<BluePrint_PointData>();

        #region Property
        [EditorGraphProperty("GroupPoint", true, EditorGraphPropertyType.EEPT_GGroupPoint, LabelWidth = 60)]
        public BluePrint_GroupPointData ListVal { get => m_InVal; set => m_InVal = value; }
        #endregion

        #region IDynamicInputNode
        public int DynamicInputCount => m_Items.Count;
        public BluePrint_Value GetDynamicInput(int index) => m_Items[index];
        public void SetDynamicInput(int index, BluePrint_Value value) => m_Items[index] = (BluePrint_PointData)value;
        public BluePrint_Value CreateDefaultDynamicInput() => new GraphEvent_BValue_Point();
        public void AddDynamicInput() => m_Items.Add(new GraphEvent_BValue_Point());
        public void RemoveDynamicInput(int index) => m_Items.RemoveAt(index);
        #endregion

        [System.NonSerialized] private List<PointData> m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_ReturnVal = EngineResourcesManager.Instance.CreatePoints();
            m_InVal.Init(part, _time);
            m_ReturnVal.AddRange(m_InVal.value);
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (m_Items[i] != null)
                {
                    m_Items[i].Init(part, _time);
                    m_ReturnVal.Add(m_Items[i].value);
                }
            }
        }

        public override List<PointData> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_Add_GroupPoint_Point GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Add_GroupPoint_Point();
                GraphEventG.ListVal = (BluePrint_GroupPointData)m_InVal.Clone();
                GraphEventG.m_Items = new List<BluePrint_PointData>(m_Items.Count);
                for (int i = 0; i < m_Items.Count; i++)
                    GraphEventG.m_Items.Add(m_Items[i] != null ? (BluePrint_PointData)m_Items[i].Clone() : null);
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
