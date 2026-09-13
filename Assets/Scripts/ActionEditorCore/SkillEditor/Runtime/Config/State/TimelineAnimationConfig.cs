using System;
using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public class TimelineAnimationConfig
    {
        // 旧 Transition 字段仅用于兼容已有 JSON/二进制数据。
        // 新运行时过渡配置位于 StateConfig.DefaultTransition 和 StateInterruptConfig.Transition。
        public float TransitionDuration = 0.1f;
        public AnimationTransitionTimeUnit TransitionTimeUnit = AnimationTransitionTimeUnit.FixedSeconds;
        public AnimancerFadeMode FadeMode = AnimancerFadeMode.FixedDuration;

        // State 动画资源自身的固定起播偏移，不是 Transition Offset。
        public float StartTime = 0f;
        public AnimationStartTimeUnit StartTimeUnit = AnimationStartTimeUnit.FixedSeconds;
    }

    /// <summary>
    /// 一条状态边的动画过渡配置。
    /// StateConfig 上表示当前状态到 DefaultNextState；Interrupt 上表示当前状态到该打断目标。
    /// </summary>
    [Serializable]
    public sealed class StateAnimationTransitionConfig
    {
        public bool IsConfigured = false;
        public float ExitTime = 1f;
        public StateTransitionExitTimeUnit ExitTimeUnit = StateTransitionExitTimeUnit.NormalizedStateTime;
        public float BlendDuration = 0.1f;
        public AnimationTransitionTimeUnit BlendDurationUnit = AnimationTransitionTimeUnit.FixedSeconds;
        public float TargetAnimationOffset = 0f;
        public AnimationStartTimeUnit TargetAnimationOffsetUnit = AnimationStartTimeUnit.FixedSeconds;

        public static StateAnimationTransitionConfig CreateDefault()
        {
            return new StateAnimationTransitionConfig();
        }
    }

    public enum StateTransitionExitTimeUnit
    {
        FixedSeconds = 0,
        NormalizedStateTime = 1,
    }

    public enum AnimationTransitionTimeUnit
    {
        FixedSeconds = 0,
        NormalizedSourceDuration = 1,
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
