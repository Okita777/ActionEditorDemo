using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Sub_GroupPoint_Point : BluePrint_GroupPointData
    {
        [SerializeReference] protected BluePrint_GroupPointData m_InVal = new GraphEvent_GValue_GGPoint();
        [SerializeReference] protected BluePrint_PointData m_InVal2 = new GraphEvent_BValue_Point();

        #region Property
        [EditorGraphProperty("GroupPoint", true, EditorGraphPropertyType.EEPT_GGroupPoint, LabelWidth = 60)]
        public BluePrint_GroupPointData ListVal
        {
            get { return m_InVal; }
            set { m_InVal = value; }
        }
        [EditorGraphProperty("Point", true, EditorGraphPropertyType.EEPT_PointData, LabelWidth = 60)]
        public BluePrint_PointData InVal2
        {
            get { return m_InVal2; }
            set { m_InVal2 = value; }
        }
        #endregion

        [System.NonSerialized] private List<PointData> m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_ReturnVal = EngineResourcesManager.Instance.CreatePoints();
            m_InVal.Init(part, _time);
            m_InVal2.Init(part, _time);
            m_ReturnVal.AddRange(m_InVal.value);
            PointData target = m_InVal2.value;
            for (int i = 0; i < m_ReturnVal.Count; i++)
            {
                if (m_ReturnVal[i].Equals(target))
                {
                    m_ReturnVal.RemoveAt(i);
                    break;
                }
            }
        }

        public override List<PointData> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_Sub_GroupPoint_Point GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Sub_GroupPoint_Point();
                GraphEventG.ListVal = (BluePrint_GroupPointData)m_InVal.Clone();
                GraphEventG.InVal2 = (BluePrint_PointData)m_InVal2.Clone();
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
