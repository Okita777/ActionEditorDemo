using System.Collections.Generic;
using SkillEditor.Preview;
using AsiSkillEditor.RunTime;
using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    public sealed class SkillPreviewUnitInspectorWindow : EditorWindow
    {
        private GameObject _candidatePrefab;

        [MenuItem("Tools/SkillEditor/Preview Unit")]
        public static void Open()
        {
            System.Type windowType = System.Type.GetType("SkillEditor.Editor.SkillEditorInspectorWindow, Assembly-CSharp-Editor");
            if (windowType == null)
            {
                Debug.LogError("SkillEditorInspectorWindow 未能加载。");
                return;
            }

            System.Reflection.MethodInfo openMethod = windowType.GetMethod("OpenPreviewUnit", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (openMethod == null)
            {
                Debug.LogError("SkillEditorInspectorWindow.OpenPreviewUnit 未找到。");
                return;
            }

            openMethod.Invoke(null, null);
        }

        private void OnEnable()
        {
            _candidatePrefab = SkillPreviewUnitSettings.LoadActivePrefab();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("预览单位", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("这里只负责编辑器预览载体，不参与技能正式数据开发。Apply 之后，会在当前 Scene 创建一个解包后的预览复制体，后续 StateTimeline 预览会以它为载体。", MessageType.Info);

            _candidatePrefab = EditorGUILayout.ObjectField("预览 Prefab", _candidatePrefab, typeof(GameObject), false) as GameObject;

            EditorGUILayout.Space(8f);
            DrawValidation();
            EditorGUILayout.Space(8f);
            DrawPreviewWeapons();
            EditorGUILayout.Space(8f);
            DrawActiveSelection();
            EditorGUILayout.Space(8f);
            DrawActions();
        }

        private void DrawValidation()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("校验结果", EditorStyles.boldLabel);

            if (_candidatePrefab == null)
            {
                EditorGUILayout.HelpBox("请先拖入一个用于预览技能的 prefab。", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            bool hasAnimator = TryGetAnimator(_candidatePrefab, out Animator animator);
            bool hasPreviewConfig = TryGetPreviewConfig(_candidatePrefab, out GameUnit previewConfig);
            bool hasMountPoints = hasPreviewConfig && previewConfig.MountPoints.Count > 0;
            bool hasMissingMountTransform = false;
            bool hasInvalidPreviewWeapon = false;

            if (hasPreviewConfig)
            {
                for (int i = 0; i < previewConfig.MountPoints.Count; i++)
                {
                    if (previewConfig.MountPoints[i].MountTransform == null)
                    {
                        hasMissingMountTransform = true;
                        break;
                    }
                }

                hasInvalidPreviewWeapon = HasInvalidPreviewWeaponSelection(previewConfig);
            }

            DrawCheckLine("Animator", hasAnimator, hasAnimator ? animator.name : "缺少 Animator");
            DrawCheckLine("GameUnit", hasPreviewConfig, hasPreviewConfig ? "已挂载" : "缺少 GameUnit 组件");
            DrawCheckLine("挂点", hasMountPoints && !hasMissingMountTransform, hasPreviewConfig
                ? (hasMissingMountTransform ? "存在空挂点引用" : previewConfig.MountPoints.Count + " 个挂点")
                : "未配置挂点");
            DrawCheckLine("预览武器", !hasInvalidPreviewWeapon, hasPreviewConfig
                ? (hasInvalidPreviewWeapon ? "存在无效预览武器配置" : "已通过或未配置")
                : "未配置预览单位");

            if (!hasAnimator || !hasPreviewConfig || !hasMountPoints || hasMissingMountTransform || hasInvalidPreviewWeapon)
            {
                EditorGUILayout.HelpBox("当前 prefab 还不能作为技能预览载体。至少需要 Animator、GameUnit、完整的角色挂点；如果配置了预览武器，则武器 prefab 和装备挂点也必须有效。", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox("当前 prefab 可以作为技能预览载体。", MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("定位 Prefab"))
            {
                Selection.activeObject = _candidatePrefab;
                EditorGUIUtility.PingObject(_candidatePrefab);
            }

            GUI.enabled = previewConfig != null;
            if (GUILayout.Button("打开挂点配置"))
            {
                Selection.activeObject = previewConfig;
                EditorGUIUtility.PingObject(previewConfig);
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewWeapons()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("预览武器", EditorStyles.boldLabel);

            if (!TryGetPreviewConfig(_candidatePrefab, out GameUnit previewConfig) ||
                previewConfig == null ||
                previewConfig.WeaponBindings == null ||
                previewConfig.WeaponBindings.Count == 0)
            {
                EditorGUILayout.HelpBox("当前角色还没有武器挂载配置。请先在角色 prefab 的 `GameUnit` 中添加武器挂载。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            HashSet<SkillWeaponType> drawnTypes = new HashSet<SkillWeaponType>();
            for (int i = 0; i < previewConfig.WeaponBindings.Count; i++)
            {
                PreviewWeaponBinding binding = previewConfig.WeaponBindings[i];
                if (binding == null || !drawnTypes.Add(binding.WeaponType))
                {
                    continue;
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(binding.WeaponType.ToString(), EditorStyles.boldLabel);
                GameObject currentPreviewWeapon = SkillPreviewUnitSettings.LoadPreviewWeaponPrefab(binding.WeaponType);
                GameObject nextPreviewWeapon = EditorGUILayout.ObjectField("预览武器", currentPreviewWeapon, typeof(GameObject), false) as GameObject;
                if (nextPreviewWeapon != currentPreviewWeapon)
                {
                    SkillPreviewUnitSettings.SetPreviewWeaponPrefab(binding.WeaponType, nextPreviewWeapon);
                    SkillPreviewUnitSettings.Save();
                }

                string currentBindingName = SkillPreviewUnitSettings.LoadPreviewWeaponBindingName(binding.WeaponType);
                string nextBindingName = DrawWeaponBindingField(previewConfig, binding.WeaponType, currentBindingName, "武器挂载");
                if (nextBindingName != currentBindingName)
                {
                    SkillPreviewUnitSettings.SetPreviewWeaponBindingName(binding.WeaponType, nextBindingName);
                    SkillPreviewUnitSettings.Save();
                }

                PreviewWeaponConfig weaponConfig = nextPreviewWeapon == null ? null : nextPreviewWeapon.GetComponent<PreviewWeaponConfig>();
                if (nextPreviewWeapon == null)
                {
                    EditorGUILayout.HelpBox("未配置该武器类型的预览武器。Apply 时会跳过这个类型。", MessageType.Info);
                }
                else if (string.IsNullOrEmpty(nextBindingName))
                {
                    EditorGUILayout.HelpBox("请为该测试武器选择一个武器挂载。", MessageType.Warning);
                }
                else if (weaponConfig == null)
                {
                    EditorGUILayout.HelpBox("该 prefab 缺少 PreviewWeaponConfig。", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField("武器挂点数", weaponConfig.MountPoints != null ? weaponConfig.MountPoints.Count.ToString() : "0");
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActiveSelection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("当前生效载体", EditorStyles.boldLabel);

            GameObject activePrefab = SkillPreviewUnitSettings.LoadActivePrefab();
            if (activePrefab == null)
            {
                EditorGUILayout.LabelField("未设置");
            }
            else
            {
                EditorGUILayout.ObjectField("Active Prefab", activePrefab, typeof(GameObject), false);
                EditorGUILayout.LabelField("路径", SkillPreviewUnitSettings.ActivePrefabPath);
            }

            GameObject sceneInstance = SkillPreviewSceneInstanceUtility.GetCurrentInstance();
            GameObject primaryWeaponInstance = SkillPreviewSceneInstanceUtility.GetCurrentPrimaryWeaponInstance();
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Scene 预览实例", EditorStyles.boldLabel);
            if (sceneInstance == null)
            {
                EditorGUILayout.LabelField("未创建");
            }
            else
            {
                EditorGUILayout.ObjectField("Instance", sceneInstance, typeof(GameObject), true);
                EditorGUILayout.LabelField("状态", PrefabUtility.IsPartOfPrefabInstance(sceneInstance) ? "Prefab Instance" : "已解包 Scene 对象");
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("主预览武器", EditorStyles.boldLabel);
            if (primaryWeaponInstance == null)
            {
                EditorGUILayout.LabelField("未挂载");
            }
            else
            {
                EditorGUILayout.ObjectField("Weapon", primaryWeaponInstance, typeof(GameObject), true);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.HelpBox("预览激活入口已经移动到 Resource Entry 顶部的 Active Unit 后面。这里保留校验和测试武器配置，但不再负责 Apply/Clear。", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private static void DrawCheckLine(string label, bool passed, string detail)
        {
            Color previousColor = GUI.color;
            GUI.color = passed ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.85f, 0.35f, 0.35f);
            EditorGUILayout.LabelField(label, passed ? "OK" : "Missing");
            GUI.color = previousColor;
            EditorGUILayout.LabelField("说明", detail);
        }

        private static bool IsCandidateValid(GameObject prefab)
        {
            if (!TryGetAnimator(prefab, out _))
            {
                return false;
            }

            if (!TryGetPreviewConfig(prefab, out GameUnit previewConfig))
            {
                return false;
            }

            if (previewConfig.MountPoints.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < previewConfig.MountPoints.Count; i++)
            {
                if (previewConfig.MountPoints[i].MountTransform == null)
                {
                    return false;
                }
            }

            return !HasInvalidPreviewWeaponSelection(previewConfig);
        }

        private static bool TryGetAnimator(GameObject prefab, out Animator animator)
        {
            animator = prefab == null ? null : prefab.GetComponentInChildren<Animator>(true);
            return animator != null;
        }

        private static bool TryGetPreviewConfig(GameObject prefab, out GameUnit previewConfig)
        {
            previewConfig = prefab == null ? null : prefab.GetComponent<GameUnit>();
            return previewConfig != null;
        }

        private static bool HasInvalidPreviewWeaponSelection(GameUnit previewConfig)
        {
            if (previewConfig == null || previewConfig.WeaponBindings == null)
            {
                return false;
            }

            System.Collections.Generic.HashSet<SkillWeaponType> checkedTypes = new System.Collections.Generic.HashSet<SkillWeaponType>();
            for (int i = 0; i < previewConfig.WeaponBindings.Count; i++)
            {
                PreviewWeaponBinding binding = previewConfig.WeaponBindings[i];
                if (binding == null || !checkedTypes.Add(binding.WeaponType))
                {
                    continue;
                }

                GameObject previewWeaponPrefab = SkillPreviewUnitSettings.LoadPreviewWeaponPrefab(binding.WeaponType);
                if (previewWeaponPrefab == null)
                {
                    continue;
                }

                string bindingName = SkillPreviewUnitSettings.LoadPreviewWeaponBindingName(binding.WeaponType);
                if (string.IsNullOrEmpty(bindingName))
                {
                    return true;
                }

                if (!TryGetWeaponBinding(previewConfig, binding.WeaponType, bindingName, out PreviewWeaponBinding selectedBinding))
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(selectedBinding.EquipSocketName) &&
                    !HasMountPoint(previewConfig.MountPoints, selectedBinding.EquipSocketName))
                {
                    return true;
                }

                PreviewWeaponConfig weaponConfig = previewWeaponPrefab.GetComponent<PreviewWeaponConfig>();
                if (weaponConfig == null)
                {
                    return true;
                }

                if (weaponConfig.MountPoints == null)
                {
                    return true;
                }

                for (int mountIndex = 0; mountIndex < weaponConfig.MountPoints.Count; mountIndex++)
                {
                    if (weaponConfig.MountPoints[mountIndex] != null && weaponConfig.MountPoints[mountIndex].MountTransform == null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string DrawWeaponBindingField(GameUnit previewConfig, SkillWeaponType weaponType, string currentValue, string label)
        {
            List<string> options = CollectWeaponBindingNames(previewConfig, weaponType);
            if (options.Count == 0)
            {
                EditorGUILayout.LabelField(label, "无可用挂载");
                return string.Empty;
            }

            string[] popupOptions = new string[options.Count + 1];
            popupOptions[0] = "未指定";
            for (int i = 0; i < options.Count; i++)
            {
                popupOptions[i + 1] = options[i];
            }

            int currentIndex = 0;
            for (int i = 1; i < popupOptions.Length; i++)
            {
                if (string.Equals(popupOptions[i], currentValue, System.StringComparison.Ordinal))
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = EditorGUILayout.Popup(label, currentIndex, popupOptions);
            return nextIndex <= 0 ? string.Empty : popupOptions[nextIndex];
        }

        private static List<string> CollectWeaponBindingNames(GameUnit previewConfig, SkillWeaponType weaponType)
        {
            List<string> results = new List<string>();
            if (previewConfig == null || previewConfig.WeaponBindings == null)
            {
                return results;
            }

            for (int i = 0; i < previewConfig.WeaponBindings.Count; i++)
            {
                PreviewWeaponBinding binding = previewConfig.WeaponBindings[i];
                if (binding == null || binding.WeaponType != weaponType)
                {
                    continue;
                }

                string displayName = string.IsNullOrEmpty(binding.DisplayName) ? $"武器挂载 {i + 1}" : binding.DisplayName;
                if (!results.Contains(displayName))
                {
                    results.Add(displayName);
                }
            }

            return results;
        }

        private static bool TryGetWeaponBinding(GameUnit previewConfig, SkillWeaponType weaponType, string bindingName, out PreviewWeaponBinding binding)
        {
            binding = null;
            if (previewConfig == null || previewConfig.WeaponBindings == null || string.IsNullOrEmpty(bindingName))
            {
                return false;
            }

            for (int i = 0; i < previewConfig.WeaponBindings.Count; i++)
            {
                PreviewWeaponBinding current = previewConfig.WeaponBindings[i];
                if (current == null || current.WeaponType != weaponType)
                {
                    continue;
                }

                string displayName = string.IsNullOrEmpty(current.DisplayName) ? $"武器挂载 {i + 1}" : current.DisplayName;
                if (string.Equals(displayName, bindingName, System.StringComparison.Ordinal))
                {
                    binding = current;
                    return true;
                }
            }

            return false;
        }

        private static bool HasMountPoint(System.Collections.Generic.IList<PreviewMountPoint> mountPoints, string socketName)
        {
            if (mountPoints == null || string.IsNullOrEmpty(socketName))
            {
                return false;
            }

            for (int i = 0; i < mountPoints.Count; i++)
            {
                PreviewMountPoint mountPoint = mountPoints[i];
                if (mountPoint == null || mountPoint.MountTransform == null)
                {
                    continue;
                }

                if (string.Equals(mountPoint.SocketName, socketName, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
