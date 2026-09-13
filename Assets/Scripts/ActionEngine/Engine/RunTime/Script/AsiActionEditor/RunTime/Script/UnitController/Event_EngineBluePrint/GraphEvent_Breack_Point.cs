using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    ///不带参的Vector3
    public class GraphEvent_Breack_Point : BluePrint_Vector3
    {
        public enum EAxisType
        {
            Position,
            Rotation,
        }
        [SerializeReference] protected BluePrint_PointData m_Vector_X = new GraphEvent_BValue_Point();
        [SerializeReference] protected EAxisType m_Axis = EAxisType.Position;

        #region Property

        [EditorGraphProperty("输出参数", false, EditorGraphPropertyType.EEPT_Enum, LabelWidth = 100)]
        public EAxisType Axis
        {
            get { return m_Axis; }
            set { m_Axis = value; }
        }

        [EditorGraphProperty("输入", true, EditorGraphPropertyType.EEPT_PointData)]
        public BluePrint_PointData InputVal
        {
            get { return m_Vector_X; }
            set { m_Vector_X = value; }
        }

        #endregion

        [System.NonSerialized] private Vector3 m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_Vector_X.Init(part, _time);
            if (Axis == EAxisType.Position) m_ReturnVal = m_Vector_X.value.pos;
            else if (Axis == EAxisType.Rotation) m_ReturnVal = m_Vector_X.value.rot.eulerAngles;
        }

        public override Vector3 value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Breack_Point _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Breack_Point();
                _graphEvent.InputVal = (BluePrint_PointData)m_Vector_X.Clone();
                _graphEvent.Axis = Axis;
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