using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_GValue_GGFloat : BluePrint_GroupFloat
    {
        [SerializeField] protected GGroupFloat m_IntVal = new GGroupFloat();
        [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_Value_SelfUnit();
        #region Property

        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit UnitVal
        {
            get { return m_UnitVal; }
            set { m_UnitVal = value; }
        }
        [EditorGraphProperty("GroupFloat", false, EditorGraphPropertyType.EEPT_GGroupFloat, LabelWidth = 60)]
        public GGroupFloat IntVal
        {
            get { return m_IntVal; }
            set { m_IntVal = value; }
        }
        #endregion

        [System.NonSerialized] private List<float> m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_ReturnVal = EngineResourcesManager.Instance.CreateFloats();
            if (!m_UnitVal.IsNode)
            {
                m_ReturnVal.AddRange(m_IntVal.GetValue(part));
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
                m_ReturnVal.AddRange(m_IntVal.GetValue(m_UnitVal.value.GetUnit().ActionStateMachine.FirstStatePart));
            }

        }
        public override List<float> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_GValue_GGFloat GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_GValue_GGFloat();
                GraphEventG.IntVal = (GGroupFloat)m_IntVal.Clone();
                GraphEventG.UnitVal = (BluePrint_Unit)m_UnitVal.Clone();
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() =>
                {
                    GraphEventG = null;
                });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
