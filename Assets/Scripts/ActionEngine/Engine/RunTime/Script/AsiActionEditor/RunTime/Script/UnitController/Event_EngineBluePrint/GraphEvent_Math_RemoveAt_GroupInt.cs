using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_RemoveAt_GroupInt : BluePrint_GroupInt
    {
        [SerializeReference] protected BluePrint_GroupInt m_InVal = new GraphEvent_GValue_GGInt();
        [SerializeReference] protected BluePrint_Int m_Index = new GraphEvent_Value_Int();

        #region Property
        [EditorGraphProperty("GroupInt", true, EditorGraphPropertyType.EEPT_GGroupInt, LabelWidth = 60)]
        public BluePrint_GroupInt IntVal
        {
            get { return m_InVal; }
            set { m_InVal = value; }
        }
        [EditorGraphProperty("Index", true, EditorGraphPropertyType.EEPT_Int)]
        public BluePrint_Int Index
        {
            get { return m_Index; }
            set { m_Index = value; }
        }
        #endregion

        [System.NonSerialized] private List<int> m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_InVal.Init(part, _time);
            m_Index.Init(part, _time);
            m_ReturnVal = EngineResourcesManager.Instance.CreateInts();
            m_ReturnVal.AddRange(m_InVal.value);
            int idx = m_Index.value;
            if (idx >= 0 && idx < m_ReturnVal.Count)
            {
                m_ReturnVal.RemoveAt(idx);
            }
#if UNITY_EDITOR
            else
            {
                string _debug = EngineDebug.DebugActionStatePart(part);
                if (Index.value < 0)
                    EngineDebug.LogWarning($"超出索引，请检查蓝图逻辑 当前Index[{Index.value}]  长度[{IntVal.value.Count}]\n" + _debug);
                else
                    EngineDebug.LogError($"超出索引，请检查蓝图逻辑 当前Index[{Index.value}]  长度[{IntVal.value.Count}]\n" + _debug);
            }
#endif
        }

        public override List<int> value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Math_RemoveAt_GroupInt GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_RemoveAt_GroupInt();
                GraphEventG.IntVal = (BluePrint_GroupInt)m_InVal.Clone();
                GraphEventG.Index = (BluePrint_Int)m_Index.Clone();
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
