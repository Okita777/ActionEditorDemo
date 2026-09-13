using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Other_Float_Distance : BluePrint_Float
    {
        [SerializeReference] protected BluePrint_Vector3 m_Euler_L = new GraphEvent_Value_Vector3();
        [SerializeReference] protected BluePrint_Vector3 m_Vector_R = new GraphEvent_Value_Vector3();
        [SerializeField] protected bool m_SqrDistance = false;

        #region Property
        [EditorGraphProperty("位置1", true, EditorGraphPropertyType.EEPT_Vector3)]
        public BluePrint_Vector3 Euler_L
        {
            get { return m_Euler_L; }
            set { m_Euler_L = value; }
        }
        [EditorGraphProperty("位置2", true, EditorGraphPropertyType.EEPT_Vector3)]
        public BluePrint_Vector3 Vector_R
        {
            get { return m_Vector_R; }
            set { m_Vector_R = value; }
        }
        [EditorGraphProperty("输出平方距离", false, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 80, Tooltip = "输出平方距离时性能消耗更低（跳过开方计算），可用于距离比较")]
        public bool SqrDistance
        {
            get { return m_SqrDistance; }
            set { m_SqrDistance = value; }
        }
        #endregion

        public override float value => m_ReturnVal;

        [System.NonSerialized] private float m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能

            m_Euler_L.Init(part, _time);
            m_Vector_R.Init(part, _time);

            Vector3 offset = (m_Euler_L.value - m_Vector_R.value);
            if (m_SqrDistance) m_ReturnVal = offset.sqrMagnitude;
            else m_ReturnVal = offset.magnitude;
        }

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Other_Float_Distance _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Other_Float_Distance
                {
                    Euler_L = (BluePrint_Vector3)m_Euler_L.Clone(),
                    Vector_R = (BluePrint_Vector3)m_Vector_R.Clone(),
                    SqrDistance = m_SqrDistance,
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