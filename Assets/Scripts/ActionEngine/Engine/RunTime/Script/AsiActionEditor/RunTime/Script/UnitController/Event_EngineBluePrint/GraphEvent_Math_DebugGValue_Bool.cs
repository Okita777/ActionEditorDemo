using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Math_DebugGValue_Bool : BluePrint_Bool
    {

        [SerializeReference] protected BluePrint_Bool m_ReadValue = new GraphEvent_Value_Bool();
        [SerializeReference] protected BluePrint_Bool m_IsDebug = new GraphEvent_Value_Bool(true);
        [SerializeField] protected string m_DebugName = "蓝图类型Bool";
        [SerializeField] protected byte m_DebugType = 0;
        #region Property

        [EditorGraphProperty("ReadValue(Bool)", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 120)]
        public BluePrint_Bool ReadValue
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

        [System.NonSerialized] private bool m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_ReadValue.Init(part, _time);

#if UNITY_EDITOR
            IsDebug.Init(part, _time);
            if (IsDebug.value)
            {
                BluePrintDebug($"【{m_DebugName}】 : {m_ReadValue.value}\nAction: {part.CurrentActionState.Name}   Hash:[{part.ActionStateMachine.GetHashCode()}]  Obj:[{part.ActionStateMachine.CurUnit.gameObject}]");
            }
#endif

            m_ReturnVal = m_ReadValue.value;
        }
        public override bool value => m_ReturnVal;

        private void BluePrintDebug(string _main)
        {
            string name = $"[<color=#ffcc00>{Time.deltaTime}</color>] 蓝图Debug ";
            string GVID = "";
            if (m_ReadValue is GraphEvent_GValue_Bool GBool)
            {
                GVID = $"\n <color=#ffcc00>GroupID[{GBool.BoolVal.mValueGroupIndex}]   IndexID[{GBool.BoolVal.mValueIndex}]  Type[{GBool.BoolVal.mType}]</color>";
            }else if(m_ReadValue is GraphEvent_SetGValue_Bool SetGBool)
            {
                GVID = $"\n <color=#ffcc00>GroupID[{SetGBool.FloatVal.mValueGroupIndex}]   IndexID[{SetGBool.FloatVal.mValueIndex}]  Type[{SetGBool.FloatVal.mType}]</color>";
            }
            if (DebugType == 0)
            {
                EngineDebug.Log(name + _main + GVID);
            }
            else if (DebugType == 1)
            {
                EngineDebug.LogWarning(name + _main + GVID);
            }
            else if (DebugType == 2)
            {
                EngineDebug.LogError(name + _main + GVID);
            }
            else if (DebugType == 3)
            {
                EngineDebug.DisplayDialog(name, _main + GVID, "关闭");
            }
        }

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_DebugGValue_Bool GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_DebugGValue_Bool();
                GraphEventG.ReadValue = (BluePrint_Bool)m_ReadValue.Clone();
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