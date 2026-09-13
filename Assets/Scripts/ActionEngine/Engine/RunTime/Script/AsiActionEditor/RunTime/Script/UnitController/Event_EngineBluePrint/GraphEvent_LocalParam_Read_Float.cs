using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_LocalParam_Read_Float : BluePrint_Float
    {
        [SerializeField] protected int m_ParamIndex;

        public int ParamIndex
        {
            get => m_ParamIndex;
            set => m_ParamIndex = value;
        }

        [System.NonSerialized] private float m_CachedValue;

        public override float value => m_CachedValue;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//避免同一帧下反复执行初始化

            m_CachedValue = BluePrintLocalContext.GetFloat(m_ParamIndex);
        }

#if UNITY_EDITOR
        [System.NonSerialized] private GraphEvent_LocalParam_Read_Float _graphEvent;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_LocalParam_Read_Float();
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
