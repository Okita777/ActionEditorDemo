// =============================================================================
// ActionEngineEvent 的 Spine 扩展（仅 Editor）：
// 将 AsiActionEditorFuntion.UpdateAnim 路由到 Spine 预览工具。
// 覆写返回 true 时，EventUpdate 不会再走 Unity Animator 回落。
//
// 约束：Spine 相关代码只出现在 Client，且统一被 ACTION_ENGINE_SPINE 宏包裹。
// =============================================================================
#if ACTION_ENGINE_SPINE
namespace AsiTimeLine.Editor
{
    public partial class ActionEngineEvent
    {
        public override bool UpdateAnim(string animName, int layer, float offsetTime)
            => ActionEnginePreviewSpine.TryPreview(animName, layer, offsetTime);
    }
}
#endif
