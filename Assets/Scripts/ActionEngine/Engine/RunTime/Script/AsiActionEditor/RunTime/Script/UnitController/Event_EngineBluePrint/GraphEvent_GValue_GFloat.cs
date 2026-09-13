using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_GValue_GFloat : BluePrint_Float
    {
        [SerializeField] protected GFloat m_FloatVal = new GFloat();
        [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_Value_SelfUnit();
        #region Property

        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit UnitVal
        {
            get { return m_UnitVal; }
            set { m_UnitVal = value; }
        }
        [EditorGraphProperty("Float", false, EditorGraphPropertyType.EEPT_GFloat)]
        public GFloat FloatVal
        {
            get { return m_FloatVal; }
            set { m_FloatVal = value; }
        }
        #endregion

        [System.NonSerialized] private float m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//避免同一帧下反复执行初始化

            if (m_UnitVal.IsNode)
            {
                m_UnitVal.Init(part, _time);

#if UNITY_EDITOR
                if (m_UnitVal.value is null)
                {
                    m_ReturnVal = 0;
                    EngineDebug.DebugUnitGruphError(m_UnitVal);
                    return;
                }
#endif
                // if (m_UnitVal.value is null) return;

                // m_FloatVal.Init(m_UnitVal.value.ActionStateMachine);
                m_ReturnVal = m_FloatVal.GetValue(m_UnitVal.value.GetUnit().ActionStateMachine.FirstStatePart);
                return;
            }
            m_ReturnVal = m_FloatVal.GetValue(part);

            // if (m_FloatVal.mStateMachine is null)
            // {
            //     m_FloatVal.Init(part.ActionStateMachine);
            //     // EngineDebug.LogError($"出错啦! [{m_FloatVal.ty}]");
            // }
            // m_ReturnVal = m_FloatVal.value;
        }
        public override float value => m_ReturnVal;


#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_GValue_GFloat GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_GValue_GFloat();
                GraphEventG.FloatVal = (GFloat)m_FloatVal.Clone();
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