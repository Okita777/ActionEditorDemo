using System;
using System.Reflection;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    internal static class SkillDataFactoryUtility
    {
        public static void RegisterAllAssemblies(Action<Type> registerType)
        {
            if (registerType == null)
            {
                return;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                RegisterAssembly(assemblies[i], registerType);
            }
        }

        public static TBase CreateInstance<TBase>(Type instanceType, string typeName)
            where TBase : class
        {
            object instance = Activator.CreateInstance(instanceType);
            if (!(instance is TBase typedInstance))
            {
                throw new InvalidOperationException($"Registered type '{instanceType.FullName}' must implement {typeName}.");
            }

            return typedInstance;
        }

        public static TData CloneSerializable<TData>(TData source, TData target = null)
            where TData : class, new()
        {
            TData result = target ?? new TData();
            if (source == null)
            {
                return result;
            }

            string json = JsonUtility.ToJson(source);
            if (!string.IsNullOrEmpty(json))
            {
                JsonUtility.FromJsonOverwrite(json, result);
            }

            return result;
        }

        private static void RegisterAssembly(Assembly assembly, Action<Type> registerType)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types;
            }

            if (types == null)
            {
                return;
            }

            for (int i = 0; i < types.Length; i++)
            {
                Type dataType = types[i];
                if (dataType == null || dataType.IsAbstract)
                {
                    continue;
                }

                registerType(dataType);
            }
        }
    }
}
