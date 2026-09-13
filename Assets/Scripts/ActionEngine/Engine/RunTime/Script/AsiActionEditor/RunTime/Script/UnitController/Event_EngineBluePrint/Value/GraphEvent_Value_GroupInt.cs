using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Value_GroupInt : BluePrint_GroupInt
    {
        [SerializeField] protected int m_ListLength;
        [System.NonSerialized] private List<int> m_ListVal;

        #region Property

        [EditorGraphProperty("长度", false, EditorGraphPropertyType.EEPT_Int)]
        public int ListLength
        {
            get { return m_ListLength; }
            set
            {
                if (value < 0) value = 0;
                if (value > MotionEngineConst.BluePrint_ListMaxCount)
                    value = MotionEngineConst.BluePrint_ListMaxCount;
                m_ListLength = value;
            }
        }

        #endregion

        public override List<int> value => m_ListVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            int count = m_ListLength;
            if (count < 0) count = 0;
            if (count > MotionEngineConst.BluePrint_ListMaxCount)
                count = MotionEngineConst.BluePrint_ListMaxCount;

            m_ListVal = EngineResourcesManager.Instance.CreateInts();
            for (int i = 0; i < count; i++)
            {
                m_ListVal.Add(0);
            }
        }

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_Value_GroupInt _graphEvent = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Value_GroupInt();
                _graphEvent.ListLength = m_ListLength;
                _graphEvent.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { _graphEvent = null; });
            }
            return _graphEvent;
#endif
            return this;
        }
    }
}
