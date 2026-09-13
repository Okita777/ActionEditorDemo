using System;
using System.Collections.Generic;
using AsiTimeLine.RunTime;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Math_RangeDis_GGUnit : BluePrint_GroupUnit
    {
        [SerializeReference] protected BluePrint_Float m_Distance = new GraphEvent_Value_Float(2.0f);
        [SerializeReference] protected BluePrint_Bool m_SelfDistance = new GraphEvent_Value_Bool(true);
        [SerializeReference] protected BluePrint_Vector3 m_TargetPos = new GraphEvent_Value_Vector3();
        [SerializeReference] protected BluePrint_GroupUnit m_IntVal = new GraphEvent_GValue_GGUnit();
        [SerializeReference] protected BluePrint_Bool m_IsDebug = new GraphEvent_Value_Bool(true);
        [SerializeReference] protected BluePrint_Float m_Delay = new GraphEvent_Value_Float(0);
        [SerializeField] protected EVector3 m_DrawColor = new EVector3(1, 0, 0);
        [SerializeField] protected byte m_IgoneType = 0;

        [NonSerialized] protected bool m_IgoneAxisX = false;
        [NonSerialized] protected bool m_IgoneAxisY = false;
        [NonSerialized] protected bool m_IgoneAxisZ = false;
        #region Property
        [EditorGraphProperty("GroupUnit", true, EditorGraphPropertyType.EEPT_GGroupUnit, LabelWidth = 60)]
        public BluePrint_GroupUnit IntVal
        {
            get { return m_IntVal; }
            set { m_IntVal = value; }
        }
        [EditorGraphProperty("目标位置", true, EditorGraphPropertyType.EEPT_Vector3, LabelWidth = 60)]
        public BluePrint_Vector3 TargetPos
        {
            get { return m_TargetPos; }
            set { m_TargetPos = value; }
        }

        [EditorGraphProperty("范围距离(m)", true, EditorGraphPropertyType.EEPT_Float, LabelWidth = 80)]
        public BluePrint_Float Distance
        {
            get { return m_Distance; }
            set { m_Distance = value; }
        }
        [EditorGraphProperty("忽略轴", false, EditorGraphPropertyType.EEPT_EnumCustom,
            EnumNames = new[] { "无忽略", "忽略X轴", "忽略Y轴", "忽略Z轴" }, LabelWidth = 60)]
        public byte IgoneType
        {
            get { return m_IgoneType; }
            set { m_IgoneType = value; }
        }
        [EditorGraphProperty("范围内", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 60)]
        public BluePrint_Bool SelfDistance
        {
            get { return m_SelfDistance; }
            set { m_SelfDistance = value; }
        }
        [EditorGraphProperty("(Debug) 是否输出Debug", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 150)]
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
        [EditorGraphProperty("(Debug) Life", true, EditorGraphPropertyType.EEPT_Float, LabelWidth = 120)]
        public BluePrint_Float Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; }
        }
        #endregion

        [System.NonSerialized] private List<ActionEngine_Unit> m_ReturnVal;
        [System.NonSerialized] private Color m_Color = Color.white;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能
            m_ReturnVal = EngineResourcesManager.Instance.CreateUnits();
            m_IntVal.Init(part, _time);
            m_ReturnVal.AddRange(m_IntVal.value);
            Distance.Init(part, _time);
            m_TargetPos.Init(part, _time);
            m_SelfDistance.Init(part, _time);
#if UNITY_EDITOR
            IsDebug.Init(part, _time);
            if (IsDebug.value)
            {
                Delay.Init(part, _time);
                m_Color.r = DrawColor.x;
                m_Color.g = DrawColor.y;
                m_Color.b = DrawColor.z;
                m_Color.a = 1;
                EngineDebug.DrawSphere(m_TargetPos.value, Distance.value, m_Color, Delay.value);
            }
#endif
            m_IgoneAxisX = false;
            m_IgoneAxisY = false;
            m_IgoneAxisZ = false;
            if (IgoneType == 1) m_IgoneAxisX = true;
            if (IgoneType == 2) m_IgoneAxisY = true;
            if (IgoneType == 3) m_IgoneAxisZ = true;

            part.ActionStateMachine.TryGetStaticLogic(out Ex_MathFuntion _Mathf, nameof(Ex_MathFuntion));
            for (int i = m_ReturnVal.Count - 1; i >= 0; i--)
            {
                bool _self = _Mathf.SelfDistance(m_TargetPos.value, m_ReturnVal[i].transform.position, Distance.value, m_IgoneAxisX, m_IgoneAxisY, m_IgoneAxisZ);
                if (_self != m_SelfDistance.value)
                {
                    m_ReturnVal.RemoveAt(i);
                }//剔除
            }
        }
        public override List<ActionEngine_Unit> value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_RangeDis_GGUnit GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_RangeDis_GGUnit();
                GraphEventG.SelfDistance = (BluePrint_Bool)m_SelfDistance.Clone();
                GraphEventG.Distance = (BluePrint_Float)m_Distance.Clone();
                GraphEventG.IntVal = (BluePrint_GroupUnit)m_IntVal.Clone();
                GraphEventG.TargetPos = (BluePrint_Vector3)m_TargetPos.Clone();
                GraphEventG.IgoneType = m_IgoneType;

                GraphEventG.IsDebug = (BluePrint_Bool)m_IsDebug.Clone();
                GraphEventG.Delay = (BluePrint_Float)m_Delay.Clone();
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