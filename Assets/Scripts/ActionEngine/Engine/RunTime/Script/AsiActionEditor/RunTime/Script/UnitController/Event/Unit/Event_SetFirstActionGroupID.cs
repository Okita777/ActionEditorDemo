using System.Collections.Generic;
using AsiActionEngine.RunTime.Graph;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_SetFirstActionGroupID : IActionEventData
    {
        [SerializeField] protected GraphEvent_NoValue_Int m_FirstActionGroupID = CreateDefaultFirstActionGroupID();
        [SerializeField] protected bool m_EnterWrite = true;
        [SerializeField] protected bool m_ExitRestore = true;

        [EditorProperty("目标ActionGroupID", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Int FirstActionGroupID
        {
            get => m_FirstActionGroupID;
            set => m_FirstActionGroupID = value;
        }

        [EditorProperty("进入时写入", EditorPropertyType.EEPT_Bool, LabelWidth = 120)]
        public bool EnterWrite
        {
            get => m_EnterWrite;
            set => m_EnterWrite = value;
        }

        [EditorProperty("退出时恢复", EditorPropertyType.EEPT_Bool, LabelWidth = 120)]
        public bool ExitRestore
        {
            get => m_ExitRestore;
            set => m_ExitRestore = value;
        }

        public int GetEvenType() => -(int)EEvenTypeInternal.EET_SetFirstActionGroupID;
        public IActionEventData Creact() => new Event_SetFirstActionGroupID();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            if (!m_EnterWrite) return;

            ActionStateMachine stateMachine = _actionState?.ActionStateMachine;
            if (stateMachine == null) return;

            int actionGroupID = m_FirstActionGroupID.value(_actionState, EngineResourcesManager.Instance.MachineTime);
            stateMachine.SetFirstActionGroupID(actionGroupID);
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            if (!m_ExitRestore) return;

            ActionStateMachine stateMachine = _actionState?.ActionStateMachine;
            if (stateMachine == null) return;

            stateMachine.SetFirstActionGroupID(-1);
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SetFirstActionGroupID eventData = _eventData as Event_SetFirstActionGroupID;
            eventData.m_FirstActionGroupID = m_FirstActionGroupID.Clone();
            eventData.m_EnterWrite = m_EnterWrite;
            eventData.m_ExitRestore = m_ExitRestore;
            return eventData;
        }

        private static GraphEvent_NoValue_Int CreateDefaultFirstActionGroupID()
        {
            return new GraphEvent_NoValue_Int()
            {
                BluePrint_Val = new GraphEvent_LocalParam_Read_Int() { ParamIndex = 0 },
                LocalIntParams = new List<int>() { -1 },
#if UNITY_EDITOR
                m_NodeEdiData = new NodeEdiData()
                {
                    nodeTitle = "目标ActionGroupID",
                    nodeToolTip = "返回值会写入状态机的优先ActionGroupID",
                    localParams = new List<BluePrintLocalParamDef>()
                    {
                        new BluePrintLocalParamDef
                        {
                            paramName = "目标ActionGroupID",
                            paramType = EBluePrintLocalParamType.Int,
                            drawToInspector = true
                        },
                    }
                }
#endif
            };
        }
    }
}
