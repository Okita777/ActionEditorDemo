
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_TrackData_ActionListID : BluePrint_Int
    {
        [SerializeField] protected bool m_UseTargetActionState = true;
        [System.NonSerialized] private int m_ReturnVal;

        #region Property
        [EditorGraphProperty("取跳转目标", false, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 80)]
        public bool UseTargetActionState
        {
            get { return m_UseTargetActionState; }
            set { m_UseTargetActionState = value; }
        }
        #endregion

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            //m_ReturnVal = part.LoopIndex;
            m_ReturnVal = -1;
            if (m_UseTargetActionState && part.TryGetInterruptTargetActionGroupID(out int _targetActionGroupID))
            {
                m_ReturnVal = _targetActionGroupID;
                return;
            }

            if (part.TryGetActiveActionGroupID(out int _activeActionGroupID))
            {
                m_ReturnVal = _activeActionGroupID;
                return;
            }

            if (part.CurrentActionState is not null)
            {
                ActionStateMachine _stateMachine = part.ActionStateMachine;
                if (_stateMachine.TryGetCurActionStateInfo(part.CurrentActionState.AnimaLayer, out int _info))
                {
                    m_ReturnVal = _info;
                }
            }
        }
        public override int value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_TrackData_ActionListID GraphEventG = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_TrackData_ActionListID();
                GraphEventG.UseTargetActionState = m_UseTargetActionState;
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