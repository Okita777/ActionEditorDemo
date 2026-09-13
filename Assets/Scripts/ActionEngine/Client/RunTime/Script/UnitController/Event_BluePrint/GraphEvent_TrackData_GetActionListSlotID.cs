using AsiTimeLine.RunTime;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_TrackData_GetActionListSlotID : BluePrint_Int
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
            //if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能
            m_ReturnVal = -1;
            if (part.ActionStateMachine.CurUnit is ActionEngine_Entity _Entity)
            {
                ActionStateMachine _stateMachine = part.ActionStateMachine;
                bool hasActionGroupID = false;
                int _info = -1;
                if (m_UseTargetActionState)
                {
                    hasActionGroupID = part.TryGetInterruptTargetActionGroupID(out _info);
                }

                if (!hasActionGroupID)
                {
                    hasActionGroupID = part.TryGetActiveActionGroupID(out _info);
                }

                if (!hasActionGroupID && part.CurrentActionState is not null)
                {
                    hasActionGroupID = _stateMachine.TryGetCurActionStateInfo(part.CurrentActionState.AnimaLayer, out _info);
                }

                if (hasActionGroupID)
                {
                    if (_Entity.TryGetSlotIDToActionListID(_info, out int _slotID))
                    {
                        m_ReturnVal = _slotID;
                    }
#if UNITY_EDITOR
                    else if(part.ActionStateMachine.ActionStateInfo.ActionGroupID != _info)
                    {
                        EngineDebug.LogError($"<color=#ffcc00>ActionListID {_info}</color>未找到槽位ID \n蓝图位置:[{EngineDebug.DebugActionStatePart(part)}]");
                    }
#endif
                }
                else
                {
                    EngineDebug.LogError($"<color=#ffcc00>层级 {part.AnimaLayer}</color>未找到ActionInfo \n蓝图位置:[{EngineDebug.DebugActionStatePart(part)}]");
                }
            }
            else
            {
                EngineDebug.LogError($"当前蓝图无法由<color=#ff0000>非Entity单位</color>执行!! \n蓝图位置:[{EngineDebug.DebugActionStatePart(part)}]");
            }
        }

        public override int value => m_ReturnVal;


#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_TrackData_GetActionListSlotID GraphEventG = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_TrackData_GetActionListSlotID();
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