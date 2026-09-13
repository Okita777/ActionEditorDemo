using System.Collections.Generic;
using AsiActionEngine.RunTime.Graph;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    /// <summary>
    /// 定时器跳转条件：跳转轨进入判定窗口时立即通过一次，随后每隔[间隔时间]毫秒再通过一帧。
    /// 计时在 Enter 时重置，每次重新进入窗口(切换Action、有限窗口轨在Action循环中再次到来)都会重新触发首次通过；
    /// 无限窗口轨(Duration &lt; 0)在Action循环回绕时窗口不中断，计时连续不重置。
    /// 计时按帧累加 ActionStatePart.DeltaTime，受状态机 TimeScale 影响(顿帧/慢动作期间计时同步变慢)；
    /// 轨道离开判定窗口期间不累计。
    /// 间隔为蓝图 Int，可由 GValue 驱动；间隔 &lt;= 0 时只保留 Enter 的首次通过。
    /// 本条件依赖 Enter/Update/Exit 生命周期，只能挂在常规跳转轨(含跳转组)上；
    /// 受击/命中跳转轨不驱动生命周期，挂在其上恒不通过。
    /// </summary>
    [System.Serializable]
    public class CheckJumpTimer : IInterruptCondition
    {
        [SerializeField] protected GraphEvent_NoValue_Int m_Interval = CreateDefaultInterval();

        [System.NonSerialized] private bool m_IsEntered;//是否处于判定窗口内，由 Enter/Exit 维护
        [System.NonSerialized] private float m_Timer;//自上次通过起累计的时长(毫秒)
        [System.NonSerialized] private bool m_IsTick;//本帧是否为通过帧，由 Enter/Update 结算，CheckInterrupt 只读

        #region Property

        [EditorProperty("间隔时间(毫秒)", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Int Interval
        {
            get => m_Interval;
            set => m_Interval = value;
        }
        #endregion

        public int InterruptType => -(int)EInterruptTypeInternal.EIT_CheckJumpTimer;

        //计时状态记在实例上，必须让每条跳转轨持有独立实例
        public bool NeedRuntimeState => true;

        //初始化计时并立即通过一次
        public void Enter(ActionStatePart actionStatePart, ActionInterrupt interrupt, bool _isSingle)
        {
            m_IsEntered = true;
            m_Timer = 0f;
            m_IsTick = true;
        }

        //按帧累计时长，攒满一个间隔就通过一帧并扣除该间隔(保留余量，避免长期漂移)
        public void Update(ActionStatePart actionStatePart, ActionInterrupt interrupt)
        {
            m_Timer += actionStatePart.DeltaTime;

            int _interval = m_Interval.value(actionStatePart, new ActionMachineTime());
            if (_interval > 0 && m_Timer >= _interval)
            {
                m_Timer -= _interval;
                m_IsTick = true;
                return;
            }

            m_IsTick = false;
        }

        public void Exit(ActionStatePart actionStatePart, ActionInterrupt interrupt, bool _isInterrupt)
        {
            m_IsEntered = false;
            m_IsTick = false;
            m_Timer = 0f;
        }

        /// <summary>
        /// 只读本帧结算结果，无副作用：同一帧内被权重预评估与主判定重复调用时结果一致。
        /// </summary>
        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            return m_IsEntered && m_IsTick;
        }

        public IInterruptCondition Clone()
        {
            CheckJumpTimer clone = new CheckJumpTimer();
            clone.m_Interval = m_Interval.Clone();
            return clone;
        }

        private static GraphEvent_NoValue_Int CreateDefaultInterval()
        {
            return new GraphEvent_NoValue_Int()
            {
                BluePrint_Val = new GraphEvent_LocalParam_Read_Int() { ParamIndex = 0 },
                LocalIntParams = new List<int>() { 1000 },
#if UNITY_EDITOR
                m_NodeEdiData = new NodeEdiData()
                {
                    nodeTitle = "间隔时间",
                    nodeToolTip = "定时器间隔(毫秒)，每经过该时长通过一帧",
                    localParams = new List<BluePrintLocalParamDef>()
                    {
                        new BluePrintLocalParamDef
                        {
                            paramName = "间隔(毫秒)",
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
