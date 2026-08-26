using System;
using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    internal static class SkillEffectConditionUtility
    {
        private static readonly Dictionary<SkillConditionConfig, SkillConditionRuntimeBase> s_runtimeCache = new Dictionary<SkillConditionConfig, SkillConditionRuntimeBase>();

        public static bool Evaluate(SkillConditionConfig config, SkillContext context, SkillEffectResult lastResult)
        {
            if (config == null || config.Data == null || context == null)
            {
                return false;
            }

            try
            {
                SkillConditionRuntimeBase runtime = GetOrCreateRuntime(config);
                return runtime.EvaluateWithContext(context, lastResult);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static SkillConditionRuntimeBase GetOrCreateRuntime(SkillConditionConfig config)
        {
            if (!s_runtimeCache.TryGetValue(config, out SkillConditionRuntimeBase runtime) || runtime == null)
            {
                runtime = SkillConditionRuntimeFactory.CreateReusable(config);
                s_runtimeCache[config] = runtime;
            }

            return runtime;
        }
    }
}
