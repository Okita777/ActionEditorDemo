using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Comp_Int : BluePrint_Bool
    {
        [SerializeReference] protected BluePrint_Int m_BluePrint_val_l = new GraphEvent_Value_Int();
        [SerializeField] protected byte m_Equal = 0;
        [SerializeReference] protected BluePrint_Int m_BluePrint_val_r = new GraphEvent_Value_Int();

        #region Property
        [EditorGraphProperty("Int", true, EditorGraphPropertyType.EEPT_Int, OnCheckGEnum = true)]
        public BluePrint_Int BluePrint_val_l
        {
            get { return m_BluePrint_val_l; }
            set { m_BluePrint_val_l = value; }
        }
        [EditorGraphProperty("", false, EditorGraphPropertyType.EEPT_EnumCustom, LabelWidth = 5, EnumNames = new string[] { "==", "!=", ">=", "<=", ">", "<" })]
        public byte Equal
        {
            get { return m_Equal; }
            set { m_Equal = value; }
        }
        [EditorGraphProperty("Int", true, EditorGraphPropertyType.EEPT_Int)]
        public BluePrint_Int BluePrint_val_r
        {
            get { return m_BluePrint_val_r; }
            set { m_BluePrint_val_r = value; }
        }
        #endregion

        [System.NonSerialized] private bool m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_BluePrint_val_l.Init(part, _time);
            m_BluePrint_val_r.Init(part, _time);
            if (m_Equal == 0)
            {
                m_ReturnVal = m_BluePrint_val_l.value == m_BluePrint_val_r.value;
            }
            else if (m_Equal == 1)
            {
                m_ReturnVal = m_BluePrint_val_l.value != m_BluePrint_val_r.value;
            }
            else if (m_Equal == 2)
            {
                m_ReturnVal = m_BluePrint_val_l.value >= m_BluePrint_val_r.value;

            }
            else if (m_Equal == 3)
            {
                m_ReturnVal = m_BluePrint_val_l.value <= m_BluePrint_val_r.value;

            }
            else if (m_Equal == 4)
            {
                m_ReturnVal = m_BluePrint_val_l.value > m_BluePrint_val_r.value;

            }
            else if (m_Equal == 5)
            {
                m_ReturnVal = m_BluePrint_val_l.value < m_BluePrint_val_r.value;

            }
            // m_ReturnVal = (m_BluePrint_val_l.value == m_BluePrint_val_r.value) == (m_Equal == 0);
        }

        public override bool value => m_ReturnVal;
#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_Comp_Int _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Math_Comp_Int();
                _graphEvent.BluePrint_val_l = (BluePrint_Int)m_BluePrint_val_l.Clone();
                _graphEvent.Equal = m_Equal;
                _graphEvent.BluePrint_val_r = (BluePrint_Int)m_BluePrint_val_r.Clone();
                //在保存好文件后重置状态
                ActionSaveFlishEvent.ActionEvent.AddListener(() =>
                {
                    _graphEvent = null;
                });
            }
            return _graphEvent;
#endif
            return this;
        }
    }
}