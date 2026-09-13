using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Math_DebugGValue_GroupTransform : BluePrint_GroupTransform
    {
        [SerializeReference] protected BluePrint_GroupTransform m_ReadValue = new GraphEvent_GValue_GGTransform();
        [SerializeReference] protected BluePrint_Bool m_IsDebug = new GraphEvent_Value_Bool(true);
        [SerializeReference] protected BluePrint_Float m_Radius = new GraphEvent_Value_Float(1);
        [SerializeReference] protected BluePrint_Float m_Delay = new GraphEvent_Value_Float(0);
        [SerializeField] protected EVector3 m_DrawColor = new EVector3(1, 0, 0);
        #region Property

        [EditorGraphProperty("ReadValue(GrouTrans)", true, EditorGraphPropertyType.EEPT_GGroupTransform, LabelWidth = 150)]
        public BluePrint_GroupTransform ReadValue
        {
            get { return m_ReadValue; }
            set { m_ReadValue = value; }
        }
        [EditorGraphProperty("是否输出Debug", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 120)]
        public BluePrint_Bool IsDebug
        {
            get { return m_IsDebug; }
            set { m_IsDebug = value; }
        }
        [EditorGraphProperty("RGB", false, EditorGraphPropertyType.EEPT_Color, LabelWidth = 80)]
        public EVector3 DrawColor
        {
            get { return m_DrawColor; }
            set { m_DrawColor = value; }
        }
        [EditorGraphProperty("半径", true, EditorGraphPropertyType.EEPT_Float, LabelWidth = 120)]
        public BluePrint_Float Radius
        {
            get { return m_Radius; }
            set { m_Radius = value; }
        }
        [EditorGraphProperty("Life(s)", true, EditorGraphPropertyType.EEPT_Float, LabelWidth = 120)]
        public BluePrint_Float Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; }
        }
        #endregion

        [System.NonSerialized] private List<Transform> m_ReturnVal;
        //public override void SetListSource(List<Transform> _list)
        //{
        //    m_ReturnVal = _list;
        //}
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            //m_ReadValue.SetListSource(m_ReturnVal);
            m_ReadValue.Init(part, _time);
            m_ReturnVal = m_ReadValue.value;

#if UNITY_EDITOR
            Radius.Init(part, _time);
            Delay.Init(part, _time);

            Color _color = new Color(DrawColor.x, DrawColor.y, DrawColor.z, 1);
            foreach (Transform p in m_ReturnVal)
            {
                EngineDebug.DrawSphere(p.position, Radius.value, _color, Delay.value);
                EngineDebug.DrawLine(p.position, p.position + p.rotation * Vector3.forward * Radius.value, _color, Delay.value);
            }
#endif
        }
        public override List<Transform> value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_DebugGValue_GroupTransform GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_DebugGValue_GroupTransform();
                GraphEventG.ReadValue = (BluePrint_GroupTransform)m_ReadValue.Clone();
                GraphEventG.IsDebug = (BluePrint_Bool)m_IsDebug.Clone();
                GraphEventG.Radius = (BluePrint_Float)m_Radius.Clone();
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