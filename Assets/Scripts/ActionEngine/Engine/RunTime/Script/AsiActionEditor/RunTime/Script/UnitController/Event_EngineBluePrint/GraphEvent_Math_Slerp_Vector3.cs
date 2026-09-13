using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Slerp_Vector3 : BluePrint_Vector3
    {
        [SerializeReference] protected BluePrint_Vector3 m_A = new GraphEvent_Value_Vector3();
        [SerializeReference] protected BluePrint_Vector3 m_B = new GraphEvent_Value_Vector3();
        [SerializeReference] protected BluePrint_Float m_T = new GraphEvent_Value_Float();

        #region Property

        [EditorGraphProperty("起点A", true, EditorGraphPropertyType.EEPT_Vector3)]
        public BluePrint_Vector3 A
        {
            get => m_A;
            set => m_A = value;
        }

        [EditorGraphProperty("终点B", true, EditorGraphPropertyType.EEPT_Vector3)]
        public BluePrint_Vector3 B
        {
            get => m_B;
            set => m_B = value;
        }

        [EditorGraphProperty("插值T", true, EditorGraphPropertyType.EEPT_Float)]
        public BluePrint_Float T
        {
            get => m_T;
            set => m_T = value;
        }

        #endregion

        [System.NonSerialized] private Vector3 m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_A.Init(part, _time);
            m_B.Init(part, _time);
            m_T.Init(part, _time);
            // 球面线性插值，适合方向向量之间的过渡
            m_ReturnVal = Vector3.Slerp(m_A.value, m_B.value, m_T.value);
        }

        public override Vector3 value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_Slerp_Vector3 GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Slerp_Vector3();
                GraphEventG.A = (BluePrint_Vector3)m_A.Clone();
                GraphEventG.B = (BluePrint_Vector3)m_B.Clone();
                GraphEventG.T = (BluePrint_Float)m_T.Clone();
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
