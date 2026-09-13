using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Math_DebugGValue_Unit : BluePrint_Unit
    {

        [SerializeReference] protected BluePrint_Unit m_ReadValue = new GraphEvent_GValue_GUnit();
        [SerializeReference] protected BluePrint_Bool m_IsDebug = new GraphEvent_Value_Bool(true);
        [SerializeField] protected string m_DebugName = "蓝图类型Unit";
        [SerializeField] protected byte m_DebugType = 0;
        #region Property

        [EditorGraphProperty("ReadValue(Unit)", true, EditorGraphPropertyType.EEPT_GUnit, LabelWidth = 120)]
        public BluePrint_Unit ReadValue
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

        [System.NonSerialized] private TargetUnit m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_ReadValue.Init(part, _time);
            m_ReturnVal = m_ReadValue.value;
#if UNITY_EDITOR
            if (m_ReadValue.isValid(part))
            {
                IsDebug.Init(part, _time);
                GameObject _obj = m_ReturnVal.GetUnit().gameObject;
                if (IsDebug.value)
                    BluePrintDebug($"{m_DebugName} Value: {_obj.name} index[{_obj.transform.GetSiblingIndex()}]\nAction: {part.CurrentActionState.Name}   Hash:[{part.ActionStateMachine.GetHashCode()}]  Obj:[{part.ActionStateMachine.CurUnit.gameObject}]");
            }
            else
            {
                if (IsDebug.value)
                    BluePrintDebug($"{m_DebugName} [<color=#ff0000>单位为空</color>]");
            }
#endif

        }
        public override TargetUnit value => m_ReturnVal;
        public override bool isValid(ActionStatePart part) => m_ReadValue.isValid(part);
        private void BluePrintDebug(string _main)
        {
            if (DebugType == 0)
            {
                EngineDebug.Log($"蓝图Debug 【{m_DebugName}】 " + _main);
            }
            else if (DebugType == 1)
            {
                EngineDebug.LogWarning($"蓝图Debug 【{m_DebugName}】 " + _main);
            }
            else if (DebugType == 2)
            {
                EngineDebug.LogError($"蓝图Debug 【{m_DebugName}】 " + _main);
            }
            else if (DebugType == 3)
            {
                EngineDebug.DisplayDialog($"蓝图Debug  【{m_DebugName}】 ", _main, "关闭");
            }
        }

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_DebugGValue_Unit GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_DebugGValue_Unit();
                GraphEventG.ReadValue = (BluePrint_Unit)m_ReadValue.Clone();
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