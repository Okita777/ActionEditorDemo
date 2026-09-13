using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_TrackData_Vector_InputDir : BluePrint_Vector3
    {
        [SerializeField] protected bool m_Val = true;
        [SerializeField] protected bool m_Trans = false;

        #region Property
        [EditorGraphProperty("按相机方向转换", false, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 100)]
        public bool Val
        {
            get { return m_Val; }
            set { m_Val = value; }
        }
        [EditorGraphProperty("忽略Y轴(标量化)", false, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 100)]
        public bool Trans
        {
            get { return m_Trans; }
            set { m_Trans = value; }
        }
        #endregion

        public override Vector3 value => m_ReturnVal;

        [System.NonSerialized] private Vector3 m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能

            if (m_Val)
            {
                // Quaternion rot = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);
                // m_ReturnVal = rot * part.ActionStateMachine.PlayerInputMoveDir;
                m_ReturnVal = part.ActionStateMachine.PlayerInputMoveDir_Cam;
            }
            else
            {
                m_ReturnVal = part.ActionStateMachine.PlayerInputMoveDir;
            }

            if (m_Trans)
            {
                m_ReturnVal.y = 0;
                m_ReturnVal.Normalize();
            }
        }

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_TrackData_Vector_InputDir _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_TrackData_Vector_InputDir();
                _graphEvent.Val = m_Val;
                _graphEvent.Trans = m_Trans;
                _graphEvent.IsNode = IsNode;
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