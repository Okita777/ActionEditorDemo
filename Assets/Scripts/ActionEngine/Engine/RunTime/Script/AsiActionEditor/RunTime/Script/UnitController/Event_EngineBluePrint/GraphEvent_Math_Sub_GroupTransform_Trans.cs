using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Sub_GroupTransform_Trans : BluePrint_GroupTransform
    {
        [SerializeReference] protected BluePrint_GroupTransform m_InVal = new GraphEvent_GValue_GGTransform();
        [SerializeReference] protected BluePrint_Transform m_InVal2 = new GraphEvent_BValue_Transform();

        #region Property
        [EditorGraphProperty("GroupTransform", true, EditorGraphPropertyType.EEPT_GGroupTransform, LabelWidth = 60)]
        public BluePrint_GroupTransform ListVal
        {
            get { return m_InVal; }
            set { m_InVal = value; }
        }
        [EditorGraphProperty("Transform", true, EditorGraphPropertyType.EEPT_Transform, LabelWidth = 60)]
        public BluePrint_Transform InVal2
        {
            get { return m_InVal2; }
            set { m_InVal2 = value; }
        }
        #endregion

        [System.NonSerialized] private List<Transform> m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_ReturnVal = EngineResourcesManager.Instance.CreateTransforms();
            m_InVal.Init(part, _time);
            m_InVal2.Init(part, _time);
            m_ReturnVal.AddRange(m_InVal.value);
            if (m_InVal2.value != null)
            {
                m_ReturnVal.Remove(m_InVal2.value);
            }
        }

        public override List<Transform> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_Sub_GroupTransform_Trans GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Sub_GroupTransform_Trans();
                GraphEventG.ListVal = (BluePrint_GroupTransform)m_InVal.Clone();
                GraphEventG.InVal2 = (BluePrint_Transform)m_InVal2.Clone();
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
