using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_TrackData_GetSkillToUnitTag : BluePrint_GroupUnit
    {
        [SerializeReference] protected BluePrint_Unit m_TargetUnit = new GraphEvent_Value_SelfUnit();
        [SerializeField] protected bool m_ExcludeSelf = false;
        [SerializeField] protected int[] m_Tags = new int[0];
        #region Property
        [EditorGraphProperty("目标单位", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit TargetUnit
        {
            get { return m_TargetUnit; }
            set { m_TargetUnit = value; }
        }
        [EditorGraphProperty("所选标签", false, EditorGraphPropertyType.EEPT_SkillTypeTag)]
        public int[] Tags
        {
            get { return m_Tags; }
            set { m_Tags = value; }
        }
        [EditorGraphProperty("排除自身", false, EditorGraphPropertyType.EEPT_Bool)]
        public bool ExcludeSelf
        {
            get { return m_ExcludeSelf; }
            set { m_ExcludeSelf = value; }
        }

        #endregion

        [System.NonSerialized] private List<ActionEngine_Unit> m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能

            m_ReturnVal = EngineResourcesManager.Instance.CreateUnits();

            TargetUnit.Init(part, _time);
            if (!TargetUnit.isValid(part)) return;
            List<ActionEngine_Unit> m_FindVal = EngineResourcesManager.Instance.CreateUnits();
            HashSet<Object> m_hashSet = EngineResourcesManager.Instance.HashSet_Object();

            foreach (int idKey in m_Tags)
            {
                //EngineDebug.LogError($"<color=#ffcc00>{TargetUnit.value.TargetUnit().gameObject.name}</color> 尝试添加技能[{idKey}]");

                if (TargetUnit.value.GetUnit().m_SkillDic.TryGetValue(idKey, out List<ActionEngine_Skill> _list))
                {
                    //EngineDebug.LogError($"尝试添加技能[{idKey}({_list.Count})]");
                    m_FindVal.AddRange(_list);
                }
            }

            //去重
            bool _findSelf = false;
            //for (int i = m_FindVal.Count - 1; i >= 0; i--)
            for (int i = 0; i < m_FindVal.Count; i++)
            {
                ActionEngine_Unit _cur = m_FindVal[i];
                if (!m_hashSet.Contains(_cur))
                {
                    if (m_ExcludeSelf && !_findSelf)
                    {
                        ActionEngine_Unit _machine = part.ActionStateMachine.CurUnit;
                        if (ReferenceEquals(_cur, _machine))
                        {
                            _findSelf = true;
                        }
                        else
                        {
                            m_ReturnVal.Add(_cur);
                        }
                    }
                    else
                    {
                        m_ReturnVal.Add(_cur);
                    }
                    m_hashSet.Add(_cur);
                }
            }
        }

        public override List<ActionEngine_Unit> value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_TrackData_GetSkillToUnitTag GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_TrackData_GetSkillToUnitTag();
                GraphEventG.TargetUnit = (BluePrint_Unit)m_TargetUnit.Clone();
                GraphEventG.ExcludeSelf = m_ExcludeSelf;
                GraphEventG.Tags = m_Tags.ToArray();
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