using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_GetHashCode_Unit : BluePrint_Int
    {
        [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_Value_SelfUnit();

        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit UnitVal
        {
            get { return m_UnitVal; }
            set { m_UnitVal = value; }
        }

        [System.NonSerialized] private int m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_UnitVal.Init(part, _time);
            TargetUnit unit = m_UnitVal.value;
            m_ReturnVal = m_UnitVal.isValid(part) && unit != null
                ? unit.GetHashCode()
                : 0;
        }

        public override int value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_GetHashCode_Unit m_GraphEvent;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (m_GraphEvent is null)
            {
                m_GraphEvent = new GraphEvent_Math_GetHashCode_Unit
                {
                    UnitVal = (BluePrint_Unit)m_UnitVal.Clone()
                };
                ActionSaveFlishEvent.ActionEvent.AddListener(() =>
                {
                    m_GraphEvent = null;
                });
            }
            return m_GraphEvent;
#else
            return this;
#endif
        }
    }
}
