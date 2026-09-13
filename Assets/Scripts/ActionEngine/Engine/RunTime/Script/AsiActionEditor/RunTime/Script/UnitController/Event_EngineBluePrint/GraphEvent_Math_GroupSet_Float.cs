using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_GroupSet_Float : BluePrint_GroupFloat
    {
        [SerializeReference] protected BluePrint_GroupFloat m_ListValue = new GraphEvent_GValue_GGFloat();
        [SerializeReference] protected BluePrint_Int m_Index = new GraphEvent_Value_Int();
        [SerializeReference] protected BluePrint_Float m_SetValue = new GraphEvent_Value_Float();

        #region Property
        [EditorGraphProperty("GroupFloat", true, EditorGraphPropertyType.EEPT_GGroupFloat, LabelWidth = 60)]
        public BluePrint_GroupFloat ListValue
        {
            get { return m_ListValue; }
            set { m_ListValue = value; }
        }
        [EditorGraphProperty("Index", true, EditorGraphPropertyType.EEPT_Int)]
        public BluePrint_Int Index
        {
            get { return m_Index; }
            set { m_Index = value; }
        }
        [EditorGraphProperty("Value", true, EditorGraphPropertyType.EEPT_Float)]
        public BluePrint_Float SetValue
        {
            get { return m_SetValue; }
            set { m_SetValue = value; }
        }
        #endregion

        [System.NonSerialized] private List<float> m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_ListValue.Init(part, _time);
            m_Index.Init(part, _time);
            m_SetValue.Init(part, _time);

            m_ReturnVal = EngineResourcesManager.Instance.CreateFloats();
            m_ReturnVal.AddRange(m_ListValue.value);

            int idx = m_Index.value;
            if (idx >= 0 && idx < m_ReturnVal.Count)
            {
                m_ReturnVal[idx] = m_SetValue.value;
            }
#if UNITY_EDITOR
            else
            {
                string _debug = EngineDebug.DebugActionStatePart(part);
                if (Index.value < 0)
                    EngineDebug.LogWarning($"超出索引，请检查蓝图逻辑 当前Index[{Index.value}]  长度[{ListValue.value.Count}]\n" + _debug);
                else
                    EngineDebug.LogError($"超出索引，请检查蓝图逻辑 当前Index[{Index.value}]  长度[{ListValue.value.Count}]\n" + _debug);
            }
#endif
        }

        public override List<float> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_GroupSet_Float GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_GroupSet_Float();
                GraphEventG.ListValue = (BluePrint_GroupFloat)m_ListValue.Clone();
                GraphEventG.Index = (BluePrint_Int)m_Index.Clone();
                GraphEventG.SetValue = (BluePrint_Float)m_SetValue.Clone();
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() =>
                {
                    GraphEventG = null;
                });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
