using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public partial class ActionStateMachine
    {
        private Vector3 mRootWeight = Vector3.one;
        private Quaternion mMouseXY { get; set; }
        private Quaternion mCamRot { get; set; }

        /// <summary>
        /// 动画扩展挂载点。跨 asmdef 使用（典型场景：Client 端的 Spine 支持）。
        /// 为 null 时完全等价于原有 Unity Animator 行为，不产生任何额外调用。
        /// </summary>
        public IActionAnimationExtension AnimationExtension { get; set; }

        private float OnGetAnimatorFloat(string _name)
        {
            if (AnimValid) return CurAnimator.GetFloat(_name);
            return 0;
        }
        private int OnGetAnimatorInt(string _name)
        {
            if (AnimValid) return CurAnimator.GetInteger(_name);
            return 0;
        }
        private float OnGetSpeed()
        {
            if (AnimValid) return CurAnimator.speed;
            return 0;
        }
        private void OnSetAnimatorFloat(string _name, float _value)
        {
            if (AnimValid) CurAnimator.SetFloat(_name, _value);
            AnimationExtension?.OnSetFloat(_name, _value);
        }
        private void OnSetAnimatorInt(string _name, int _value)
        {
            if (AnimValid) CurAnimator.SetInteger(_name, _value);
            AnimationExtension?.OnSetInt(_name, _value);
        }
        private void OnSetAnimatorSpeed(float _speed)
        {
            if (AnimValid) CurAnimator.speed = _speed;
            AnimationExtension?.OnSetSpeed(_speed);
        }
        private void OnCrossFade(string stateName, float normalizedTransitionDuration, int layer, float normalizedTimeOffset)
        {
            if (AnimValid) CurAnimator.CrossFade(stateName, normalizedTransitionDuration, layer, normalizedTimeOffset);
            AnimationExtension?.OnCrossFade(stateName, normalizedTransitionDuration, layer, normalizedTimeOffset);
        }
        private void OnCrossFadeInFixedTime(string stateName, float fixedTransitionDuration, int layer, float fixedTimeOffset)
        {
            if (AnimValid) CurAnimator.CrossFadeInFixedTime(stateName, fixedTransitionDuration, layer, fixedTimeOffset);
            AnimationExtension?.OnCrossFadeInFixedTime(stateName, fixedTransitionDuration, layer, fixedTimeOffset);
        }
        private void OnUpdateAnimClip(string stateName, int layer, float normalizedTimeOffset)
        {
            if (AnimValid) CurAnimator.Play(stateName, layer, normalizedTimeOffset);
            AnimationExtension?.OnUpdateAnimClip(stateName, layer, normalizedTimeOffset);
        }
        //private void OnUpdateAnim(string stateName, float fixedTransitionDuration, int layer, float fixedTimeOffset)
        //{
        //    if (AnimValid) CurAnimator.CrossFadeInFixedTime(stateName, fixedTransitionDuration, layer, fixedTimeOffset);
        //}
    }
}
