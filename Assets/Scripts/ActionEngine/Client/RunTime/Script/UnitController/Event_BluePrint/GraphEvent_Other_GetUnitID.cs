using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Other_GetUnitID : BluePrint_Int
    {
        [SerializeReference] protected BluePrint_Unit m_Unit = new GraphEvent_Value_SelfUnit();

        #region Property
        [EditorGraphProperty("单位", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit Unit
        {
            get { return m_Unit; }
            set { m_Unit = value; }
        }

        #endregion

        public override int value => m_ReturnVal;

        [System.NonSerialized] private int m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能

            m_Unit.Init(part, _time);
            if (m_Unit.isValid(part))
                m_ReturnVal = m_Unit.value.GetUnit().UnitWarpID;
            else m_ReturnVal = -1;
        }

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Other_GetUnitID _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Other_GetUnitID
                {
                    Unit = (BluePrint_Unit)m_Unit.Clone(),
                    IsNode = IsNode
                };
                //在保存好文件后重置状态
                ActionSaveFlishEvent.ActionEvent.AddListener(() =>
                {
                    _graphEvent = null;
                });
            }
            return _graphEvent;
#endif
            return this;
        }
    }
}