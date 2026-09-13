namespace AsiActionEngine.RunTime
{
    /// <summary>
    /// 动画扩展挂载点：跨 asmdef 的可选动画通道（如 Client 端的 Spine 支持）。
    /// Engine 端在 Unity Animator 分支执行后会调用此接口对应方法，实现方自行判断
    /// 是否接管（通常是在 Unity Animator 失效时才路由到替代骨架）。
    /// 
    /// 设计要点：
    ///   - 接口定义不得引用任何第三方插件符号，保证 Engine 零耦合。
    ///   - 所有方法语义对齐 Unity Animator 的既有 API（名称 + 语义），便于映射。
    ///   - 未绑定实现时不影响 Unity Animator 原路径，调用处统一 null 判空即可。
    /// </summary>
    public interface IActionAnimationExtension
    {
        /// <summary>对应 Animator.CrossFade。</summary>
        void OnCrossFade(string stateName, float normalizedTransitionDuration, int layer, float normalizedTimeOffset);

        /// <summary>对应 Animator.CrossFadeInFixedTime。</summary>
        void OnCrossFadeInFixedTime(string stateName, float fixedTransitionDuration, int layer, float fixedTimeOffset);

        /// <summary>对应 Animator.Play（当前按归一化偏移跳转至指定 state）。</summary>
        void OnUpdateAnimClip(string stateName, int layer, float normalizedTimeOffset);

        /// <summary>对应 Animator.speed 赋值。</summary>
        void OnSetSpeed(float speed);

        /// <summary>对应 Animator.SetFloat。Spine 无等价概念时可留空。</summary>
        void OnSetFloat(string name, float value);

        /// <summary>对应 Animator.SetInteger。Spine 无等价概念时可留空。</summary>
        void OnSetInt(string name, int value);
    }
}
