using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public partial class ActionStatePart
    {
        private Animator mCurAnimator => mActionStateMachine.CurAnimator;
        private void OnPlayAnim(string _name, float _mixTime, int _layer, float _offsetTime)
        {
            if (!mActionStateMachine.AnimValid) return;
            if (_layer >= mCurAnimator.layerCount) return;
#if UNITY_EDITOR
            if (!mCurAnimator.HasState(_layer, Animator.StringToHash(_name)))
            {
                EngineDebug.Log($"在 {_layer} 层下不存在 [{_name}] 动画,我是由 [{mCurActionLayer}] 执行");
                return;
            }
#endif

            if (ActionStateMachine.AnimEnble)
            {
                // int getAnimLenth = CurrentActionState.AnimLenth > 0
                //     ? CurrentActionState.AnimLenth
                //     : CurrentActionState.TotalTime;
                // float animLenth = getAnimLenth * MotionEngineConst.TimeDoubling_F;
                // //_mixTime *= MotionEngineConst.TimeDoubling;
                // // mCurAnimator.CrossFade(_name, _mixTime / animLenth, _layer, _offsetTime / animLenth);
                // Debug.Log($"<color=#ffcc00>{_name}  播放动画: {CurrentActionState.AnimLenth}, Mix:{_mixTime / animLenth}, Offset:{_offsetTime / animLenth}</color>");
                // mCurAnimator.CrossFade(_name, 0.2f, _layer);
                //
                //float _speed = mCurAnimator.speed;
                //if (_speed > 0)
                //{
                //    mActionStateMachine.CrossFadeInFixedTime(_name, _mixTime / _speed, _layer, _offsetTime / _speed);
                //}
                //else
                {
                    //必须用CrossFade才能避免Animator.speed带来的影响
                    int getAnimLenth = CurrentActionState.AnimLenth > 0
                        ? CurrentActionState.AnimLenth
                        : CurrentActionState.TotalTime;
                    float animLenth = getAnimLenth * MotionEngineConst.TimeDoubling_F;
                    //_mixTime *= MotionEngineConst.TimeDoubling;
                    mActionStateMachine.CrossFade(_name, _mixTime / animLenth, _layer, _offsetTime / animLenth);
                    // Debug.Log($"<color=#ffcc00>{_name}  播放动画: {CurrentActionState.AnimLenth}, Mix:{_mixTime / animLenth}, Offset:{_offsetTime / animLenth}</color>");
                    // mCurAnimator.CrossFade(_name, 0.2f, _layer);
                }
            }
        }
    }
}