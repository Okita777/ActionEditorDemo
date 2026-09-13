using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

#if UNITY_EDITOR
using AsiActionEngine.Editor;
#endif

namespace AsiTimeLine.RunTime
{
    /// <summary>
    /// <see cref="ActionEngine_GValueMonitor"/> 的一条监视配置。
    /// <para>
    /// <see cref="Value"/> 由 Inspector 通过动作编辑器同款 GV 下拉写入，其
    /// <see cref="GValue.mValueIndex"/> 存的是运行时 ValueID（同类型内序号），
    /// 可直接用于 <see cref="GValuePool"/> 读写，不需要再走 EditorID 转换。
    /// </para>
    /// </summary>
    [System.Serializable]
    public class GValueWatchEntry
    {
        [SerializeField] private EGValueType m_Type = EGValueType.GInt;

        // GValue 是抽象类，多态存储必须用 SerializeReference（与 GValue_SettingPar.GValueValue 同款）
        [SerializeReference] private GValue m_Value = CreateValue(EGValueType.GInt);

        [SerializeField] private string m_OverrideName = "";
        [SerializeField] private bool m_Writable = true;
        [SerializeField] private int m_PageIndex = 0;

        [System.NonSerialized] private bool m_Resolved;
        [System.NonSerialized] private string m_ResolvedName;
        [System.NonSerialized] private List<string> m_EnumNames;

#if UNITY_EDITOR
        private static readonly List<string> s_TmpNames = new List<string>();
        private static readonly List<EditorEngineGValuePart> s_TmpParts = new List<EditorEngineGValuePart>();
        private static bool s_ResolveDisabled;
#endif

        public EGValueType Type
        {
            get => m_Type;
            set => m_Type = value;
        }

        public GValue Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        /// <summary> 留空时使用编辑器 GV 定义里的名字。 </summary>
        public string OverrideName
        {
            get => m_OverrideName;
            set => m_OverrideName = value;
        }

        public bool Writable
        {
            get => m_Writable;
            set => m_Writable = value;
        }

        /// <summary> 所属分页在分页名列表中的下标。分页列表为空时全部条目视为同一页。 </summary>
        public int PageIndex
        {
            get => m_PageIndex;
            set => m_PageIndex = value;
        }

        /// <summary> 分页被删改后可能留下越界下标，统一归到第一页，避免条目在面板上凭空消失。 </summary>
        public int ResolvePageIndex(int _pageCount)
        {
            if (_pageCount <= 0) return 0;
            return m_PageIndex < 0 || m_PageIndex >= _pageCount ? 0 : m_PageIndex;
        }

        /// <summary> 去重用的身份：同一 (类型, 组, 序号) 在多个分页里重复配置时只显示一次。 </summary>
        public (EGValueType, ushort, ushort) Identity => (m_Type, GroupIndex, ValueIndex);

        public ushort GroupIndex => m_Value == null ? (ushort)0 : m_Value.mValueGroupIndex;

        public ushort ValueIndex => m_Value == null ? (ushort)0 : m_Value.mValueIndex;

        /// <summary> 面板上显示的名字。解析失败时退化为 <c>G{组}:{序号}</c>。 </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(m_OverrideName)) return m_OverrideName;
                if (!m_Resolved) Resolve();
                return m_ResolvedName;
            }
        }

        /// <summary> GEnum 的枚举项名列表，非 GEnum 或解析失败时为 null。 </summary>
        public List<string> EnumNames
        {
            get
            {
                if (!m_Resolved) Resolve();
                return m_EnumNames;
            }
        }

        public bool IsValid => m_Value != null && IsSupportedType(m_Type);

        /// <summary> 配置被改动后调用，下次读取时重新解析名字。 </summary>
        public void InvalidateCache()
        {
            m_Resolved = false;
            m_ResolvedName = null;
            m_EnumNames = null;
        }

        /// <summary> 只有这些类型在池上有明确的读写 API，面板才能安全展示与写入。 </summary>
        public static bool IsSupportedType(EGValueType _type)
        {
            switch (_type)
            {
                case EGValueType.GInt:
                case EGValueType.GFloat:
                case EGValueType.GBool:
                case EGValueType.GEnum:
                case EGValueType.GString:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary> 按类型创建 GV 引用实例。mType=true 表示指向运行时池而非序列化常量。 </summary>
        public static GValue CreateValue(EGValueType _type)
        {
            switch (_type)
            {
                case EGValueType.GFloat: return new GFloat() { mType = true };
                case EGValueType.GBool: return new GBool() { mType = true };
                case EGValueType.GEnum: return new GEnum() { mType = true };
                case EGValueType.GString: return new GString() { mType = true };
                default: return new GInt() { mType = true };
            }
        }

        private void Resolve()
        {
            m_Resolved = true;
            m_EnumNames = null;
            m_ResolvedName = $"G{GroupIndex}:{ValueIndex}";
            if (m_Value == null) return;

#if UNITY_EDITOR
            // 名字只存在于编辑器 GV 定义（EditorEngineGValuePart.Name），运行时二进制不带名字。
            if (s_ResolveDisabled) return;

            try
            {
                ResourcesWindow _window = ResourcesWindow.Instance;
                if (_window == null) return;

                // Init 幂等，但首次会加载全套编辑器 JSON 配置。解析是懒执行的（折叠态不取名字），
                // 所以这份成本最多在首次展开某个面板时出现一次
                _window.Init();

                if (!_window.TryGetGValuePartsOfType(GroupIndex, m_Type, s_TmpNames, s_TmpParts)) return;

                for (int i = 0; i < s_TmpParts.Count; i++)
                {
                    EditorEngineGValuePart _part = s_TmpParts[i];
                    if (_part.ValueID != ValueIndex) continue;

                    if (!string.IsNullOrEmpty(_part.Name)) m_ResolvedName = LeafName(_part.Name);
                    if (m_Type == EGValueType.GEnum) m_EnumNames = _part.names;
                    return;
                }
            }
            catch (System.Exception _e)
            {
                // 编辑器 GV 配置未就绪时不应影响 Play；后续条目不再重试，避免逐条刷屏
                s_ResolveDisabled = true;
                EngineDebug.LogWarning($"[GValueMonitor] 读取编辑器 GV 定义失败，名字退化为 ID 显示：{_e.Message}");
            }
#endif
        }

        /// <summary> GV 名可能带 '/' 分组路径，面板只显示叶子段。 </summary>
        private static string LeafName(string _name)
        {
            int _slash = _name.LastIndexOf('/');
            return _slash < 0 ? _name : _name.Substring(_slash + 1);
        }
    }
}
