using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public sealed class StateTimelineConfig
    {
        public float Duration = 0f;
        public TimelineAnimationConfig Animation = new TimelineAnimationConfig();
        public List<TimelineTrackConfig> Tracks = new List<TimelineTrackConfig>();
        public List<StateInterruptTrackConfig> InterruptTracks = new List<StateInterruptTrackConfig>();
        public List<StateInterruptConfig> Interrupts = new List<StateInterruptConfig>();
    }

    [Serializable]
    public sealed class StateInterruptTrackConfig
    {
        public string TrackId = string.Empty;
        public string DisplayName = string.Empty;
        public bool IsEnabled = true;
        public List<StateInterruptConfig> Interrupts = new List<StateInterruptConfig>();
    }

    [Serializable]
    public sealed class StateInterruptConfig
    {
        public bool IsEnabled = true;
        public string TargetStateId = string.Empty;
        public float TriggerTime = 0f;
        public float Duration = 0f;
        public float ExecuteTime = 0f;
        public int SortOrder = 0;
        public bool CheckAllConditions = true;
        [OptionalField] public StateAnimationTransitionConfig Transition = StateAnimationTransitionConfig.CreateDefault();

        // 旧字段仅用于迁移已有资源。
        public bool UseTransitionOverride = false;
        public float TransitionDuration = 0f;
        public AnimationTransitionTimeUnit TransitionTimeUnit = AnimationTransitionTimeUnit.FixedSeconds;
        public StateTransitionPolicy TransitionPolicy = StateTransitionPolicy.SameLayerOnly;
        [SerializeReference] public List<IStateInterruptCondition> Conditions = new List<IStateInterruptCondition>();
    }

    public interface IStateInterruptCondition
    {
        string GetDisplayName();
        bool Evaluate(StateInterruptContext context);
        IStateInterruptCondition Clone();
    }
}