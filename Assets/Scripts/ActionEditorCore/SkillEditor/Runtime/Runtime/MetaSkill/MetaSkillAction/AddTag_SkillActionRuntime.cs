using System.Collections.Generic;
using ActionEditor.TagSystem;
using SkillEditor.Preview;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    [SkillActionRuntime(typeof(AddTag_SkillActionData))]
    public sealed class AddTag_SkillActionRuntime : SkillActionRuntimeBase
    {
        private readonly AddTag_SkillActionData _data;

        public AddTag_SkillActionRuntime(SkillActionConfig config) : base(config)
        {
            _data = mData as AddTag_SkillActionData;
        }

        public override SkillEffectResult Execute(SkillEffectResult lastResult)
        {
            if (_data == null || _data.Args == null)
            {
                return SkillEffectResult.Fail(SkillEffectFailureKind.InvalidData);
            }

            if (mContext == null)
            {
                return SkillEffectResult.Fail(SkillEffectFailureKind.MissingContext);
            }

            if (!(mContext.TagQueryService is ITagService tagService))
            {
                return SkillEffectResult.Fail(SkillEffectFailureKind.MissingService);
            }

            GameUnit target = SkillTargetResolver.Resolve(_data.Args.QueryTarget, mContext);
            if (target == null)
            {
                return SkillEffectResult.Fail(SkillEffectFailureKind.MissingTarget);
            }

            List<string> validTags = CollectValidTags(_data.Args.Tags);
            if (validTags.Count == 0)
            {
                return SkillEffectResult.Fail(SkillEffectFailureKind.InvalidData);
            }

            string sourceId = string.Empty;
            if (_data.Args.ApplyLifetime == AttributeApplyLifetime.TemporaryBuff)
            {
                sourceId = mContext.ActiveBuffSourceId;
                if (string.IsNullOrEmpty(sourceId))
                {
                    return SkillEffectResult.Fail(SkillEffectFailureKind.MissingContext);
                }

                mContext.RegisterTemporaryContributionTarget?.Invoke(target);
            }

            int stack = Mathf.Max(1, _data.Args.Stack);
            for (int i = 0; i < validTags.Count; i++)
            {
                tagService.AddTag(target, validTags[i], stack, sourceId);
            }

            ActionContext actionContext = new ActionContext
            {
                Caster = mContext.Caster,
                PrimaryTarget = mContext.PrimaryTarget,
                HasExecuted = true,
                Succeeded = true,
            };
            actionContext.AffectedTargets.Add(target);
            for (int i = 0; i < validTags.Count; i++)
            {
                actionContext.DataContext.AddTag(target, validTags[i], stack);
            }

            return SkillEffectResult.Succeed(actionContext);
        }

        private static List<string> CollectValidTags(List<string> source)
        {
            List<string> result = new List<string>();
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
    }
}