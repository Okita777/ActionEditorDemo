using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_TrackData_Transform_CamMain : BluePrint_Transform
    {
        #region Property

        #endregion

        public override Transform value => m_ReturnVal;

        [System.NonSerialized] private Transform m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            m_ReturnVal = Camera.main.transform;
        }

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_TrackData_Transform_CamMain _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_TrackData_Transform_CamMain();
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