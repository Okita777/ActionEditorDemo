using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Add_GroupUnit_Group : BluePrint_GroupUnit
    {
        [SerializeReference] protected BluePrint_GroupUnit m_InVal = new GraphEvent_GValue_GGUnit();
        [SerializeReference] protected BluePrint_GroupUnit m_InVal2 = new GraphEvent_GValue_GGUnit();

        #region Property

        [EditorGraphProperty("GroupUnit", true, EditorGraphPropertyType.EEPT_GGroupUnit, LabelWidth = 60)]
        public BluePrint_GroupUnit IntVal
        {
            get { return m_InVal; }
            set { m_InVal = value; }
        }
        [EditorGraphProperty("GroupUnit", true, EditorGraphPropertyType.EEPT_GGroupUnit, LabelWidth = 60)]
        public BluePrint_GroupUnit InVal2
        {
            get { return m_InVal2; }
            set { m_InVal2 = value; }
        }
        #endregion

        [System.NonSerialized] private List<ActionEngine_Unit> m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能

            m_ReturnVal = EngineResourcesManager.Instance.CreateUnits();
            //HashSet<Object> m_HashSet = EngineResourcesManager.Instance.HashSet_Object();

            m_InVal.Init(part, _time);
            m_InVal2.Init(part, _time);
            //m_ReturnVal.AddRange(m_InVal.value);
            for (int i = 0; i < m_InVal.value.Count; i++)
            {
                //Debug.Log($"尝试添加[{m_InVal.value[i].transform.GetSiblingIndex()}]  [{m_InVal.value[i].transform.name}]");
                if (!m_ReturnVal.Contains(m_InVal.value[i]))
                {
                    //m_HashSet.Add(m_InVal.value[i]);
                    m_ReturnVal.Add(m_InVal.value[i]);
                    //Debug.Log($"实际 添加[{m_InVal.value[i].transform.GetSiblingIndex()}]  [{m_InVal.value[i].transform.name}]");
                }
            }
            for (int i = 0; i < m_InVal2.value.Count; i++)
            {
                //Debug.Log($"尝试添加[{m_InVal2.value[i].transform.GetSiblingIndex()}]  [{m_InVal2.value[i].transform.name}]----------");
                if (!m_ReturnVal.Contains(m_InVal2.value[i]))
                {
                    m_ReturnVal.Add(m_InVal2.value[i]);
                    //m_HashSet.Add(m_InVal2.value[i]);
                    //Debug.Log($"实际 添加[{m_InVal2.value[i].transform.GetSiblingIndex()}]  [{m_InVal2.value[i].transform.name}]----------");
                }
            }

            //string _name = "";
            //foreach (var item in m_ReturnVal)
            //{
            //    _name += $"\n  {item.transform.GetSiblingIndex()}  [{item.transform.name}]";
            //}
            //Debug.Log("<color=#ffcc00>检查结束" + _name);
        }

        public override List<ActionEngine_Unit> value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_Add_GroupUnit_Group GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Add_GroupUnit_Group();
                GraphEventG.IntVal = (BluePrint_GroupUnit)m_InVal.Clone();
                GraphEventG.InVal2 = (BluePrint_GroupUnit)m_InVal2.Clone();
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