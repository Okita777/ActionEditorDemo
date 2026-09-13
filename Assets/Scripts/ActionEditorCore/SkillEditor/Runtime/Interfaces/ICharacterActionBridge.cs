namespace AsiSkillEditor.RunTime
{
    public sealed class StateAnimationTransitionContext
    {
        public StateConfig SourceState;
        public StateConfig TargetState;
        public StateAnimationTransitionConfig Transition;
        public float TargetStateTime;
        public StateTransitionRequestType RequestType;
    }

    // 纯表现层动画控制接口。
    public interface ICharacterAnimationController
    {
        void ConfigureAnimationLayers(UnitConfig unitConfig);
        void PlayStateAnimation(SkillContext context, StateConfig stateConfig, StateInterruptConfig interruptConfig);
        bool CanPlayStateAnimation(StateConfig stateConfig);
        void TransitionStateAnimation(SkillContext context, StateAnimationTransitionContext transitionContext);
        void SeekStateAnimation(SkillContext context, StateConfig stateConfig, float animationTime);
        void StopStateAnimation(SkillContext context, StateConfig stateConfig, bool interrupted);
        void StopAllStateAnimations(SkillContext context);
        void SetPlaybackScale(float scale);
    }
}
