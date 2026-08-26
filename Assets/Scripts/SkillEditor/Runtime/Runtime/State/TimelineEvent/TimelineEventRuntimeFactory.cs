using System;
using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    public static class TimelineEventRuntimeFactory
    {
        private static readonly Dictionary<Type, Type> s_runtimeTypeLookup = new Dictionary<Type, Type>();
        private static bool s_initialized;

        public static TimelineEventRuntimeBase Create(TimelineEventConfig config, SkillContext context)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config.Data == null)
            {
                throw new InvalidOperationException("TimelineEventConfig.Data is null.");
            }

            EnsureInitialized();

            Type dataType = config.Data.GetType();
            if (!s_runtimeTypeLookup.TryGetValue(dataType, out Type runtimeType))
            {
                throw new InvalidOperationException($"No TimelineEventRuntime registered for event data type '{dataType.FullName}'.");
            }

            return SkillRuntimeFactoryUtility.CreateAndBind(
                runtimeType,
                config,
                context,
                nameof(TimelineEventRuntimeBase),
                (TimelineEventRuntimeBase runtime, SkillContext bindContext) => runtime.BindContext(bindContext));
        }

        private static void EnsureInitialized()
        {
            if (s_initialized)
            {
                return;
            }

            s_initialized = true;
            s_runtimeTypeLookup.Clear();
            SkillRuntimeFactoryUtility.RegisterAllAssemblies(RegisterType);
        }

        private static void RegisterType(Type runtimeType)
        {
            if (!typeof(TimelineEventRuntimeBase).IsAssignableFrom(runtimeType))
            {
                return;
            }

            object[] attributes = runtimeType.GetCustomAttributes(typeof(TimelineEventRuntimeAttribute), false);
            for (int i = 0; i < attributes.Length; i++)
            {
                TimelineEventRuntimeAttribute attribute = attributes[i] as TimelineEventRuntimeAttribute;
                if (attribute == null || attribute.EventDataType == null)
                {
                    continue;
                }

                if (s_runtimeTypeLookup.TryGetValue(attribute.EventDataType, out Type existingType))
                {
                    throw new InvalidOperationException(
                        $"Duplicate MetaSkillEventRuntime registration for '{attribute.EventDataType.FullName}': '{existingType.FullName}' and '{runtimeType.FullName}'.");
                }

                s_runtimeTypeLookup.Add(attribute.EventDataType, runtimeType);
            }
        }
    }
}
