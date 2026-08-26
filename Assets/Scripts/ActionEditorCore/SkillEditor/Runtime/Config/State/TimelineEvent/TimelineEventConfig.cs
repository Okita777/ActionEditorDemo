using System;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public class TimelineEventConfig
    {
        public string EventId = Guid.NewGuid().ToString("N");
        public string DisplayName = "事件";
        public bool IsEnabled = true;
        public float TriggerTime = 0f;
        public float Duration = 0f;

        [SerializeReference]
        public TimelineEventData Data;

        public TimelineEventType EventType => Data != null ? Data.EventType : TimelineEventType.None;

        public void CreateData(TimelineEventType type)
        {
            Data = TimelineEventDataFactory.Create(type);
        }

        public void CloneData(TimelineEventData source)
        {
            Data = source == null ? null : source.Clone(Data);
        }

        public void ClearData()
        {
            Data = null;
        }
    }
}
