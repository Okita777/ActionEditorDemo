using System.Collections.Generic;
using AsiActionEngine.RunTime.Graph;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    /// <summary>
    /// 把被引用公式中"勾选了绘制到 Inspector"的局部变量，绑定到调用者（GraphEvent_GValue_GEquation*）上作为输入引脚。
    ///
    /// 职责与存在理由（不能去掉此结构）：
    /// - <c>pin</c>：用户在蓝图编辑器里拖进来的 BluePrint_Value 节点，必须 <c>[SerializeReference]</c> 持久化，
    ///   否则保存后连线永久丢失；这是本结构无法被消除的根本原因。
    /// - <c>paramName</c> + <c>paramType</c>：用于公式后续被改名 / 改类型 / 重排时做稳健迁移。
    ///   没有它们就只能按位置索引匹配，公式编辑一次即可能造成整排 pin "串位"。
    ///
    /// 运行时入口：<see cref="EquationExposedParamRuntime.Apply"/>
    /// </summary>
    [System.Serializable]
    public class EquationExposedParamBinding
    {
        /// <summary>与公式内部 <c>NoValue_*.NodeEdiData.localParams[i].paramName</c> 对齐。</summary>
        public string paramName;
        /// <summary>与公式内部 <c>NoValue_*.NodeEdiData.localParams[i].paramType</c> 对齐。</summary>
        public EBluePrintLocalParamType paramType;
        /// <summary>
        /// 写入该参数的蓝图节点；<c>null</c> 表示未连线（运行时跳过覆写，沿用公式自身默认值）。
        /// </summary>
        [SerializeReference] public BluePrint_Value pin;

        public EquationExposedParamBinding Clone()
        {
            EquationExposedParamBinding clone = new EquationExposedParamBinding
            {
                paramName = paramName,
                paramType = paramType,
                pin = pin != null ? pin.Clone() : null,
            };
            return clone;
        }

        /// <summary>
        /// 深拷贝整份 bindings 列表（含每个 binding 的 pin.Clone()），供各 GraphEvent_GValue_GEquation*.Clone 复用。
        /// </summary>
        public static List<EquationExposedParamBinding> CloneList(List<EquationExposedParamBinding> src)
        {
            if (src == null) return new List<EquationExposedParamBinding>();
            List<EquationExposedParamBinding> dst = new List<EquationExposedParamBinding>(src.Count);
            for (int i = 0; i < src.Count; i++)
            {
                if (src[i] == null) continue;
                dst.Add(src[i].Clone());
            }
            return dst;
        }
    }
}
