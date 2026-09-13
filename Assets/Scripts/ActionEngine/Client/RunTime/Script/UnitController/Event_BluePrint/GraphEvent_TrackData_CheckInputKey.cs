using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_TrackData_CheckInputKey : BluePrint_Bool
    {
        [SerializeReference] protected BluePrint_String m_InputKey = new GraphEvent_Value_String("InputAction");
        [SerializeField] protected EInputKeyType m_InputType = 0;
        [SerializeField] protected bool m_ExcludeSelf = false;
        #region Property
        [EditorGraphProperty("按键行为", true, EditorGraphPropertyType.EEPT_String)]
        public BluePrint_String InputKey
        {
            get { return m_InputKey; }
            set { m_InputKey = value; }
        }
        [EditorGraphProperty("按键类型", false, EditorGraphPropertyType.EEPT_Enum)]
        public EInputKeyType InputType
        {
            get { return m_InputType; }
            set { m_InputType = value; }
        }
        [EditorGraphProperty("注册按键", false, EditorGraphPropertyType.EEPT_Bool)]
        public bool ExcludeSelf
        {
            get { return m_ExcludeSelf; }
            set { m_ExcludeSelf = value; }
        }

        #endregion

        [System.NonSerialized] private bool m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能

            m_ReturnVal = false;
            ActionStatePart actionStatePart = part;
            if (actionStatePart.DisableInput) return;
            InputKey.Init(actionStatePart, _time);
            string mCheckKeyName = InputKey.value;
            if (InputType == EInputKeyType.OnDown)
            {
                m_ReturnVal = mCheckKeyName == actionStatePart.NowInputDownKey;
                if (ExcludeSelf) actionStatePart.NowInputDownKey = MotionEngineConst.NondKeyName;
            }
            else if (InputType == EInputKeyType.OnUp)
            {
                m_ReturnVal = mCheckKeyName == actionStatePart.NowInputUpKey;
                if (ExcludeSelf) actionStatePart.NowInputUpKey = MotionEngineConst.NondKeyName;
            }
            else if (InputType == EInputKeyType.OnClick)
            {
                m_ReturnVal = mCheckKeyName == actionStatePart.NowInputClickKey;
                if (ExcludeSelf) actionStatePart.NowInputClickKey = MotionEngineConst.NondKeyName;
            }
            else if (InputType == EInputKeyType.Down_State)
            {
                m_ReturnVal = actionStatePart.NowInputKey.Contains(mCheckKeyName);
            }
            else if (InputType == EInputKeyType.Up_State)
            {
                m_ReturnVal = !actionStatePart.NowInputKey.Contains(mCheckKeyName);
            }
            else
            {
                m_ReturnVal = mCheckKeyName == actionStatePart.NowInputHoldKey;
                if (ExcludeSelf) actionStatePart.NowInputHoldKey = MotionEngineConst.NondKeyName;
            }
        }

        public override bool value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_TrackData_CheckInputKey GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_TrackData_CheckInputKey();
                GraphEventG.InputKey = (BluePrint_String)InputKey.Clone();
                GraphEventG.InputType = InputType;
                GraphEventG.ExcludeSelf = m_ExcludeSelf;
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