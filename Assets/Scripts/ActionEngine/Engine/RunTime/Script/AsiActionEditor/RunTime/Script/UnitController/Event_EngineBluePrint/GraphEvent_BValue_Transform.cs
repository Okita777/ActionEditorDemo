using System;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_BValue_Transform : BluePrint_Transform
    {
        // [SerializeReference] protected BluePrint_Vector3 m_BluePrint_Vector3;
        [NonSerialized] protected Transform m_EVector3 = null;
        #region Property

        [EditorGraphProperty("Transform", false, EditorGraphPropertyType.EEPT_Transform)]
        public Transform mEVector3
        {
            get { return m_EVector3; }
            set { m_EVector3 = value; }
        }

        #endregion

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            // base.Init(part, _time);
        }
        public override Transform value => m_EVector3;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [NonSerialized] public GraphEvent_BValue_Transform _graphEvent = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_BValue_Transform();
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