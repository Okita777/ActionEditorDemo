using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Value_Vector3 : BluePrint_Vector3
    {
        // [SerializeReference] protected BluePrint_Vector3 m_BluePrint_Vector3; 
        [SerializeField] protected EVector3 m_EVector3;
        public GraphEvent_Value_Vector3(){
            m_EVector3 = new EVector3();
        }
        public GraphEvent_Value_Vector3(Vector3 vector3)
        {
            m_EVector3 = new EVector3(vector3);
        }
        #region Property

        [EditorGraphProperty("Vector3", false, EditorGraphPropertyType.EEPT_Vector3)]
        public EVector3 mEVector3
        {
            get { return m_EVector3; }
            set { m_EVector3 = value; }
        }

        #endregion

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            // base.Init(part, _time);
        }
        public override Vector3 value => m_EVector3.GetValue();

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] public GraphEvent_Value_Vector3 _graphEvent = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Value_Vector3();
                _graphEvent.mEVector3 = m_EVector3;
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