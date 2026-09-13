using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_GValue_GGString : BluePrint_GroupString
    {
        [SerializeField] protected GGroupString m_GroupVal = new GGroupString();
        [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_Value_SelfUnit();

        #region Property

        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit UnitVal
        {
            get { return m_UnitVal; }
            set { m_UnitVal = value; }
        }

        [EditorGraphProperty("GroupString", false, EditorGraphPropertyType.EEPT_GGroupString, LabelWidth = 60)]
        public GGroupString GroupVal
        {
            get { return m_GroupVal; }
            set { m_GroupVal = value; }
        }

        #endregion

        [System.NonSerialized] private List<string> m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_ReturnVal = EngineResourcesManager.Instance.CreateStrings();
            if (!m_UnitVal.IsNode)
            {
                m_ReturnVal.AddRange(m_GroupVal.GetValue(part));
            }
            else
            {
                m_UnitVal.Init(part, _time);
#if UNITY_EDITOR
                if (m_UnitVal.value is null)
                {
                    EngineDebug.DebugUnitGruphError(m_UnitVal);
                    return;
                }
#endif
                m_ReturnVal.AddRange(m_GroupVal.GetValue(m_UnitVal.value.GetUnit().ActionStateMachine.FirstStatePart));
            }
        }

        public override List<string> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_GValue_GGString GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_GValue_GGString();
                GraphEventG.GroupVal = (GGroupString)m_GroupVal.Clone();
                GraphEventG.UnitVal = (BluePrint_Unit)m_UnitVal.Clone();
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
