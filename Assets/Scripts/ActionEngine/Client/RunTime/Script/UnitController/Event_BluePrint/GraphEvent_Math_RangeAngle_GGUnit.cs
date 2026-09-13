using System;
using System.Collections.Generic;
using AsiTimeLine.RunTime;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Math_RangeAngle_GGUnit : BluePrint_GroupUnit
    {
        [SerializeReference] protected BluePrint_Float m_Distance = new GraphEvent_Value_Float(60f);
        [SerializeReference] protected BluePrint_Bool m_SelfDistance = new GraphEvent_Value_Bool(true);
        [SerializeReference] protected BluePrint_Vector3 m_TargetPos = new GraphEvent_Value_Vector3();
        [SerializeReference] protected BluePrint_GroupUnit m_IntVal = new GraphEvent_GValue_GGUnit();
        [SerializeReference] protected BluePrint_Vector3 m_ReferAxis = new GraphEvent_Value_Vector3();
        [SerializeReference] protected BluePrint_Vector3 m_AxisType = new GraphEvent_Value_Vector3();
        [SerializeReference] protected BluePrint_Bool m_IsDebug = new GraphEvent_Value_Bool(true);
        [SerializeReference] protected BluePrint_Float m_Radius = new GraphEvent_Value_Float(1);
        [SerializeReference] protected BluePrint_Float m_Delay = new GraphEvent_Value_Float(0);
        [SerializeField] protected EVector3 m_DrawColor = new EVector3(1, 0, 0);

        [NonSerialized] private bool m_IsRefer;
        [NonSerialized] private Vector3 m_TargetAxis;
        [NonSerialized] private Color m_Color = Color.white;

        #region Property
        [EditorGraphProperty("GroupPoint", true, EditorGraphPropertyType.EEPT_GGroupUnit, LabelWidth = 60)]
        public BluePrint_GroupUnit IntVal
        {
            get { return m_IntVal; }
            set { m_IntVal = value; }
        }
        [EditorGraphProperty("目标位置", true, EditorGraphPropertyType.EEPT_Enum, LabelWidth = 60)]
        public BluePrint_Vector3 AxisType
        {
            get { return m_AxisType; }
            set { m_AxisType = value; }
        }
        [EditorGraphProperty("目标方向", true, EditorGraphPropertyType.EEPT_Vector3, LabelWidth = 60)]
        public BluePrint_Vector3 TargetPos
        {
            get { return m_TargetPos; }
            set { m_TargetPos = value; }
        }
        [EditorGraphProperty("参考轴", true, EditorGraphPropertyType.EEPT_Vector3, LabelWidth = 60)]
        public BluePrint_Vector3 ReferAxis
        {
            get { return m_ReferAxis; }
            set { m_ReferAxis = value; }
        }
        [EditorGraphProperty("角度范围", true, EditorGraphPropertyType.EEPT_Float, LabelWidth = 60)]
        public BluePrint_Float Distance
        {
            get { return m_Distance; }
            set { m_Distance = value; }
        }

        [EditorGraphProperty("范围内", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 60)]
        public BluePrint_Bool SelfDistance
        {
            get { return m_SelfDistance; }
            set { m_SelfDistance = value; }
        }
        [EditorGraphProperty("(Debug) 是否输出Debug", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 120)]
        public BluePrint_Bool IsDebug
        {
            get { return m_IsDebug; }
            set { m_IsDebug = value; }
        }
        [EditorGraphProperty("(Debug) RGB", false, EditorGraphPropertyType.EEPT_Color, LabelWidth = 80)]
        public EVector3 DrawColor
        {
            get { return m_DrawColor; }
            set { m_DrawColor = value; }
        }
        [EditorGraphProperty("(Debug) 半径", true, EditorGraphPropertyType.EEPT_Float, LabelWidth = 120)]
        public BluePrint_Float Radius
        {
            get { return m_Radius; }
            set { m_Radius = value; }
        }
        [EditorGraphProperty("(Debug) Life", true, EditorGraphPropertyType.EEPT_Float, LabelWidth = 120)]
        public BluePrint_Float Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; }
        }
        #endregion

        [System.NonSerialized] private List<ActionEngine_Unit> m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能
            m_ReturnVal = EngineResourcesManager.Instance.CreateUnits();
            m_IntVal.Init(part, _time);
            m_ReturnVal.AddRange(m_IntVal.value);
            Distance.Init(part, _time);
            m_TargetPos.Init(part, _time);
            m_SelfDistance.Init(part, _time);
            m_ReferAxis.Init(part, _time);
            AxisType.Init(part, _time);

            m_IsRefer = m_ReferAxis.value.sqrMagnitude > 0.000001f;
#if UNITY_EDITOR
            IsDebug.Init(part, _time);
            if (IsDebug.value)
            {
                Delay.Init(part, _time);
                Radius.Init(part, _time);
                m_Color.r = DrawColor.x;
                m_Color.g = DrawColor.y;
                m_Color.b = DrawColor.z;
                m_Color.a = 0.2f;
                Vector3 _axis = m_IsRefer ? m_ReferAxis.value : Vector3.up;

                EngineDebug.DrawSolidArc(AxisType.value, _axis,
                    m_TargetPos.value, Distance.value * 0.5f, Radius.value, m_Color, Delay.value);
                EngineDebug.DrawSolidArc(AxisType.value, _axis,
                    m_TargetPos.value, Distance.value * -0.5f, Radius.value, m_Color, Delay.value);
            }
#endif
            part.ActionStateMachine.TryGetStaticLogic(out Ex_MathFuntion _Mathf, nameof(Ex_MathFuntion));

            if (m_IsRefer)
            {
                for (int i = m_ReturnVal.Count - 1; i >= 0; i--)
                {
                    m_TargetAxis = m_ReturnVal[i].transform.position - AxisType.value;
                    bool _self = _Mathf.SelfAngle(m_TargetPos.value, m_TargetAxis, Distance.value, m_ReferAxis.value);
                    if (_self != m_SelfDistance.value)
                    {
                        m_ReturnVal.RemoveAt(i);
                    }//剔除
                }
            }
            else
            {
                for (int i = m_ReturnVal.Count - 1; i >= 0; i--)
                {
                    m_TargetAxis = m_ReturnVal[i].transform.position - AxisType.value;
                    bool _self = _Mathf.SelfAngle(m_TargetPos.value, m_TargetAxis, Distance.value);
                    if (_self != m_SelfDistance.value)
                    {
                        m_ReturnVal.RemoveAt(i);
                    }//剔除
                }
            }
        }
        public override List<ActionEngine_Unit> value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_RangeAngle_GGUnit GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_RangeAngle_GGUnit();
                GraphEventG.SelfDistance = (BluePrint_Bool)m_SelfDistance.Clone();
                GraphEventG.Distance = (BluePrint_Float)m_Distance.Clone();
                GraphEventG.IntVal = (BluePrint_GroupUnit)m_IntVal.Clone();
                GraphEventG.TargetPos = (BluePrint_Vector3)m_TargetPos.Clone();
                GraphEventG.ReferAxis = (BluePrint_Vector3)m_ReferAxis.Clone();
                GraphEventG.AxisType = (BluePrint_Vector3)m_AxisType.Clone();
                GraphEventG.IsDebug = (BluePrint_Bool)m_IsDebug.Clone();
                GraphEventG.Delay = (BluePrint_Float)m_Delay.Clone();
                GraphEventG.Radius = (BluePrint_Float)m_Radius.Clone();
                GraphEventG.DrawColor = m_DrawColor;
                GraphEventG.IsNode = IsNode;
                //在保存好文件后重置状态
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