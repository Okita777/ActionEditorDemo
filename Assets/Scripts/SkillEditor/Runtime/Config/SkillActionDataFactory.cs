using System;
using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    public static class SkillActionDataFactory
    {
        private static readonly Dictionary<SkillActionType, Type> s_actionDataTypes = new Dictionary<SkillActionType, Type>();
        private static bool s_initialized;

        public static SkillActionData Create(SkillActionType type)
        {
            if (type == SkillActionType.None)
            {
                return null;
            }

            EnsureInitialized();
            if (!s_actionDataTypes.TryGetValue(type, out Type instanceType))
            {
                throw new InvalidOperationException($"No SkillActionData registered for enum value '{type}'.");
            }

            SkillActionData prototype = SkillDataFactoryUtility.CreateInstance<SkillActionData>(instanceType, "SkillActionData");
            return prototype.Create();
        }

        private static void EnsureInitialized()
        {
            if (s_initialized)
            {
                return;
            }

            s_initialized = true;
            s_actionDataTypes.Clear();
            SkillDataFactoryUtility.RegisterAllAssemblies(RegisterType);
        }

        private static void RegisterType(Type dataType)
        {
            object[] attributes = dataType.GetCustomAttributes(typeof(SkillActionDataAttribute), false);
            for (int i = 0; i < attributes.Length; i++)
            {
                SkillActionDataAttribute attribute = attributes[i] as SkillActionDataAttribute;
                if (attribute == null)
                {
                    continue;
                }

                if (s_actionDataTypes.TryGetValue(attribute.ActionType, out Type existingType))
                {
                    throw new InvalidOperationException(
                        $"Duplicate SkillActionData registration for '{attribute.ActionType}': '{existingType.FullName}' and '{dataType.FullName}'.");
                }

                s_actionDataTypes.Add(attribute.ActionType, dataType);
            }
        }
    }
}
