using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_D_RandomNumber : BluePrint_Float
    {
        [SerializeReference] protected BluePrint_Float m_BluePrint_val_l = new GraphEvent_Value_Float();
        [SerializeReference] protected BluePrint_Float m_BluePrint_val_r = new GraphEvent_Value_Float();
        [SerializeField] protected bool m_OnlyInit = false;
        // [SerializeReference] protected BluePrint_Bool m_Select = new GraphEvent_Value_Bool();

        #region Property
        [EditorGraphProperty("最小", true, EditorGraphPropertyType.EEPT_Float)]
        public BluePrint_Float BluePrint_val_l
        {
            get { return m_BluePrint_val_l; }
            set { m_BluePrint_val_l = value; }
        }

        [EditorGraphProperty("最大", true, EditorGraphPropertyType.EEPT_Float)]
        public BluePrint_Float BluePrint_val_r
        {
            get { return m_BluePrint_val_r; }
            set { m_BluePrint_val_r = value; }
        }
        [EditorGraphProperty("每次获取时随机", false, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 100)]
        public bool OnlyInit
        {
            get { return m_OnlyInit; }
            set { m_OnlyInit = value; }
        }
        // [EditorGraphProperty("Select", true, EditorGraphPropertyType.EEPT_Bool)]
        // public BluePrint_Bool Select
        // {
        //     get { return m_Select; }
        //     set { m_Select = value; }
        // }
        #endregion

        [System.NonSerialized] private float m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (!OnlyInit && part.ActionStateMachine.BluePrintIsUse(this)) return;

            // m_Select.Init(part, _time);
            // if (m_Select.value)
            // {
            //     m_BluePrint_val_l.Init(part, _time);
            //     m_ReturnVal = m_BluePrint_val_l.value;
            // }
            // else
            // {
            //     m_BluePrint_val_r.Init(part, _time);
            //     m_ReturnVal = m_BluePrint_val_r.value;
            // }
            m_BluePrint_val_l.Init(part, _time);
            m_BluePrint_val_r.Init(part, _time);
            m_ReturnVal = Random.Range(m_BluePrint_val_l.value, m_BluePrint_val_r.value);
        }

        public override float value => m_ReturnVal;
#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_D_RandomNumber _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Math_D_RandomNumber();
                _graphEvent.BluePrint_val_l = (BluePrint_Float)m_BluePrint_val_l.Clone();
                // _graphEvent.Select = (BluePrint_Bool)m_Select.Clone();
                _graphEvent.BluePrint_val_r = (BluePrint_Float)m_BluePrint_val_r.Clone();
                _graphEvent.OnlyInit = m_OnlyInit;
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