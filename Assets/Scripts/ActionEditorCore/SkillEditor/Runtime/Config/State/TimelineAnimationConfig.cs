using System;
using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public class TimelineAnimationConfig
    {
        public float TransitionDuration = 0.1f;
        public AnimationTransitionTimeUnit TransitionTimeUnit = AnimationTransitionTimeUnit.FixedSeconds;
        public AnimancerFadeMode FadeMode = AnimancerFadeMode.FixedDuration;
        public float StartTime = 0f;
        public AnimationStartTimeUnit StartTimeUnit = AnimationStartTimeUnit.FixedSeconds;
    }

    public enum AnimationTransitionTimeUnit
    {
        FixedSeconds = 0,
        NormalizedDuration = 1,
    }

    public enum AnimationStartTimeUnit
    {
        FixedSeconds = 0,
        NormalizedTime = 1,
    }

    public enum AnimancerFadeMode
    {
        FixedSpeed = 0,
        FixedDuration = 1,
        FromStart = 2,
        NormalizedSpeed = 3,
        NormalizedDuration = 4,
        NormalizedFromStart = 5,
    }

}
