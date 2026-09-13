using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    ///不带参的Vector3
    public class GraphEvent_Breack_Vector3 : BluePrint_Float
    {
        public enum EAxisType
        {
            X,
            Y,
            Z
        }
        [SerializeReference] protected BluePrint_Vector3 m_Vector_X = new GraphEvent_Value_Vector3();
        [SerializeReference] protected EAxisType m_Axis = EAxisType.X;

        #region Property

        [EditorGraphProperty("输出轴向", false, EditorGraphPropertyType.EEPT_Enum, LabelWidth = 100)]
        public EAxisType Axis
        {
            get { return m_Axis; }
            set { m_Axis = value; }
        }

        [EditorGraphProperty("输入", true, EditorGraphPropertyType.EEPT_Vector3)]
        public BluePrint_Vector3 InputVal
        {
            get { return m_Vector_X; }
            set { m_Vector_X = value; }
        }

        #endregion

        [System.NonSerialized] private float m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_Vector_X.Init(part, _time);
            if (Axis == EAxisType.X) m_ReturnVal = m_Vector_X.value.x;
            else if (Axis == EAxisType.Y) m_ReturnVal = m_Vector_X.value.y;
            else if (Axis == EAxisType.Z) m_ReturnVal = m_Vector_X.value.z;
        }

        public override float value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Breack_Vector3 _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Breack_Vector3();
                _graphEvent.InputVal = (BluePrint_Vector3)m_Vector_X.Clone();
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