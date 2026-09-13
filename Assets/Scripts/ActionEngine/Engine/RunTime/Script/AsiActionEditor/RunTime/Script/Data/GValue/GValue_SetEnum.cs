using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GValue_SetEnum
    {
        [SerializeField] public GEnum m_Value = new GEnum(0, true);
        [SerializeField] public bool m_IsSet = true;
        public delegate byte GetIntDelegate();

        public GValue_SetEnum(bool isSet = true)
        {
            m_IsSet = isSet;
        }

        public void Set(ActionStatePart _part, byte pointData)
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
        public GValue_SetEnum Clone()
        {
#if UNITY_EDITOR
            GValue_SetEnum clone = new GValue_SetEnum();
            clone.m_Value = (GEnum)m_Value.Clone();
            clone.m_IsSet = m_IsSet;
            return clone;
#endif
            return this;
        }
    }
}