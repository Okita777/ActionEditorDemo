using System;
using ActionEditor.TagSystem;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    [TimelineEventRuntime(typeof(AddTag_TimelineEventData))]
    public sealed class AddTag_TimelineEventRuntime : TimelineEventRuntimeBase
    {
        private readonly AddTag_TimelineEventData _data;
        private object _activeCarrier;
        private readonly System.Collections.Generic.List<string> _activeTags = new System.Collections.Generic.List<string>();

        public AddTag_TimelineEventRuntime(TimelineEventConfig config) : base(config)
        {
            _data = mData as AddTag_TimelineEventData;
        }

        protected override void OnBegin()
        {
            if (!TryResolve(out object carrier, out System.Collections.Generic.List<string> tags, out int stack))
            {
                return;
            }

            _activeCarrier = carrier;
            _activeTags.Clear();
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                AddCarrierTag(carrier, tag, stack, GetSourceId());
                _activeTags.Add(tag);
            }

        }

        protected override void OnEnd(bool interrupted)
        {
            if (_activeCarrier == null || mContext == null ||
                _activeTags.Count == 0 || _data == null || _data.Args == null)
            {
                _activeCarrier = null;
                _activeTags.Clear();
                return;
            }

            for (int i = 0; i < _activeTags.Count; i++)
            {
                RemoveCarrierTag(_activeCarrier, _activeTags[i], Mathf.Max(1, _data.Args.Stack), GetSourceId());
            }

            _activeCarrier = null;
            _activeTags.Clear();
        }

        protected override void OnTrigger()
        {
            if (!TryResolve(out object carrier, out System.Collections.Generic.List<string> tags, out int stack))
            {
                return;
            }

            // Single-frame semantics: add and remove within trigger flow.
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                AddCarrierTag(carrier, tag, stack, string.Empty);
                RemoveCarrierTag(carrier, tag, stack, string.Empty);
            }

        }

        private bool TryResolve(out object carrier, out System.Collections.Generic.List<string> tags, out int stack)
        {
            carrier = null;
            tags = null;
            stack = 0;

            if (_data == null || _data.Args == null)
            {
                throw new InvalidOperationException("AddTag meta skill event data is invalid.");
            }

            if (mContext == null)
            {
                throw new InvalidOperationException("SkillContext is missing.");
            }

            object resolvedCarrier = mContext.CurrentMetaSkillConfig;
            if (resolvedCarrier == null)
            {
                return false;
            }

            System.Collections.Generic.List<string> validTags = CollectValidTags(_data.Args.Tags);
            if (validTags.Count == 0)
            {
                return false;
            }

            carrier = resolvedCarrier;
            tags = validTags;
            stack = Mathf.Max(1, _data.Args.Stack);
            return true;
        }

        private static void AddCarrierTag(object carrier, string tag, int stack, string sourceId)
        {
            RuntimeTagContainer container = ResolveCarrierTagContainer(carrier);
            container?.AddTag(tag, stack, sourceId);
        }

        private static void RemoveCarrierTag(object carrier, string tag, int stack, string sourceId)
        {
            RuntimeTagContainer container = ResolveCarrierTagContainer(carrier);
            container?.RemoveTag(tag, stack, sourceId);
        }

        private static RuntimeTagContainer ResolveCarrierTagContainer(object carrier)
        {
            switch (carrier)
            {
                case RuntimeTagContainer runtimeTagContainer:
                    return runtimeTagContainer;
                case IRuntimeTagContainerOwner runtimeTagOwner:
                    return runtimeTagOwner.RuntimeTags;
                default:
                    return null;
            }
        }

        private static System.Collections.Generic.List<string> CollectValidTags(System.Collections.Generic.List<string> source)
        {
            System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                string tag = source[i];
                if (string.IsNullOrEmpty(tag) || result.Contains(tag))
                {
                    continue;
                }

                result.Add(tag);
            }

            return result;
        }

        private string GetSourceId()
        {
            return mConfig != null ? mConfig.EventId : string.Empty;
        }
    }
}
