using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GValue_SetPoint
    {
        [SerializeField] public GPoint m_Value = new GPoint(true);
        [SerializeField] public bool m_IsSet = true;
        public delegate PointData GetPointDataDelegate();

        public GValue_SetPoint(bool isSet = true)
        {
            m_IsSet = isSet;
        }

        public void Set(ActionStatePart _part, PointData pointData)
        {
            if (m_IsSet)
            {
                m_Value.SetValue(_part, pointData);
            }
        }

        public void Set(ActionStatePart _part, Transform _transform)
        {
            if (m_IsSet)
            {
                m_Value.SetValue(_part, new PointData(_transform.position, _transform.rotation));
            }
        }
        public void Set(ActionStatePart _part, GetPointDataDelegate action)
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

        public GValue_SetPoint Clone()
        {
#if UNITY_EDITOR
            GValue_SetPoint clone = new GValue_SetPoint();
            clone.m_Value = (GPoint)m_Value.Clone();
            clone.m_IsSet = m_IsSet;
            return clone;
#endif
            return this;
        }
    }
}