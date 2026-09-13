using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GValue_SetString
    {
        [SerializeField] public GString m_Value = new GString(true);
        [SerializeField] public bool m_IsSet = true;
        public delegate string GetBoolDelegate();

        public void Set(ActionStatePart _part, string pointData)
        {
            if (m_IsSet)
            {
                m_Value.SetValue(_part, pointData);
            }
        }

        public void Set(ActionStatePart _part, GetBoolDelegate action)
        {
            if (m_IsSet)
            {
#if UNITY_EDITOR
                if (action == null)
                {
                    EngineDebug.LogError("GValue_SetPoint: action is null");
                    return;
                }
#endif
                m_Value.SetValue(_part, action());
            }
        }
        public GValue_SetString Clone()
        {
#if UNITY_EDITOR
            GValue_SetString clone = new GValue_SetString();
            clone.m_Value = (GString)m_Value.Clone();
            clone.m_IsSet = m_IsSet;
            return clone;
#endif
            return this;
        }
    }
}