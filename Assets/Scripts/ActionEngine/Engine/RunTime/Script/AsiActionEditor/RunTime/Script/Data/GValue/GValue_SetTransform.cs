using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GValue_SetTransform
    {
        [SerializeField] public GTransform m_Value = new GTransform(true);
        [SerializeField] public bool m_IsSet = true;
        public delegate Transform GetTransformDelegate();

        public GValue_SetTransform(bool isSet = true)
        {
            m_IsSet = isSet;
        }

        public void Set(ActionStatePart _part, Transform pointData)
        {
            if (m_IsSet)
            {
                m_Value.SetValue(_part, pointData);
            }
        }

        public void SetNull(ActionStatePart _part)
        {
            if (m_IsSet) m_Value.SetValue(_part, null);
        }

        public void Set(ActionStatePart _part, GetTransformDelegate action)
        {
            if (m_IsSet)
            {
#if UNITY_EDITOR
                if (action == null)
                {
                    EngineDebug.LogError("GValue_SetTransform: action is null");
                    return;
                }
#endif
                m_Value.SetValue(_part, action());
            }
        }

        public GValue_SetTransform Clone()
        {
#if UNITY_EDITOR
            GValue_SetTransform clone = new GValue_SetTransform();
            clone.m_Value = (GTransform)m_Value.Clone();
            clone.m_IsSet = m_IsSet;
            return clone;
#endif
            return this;
        }
    }
}