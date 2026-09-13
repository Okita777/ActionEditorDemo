using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_CustomFor_PointData : BluePrint_PointData
    {
        [SerializeReference] protected BluePrint_PointData m_Value = new GraphEvent_BValue_Point();
        [SerializeReference] protected BluePrint_Int m_LoopCount = new GraphEvent_Value_Int() { IntVal = 1 };
        [SerializeReference] protected BluePrint_Bool m_EndCondition = new GraphEvent_Value_Bool();

        [EditorGraphProperty("值(PointData)", true, EditorGraphPropertyType.EEPT_PointData, LabelWidth = 80)]
        public BluePrint_PointData Value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        [EditorGraphProperty("循环次数(Int)", true, EditorGraphPropertyType.EEPT_Int, LabelWidth = 80)]
        public BluePrint_Int LoopCount
        {
            get { return m_LoopCount; }
            set { m_LoopCount = value; }
        }

        [EditorGraphProperty("结束条件(Bool)", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 80)]
        public BluePrint_Bool EndCondition
        {
            get { return m_EndCondition; }
            set { m_EndCondition = value; }
        }

        [System.NonSerialized] private PointData m_ReturnValue;

        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_LoopCount.Init(part, time);
            int loopCount = Mathf.Clamp(m_LoopCount.value, 1, MotionEngineConst.BluePrint_ListMaxCount);
            bool previousCheckState = part.ActionStateMachine.IsCheckBluePrintCon;
            part.ActionStateMachine.IsCheckBluePrintCon = false;

            for (int index = 0; index < loopCount; index++)
            {
                part.ActionStateMachine.BluePrint_LoopIndex = index;
                m_Value.Init(part, time);
                m_ReturnValue = m_Value.value;

                m_EndCondition.Init(part, time);
                if (m_EndCondition.value) break;
            }
        }

        public override PointData value => m_ReturnValue;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_CustomFor_PointData GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_CustomFor_PointData();
                GraphEventG.Value = (BluePrint_PointData)m_Value.Clone();
                GraphEventG.LoopCount = (BluePrint_Int)m_LoopCount.Clone();
                GraphEventG.EndCondition = (BluePrint_Bool)m_EndCondition.Clone();
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
