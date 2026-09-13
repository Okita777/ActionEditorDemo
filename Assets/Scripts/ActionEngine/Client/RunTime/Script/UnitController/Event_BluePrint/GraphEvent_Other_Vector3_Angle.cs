using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Other_Vector3_Angle : BluePrint_Float
    {
        [SerializeReference] protected BluePrint_Vector3 m_Euler_L = new GraphEvent_Value_Vector3();
        [SerializeReference] protected BluePrint_Vector3 m_Vector_R = new GraphEvent_Value_Vector3();
        [SerializeReference] protected BluePrint_Vector3 m_Axis = new GraphEvent_Value_Vector3();

        #region Property
        [EditorGraphProperty("起始方向", true, EditorGraphPropertyType.EEPT_Vector3)]
        public BluePrint_Vector3 Euler_L
        {
            get { return m_Euler_L; }
            set { m_Euler_L = value; }
        }
        [EditorGraphProperty("目标方向", true, EditorGraphPropertyType.EEPT_Vector3)]
        public BluePrint_Vector3 Vector_R
        {
            get { return m_Vector_R; }
            set { m_Vector_R = value; }
        }
        [EditorGraphProperty("参考轴", true, EditorGraphPropertyType.EEPT_Vector3)]
        public BluePrint_Vector3 Axis
        {
            get { return m_Axis; }
            set { m_Axis = value; }
        }
        #endregion

        public override float value => m_ReturnVal;

        [System.NonSerialized] private float m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能

            m_Euler_L.Init(part, _time);
            m_Vector_R.Init(part, _time);
            m_Axis.Init(part, _time);

            m_ReturnVal = Vector3.SignedAngle(m_Euler_L.value, m_Vector_R.value, m_Axis.value);
        }

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Other_Vector3_Angle _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Other_Vector3_Angle
                {
                    Euler_L = (BluePrint_Vector3)m_Euler_L.Clone(),
                    Vector_R = (BluePrint_Vector3)m_Vector_R.Clone(),
                    Axis = (BluePrint_Vector3)m_Axis.Clone(),
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