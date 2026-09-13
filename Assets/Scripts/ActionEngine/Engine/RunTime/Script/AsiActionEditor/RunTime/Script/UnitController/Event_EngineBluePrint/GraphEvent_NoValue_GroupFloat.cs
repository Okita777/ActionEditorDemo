using System.Collections.Generic;
using AsiActionEngine.RunTime.Graph;
using UnityEngine;

namespace AsiActionEngine.RunTime.GraphVal
{
    [System.Serializable]
    public class GraphEvent_NoValue_GroupFloat : INodeEdiDataHolder
    {
        [SerializeReference] protected BluePrint_GroupFloat m_BluePrint_Val = new GraphEvent_GValue_GGFloat();
        public NodeEdiData m_NodeEdiData = new NodeEdiData();

        [SerializeField] private List<int> m_LocalIntParams;
        [SerializeField] private List<float> m_LocalFloatParams;
        [SerializeField] private List<bool> m_LocalBoolParams;
        [SerializeField] private List<string> m_LocalStringParams;

        public List<int> LocalIntParams { get { if (m_LocalIntParams == null) m_LocalIntParams = new List<int>(); return m_LocalIntParams; } set { m_LocalIntParams = value; } }
        public List<float> LocalFloatParams { get { if (m_LocalFloatParams == null) m_LocalFloatParams = new List<float>(); return m_LocalFloatParams; } set { m_LocalFloatParams = value; } }
        public List<bool> LocalBoolParams { get { if (m_LocalBoolParams == null) m_LocalBoolParams = new List<bool>(); return m_LocalBoolParams; } set { m_LocalBoolParams = value; } }
        public List<string> LocalStringParams { get { if (m_LocalStringParams == null) m_LocalStringParams = new List<string>(); return m_LocalStringParams; } set { m_LocalStringParams = value; } }

        #region Property
        [EditorGraphProperty("GroupFloat", true, EditorGraphPropertyType.EEPT_GroupFloat)]
        public BluePrint_GroupFloat BluePrint_Val
        {
            get { return m_BluePrint_Val; }
            set { m_BluePrint_Val = value; }
        }
        #endregion

        NodeEdiData INodeEdiDataHolder.NodeEdiData
        {
            get => m_NodeEdiData;
            set => m_NodeEdiData = value;
        }

        [System.NonSerialized] private List<float> m_ReturnVal;
        private void Init(ActionStatePart part, ActionMachineTime _time)
        {
            m_BluePrint_Val.Init(part, _time);
            m_ReturnVal = m_BluePrint_Val.value;
        }

        public List<float> value(ActionStatePart part, ActionMachineTime _time, bool _isInit = true)
        {
            if (_isInit) part.ActionStateMachine.BluePrintClear();
            BluePrintLocalContext.Set(LocalIntParams, LocalFloatParams, LocalBoolParams, LocalStringParams);
            Init(part, _time);
            BluePrintLocalContext.Clear();
            return m_ReturnVal;
        }

        public GraphEvent_NoValue_GroupFloat Clone()
        {
#if UNITY_EDITOR
            GraphEvent_NoValue_GroupFloat clone = new GraphEvent_NoValue_GroupFloat();
            clone.BluePrint_Val = (BluePrint_GroupFloat)m_BluePrint_Val.Clone();
            if (m_NodeEdiData is not null) clone.m_NodeEdiData = m_NodeEdiData.Clone();
            clone.m_LocalIntParams = new List<int>(LocalIntParams);
            clone.m_LocalFloatParams = new List<float>(LocalFloatParams);
            clone.m_LocalBoolParams = new List<bool>(LocalBoolParams);
            clone.m_LocalStringParams = new List<string>(LocalStringParams);
            return clone;
#endif
            return this;
        }
    }
}
