using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_GValue_GGTransform : BluePrint_GroupTransform
    {
        [SerializeField] protected GGroupTransform m_IntVal = new GGroupTransform();
        [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_Value_SelfUnit();
        #region Property

        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit UnitVal
        {
            get { return m_UnitVal; }
            set { m_UnitVal = value; }
        }
        [EditorGraphProperty("GroupTrans", false, EditorGraphPropertyType.EEPT_GGroupTransform, LabelWidth = 60)]
        public GGroupTransform IntVal
        {
            get { return m_IntVal; }
            set { m_IntVal = value; }
        }
        #endregion

        [System.NonSerialized] private List<Transform> m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_ReturnVal = EngineResourcesManager.Instance.CreateTransforms();
            if (!m_UnitVal.IsNode)
            {
                // m_IntVal.Init(part.ActionStateMachine);
                m_ReturnVal.AddRange(m_IntVal.GetValue(part));
            }
            else
            {
                m_UnitVal.Init(part, _time);
                if (m_UnitVal.value == null)//ReferenceEquals(m_UnitVal.value,null) || 
                {
#if UNITY_EDITOR
                    EngineDebug.DebugUnitGruphError(m_UnitVal);
#endif
                    return;
                }
                m_ReturnVal.AddRange(m_IntVal.GetValue(m_UnitVal.value.GetUnit().ActionStateMachine.FirstStatePart));
            }
        }
        public override List<Transform> value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_GValue_GGTransform GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_GValue_GGTransform();
                GraphEventG.IntVal = (GGroupTransform)m_IntVal.Clone();
                GraphEventG.UnitVal = (BluePrint_Unit)m_UnitVal.Clone();
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