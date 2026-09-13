using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GValue_SetInt
    {
        [SerializeField] public GInt m_Value = new GInt(true);
        [SerializeField] public bool m_IsSet = true;
        public delegate int GetIntDelegate();

        public GValue_SetInt(bool isSet = true)
        {
            m_IsSet = isSet;
        }

        public void Set(ActionStatePart _part, int pointData)
        {
            if (m_IsSet)
            {
                m_Value.SetValue(_part, pointData);
            }
        }

        public void Set(ActionStatePart _part, GetIntDelegate action)
        {
            if (m_IsSet)
            {
#if UNITY_EDITOR
                if (action == null)
                {
                    EngineDebug.LogError("GValue_SetInt: action is null");
                    return;
                }
#endif
                m_Value.SetValue(_part, action());
            }
        }
        public GValue_SetInt Clone()
        {
#if UNITY_EDITOR
            GValue_SetInt clone = new GValue_SetInt();
            clone.m_Value = (GInt)m_Value.Clone();
            clone.m_IsSet = m_IsSet;
            return clone;
#endif
            return this;
        }
    }
}