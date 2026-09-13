using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Sub_GroupFloat_Float : BluePrint_GroupFloat
    {
        [SerializeReference] protected BluePrint_GroupFloat m_InVal = new GraphEvent_GValue_GGFloat();
        [SerializeReference] protected BluePrint_Float m_InVal2 = new GraphEvent_Value_Float();

        #region Property
        [EditorGraphProperty("GroupFloat", true, EditorGraphPropertyType.EEPT_GGroupFloat, LabelWidth = 60)]
        public BluePrint_GroupFloat ListVal
        {
            get { return m_InVal; }
            set { m_InVal = value; }
        }
        [EditorGraphProperty("Float", true, EditorGraphPropertyType.EEPT_Float, LabelWidth = 60)]
        public BluePrint_Float InVal2
        {
            get { return m_InVal2; }
            set { m_InVal2 = value; }
        }
        #endregion

        [System.NonSerialized] private List<float> m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_ReturnVal = EngineResourcesManager.Instance.CreateFloats();
            m_InVal.Init(part, _time);
            m_InVal2.Init(part, _time);
            m_ReturnVal.AddRange(m_InVal.value);
            m_ReturnVal.Remove(m_InVal2.value);
        }

        public override List<float> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_Sub_GroupFloat_Float GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Sub_GroupFloat_Float();
                GraphEventG.ListVal = (BluePrint_GroupFloat)m_InVal.Clone();
                GraphEventG.InVal2 = (BluePrint_Float)m_InVal2.Clone();
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
