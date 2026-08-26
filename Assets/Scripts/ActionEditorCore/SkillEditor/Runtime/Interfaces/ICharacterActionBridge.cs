namespace AsiSkillEditor.RunTime
{
    // 纯表现层动画控制接口。
    public interface ICharacterAnimationController
    {
        void ConfigureAnimationLayers(UnitConfig unitConfig);
        void PlayStateAnimation(SkillContext context, StateConfig stateConfig, StateInterruptConfig interruptConfig);
        void SeekStateAnimation(SkillContext context, StateConfig stateConfig, float animationTime);
        void StopStateAnimation(SkillContext context, StateConfig stateConfig, bool interrupted);
        void StopAllStateAnimations(SkillContext context);
        void SetPlaybackScale(float scale);
    }
}
