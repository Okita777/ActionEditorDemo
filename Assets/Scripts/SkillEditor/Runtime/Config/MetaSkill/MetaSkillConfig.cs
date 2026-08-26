using System;
using System.Collections.Generic;
using ActionEditor.TagSystem;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public class MetaSkillConfig : IRuntimeTagContainerOwner
    {
        public string MetaSkillId = "metaSkill_001";
        public string MetaSkillName = "New MetaSkill";

        // V3: execute/recovery are represented by two state bindings.
        public StateConfig SkillStateTimeLineState;
        public StateConfig RecoverySkillStateTimeLineState;

        // OnAdd / OnEnd 内嵌效果树，不做跨资源复用。
        public SkillEffectConfig OnAddEffect = new SkillEffectConfig();
        public SkillEffectConfig OnEndEffect = new SkillEffectConfig();
        public TagContainer Tags = new TagContainer();

        [NonSerialized] private RuntimeTagContainer _runtimeTags;

        public RuntimeTagContainer RuntimeTags => _runtimeTags ??= new RuntimeTagContainer();

        public bool HasSkillState => SkillStateTimeLineState != null;

        public bool HasRecoveryState => RecoverySkillStateTimeLineState != null;

        public string GetExecuteAnimationClipPath()
        {
            return SkillStateTimeLineState != null ? SkillStateTimeLineState.AnimationClipPath : string.Empty;
        }

        public string GetRecoveryAnimationClipPath()
        {
            return RecoverySkillStateTimeLineState != null ? RecoverySkillStateTimeLineState.AnimationClipPath : string.Empty;
        }

        public TimelineAnimationConfig GetExecuteAnimationConfig()
        {
            return SkillStateTimeLineState != null && SkillStateTimeLineState.Timeline != null
                ? SkillStateTimeLineState.Timeline.Animation
                : null;
        }

        public TimelineAnimationConfig GetRecoveryAnimationConfig()
        {
            return RecoverySkillStateTimeLineState != null && RecoverySkillStateTimeLineState.Timeline != null
                ? RecoverySkillStateTimeLineState.Timeline.Animation
                : null;
        }

        public StateTimelineConfig GetExecuteTimeline()
        {
            return SkillStateTimeLineState != null && SkillStateTimeLineState.Timeline != null
                ? SkillStateTimeLineState.Timeline
                : null;
        }

        public StateTimelineConfig GetRecoveryTimeline()
        {
            return RecoverySkillStateTimeLineState != null && RecoverySkillStateTimeLineState.Timeline != null
                ? RecoverySkillStateTimeLineState.Timeline
                : null;
        }

        public bool HasExecuteTimelineData()
        {
            return HasTimelineData(GetExecuteTimeline());
        }

        public bool HasRecoveryTimelineData()
        {
            return HasTimelineData(GetRecoveryTimeline());
        }

        private static bool HasTimelineData(StateTimelineConfig config)
        {
            return config != null &&
                   (config.Duration > 0f || (config.Tracks != null && config.Tracks.Count > 0));
        }
    }
}
