using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Math_DebugGValue_Float : BluePrint_Float
    {
        [SerializeReference] protected BluePrint_Float m_ReadValue = new GraphEvent_Value_Float();
        [SerializeReference] protected BluePrint_Bool m_IsDebug = new GraphEvent_Value_Bool(true);

        [SerializeField] protected string m_DebugName = "蓝图类型Float";
        [SerializeField] protected byte m_DebugType = 0;

        #region Property

        [EditorGraphProperty("ReadValue(Float)", true, EditorGraphPropertyType.EEPT_Float, LabelWidth = 120)]
        public BluePrint_Float ReadValue
        {
            get { return m_ReadValue; }
            set { m_ReadValue = value; }
        }
        [EditorGraphProperty("是否输出Debug", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 120)]
        public BluePrint_Bool IsDebug
        {
            get
            {
                if (m_IsDebug is null) m_IsDebug = new GraphEvent_Value_Bool(true);
                return m_IsDebug;
            }
            set { m_IsDebug = value; }
        }
        [EditorGraphProperty("DebugName", false, EditorGraphPropertyType.EEPT_String, LabelWidth = 80)]
        public string DebugName
        {
            get { return m_DebugName; }
            set { m_DebugName = value; }
        }
        [EditorGraphProperty("DebugType", false, EditorGraphPropertyType.EEPT_EnumCustom, LabelWidth = 80, EnumNames = new[] { "默认", "警告", "错误", "弹窗" })]
        public byte DebugType
        {
            get { return m_DebugType; }
            set { m_DebugType = value; }
        }
        #endregion

        [System.NonSerialized] private float m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_ReadValue.Init(part, _time);

#if UNITY_EDITOR
            IsDebug.Init(part, _time);
            if (IsDebug.value)
                BluePrintDebug($"[GVHash(<color=#ffcc00>{_time.Deltatime}</color>)] 蓝图Debug“ 【{m_DebugName}】 : {m_ReadValue.value}\nAction: {part.CurrentActionState.Name}   Hash:[{part.ActionStateMachine.GetHashCode()}]  Obj:[{part.ActionStateMachine.CurUnit.gameObject.name}]" +
                    $"\n[{EngineDebug.DebugActionStatePart(part)}]");
#endif

            m_ReturnVal = m_ReadValue.value;
        }
        public override float value => m_ReturnVal;

        private void BluePrintDebug(string _main)
        {
            if (DebugType == 0)
            {
                EngineDebug.Log(_main);
            }
            else if (DebugType == 1)
            {
                EngineDebug.LogWarning(_main);
            }
            else if (DebugType == 2)
            {
                EngineDebug.LogError(_main);
            }
            else if (DebugType == 3)
            {
                EngineDebug.DisplayDialog("蓝图Debug", _main, "关闭");
            }
        }
#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_DebugGValue_Float GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_DebugGValue_Float();
                GraphEventG.ReadValue = (BluePrint_Float)m_ReadValue.Clone();
                GraphEventG.IsDebug = (BluePrint_Bool)m_IsDebug.Clone();

                GraphEventG.DebugName = m_DebugName;
                GraphEventG.DebugType = m_DebugType;

                GraphEventG.IsNode = IsNode;
                //在保存好文件后重置状态
                ActionSaveFlishEvent.ActionEvent.AddListener(() =>
                {
                    GraphEventG = null;
                });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}