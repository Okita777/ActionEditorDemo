using System.Collections.Generic;
using AsiActionEngine.RunTime.Graph;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    /// <summary>
    /// 跳转权重条件：在同一帧内，所有"权重条件之前的条件均通过"的跳转轨进入竞争池，
    /// 按各自权重做归一化加权随机，仅被选中的一条在权重条件处判为通过，随后继续判定其后的条件。
    /// 勾选 OnlyFalse 的轨照常参与竞争、消耗权重份额，但永远不会跳转。
    /// 是否"被选中"的门控集中在 ActionStatePart.CheckInterrupCondition，
    /// 本条件的 CheckInterrupt 仅作占位，不直接决定通过。
    /// </summary>
    [System.Serializable]
    public class CheckJumpWeight : IInterruptCondition
    {
        [SerializeField] protected GraphEvent_NoValue_Int m_Weight = CreateDefaultWeight();
        [SerializeField] protected bool m_OnlyFalse = false;

        #region Property

        [EditorProperty("权重", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Int Weight
        {
            get => m_Weight;
            set => m_Weight = value;
        }
        [EditorProperty("始终false(仅占权重不跳转)", EditorPropertyType.EEPT_Bool, LabelWidth = 150)]
        public bool OnlyFalse
        {
            get => m_OnlyFalse;
            set => m_OnlyFalse = value;
        }
        #endregion

        public int InterruptType => -(int)EInterruptTypeInternal.EIT_CheckJumpWeight;

        // 权重轨的真正门控在 CheckInterrupCondition 中按"自上而下"短路处理，这里只作占位。
        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart) => !OnlyFalse;

        /// <summary>求当前权重。负值夹紧为 0。time 必须是所属跳转轨的真实时间上下文。</summary>
        public int GetWeight(ActionStatePart part, ActionMachineTime time) => Mathf.Max(0, m_Weight.value(part, time));

        public IInterruptCondition Clone()
        {
            CheckJumpWeight clone = new CheckJumpWeight();
            clone.m_Weight = m_Weight.Clone();
            clone.m_OnlyFalse = m_OnlyFalse;
            return clone;
        }

        private static GraphEvent_NoValue_Int CreateDefaultWeight()
        {
            return new GraphEvent_NoValue_Int()
            {
                BluePrint_Val = new GraphEvent_LocalParam_Read_Int() { ParamIndex = 0 },
                LocalIntParams = new List<int>() { 1 },
#if UNITY_EDITOR
                m_NodeEdiData = new NodeEdiData()
                {
                    nodeTitle = "权重",
                    nodeToolTip = "正整数权重值，越大被选中概率越高",
                    localParams = new List<BluePrintLocalParamDef>()
                    {
                        new BluePrintLocalParamDef
                        {
                            paramName = "权重",
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
