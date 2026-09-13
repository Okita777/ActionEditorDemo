using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Select_Unit : BluePrint_Unit
    {
        [SerializeReference] protected BluePrint_Unit m_BluePrint_val_l = new GraphEvent_Value_SelfUnit();
        [SerializeReference] protected BluePrint_Unit m_BluePrint_val_r = new GraphEvent_Value_SelfUnit();
        [SerializeReference] protected BluePrint_Bool m_Select = new GraphEvent_Value_Bool();

        #region Property
        [EditorGraphProperty("True", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit BluePrint_val_l
        {
            get { return m_BluePrint_val_l; }
            set { m_BluePrint_val_l = value; }
        }

        [EditorGraphProperty("False", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit BluePrint_val_r
        {
            get { return m_BluePrint_val_r; }
            set { m_BluePrint_val_r = value; }
        }
        [EditorGraphProperty("Select", true, EditorGraphPropertyType.EEPT_Bool)]
        public BluePrint_Bool Select
        {
            get { return m_Select; }
            set { m_Select = value; }
        }
        #endregion

        [System.NonSerialized] private TargetUnit m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_Select.Init(part, _time);
            if (m_Select.value)
            {
                m_BluePrint_val_l.Init(part, _time);
                m_ReturnVal = m_BluePrint_val_l.value;
            }
            else
            {
                m_BluePrint_val_r.Init(part, _time);
                m_ReturnVal = m_BluePrint_val_r.value;
            }
        }

        public override TargetUnit value => m_ReturnVal;
#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_Select_Unit _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Math_Select_Unit();
                _graphEvent.BluePrint_val_l = (BluePrint_Unit)m_BluePrint_val_l.Clone();
                _graphEvent.Select = (BluePrint_Bool)m_Select.Clone();
                _graphEvent.BluePrint_val_r = (BluePrint_Unit)m_BluePrint_val_r.Clone();
                _graphEvent.IsNode = IsNode;
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

        public override bool isValid(ActionStatePart part) => m_ReturnVal is not null;

    }
}