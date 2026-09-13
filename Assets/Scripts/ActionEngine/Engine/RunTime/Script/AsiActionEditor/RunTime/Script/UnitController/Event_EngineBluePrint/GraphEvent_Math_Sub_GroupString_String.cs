using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Sub_GroupString_String : BluePrint_GroupString
    {
        [SerializeReference] protected BluePrint_GroupString m_InVal = new GraphEvent_GValue_GGString();
        [SerializeReference] protected BluePrint_String m_InVal2 = new GraphEvent_Value_String();

        #region Property
        [EditorGraphProperty("GroupString", true, EditorGraphPropertyType.EEPT_GGroupString, LabelWidth = 60)]
        public BluePrint_GroupString ListVal
        {
            get { return m_InVal; }
            set { m_InVal = value; }
        }
        [EditorGraphProperty("String", true, EditorGraphPropertyType.EEPT_String, LabelWidth = 60)]
        public BluePrint_String InVal2
        {
            get { return m_InVal2; }
            set { m_InVal2 = value; }
        }
        #endregion

        [System.NonSerialized] private List<string> m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_ReturnVal = EngineResourcesManager.Instance.CreateStrings();
            m_InVal.Init(part, _time);
            m_InVal2.Init(part, _time);
            m_ReturnVal.AddRange(m_InVal.value);
            m_ReturnVal.Remove(m_InVal2.value);
        }

        public override List<string> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_Sub_GroupString_String GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Sub_GroupString_String();
                GraphEventG.ListVal = (BluePrint_GroupString)m_InVal.Clone();
                GraphEventG.InVal2 = (BluePrint_String)m_InVal2.Clone();
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
