using System;
using System.Collections.Generic;
using SkillEditor.Preview;
using UnityEngine;

namespace ActionEditor.TagSystem
{
    /// <summary>
    /// Independent runtime tag system service. State lives on the target's runtime tag container.
    /// </summary>
    public sealed class TagRuntimeService : ITagService
    {
        public bool HasTag(GameUnit target, string tag)
        {
            return target != null && target.HasTag(tag);
        }

        public int GetTagCount(GameUnit target, string tag)
        {
            return target != null ? target.GetTagCount(tag) : 0;
        }

        public void AddTag(GameUnit target, string tag, int stack = 1, string sourceId = null)
        {
            target?.RuntimeTags?.AddTag(tag, stack, sourceId);
        }

        public void RemoveTag(GameUnit target, string tag, int stack = 1, string sourceId = null)
        {
            target?.RuntimeTags?.RemoveTag(tag, stack, sourceId);
        }
    }
}
