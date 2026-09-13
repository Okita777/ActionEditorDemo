using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Math_DebugGValue_Vector3 : BluePrint_Vector3
    {
        [SerializeReference] protected BluePrint_Vector3 m_ReadValue = new GraphEvent_Value_Vector3();
        [SerializeReference] protected BluePrint_Bool m_IsDraw = new GraphEvent_Value_Bool(true);
        [SerializeReference] protected BluePrint_Float m_Radius = new GraphEvent_Value_Float(1);
        [SerializeReference] protected BluePrint_Float m_Delay = new GraphEvent_Value_Float(0);
        [SerializeReference] protected BluePrint_Bool m_IsDebug = new GraphEvent_Value_Bool(true);

        [SerializeField] protected EVector3 m_DrawColor = new EVector3(1, 0, 0);
        [SerializeField] protected string m_DebugName = "蓝图类型Vector";
        [SerializeField] protected int m_FontSize = 22;
        [SerializeField] protected byte m_DebugGruphType;

        #region Property

        [EditorGraphProperty("ReadValue(V3)", true, EditorGraphPropertyType.EEPT_Int, LabelWidth = 120)]
        public BluePrint_Vector3 ReadValue
        {
            get { return m_ReadValue; }
            set { m_ReadValue = value; }
        }
        [EditorGraphProperty("是否输出Debug", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 120)]
        public BluePrint_Bool IsDebug
        {
            get
            {
                if (m_IsDebug is null) m_IsDebug = new GraphEvent_Value_Bool(true);
                return m_IsDebug;
            }
            set { m_IsDebug = value; }
        }
        [EditorGraphProperty("DebugName", false, EditorGraphPropertyType.EEPT_String, LabelWidth = 80)]
        public string DebugName
        {
            get { return m_DebugName; }
            set { m_DebugName = value; }
        }
        [EditorGraphProperty("绘制到视图", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 80)]
        public BluePrint_Bool IsDraw
        {
            get { return m_IsDraw; }
            set { m_IsDraw = value; }
        }
        [EditorGraphProperty("绘制类型", false, EditorGraphPropertyType.EEPT_EnumCustom, LabelWidth = 80, EnumNames = new[] { "绘制点", "绘制朝向" })]
        public byte DebugGruphType
        {
            get { return m_DebugGruphType; }
            set { m_DebugGruphType = value; }
        }
        [EditorGraphProperty("半径", true, EditorGraphPropertyType.EEPT_Float, LabelWidth = 80)]
        public BluePrint_Float Radius
        {
            get { return m_Radius; }
            set { m_Radius = value; }
        }
        [EditorGraphProperty("字体大小", false, EditorGraphPropertyType.EEPT_Int, LabelWidth = 80)]
        public int FontSize
        {
            get { return m_FontSize; }
            set { m_FontSize = value; }
        }
        [EditorGraphProperty("RGB", false, EditorGraphPropertyType.EEPT_Color, LabelWidth = 80)]
        public EVector3 DrawColor
        {
            get { return m_DrawColor; }
            set { m_DrawColor = value; }
        }
        [EditorGraphProperty("Life(s)", true, EditorGraphPropertyType.EEPT_Float, LabelWidth = 80)]
        public BluePrint_Float Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; }
        }
        #endregion

        [System.NonSerialized] private Vector3 m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_ReadValue.Init(part, _time);
            m_ReturnVal = m_ReadValue.value;

#if UNITY_EDITOR
            IsDebug.Init(part, _time);
            if (IsDebug.value)
            {
                m_IsDraw.Init(part, _time);
                m_Radius.Init(part, _time);
                Delay.Init(part, _time);
                if (m_IsDraw.value)
                {
                    Vector3 _c = DrawColor.GetValue();
                    Color _color = new Color(_c.x, _c.y, _c.z, 1);

                    if (DebugGruphType == 0)
                    {
                        //绘制点
                        EngineScenceDraw.Sphere(m_ReturnVal, Quaternion.identity, m_Radius.value, _color, Delay.value);
                        if (!string.IsNullOrEmpty(m_DebugName))
                            EngineScenceDraw.Text(m_ReturnVal, m_DebugName, m_FontSize, _color, Delay.value);
                    }
                    else
                    {
                        //绘制朝向
                        //Debug.DrawRay
                        Vector3 mStart;
                        if (part.IsTem)
                            mStart = part.Pos;
                        else
                            mStart = part.ActionStateMachine.CurUnit.transform.position;
                        Vector3 mEnd = mStart + m_ReturnVal * m_Radius.value;

                        EngineScenceDraw.Line(mStart, mEnd, _color, Delay.value);
                        EngineScenceDraw.WireDisc(mStart, Vector3.up, m_Radius.value, _color, Delay.value);
                        if (!string.IsNullOrEmpty(m_DebugName))
                            EngineScenceDraw.Text(mStart, m_DebugName, m_FontSize, _color, Delay.value);
                    }

                }
                else
                {
                    EngineDebug.Log($"蓝图Debug“ 【{m_DebugName}】 : {m_ReadValue.value}\nAction: {part.CurrentActionState.Name}   Hash:[{part.ActionStateMachine.GetHashCode()}]  Obj:[{part.ActionStateMachine.CurUnit.gameObject}]");
                }
            }
#endif
        }
        public override Vector3 value => m_ReturnVal;


#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_DebugGValue_Vector3 GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_DebugGValue_Vector3();
                GraphEventG.ReadValue = (BluePrint_Vector3)m_ReadValue.Clone();
                GraphEventG.IsDraw = (BluePrint_Bool)m_IsDraw.Clone();
                GraphEventG.IsDebug = (BluePrint_Bool)m_IsDebug.Clone();

                GraphEventG.Radius = (BluePrint_Float)m_Radius.Clone();
                GraphEventG.Delay = (BluePrint_Float)m_Delay.Clone();
                GraphEventG.DrawColor = m_DrawColor;
                GraphEventG.DebugName = m_DebugName;
                GraphEventG.FontSize = m_FontSize;
                GraphEventG.DebugGruphType = m_DebugGruphType;
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