using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Sub_GroupBool_Bool : BluePrint_GroupBool
    {
        [SerializeReference] protected BluePrint_GroupBool m_InVal = new GraphEvent_GValue_GGBool();
        [SerializeReference] protected BluePrint_Bool m_InVal2 = new GraphEvent_Value_Bool();

        #region Property
        [EditorGraphProperty("GroupBool", true, EditorGraphPropertyType.EEPT_GGroupBool, LabelWidth = 60)]
        public BluePrint_GroupBool ListVal
        {
            get { return m_InVal; }
            set { m_InVal = value; }
        }
        [EditorGraphProperty("Bool", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 60)]
        public BluePrint_Bool InVal2
        {
            get { return m_InVal2; }
            set { m_InVal2 = value; }
        }
        #endregion

        [System.NonSerialized] private List<bool> m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_ReturnVal = EngineResourcesManager.Instance.CreateBools();
            m_InVal.Init(part, _time);
            m_InVal2.Init(part, _time);
            m_ReturnVal.AddRange(m_InVal.value);
            m_ReturnVal.Remove(m_InVal2.value);
        }

        public override List<bool> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_Sub_GroupBool_Bool GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Sub_GroupBool_Bool();
                GraphEventG.ListVal = (BluePrint_GroupBool)m_InVal.Clone();
                GraphEventG.InVal2 = (BluePrint_Bool)m_InVal2.Clone();
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
