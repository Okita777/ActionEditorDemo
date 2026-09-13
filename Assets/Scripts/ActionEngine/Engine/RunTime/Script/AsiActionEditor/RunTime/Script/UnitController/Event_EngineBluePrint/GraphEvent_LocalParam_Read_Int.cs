using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_LocalParam_Read_Int : BluePrint_Int
    {
        [SerializeField] protected int m_ParamIndex;

        public int ParamIndex
        {
            get => m_ParamIndex;
            set => m_ParamIndex = value;
        }

        [System.NonSerialized] private int m_CachedValue;

        public override int value => m_CachedValue;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//避免同一帧下反复执行初始化

            m_CachedValue = BluePrintLocalContext.GetInt(m_ParamIndex);
        }

#if UNITY_EDITOR
        [System.NonSerialized] private GraphEvent_LocalParam_Read_Int _graphEvent;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_LocalParam_Read_Int();
                _graphEvent.m_ParamIndex = m_ParamIndex;
                _graphEvent.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { _graphEvent = null; });
            }
            return _graphEvent;
#endif
            return this;
        }
    }
}
