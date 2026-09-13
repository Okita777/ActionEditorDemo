using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Add_GroupBool_Bool : BluePrint_GroupBool, IDynamicInputNode
    {
        [SerializeReference] protected BluePrint_GroupBool m_InVal = new GraphEvent_GValue_GGBool();
        [SerializeReference] protected List<BluePrint_Bool> m_Items = new List<BluePrint_Bool>();

        #region Property
        [EditorGraphProperty("GroupBool", true, EditorGraphPropertyType.EEPT_GGroupBool, LabelWidth = 60)]
        public BluePrint_GroupBool ListVal { get => m_InVal; set => m_InVal = value; }
        #endregion

        #region IDynamicInputNode
        public int DynamicInputCount => m_Items.Count;
        public BluePrint_Value GetDynamicInput(int index) => m_Items[index];
        public void SetDynamicInput(int index, BluePrint_Value value) => m_Items[index] = (BluePrint_Bool)value;
        public BluePrint_Value CreateDefaultDynamicInput() => new GraphEvent_Value_Bool();
        public void AddDynamicInput() => m_Items.Add(new GraphEvent_Value_Bool());
        public void RemoveDynamicInput(int index) => m_Items.RemoveAt(index);
        #endregion

        [System.NonSerialized] private List<bool> m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_ReturnVal = EngineResourcesManager.Instance.CreateBools();
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

        public override List<bool> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_Add_GroupBool_Bool GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Add_GroupBool_Bool();
                GraphEventG.ListVal = (BluePrint_GroupBool)m_InVal.Clone();
                GraphEventG.m_Items = new List<BluePrint_Bool>(m_Items.Count);
                for (int i = 0; i < m_Items.Count; i++)
                    GraphEventG.m_Items.Add(m_Items[i] != null ? (BluePrint_Bool)m_Items[i].Clone() : null);
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
