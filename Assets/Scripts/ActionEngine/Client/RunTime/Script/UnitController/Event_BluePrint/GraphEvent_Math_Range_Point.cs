using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Math_Range_Point : BluePrint_PointData
    {
        [SerializeReference] protected BluePrint_GroupPointData m_IntVal = new GraphEvent_GValue_GGPoint();
        [SerializeReference] protected BluePrint_Vector3 m_TargetPos = new GraphEvent_Value_Vector3();
        [SerializeReference] protected BluePrint_Vector3 m_TargetDot = new GraphEvent_Value_Vector3();

        [SerializeField] protected byte m_RangeType = 0;
        #region Property
        [EditorGraphProperty("GroupPoint", true, EditorGraphPropertyType.EEPT_GGroupPoint, LabelWidth = 60)]
        public BluePrint_GroupPointData IntVal
        {
            get { return m_IntVal; }
            set { m_IntVal = value; }
        }
        [EditorGraphProperty("目标位置", true, EditorGraphPropertyType.EEPT_Vector3, LabelWidth = 60)]
        public BluePrint_Vector3 TargetPos
        {
            get { return m_TargetPos; }
            set { m_TargetPos = value; }
        }
        [EditorGraphProperty("目标方向", true, EditorGraphPropertyType.EEPT_Vector3, LabelWidth = 60)]
        public BluePrint_Vector3 TargetDot
        {
            get { return m_TargetDot; }
            set { m_TargetDot = value; }
        }
        [EditorGraphProperty("查找类型", false, EditorGraphPropertyType.EEPT_EnumCustom,
            EnumNames = new[] { "找最小角度", "找最小距离" }, LabelWidth = 60)]
        public byte RangeType
        {
            get { return m_RangeType; }
            set { m_RangeType = value; }
        }
        #endregion

        [System.NonSerialized] private int m_Index;
        [System.NonSerialized] private float m_MinValue;
        [System.NonSerialized] private float m_MinValue_f;
        [System.NonSerialized] private Vector3 m_dir_f;
        [System.NonSerialized] private PointData m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能

            //m_IntVal.SetListSource(EngineResourcesManager.Instance.GetPoints());//提供原始List
            m_IntVal.Init(part, _time);

            if (m_IntVal.value.Count > 0)
            {
                TargetPos.Init(part, _time);
                TargetDot.Init(part, _time);

                m_Index = 0;
                if (m_RangeType == 0)
                {
                    //最小角度判断
                    m_MinValue = -1;
                    for (int i = 0; i < m_IntVal.value.Count; i++)
                    {
                        m_dir_f = (m_IntVal.value[i].pos - TargetPos.value).normalized;
                        m_MinValue_f = Vector3.Dot(m_dir_f, TargetDot.value);
                        if (m_MinValue_f > m_MinValue)
                        {
                            m_Index = i;
                            m_MinValue = m_MinValue_f;
                        }
                    }
                }
                else
                {
                    //最小距离判断
                    m_MinValue = float.MaxValue;
                    for (int i = 0; i < m_IntVal.value.Count; i++)
                    {
                        m_MinValue_f = (m_IntVal.value[i].pos - TargetPos.value).sqrMagnitude;
                        if (m_MinValue_f < m_MinValue)
                        {
                            m_Index = i;
                            m_MinValue = m_MinValue_f;
                        }
                    }
                }
                m_ReturnVal = m_IntVal.value[m_Index];
            }
            else
            {
                //输入的数组长度为0
                m_ReturnVal.Reset();
                //#if UNITY_EDITOR
                //                EngineDebug.LogWarning("当前节点下的数组长度为零");
                //#endif
            }

        }
        public override PointData value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_Range_Point GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Range_Point();
                GraphEventG.IntVal = (BluePrint_GroupPointData)m_IntVal.Clone();
                GraphEventG.TargetPos = (BluePrint_Vector3)m_TargetPos.Clone();
                GraphEventG.TargetDot = (BluePrint_Vector3)m_TargetDot.Clone();
                GraphEventG.RangeType = m_RangeType;
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