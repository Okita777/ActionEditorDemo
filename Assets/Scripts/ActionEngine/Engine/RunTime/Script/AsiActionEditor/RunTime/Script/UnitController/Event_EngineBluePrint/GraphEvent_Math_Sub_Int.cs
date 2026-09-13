using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Sub_Int : BluePrint_Int
    {
        [SerializeReference] protected BluePrint_Int m_BluePrint_Vector3 = new GraphEvent_Value_Int();
        [SerializeReference] protected BluePrint_Int m_BluePrint_Vector32 = new GraphEvent_Value_Int();

        #region Property
        [EditorGraphProperty("被减数", true, EditorGraphPropertyType.EEPT_Int)]
        public BluePrint_Int BluePrint_Vector3
        {
            get { return m_BluePrint_Vector3; }
            set { m_BluePrint_Vector3 = value; }
        }
        [EditorGraphProperty("减数", true, EditorGraphPropertyType.EEPT_Int)]
        public BluePrint_Int BluePrint_Vector32
        {
            get { return m_BluePrint_Vector32; }
            set { m_BluePrint_Vector32 = value; }
        }
        #endregion

        [System.NonSerialized] private int m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_BluePrint_Vector3.Init(part, _time);
            BluePrint_Vector32.Init(part, _time);
            m_ReturnVal = m_BluePrint_Vector3.value - BluePrint_Vector32.value;
        }

        public override int value => m_ReturnVal;
#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_Sub_Int _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Math_Sub_Int();
                _graphEvent.BluePrint_Vector3 = (BluePrint_Int)m_BluePrint_Vector3.Clone();
                _graphEvent.BluePrint_Vector32 = (BluePrint_Int)m_BluePrint_Vector32.Clone();
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