using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_SetGValue_Unit : BluePrint_Unit
    {

        [SerializeReference] protected BluePrint_Unit m_ReadValue = new GraphEvent_Value_SelfUnit();
        [SerializeField] protected GUnit m_FloatVal = new GUnit();
        [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_Value_SelfUnit();
        #region Property

        [EditorGraphProperty("ReadValue", true, EditorGraphPropertyType.EEPT_Int)]
        public BluePrint_Unit ReadValue
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
        [EditorGraphProperty("GUnit", false, EditorGraphPropertyType.EEPT_GUnit)]
        public GUnit FloatVal
        {
            get { return m_FloatVal; }
            set { m_FloatVal = value; }
        }
        #endregion

        [System.NonSerialized] private TargetUnit m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            m_ReadValue.Init(part, _time);
            m_ReturnVal = m_ReadValue.value;
            m_UnitVal.Init(part, _time);
            m_FloatVal.SetValue(m_UnitVal.value.GetUnit().ActionStateMachine.FirstStatePart, m_ReturnVal);
        }
        public override TargetUnit value => m_ReturnVal;


#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_SetGValue_Unit GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_SetGValue_Unit();
                GraphEventG.ReadValue = (BluePrint_Unit)m_ReadValue.Clone();
                GraphEventG.FloatVal = (GUnit)m_FloatVal.Clone();
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

        public override bool isValid(ActionStatePart part) => ReadValue is not null;
    }
}