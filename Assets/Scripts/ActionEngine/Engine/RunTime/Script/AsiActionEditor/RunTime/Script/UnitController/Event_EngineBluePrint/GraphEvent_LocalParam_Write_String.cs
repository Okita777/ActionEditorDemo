using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_LocalParam_Write_String : BluePrint_String
    {
        [SerializeField] protected int m_ParamIndex;
        [SerializeReference] protected BluePrint_String m_InputValue = new GraphEvent_Value_String();

        #region Property

        [EditorGraphProperty("写入值", true, EditorGraphPropertyType.EEPT_String)]
        public BluePrint_String InputValue
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

        [System.NonSerialized] private string m_CachedValue;

        public override string value => m_CachedValue;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//避免同一帧下反复执行初始化

            m_InputValue.Init(part, _time);
            m_CachedValue = m_InputValue.value;
            BluePrintLocalContext.SetString(m_ParamIndex, m_CachedValue);
        }

#if UNITY_EDITOR
        [System.NonSerialized] private GraphEvent_LocalParam_Write_String _graphEvent;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_LocalParam_Write_String();
                _graphEvent.m_ParamIndex = m_ParamIndex;
                _graphEvent.m_InputValue = (BluePrint_String)m_InputValue.Clone();
                _graphEvent.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { _graphEvent = null; });
            }
            return _graphEvent;
#endif
            return this;
        }
    }
}
