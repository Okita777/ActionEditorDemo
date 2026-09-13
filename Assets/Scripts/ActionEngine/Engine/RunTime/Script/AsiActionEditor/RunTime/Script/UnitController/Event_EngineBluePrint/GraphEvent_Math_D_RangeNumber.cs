using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_D_RangeNumber : BluePrint_Bool
    {
        [SerializeReference] protected BluePrint_Float m_BluePrint_val = new GraphEvent_Value_Float();
        [SerializeReference] protected BluePrint_Bool m_BluePrint_bool = new GraphEvent_Value_Bool();
        [SerializeReference] protected BluePrint_Float m_BluePrint_val_l = new GraphEvent_Value_Float();
        [SerializeReference] protected BluePrint_Float m_BluePrint_val_r = new GraphEvent_Value_Float();

        #region Property
        [EditorGraphProperty("判定值", true, EditorGraphPropertyType.EEPT_Float)]
        public BluePrint_Float BluePrint_val
        {
            get { return m_BluePrint_val; }
            set { m_BluePrint_val = value; }
        }
        [EditorGraphProperty("范围内", true, EditorGraphPropertyType.EEPT_Bool)]
        public BluePrint_Bool BluePrint_bool
        {
            get { return m_BluePrint_bool; }
            set { m_BluePrint_bool = value; }
        }
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

        #endregion

        [System.NonSerialized] private bool m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_BluePrint_val.Init(part, _time);
            m_BluePrint_bool.Init(part, _time);
            m_BluePrint_val_l.Init(part, _time);
            m_BluePrint_val_r.Init(part, _time);

            m_ReturnVal =
                (m_BluePrint_val.value >= m_BluePrint_val_l.value &&
                 m_BluePrint_val.value <= m_BluePrint_val_r.value) == m_BluePrint_bool.value;
            // Debug.Log("判断值: " + m_BluePrint_val.value);
            // Debug.Log("最小值: " + m_BluePrint_val_l.value);
            // Debug.Log("最大值: " + m_BluePrint_val_r.value);
            // Debug.Log("输出结果: " + m_ReturnVal);
        }

        public override bool value => m_ReturnVal;
#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_D_RangeNumber _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Math_D_RangeNumber();
                _graphEvent.BluePrint_val = (BluePrint_Float)m_BluePrint_val.Clone();
                _graphEvent.BluePrint_bool = (BluePrint_Bool)m_BluePrint_bool.Clone();
                _graphEvent.BluePrint_val_l = (BluePrint_Float)m_BluePrint_val_l.Clone();
                // _graphEvent.Select = (BluePrint_Bool)m_Select.Clone();
                _graphEvent.BluePrint_val_r = (BluePrint_Float)m_BluePrint_val_r.Clone();
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