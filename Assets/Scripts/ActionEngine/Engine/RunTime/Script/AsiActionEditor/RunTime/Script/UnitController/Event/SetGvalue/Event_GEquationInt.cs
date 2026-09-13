using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_GEquationInt : IActionEventData
    {
        [SerializeField] protected GEquation m_GraphEvent = new GEquation();
        [SerializeField] protected GValue_SetInt m_SetFloat = new GValue_SetInt();
        [SerializeField] protected byte m_FH = 0;
        #region property
        [EditorProperty("设置对象", EditorPropertyType.EEPT_SetGInt)]
        public GValue_SetInt SetFloat
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
        public int GetEvenType() => -(int)EEvenTypeInternal.EET_GEquationInt;

        public IActionEventData Creact() => new Event_GEquationInt();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            // if (_isSingle)
            {
                if (m_SetFloat.m_IsSet)
                {
                    int main_val = m_SetFloat.m_Value.GetValue(_actionState);
                    int val = GraphEvent.intValue(_actionState, new ActionMachineTime(0, 0, 0, 0));

                    int returnVal = 0;

                    if (FH == 0) returnVal = main_val + val;
                    else if (FH == 1) returnVal = main_val - val;
                    else if (FH == 2) returnVal = main_val * val;
                    else if (FH == 3 && val != 0) returnVal = main_val / val;
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
            Event_GEquationInt eventCast = _eventData as Event_GEquationInt;
            eventCast.m_GraphEvent = (GEquation)m_GraphEvent.Clone();
            eventCast.m_SetFloat = m_SetFloat.Clone();
            eventCast.FH = m_FH;
            return eventCast;
        }
    }
}