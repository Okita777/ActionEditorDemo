using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_SetGValue_Float : BluePrint_Float
    {

        [SerializeReference] protected BluePrint_Float m_ReadValue = new GraphEvent_Value_Float();
        [SerializeField] protected GFloat m_FloatVal = new GFloat();
        [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_Value_SelfUnit();
        #region Property

        [EditorGraphProperty("ReadValue", true, EditorGraphPropertyType.EEPT_Float)]
        public BluePrint_Float ReadValue
        {
            get { return m_ReadValue; }
            set { m_ReadValue = value; }
        }
        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit UnitVal
        {
            get { return m_UnitVal; }
            set { m_UnitVal = value; }
        }
        [EditorGraphProperty("GFloat", false, EditorGraphPropertyType.EEPT_GFloat)]
        public GFloat FloatVal
        {
            get { return m_FloatVal; }
            set { m_FloatVal = value; }
        }
        #endregion

        [System.NonSerialized] private float m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            m_ReadValue.Init(part, _time);
            m_ReturnVal = m_ReadValue.value;

            if (!m_UnitVal.IsNode)
            {
                m_FloatVal.SetValue(part, m_ReturnVal);
            }
            else
            {
                m_UnitVal.Init(part, _time);

#if UNITY_EDITOR
                if (m_UnitVal.value is null)
                {
                    EngineDebug.DebugUnitGruphError(m_UnitVal);
                    return;
                }
#endif

                // m_FloatVal.Init(m_UnitVal.value.ActionStateMachine);
                // m_FloatVal.value = m_ReturnVal;
                m_FloatVal.SetValue(m_UnitVal.value.GetUnit().ActionStateMachine.FirstStatePart, m_ReturnVal);
            }
        }
        public override float value => m_ReturnVal;


#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_SetGValue_Float GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_SetGValue_Float();
                GraphEventG.ReadValue = (BluePrint_Float)m_ReadValue.Clone();
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