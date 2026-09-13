using AsiTimeLine.RunTime;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_TrackData_CheckNavMesh : BluePrint_Bool
    {
        [SerializeReference] protected BluePrint_PointData m_CenterPos = new GraphEvent_BValue_Point();
        [SerializeReference] protected BluePrint_Float m_Range = new GraphEvent_Value_Float(0.1f);
        #region Property

        [EditorGraphProperty("目标点", true, EditorGraphPropertyType.EEPT_PointData)]
        public BluePrint_PointData CenterPos
        {
            get { return m_CenterPos; }
            set { m_CenterPos = value; }
        }
        [EditorGraphProperty("范围半径", true, EditorGraphPropertyType.EEPT_Float)]
        public BluePrint_Float Range
        {
            get { return m_Range; }
            set { m_Range = value; }
        }
        #endregion

        [System.NonSerialized] private bool m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能
            CenterPos.Init(part, _time);
            Range.Init(part, _time);

            part.ActionStateMachine.TryGetStaticLogic(out Ex_NavMash navMash, nameof(Ex_NavMash));
            m_ReturnVal = navMash.CheckPointToNavMash(CenterPos.value.pos, Range.value);
        }

        public override bool value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_TrackData_CheckNavMesh GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_TrackData_CheckNavMesh();
                GraphEventG.CenterPos = (BluePrint_PointData)m_CenterPos.Clone();
                GraphEventG.Range = (BluePrint_Float)m_Range.Clone();
                GraphEventG.IsNode = IsNode;
                //在保存好文件后重置状态
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