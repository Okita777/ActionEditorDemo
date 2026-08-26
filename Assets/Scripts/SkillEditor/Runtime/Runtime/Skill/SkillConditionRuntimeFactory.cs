using System;
using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    public static class SkillConditionRuntimeFactory
    {
        private static readonly Dictionary<Type, Type> s_runtimeTypeLookup = new Dictionary<Type, Type>();
        private static bool s_initialized;

        public static SkillConditionRuntimeBase Create(SkillConditionConfig config, SkillContext context)
        {
            SkillConditionRuntimeBase runtime = CreateReusable(config);
            runtime.BindContext(context);
            return runtime;
        }

        public static SkillConditionRuntimeBase CreateReusable(SkillConditionConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config.Data == null)
            {
                throw new InvalidOperationException("SkillConditionConfig.Data is null.");
            }

            EnsureInitialized();

            Type dataType = config.Data.GetType();
            if (!s_runtimeTypeLookup.TryGetValue(dataType, out Type runtimeType))
            {
                throw new InvalidOperationException($"No SkillConditionRuntime registered for condition data type '{dataType.FullName}'.");
            }

            return SkillRuntimeFactoryUtility.CreateInstance<SkillConditionRuntimeBase>(
                runtimeType,
                config,
                nameof(SkillConditionRuntimeBase));
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
            if (!typeof(SkillConditionRuntimeBase).IsAssignableFrom(runtimeType))
            {
                return;
            }

            object[] attributes = runtimeType.GetCustomAttributes(typeof(SkillConditionRuntimeAttribute), false);
            for (int i = 0; i < attributes.Length; i++)
            {
                SkillConditionRuntimeAttribute attribute = attributes[i] as SkillConditionRuntimeAttribute;
                if (attribute == null || attribute.ConditionDataType == null)
                {
                    continue;
                }

                if (s_runtimeTypeLookup.TryGetValue(attribute.ConditionDataType, out Type existingType))
                {
                    throw new InvalidOperationException(
                        $"Duplicate SkillConditionRuntime registration for '{attribute.ConditionDataType.FullName}': '{existingType.FullName}' and '{runtimeType.FullName}'.");
                }

                s_runtimeTypeLookup.Add(attribute.ConditionDataType, runtimeType);
            }
        }
    }
}
