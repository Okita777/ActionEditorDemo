using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Sub_GroupInt_Int : BluePrint_GroupInt
    {
        [SerializeReference] protected BluePrint_GroupInt m_InVal = new GraphEvent_GValue_GGInt();
        [SerializeReference] protected BluePrint_Int m_InVal2 = new GraphEvent_Value_Int();

        #region Property
        [EditorGraphProperty("GroupInt", true, EditorGraphPropertyType.EEPT_GGroupInt, LabelWidth = 60)]
        public BluePrint_GroupInt IntVal
        {
            get { return m_InVal; }
            set { m_InVal = value; }
        }
        [EditorGraphProperty("Int", true, EditorGraphPropertyType.EEPT_Int, LabelWidth = 60)]
        public BluePrint_Int InVal2
        {
            get { return m_InVal2; }
            set { m_InVal2 = value; }
        }
        #endregion

        [System.NonSerialized] private List<int> m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_ReturnVal = EngineResourcesManager.Instance.CreateInts();
            m_InVal.Init(part, _time);
            m_InVal2.Init(part, _time);
            m_ReturnVal.AddRange(m_InVal.value);
            m_ReturnVal.Remove(m_InVal2.value);
        }

        public override List<int> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_Sub_GroupInt_Int GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Sub_GroupInt_Int();
                GraphEventG.IntVal = (BluePrint_GroupInt)m_InVal.Clone();
                GraphEventG.InVal2 = (BluePrint_Int)m_InVal2.Clone();
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
