using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class ActionInterruptGroup
    {
        [System.Serializable]
        public struct SActionOffset
        {
            public int m_ActionStateID;
            public int m_ActionOffsetTime;

            public SActionOffset(int actionStateID, int actionOffsetTime)
            {
                m_ActionStateID = actionStateID;
                m_ActionOffsetTime = actionOffsetTime;
            }
        }

        [SerializeField] private int m_ActionID;
        [SerializeField] private int m_Offset;
        [SerializeField] private List<int> m_ActionHide;
        [SerializeField] private List<SActionOffset> m_ActionOffset;
        [NonSerialized] private Dictionary<int, int> m_DicActionOffset = null;
        [NonSerialized] private bool m_IsInitialized = false;

        public bool TryGetActionOffset(int actionStateID, out int actionOffset)
        {
            if (!m_IsInitialized)
            {
                m_DicActionOffset = new Dictionary<int, int>();
                if (m_ActionOffset is not null)
                {
                    foreach (SActionOffset _offset in m_ActionOffset)
                    {
                        m_DicActionOffset.TryAdd(_offset.m_ActionStateID, _offset.m_ActionOffsetTime);
                    }
                }
                m_IsInitialized = true;
            }
            return m_DicActionOffset.TryGetValue(actionStateID, out actionOffset);
        }

        public ActionInterruptGroup(int _mActionID, int _mOffset, List<int> _mActionHide, List<SActionOffset> _mActionOffset)
        {
            m_ActionID = _mActionID;
            m_Offset = _mOffset;
            m_ActionHide = _mActionHide;
            m_ActionOffset = _mActionOffset;
        }

        public int ActionID
        {
            get { return m_ActionID; }
            set { m_ActionID = value; }
        }

        public int Offset
        {
            get { return m_Offset; }
            set { m_Offset = value; }
        }

        public List<int> ActionHide
        {
            get { return m_ActionHide; }
            set { m_ActionHide = value; }
        }

        public List<SActionOffset> ActionOffset
        {
            get { return m_ActionOffset; }
            set { m_ActionOffset = value; }
        }
    }
}