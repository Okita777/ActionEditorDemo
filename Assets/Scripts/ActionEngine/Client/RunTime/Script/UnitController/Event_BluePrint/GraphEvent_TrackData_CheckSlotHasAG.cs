using AsiTimeLine.RunTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_TrackData_CheckSlotHasAG : BluePrint_Bool
    {
        [SerializeReference] protected BluePrint_Int m_SlotID = new GraphEvent_Value_Int();
        [System.NonSerialized] private bool m_ReturnVal;

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

            m_ReturnVal = false;
            m_SlotID.Init(part, _time);
            if(part.ActionStateMachine.CurUnit is ActionEngine_Entity _Entity)
            {
                //m_ReturnVal = _Entity.DicSlotIDToActionListID.ContainsKey(m_SlotID.value);
                //foreach (var item in _Entity.DicSlotIDToActionListID)
                //{
                //    if (item.Value == m_SlotID.value)
                //    {
                //        m_ReturnVal = true;
                //        break;
                //    }
                //}
                m_ReturnVal = _Entity.DicSlotIDToActionListID.ContainsValue(m_SlotID.value);
            }
        }

        public override bool value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_TrackData_CheckSlotHasAG GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_TrackData_CheckSlotHasAG();
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
