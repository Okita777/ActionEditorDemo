using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Clear_GroupBool : BluePrint_GroupBool
    {
        [SerializeReference] protected BluePrint_GroupBool m_InVal = new GraphEvent_GValue_GGBool();

        #region Property
        [EditorGraphProperty("GroupBool", true, EditorGraphPropertyType.EEPT_GGroupBool, LabelWidth = 60)]
        public BluePrint_GroupBool ListVal
        {
            get { return m_InVal; }
            set { m_InVal = value; }
        }
        #endregion

        [System.NonSerialized] private List<bool> m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_InVal.Init(part, _time);
            m_InVal.value.Clear();
            m_ReturnVal = m_InVal.value;
        }

        public override List<bool> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_Clear_GroupBool GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Clear_GroupBool();
                GraphEventG.ListVal = (BluePrint_GroupBool)m_InVal.Clone();
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
