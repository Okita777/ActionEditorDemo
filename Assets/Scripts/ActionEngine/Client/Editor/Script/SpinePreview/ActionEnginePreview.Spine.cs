// =============================================================================
// Editor 下的 Spine 动画预览工具：
// 时间轴拖动时，接管 EventUpdate 调用 AsiActionEditorFuntion.UpdateAnim 的入口，
// 在无 Unity Animator 的 Spine 预览模型上实现"按归一化时间定帧"的预览。
//
// 约束：
//   - 所有 Spine 相关代码仅位于 Assets/Scripts/ActionEngine/Client 下；
//   - 统一用 ACTION_ENGINE_SPINE 宏包裹；
//   - 不依赖 Engine 侧的 Spine 字段，仅通过 ResourcesWindow.PreviewRoot 查组件。
// =============================================================================
#if ACTION_ENGINE_SPINE
using AsiActionEngine.Editor;
using Spine;
using Spine.Unity;
using UnityEngine;
using AnimationState = Spine.AnimationState;

namespace AsiTimeLine.Editor
{
    /// <summary>
    /// Spine 骨架的编辑器预览辅助类。
    /// </summary>
    internal static class ActionEnginePreviewSpine
    {
        /// <summary>
        /// 尝试按归一化时间（0~1）刷新预览模型的 Spine 姿态。
        /// </summary>
        /// <returns>
        /// 处理成功返回 true；若当前预览模型没有 <see cref="SkeletonAnimation"/>，
        /// 或动画不存在，则返回 false，交还给上层走 Unity Animator 回落路径。
        /// </returns>
        public static bool TryPreview(string animName, int trackIndex, float normalizedTime)
        {
            if (string.IsNullOrEmpty(animName)) return false;

            GameObject _root = ResourcesWindow.Instance?.PreviewRoot;
            if (_root == null) return false;

            SkeletonAnimation _skel = _root.GetComponentInChildren<SkeletonAnimation>(true);
            if (_skel == null) return false;

            AnimationState _state = _skel.AnimationState;
            if (_state == null) return false;

            SkeletonData _data = _skel.Skeleton?.Data;
            if (_data == null || _data.FindAnimation(animName) == null) return false;

            TrackEntry _entry = _state.GetCurrent(trackIndex);
            bool _needSet = _entry == null
                            || _entry.Animation == null
                            || _entry.Animation.Name != animName;
            if (_needSet)
            {
                _entry = _state.SetAnimation(trackIndex, animName, false);
            }
            if (_entry == null || _entry.Animation == null) return false;

            float _t = Mathf.Clamp01(normalizedTime);
            _entry.TrackTime = _entry.Animation.Duration * _t;
            _entry.TimeScale = 0f;

            _state.Apply(_skel.Skeleton);
            _skel.Skeleton.UpdateWorldTransform();
            _skel.LateUpdate();
            return true;
        }
    }
}
#endif
