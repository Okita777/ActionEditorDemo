using System;

namespace AsiSkillEditor.RunTime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class TimelineEventRuntimeAttribute : Attribute
    {
        public Type EventDataType { get; }

        public TimelineEventRuntimeAttribute(Type eventDataType)
        {
            EventDataType = eventDataType;
        }
    }
}
