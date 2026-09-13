using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_GValue_GEnum : BluePrint_Int
    {
        [SerializeField] protected GEnum m_IntVal = new GEnum();
        [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_Value_SelfUnit();
        #region Property

        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit UnitVal
        {
            get { return m_UnitVal; }
            set { m_UnitVal = value; }
        }
        [EditorGraphProperty("Enum", false, EditorGraphPropertyType.EEPT_GEnum)]
        public GEnum IntVal
        {
            get { return m_IntVal; }
            set { m_IntVal = value; }
        }
        #endregion

        [System.NonSerialized] private int m_ReturnVal;

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

                // m_IntVal.Init(m_UnitVal.value.ActionStateMachine);
                m_ReturnVal = m_IntVal.GetValue(m_UnitVal.value.GetUnit().ActionStateMachine.FirstStatePart);
                return;
            }

            m_ReturnVal = m_IntVal.GetValue(part);
        }
        public override int value => m_ReturnVal;


#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_GValue_GEnum GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_GValue_GEnum();
                GraphEventG.IntVal = (GEnum)m_IntVal.Clone();
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