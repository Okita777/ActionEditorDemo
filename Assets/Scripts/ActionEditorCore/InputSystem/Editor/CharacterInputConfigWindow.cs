using System;
using System.Collections.Generic;
using System.IO;
using ActionEditor.InputSystem;
using UnityEditor;
using UnityEngine;

namespace ActionEditor.InputSystem.Editor
{
    public sealed class CharacterInputConfigWindow : EditorWindow
    {
        private sealed class KeyCodeProbe : ScriptableObject
        {
            public KeyCode Value = KeyCode.None;
        }

        private static readonly KeyCode[] PcKeyOptions =
        {
            KeyCode.None,
            KeyCode.Mouse0,
            KeyCode.Mouse1,
            KeyCode.Mouse2,
            KeyCode.Space,
            KeyCode.Tab,
            KeyCode.Return,
            KeyCode.Escape,
            KeyCode.LeftShift,
            KeyCode.RightShift,
            KeyCode.LeftControl,
            KeyCode.RightControl,
            KeyCode.LeftAlt,
            KeyCode.RightAlt,
            KeyCode.UpArrow,
            KeyCode.DownArrow,
            KeyCode.LeftArrow,
            KeyCode.RightArrow,
            KeyCode.W,
            KeyCode.A,
            KeyCode.S,
            KeyCode.D,
            KeyCode.Q,
            KeyCode.E,
            KeyCode.R,
            KeyCode.F,
            KeyCode.Z,
            KeyCode.X,
            KeyCode.C,
            KeyCode.V,
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8,
            KeyCode.Alpha9,
            KeyCode.Alpha0,
            KeyCode.F1,
            KeyCode.F2,
            KeyCode.F3,
            KeyCode.F4,
            KeyCode.F5,
            KeyCode.F6,
            KeyCode.F7,
            KeyCode.F8,
            KeyCode.F9,
            KeyCode.F10,
            KeyCode.F11,
            KeyCode.F12,
        };

        private static readonly string[] PcKeyLabels =
        {
            "None",
            "Mouse0 (Left)",
            "Mouse1 (Right)",
            "Mouse2 (Middle)",
            "Space",
            "Tab",
            "Enter",
            "Escape",
            "Left Shift",
            "Right Shift",
            "Left Ctrl",
            "Right Ctrl",
            "Left Alt",
            "Right Alt",
            "Up Arrow",
            "Down Arrow",
            "Left Arrow",
            "Right Arrow",
            "W",
            "A",
            "S",
            "D",
            "Q",
            "E",
            "R",
            "F",
            "Z",
            "X",
            "C",
            "V",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "0",
            "F1",
            "F2",
            "F3",
            "F4",
            "F5",
            "F6",
            "F7",
            "F8",
            "F9",
            "F10",
            "F11",
            "F12",
        };

        private CharacterInputMapConfig _config;
        private SerializedObject _serializedObject;
        private Vector2 _scrollPosition;
        private int _selectedTab;
        private static Dictionary<int, int> s_corruptedPcKeyMap;

        [MenuItem("Tools/ActionEditor/Input System")]
        public static void OpenWindow()
        {
            CharacterInputConfigWindow window = GetWindow<CharacterInputConfigWindow>("输入系统设置");
            window.minSize = new Vector2(860f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadOrCreateConfig();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_config == null)
            {
                EditorGUILayout.HelpBox("未找到主输入配置资产。该资产将作为玩家运行时输入配置。", MessageType.Warning);
                if (GUILayout.Button("创建主输入配置"))
                {
                    LoadOrCreateConfig(forceCreate: true);
                }

                return;
            }

            if (_serializedObject == null || _serializedObject.targetObject != _config)
            {
                _serializedObject = new SerializedObject(_config);
            }

            _serializedObject.Update();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case 0:
                    DrawActionList();
                    break;
                default:
                    DrawBindingList();
                    break;
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("保存配置", GUILayout.Width(120f)))
                {
                    _serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(_config);
                    AssetDatabase.SaveAssets();
                }
            }

            _serializedObject.ApplyModifiedProperties();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(_config != null ? _config.name : "主输入配置未创建", GUILayout.Width(260f));

                if (GUILayout.Button("定位资产", EditorStyles.toolbarButton, GUILayout.Width(80f)) && _config != null)
                {
                    Selection.activeObject = _config;
                    EditorGUIUtility.PingObject(_config);
                }

                if (GUILayout.Button("创建主配置", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                {
                    LoadOrCreateConfig(forceCreate: true);
                }

                GUILayout.FlexibleSpace();
                _selectedTab = GUILayout.Toolbar(_selectedTab, new[] { "行为列表配置", "按键配置" }, EditorStyles.toolbarButton, GUILayout.MinWidth(240f));
            }

            EditorGUILayout.Space();
        }

        private void DrawActionList()
        {
            SerializedProperty actions = _serializedObject.FindProperty("Actions");
            EditorGUILayout.LabelField("行为列表", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("这里只定义按钮类行为，例如 Jump、Dodge、SkillSlot1。Move/Look 为系统固定输入，不在此自由配置。", MessageType.Info);

            for (int i = 0; i < actions.arraySize; i++)
            {
                SerializedProperty item = actions.GetArrayElementAtIndex(i);
                SerializedProperty actionName = item.FindPropertyRelative("ActionName");
                SerializedProperty valueType = item.FindPropertyRelative("ValueType");

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(actionName, new GUIContent("行为名"));
                        GUILayout.Space(8f);
                        EditorGUILayout.PropertyField(valueType, new GUIContent("值类型"), GUILayout.Width(220f));
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("删除", GUILayout.Width(72f)))
                        {
                            actions.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                }
            }

            if (GUILayout.Button("添加行为", GUILayout.Width(100f)))
            {
                actions.InsertArrayElementAtIndex(actions.arraySize);
                SerializedProperty item = actions.GetArrayElementAtIndex(actions.arraySize - 1);
                item.FindPropertyRelative("ActionName").stringValue = "NewAction";
                item.FindPropertyRelative("ValueType").enumValueIndex = (int)CharacterInputActionValueType.Button;
            }
        }

        private void DrawBindingList()
        {
            SerializedProperty buttonBindings = _serializedObject.FindProperty("ButtonBindings");
            string[] actionOptions = BuildActionOptions();

            EditorGUILayout.LabelField("按键绑定", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("PC 只保留常用键鼠键位下拉。手柄使用语义化按钮枚举。Move 固定为 Horizontal/Vertical，Look 固定为 Mouse X/Y。", MessageType.None);

            if (actionOptions.Length == 0)
            {
                EditorGUILayout.HelpBox("请先在行为列表中创建至少一个行为，再添加按键绑定。", MessageType.Warning);
            }

            for (int i = 0; i < buttonBindings.arraySize; i++)
            {
                SerializedProperty item = buttonBindings.GetArrayElementAtIndex(i);
                SerializedProperty actionNameProperty = item.FindPropertyRelative("ActionName");
                SerializedProperty pcKeyProperty = item.FindPropertyRelative("PcKey");
                SerializedProperty gamepadButtonProperty = item.FindPropertyRelative("GamepadButton");

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUI.DisabledScope(actionOptions.Length == 0))
                    {
                        int currentActionIndex = FindActionIndex(actionOptions, actionNameProperty.stringValue);
                        int nextActionIndex = EditorGUILayout.Popup("行为", Mathf.Max(0, currentActionIndex), actionOptions);
                        if (actionOptions.Length > 0 && nextActionIndex >= 0 && nextActionIndex < actionOptions.Length)
                        {
                            actionNameProperty.stringValue = actionOptions[nextActionIndex];
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUILayout.VerticalScope("box"))
                        {
                            EditorGUILayout.LabelField("PC 适配", EditorStyles.boldLabel);
                            DrawPcKeyPopup(pcKeyProperty);
                            EditorGUILayout.HelpBox("这里只显示常用键鼠键位。鼠标请选择 Mouse0 / Mouse1 / Mouse2。", MessageType.None);
                        }

                        using (new EditorGUILayout.VerticalScope("box"))
                        {
                            EditorGUILayout.LabelField("手柄适配", EditorStyles.boldLabel);
                            EditorGUILayout.PropertyField(gamepadButtonProperty, new GUIContent("键位配置"));
                            EditorGUILayout.HelpBox("South/East/West/North 分别对应常见手柄面键的南/东/西/北位。", MessageType.None);
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("删除绑定", GUILayout.Width(90f)))
                        {
                            buttonBindings.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                }
            }

            using (new EditorGUI.DisabledScope(actionOptions.Length == 0))
            {
                if (GUILayout.Button("添加按钮绑定", GUILayout.Width(110f)))
                {
                    buttonBindings.InsertArrayElementAtIndex(buttonBindings.arraySize);
                    SerializedProperty item = buttonBindings.GetArrayElementAtIndex(buttonBindings.arraySize - 1);
                    item.FindPropertyRelative("ActionName").stringValue = actionOptions.Length > 0 ? actionOptions[0] : string.Empty;
                    item.FindPropertyRelative("PcKey").intValue = (int)KeyCode.None;
                    item.FindPropertyRelative("GamepadButton").enumValueIndex = (int)CharacterInputGamepadButton.None;
                }
            }

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("固定轴向规则", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Move: PC 使用 Horizontal/Vertical（默认等价 WASD）；Look: PC 使用 Mouse X/Y。手柄摇杆与移动端虚拟摇杆不在本轮自由配置范围内。", MessageType.None);
        }

        private void LoadOrCreateConfig(bool forceCreate = false)
        {
            if (!forceCreate)
            {
                _config = AssetDatabase.LoadAssetAtPath<CharacterInputMapConfig>(CharacterInputConstants.MainConfigAssetPath);
            }

            if (_config == null && forceCreate)
            {
                string directory = Path.GetDirectoryName(CharacterInputConstants.MainConfigAssetPath);
                if (!string.IsNullOrWhiteSpace(directory) && !AssetDatabase.IsValidFolder(directory))
                {
                    EnsureFolder(directory);
                }

                _config = CreateInstance<CharacterInputMapConfig>();
                AssetDatabase.CreateAsset(_config, CharacterInputConstants.MainConfigAssetPath);
                AssetDatabase.SaveAssets();
                Selection.activeObject = _config;
            }

            if (_config != null)
            {
                RepairCorruptedPcKeys(_config);
                _serializedObject = new SerializedObject(_config);
            }
        }

        private string[] BuildActionOptions()
        {
            SerializedProperty actions = _serializedObject.FindProperty("Actions");
            if (actions == null || actions.arraySize == 0)
            {
                return Array.Empty<string>();
            }

            List<string> results = new List<string>();
            for (int i = 0; i < actions.arraySize; i++)
            {
                SerializedProperty item = actions.GetArrayElementAtIndex(i);
                string actionName = item.FindPropertyRelative("ActionName").stringValue;
                if (!string.IsNullOrWhiteSpace(actionName))
                {
                    results.Add(actionName);
                }
            }

            return results.ToArray();
        }

        private static int FindActionIndex(string[] actionOptions, string actionName)
        {
            if (actionOptions == null || actionOptions.Length == 0)
            {
                return -1;
            }

            for (int i = 0; i < actionOptions.Length; i++)
            {
                if (string.Equals(actionOptions[i], actionName, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return 0;
        }

        private static void DrawPcKeyPopup(SerializedProperty pcKeyProperty)
        {
            KeyCode currentKeyCode = (KeyCode)pcKeyProperty.intValue;
            int optionIndex = FindPcKeyOptionIndex(currentKeyCode);
            int nextIndex = EditorGUILayout.Popup("键位配置", optionIndex, PcKeyLabels);
            if (nextIndex >= 0 && nextIndex < PcKeyOptions.Length)
            {
                pcKeyProperty.intValue = (int)PcKeyOptions[nextIndex];
            }
        }

        private static int FindPcKeyOptionIndex(KeyCode keyCode)
        {
            for (int i = 0; i < PcKeyOptions.Length; i++)
            {
                if (PcKeyOptions[i] == keyCode)
                {
                    return i;
                }
            }

            return 0;
        }

        private static void RepairCorruptedPcKeys(CharacterInputMapConfig config)
        {
            if (config == null || config.ButtonBindings == null)
            {
                return;
            }

            Dictionary<int, int> corruptedKeyMap = GetCorruptedPcKeyMap();
            bool changed = false;

            for (int i = 0; i < config.ButtonBindings.Count; i++)
            {
                CharacterInputButtonBinding binding = config.ButtonBindings[i];
                if (binding == null)
                {
                    continue;
                }

                int rawValue = (int)binding.PcKey;
                if (corruptedKeyMap.TryGetValue(rawValue, out int correctedValue) && rawValue != correctedValue)
                {
                    binding.PcKey = (KeyCode)correctedValue;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
        }

        private static Dictionary<int, int> GetCorruptedPcKeyMap()
        {
            if (s_corruptedPcKeyMap != null)
            {
                return s_corruptedPcKeyMap;
            }

            s_corruptedPcKeyMap = new Dictionary<int, int>();
            KeyCodeProbe probe = CreateInstance<KeyCodeProbe>();

            try
            {
                SerializedObject serializedObject = new SerializedObject(probe);
                SerializedProperty valueProperty = serializedObject.FindProperty("Value");
                if (valueProperty == null)
                {
                    return s_corruptedPcKeyMap;
                }

                for (int i = 0; i < PcKeyOptions.Length; i++)
                {
                    KeyCode intendedKey = PcKeyOptions[i];
                    int intendedValue = (int)intendedKey;

                    valueProperty.enumValueIndex = Mathf.Clamp(intendedValue, 0, valueProperty.enumDisplayNames.Length - 1);
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();

                    int corruptedValue = valueProperty.intValue;
                    if (corruptedValue != intendedValue && !s_corruptedPcKeyMap.ContainsKey(corruptedValue))
                    {
                        s_corruptedPcKeyMap.Add(corruptedValue, intendedValue);
                    }
                }
            }
            finally
            {
                DestroyImmediate(probe);
            }

            return s_corruptedPcKeyMap;
        }

        private static void EnsureFolder(string path)
        {
            string normalizedPath = path.Replace('\\', '/');
            string[] parts = normalizedPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}