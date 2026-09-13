using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Value_String : BluePrint_String
    {
        public GraphEvent_Value_String(string _defaultVal = "")
        {
            m_StringVal = _defaultVal;
        }

        [SerializeField] protected string m_StringVal = string.Empty;
        #region Property

        [EditorGraphProperty("String", false, EditorGraphPropertyType.EEPT_String)]
        public string StringVal
        {
            get { return m_StringVal; }
            set { m_StringVal = value; }
        }

        #endregion

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            // base.Init(part, _time);
        }
        public override string value => m_StringVal;


#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Value_String _graphEvent = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Value_String();
                _graphEvent.StringVal = m_StringVal;
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