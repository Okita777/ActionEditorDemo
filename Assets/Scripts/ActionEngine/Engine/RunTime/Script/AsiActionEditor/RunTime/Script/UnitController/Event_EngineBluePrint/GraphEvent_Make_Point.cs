using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    ///不带参的Vector3
    public class GraphEvent_Make_Point : BluePrint_PointData
    {
        // [SerializeReference] protected BluePrint_Vector3 m_BluePrint_Val = new GraphEvent_Value_Vector3();
        [SerializeReference] protected BluePrint_Vector3 m_Position = new GraphEvent_Value_Vector3();
        [SerializeReference] protected BluePrint_Vector3 m_Rotation = new GraphEvent_Value_Vector3();

        #region Property

        [EditorGraphProperty("Position", true, EditorGraphPropertyType.EEPT_Vector3)]
        public BluePrint_Vector3 Position
        {
            get { return m_Position; }
            set { m_Position = value; }
        }

        [EditorGraphProperty("Rotation", true, EditorGraphPropertyType.EEPT_Vector3)]
        public BluePrint_Vector3 Rotation
        {
            get { return m_Rotation; }
            set { m_Rotation = value; }
        }
        #endregion

        [System.NonSerialized] private PointData m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            Position.Init(part, _time);
            Rotation.Init(part, _time);
            m_ReturnVal = new PointData(m_Position.value, Quaternion.Euler(m_Rotation.value));
        }

        public override PointData value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Make_Point _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Make_Point();
                _graphEvent.Position = (BluePrint_Vector3)m_Position.Clone();
                _graphEvent.Rotation = (BluePrint_Vector3)m_Rotation.Clone();
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