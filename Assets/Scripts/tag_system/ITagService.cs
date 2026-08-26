using System;
using System.Collections.Generic;
using SkillEditor.Preview;
using UnityEngine;

namespace ActionEditor.TagSystem
{
    public interface IRuntimeTagContainerOwner
    {
        RuntimeTagContainer RuntimeTags { get; }
    }

    public interface ITagQueryTarget
    {
        bool HasTag(string tag);
        int GetTagCount(string tag);
        IReadOnlyList<string> GetTags();
    }

    [Serializable]
    public sealed class RuntimeTagContainer
    {
        [Serializable]
        private sealed class TagInstance
        {
            public string Tag;
            public int Stack;
            public string SourceId;
        }

        [NonSerialized] private List<TagInstance> _instances;
        [NonSerialized] private List<string> _visibleTags;

        public IReadOnlyList<string> Tags
        {
            get
            {
                EnsureState();
                return _visibleTags;
            }
        }

        public bool HasTag(string tag)
        {
            return GetTagCount(tag) > 0;
        }

        public int GetTagCount(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return 0;
            }

            EnsureState();

            int total = 0;
            for (int i = 0; i < _instances.Count; i++)
            {
                TagInstance instance = _instances[i];
                if (instance == null || !string.Equals(instance.Tag, tag, StringComparison.Ordinal))
                {
                    continue;
                }

                total += Mathf.Max(0, instance.Stack);
            }

            return total;
        }

        public void AddTag(string tag, int stack = 1, string sourceId = null)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }

            EnsureState();

            _instances.Add(new TagInstance
            {
                Tag = tag,
                Stack = Mathf.Max(1, stack),
                SourceId = sourceId ?? string.Empty,
            });

            if (!_visibleTags.Contains(tag))
            {
                _visibleTags.Add(tag);
            }
        }

        public void RemoveTag(string tag, int stack = 1, string sourceId = null)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }

            EnsureState();

            int remainingToRemove = Mathf.Max(1, stack);
            bool filterBySource = !string.IsNullOrEmpty(sourceId);

            for (int i = _instances.Count - 1; i >= 0 && remainingToRemove > 0; i--)
            {
                TagInstance instance = _instances[i];
                if (instance == null || !string.Equals(instance.Tag, tag, StringComparison.Ordinal))
                {
                    continue;
                }

                if (filterBySource && !string.Equals(instance.SourceId, sourceId, StringComparison.Ordinal))
                {
                    continue;
                }

                int consumed = Mathf.Min(instance.Stack, remainingToRemove);
                instance.Stack -= consumed;
                remainingToRemove -= consumed;
                if (instance.Stack <= 0)
                {
                    _instances.RemoveAt(i);
                }
            }

            if (GetTagCount(tag) <= 0)
            {
                _visibleTags.Remove(tag);
            }
        }

        public void RemoveAllTagsFromSource(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId))
            {
                return;
            }

            EnsureState();

            bool changed = false;
            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                TagInstance instance = _instances[i];
                if (instance == null || !string.Equals(instance.SourceId, sourceId, StringComparison.Ordinal))
                {
                    continue;
                }

                _instances.RemoveAt(i);
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            _visibleTags.Clear();
            for (int i = 0; i < _instances.Count; i++)
            {
                TagInstance instance = _instances[i];
                if (instance == null || instance.Stack <= 0 || string.IsNullOrEmpty(instance.Tag) || _visibleTags.Contains(instance.Tag))
                {
                    continue;
                }

                _visibleTags.Add(instance.Tag);
            }
        }

        private void EnsureState()
        {
            _instances ??= new List<TagInstance>();
            _visibleTags ??= new List<string>();
        }
    }

    public interface ITagQueryService
    {
        bool HasTag(GameUnit target, string tag);
    }

    public interface ITagService : ITagQueryService
    {
        int GetTagCount(GameUnit target, string tag);
        void AddTag(GameUnit target, string tag, int stack = 1, string sourceId = null);
        void RemoveTag(GameUnit target, string tag, int stack = 1, string sourceId = null);
    }
}
