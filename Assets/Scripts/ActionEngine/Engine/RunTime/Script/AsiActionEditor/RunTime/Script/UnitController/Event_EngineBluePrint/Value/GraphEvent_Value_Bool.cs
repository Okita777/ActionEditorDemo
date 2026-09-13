using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Value_Bool : BluePrint_Bool
    {
        [SerializeField] protected bool m_IntVal;

        public GraphEvent_Value_Bool(bool val = false)
        {
            m_IntVal = val;
        }

        #region Property
        [EditorGraphProperty("Bool", false, EditorGraphPropertyType.EEPT_Bool)]
        public bool IntVal
        {
            get { return m_IntVal; }
            set { m_IntVal = value; }
        }

        #endregion

        public override bool value => m_IntVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            // base.Init(part, _time);
        }

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Value_Bool _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Value_Bool();
                _graphEvent.IntVal = m_IntVal;
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