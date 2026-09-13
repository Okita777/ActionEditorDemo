using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_SetFirstActionGroupID_Bool : BluePrint_Bool
    {
        [SerializeReference] protected BluePrint_Int m_FirstActionGroupID = new GraphEvent_Value_Int() { IntVal = -1 };

        [EditorGraphProperty("ActionGroupID", true, EditorGraphPropertyType.EEPT_Int,LabelWidth = 90)]
        public BluePrint_Int FirstActionGroupID
        {
            get => m_FirstActionGroupID;
            set => m_FirstActionGroupID = value;
        }

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_FirstActionGroupID.Init(part, _time);
            part.ActionStateMachine.SetFirstActionGroupID(m_FirstActionGroupID.value);
        }

        public override bool value => true;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_SetFirstActionGroupID_Bool GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_SetFirstActionGroupID_Bool();
                GraphEventG.FirstActionGroupID = (BluePrint_Int)m_FirstActionGroupID.Clone();
                GraphEventG.IsNode = IsNode;
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
