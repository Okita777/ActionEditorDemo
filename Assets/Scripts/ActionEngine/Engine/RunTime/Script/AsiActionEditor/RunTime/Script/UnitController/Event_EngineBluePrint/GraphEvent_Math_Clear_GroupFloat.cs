using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Clear_GroupFloat : BluePrint_GroupFloat
    {
        [SerializeReference] protected BluePrint_GroupFloat m_InVal = new GraphEvent_GValue_GGFloat();

        #region Property
        [EditorGraphProperty("GroupFloat", true, EditorGraphPropertyType.EEPT_GGroupFloat, LabelWidth = 60)]
        public BluePrint_GroupFloat ListVal
        {
            get { return m_InVal; }
            set { m_InVal = value; }
        }
        #endregion

        [System.NonSerialized] private List<float> m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_InVal.Init(part, _time);
            m_InVal.value.Clear();
            m_ReturnVal = m_InVal.value;
        }

        public override List<float> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_Clear_GroupFloat GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Clear_GroupFloat();
                GraphEventG.ListVal = (BluePrint_GroupFloat)m_InVal.Clone();
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
