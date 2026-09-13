using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Math_DebugGValue_Int : BluePrint_Int
    {
        [SerializeReference] protected BluePrint_Int m_ReadValue = new GraphEvent_Value_Int();
        [SerializeReference] protected BluePrint_Bool m_IsDebug = new GraphEvent_Value_Bool(true);

        [SerializeField] protected string m_DebugName = "蓝图类型Int";
        [SerializeField] protected byte m_DebugType = 0;

        #region Property

        [EditorGraphProperty("ReadValue(Int)", true, EditorGraphPropertyType.EEPT_Int, LabelWidth = 120)]
        public BluePrint_Int ReadValue
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

        [System.NonSerialized] private int m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_ReadValue.Init(part, _time);

#if UNITY_EDITOR
            if (part == null || part.ActionStateMachine.CurUnit == null || part.ActionStateMachine.CurUnit.gameObject == null)
            {
                EngineDebug.LogError($"重要报错！！！！！part is null.{this}");
                return;
            }

            IsDebug.Init(part, _time);
            if (IsDebug.value && EngineDebug.IsBlueprintDebugOutputEnabled())
            {
                string stall = "";
                if (m_ReadValue is GraphEvent_GValue_GInt _gint)
                {
                    GInt gv = _gint.IntVal;
                    stall = $"\n<color=#ffcc00>当前为GInt</color> GroupIndex:[{gv.mValueGroupIndex}] Index:[{gv.mValueIndex}]   type:[{gv.mType}]  ";
                    //+ $"[{EngineDebug.DebugActionStatePart(_gint.UnitVal.value.GetUnit().ActionStateMachine.FirstStatePart)}]";
                    //if ((gv.mValueGroupIndex, gv.mValueIndex) == (7, 115))
                    //{
                    //    if (_gint.UnitVal.value != null)
                    //    {
                    //        stall += $"<color=#ffcc00>{EngineDebug.DebugActionStatePart(_gint.UnitVal.value.GetUnit().ActionStateMachine.FirstStatePart)}</color>";
                    //    }
                    //    //EngineDebug.LogError($"<color=#ffcc00>旋风斩值已经修改: </color> 层数[{value}]");
                    //}
                }
                if (EngineDebug.Delegate_GetGENameCallBack is not null && m_ReadValue is GraphEvent_GValue_GEnum _bluePrintEnum)
                {
                    EngineDebug.GetGEnumNames(part, _bluePrintEnum.IntVal, out string _gn, out string _gn2, out string _gn3);
                    BluePrintDebug($"<color=#ffcc00>GEnum</color> 【{m_DebugName}】 : 值:[{_gn3}]    Group[{_gn}]  GValue[{_gn2}]" +
                        $"\nAction: {EngineDebug.DebugActionStatePart(part)}" + stall);
                }
                else
                {
                    BluePrintDebug($"【{m_DebugName}】 : {m_ReadValue.value}" +
                        $"\nAction: {EngineDebug.DebugActionStatePart(part)}" + stall);
                }
            }
#endif

            m_ReturnVal = m_ReadValue.value;
        }
        public override int value => m_ReturnVal;
        private void BluePrintDebug(string _main)
        {
            if (DebugType == 0)
            {
                EngineDebug.Log("蓝图Debug " + _main);
            }
            else if (DebugType == 1)
            {
                EngineDebug.LogWarning("蓝图Debug " + _main);
            }
            else if (DebugType == 2)
            {
                EngineDebug.LogError("蓝图Debug " + _main);
            }
            else if (DebugType == 3)
            {
                EngineDebug.DisplayDialog("蓝图Debug", _main, "关闭");
            }
        }

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_DebugGValue_Int GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_DebugGValue_Int();
                GraphEventG.ReadValue = (BluePrint_Int)m_ReadValue.Clone();
                GraphEventG.IsDebug = (BluePrint_Bool)m_IsDebug.Clone();

                GraphEventG.DebugName = m_DebugName;
                GraphEventG.IsNode = IsNode;
                GraphEventG.DebugType = m_DebugType;

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