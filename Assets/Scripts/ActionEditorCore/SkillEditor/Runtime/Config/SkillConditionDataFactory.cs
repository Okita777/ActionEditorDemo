using System;
using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    public static class SkillConditionDataFactory
    {
        private static readonly Dictionary<SkillConditionType, Type> s_conditionDataTypes = new Dictionary<SkillConditionType, Type>();
        private static bool s_initialized;

        public static SkillConditionData Create(SkillConditionType type)
        {
            if (type == SkillConditionType.None)
            {
                return null;
            }

            EnsureInitialized();
            if (!s_conditionDataTypes.TryGetValue(type, out Type instanceType))
            {
                throw new InvalidOperationException($"No SkillConditionData registered for enum value '{type}'.");
            }

            SkillConditionData prototype = SkillDataFactoryUtility.CreateInstance<SkillConditionData>(instanceType, "SkillConditionData");
            return prototype.Create();
        }

        private static void EnsureInitialized()
        {
            if (s_initialized)
            {
                return;
            }

            s_initialized = true;
            s_conditionDataTypes.Clear();
            SkillDataFactoryUtility.RegisterAllAssemblies(RegisterType);
        }

        private static void RegisterType(Type dataType)
        {
            object[] attributes = dataType.GetCustomAttributes(typeof(SkillConditionDataAttribute), false);
            for (int i = 0; i < attributes.Length; i++)
            {
                SkillConditionDataAttribute attribute = attributes[i] as SkillConditionDataAttribute;
                if (attribute == null)
                {
                    continue;
                }

                if (s_conditionDataTypes.TryGetValue(attribute.ConditionType, out Type existingType))
                {
                    throw new InvalidOperationException(
                        $"Duplicate SkillConditionData registration for '{attribute.ConditionType}': '{existingType.FullName}' and '{dataType.FullName}'.");
                }

                s_conditionDataTypes.Add(attribute.ConditionType, dataType);
            }
        }
    }
}
