using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_LocalParam_Write_Bool : BluePrint_Bool
    {
        [SerializeField] protected int m_ParamIndex;
        [SerializeReference] protected BluePrint_Bool m_InputValue = new GraphEvent_Value_Bool();

        #region Property

        [EditorGraphProperty("写入值", true, EditorGraphPropertyType.EEPT_Bool)]
        public BluePrint_Bool InputValue
        {
            get => m_InputValue;
            set => m_InputValue = value;
        }

        public int ParamIndex
        {
            get => m_ParamIndex;
            set => m_ParamIndex = value;
        }

        #endregion

        [System.NonSerialized] private bool m_CachedValue;

        public override bool value => m_CachedValue;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//避免同一帧下反复执行初始化

            m_InputValue.Init(part, _time);
            m_CachedValue = m_InputValue.value;
            BluePrintLocalContext.SetBool(m_ParamIndex, m_CachedValue);
        }

#if UNITY_EDITOR
        [System.NonSerialized] private GraphEvent_LocalParam_Write_Bool _graphEvent;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_LocalParam_Write_Bool();
                _graphEvent.m_ParamIndex = m_ParamIndex;
                _graphEvent.m_InputValue = (BluePrint_Bool)m_InputValue.Clone();
                _graphEvent.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { _graphEvent = null; });
            }
            return _graphEvent;
#endif
            return this;
        }
    }
}
