using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Mul_Float : BluePrint_Float
    {
        [SerializeReference] protected BluePrint_Float m_BluePrint_Vector3 = new GraphEvent_Value_Float();
        [SerializeReference] protected BluePrint_Float m_BluePrint_Vector32 = new GraphEvent_Value_Float();

        #region Property
        [EditorGraphProperty("乘数", true, EditorGraphPropertyType.EEPT_Float)]
        public BluePrint_Float BluePrint_Vector3
        {
            get { return m_BluePrint_Vector3; }
            set { m_BluePrint_Vector3 = value; }
        }
        [EditorGraphProperty("乘数", true, EditorGraphPropertyType.EEPT_Float)]
        public BluePrint_Float BluePrint_Vector32
        {
            get { return m_BluePrint_Vector32; }
            set { m_BluePrint_Vector32 = value; }
        }
        #endregion

        [System.NonSerialized] private float m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_BluePrint_Vector3.Init(part, _time);
            BluePrint_Vector32.Init(part, _time);
            m_ReturnVal = m_BluePrint_Vector3.value * BluePrint_Vector32.value;
        }

        public override float value => m_ReturnVal;
#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_Mul_Float _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Math_Mul_Float();
                _graphEvent.BluePrint_Vector3 = (BluePrint_Float)m_BluePrint_Vector3.Clone();
                _graphEvent.BluePrint_Vector32 = (BluePrint_Float)m_BluePrint_Vector32.Clone();
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