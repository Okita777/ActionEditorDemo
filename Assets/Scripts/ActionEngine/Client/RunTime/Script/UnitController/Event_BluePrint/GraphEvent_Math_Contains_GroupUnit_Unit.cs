using System.Linq;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Contains_GroupUnit_Unit : BluePrint_Bool
    {
        [SerializeReference] protected BluePrint_GroupUnit m_InVal = new GraphEvent_GValue_GGUnit();
        [SerializeReference] protected BluePrint_Unit m_InVal2 = new GraphEvent_Value_SelfUnit();

        #region Property

        [EditorGraphProperty("GroupUnit", true, EditorGraphPropertyType.EEPT_GGroupUnit, LabelWidth = 60)]
        public BluePrint_GroupUnit IntVal
        {
            get { return m_InVal; }
            set { m_InVal = value; }
        }
        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit, LabelWidth = 60)]
        public BluePrint_Unit InVal2
        {
            get { return m_InVal2; }
            set { m_InVal2 = value; }
        }
        #endregion

        [System.NonSerialized] private bool m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能
            m_InVal.Init(part, _time);
            m_InVal2.Init(part, _time);
            if (m_InVal2.isValid(part))
                m_ReturnVal = m_InVal.value.Contains(m_InVal2.value);
            else
                m_ReturnVal = false;
        }

        public override bool value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_Contains_GroupUnit_Unit GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Contains_GroupUnit_Unit();
                GraphEventG.IntVal = (BluePrint_GroupUnit)m_InVal.Clone();
                GraphEventG.InVal2 = (BluePrint_Unit)m_InVal2.Clone();
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