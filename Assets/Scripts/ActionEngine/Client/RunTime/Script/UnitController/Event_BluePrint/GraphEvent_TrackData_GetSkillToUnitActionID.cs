using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_TrackData_GetSkillToUnitActionID : BluePrint_GroupUnit
    {
        [SerializeReference] protected BluePrint_Unit m_TargetUnit = new GraphEvent_Value_SelfUnit();
        [SerializeField] protected bool m_ExcludeSelf = false;
        [SerializeReference] protected BluePrint_Int m_SkillID = new GraphEvent_Value_Int();
        #region Property
        [EditorGraphProperty("目标单位", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit TargetUnit
        {
            get { return m_TargetUnit; }
            set { m_TargetUnit = value; }
        }
        [EditorGraphProperty("想获取的技能", true, EditorGraphPropertyType.EEPT_Int, LabelWidth = 80)]
        public BluePrint_Int SkillID
        {
            get { return m_SkillID; }
            set { m_SkillID = value; }
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
            SkillID.Init(part, _time);
            if (!TargetUnit.isValid(part)) return;
            //List<ActionEngine_Unit> m_FindVal = EngineResourcesManager.Instance.CreateUnits();
            HashSet<Object> m_hashSet = EngineResourcesManager.Instance.HashSet_Object();

            //string _debug = $"当前持有技能(ExcludeSelf <color=#ffcc00>{ExcludeSelf}</color>):";
            foreach (KeyValuePair<int, List<ActionEngine_Skill>> obj in TargetUnit.value.GetUnit().m_SkillDic)
            {
                foreach (ActionEngine_Skill item in obj.Value)
                {
                    if (item.UnitWarpID == SkillID.value && !m_hashSet.Contains(item))
                    {
                        if (ExcludeSelf)
                        {
                            if (item != TargetUnit.value.GetUnit())
                                m_ReturnVal.Add(item);
                        }
                        else
                        {
                            m_ReturnVal.Add(item);
                        }
                        m_hashSet.Add(item);
                    }
                    //_debug += $"\n 技能: {item.gameObject.name}";
                }
            }
            //EngineDebug.LogError(_debug);
        }

        public override List<ActionEngine_Unit> value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_TrackData_GetSkillToUnitActionID GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_TrackData_GetSkillToUnitActionID();
                GraphEventG.SkillID = (BluePrint_Int)m_SkillID.Clone();
                GraphEventG.TargetUnit = (BluePrint_Unit)m_TargetUnit.Clone();
                GraphEventG.ExcludeSelf = m_ExcludeSelf;
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