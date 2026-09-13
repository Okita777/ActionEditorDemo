using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Value_Float : BluePrint_Float
    {
        public GraphEvent_Value_Float(float _defaultVal = 0.0f)
        {
            m_FloatVal = _defaultVal;
        }

        [SerializeField] protected float m_FloatVal = 1f;
        #region Property

        [EditorGraphProperty("Float", false, EditorGraphPropertyType.EEPT_Float)]
        public float FloatVal
        {
            get { return m_FloatVal; }
            set { m_FloatVal = value; }
        }

        #endregion

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            // base.Init(part, _time);
        }
        public override float value => m_FloatVal;


#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Value_Float _graphEvent = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Value_Float();
                _graphEvent.FloatVal = m_FloatVal;
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