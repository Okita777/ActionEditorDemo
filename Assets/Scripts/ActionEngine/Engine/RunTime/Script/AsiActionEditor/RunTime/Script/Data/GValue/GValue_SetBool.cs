using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GValue_SetBool
    {
        [SerializeField] public GBool m_Value = new GBool(true);
        [SerializeField] public bool m_IsSet = true;
        public delegate bool GetBoolDelegate();

        public GValue_SetBool(bool isSet = true)
        {
            m_IsSet = isSet;
        }

        public void Set(ActionStatePart _part, bool _val)
        {
            if (m_IsSet)
            {
                m_Value.SetValue(_part, _val);
            }
        }

        public void Set(ActionStatePart _part, GetBoolDelegate action)
        {
            if (m_IsSet)
            {
#if UNITY_EDITOR
                if (action == null)
                {
                    EngineDebug.LogError("GValue_SetBool: action is null");
                    return;
                }
#endif
                m_Value.SetValue(_part, action());
            }
        }
        public GValue_SetBool Clone()
        {
#if UNITY_EDITOR
            GValue_SetBool clone = new GValue_SetBool();
            clone.m_Value = (GBool)m_Value.Clone();
            clone.m_IsSet = m_IsSet;
            return clone;
#endif
            return this;
        }
    }
}