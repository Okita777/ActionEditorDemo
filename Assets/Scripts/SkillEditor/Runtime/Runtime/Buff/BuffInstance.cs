using System.Collections.Generic;
using ActionEditor.TagSystem;
using SkillEditor.Preview;

namespace AsiSkillEditor.RunTime
{
    public sealed class BuffInstance : ICharacterBuff, IRuntimeTagContainerOwner, ITagQueryTarget
    {
        private const string DefaultBuffTagSourceId = "__buff_default__";
        private readonly List<GameUnit> _temporaryContributionTargets = new List<GameUnit>();

        public BuffInstance(BuffConfig config, IGameUnit owner, GameUnit source, float remainingDuration, int stackCount)
        {
            RuntimeId = System.Guid.NewGuid().ToString("N");
            Config = config;
            Owner = owner;
            Source = source;
            RemainingDuration = remainingDuration;
            StackCount = stackCount;
            IsInfiniteDuration = remainingDuration <= 0f;
            RuntimeTags = new RuntimeTagContainer();

            if (config == null || config.Tags == null || config.Tags.Tags == null)
            {
                return;
            }

            for (int i = 0; i < config.Tags.Tags.Count; i++)
            {
                string tag = config.Tags.Tags[i];
                if (!string.IsNullOrEmpty(tag))
                {
                    RuntimeTags.AddTag(tag, 1, DefaultBuffTagSourceId);
                }
            }
        }

        public BuffConfig Config { get; }

        public string RuntimeId { get; }

        public IGameUnit Owner { get; }

        public GameUnit Source { get; set; }

        public float RemainingDuration { get; set; }

        public bool IsInfiniteDuration { get; set; }

        public int StackCount { get; set; }

        public float UpdateElapsedTime { get; set; }

        public string BuffId => Config != null ? Config.BuffId : string.Empty;

        public string BuffName => Config != null ? Config.BuffName : string.Empty;

        public BuffType BuffType => Config != null ? Config.BuffType : BuffType.None;

        public IReadOnlyList<string> Tags => RuntimeTags.Tags;

        public RuntimeTagContainer RuntimeTags { get; }

        public IReadOnlyList<GameUnit> TemporaryContributionTargets => _temporaryContributionTargets;

        public bool HasTag(string tag)
        {
            return RuntimeTags.HasTag(tag);
        }

        public int GetTagCount(string tag)
        {
            return RuntimeTags.GetTagCount(tag);
        }

        public IReadOnlyList<string> GetTags()
        {
            return RuntimeTags.Tags;
        }

        public void RegisterTemporaryContributionTarget(GameUnit target)
        {
            if (target == null)
            {
                return;
            }

            for (int i = 0; i < _temporaryContributionTargets.Count; i++)
            {
                GameUnit existing = _temporaryContributionTargets[i];
                if (ReferenceEquals(existing, target) || Equals(existing, target))
                {
                    return;
                }
            }

            _temporaryContributionTargets.Add(target);
        }
    }
}