using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_SetHitRelation_Bool : BluePrint_Bool
    {
        [SerializeReference] protected BluePrint_Unit m_TargetUnit = new GraphEvent_Value_SelfUnit();
        [SerializeReference] protected BluePrint_Unit m_WriteUnit = new GraphEvent_Value_SelfUnit();
        [SerializeField] protected byte m_RelationType = 0;

        #region Property
        [EditorGraphProperty("目标Unit", true, EditorGraphPropertyType.EEPT_GUnit, LabelWidth = 80)]
        public BluePrint_Unit TargetUnit
        {
            get { return m_TargetUnit; }
            set { m_TargetUnit = value; }
        }

        [EditorGraphProperty("写入目标", false, EditorGraphPropertyType.EEPT_EnumCustom, LabelWidth = 80, EnumNames = new[] { "攻击者", "命中者" })]
        public byte RelationType
        {
            get { return m_RelationType; }
            set { m_RelationType = value; }
        }

        [EditorGraphProperty("写入Unit", true, EditorGraphPropertyType.EEPT_GUnit, LabelWidth = 80)]
        public BluePrint_Unit WriteUnit
        {
            get { return m_WriteUnit; }
            set { m_WriteUnit = value; }
        }


        #endregion

        [System.NonSerialized] private bool m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            if (m_WriteUnit is null)
            {
                m_ReturnVal = false;
                return;
            }

            m_WriteUnit.Init(part, _time);
            m_ReturnVal = m_WriteUnit.isValid(part);
            if (!m_ReturnVal) return;

            if (m_TargetUnit is null) return;

            m_TargetUnit.Init(part, _time);
            if (!m_TargetUnit.isValid(part)) return;

            ActionStateMachine targetMachine = m_TargetUnit.value.GetUnit().ActionStateMachine;
            TargetUnit writeUnit = m_WriteUnit.value;
            if (m_RelationType == 0)
            {
                targetMachine.AttackerUnit = writeUnit.GetUnit();
            }
            else
            {
                targetMachine.HitUnit = writeUnit;
            }
        }

        public override bool value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_SetHitRelation_Bool GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_SetHitRelation_Bool();
                GraphEventG.TargetUnit = m_TargetUnit is null ? new GraphEvent_Value_SelfUnit() : (BluePrint_Unit)m_TargetUnit.Clone();
                GraphEventG.WriteUnit = m_WriteUnit is null ? new GraphEvent_Value_SelfUnit() : (BluePrint_Unit)m_WriteUnit.Clone();
                GraphEventG.RelationType = m_RelationType;
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
