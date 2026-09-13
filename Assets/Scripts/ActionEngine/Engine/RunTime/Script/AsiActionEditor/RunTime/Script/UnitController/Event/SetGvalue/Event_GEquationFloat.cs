using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_GEquationFloat : IActionEventData
    {
        [SerializeField] protected GEquation m_GraphEvent = new GEquation();
        [SerializeField] protected GValue_SetFloat m_SetFloat = new GValue_SetFloat();
        [SerializeField] protected byte m_FH = 0;
        #region property
        [EditorProperty("设置对象", EditorPropertyType.EEPT_SetGFloat)]
        public GValue_SetFloat SetFloat
        {
            get { return m_SetFloat; }
            set { m_SetFloat = value; }
        }

        [EditorProperty("运算符号", EditorPropertyType.EEPT_Enum, EnumNames = new[] { "+", "-", "x", "\\", "=" })]
        public byte FH
        {
            get { return m_FH; }
            set { m_FH = value; }
        }

        [EditorProperty("公式结果", EditorPropertyType.EEPT_GEquation)]
        public GEquation GraphEvent
        {
            get { return m_GraphEvent; }
            set { m_GraphEvent = value; }
        }

        #endregion
        public int GetEvenType() => -(int)EEvenTypeInternal.EET_GEquationFloat;

        public IActionEventData Creact() => new Event_GEquationFloat();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            // if (_isSingle)
            {
                if (m_SetFloat.m_IsSet)
                {
                    float main_val = m_SetFloat.m_Value.GetValue(_actionState);
                    float val = GraphEvent.value(_actionState, new ActionMachineTime(0, 0, 0, 0));
                    // m_SetFloat.Init(_actionState.ActionStateMachine);

                    float returnVal = 0;

                    if (FH == 0) returnVal = main_val + val;
                    else if (FH == 1) returnVal = main_val - val;
                    else if (FH == 2) returnVal = main_val * val;
                    else if (FH == 3) returnVal = main_val / val;
                    else if (FH == 4) returnVal = val;

                    m_SetFloat.Set(_actionState, returnVal);
                }
            }
        }

        // public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        // {
        //     if (m_SetFloat.m_IsSet)
        //     {
        //         // EngineDebug.Log($"Gvalue引擎: {m_SetFloat.m_Value.mStateMachine.GetHashCode()}   当前引擎: {_actionState.ActionStateMachine.GetHashCode()}");
        //         m_SetFloat.Init(_actionState.ActionStateMachine);
        //         m_SetFloat.Set(GraphEvent.value(_actionState, _actionTime));
        //     }
        // }


        public IActionEventData Clone(IActionEventData _eventData)
        {
            // EngineDebug.LogError($"克隆: {this.GetHashCode()}");
            Event_GEquationFloat eventCast = _eventData as Event_GEquationFloat;

            if (!Application.isPlaying)
            {
                GEquation GEquationVal = m_GraphEvent;
                if (GEquationVal is null)
                {
                    EngineDebug.LogError($"<color=#ff0000>GV公式报空!!!</color>\n{EngineDebug.GetEventPath()}");
                }
                else
                {
                    int g = GEquationVal.mValueGroupIndex;
                    int i = GEquationVal.mValueIndex;
                    string _str = "(事件)->";
                    if (g == 0 && i == 0) _str += "<color=#ff0000>当前公式为默认值</color>";
                    else _str += $"[<color=#ffcc00>{g},{i}</color>]";
                    _str += $"\n{EngineDebug.GetEventPath()}";
                    EngineDebug.Log($"读取到公式蓝图!!" + _str);
                }
            }

            eventCast.m_GraphEvent = (GEquation)m_GraphEvent.Clone();
            eventCast.m_SetFloat = m_SetFloat.Clone();
            eventCast.FH = m_FH;
            return eventCast;
        }
    }
}