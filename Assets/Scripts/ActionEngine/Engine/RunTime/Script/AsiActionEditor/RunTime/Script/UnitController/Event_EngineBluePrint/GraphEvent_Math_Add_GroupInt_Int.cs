using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Add_GroupInt_Int : BluePrint_GroupInt, IDynamicInputNode
    {
        [SerializeReference] protected BluePrint_GroupInt m_InVal = new GraphEvent_GValue_GGInt();
        [SerializeReference] protected List<BluePrint_Int> m_Items = new List<BluePrint_Int>();

        #region Property
        [EditorGraphProperty("GroupInt", true, EditorGraphPropertyType.EEPT_GGroupInt, LabelWidth = 60)]
        public BluePrint_GroupInt IntVal { get => m_InVal; set => m_InVal = value; }
        #endregion

        #region IDynamicInputNode
        public int DynamicInputCount => m_Items.Count;
        public BluePrint_Value GetDynamicInput(int index) => m_Items[index];
        public void SetDynamicInput(int index, BluePrint_Value value) => m_Items[index] = (BluePrint_Int)value;
        public BluePrint_Value CreateDefaultDynamicInput() => new GraphEvent_Value_Int();
        public void AddDynamicInput() => m_Items.Add(new GraphEvent_Value_Int());
        public void RemoveDynamicInput(int index) => m_Items.RemoveAt(index);
        #endregion

        [System.NonSerialized] private List<int> m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_ReturnVal = EngineResourcesManager.Instance.CreateInts();
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

        public override List<int> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_Add_GroupInt_Int GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Add_GroupInt_Int();
                GraphEventG.IntVal = (BluePrint_GroupInt)m_InVal.Clone();
                GraphEventG.m_Items = new List<BluePrint_Int>(m_Items.Count);
                for (int i = 0; i < m_Items.Count; i++)
                    GraphEventG.m_Items.Add(m_Items[i] != null ? (BluePrint_Int)m_Items[i].Clone() : null);
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
