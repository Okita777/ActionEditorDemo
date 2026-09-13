using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Math_For_GroupPoint : BluePrint_GroupPointData
    {
        [SerializeReference] protected BluePrint_GroupPointData m_GroupValue = new GraphEvent_GValue_GGPoint();
        [SerializeReference] protected BluePrint_Bool m_IsCheckValid = new GraphEvent_Value_Bool(true);

        #region Property
        [EditorGraphProperty("GroupPoint", true, EditorGraphPropertyType.EEPT_GGroupPoint, LabelWidth = 60)]
        public BluePrint_GroupPointData GroupVal
        {
            get { return m_GroupValue; }
            set { m_GroupValue = value; }
        }

        [EditorGraphProperty("筛选条件(Bool)", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 80)]
        public BluePrint_Bool IsCheckValid
        {
            get { return m_IsCheckValid; }
            set { m_IsCheckValid = value; }
        }
        #endregion

        [System.NonSerialized] private List<PointData> m_ReturnVal = null;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能

            m_ReturnVal = EngineResourcesManager.Instance.CreatePoints();
            m_GroupValue.Init(part, _time);
            //m_ReturnVal.AddRange(m_GroupValue.value);

            part.ActionStateMachine.IsCheckBluePrintCon = false;//避免Bool蓝图相关节点未及时更新逻辑
            for (int i = 0; i < m_GroupValue.value.Count; i++)
            {
                part.ActionStateMachine.BluePrint_LoopIndex = i;//更新序号至外部
                IsCheckValid.Init(part, _time);
                if (IsCheckValid.value)
                {
                    m_ReturnVal.Add(m_GroupValue.value[i]);
                }
            }
            part.ActionStateMachine.IsCheckBluePrintCon = true;
        }
        public override List<PointData> value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_For_GroupPoint GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_For_GroupPoint();
                GraphEventG.GroupVal = (BluePrint_GroupPointData)m_GroupValue.Clone();
                GraphEventG.IsCheckValid = (BluePrint_Bool)m_IsCheckValid.Clone();
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