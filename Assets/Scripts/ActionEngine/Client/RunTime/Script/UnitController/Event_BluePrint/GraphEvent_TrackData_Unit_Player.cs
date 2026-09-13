using AsiTimeLine.RunTime;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_TrackData_Unit_Player : BluePrint_Unit
    {
        // [SerializeField] protected bool m_Val = true;

        #region Property
        // [EditorGraphProperty("按相机方向转换", false, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 180)]
        // public bool Val
        // {
        //     get { return m_Val; }
        //     set { m_Val = value; }
        // }
        #endregion

        public override TargetUnit value => m_ReturnVal;
        public override bool isValid(ActionStatePart part) => ActionEngineManager_Input.Instance.Player is not null;

        [System.NonSerialized] private ActionEngine_Unit m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
#if UNITY_EDITOR
            if (ActionEngineManager_Input.Instance.Player is null)
            {
                EngineDebug.LogError("控制对象是空的!!");
            }
#endif
            m_ReturnVal = ActionEngineManager_Input.Instance.Player;
        }

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_TrackData_Unit_Player _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_TrackData_Unit_Player();
                // _graphEvent.Val = m_Val;
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