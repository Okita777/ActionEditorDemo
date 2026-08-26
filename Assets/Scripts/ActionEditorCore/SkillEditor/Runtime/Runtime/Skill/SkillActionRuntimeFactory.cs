using System;
using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    public static class SkillActionRuntimeFactory
    {
        private static readonly Dictionary<Type, Type> s_runtimeTypeLookup = new Dictionary<Type, Type>();
        private static bool s_initialized;

        public static SkillActionRuntimeBase Create(SkillActionConfig config, SkillContext context)
        {
            SkillActionRuntimeBase runtime = CreateReusable(config);
            runtime.BindContext(context);
            return runtime;
        }

        public static SkillActionRuntimeBase CreateReusable(SkillActionConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config.Data == null)
            {
                throw new InvalidOperationException("SkillActionConfig.Data is null.");
            }

            EnsureInitialized();

            Type dataType = config.Data.GetType();
            if (!s_runtimeTypeLookup.TryGetValue(dataType, out Type runtimeType))
            {
                throw new InvalidOperationException($"No SkillActionRuntime registered for action data type '{dataType.FullName}'.");
            }

            return SkillRuntimeFactoryUtility.CreateInstance<SkillActionRuntimeBase>(
                runtimeType,
                config,
                nameof(SkillActionRuntimeBase));
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
            if (!typeof(SkillActionRuntimeBase).IsAssignableFrom(runtimeType))
            {
                return;
            }

            object[] attributes = runtimeType.GetCustomAttributes(typeof(SkillActionRuntimeAttribute), false);
            for (int i = 0; i < attributes.Length; i++)
            {
                SkillActionRuntimeAttribute attribute = attributes[i] as SkillActionRuntimeAttribute;
                if (attribute == null || attribute.ActionDataType == null)
                {
                    continue;
                }

                if (s_runtimeTypeLookup.TryGetValue(attribute.ActionDataType, out Type existingType))
                {
                    throw new InvalidOperationException(
                        $"Duplicate SkillActionRuntime registration for '{attribute.ActionDataType.FullName}': '{existingType.FullName}' and '{runtimeType.FullName}'.");
                }

                s_runtimeTypeLookup.Add(attribute.ActionDataType, runtimeType);
            }
        }
    }
}
