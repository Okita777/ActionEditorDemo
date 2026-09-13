using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Lerp_PointData : BluePrint_PointData
    {
        [SerializeReference] protected BluePrint_PointData m_A = new GraphEvent_BValue_Point();
        [SerializeReference] protected BluePrint_PointData m_B = new GraphEvent_BValue_Point();
        [SerializeReference] protected BluePrint_Float m_T = new GraphEvent_Value_Float();

        #region Property

        [EditorGraphProperty("起点A", true, EditorGraphPropertyType.EEPT_PointData)]
        public BluePrint_PointData A
        {
            get => m_A;
            set => m_A = value;
        }

        [EditorGraphProperty("终点B", true, EditorGraphPropertyType.EEPT_PointData)]
        public BluePrint_PointData B
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

        [System.NonSerialized] private PointData m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_A.Init(part, _time);
            m_B.Init(part, _time);
            m_T.Init(part, _time);

            float t = m_T.value;
            Vector3 lerpedPos = Vector3.Lerp(m_A.value.pos, m_B.value.pos, t);
            // 旋转分量使用 Quaternion.Slerp 保证角度插值路径最短且无畸变
            Quaternion slerpedRot = Quaternion.Slerp(m_A.value.rot, m_B.value.rot, t);
            m_ReturnVal = new PointData(lerpedPos, slerpedRot);
        }

        public override PointData value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_Lerp_PointData GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Lerp_PointData();
                GraphEventG.A = (BluePrint_PointData)m_A.Clone();
                GraphEventG.B = (BluePrint_PointData)m_B.Clone();
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
