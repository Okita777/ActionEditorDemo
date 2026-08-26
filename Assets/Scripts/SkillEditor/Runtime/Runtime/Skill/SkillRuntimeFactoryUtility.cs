using System;
using System.Reflection;

namespace AsiSkillEditor.RunTime
{
    internal static class SkillRuntimeFactoryUtility
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

        public static TRuntime CreateAndBind<TRuntime>(
            Type runtimeType,
            object config,
            SkillContext context,
            string runtimeName,
            Action<TRuntime, SkillContext> bindContext)
            where TRuntime : class
        {
            object instance = Activator.CreateInstance(runtimeType, config);
            if (!(instance is TRuntime runtime))
            {
                throw new InvalidOperationException($"Runtime '{runtimeType.FullName}' must inherit {runtimeName}.");
            }

            bindContext?.Invoke(runtime, context);
            return runtime;
        }

        public static TRuntime CreateInstance<TRuntime>(
            Type runtimeType,
            object config,
            string runtimeName)
            where TRuntime : class
        {
            object instance = Activator.CreateInstance(runtimeType, config);
            if (!(instance is TRuntime runtime))
            {
                throw new InvalidOperationException($"Runtime '{runtimeType.FullName}' must inherit {runtimeName}.");
            }

            return runtime;
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
                Type runtimeType = types[i];
                if (runtimeType == null || runtimeType.IsAbstract)
                {
                    continue;
                }

                registerType(runtimeType);
            }
        }
    }
}
