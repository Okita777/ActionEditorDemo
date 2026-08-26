using System;
using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    public static class TimelineEventDataFactory
    {
        private static readonly Dictionary<TimelineEventType, Type> s_eventDataTypes = new Dictionary<TimelineEventType, Type>();
        private static bool s_initialized;

        public static TimelineEventData Create(TimelineEventType type)
        {
            if (type == TimelineEventType.None)
            {
                return null;
            }

            EnsureInitialized();
            if (!s_eventDataTypes.TryGetValue(type, out Type instanceType))
            {
                throw new InvalidOperationException($"No TimelineEventData registered for enum value '{type}'.");
            }

            TimelineEventData prototype = SkillDataFactoryUtility.CreateInstance<TimelineEventData>(instanceType, "TimelineEventData");
            return prototype.Create();
        }

        private static void EnsureInitialized()
        {
            if (s_initialized)
            {
                return;
            }

            s_initialized = true;
            s_eventDataTypes.Clear();
            SkillDataFactoryUtility.RegisterAllAssemblies(RegisterType);
        }

        private static void RegisterType(Type dataType)
        {
            object[] attributes = dataType.GetCustomAttributes(typeof(TimelineEventDataAttribute), false);
            for (int i = 0; i < attributes.Length; i++)
            {
                TimelineEventDataAttribute attribute = attributes[i] as TimelineEventDataAttribute;
                if (attribute == null)
                {
                    continue;
                }

                if (s_eventDataTypes.TryGetValue(attribute.EventType, out Type existingType))
                {
                    throw new InvalidOperationException(
                        $"Duplicate TimelineEventData registration for '{attribute.EventType}': '{existingType.FullName}' and '{dataType.FullName}'.");
                }

                s_eventDataTypes.Add(attribute.EventType, dataType);
            }
        }
    }
}
