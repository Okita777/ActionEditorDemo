using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Math_For_GroupUnit : BluePrint_GroupUnit
    {
        [SerializeReference] protected BluePrint_GroupUnit m_GroupValue = new GraphEvent_GValue_GGUnit();
        [SerializeReference] protected BluePrint_Bool m_IsCheckValid = new GraphEvent_Value_Bool(true);

        #region Property
        [EditorGraphProperty("GroupUnit", true, EditorGraphPropertyType.EEPT_GGroupUnit, LabelWidth = 60)]
        public BluePrint_GroupUnit GroupVal
        {
            get { return m_GroupValue; }
            set { m_GroupValue = value; }
        }

        [EditorGraphProperty("筛选条件(Bool)", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 80)]
        public BluePrint_Bool IsCheckValid
        {
            get { return m_IsCheckValid; }
            set { m_IsCheckValid = value; }
        }
        #endregion

        [System.NonSerialized] private List<ActionEngine_Unit> m_ReturnVal = null;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能

            m_ReturnVal = EngineResourcesManager.Instance.CreateUnits();
            m_GroupValue.Init(part, _time);
            //m_ReturnVal.AddRange(m_GroupValue.value);

            part.ActionStateMachine.IsCheckBluePrintCon = false;
            for (int i = 0; i < m_GroupValue.value.Count; i++)
            {
                part.ActionStateMachine.BluePrint_LoopIndex = i;
                IsCheckValid.Init(part, _time);
                if (IsCheckValid.value)
                {
                    m_ReturnVal.Add(m_GroupValue.value[i]);
                    //EngineDebug.LogError($"<color=#ffcc00> [{m_ReturnVal.GetHashCode()}] 添加[{i}]  输出长度[{m_ReturnVal.Count}]");
                }
            }
            //EngineDebug.LogError($"输出长度[{m_ReturnVal.Count}]  j检查长度[{m_GroupValue.value.Count}]");
            part.ActionStateMachine.IsCheckBluePrintCon = true;
        }
        public override List<ActionEngine_Unit> value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_For_GroupUnit GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_For_GroupUnit();
                GraphEventG.GroupVal = (BluePrint_GroupUnit)m_GroupValue.Clone();
                GraphEventG.IsCheckValid = (BluePrint_Bool)m_IsCheckValid.Clone();
                GraphEventG.IsNode = IsNode;
                //在保存好文件后重置状态
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