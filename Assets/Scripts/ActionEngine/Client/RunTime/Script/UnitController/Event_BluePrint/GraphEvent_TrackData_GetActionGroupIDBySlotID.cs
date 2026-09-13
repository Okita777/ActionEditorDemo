using AsiTimeLine.RunTime;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_TrackData_GetActionGroupIDBySlotID : BluePrint_Int
    {
        [SerializeReference] protected BluePrint_Int m_SlotID = new GraphEvent_Value_Int();
        [System.NonSerialized] private int m_ReturnVal;

        #region Property
        [EditorGraphProperty("SlotID", true, EditorGraphPropertyType.EEPT_Int, LabelWidth = 60)]
        public BluePrint_Int SlotID
        {
            get { return m_SlotID; }
            set { m_SlotID = value; }
        }
        #endregion

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_ReturnVal = -1;
            m_SlotID.Init(part, _time);

            if (part.ActionStateMachine.CurUnit is ActionEngine_Entity _Entity)
            {
//                if (!_Entity.DicSlotIDToActionListID.TryGetValue(m_SlotID.value, out m_ReturnVal))
//                {
//                    m_ReturnVal = -1;
//#if UNITY_EDITOR
//                    EngineDebug.LogError($"不存在槽位 [{m_SlotID.value}],请重新检查确认. \n[{EngineDebug.DebugActionStatePart(part)}]");
//#endif
//                }

                bool isFind = false;
                foreach (var item in _Entity.DicSlotIDToActionListID)
                {
                    if(item.Value == m_SlotID.value)
                    {
                        m_ReturnVal = item.Key;
                        isFind = true;
                        break;
                    }
                }

#if UNITY_EDITOR
                if (!isFind)
                {
                    EngineDebug.LogError($"不存在槽位 [{m_SlotID.value}],请重新检查确认. \n[{EngineDebug.DebugActionStatePart(part)}]");
                }
#endif

            }
        }

        public override int value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_TrackData_GetActionGroupIDBySlotID GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_TrackData_GetActionGroupIDBySlotID();
                GraphEventG.SlotID = (BluePrint_Int)m_SlotID.Clone();
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
