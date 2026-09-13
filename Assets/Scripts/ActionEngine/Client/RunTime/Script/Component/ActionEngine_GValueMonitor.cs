using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    /// <summary>
    /// Game 视图 GValue 监视器：在每个单位头顶绘制一块可独立收纳的 GV 列表，支持手动写入。
    /// 场景里只需挂一个，它自己遍历 <see cref="ActionEngineManager_Unit.Units"/>。
    /// <para>
    /// 仅在编辑器 Play Mode 生效：GV 的名字只存在于编辑器 GV 定义里，运行时二进制不带名字。
    /// 类本身在 Player 下依然存在（避免场景残留 Missing Script），但不含任何 Unity 回调，零开销。
    /// </para>
    /// <para>
    /// 注意 <see cref="GValuePool"/> 的 getter 对未访问过的槽位会懒加载定义默认值，
    /// 也就是说本组件的读取会提前把槽位实例化出来。取到的是配置默认值，不改变业务语义。
    /// </para>
    /// </summary>
    public class ActionEngine_GValueMonitor : MonoBehaviour
    {
        [Header("监视的 GValue")]
        [SerializeField] private List<GValueWatchEntry> m_Watches = new List<GValueWatchEntry>();

        [Tooltip("分页名。为空时所有条目视为同一页，面板不显示页签栏")]
        [SerializeField] private List<string> m_PageNames = new List<string>();

        [Tooltip("要显示的分页下标。留空视为全部显示")]
        [SerializeField] private List<int> m_SelectedPages = new List<int>();

        [Header("单位筛选")]
        [Tooltip("只显示当前操作对象（Player）")]
        [SerializeField] private bool m_OnlyPlayer = false;

        [Tooltip("是否显示技能实体。Units 里同时含角色与技能实体，默认过滤掉技能实体")]
        [SerializeField] private bool m_IncludeSkillEntity = false;

        [Tooltip("单位名关键字过滤，留空为不限")]
        [SerializeField] private string m_NameFilter = "";

        [Tooltip("与相机的最大距离，0 为不限")]
        [SerializeField] private float m_MaxDistance = 0f;

        [Header("显示")]
        [Tooltip("无 HUD 挂点时，面板相对单位根节点的高度")]
        [SerializeField] private float m_DrawHeight = 2f;

        [SerializeField] private float m_PanelWidth = 300f;
        [SerializeField] private float m_RowHeight = 18f;

        [Tooltip("单位首次出现时是否处于收纳状态")]
        [SerializeField] private bool m_DefaultFolded = true;

        [Header("开关")]
        [SerializeField] private bool m_Enable = true;

        [Tooltip("切换总开关的按键")]
        [SerializeField] private KeyCode m_ToggleKey = KeyCode.F3;

        [Tooltip("左上角总控条的位置")]
        [SerializeField] private Vector2 m_BarPos = new Vector2(10f, 10f);

        public List<GValueWatchEntry> Watches => m_Watches;

        public List<string> PageNames => m_PageNames;

        public List<int> SelectedPages => m_SelectedPages;

#if UNITY_EDITOR

        /// <summary> 单个单位的面板状态。每单位独立，否则多单位同时展开时输入会串台。 </summary>
        private class UnitPanelState
        {
            public bool Folded;

            // 按 entry 引用索引而非行号：切换分页会改变行顺序，用行号会让输入缓冲错位到别的 GV 上
            public readonly Dictionary<GValueWatchEntry, RowState> Rows =
                new Dictionary<GValueWatchEntry, RowState>();

            public RowState GetRow(GValueWatchEntry _entry)
            {
                if (Rows.TryGetValue(_entry, out RowState _row)) return _row;

                _row = new RowState();
                Rows.Add(_entry, _row);
                return _row;
            }
        }

        /// <summary>
        /// 一行的输入缓冲。<see cref="LastSeen"/> 用于区分「用户正在编辑」与「外部改了值」：
        /// 只有当前值与上次看到的不同时才冲掉缓冲，用户输入过程中不会被逐帧重置。
        /// </summary>
        private class RowState
        {
            public string LastSeen;
            public string Buffer;
        }

        private static readonly Color DirtyColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color SelectedPageColor = new Color(0.45f, 1f, 0.45f);

        private readonly GUIContent m_TmpContent = new GUIContent();

        private readonly Dictionary<ActionEngine_Unit, UnitPanelState> m_States =
            new Dictionary<ActionEngine_Unit, UnitPanelState>();
        private readonly List<ActionEngine_Unit> m_DeadKeys = new List<ActionEngine_Unit>();

        // 每帧按选中分页重建一次的可见条目，所有单位面板共用
        private readonly List<GValueWatchEntry> m_VisibleWatches = new List<GValueWatchEntry>();
        private readonly HashSet<(EGValueType, ushort, ushort)> m_SeenIdentities =
            new HashSet<(EGValueType, ushort, ushort)>();

        private GUIStyle m_LabelStyle;
        private GUIStyle m_ValueStyle;
        private GUIStyle m_TitleStyle;
        private int m_VisibleCount;

        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            HandleToggleKey();
            EnsureStyles();

            if (Event.current.type == EventType.Layout) CleanupStates();
            RebuildVisibleWatches();

            DrawBar();
            if (!m_Enable) return;

            DrawPageBar();
            DrawUnitPanels();
        }

        /// <summary>
        /// 按选中分页筛出要画的条目，并按 (类型, 组, 序号) 去重。
        /// 未选中任何分页时视为全选，这样新加的分页默认就是可见的。
        /// <para>
        /// 每次 OnGUI 都重建而不是只在 Layout 事件重建：一帧内页签按钮的点击会立刻改变选中集合，
        /// 绑到单一事件类型会让同帧后续的绘制用到过期列表。
        /// </para>
        /// </summary>
        private void RebuildVisibleWatches()
        {
            m_VisibleWatches.Clear();
            m_SeenIdentities.Clear();

            int _pageCount = m_PageNames.Count;
            if (_pageCount == 0)
            {
                CollectPage(-1, 0);
                return;
            }

            // 外层按分页顺序、内层按条目顺序，面板的上下顺序就等于 Inspector 里看到的顺序
            bool _all = m_SelectedPages.Count == 0;
            for (int _page = 0; _page < _pageCount; _page++)
            {
                if (!_all && !m_SelectedPages.Contains(_page)) continue;
                CollectPage(_page, _pageCount);
            }
        }

        /// <param name="_page"> -1 表示没有分页定义时收下全部条目。 </param>
        private void CollectPage(int _page, int _pageCount)
        {
            for (int i = 0; i < m_Watches.Count; i++)
            {
                GValueWatchEntry _entry = m_Watches[i];
                if (_entry == null || !_entry.IsValid) continue;
                if (_page >= 0 && _entry.ResolvePageIndex(_pageCount) != _page) continue;

                // 跨页重复的 GV 保留先出现的那条，与「从上到下」一致
                if (!m_SeenIdentities.Add(_entry.Identity)) continue;

                m_VisibleWatches.Add(_entry);
            }
        }

        private void HandleToggleKey()
        {
            // 用 Event.current 而非 Input.GetKeyDown：OnGUI 一帧会被调用多次，GetKeyDown 会重复触发
            Event _e = Event.current;
            if (_e.type != EventType.KeyDown || _e.keyCode != m_ToggleKey) return;

            m_Enable = !m_Enable;
            _e.Use();
        }

        private void EnsureStyles()
        {
            if (m_LabelStyle != null) return;

            m_LabelStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(2, 2, 0, 0),
                clipping = TextClipping.Clip,
            };
            m_ValueStyle = new GUIStyle(m_LabelStyle);
            m_TitleStyle = new GUIStyle(m_LabelStyle) { fontStyle = FontStyle.Bold };
        }

        private void DrawBar()
        {
            string _state = m_Enable ? "<color=#7CFC00>开</color>" : "<color=#ff5555>关</color>";
            Rect _rect = new Rect(m_BarPos.x, m_BarPos.y, 150f, m_RowHeight);
            GUI.Label(_rect, $"GV监视[{m_ToggleKey}] {_state}  单位:{m_VisibleCount}", m_TitleStyle);

            if (!m_Enable) return;

            _rect.x += _rect.width;
            _rect.width = 56f;
            if (GUI.Button(_rect, "全展开")) SetAllFolded(false);

            _rect.x += _rect.width + 2f;
            if (GUI.Button(_rect, "全收起")) SetAllFolded(true);
        }

        private void DrawPageBar()
        {
            if (m_PageNames.Count == 0) return;

            float _x = m_BarPos.x;
            float _y = m_BarPos.y + m_RowHeight + 2f;

            if (DrawPageButton(ref _x, _y, "全部", m_SelectedPages.Count == 0))
                m_SelectedPages.Clear();

            for (int i = 0; i < m_PageNames.Count; i++)
            {
                string _name = string.IsNullOrEmpty(m_PageNames[i]) ? $"页{i}" : m_PageNames[i];
                if (!DrawPageButton(ref _x, _y, _name, m_SelectedPages.Contains(i))) continue;

                if (!m_SelectedPages.Remove(i)) m_SelectedPages.Add(i);
            }
        }

        private bool DrawPageButton(ref float _x, float _y, string _label, bool _selected)
        {
            m_TmpContent.text = _label;
            float _width = GUI.skin.button.CalcSize(m_TmpContent).x + 8f;
            Rect _rect = new Rect(_x, _y, _width, m_RowHeight);
            _x += _width + 2f;

            Color _old = GUI.backgroundColor;
            if (_selected) GUI.backgroundColor = SelectedPageColor;
            bool _clicked = GUI.Button(_rect, m_TmpContent);
            GUI.backgroundColor = _old;

            return _clicked;
        }

        private void SetAllFolded(bool _folded)
        {
            m_DefaultFolded = _folded;
            foreach (KeyValuePair<ActionEngine_Unit, UnitPanelState> _pair in m_States)
                _pair.Value.Folded = _folded;
        }

        private void CleanupStates()
        {
            if (m_States.Count == 0) return;

            m_DeadKeys.Clear();
            foreach (KeyValuePair<ActionEngine_Unit, UnitPanelState> _pair in m_States)
                if (_pair.Key == null) m_DeadKeys.Add(_pair.Key);

            for (int i = 0; i < m_DeadKeys.Count; i++) m_States.Remove(m_DeadKeys[i]);
            m_DeadKeys.Clear();
        }

        private void DrawUnitPanels()
        {
            Camera _cam = ActionEngineManager_Input.Instance.PlayerCam;
            if (_cam == null) return;

            List<ActionEngine_Unit> _units = ActionEngineManager_Unit.Instance.Units;
            if (_units == null) return;

            Vector3 _camPos = _cam.transform.position;
            int _visible = 0;

            for (int i = 0; i < _units.Count; i++)
            {
                ActionEngine_Unit _unit = _units[i];
                if (_unit == null) continue;
                if (!PassFilter(_unit, _camPos)) continue;

                _visible++;

                Vector3 _screen = _cam.WorldToScreenPoint(GetAnchorPos(_unit));
                if (_screen.z <= 0f) continue;

                float _x = _screen.x;
                float _y = Screen.height - _screen.y;
                if (_x < -m_PanelWidth || _x > Screen.width) continue;
                if (_y < 0f || _y > Screen.height) continue;

                DrawUnitPanel(_unit, _x, _y);
            }

            m_VisibleCount = _visible;
        }

        private bool PassFilter(ActionEngine_Unit _unit, Vector3 _camPos)
        {
            ActionStateMachine _machine = _unit.ActionStateMachine;
            if (_machine == null || _machine.GValuePool == null) return false;

            if (!m_IncludeSkillEntity && _unit is ActionEngine_Skill) return false;
            if (m_OnlyPlayer && !ActionEngineManager_Input.Instance.IsPlayer(_unit)) return false;

            if (!string.IsNullOrEmpty(m_NameFilter) &&
                _unit.gameObject.name.IndexOf(m_NameFilter, System.StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            if (m_MaxDistance > 0f)
            {
                Vector3 _delta = _unit.transform.position - _camPos;
                if (_delta.sqrMagnitude > m_MaxDistance * m_MaxDistance) return false;
            }

            return true;
        }

        /// <summary>
        /// 面板锚点。优先用 HUD 挂点（与 UICHudHpEnemy 一致），
        /// 缺挂点时才回退到根节点加高度偏移——所以不能直接用 TryGetCharacterLimb，它会静默回退。
        /// </summary>
        private Vector3 GetAnchorPos(ActionEngine_Unit _unit)
        {
            ActionStateMachine _machine = _unit.ActionStateMachine;
            if (_machine != null &&
                _machine.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig)) &&
                _config.HelpPointDic != null &&
                _config.HelpPointDic.TryGetValue(ECharacteLimbType.HUD, out Transform _hud) &&
                _hud != null)
            {
                return _hud.position;
            }

            return _unit.transform.position + Vector3.up * m_DrawHeight;
        }

        private void DrawUnitPanel(ActionEngine_Unit _unit, float _x, float _y)
        {
            UnitPanelState _state = GetState(_unit);

            if (_state.Folded)
            {
                Rect _btn = new Rect(_x - 24f, _y, 48f, m_RowHeight);
                if (GUI.Button(_btn, $"GV·{m_VisibleWatches.Count}")) _state.Folded = false;
                return;
            }

            float _left = _x - m_PanelWidth * 0.5f;
            int _rows = Mathf.Max(m_VisibleWatches.Count, 1) + 1;

            GUI.Box(new Rect(_left, _y, m_PanelWidth, m_RowHeight * _rows + 6f), GUIContent.none);

            Rect _line = new Rect(_left + 3f, _y + 3f, m_PanelWidth - 6f, m_RowHeight);

            if (GUI.Button(new Rect(_line.x, _line.y, 20f, m_RowHeight), "-")) _state.Folded = true;
            GUI.Label(new Rect(_line.x + 22f, _line.y, _line.width - 22f, m_RowHeight),
                UnitTitle(_unit), m_TitleStyle);

            if (m_VisibleWatches.Count == 0)
            {
                _line.y += m_RowHeight;
                GUI.Label(_line, "<color=#ff9955>当前分页没有可显示的 GV</color>", m_LabelStyle);
                return;
            }

            for (int i = 0; i < m_VisibleWatches.Count; i++)
            {
                GValueWatchEntry _entry = m_VisibleWatches[i];
                _line.y += m_RowHeight;
                DrawWatchRow(_unit, _entry, _state.GetRow(_entry), _line);
            }
        }

        private static string UnitTitle(ActionEngine_Unit _unit)
        {
            string _name = _unit.gameObject.name;
            if (_unit is ActionEngine_Skill) return $"<color=#66ccff>[技能]{_name}</color>";
            if (ActionEngineManager_Input.Instance.IsPlayer(_unit)) return $"<color=#ccff00>[玩家]{_name}</color>";
            return $"<color=#dddddd>{_name}</color>";
        }

        private UnitPanelState GetState(ActionEngine_Unit _unit)
        {
            if (!m_States.TryGetValue(_unit, out UnitPanelState _state))
            {
                _state = new UnitPanelState { Folded = m_DefaultFolded };
                m_States.Add(_unit, _state);
            }

            return _state;
        }

        private void DrawWatchRow(ActionEngine_Unit _unit, GValueWatchEntry _entry, RowState _row, Rect _line)
        {
            float _nameWidth = _line.width * 0.42f;
            Rect _nameRect = new Rect(_line.x, _line.y, _nameWidth, _line.height);
            Rect _valRect = new Rect(_line.x + _nameWidth, _line.y, _line.width - _nameWidth, _line.height);

            GUI.Label(_nameRect, _entry.DisplayName, m_LabelStyle);

            GValuePool _pool = _unit.ActionStateMachine.GValuePool;
            ushort _group = _entry.GroupIndex;
            ushort _id = _entry.ValueIndex;

            switch (_entry.Type)
            {
                case EGValueType.GBool:
                    DrawBoolRow(_unit, _entry, _valRect, _pool, _group, _id);
                    break;
                case EGValueType.GEnum:
                    DrawEnumRow(_unit, _entry, _valRect, _pool, _group, _id);
                    break;
                default:
                    DrawTextRow(_unit, _entry, _row, _valRect, _pool, _group, _id);
                    break;
            }
        }

        private void DrawBoolRow(ActionEngine_Unit _unit, GValueWatchEntry _entry, Rect _rect,
            GValuePool _pool, ushort _group, ushort _id)
        {
            bool _cur = _pool.GetBool(_group, _id);

            if (!_entry.Writable)
            {
                GUI.Label(_rect, _cur ? "true" : "false", m_ValueStyle);
                return;
            }

            bool _next = GUI.Toggle(_rect, _cur, _cur ? " true" : " false");
            if (_next == _cur) return;

            _pool.SetBool(_group, _id, _next);
            _unit.ActionStateMachine.SendChangeMessage_GBool(_group, _id, _cur, _next);
        }

        private void DrawEnumRow(ActionEngine_Unit _unit, GValueWatchEntry _entry, Rect _rect,
            GValuePool _pool, ushort _group, ushort _id)
        {
            byte _cur = _pool.GetEnum(_group, _id);
            List<string> _names = _entry.EnumNames;
            string _disp = _names != null && _cur < _names.Count ? _names[_cur] : _cur.ToString();

            if (!_entry.Writable)
            {
                GUI.Label(_rect, $"{_cur}:{_disp}", m_ValueStyle);
                return;
            }

            if (GUI.Button(new Rect(_rect.x, _rect.y, 18f, _rect.height), "<") && _cur > 0)
                WriteEnum(_unit, _pool, _group, _id, _cur, (byte)(_cur - 1));

            GUI.Label(new Rect(_rect.x + 20f, _rect.y, _rect.width - 42f, _rect.height),
                $"{_cur}:{_disp}", m_ValueStyle);

            if (GUI.Button(new Rect(_rect.xMax - 20f, _rect.y, 18f, _rect.height), ">") && _cur < byte.MaxValue)
                WriteEnum(_unit, _pool, _group, _id, _cur, (byte)(_cur + 1));
        }

        private static void WriteEnum(ActionEngine_Unit _unit, GValuePool _pool,
            ushort _group, ushort _id, byte _old, byte _new)
        {
            _pool.SetEnum(_group, _id, _new);
            _unit.ActionStateMachine.SendChangeMessage_GEnum(_group, _id, _old, _new);
        }

        private void DrawTextRow(ActionEngine_Unit _unit, GValueWatchEntry _entry, RowState _row, Rect _rect,
            GValuePool _pool, ushort _group, ushort _id)
        {
            string _cur = ReadAsString(_entry.Type, _pool, _group, _id);

            // 外部改了值才冲掉输入缓冲，否则用户的输入会被逐帧重置
            if (_row.LastSeen != _cur)
            {
                _row.LastSeen = _cur;
                _row.Buffer = _cur;
            }

            if (!_entry.Writable)
            {
                GUI.Label(_rect, _cur, m_ValueStyle);
                return;
            }

            Rect _field = new Rect(_rect.x, _rect.y, _rect.width - 24f, _rect.height);
            Rect _commit = new Rect(_rect.xMax - 22f, _rect.y, 22f, _rect.height);

            Color _old = GUI.color;
            if (_row.Buffer != _cur) GUI.color = DirtyColor;
            _row.Buffer = GUI.TextField(_field, _row.Buffer ?? "");
            GUI.color = _old;

            if (GUI.Button(_commit, "√")) Commit(_unit, _entry, _row, _pool, _group, _id);
        }

        private static string ReadAsString(EGValueType _type, GValuePool _pool, ushort _group, ushort _id)
        {
            switch (_type)
            {
                case EGValueType.GFloat: return _pool.GetFloat(_group, _id).ToString("0.####");
                case EGValueType.GString: return _pool.GetString(_group, _id);
                default: return _pool.GetInt(_group, _id).ToString();
            }
        }

        /// <summary>
        /// 写入走「池 + SendChangeMessage_*」，与 GInt.SetValue 的 mType=true 分支语义一致，
        /// 保证 GV 变更监听轨道与网络同步都会被触发。
        /// </summary>
        private static void Commit(ActionEngine_Unit _unit, GValueWatchEntry _entry, RowState _row,
            GValuePool _pool, ushort _group, ushort _id)
        {
            switch (_entry.Type)
            {
                case EGValueType.GFloat:
                    if (!float.TryParse(_row.Buffer, out float _fv))
                    {
                        WarnParseFailed(_entry, _row);
                        return;
                    }
                    float _fOld = _pool.GetFloat(_group, _id);
                    _pool.SetFloat(_group, _id, _fv);
                    _unit.ActionStateMachine.SendChangeMessage_GFloat(_group, _id, _fOld, _fv);
                    break;

                case EGValueType.GString:
                    // GString 池写入本身没有变更通知，与 GString.SetValue 保持一致
                    _pool.SetString(_group, _id, _row.Buffer ?? "");
                    break;

                default:
                    if (!int.TryParse(_row.Buffer, out int _iv))
                    {
                        WarnParseFailed(_entry, _row);
                        return;
                    }
                    int _iOld = _pool.GetInt(_group, _id);
                    _pool.SetInt(_group, _id, _iv);
                    _unit.ActionStateMachine.SendChangeMessage_GInt(_group, _id, _iOld, _iv);
                    break;
            }
        }

        private static void WarnParseFailed(GValueWatchEntry _entry, RowState _row)
        {
            EngineDebug.LogWarning(
                $"[GValueMonitor] [{_entry.DisplayName}] 输入无法解析为 {_entry.Type}：[{_row.Buffer}]");
            _row.LastSeen = null;
        }

#endif
    }
}
