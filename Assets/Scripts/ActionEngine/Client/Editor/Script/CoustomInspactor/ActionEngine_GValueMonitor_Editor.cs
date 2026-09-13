using System.Collections.Generic;
using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    [CustomEditor(typeof(ActionEngine_GValueMonitor))]
    public class ActionEngine_GValueMonitor_Editor : UnityEditor.Editor
    {
        private static readonly EGValueType[] SupportedTypes =
        {
            EGValueType.GInt,
            EGValueType.GFloat,
            EGValueType.GBool,
            EGValueType.GEnum,
            EGValueType.GString,
        };

        private static readonly string[] SupportedTypeNames =
        {
            "GInt", "GFloat", "GBool", "GEnum", "GString",
        };

        private const float RowHeight = 18f;
        private const float GvRowHeight = 20f;
        private const float HelpBoxHeight = 32f;
        private const float Gap = 2f;
        private const float ElementPad = 4f;

        private const float ElementHeightNormal =
            ElementPad + RowHeight + Gap + GvRowHeight + Gap + RowHeight;
        private const float ElementHeightBadGroup =
            ElementPad + RowHeight + Gap + HelpBoxHeight + Gap + RowHeight + Gap + RowHeight;

        // GV 下拉用的是 AdvancedDropdown，选中回调异步发生，ChangeCheck 抓不到。
        // 所以额外维护一份 (组, 序号, 类型, 分页) 快照，靠比对来触发 SetDirty 与名字缓存失效。
        private readonly List<GValueKey> m_LastKeys = new List<GValueKey>();

        // ReorderableList 必须跨帧复用，否则拖拽状态每帧被丢弃。
        // 每页一份缓冲列表作为它的数据源，重排后再回写到组件的扁平条目列表。
        private readonly List<List<GValueWatchEntry>> m_PageBuffers = new List<List<GValueWatchEntry>>();
        private readonly List<ReorderableList> m_EntryLists = new List<ReorderableList>();
        private readonly List<bool> m_PageFolds = new List<bool>();
        private ReorderableList m_PageList;
        private int m_BuiltPageCount = -1;
        private string[] m_PageLabels = new string[0];

        private struct GValueKey
        {
            public ushort Group;
            public ushort Index;
            public EGValueType Type;
            public int Page;

            public bool Same(GValueKey _other) =>
                Group == _other.Group && Index == _other.Index &&
                Type == _other.Type && Page == _other.Page;
        }

        private ActionEngine_GValueMonitor Main => target as ActionEngine_GValueMonitor;

        public override void OnInspectorGUI()
        {
            ActionEngine_GValueMonitor _main = Main;
            if (_main == null) return;

            // 先把标量字段回写完，再直接操作 target 的列表，
            // 否则 ApplyModifiedProperties 会用 SerializedObject 的旧副本覆盖列表改动。
            serializedObject.Update();
            DrawScalarFields();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6f);

            RebuildPageLabels(_main.PageNames);
            EnsureLists(_main);
            SyncPageBuffers(_main);

            DrawPageList(_main);
            DrawPageBodies();
            SyncKeySnapshot(_main);

            if (Application.isPlaying) Repaint();
        }

        private void DrawScalarFields()
        {
            SerializedProperty _it = serializedObject.GetIterator();
            bool _enterChildren = true;
            while (_it.NextVisible(_enterChildren))
            {
                _enterChildren = false;
                if (_it.propertyPath == "m_Script") continue;
                if (_it.propertyPath == "m_Watches") continue;
                if (_it.propertyPath == "m_PageNames") continue;
                if (_it.propertyPath == "m_SelectedPages") continue;
                EditorGUILayout.PropertyField(_it, true);
            }
        }

        #region 列表构建

        /// <summary> 页数变化时整体重建列表对象；页内条目增删只需要同步缓冲内容。 </summary>
        private void EnsureLists(ActionEngine_GValueMonitor _main)
        {
            int _pageCount = _main.PageNames.Count;

            // 没有分页时用一个隐式页容纳全部条目
            int _bodyCount = Mathf.Max(_pageCount, 1);
            if (m_BuiltPageCount == _pageCount && m_PageList != null && m_EntryLists.Count == _bodyCount) return;

            m_BuiltPageCount = _pageCount;
            m_PageList = CreatePageList(_main);

            m_EntryLists.Clear();
            m_PageBuffers.Clear();
            for (int i = 0; i < _bodyCount; i++)
            {
                m_PageBuffers.Add(new List<GValueWatchEntry>());
                m_EntryLists.Add(CreateEntryList(i, _pageCount == 0));
            }

            while (m_PageFolds.Count < _bodyCount) m_PageFolds.Add(true);
            while (m_PageFolds.Count > _bodyCount) m_PageFolds.RemoveAt(m_PageFolds.Count - 1);
        }

        private void SyncPageBuffers(ActionEngine_GValueMonitor _main)
        {
            List<GValueWatchEntry> _watches = _main.Watches;
            bool _implicitPage = _main.PageNames.Count == 0;

            for (int p = 0; p < m_PageBuffers.Count; p++) m_PageBuffers[p].Clear();

            for (int i = 0; i < _watches.Count; i++)
            {
                int _page = _implicitPage ? 0 : PageOf(_main, _watches[i]);
                if (_page < 0 || _page >= m_PageBuffers.Count) continue;
                m_PageBuffers[_page].Add(_watches[i]);
            }
        }

        private ReorderableList CreatePageList(ActionEngine_GValueMonitor _main)
        {
            ReorderableList _list = new ReorderableList(_main.PageNames, typeof(string), true, true, true, true);

            _list.drawHeaderCallback = _rect =>
                EditorGUI.LabelField(_rect, "分页顺序（拖拽调整，面板从上到下依此排列）");

            _list.elementHeight = RowHeight + 4f;
            _list.drawElementCallback = (_rect, _index, _active, _focus) => DrawPageElement(_rect, _index);
            _list.onAddCallback = _l => AddPage();
            _list.onRemoveCallback = _l => RemovePage(_l.index);
            _list.onReorderCallbackWithDetails = (_l, _old, _new) => RemapPageOrder(_old, _new);

            return _list;
        }

        private ReorderableList CreateEntryList(int _pageIndex, bool _implicitPage)
        {
            ReorderableList _list = new ReorderableList(
                m_PageBuffers[_pageIndex], typeof(GValueWatchEntry), true, true, true, true);

            _list.drawHeaderCallback = _rect => EditorGUI.LabelField(_rect, "GV 条目（拖拽调整顺序）");
            _list.elementHeightCallback = _index => ElementHeight(_pageIndex, _index);
            _list.drawElementCallback = (_rect, _index, _active, _focus) => DrawEntryElement(_pageIndex, _rect, _index);
            _list.onAddCallback = _l => AddEntry(_implicitPage ? 0 : _pageIndex);
            _list.onRemoveCallback = _l => RemoveEntry(_pageIndex, _l.index);
            _list.onReorderCallback = _l => ApplyEntryOrder(_pageIndex);

            return _list;
        }

        #endregion

        #region 分页

        private void DrawPageList(ActionEngine_GValueMonitor _main)
        {
            EditorGUILayout.LabelField(
                $"分页 {_main.PageNames.Count}　条目 {_main.Watches.Count}", EditorStyles.boldLabel);

            if (_main.PageNames.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "勾选「显示」的分页才会出现在 Game 面板；一个都不勾等于全部显示。同一个 GV 出现在多个分页时只显示靠前的那条。",
                    MessageType.Info);
            }

            m_PageList.DoLayoutList();

            if (_main.PageNames.Count == 0)
                EditorGUILayout.HelpBox("没有分页，所有条目一起显示，Game 面板也不出现页签栏。", MessageType.None);
        }

        private void DrawPageElement(Rect _rect, int _index)
        {
            ActionEngine_GValueMonitor _main = Main;
            List<string> _pages = _main.PageNames;
            if (_index < 0 || _index >= _pages.Count) return;

            _rect.y += 2f;
            _rect.height = RowHeight;

            Rect _toggleRect = new Rect(_rect.x, _rect.y, 46f, RowHeight);
            bool _selected = _main.SelectedPages.Contains(_index);
            if (GUI.Toggle(_toggleRect, _selected, "显示") != _selected)
            {
                Undo.RecordObject(_main, "切换分页显示");
                if (!_main.SelectedPages.Remove(_index)) _main.SelectedPages.Add(_index);
                EditorUtility.SetDirty(_main);
            }

            Rect _countRect = new Rect(_rect.xMax - 34f, _rect.y, 34f, RowHeight);
            Rect _nameRect = new Rect(_rect.x + 50f, _rect.y, _rect.width - 50f - 36f, RowHeight);

            EditorGUI.BeginChangeCheck();
            string _name = EditorGUI.TextField(_nameRect, _pages[_index]);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_main, "修改分页名");
                _pages[_index] = _name;
                EditorUtility.SetDirty(_main);
            }

            int _count = _index < m_PageBuffers.Count ? m_PageBuffers[_index].Count : 0;
            EditorGUI.LabelField(_countRect, $"{_count} 条", EditorStyles.miniLabel);
        }

        private void DrawPageBodies()
        {
            for (int p = 0; p < m_EntryLists.Count; p++)
            {
                // 分页数在本帧的列表回调里可能刚被改过，标签数组要等下一帧才同步，这里按长度兜底
                string _title = p < m_PageLabels.Length
                    ? $"{m_PageLabels[p]}（{m_PageBuffers[p].Count}）"
                    : $"GV 条目（{m_PageBuffers[p].Count}）";

                m_PageFolds[p] = EditorGUILayout.Foldout(m_PageFolds[p], _title, true, EditorStyles.foldoutHeader);
                if (!m_PageFolds[p]) continue;

                m_EntryLists[p].DoLayoutList();
            }
        }

        private void AddPage()
        {
            ActionEngine_GValueMonitor _main = Main;
            Undo.RecordObject(_main, "添加分页");
            _main.PageNames.Add($"分页{_main.PageNames.Count}");
            InvalidateLists();
            EditorUtility.SetDirty(_main);
        }

        private void RemovePage(int _pageIndex)
        {
            ActionEngine_GValueMonitor _main = Main;
            List<string> _pages = _main.PageNames;
            if (_pageIndex < 0) _pageIndex = _pages.Count - 1; // 没选中任何行时按 Unity 惯例删最后一个
            if (_pageIndex < 0 || _pageIndex >= _pages.Count) return;

            int _count = m_PageBuffers[_pageIndex].Count;
            if (_count > 0 && !EditorUtility.DisplayDialog(
                    "删除分页",
                    $"分页「{_pages[_pageIndex]}」下有 {_count} 个 GV 条目，会一并删除。",
                    "删除", "取消"))
            {
                return;
            }

            Undo.RecordObject(_main, "删除分页");

            // 条目与选中集合都按下标引用分页，删页后必须整体前移，否则归属会错位
            List<GValueWatchEntry> _watches = _main.Watches;
            for (int i = _watches.Count - 1; i >= 0; i--)
            {
                GValueWatchEntry _entry = _watches[i];
                if (_entry == null) continue;

                int _page = PageOf(_main, _entry);
                if (_page == _pageIndex) _watches.RemoveAt(i);
                else if (_page > _pageIndex) _entry.PageIndex = _page - 1;
            }

            List<int> _selected = _main.SelectedPages;
            for (int i = _selected.Count - 1; i >= 0; i--)
            {
                if (_selected[i] == _pageIndex) _selected.RemoveAt(i);
                else if (_selected[i] > _pageIndex) _selected[i]--;
            }

            _pages.RemoveAt(_pageIndex);
            InvalidateLists();
            EditorUtility.SetDirty(_main);
        }

        /// <summary>
        /// 分页列表本身已被 ReorderableList 重排完毕，这里把按下标引用分页的两处跟着搬过去：
        /// 条目的所属分页，和「显示」勾选集合。
        /// 重排在回调触发前就已落到数据上，事后 RecordObject 只会记下重排后的状态，
        /// 与其留一个撤销到一半的不一致状态，不如让拖拽整体不进 Undo 栈。
        /// </summary>
        private void RemapPageOrder(int _oldIndex, int _newIndex)
        {
            if (_oldIndex == _newIndex) return;

            ActionEngine_GValueMonitor _main = Main;
            List<GValueWatchEntry> _watches = _main.Watches;
            for (int i = 0; i < _watches.Count; i++)
            {
                GValueWatchEntry _entry = _watches[i];
                if (_entry == null) continue;
                _entry.PageIndex = ShiftIndex(_entry.ResolvePageIndex(_main.PageNames.Count), _oldIndex, _newIndex);
            }

            List<int> _selected = _main.SelectedPages;
            for (int i = 0; i < _selected.Count; i++)
                _selected[i] = ShiftIndex(_selected[i], _oldIndex, _newIndex);

            m_LastKeys.Clear();
            EditorUtility.SetDirty(_main);
        }

        /// <summary> 元素从 _old 移动到 _new 后，下标 _value 的新位置。 </summary>
        private static int ShiftIndex(int _value, int _old, int _new)
        {
            if (_value == _old) return _new;
            if (_old < _new && _value > _old && _value <= _new) return _value - 1;
            if (_old > _new && _value >= _new && _value < _old) return _value + 1;
            return _value;
        }

        #endregion

        #region 条目

        private float ElementHeight(int _pageIndex, int _index)
        {
            List<GValueWatchEntry> _buffer = m_PageBuffers[_pageIndex];
            if (_index < 0 || _index >= _buffer.Count) return ElementPad + RowHeight;

            GValueWatchEntry _entry = _buffer[_index];
            if (_entry == null) return ElementPad + RowHeight;

            return TryGetGroup(_entry.GroupIndex, out _)
                ? ElementHeightNormal
                : ElementHeightBadGroup;
        }

        private void DrawEntryElement(int _pageIndex, Rect _rect, int _index)
        {
            ActionEngine_GValueMonitor _main = Main;
            List<GValueWatchEntry> _buffer = m_PageBuffers[_pageIndex];
            if (_index < 0 || _index >= _buffer.Count) return;

            GValueWatchEntry _entry = _buffer[_index];
            _rect.y += 2f;

            if (_entry == null)
            {
                Rect _fixRect = new Rect(_rect.x, _rect.y, _rect.width, RowHeight);
                if (!GUI.Button(_fixRect, "条目丢失，点击重建")) return;

                Undo.RecordObject(_main, "重建 GV 监视项");
                int _at = _main.Watches.IndexOf(null);
                if (_at >= 0) _main.Watches[_at] = new GValueWatchEntry { PageIndex = _pageIndex };
                m_LastKeys.Clear();
                EditorUtility.SetDirty(_main);
                return;
            }

            DrawEntryHeaderRow(_main, _entry, new Rect(_rect.x, _rect.y, _rect.width, RowHeight));

            float _y = _rect.y + RowHeight + Gap;
            _y = DrawEntryGValueRows(_main, _entry, _rect.x, _y, _rect.width);
            DrawOverrideNameRow(_main, _entry, new Rect(_rect.x, _y, _rect.width, RowHeight));
        }

        private void DrawEntryHeaderRow(ActionEngine_GValueMonitor _main, GValueWatchEntry _entry, Rect _rect)
        {
            Rect _typeRect = new Rect(_rect.x, _rect.y, 66f, RowHeight);
            int _typeIdx = IndexOfType(_entry.Type);
            int _newIdx = EditorGUI.Popup(_typeRect, _typeIdx, SupportedTypeNames);
            if (_newIdx != _typeIdx) ChangeType(_main, _entry, SupportedTypes[_newIdx]);

            if (m_PageLabels.Length > 0)
            {
                Rect _pageRect = new Rect(_rect.x + 70f, _rect.y, 96f, RowHeight);
                int _curPage = _entry.ResolvePageIndex(m_PageLabels.Length);
                int _newPage = EditorGUI.Popup(_pageRect, _curPage, m_PageLabels);
                if (_newPage != _curPage)
                {
                    Undo.RecordObject(_main, "移动 GV 到其他分页");
                    _entry.PageIndex = _newPage;
                    EditorUtility.SetDirty(_main);
                }
            }

            Rect _writeRect = new Rect(_rect.xMax - 46f, _rect.y, 46f, RowHeight);
            bool _writable = GUI.Toggle(_writeRect, _entry.Writable, "可写");
            if (_writable == _entry.Writable) return;

            Undo.RecordObject(_main, "修改 GV 可写");
            _entry.Writable = _writable;
            EditorUtility.SetDirty(_main);
        }

        /// <summary>
        /// mValueIndex 是「同类型内的序号」，换类型后旧序号指向的是另一个槽位，
        /// 所以只保留组，序号归零让用户重选。
        /// </summary>
        private void ChangeType(ActionEngine_GValueMonitor _main, GValueWatchEntry _entry, EGValueType _type)
        {
            Undo.RecordObject(_main, "修改 GV 类型");

            ushort _group = _entry.GroupIndex;
            _entry.Type = _type;
            _entry.Value = GValueWatchEntry.CreateValue(_type);
            _entry.Value.mValueGroupIndex = _group;
            _entry.Value.mValueIndex = 0;
            _entry.InvalidateCache();

            m_LastKeys.Clear();
            EditorUtility.SetDirty(_main);
        }

        /// <returns> 下一行的 y 坐标。 </returns>
        private float DrawEntryGValueRows(
            ActionEngine_GValueMonitor _main, GValueWatchEntry _entry, float _x, float _y, float _width)
        {
            if (_entry.Value == null)
            {
                Undo.RecordObject(_main, "修复 GV 引用");
                _entry.Value = GValueWatchEntry.CreateValue(_entry.Type);
                EditorUtility.SetDirty(_main);
            }

            ushort _group = _entry.GroupIndex;
            if (TryGetGroup(_group, out _))
            {
                _entry.Value = DrawEditorAttribute.DrawGValue(
                    new Rect(_x, _y, _width, GvRowHeight), "GV", _entry.Value, _entry.Type, 26);
                return _y + GvRowHeight + Gap;
            }

            // DrawGValue 内部按组 ID 直接索引字典，组不存在会抛异常，这里必须先拦住
            EditorGUI.HelpBox(
                new Rect(_x, _y, _width, HelpBoxHeight),
                $"GV 组 [{_group}] 不存在，无法绘制下拉。", MessageType.Error);

            _y += HelpBoxHeight + Gap;
            DrawGroupFallbackPicker(_main, _entry, new Rect(_x, _y, _width, RowHeight));
            return _y + RowHeight + Gap;
        }

        private void DrawGroupFallbackPicker(
            ActionEngine_GValueMonitor _main, GValueWatchEntry _entry, Rect _rect)
        {
            List<EditorEngineGValue> _groups = ResourcesWindow.Instance.mEngineGValueList;
            if (_groups == null || _groups.Count == 0) return;

            EditorGUI.LabelField(new Rect(_rect.x, _rect.y, 44f, RowHeight), "改到组");

            float _x = _rect.x + 46f;
            for (int i = 0; i < _groups.Count; i++)
            {
                EditorEngineGValue _g = _groups[i];
                if (_g == null) continue;
                if (_x + 30f > _rect.xMax) return;

                Rect _btn = new Rect(_x, _rect.y, 30f, RowHeight);
                _x += 32f;
                if (!GUI.Button(_btn, _g.mID.ToString(), EditorStyles.miniButton)) continue;

                Undo.RecordObject(_main, "修改 GV 组");
                _entry.Value.mValueGroupIndex = _g.mID;
                _entry.Value.mValueIndex = 0;
                _entry.InvalidateCache();
                m_LastKeys.Clear();
                EditorUtility.SetDirty(_main);
                return;
            }
        }

        private void DrawOverrideNameRow(
            ActionEngine_GValueMonitor _main, GValueWatchEntry _entry, Rect _rect)
        {
            EditorGUI.LabelField(new Rect(_rect.x, _rect.y, 44f, RowHeight), "显示名");

            float _fieldWidth = Mathf.Max(_rect.width - 44f - 120f, 60f);
            Rect _fieldRect = new Rect(_rect.x + 44f, _rect.y, _fieldWidth, RowHeight);

            EditorGUI.BeginChangeCheck();
            string _name = EditorGUI.TextField(_fieldRect, _entry.OverrideName);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_main, "修改 GV 显示名");
                _entry.OverrideName = _name;
                _entry.InvalidateCache();
                EditorUtility.SetDirty(_main);
            }

            Rect _previewRect = new Rect(_fieldRect.xMax + 4f, _rect.y, _rect.xMax - _fieldRect.xMax - 4f, RowHeight);
            EditorGUI.LabelField(_previewRect, $"→ {_entry.DisplayName}", EditorStyles.miniLabel);
        }

        private void AddEntry(int _pageIndex)
        {
            ActionEngine_GValueMonitor _main = Main;
            Undo.RecordObject(_main, "添加 GV 监视项");
            _main.Watches.Add(new GValueWatchEntry { PageIndex = _pageIndex });
            m_LastKeys.Clear();
            EditorUtility.SetDirty(_main);
        }

        private void RemoveEntry(int _pageIndex, int _index)
        {
            List<GValueWatchEntry> _buffer = m_PageBuffers[_pageIndex];
            if (_index < 0) _index = _buffer.Count - 1; // 没选中任何行时按 Unity 惯例删最后一个
            if (_index < 0 || _index >= _buffer.Count) return;

            ActionEngine_GValueMonitor _main = Main;
            int _at = _main.Watches.IndexOf(_buffer[_index]);
            if (_at < 0) return;

            Undo.RecordObject(_main, "移除 GV 监视项");
            _main.Watches.RemoveAt(_at);
            m_LastKeys.Clear();
            EditorUtility.SetDirty(_main);
        }

        /// <summary>
        /// 缓冲列表已被 ReorderableList 重排，把新顺序填回扁平条目列表中属于本页的那些位置，
        /// 其它分页的条目位置保持不动。与分页拖拽同理，重排不进 Undo 栈。
        /// </summary>
        private void ApplyEntryOrder(int _pageIndex)
        {
            ActionEngine_GValueMonitor _main = Main;
            List<GValueWatchEntry> _watches = _main.Watches;
            List<GValueWatchEntry> _buffer = m_PageBuffers[_pageIndex];
            bool _implicitPage = _main.PageNames.Count == 0;

            int _cursor = 0;
            for (int i = 0; i < _watches.Count && _cursor < _buffer.Count; i++)
            {
                int _page = _implicitPage ? 0 : PageOf(_main, _watches[i]);
                if (_page != _pageIndex) continue;
                _watches[i] = _buffer[_cursor++];
            }

            m_LastKeys.Clear();
            EditorUtility.SetDirty(_main);
        }

        #endregion

        #region 工具

        private void InvalidateLists()
        {
            m_BuiltPageCount = -1;
        }

        private void RebuildPageLabels(List<string> _pages)
        {
            if (m_PageLabels.Length != _pages.Count) m_PageLabels = new string[_pages.Count];

            for (int i = 0; i < _pages.Count; i++)
                m_PageLabels[i] = string.IsNullOrEmpty(_pages[i]) ? $"页{i}" : _pages[i];
        }

        private static int PageOf(ActionEngine_GValueMonitor _main, GValueWatchEntry _entry)
        {
            return _entry == null ? 0 : _entry.ResolvePageIndex(_main.PageNames.Count);
        }

        private void SyncKeySnapshot(ActionEngine_GValueMonitor _main)
        {
            List<GValueWatchEntry> _watches = _main.Watches;

            if (m_LastKeys.Count != _watches.Count)
            {
                m_LastKeys.Clear();
                for (int i = 0; i < _watches.Count; i++) m_LastKeys.Add(MakeKey(_watches[i]));
                return;
            }

            bool _changed = false;
            for (int i = 0; i < _watches.Count; i++)
            {
                GValueKey _key = MakeKey(_watches[i]);
                if (m_LastKeys[i].Same(_key)) continue;

                m_LastKeys[i] = _key;
                _watches[i]?.InvalidateCache();
                _changed = true;
            }

            if (_changed) EditorUtility.SetDirty(_main);
        }

        private static GValueKey MakeKey(GValueWatchEntry _entry)
        {
            if (_entry == null) return new GValueKey();
            return new GValueKey
            {
                Group = _entry.GroupIndex,
                Index = _entry.ValueIndex,
                Type = _entry.Type,
                Page = _entry.PageIndex,
            };
        }

        private static int IndexOfType(EGValueType _type)
        {
            for (int i = 0; i < SupportedTypes.Length; i++)
                if (SupportedTypes[i] == _type) return i;
            return 0;
        }

        private static bool TryGetGroup(ushort _groupID, out EditorEngineGValue _group)
        {
            _group = null;
            List<EditorEngineGValue> _groups = ResourcesWindow.Instance.mEngineGValueList;
            if (_groups == null) return false;

            for (int i = 0; i < _groups.Count; i++)
            {
                if (_groups[i] == null || _groups[i].mID != _groupID) continue;
                _group = _groups[i];
                return true;
            }

            return false;
        }

        #endregion
    }
}
