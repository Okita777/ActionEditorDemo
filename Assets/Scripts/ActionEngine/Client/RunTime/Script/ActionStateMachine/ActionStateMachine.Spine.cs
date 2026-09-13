// =============================================================================
// ActionStateMachine 的 Spine 扩展（运行时 / 编辑器均可用：播放 / 混合 / 预览）
//
// 约束：
//   1. 所有 Spine 相关代码仅允许出现在 Assets/Scripts/ActionEngine/Client 下；
//   2. 所有 Spine 引用统一用 ACTION_ENGINE_SPINE 宏包裹，未定义时本文件为空；
//   3. Engine 与 Client 属于不同 asmdef，因此不能使用 partial class 跨 assembly 扩展，
//      这里改为：
//        - 实现 Engine 侧公开的 IActionAnimationExtension 接口（零 Spine 依赖），
//          由 ActionStateMachine 内部在 CrossFade / UpdateAnimClip 等调用处转发；
//        - 通过 extension methods 向外部提供 Spine 专用 API（BindSpineAnim 等），
//          避免污染 Engine 类型系统。
// =============================================================================
#if ACTION_ENGINE_SPINE
using AsiActionEngine.RunTime;
using Spine;
using Spine.Unity;
using UnityEngine;
using Animation = Spine.Animation;
using AnimationState = Spine.AnimationState;

namespace AsiActionEngine.Client.RunTime
{
    /// <summary>
    /// Spine 侧的动画路由实现。作为 <see cref="IActionAnimationExtension"/> 的
    /// 具体实现挂到 <see cref="ActionStateMachine.AnimationExtension"/> 上，由
    /// Engine 统一分发 CrossFade / UpdateAnimClip / SetSpeed 等调用。
    ///
    /// 与 Unity Animator 的优先级策略：
    ///   仅当 Unity Animator 不可用（AnimValid == false）时才会实际接管，从而保证
    ///   既有动画流程零侵入；若将来需要"Animator + Spine 同帧双驱动"可在此放开判断。
    /// </summary>
    internal sealed class SpineAnimationExtension : IActionAnimationExtension
    {
        private readonly ActionStateMachine mOwner;
        public SkeletonAnimation Target { get; set; }

        public SpineAnimationExtension(ActionStateMachine owner)
        {
            mOwner = owner;
        }

        private bool ShouldRoute()
            => mOwner != null && !mOwner.AnimValid && Target != null;

        public void OnCrossFade(string stateName, float normalizedTransitionDuration, int layer, float normalizedTimeOffset)
        {
            if (!ShouldRoute()) return;
            TrackEntry entry = SpineInternal.Play(Target, stateName, layer, loop: true);
            if (entry != null && normalizedTransitionDuration > 0f)
                entry.MixDuration = normalizedTransitionDuration;
            if (normalizedTimeOffset > 0f)
                SpineInternal.SeekNormalized(Target, stateName, layer, normalizedTimeOffset);
        }

        public void OnCrossFadeInFixedTime(string stateName, float fixedTransitionDuration, int layer, float fixedTimeOffset)
        {
            if (!ShouldRoute()) return;
            TrackEntry entry = SpineInternal.Play(Target, stateName, layer, loop: true);
            if (entry != null && fixedTransitionDuration > 0f)
                entry.MixDuration = fixedTransitionDuration;
            if (fixedTimeOffset > 0f)
                SpineInternal.SeekNormalized(Target, stateName, layer, fixedTimeOffset);
        }

        public void OnUpdateAnimClip(string stateName, int layer, float normalizedTimeOffset)
        {
            if (!ShouldRoute()) return;
            SpineInternal.SeekNormalized(Target, stateName, layer, normalizedTimeOffset);
        }

        public void OnSetSpeed(float speed)
        {
            if (Target == null) return;
            Target.timeScale = speed;
            if (Target.AnimationState != null)
                Target.AnimationState.TimeScale = speed;
        }

        public void OnSetFloat(string name, float value)
        {
            // 预留：Spine 没有 Animator Parameters 的等价概念；
            // 如需映射到 AnimationState.Tracks / 自定义上下文，可在此实现。
        }

        public void OnSetInt(string name, int value)
        {
            // 同上，预留钩子。
        }
    }

    /// <summary>
    /// 内部工具：统一 Spine 动画的查询/设置/跳帧逻辑，避免在扩展类与扩展方法中重复。
    /// </summary>
    internal static class SpineInternal
    {
        public static TrackEntry Play(SkeletonAnimation skel, string animName, int trackIndex, bool loop)
        {
            if (skel == null || string.IsNullOrEmpty(animName)) return null;
            AnimationState state = skel.AnimationState;
            if (state == null) return null;
            if (skel.Skeleton?.Data?.FindAnimation(animName) == null) return null;
            return state.SetAnimation(trackIndex, animName, loop);
        }

        /// <summary>按归一化时间（0~1）跳转并刷新骨架姿态。用于拖动时间轴、预览对齐等场景。</summary>
        public static void SeekNormalized(SkeletonAnimation skel, string animName, int trackIndex, float normalizedTime)
        {
            if (skel == null) return;
            AnimationState state = skel.AnimationState;
            if (state == null) return;

            TrackEntry entry = state.GetCurrent(trackIndex);
            bool needSet = entry == null
                           || (animName != null && entry.Animation != null && entry.Animation.Name != animName);
            if (needSet)
            {
                entry = Play(skel, animName, trackIndex, loop: false);
                if (entry == null) return;
            }

            Animation anim = entry.Animation;
            if (anim == null) return;

            float t = Mathf.Clamp01(normalizedTime);
            entry.TrackTime = anim.Duration * t;

            state.Apply(skel.Skeleton);
            skel.Skeleton.UpdateWorldTransform();
        }
    }

    /// <summary>
    /// 扩展方法：在 <see cref="ActionStateMachine"/> 之上提供 Spine 专用 API。
    /// 采用扩展方法的理由——Engine 与 Client 分属不同 asmdef，无法使用 partial class，
    /// 扩展方法既保留了"向状态机追加接口"的使用语义，又不破坏 Engine 零 Spine 耦合。
    /// </summary>
    public static class ActionStateMachineSpineExtensions
    {
        /// <summary>
        /// 绑定 Spine 骨架动画。绑定后 Engine 侧的 CrossFade / UpdateAnimClip / SetSpeed
        /// 等调用会在 Unity Animator 不可用时自动路由到该骨架。
        /// </summary>
        public static void BindSpineAnim(this ActionStateMachine self, SkeletonAnimation skel)
        {
            if (self == null) return;
            SpineAnimationExtension ext = self.AnimationExtension as SpineAnimationExtension;
            if (ext == null)
            {
                ext = new SpineAnimationExtension(self);
                self.AnimationExtension = ext;
            }
            ext.Target = skel;
        }

        /// <summary>解绑 Spine 骨架并摘除扩展点，回到纯 Unity Animator 行为。</summary>
        public static void UnbindSpineAnim(this ActionStateMachine self)
        {
            if (self == null) return;
            if (self.AnimationExtension is SpineAnimationExtension ext)
            {
                ext.Target = null;
                self.AnimationExtension = null;
            }
        }

        /// <summary>获取当前绑定的 Spine 骨架动画组件。未绑定时返回 null。</summary>
        public static SkeletonAnimation GetSpineAnim(this ActionStateMachine self)
            => (self?.AnimationExtension as SpineAnimationExtension)?.Target;

        /// <summary>是否存在有效的 Spine 骨架。与 Unity Animator 通道互为补充。</summary>
        public static bool SpineValid(this ActionStateMachine self)
            => GetSpineAnim(self) != null;

        /// <summary>在指定轨道直接播放一段 Spine 动画（无混合）。</summary>
        public static TrackEntry SpinePlay(this ActionStateMachine self, string animName, int trackIndex, bool loop = true)
            => SpineInternal.Play(GetSpineAnim(self), animName, trackIndex, loop);

        /// <summary>
        /// 带混合时长的 Spine 动画切换（对齐 Animator.CrossFadeInFixedTime 语义）。
        /// </summary>
        /// <param name="mixDuration">混合持续时间（秒）。</param>
        public static TrackEntry SpineCrossFade(this ActionStateMachine self, string animName, int trackIndex, float mixDuration, bool loop = true)
        {
            TrackEntry entry = SpinePlay(self, animName, trackIndex, loop);
            if (entry != null && mixDuration > 0f)
                entry.MixDuration = mixDuration;
            return entry;
        }

        /// <summary>
        /// 按归一化时间（0~1）跳至指定帧并刷新骨架姿态。编辑器预览与运行时按比例跳转共用。
        /// </summary>
        public static void SpineUpdateTrackTime(this ActionStateMachine self, string animName, int trackIndex, float normalizedTime)
            => SpineInternal.SeekNormalized(GetSpineAnim(self), animName, trackIndex, normalizedTime);

        /// <summary>设置 Spine 动画播放速率（对齐 Animator.speed 语义）。</summary>
        public static void SetSpineSpeed(this ActionStateMachine self, float speed)
        {
            SkeletonAnimation skel = GetSpineAnim(self);
            if (skel == null) return;
            skel.timeScale = speed;
            if (skel.AnimationState != null)
                skel.AnimationState.TimeScale = speed;
        }
    }
}
#endif
