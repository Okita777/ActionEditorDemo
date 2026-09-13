using System.Collections.Generic;
using AsiTimeLine.RunTime;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_TrackData_AllUnit : BluePrint_GroupUnit
    {
        [SerializeField] protected bool m_ExcludeSelf = true;
        #region Property

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
            if (m_ExcludeSelf)
            {
                ActionEngine_Unit _machine = part.ActionStateMachine.CurUnit;
                foreach (var item in ActionEngineManager_Unit.Instance.Units)
                {
                    if (!ReferenceEquals(item, _machine))
                    {
                        m_ReturnVal.Add(item);
                    }
                }
            }
            else
            {
                m_ReturnVal.AddRange(ActionEngineManager_Unit.Instance.Units);
            }
        }

        public override List<ActionEngine_Unit> value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_TrackData_AllUnit GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_TrackData_AllUnit();
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