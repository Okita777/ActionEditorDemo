using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_GValue_GTransform : BluePrint_Transform
    {
        [SerializeField] protected GTransform m_IntVal = new GTransform();
        [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_Value_SelfUnit();
        #region Property

        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit UnitVal
        {
            get { return m_UnitVal; }
            set { m_UnitVal = value; }
        }
        [EditorGraphProperty("Transform", false, EditorGraphPropertyType.EEPT_GTransform)]
        public GTransform IntVal
        {
            get { return m_IntVal; }
            set { m_IntVal = value; }
        }
        #endregion

        [System.NonSerialized] private Transform m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//避免同一帧下反复执行初始化

            if (!m_UnitVal.IsNode)
            {
                m_ReturnVal = m_IntVal.GetValue(part);
            }
            else
            {
                m_UnitVal.Init(part, _time);
                if (m_UnitVal.value == null)
                {
                    m_ReturnVal = part.ActionStateMachine.CurUnit.transform;
#if UNITY_EDITOR

                    EngineDebug.DebugUnitGruphError(m_UnitVal);
#endif
                    return;
                }
                m_IntVal.GetValue(m_UnitVal.value.GetUnit().ActionStateMachine.FirstStatePart);
            }

        }
        public override Transform value => m_ReturnVal;


#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_GValue_GTransform GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_GValue_GTransform();
                GraphEventG.IntVal = (GTransform)m_IntVal.Clone();
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