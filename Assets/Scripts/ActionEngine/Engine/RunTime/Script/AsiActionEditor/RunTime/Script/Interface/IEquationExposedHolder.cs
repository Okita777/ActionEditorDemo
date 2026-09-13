using System.Collections.Generic;

namespace AsiActionEngine.RunTime
{
    /// <summary>
    /// "GEquation 调用者侧持久化暴露引脚连线"的唯一入口。
    /// 由 GraphEvent_GValue_GEquation* 调用者实现：
    /// - <see cref="GEquationVal"/>：指向被引用的公式（组 id + 公式 index）。
    /// - <see cref="ExposedBindings"/>：持久化"用户在蓝图编辑器里连到公式暴露参数引脚的 BluePrint_Value 节点"。
    ///
    /// 职责分工：
    /// - 编辑器端：<see cref="AsiActionEngine.Editor.DrawGraphEditorAttribute"/> 根据被引用公式里
    ///   <c>drawToInspector==true</c> 的局部变量自动建立 / 回收 bindings（见 Reconcile 逻辑）。
    /// - 运行时：<see cref="EquationExposedParamRuntime.Apply"/> 用 bindings 临时覆写公式内部 LocalXxxParams，
    ///   求值结束后由 <see cref="EquationExposedParamRuntime.Restore"/> 还原，使用 [NonSerialized] SavedBuffer 避免 GC。
    /// </summary>
    public interface IEquationExposedHolder
    {
        GEquation GEquationVal { get; }
        List<EquationExposedParamBinding> ExposedBindings { get; }
    }
}
