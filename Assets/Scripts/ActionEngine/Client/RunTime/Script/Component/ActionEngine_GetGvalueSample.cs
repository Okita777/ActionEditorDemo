using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    //所有GValue都一样的获取方法，这里就只演示 GFloat和 GInt
    //inspector 面板的绘制方案见 ActionEditor_GetGvalueSample_Editor
    public class ActionEngine_GetGvalueSample : MonoBehaviour
    {
        public float m_DisPlayGFloat = 0.0f;
        public int m_DisPlayGInt = 0;

        [SerializeField] private GFloat m_GFloat = new GFloat(0, true);
        [SerializeField] private GInt m_GInt = new GInt(0, true);

        [EditorProperty("要Debug的GFloat", EditorPropertyType.EEPT_GFloat)]
        public GFloat mGFloat
        {
            get { return m_GFloat; }
            set { m_GFloat = value; }
        }

        [EditorProperty("要Debug的GInt", EditorPropertyType.EEPT_GInt)]
        public GInt mGInt
        {
            get { return m_GInt; }
            set { m_GInt = value; }
        }

        private ActionEngine_Unit m_Unit;
        // private ActionStateMachine m_ActionStateMachine;
        private void Start()
        {
            m_Unit = GetComponent<ActionEngine_Unit>();
            // m_ActionStateMachine = m_Unit.ActionStateMachine;
            // EngineDebug.Log("m_ActionStateMachine: " +( m_ActionStateMachine is not null));
        }

        private void Update()
        {
            ActionStatePart part = m_Unit.ActionStateMachine.AllActionStatePart[0];
            m_DisPlayGFloat = m_GFloat.GetValue(part);
            m_DisPlayGInt = m_GInt.GetValue(part);
        }
    }
}