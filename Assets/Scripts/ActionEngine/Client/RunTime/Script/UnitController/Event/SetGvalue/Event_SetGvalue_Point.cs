using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_SetGvalue_Point : IActionEventData
    {
        [SerializeField] protected bool m_LocalPlayer;
        [SerializeField] protected SelectTransform m_SelectTransform = new SelectTransform();
        [SerializeField] protected GValue_SetPoint m_GValueSetPoint = new GValue_SetPoint();

        #region MyRegion

        [EditorProperty("是否获取本地玩家", EditorPropertyType.EEPT_Bool)]
        public bool LocalPlayer
        {
            get { return m_LocalPlayer; }
            set { m_LocalPlayer = value; }
        }
        [EditorProperty("是否获取本地玩家", EditorPropertyType.EEPT_SelectTransform)]
        public SelectTransform SelectTransform
        {
            get { return m_SelectTransform; }
            set { m_SelectTransform = value; }
        }
        [EditorProperty("是否获取本地玩家", EditorPropertyType.EEPT_SetGPoint)]
        public GValue_SetPoint GValueSetPoint
        {
            get { return m_GValueSetPoint; }
            set { m_GValueSetPoint = value; }
        }

        #endregion
        public int GetEvenType()
        {
            throw new System.NotImplementedException();
        }

        public IActionEventData Creact() => new Event_SetGvalue_Point();

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SetGvalue_Point _event = (Event_SetGvalue_Point)_eventData;
            _event.LocalPlayer = m_LocalPlayer;
            _event.SelectTransform = m_SelectTransform;
            _event.GValueSetPoint = m_GValueSetPoint;
            return _event;
        }
    }
}