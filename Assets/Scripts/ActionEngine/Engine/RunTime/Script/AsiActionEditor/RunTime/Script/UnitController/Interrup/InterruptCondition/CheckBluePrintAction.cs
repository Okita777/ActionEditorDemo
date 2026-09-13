using System.Collections.Generic;
using AsiActionEngine.RunTime.Graph;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class CheckBluePrintAction : IInterruptCondition
    {
        [SerializeField] protected GraphEvent_NoValue_Int m_TargetActionID = CreateDefaultTargetActionID();
        [SerializeField] protected bool m_UseOriginalActionWhenReplaceFailed = true;

        #region Property

        [EditorProperty("目标ActionID", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Int TargetActionID
        {
            get => m_TargetActionID;
            set => m_TargetActionID = value;
        }

        [EditorProperty("替换失败使用原Action", EditorPropertyType.EEPT_Bool, LabelWidth = 120)]
        public bool UseOriginalActionWhenReplaceFailed
        {
            get => m_UseOriginalActionWhenReplaceFailed;
            set => m_UseOriginalActionWhenReplaceFailed = value;
        }

        #endregion

        public int InterruptType => -(int)EInterruptTypeInternal.EIT_BluePrintAction;

        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            int targetActionID = m_TargetActionID.value(actionStatePart, new ActionMachineTime());
            if (actionStatePart.TryOverrideInterruptTargetAction(targetActionID))
            {
                return true;
            }

            if (m_UseOriginalActionWhenReplaceFailed)
            {
                actionStatePart.UseOriginalInterruptTargetAction();
            }

            return m_UseOriginalActionWhenReplaceFailed;
        }

        public IInterruptCondition Clone()
        {
            CheckBluePrintAction clone = new CheckBluePrintAction();
            clone.TargetActionID = m_TargetActionID.Clone();
            clone.UseOriginalActionWhenReplaceFailed = m_UseOriginalActionWhenReplaceFailed;
            return clone;
        }

        private static GraphEvent_NoValue_Int CreateDefaultTargetActionID()
        {
            return new GraphEvent_NoValue_Int()
            {
                BluePrint_Val = new GraphEvent_LocalParam_Read_Int() { ParamIndex = 0 },
                LocalIntParams = new List<int>() { -1 },
#if UNITY_EDITOR
                m_NodeEdiData = new NodeEdiData()
                {
                    nodeTitle = "目标ActionID",
                    nodeToolTip = "返回值会作为本次跳转轨的目标ActionID",
                    localParams = new List<BluePrintLocalParamDef>()
                    {
                        new BluePrintLocalParamDef
                        {
                            paramName = "目标ActionID",
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
