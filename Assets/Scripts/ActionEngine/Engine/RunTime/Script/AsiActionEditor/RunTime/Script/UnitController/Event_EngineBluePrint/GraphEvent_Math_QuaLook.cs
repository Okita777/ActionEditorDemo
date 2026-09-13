using System;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_QuaLook : BluePrint_Vector3
    {
        [SerializeReference] protected BluePrint_Vector3 m_BluePrint_Vector3 = new GraphEvent_Value_Vector3();
        [SerializeReference] protected BluePrint_Vector3 m_BluePrint_Vector2 = new GraphEvent_Value_Vector3();
        // [SerializeReference] protected BluePrint_Float m_BluePrint_Vector32 = new GraphEvent_Value_Float();
        [NonSerialized] private float threshold = 1e-5f;
        #region Property
        [EditorGraphProperty("注视方向(Z)", true, EditorGraphPropertyType.EEPT_Vector3, LabelWidth = 70)]
        public BluePrint_Vector3 BluePrint_Vector3
        {
            get { return m_BluePrint_Vector3; }
            set { m_BluePrint_Vector3 = value; }
        }
        [EditorGraphProperty("注视方向(Y)", true, EditorGraphPropertyType.EEPT_Vector3, LabelWidth = 70)]
        public BluePrint_Vector3 BluePrint_Vector2
        {
            get { return m_BluePrint_Vector2; }
            set { m_BluePrint_Vector2 = value; }
        }
        #endregion

        [System.NonSerialized] private Vector3 m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_BluePrint_Vector3.Init(part, _time);
            m_BluePrint_Vector2.Init(part, _time);
            Vector3 _forward = m_BluePrint_Vector3.value;
            Vector3 _up = m_BluePrint_Vector2.value;

            if (_forward.sqrMagnitude < threshold) _forward = part.ActionStateMachine.CurUnit.transform.forward;
            if (_up.sqrMagnitude < threshold) _forward = Vector3.up;

            m_ReturnVal = Quaternion.LookRotation(_forward, _up).eulerAngles;
        }

        public override Vector3 value => m_ReturnVal;
#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_QuaLook _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Math_QuaLook();
                _graphEvent.BluePrint_Vector3 = (BluePrint_Vector3)m_BluePrint_Vector3.Clone();
                _graphEvent.BluePrint_Vector2 = (BluePrint_Vector3)m_BluePrint_Vector2.Clone();
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