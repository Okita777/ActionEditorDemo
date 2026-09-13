using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_CheckUnitLayer : BluePrint_Bool
    {
        [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_Value_SelfUnit();
        [SerializeField] protected int m_LayerMask = 0;

        #region Property
        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_Bool)]
        public BluePrint_Unit UnitVal
        {
            get { return m_UnitVal; }
            set { m_UnitVal = value; }
        }
        [EditorGraphProperty("Layer", false, EditorGraphPropertyType.EEPT_LayerMask)]
        public int LayerMask
        {
            get { return m_LayerMask; }
            set { m_LayerMask = value; }
        }
        #endregion

        [System.NonSerialized] private bool m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_UnitVal.Init(part, _time);

            if (m_UnitVal.isValid(part))
            {
                if (m_UnitVal.value is ActionEngine_Unit _unit)
                {
                    int _layer = _unit.gameObject.layer;
                    m_ReturnVal = (m_LayerMask & (1 << _layer)) != 0;
                    return;
                }
            }
            m_ReturnVal = false;
        }

        public override bool value => m_ReturnVal;
#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_CheckUnitLayer _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Math_CheckUnitLayer();
                _graphEvent.m_UnitVal = (BluePrint_Unit)m_UnitVal.Clone();
                _graphEvent.m_LayerMask = m_LayerMask;
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