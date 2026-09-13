using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GValue_SetUnit
    {
        [SerializeField] public GUnit m_Value = new GUnit(true);
        [SerializeField] public bool m_IsSet = true;
        public delegate ActionEngine_Unit GetUnitDelegate();

        public GValue_SetUnit(bool isSet = true)
        {
            m_IsSet = isSet;
        }

        public void Set(ActionStatePart _part, TargetUnit pointData)
        {
            if (m_IsSet)
            {
                m_Value.SetValue(_part, pointData);
            }
        }
        public void Set(ActionStatePart _part)
        {
            if (m_IsSet)
            {
                m_Value.SetValue(_part, null);
            }
        }
        public void SetNull()
        {
            if (m_IsSet) m_Value = null;
        }


        public void Set(ActionStatePart _part, GetUnitDelegate action)
        {
            if (m_IsSet)
            {
#if UNITY_EDITOR
                if (action == null)
                {
                    EngineDebug.LogError("GValue_SetUnit: action is null");
                    return;
                }
#endif
                m_Value.SetValue(_part, action());
            }
        }
        public GValue_SetUnit Clone()
        {
#if UNITY_EDITOR
            GValue_SetUnit clone = new GValue_SetUnit();
            clone.m_Value = (GUnit)m_Value.Clone();
            clone.m_IsSet = m_IsSet;
            return clone;
#endif
            return this;
        }
    }
}