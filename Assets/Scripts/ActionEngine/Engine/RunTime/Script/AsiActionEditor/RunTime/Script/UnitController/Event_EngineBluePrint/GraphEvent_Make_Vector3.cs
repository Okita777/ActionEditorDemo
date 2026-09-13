using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    ///不带参的Vector3
    public class GraphEvent_Make_Vector3 : BluePrint_Vector3
    {
        // [SerializeReference] protected BluePrint_Vector3 m_BluePrint_Val = new GraphEvent_Value_Vector3();
        [SerializeReference] protected BluePrint_Float m_Vector_X = new GraphEvent_Value_Float();
        [SerializeReference] protected BluePrint_Float m_Vector_Y = new GraphEvent_Value_Float();
        [SerializeReference] protected BluePrint_Float m_Vector_Z = new GraphEvent_Value_Float();

        #region Property

        [EditorGraphProperty("X", true, EditorGraphPropertyType.EEPT_Float)]
        public BluePrint_Float X
        {
            get { return m_Vector_X; }
            set { m_Vector_X = value; }
        }
        [EditorGraphProperty("Y", true, EditorGraphPropertyType.EEPT_Float)]
        public BluePrint_Float Y
        {
            get { return m_Vector_Y; }
            set { m_Vector_Y = value; }
        }
        [EditorGraphProperty("Z", true, EditorGraphPropertyType.EEPT_Float)]
        public BluePrint_Float Z
        {
            get { return m_Vector_Z; }
            set { m_Vector_Z = value; }
        }
        #endregion

        [System.NonSerialized] private Vector3 m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_Vector_X.Init(part, _time);
            m_Vector_Y.Init(part, _time);
            m_Vector_Z.Init(part, _time);
            m_ReturnVal = new Vector3(m_Vector_X.value, m_Vector_Y.value, m_Vector_Z.value);
        }

        public override Vector3 value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Make_Vector3 _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Make_Vector3();
                _graphEvent.m_Vector_X = (BluePrint_Float)m_Vector_X.Clone();
                _graphEvent.m_Vector_Y = (BluePrint_Float)m_Vector_Y.Clone();
                _graphEvent.m_Vector_Z = (BluePrint_Float)m_Vector_Z.Clone();
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