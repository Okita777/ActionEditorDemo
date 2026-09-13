using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Clear_GroupPoint : BluePrint_GroupPointData
    {
        [SerializeReference] protected BluePrint_GroupPointData m_InVal = new GraphEvent_GValue_GGPoint();

        #region Property
        [EditorGraphProperty("GroupPoint", true, EditorGraphPropertyType.EEPT_GGroupPoint, LabelWidth = 60)]
        public BluePrint_GroupPointData ListVal
        {
            get { return m_InVal; }
            set { m_InVal = value; }
        }
        #endregion

        [System.NonSerialized] private List<PointData> m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_InVal.Init(part, _time);
            m_InVal.value.Clear();
            m_ReturnVal = m_InVal.value;
        }

        public override List<PointData> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_Clear_GroupPoint GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Clear_GroupPoint();
                GraphEventG.ListVal = (BluePrint_GroupPointData)m_InVal.Clone();
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
