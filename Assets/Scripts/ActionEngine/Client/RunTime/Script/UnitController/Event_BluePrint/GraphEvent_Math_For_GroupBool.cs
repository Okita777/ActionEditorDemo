using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_For_GroupBool : BluePrint_GroupBool
    {
        [SerializeReference] protected BluePrint_GroupBool m_GroupValue = new GraphEvent_GValue_GGBool();
        [SerializeReference] protected BluePrint_Bool m_IsCheckValid = new GraphEvent_Value_Bool(true);

        #region Property
        [EditorGraphProperty("GroupBool", true, EditorGraphPropertyType.EEPT_GGroupBool, LabelWidth = 60)]
        public BluePrint_GroupBool GroupVal
        {
            get { return m_GroupValue; }
            set { m_GroupValue = value; }
        }

        [EditorGraphProperty("筛选条件(Bool)", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 80)]
        public BluePrint_Bool IsCheckValid
        {
            get { return m_IsCheckValid; }
            set { m_IsCheckValid = value; }
        }
        #endregion

        [System.NonSerialized] private List<bool> m_ReturnVal = null;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_ReturnVal = EngineResourcesManager.Instance.CreateBools();
            m_GroupValue.Init(part, _time);

            part.ActionStateMachine.IsCheckBluePrintCon = false;
            for (int i = 0; i < m_GroupValue.value.Count; i++)
            {
                part.ActionStateMachine.BluePrint_LoopIndex = i;
                IsCheckValid.Init(part, _time);
                if (IsCheckValid.value)
                {
                    m_ReturnVal.Add(m_GroupValue.value[i]);
                }
            }
            part.ActionStateMachine.IsCheckBluePrintCon = true;
        }
        public override List<bool> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_For_GroupBool GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_For_GroupBool();
                GraphEventG.GroupVal = (BluePrint_GroupBool)m_GroupValue.Clone();
                GraphEventG.IsCheckValid = (BluePrint_Bool)m_IsCheckValid.Clone();
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
