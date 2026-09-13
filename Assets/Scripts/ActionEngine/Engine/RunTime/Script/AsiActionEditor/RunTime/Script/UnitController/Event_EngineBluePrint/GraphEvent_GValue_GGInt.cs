using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_GValue_GGInt : BluePrint_GroupInt
    {
        [SerializeField] protected GGroupInt m_IntVal = new GGroupInt();
        [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_Value_SelfUnit();
        #region Property

        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit UnitVal
        {
            get { return m_UnitVal; }
            set { m_UnitVal = value; }
        }
        [EditorGraphProperty("GroupInt", false, EditorGraphPropertyType.EEPT_GGroupInt, LabelWidth = 60)]
        public GGroupInt IntVal
        {
            get { return m_IntVal; }
            set { m_IntVal = value; }
        }
        #endregion

        [System.NonSerialized] private List<int> m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_ReturnVal = EngineResourcesManager.Instance.CreateInts();
            if (!m_UnitVal.IsNode)
            {
                // m_IntVal.Init(part.ActionStateMachine);
                m_ReturnVal.AddRange(m_IntVal.GetValue(part));
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
                m_ReturnVal.AddRange(m_IntVal.GetValue(m_UnitVal.value.GetUnit().ActionStateMachine.FirstStatePart));
            }

        }
        public override List<int> value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_GValue_GGInt GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_GValue_GGInt();
                GraphEventG.IntVal = (GGroupInt)m_IntVal.Clone();
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