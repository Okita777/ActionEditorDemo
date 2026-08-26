using System;
using System.Collections.Generic;
using System.Reflection;
using AsiSkillEditor.RunTime;
using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    public sealed class SkillResourceEntryWindow : EditorWindow
    {
        private readonly List<SkillResourceFileEntry> _unitEntries = new List<SkillResourceFileEntry>();
        private readonly List<SkillResourceFileEntry> _skillEntries = new List<SkillResourceFileEntry>();
        private readonly List<SkillResourceFileEntry> _metaSkillEntries = new List<SkillResourceFileEntry>();
        private readonly List<SkillResourceFileEntry> _stateEntries = new List<SkillResourceFileEntry>();
        private readonly List<SkillResourceFileEntry> _buffEntries = new List<SkillResourceFileEntry>();

        private SkillResourceType _resourceType = SkillResourceType.Unit;
        private Vector2 _scrollPosition;
        private SkillResourceFileEntry _selectedEntry;
        private bool _selectionDirty;

        [MenuItem("Tools/SkillEditor/Resource Entry")]
        public static void Open()
        {
            SkillResourceEntryWindow window = GetWindow<SkillResourceEntryWindow>();
            window.titleContent = new GUIContent("Skill Resources");
            window.minSize = new Vector2(760f, 560f);
            window.RefreshAssets();
        }

        private void OnEnable()
        {
            RefreshAssets();
        }

        private void OnProjectChange()
        {
            RefreshAssets();
            Repaint();
        }

        private void OnGUI()
        {
            DrawResourceTypeTabs();
            EditorGUILayout.Space(4f);
            DrawUnitContextBar();
            EditorGUILayout.Space(4f);
            DrawToolbar();
            EditorGUILayout.Space(8f);
            DrawList();
        }

        private void DrawResourceTypeTabs()
        {
            EditorGUILayout.BeginHorizontal();
            DrawTypeTabButton(SkillResourceType.Unit, "Unit");
            DrawTypeTabButton(SkillResourceType.Skill, "技能");
            DrawTypeTabButton(SkillResourceType.MetaSkill, "元技能");
            DrawTypeTabButton(SkillResourceType.State, "状态");
            DrawTypeTabButton(SkillResourceType.Buff, "Buff");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTypeTabButton(SkillResourceType type, string label)
        {
            bool isSelected = _resourceType == type;
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 44f,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
            };

            Color backgroundColor = GUI.backgroundColor;
            if (isSelected)
            {
                GUI.backgroundColor = new Color(0.75f, 0.85f, 1f);
            }

            if (GUILayout.Button(label, style))
            {
                SwitchResourceType(type);
            }

            GUI.backgroundColor = backgroundColor;
        }

        private void DrawUnitContextBar()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("当前 Unit 上下文", EditorStyles.boldLabel);
            if (_unitEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("当前还没有 UnitResource。请先切到 Unit 标签创建。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            string activeUnitId = SkillPreviewUnitSettings.ActiveUnitId;
            SkillResourceFileEntry activeUnitEntry = null;
            string[] options = new string[_unitEntries.Count + 1];
            options[0] = "<未指定 Unit>";
            int currentIndex = 0;
            for (int i = 0; i < _unitEntries.Count; i++)
            {
                UnitConfig config = _unitEntries[i].Config as UnitConfig;
                string unitId = config != null ? config.UnitId : string.Empty;
                string displayName = config != null && !string.IsNullOrEmpty(config.DisplayName)
                    ? config.DisplayName
                    : _unitEntries[i].BaseName;
                options[i + 1] = string.IsNullOrEmpty(unitId) ? displayName : $"{displayName} ({unitId})";
                if (string.Equals(unitId, activeUnitId, System.StringComparison.Ordinal))
                {
                    currentIndex = i + 1;
                    activeUnitEntry = _unitEntries[i];
                }
            }

            EditorGUILayout.BeginHorizontal();
            int nextIndex = EditorGUILayout.Popup("Active Unit", currentIndex, options);
            string nextUnitId = nextIndex <= 0 ? string.Empty : ((_unitEntries[nextIndex - 1].Config as UnitConfig)?.UnitId ?? string.Empty);
            if (!string.Equals(nextUnitId, activeUnitId, System.StringComparison.Ordinal))
            {
                SkillPreviewUnitSettings.ActiveUnitId = nextUnitId;
                SkillPreviewUnitSettings.Save();
                RefreshAssets();
                activeUnitEntry = GetActiveUnitEntry();
            }

            using (new EditorGUI.DisabledScope(activeUnitEntry == null))
            {
                if (GUILayout.Button("编辑", GUILayout.Width(68f)))
                {
                    OpenUnitInspector(activeUnitEntry);
                }

                if (GUILayout.Button("Apply", GUILayout.Width(68f)))
                {
                    ApplyActiveUnit(activeUnitEntry);
                }
            }

            using (new EditorGUI.DisabledScope(SkillPreviewUnitSettings.LoadActivePrefab() == null))
            {
                if (GUILayout.Button("Clear", GUILayout.Width(68f)))
                {
                    SkillPreviewSceneInstanceUtility.RemoveCurrentInstance();
                    SkillPreviewUnitSettings.ClearActivePreviewCarrier();
                    RefreshAssets();
                }
            }

            EditorGUILayout.EndHorizontal();

            if (RequiresUnitContext() && !HasActiveUnitContext())
            {
                EditorGUILayout.HelpBox("当前资源类型需要先指定 Active Unit。", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !RequiresUnitContext() || HasActiveUnitContext();
            if (GUILayout.Button("新建", GUILayout.Height(36f)))
            {
                CreateEntry();
            }

            GUI.enabled = _selectedEntry != null && (!RequiresUnitContext() || HasActiveUnitContext() || _selectedEntry.ResourceType == SkillResourceType.Unit);
            if (GUILayout.Button("复制", GUILayout.Height(36f)))
            {
                DuplicateEntry();
            }

            if (GUILayout.Button("删除", GUILayout.Height(36f)))
            {
                DeleteSelectedEntry();
            }

            string saveLabel = SkillResourceRepository.HasDirtyEntries() ? "保存全部*" : "保存";
            if (GUILayout.Button(saveLabel, GUILayout.Height(36f)))
            {
                SaveSelectedEntry();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawList()
        {
            EditorGUILayout.BeginVertical("box");
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(220f));

            List<SkillResourceFileEntry> entries = GetActiveEntries();
            if (RequiresUnitContext() && !HasActiveUnitContext())
            {
                EditorGUILayout.HelpBox("请先在上方选择 Active Unit。", MessageType.Info);
            }
            else if (entries.Count == 0)
            {
                EditorGUILayout.LabelField("暂无资源。");
            }
            else
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    DrawEntryButton(entries[i], SkillResourceRepository.GetDisplayName(entries[i]));
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawEntryButton(SkillResourceFileEntry entry, string label)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 48f,
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
            };

            Color backgroundColor = GUI.backgroundColor;
            if (_selectedEntry == entry)
            {
                GUI.backgroundColor = new Color(0.85f, 0.92f, 1f);
            }

            if (GUILayout.Button(label, style))
            {
                _selectedEntry = entry;
                if (entry.ResourceType == SkillResourceType.Unit)
                {
                    OpenUnitInspector(entry);
                }
                else if (entry.ResourceType == SkillResourceType.Skill)
                {
                    SkillEditorInspectorWindow.OpenSkill(entry);
                }
                else if (entry.ResourceType == SkillResourceType.MetaSkill)
                {
                    SkillEditorInspectorWindow.OpenMetaSkill(entry);
                }
                else if (entry.ResourceType == SkillResourceType.State)
                {
                    SkillEditorInspectorWindow.OpenState(entry);
                }
                else if (entry.ResourceType == SkillResourceType.Buff)
                {
                    SkillEditorInspectorWindow.OpenBuff(entry);
                }
            }

            GUI.backgroundColor = backgroundColor;
            GUILayout.Space(2f);
        }

        private static void DrawUnitActiveSkillSlots(UnitConfig config)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("主动技能槽位", EditorStyles.boldLabel);
            config.ActiveSkillSlots ??= new List<UnitActiveSkillSlotConfig>();

            List<SkillResourceFileEntry> allSkillEntries = SkillResourceRepository.LoadSkills(config.UnitId);
            List<SkillResourceFileEntry> skillEntries = FilterUnitSkillEntriesByCategory(allSkillEntries, SkillCastCategory.Active, config.ActiveSkillSlots, null);
            for (int i = 0; i < config.ActiveSkillSlots.Count; i++)
            {
                UnitActiveSkillSlotConfig slot = config.ActiveSkillSlots[i] ?? new UnitActiveSkillSlotConfig();
                config.ActiveSkillSlots[i] = slot;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                slot.DisplayName = EditorGUILayout.TextField("名称", slot.DisplayName);
                if (GUILayout.Button("-", GUILayout.Width(28f)))
                {
                    config.ActiveSkillSlots.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                slot.SlotIndex = Mathf.Max(1, EditorGUILayout.IntField("槽位编号", slot.SlotIndex));
                slot.ActionName = InputActionEditorUtility.DrawActionPopup("输入动作", slot.ActionName);
                slot.SkillId = DrawSkillPopup("技能", skillEntries, slot.SkillId);
                DrawSkillSummary(allSkillEntries, slot.SkillId, SkillCastCategory.Active);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("添加主动技能槽", GUILayout.Height(28f)))
            {
                config.ActiveSkillSlots.Add(new UnitActiveSkillSlotConfig
                {
                    SlotIndex = config.ActiveSkillSlots.Count + 1,
                    DisplayName = $"主动技能槽{config.ActiveSkillSlots.Count + 1}",
                });
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawUnitPassiveSkillSlots(UnitConfig config)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("被动技能槽位", EditorStyles.boldLabel);
            config.PassiveSkillSlots ??= new List<UnitPassiveSkillSlotConfig>();

            List<SkillResourceFileEntry> allSkillEntries = SkillResourceRepository.LoadSkills(config.UnitId);
            List<SkillResourceFileEntry> skillEntries = FilterUnitSkillEntriesByCategory(allSkillEntries, SkillCastCategory.Passive, null, config.PassiveSkillSlots);
            for (int i = 0; i < config.PassiveSkillSlots.Count; i++)
            {
                UnitPassiveSkillSlotConfig slot = config.PassiveSkillSlots[i] ?? new UnitPassiveSkillSlotConfig();
                config.PassiveSkillSlots[i] = slot;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                slot.DisplayName = EditorGUILayout.TextField("名称", slot.DisplayName);
                if (GUILayout.Button("-", GUILayout.Width(28f)))
                {
                    config.PassiveSkillSlots.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                slot.SlotIndex = Mathf.Max(1, EditorGUILayout.IntField("槽位编号", slot.SlotIndex));
                slot.SkillId = DrawSkillPopup("技能", skillEntries, slot.SkillId);
                DrawSkillSummary(allSkillEntries, slot.SkillId, SkillCastCategory.Passive);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("添加被动技能槽", GUILayout.Height(28f)))
            {
                config.PassiveSkillSlots.Add(new UnitPassiveSkillSlotConfig
                {
                    SlotIndex = config.PassiveSkillSlots.Count + 1,
                    DisplayName = $"被动技能槽{config.PassiveSkillSlots.Count + 1}",
                });
            }

            EditorGUILayout.EndVertical();
        }

        private static List<SkillResourceFileEntry> FilterUnitSkillEntriesByCategory(List<SkillResourceFileEntry> allSkillEntries, SkillCastCategory expectedCategory, List<UnitActiveSkillSlotConfig> activeSlots, List<UnitPassiveSkillSlotConfig> passiveSlots)
        {
            List<SkillResourceFileEntry> results = new List<SkillResourceFileEntry>();
            HashSet<string> equippedAssets = new HashSet<string>(System.StringComparer.Ordinal);
            if (activeSlots != null)
            {
                for (int i = 0; i < activeSlots.Count; i++)
                {
                    if (activeSlots[i] != null && !string.IsNullOrEmpty(activeSlots[i].SkillId))
                    {
                        equippedAssets.Add(activeSlots[i].SkillId);
                    }
                }
            }

            if (passiveSlots != null)
            {
                for (int i = 0; i < passiveSlots.Count; i++)
                {
                    if (passiveSlots[i] != null && !string.IsNullOrEmpty(passiveSlots[i].SkillId))
                    {
                        equippedAssets.Add(passiveSlots[i].SkillId);
                    }
                }
            }

            if (allSkillEntries == null)
            {
                return results;
            }

            for (int i = 0; i < allSkillEntries.Count; i++)
            {
                SkillResourceFileEntry entry = allSkillEntries[i];
                SkillConfig skillConfig = entry != null ? entry.Config as SkillConfig : null;
                if (entry == null || skillConfig == null)
                {
                    continue;
                }

                if (skillConfig.SkillCategory == expectedCategory ||
                    equippedAssets.Contains(SkillResourceRepository.GetSkillRuntimeId(entry)) ||
                    equippedAssets.Contains(skillConfig.SkillId) ||
                    equippedAssets.Contains(entry.BaseName))
                {
                    results.Add(entry);
                }
            }

            return results;
        }

        private static string DrawSkillPopup(string label, List<SkillResourceFileEntry> skillEntries, string currentSkillId)
        {
            if (skillEntries == null || skillEntries.Count == 0)
            {
                EditorGUILayout.LabelField(label, "<暂无 Skill 资源>");
                return string.Empty;
            }

            string[] options = new string[skillEntries.Count + 1];
            options[0] = "<未装配>";
            int currentIndex = 0;
            for (int i = 0; i < skillEntries.Count; i++)
            {
                SkillResourceFileEntry entry = skillEntries[i];
                SkillConfig skillConfig = entry.Config as SkillConfig;
                string displayName = SkillResourceRepository.GetDisplayName(entry);
                options[i + 1] = string.IsNullOrEmpty(skillConfig?.SkillId) ? displayName : $"{displayName} ({skillConfig.SkillId})";
                if (SkillResourceRepository.IsMatchingSkillReference(entry, currentSkillId))
                {
                    currentIndex = i + 1;
                }
            }

            int nextIndex = EditorGUILayout.Popup(label, currentIndex, options);
            if (nextIndex <= 0)
            {
                return string.Empty;
            }

            SkillConfig selectedConfig = skillEntries[nextIndex - 1].Config as SkillConfig;
            string runtimeSkillId = SkillResourceRepository.GetSkillRuntimeId(skillEntries[nextIndex - 1]);
            return !string.IsNullOrEmpty(runtimeSkillId)
                ? runtimeSkillId
                : (!string.IsNullOrEmpty(selectedConfig?.SkillId) ? selectedConfig.SkillId : skillEntries[nextIndex - 1].BaseName);
        }

        private static void DrawSkillSummary(List<SkillResourceFileEntry> skillEntries, string skillId, SkillCastCategory expectedCategory)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                EditorGUILayout.HelpBox("当前槽位还没有装配 Skill。", MessageType.Info);
                return;
            }

            SkillConfig skillConfig = FindSkillConfig(skillEntries, skillId);
            if (skillConfig == null)
            {
                EditorGUILayout.HelpBox($"未找到 Skill 资源: {skillId}", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("skillId", skillConfig.SkillId, EditorStyles.miniLabel);
            EditorGUILayout.LabelField("skillType", skillConfig.SkillCategory.ToString(), EditorStyles.miniLabel);
            if (skillConfig.SkillCategory != expectedCategory)
            {
                EditorGUILayout.HelpBox($"该 Skill 当前被标记为 {skillConfig.SkillCategory}，与当前槽位期望的 {expectedCategory} 不一致。", MessageType.Warning);
            }
        }

        private static SkillConfig FindSkillConfig(List<SkillResourceFileEntry> skillEntries, string skillReference)
        {
            if (skillEntries == null || string.IsNullOrEmpty(skillReference))
            {
                return null;
            }

            for (int i = 0; i < skillEntries.Count; i++)
            {
                if (SkillResourceRepository.IsMatchingSkillReference(skillEntries[i], skillReference))
                {
                    return skillEntries[i].Config as SkillConfig;
                }
            }

            return null;
        }

        private static string SanitizeNumericId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            char[] buffer = new char[value.Length];
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (char.IsDigit(current))
                {
                    buffer[count++] = current;
                }
            }

            return count == 0 ? string.Empty : new string(buffer, 0, count);
        }

        private void SwitchResourceType(SkillResourceType type)
        {
            _resourceType = type;
            _selectedEntry = null;
            _selectionDirty = false;
            _scrollPosition = Vector2.zero;
        }

        private void RefreshAssets()
        {
            _unitEntries.Clear();
            _skillEntries.Clear();
            _metaSkillEntries.Clear();
            _stateEntries.Clear();
            _buffEntries.Clear();

            _unitEntries.AddRange(SkillResourceRepository.LoadUnits());
            if (HasActiveUnitContext())
            {
                _skillEntries.AddRange(SkillResourceRepository.LoadSkills(SkillPreviewUnitSettings.ActiveUnitId));
                _metaSkillEntries.AddRange(SkillResourceRepository.LoadMetaSkills(SkillPreviewUnitSettings.ActiveUnitId));
                _stateEntries.AddRange(SkillResourceRepository.LoadStates(SkillPreviewUnitSettings.ActiveUnitId));
            }

            _buffEntries.AddRange(SkillResourceRepository.LoadBuffs());

            if (_selectedEntry != null && !ContainsEntry(_selectedEntry))
            {
                _selectedEntry = null;
            }
        }

        private void CreateEntry()
        {
            SkillResourceFileEntry entry = SkillResourceRepository.Create(_resourceType, SkillPreviewUnitSettings.ActiveUnitId);
            RefreshAssets();
            SelectEntryByPath(entry.JsonAssetPath);
        }

        private void DuplicateEntry()
        {
            if (_selectedEntry == null)
            {
                return;
            }

            SkillResourceFileEntry entry = SkillResourceRepository.Duplicate(_selectedEntry);
            RefreshAssets();
            if (entry != null)
            {
                SelectEntryByPath(entry.JsonAssetPath);
            }
        }

        private void DeleteSelectedEntry()
        {
            if (_selectedEntry == null)
            {
                return;
            }

            if (!EditorUtility.DisplayDialog("删除资源", $"确定删除：{SkillResourceRepository.GetDisplayName(_selectedEntry)} ?", "删除", "取消"))
            {
                return;
            }

            SkillResourceRepository.Delete(_selectedEntry);
            _selectedEntry = null;
            _selectionDirty = false;
            RefreshAssets();
        }

        private void SaveSelectedEntry()
        {
            if (_selectedEntry == null && !SkillResourceRepository.HasDirtyEntries())
            {
                return;
            }

            FlushOpenEditorChanges();

            SkillResourceFileEntry saveEntry = ResolveSaveEntry();
            string jsonAssetPath = saveEntry != null
                ? saveEntry.JsonAssetPath
                : _selectedEntry != null ? _selectedEntry.JsonAssetPath : string.Empty;

            List<SkillResourceFileEntry> dirtyEntries = SkillResourceRepository.GetDirtyEntries();
            if (dirtyEntries.Count > 0)
            {
                for (int i = 0; i < dirtyEntries.Count; i++)
                {
                    SkillResourceRepository.Save(dirtyEntries[i]);
                }
            }
            else if (saveEntry != null)
            {
                SkillResourceRepository.Save(saveEntry);
            }

            object activeStateTimelineWindow = GetActiveStateTimelineWindow();
            if (activeStateTimelineWindow != null && WindowMatchesEntry(activeStateTimelineWindow, jsonAssetPath))
            {
                InvokeWindowMethod(activeStateTimelineWindow, "MarkSavedFromOuter");
            }

            _selectionDirty = false;
            RefreshAssets();
            SelectEntryByPath(jsonAssetPath);
        }

        private SkillResourceFileEntry ResolveSaveEntry()
        {
            if (_selectedEntry == null)
            {
                return null;
            }

            object activeStateTimelineWindow = GetActiveStateTimelineWindow();
            if (activeStateTimelineWindow != null && WindowMatchesEntry(activeStateTimelineWindow, _selectedEntry.JsonAssetPath))
            {
                return InvokeWindowMethod(activeStateTimelineWindow, "GetBoundEntry") as SkillResourceFileEntry ?? _selectedEntry;
            }

            return _selectedEntry;
        }

        private static void FlushOpenEditorChanges()
        {
            object activeStateTimelineWindow = GetActiveStateTimelineWindow();
            if (activeStateTimelineWindow != null)
            {
                InvokeWindowMethod(activeStateTimelineWindow, "PrepareForOuterSave");
            }
        }

        private static object GetActiveStateTimelineWindow()
        {
            Type windowType = typeof(SkillResourceEntryWindow).Assembly.GetType("SkillEditor.Editor.StateTimelineEditorWindow");
            MethodInfo getActiveInstanceMethod = windowType?.GetMethod("GetActiveInstance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return getActiveInstanceMethod?.Invoke(null, null);
        }

        private static bool WindowMatchesEntry(object window, string jsonAssetPath)
        {
            object result = InvokeWindowMethod(window, "MatchesEntry", jsonAssetPath);
            return result is bool matched && matched;
        }

        private static object InvokeWindowMethod(object instance, string methodName, params object[] args)
        {
            if (instance == null)
            {
                return null;
            }

            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return method?.Invoke(instance, args);
        }

        private void SelectEntryByPath(string jsonAssetPath)
        {
            List<SkillResourceFileEntry> allEntries = GetAllEntries();
            for (int i = 0; i < allEntries.Count; i++)
            {
                if (allEntries[i].JsonAssetPath == jsonAssetPath)
                {
                    _selectedEntry = allEntries[i];
                    _selectionDirty = false;
                    return;
                }
            }
        }

        private List<SkillResourceFileEntry> GetActiveEntries()
        {
            switch (_resourceType)
            {
                case SkillResourceType.Unit:
                    return _unitEntries;
                case SkillResourceType.Skill:
                    return _skillEntries;
                case SkillResourceType.MetaSkill:
                    return _metaSkillEntries;
                case SkillResourceType.State:
                    return _stateEntries;
                case SkillResourceType.Buff:
                    return _buffEntries;
                default:
                    return _unitEntries;
            }
        }

        private List<SkillResourceFileEntry> GetAllEntries()
        {
            List<SkillResourceFileEntry> allEntries = new List<SkillResourceFileEntry>();
            allEntries.AddRange(_unitEntries);
            allEntries.AddRange(_skillEntries);
            allEntries.AddRange(_metaSkillEntries);
            allEntries.AddRange(_stateEntries);
            allEntries.AddRange(_buffEntries);
            return allEntries;
        }

        private bool ContainsEntry(SkillResourceFileEntry entry)
        {
            List<SkillResourceFileEntry> allEntries = GetAllEntries();
            for (int i = 0; i < allEntries.Count; i++)
            {
                if (allEntries[i].JsonAssetPath == entry.JsonAssetPath)
                {
                    return true;
                }
            }

            return false;
        }

        private bool RequiresUnitContext()
        {
            return _resourceType == SkillResourceType.Skill ||
                   _resourceType == SkillResourceType.MetaSkill ||
                   _resourceType == SkillResourceType.State;
        }

        private bool HasActiveUnitContext()
        {
            string activeUnitId = SkillPreviewUnitSettings.ActiveUnitId;
            if (string.IsNullOrEmpty(activeUnitId))
            {
                return false;
            }

            for (int i = 0; i < _unitEntries.Count; i++)
            {
                UnitConfig config = _unitEntries[i].Config as UnitConfig;
                if (config != null && string.Equals(config.UnitId, activeUnitId, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private SkillResourceFileEntry GetActiveUnitEntry()
        {
            string activeUnitId = SkillPreviewUnitSettings.ActiveUnitId;
            if (string.IsNullOrEmpty(activeUnitId))
            {
                return null;
            }

            for (int i = 0; i < _unitEntries.Count; i++)
            {
                if (_unitEntries[i].Config is UnitConfig config && string.Equals(config.UnitId, activeUnitId, System.StringComparison.Ordinal))
                {
                    return _unitEntries[i];
                }
            }

            return null;
        }

        private void OpenUnitInspector(SkillResourceFileEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            SkillEditorInspectorWindow.OpenUnit(entry);
        }

        private void ApplyActiveUnit(SkillResourceFileEntry entry)
        {
            if (entry == null || !(entry.Config is UnitConfig unitConfig))
            {
                return;
            }

            if (!TryApplyUnit(unitConfig, out string errorMessage))
            {
                EditorUtility.DisplayDialog("Apply Unit 失败", errorMessage, "确定");
                return;
            }

            RefreshAssets();
            Repaint();
        }

        private static bool TryApplyUnit(UnitConfig unitConfig, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (unitConfig == null)
            {
                errorMessage = "当前没有可应用的 Unit。";
                return false;
            }

            if (string.IsNullOrEmpty(unitConfig.PrefabAssetPath))
            {
                errorMessage = "当前 Unit 还没有配置 prefab。";
                return false;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(unitConfig.PrefabAssetPath);
            if (prefab == null)
            {
                errorMessage = $"未找到 prefab: {unitConfig.PrefabAssetPath}";
                return false;
            }

            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                errorMessage = "当前 prefab 缺少 Animator。";
                return false;
            }

            SkillEditor.Preview.GameUnit previewConfig = prefab.GetComponent<SkillEditor.Preview.GameUnit>();
            if (previewConfig == null)
            {
                errorMessage = "当前 prefab 缺少 GameUnit 组件。";
                return false;
            }

            if (previewConfig.MountPoints == null || previewConfig.MountPoints.Count == 0)
            {
                errorMessage = "当前 prefab 还没有配置角色挂点。";
                return false;
            }

            for (int i = 0; i < previewConfig.MountPoints.Count; i++)
            {
                if (previewConfig.MountPoints[i] != null && previewConfig.MountPoints[i].MountTransform == null)
                {
                    errorMessage = "当前 prefab 存在空的角色挂点引用。";
                    return false;
                }
            }

            if (!SkillPreviewSceneInstanceUtility.ValidateCameraConfiguration(unitConfig, prefab, out errorMessage))
            {
                return false;
            }

            previewConfig.UnitId = unitConfig.UnitId;
            if (!string.IsNullOrEmpty(unitConfig.AnimationDirectory))
            {
                previewConfig.AnimationSearchRoot = unitConfig.AnimationDirectory;
            }

            previewConfig.ActiveSkillSlots = new List<SkillEditor.Preview.PreviewActiveSkillSlotConfig>();
            if (unitConfig.ActiveSkillSlots != null)
            {
                for (int i = 0; i < unitConfig.ActiveSkillSlots.Count; i++)
                {
                    UnitActiveSkillSlotConfig slot = unitConfig.ActiveSkillSlots[i];
                    if (slot == null)
                    {
                        continue;
                    }

                    previewConfig.ActiveSkillSlots.Add(new SkillEditor.Preview.PreviewActiveSkillSlotConfig
                    {
                        SlotIndex = slot.SlotIndex,
                        DisplayName = slot.DisplayName,
                        ActionName = slot.ActionName,
                        SkillAssetName = SkillResourceRepository.NormalizeSkillReference(unitConfig.UnitId, slot.SkillId),
                    });
                }
            }

            previewConfig.PassiveSkillSlots = new List<SkillEditor.Preview.PreviewPassiveSkillSlotConfig>();
            if (unitConfig.PassiveSkillSlots != null)
            {
                for (int i = 0; i < unitConfig.PassiveSkillSlots.Count; i++)
                {
                    UnitPassiveSkillSlotConfig slot = unitConfig.PassiveSkillSlots[i];
                    if (slot == null)
                    {
                        continue;
                    }

                    previewConfig.PassiveSkillSlots.Add(new SkillEditor.Preview.PreviewPassiveSkillSlotConfig
                    {
                        SlotIndex = slot.SlotIndex,
                        DisplayName = slot.DisplayName,
                        SkillAssetName = SkillResourceRepository.NormalizeSkillReference(unitConfig.UnitId, slot.SkillId),
                    });
                }
            }

            EditorUtility.SetDirty(previewConfig);
            AssetDatabase.SaveAssets();

            SkillPreviewUnitSettings.ActiveUnitId = unitConfig.UnitId;
            SkillPreviewUnitSettings.ActivePrefabPath = unitConfig.PrefabAssetPath;
            SkillPreviewUnitSettings.Save();
            SkillPreviewSceneInstanceUtility.CreateOrReplace(prefab, unitConfig.CameraResourcePath);
            return true;
        }
    }
}
