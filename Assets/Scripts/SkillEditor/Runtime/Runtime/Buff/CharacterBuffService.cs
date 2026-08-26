using SkillEditor.Preview;

namespace AsiSkillEditor.RunTime
{
    public sealed class CharacterBuffService : IBuffService
    {
        public bool HasBuff(GameUnit target, string buffId)
        {
            return target != null && target.HasBuff(buffId);
        }

        public void AddBuff(GameUnit target, BuffActionArgs args, SkillContext context)
        {
            if (args == null || string.IsNullOrEmpty(args.BuffId))
            {
                UnityEngine.Debug.LogWarning("CharacterBuffService.AddBuff: args or BuffId is missing.");
                return;
            }

            if (target == null)
            {
                UnityEngine.Debug.LogWarning($"CharacterBuffService.AddBuff: target is null for buff '{args.BuffId}'.");
                return;
            }

            BuffConfig buffConfig = null;
            if (!SkillRuntimeLoadData.Instance.LoadBuff(args.BuffId, config => buffConfig = config) || buffConfig == null)
            {
                UnityEngine.Debug.LogWarning($"CharacterBuffService.AddBuff: failed to load BuffConfig '{args.BuffId}' for target '{target.name}'.");
                return;
            }

            UnityEngine.Debug.Log($"CharacterBuffService.AddBuff: applying buff '{args.BuffId}' to '{target.name}' with actionDuration={args.Duration:0.###}, configDuration={buffConfig.Duration:0.###}.", target.UnitObject);
            target.Buffs.AddBuff(buffConfig, args, context);
        }

        public void RemoveBuff(GameUnit target, BuffActionArgs args, SkillContext context)
        {
            if (args == null)
            {
                UnityEngine.Debug.LogWarning("CharacterBuffService.RemoveBuff: args is missing.");
                return;
            }

            if (target == null)
            {
                UnityEngine.Debug.LogWarning($"CharacterBuffService.RemoveBuff: target is null for buff '{args.BuffId}'.");
                return;
            }

            UnityEngine.Debug.Log($"CharacterBuffService.RemoveBuff: removing buff '{args.BuffId}' from '{target.name}'.", target.UnitObject);
            target.Buffs.RemoveBuff(args.BuffId, context);
        }
    }
}