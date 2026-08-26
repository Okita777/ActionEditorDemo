using System;
using System.Collections.Generic;
using AsiSkillEditor.RunTime;
using SkillEditor.Preview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SkillEditor.Editor
{
    [CustomEditor(typeof(GameUnit))]
    public sealed class GameUnitEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GameUnit config = (GameUnit)target;

            EditorGUILayout.LabelField("预览挂点配置", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("这里用于编辑器技能预览载体配置。第二版开始，它也承担当前预览 prefab 绑定哪个 UnitResource 的入口。", MessageType.Info);
            EditorGUILayout.Space(6f);

            DrawUnitBinding(config);
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("动画过滤", EditorStyles.boldLabel);
            DefaultAsset currentFolder = string.IsNullOrEmpty(config.AnimationSearchRoot)
                ? null
                : AssetDatabase.LoadAssetAtPath<DefaultAsset>(config.AnimationSearchRoot);
            DefaultAsset nextFolder = EditorGUILayout.ObjectField("搜索目录", currentFolder, typeof(DefaultAsset), false) as DefaultAsset;
            if (nextFolder != currentFolder)
            {
                string nextPath = nextFolder == null ? string.Empty : AssetDatabase.GetAssetPath(nextFolder);
                if (!string.IsNullOrEmpty(nextPath) && !AssetDatabase.IsValidFolder(nextPath))
                {
                    EditorUtility.DisplayDialog("无效目录", "这里需要选择一个文件夹，而不是具体资源。", "确定");
                }
                else
                {
                    config.AnimationSearchRoot = nextPath;
                    EditorUtility.SetDirty(config);
                }
            }

            config.AnimationFilterKey = EditorGUILayout.TextField("过滤关键字", config.AnimationFilterKey);
            if (GUILayout.Button("使用 Prefab 所在目录", GUILayout.Height(24f)))
            {
                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(config.gameObject);
                if (string.IsNullOrEmpty(prefabPath))
                {
                    prefabPath = AssetDatabase.GetAssetPath(config.gameObject);
                }

                string prefabFolder = string.IsNullOrEmpty(prefabPath) ? string.Empty : System.IO.Path.GetDirectoryName(prefabPath)?.Replace("\\", "/");
                config.AnimationSearchRoot = prefabFolder ?? string.Empty;
                EditorUtility.SetDirty(config);
            }

            EditorGUILayout.HelpBox("MetaSkill 的 anim 选择器会优先按这里的搜索目录和过滤关键字筛选动画。", MessageType.Info);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);

            EditorGUILayout.LabelField("角色挂点", EditorStyles.boldLabel);
            for (int i = 0; i < config.MountPoints.Count; i++)
            {
                PreviewMountPoint mountPoint = config.MountPoints[i];
                if (mountPoint == null)
                {
                    mountPoint = new PreviewMountPoint();
                    config.MountPoints[i] = mountPoint;
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                string nextSocketName = EditorGUILayout.TextField("挂点名", mountPoint.SocketName);
                if (!string.Equals(nextSocketName, mountPoint.SocketName, StringComparison.Ordinal))
                {
                    mountPoint.SocketName = nextSocketName;
                    EditorUtility.SetDirty(config);
                }

                if (GUILayout.Button("-", GUILayout.Width(28f)))
                {
                    config.MountPoints.RemoveAt(i);
                    EditorUtility.SetDirty(config);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }

                EditorGUILayout.EndHorizontal();
                mountPoint.MountTransform =
                    EditorGUILayout.ObjectField("Transform", mountPoint.MountTransform, typeof(Transform), true) as Transform;
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("添加角色挂点", GUILayout.Height(28f)))
            {
                config.MountPoints.Add(new PreviewMountPoint());
                EditorUtility.SetDirty(config);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("武器挂载配置", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("这里记录角色对不同武器类型的真实挂载数据。`姿态采样武器` 仅用于你在 prefab 编辑阶段挂到角色武器挂点下，采样本地位置和旋转；技能编辑器里的测试武器仍然在 Preview Unit 窗口单独配置。", MessageType.Info);

            for (int i = 0; i < config.WeaponBindings.Count; i++)
            {
                PreviewWeaponBinding binding = config.WeaponBindings[i];
                if (binding == null)
                {
                    binding = new PreviewWeaponBinding();
                    config.WeaponBindings[i] = binding;
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                binding.DisplayName = EditorGUILayout.TextField("名称", binding.DisplayName);
                if (GUILayout.Button("-", GUILayout.Width(28f)))
                {
                    config.WeaponBindings.RemoveAt(i);
                    EditorUtility.SetDirty(config);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }

                EditorGUILayout.EndHorizontal();
                binding.WeaponType = (SkillWeaponType)EditorGUILayout.EnumPopup("武器类型", binding.WeaponType);
                binding.EquipSocketName = Draw角色挂点字段("装备挂点", config, binding.EquipSocketName);
                binding.WeaponPrefab = EditorGUILayout.ObjectField("姿态采样武器", binding.WeaponPrefab, typeof(GameObject), false) as GameObject;
                Vector3 localPosition = EditorGUILayout.Vector3Field("本地位置", binding.LocalPosition);
                if (localPosition != binding.LocalPosition)
                {
                    binding.LocalPosition = localPosition;
                    EditorUtility.SetDirty(config);
                }

                Vector3 localRotation = EditorGUILayout.Vector3Field("本地旋转", binding.LocalRotation);
                if (localRotation != binding.LocalRotation)
                {
                    binding.LocalRotation = localRotation;
                    EditorUtility.SetDirty(config);
                }

                PreviewWeaponConfig authoringWeaponConfig = binding.WeaponPrefab == null ? null : binding.WeaponPrefab.GetComponent<PreviewWeaponConfig>();
                if (binding.WeaponPrefab == null)
                {
                    EditorGUILayout.HelpBox("如果你要在 prefab 编辑阶段采样姿态，把一个武器 prefab 拖到这里，再把同一个武器实例挂到角色对应挂点下。采样完成后可以把它清空。", MessageType.Info);
                }
                else if (authoringWeaponConfig == null)
                {
                    EditorGUILayout.HelpBox("姿态采样武器缺少 PreviewWeaponConfig，无法为攻击盒/发射器提供武器挂点。", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField("采样武器挂点数", authoringWeaponConfig.MountPoints != null ? authoringWeaponConfig.MountPoints.Count.ToString() : "0");
                }

                GameObject previewWeaponPrefab = SkillPreviewUnitSettings.LoadPreviewWeaponPrefab(binding.WeaponType);
                if (previewWeaponPrefab != null)
                {
                    EditorGUILayout.ObjectField("技能编辑器测试武器", previewWeaponPrefab, typeof(GameObject), false);
                }

                Draw场景姿态按钮(config, i, binding);

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("添加武器挂载", GUILayout.Height(28f)))
            {
                config.WeaponBindings.Add(new PreviewWeaponBinding());
                EditorUtility.SetDirty(config);
            }

            EditorGUILayout.Space(8f);
            PreviewSkillSlotEditorUtility.DrawSkillSlots(config, () => EditorUtility.SetDirty(config));

            if (GUI.changed)
            {
                EditorUtility.SetDirty(config);
            }
        }

        private static void DrawUnitBinding(GameUnit config)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Unit 绑定", EditorStyles.boldLabel);

            List<SkillResourceFileEntry> units = SkillResourceRepository.LoadUnits();
            if (units.Count == 0)
            {
                EditorGUILayout.HelpBox("当前还没有 UnitResource。请先到 Resource Entry 创建 Unit。", MessageType.Info);
                config.UnitId = EditorGUILayout.TextField("UnitId", config.UnitId);
                EditorGUILayout.EndVertical();
                return;
            }

            string[] options = new string[units.Count + 1];
            options[0] = "<未绑定 Unit>";
            int currentIndex = 0;
            for (int i = 0; i < units.Count; i++)
            {
                UnitConfig unitConfig = units[i].Config as UnitConfig;
                string unitId = unitConfig != null ? unitConfig.UnitId : string.Empty;
                string displayName = unitConfig != null && !string.IsNullOrEmpty(unitConfig.DisplayName)
                    ? unitConfig.DisplayName
                    : units[i].BaseName;
                options[i + 1] = string.IsNullOrEmpty(unitId) ? displayName : $"{displayName} ({unitId})";
                if (string.Equals(unitId, config.UnitId, StringComparison.Ordinal))
                {
                    currentIndex = i + 1;
                }
            }

            int nextIndex = EditorGUILayout.Popup("Unit", currentIndex, options);
            string nextUnitId = nextIndex <= 0 ? string.Empty : ((units[nextIndex - 1].Config as UnitConfig)?.UnitId ?? string.Empty);
            if (!string.Equals(nextUnitId, config.UnitId, StringComparison.Ordinal))
            {
                config.UnitId = nextUnitId;
                if (!string.IsNullOrEmpty(nextUnitId))
                {
                    UnitConfig unitConfig = SkillResourceRepository.LoadUnitConfig(nextUnitId);
                    if (unitConfig != null && !string.IsNullOrEmpty(unitConfig.AnimationDirectory))
                    {
                        config.AnimationSearchRoot = unitConfig.AnimationDirectory;
                    }
                }

                EditorUtility.SetDirty(config);
            }

            EditorGUILayout.LabelField("当前 UnitId", string.IsNullOrEmpty(config.UnitId) ? "<未绑定>" : config.UnitId);
            EditorGUILayout.EndVertical();
        }

        private static string Draw角色挂点字段(string label, GameUnit config, string currentValue)
        {
            if (config == null || config.MountPoints == null || config.MountPoints.Count == 0)
            {
                return EditorGUILayout.TextField(label, currentValue);
            }

            string[] options = new string[config.MountPoints.Count + 1];
            options[0] = "根节点";
            for (int i = 0; i < config.MountPoints.Count; i++)
            {
                string socketName = config.MountPoints[i] != null ? config.MountPoints[i].SocketName : string.Empty;
                options[i + 1] = string.IsNullOrEmpty(socketName) ? $"挂点 {i + 1}" : socketName;
            }

            int currentIndex = 0;
            for (int i = 1; i < options.Length; i++)
            {
                if (string.Equals(options[i], currentValue))
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = EditorGUILayout.Popup(label, currentIndex, options);
            return nextIndex <= 0 ? string.Empty : options[nextIndex];
        }

        private static void Draw场景姿态按钮(GameUnit config, int bindingIndex, PreviewWeaponBinding binding)
        {
            Transform currentSceneWeapon = TryFindAuthoringWeaponTransform(config, binding, out Transform equipSocket)
                ? FindAuthoredWeaponUnderSocket(binding, equipSocket)
                : null;

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(currentSceneWeapon == null))
            {
                if (GUILayout.Button("从场景回填姿态", GUILayout.Height(22f)))
                {
                    Undo.RecordObject(config, "Capture Weapon Binding Pose");
                    binding.LocalPosition = currentSceneWeapon.localPosition;
                    binding.LocalRotation = currentSceneWeapon.localEulerAngles;
                    EditorUtility.SetDirty(config);
                    if (config.gameObject.scene.IsValid())
                    {
                        EditorSceneManager.MarkSceneDirty(config.gameObject.scene);
                    }
                }

                if (GUILayout.Button("应用到场景", GUILayout.Height(22f)))
                {
                    Undo.RecordObject(currentSceneWeapon, "Apply Weapon Binding Pose");
                    currentSceneWeapon.localPosition = binding.LocalPosition;
                    currentSceneWeapon.localRotation = Quaternion.Euler(binding.LocalRotation);
                    if (currentSceneWeapon.gameObject.scene.IsValid())
                    {
                        EditorSceneManager.MarkSceneDirty(currentSceneWeapon.gameObject.scene);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(!IsActivePreviewConfig(config)))
            {
                if (GUILayout.Button("重建场景预览", GUILayout.Height(22f)))
                {
                    GameObject activePrefab = SkillPreviewUnitSettings.LoadActivePrefab();
                    if (activePrefab != null)
                    {
                        SkillPreviewSceneInstanceUtility.CreateOrReplace(activePrefab);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            if (currentSceneWeapon == null)
            {
                EditorGUILayout.HelpBox("当前 prefab 编辑场景里没有在目标装备挂点下找到采样武器。做法是：打开角色 prefab，把 `姿态采样武器` 对应的武器实例拖到该挂点下面，摆好后再点“从场景回填姿态”。", MessageType.Info);
            }
        }

        private static bool TryFindAuthoringWeaponTransform(GameUnit config, PreviewWeaponBinding binding, out Transform weaponTransform)
        {
            weaponTransform = null;
            if (config == null || binding == null)
            {
                return false;
            }

            if (!TryResolveEquipSocket(config, binding.EquipSocketName, out Transform equipSocket))
            {
                return false;
            }

            weaponTransform = FindAuthoredWeaponUnderSocket(binding, equipSocket);
            return weaponTransform != null;
        }

        private static bool TryResolveEquipSocket(GameUnit config, string socketName, out Transform equipSocket)
        {
            equipSocket = null;
            if (config == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(socketName))
            {
                equipSocket = config.transform;
                return true;
            }

            if (config.MountPoints != null)
            {
                for (int i = 0; i < config.MountPoints.Count; i++)
                {
                    PreviewMountPoint mountPoint = config.MountPoints[i];
                    if (mountPoint == null || mountPoint.MountTransform == null)
                    {
                        continue;
                    }

                    if (string.Equals(mountPoint.SocketName, socketName, System.StringComparison.Ordinal))
                    {
                        equipSocket = mountPoint.MountTransform;
                        return true;
                    }
                }
            }

            return false;
        }

        private static Transform FindAuthoredWeaponUnderSocket(PreviewWeaponBinding binding, Transform equipSocket)
        {
            if (equipSocket == null)
            {
                return null;
            }

            string weaponPrefabPath = binding != null && binding.WeaponPrefab != null
                ? AssetDatabase.GetAssetPath(binding.WeaponPrefab)
                : string.Empty;

            return FindMatchingWeaponRecursive(equipSocket, weaponPrefabPath);
        }

        private static Transform FindMatchingWeaponRecursive(Transform root, string weaponPrefabPath)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(weaponPrefabPath))
                {
                    GameObject prefabInstanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(child.gameObject);
                    string childPrefabPath = prefabInstanceRoot == null
                        ? string.Empty
                        : PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabInstanceRoot);
                    if (string.Equals(childPrefabPath, weaponPrefabPath, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return prefabInstanceRoot != null ? prefabInstanceRoot.transform : child;
                    }
                }

                PreviewWeaponConfig weaponConfig = child.GetComponent<PreviewWeaponConfig>();
                if (weaponConfig != null)
                {
                    return child;
                }

                Transform nestedMatch = FindMatchingWeaponRecursive(child, weaponPrefabPath);
                if (nestedMatch != null)
                {
                    return nestedMatch;
                }
            }

            return null;
        }

        private static bool IsActivePreviewConfig(GameUnit config)
        {
            if (config == null)
            {
                return false;
            }

            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(config.gameObject);
            if (string.IsNullOrEmpty(prefabPath))
            {
                prefabPath = AssetDatabase.GetAssetPath(config.gameObject);
            }

            return string.Equals(prefabPath, SkillPreviewUnitSettings.ActivePrefabPath, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}

namespace SkillEditor.Editor
{
    internal static class PreviewSkillSlotEditorUtility
    {
        public static void DrawSkillSlots(GameUnit config, Action markDirty)
        {
            if (config == null)
            {
                return;
            }

            DrawActiveSkillSlots(config, markDirty);
            EditorGUILayout.Space(8f);
            DrawPassiveSkillSlots(config, markDirty);
        }

        private static void DrawActiveSkillSlots(GameUnit config, Action markDirty)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("主动技能槽位", EditorStyles.boldLabel);
            config.ActiveSkillSlots ??= new List<PreviewActiveSkillSlotConfig>();

            List<SkillResourceFileEntry> allSkillEntries = SkillResourceRepository.LoadSkills(config.UnitId);
            List<SkillResourceFileEntry> skillEntries = FilterSkillEntriesByCategory(allSkillEntries, SkillCastCategory.Active, config.ActiveSkillSlots);
            for (int i = 0; i < config.ActiveSkillSlots.Count; i++)
            {
                PreviewActiveSkillSlotConfig slot = config.ActiveSkillSlots[i] ?? new PreviewActiveSkillSlotConfig();
                config.ActiveSkillSlots[i] = slot;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                slot.DisplayName = EditorGUILayout.TextField("名称", slot.DisplayName);
                if (GUILayout.Button("-", GUILayout.Width(28f)))
                {
                    config.ActiveSkillSlots.RemoveAt(i);
                    markDirty?.Invoke();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                int slotIndex = EditorGUILayout.IntField("槽位编号", slot.SlotIndex);
                if (slotIndex != slot.SlotIndex)
                {
                    slot.SlotIndex = Mathf.Max(1, slotIndex);
                    markDirty?.Invoke();
                }

                string actionName = InputActionEditorUtility.DrawActionPopup("输入动作", slot.ActionName);
                if (!string.Equals(actionName, slot.ActionName, StringComparison.Ordinal))
                {
                    slot.ActionName = actionName;
                    markDirty?.Invoke();
                }

                string nextSkillAssetName = DrawSkillPopup("技能", skillEntries, slot.SkillAssetName);
                if (!string.Equals(nextSkillAssetName, slot.SkillAssetName, StringComparison.Ordinal))
                {
                    slot.SkillAssetName = nextSkillAssetName;
                    markDirty?.Invoke();
                }

                DrawSkillSummary(allSkillEntries, slot.SkillAssetName, SkillCastCategory.Active);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("添加主动技能槽", GUILayout.Height(28f)))
            {
                config.ActiveSkillSlots.Add(new PreviewActiveSkillSlotConfig
                {
                    SlotIndex = config.ActiveSkillSlots.Count + 1,
                    DisplayName = $"主动技能槽{config.ActiveSkillSlots.Count + 1}",
                });
                markDirty?.Invoke();
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawPassiveSkillSlots(GameUnit config, Action markDirty)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("被动技能槽位", EditorStyles.boldLabel);
            config.PassiveSkillSlots ??= new List<PreviewPassiveSkillSlotConfig>();

            List<SkillResourceFileEntry> allSkillEntries = SkillResourceRepository.LoadSkills(config.UnitId);
            List<SkillResourceFileEntry> skillEntries = FilterSkillEntriesByCategory(allSkillEntries, SkillCastCategory.Passive, config.PassiveSkillSlots);
            for (int i = 0; i < config.PassiveSkillSlots.Count; i++)
            {
                PreviewPassiveSkillSlotConfig slot = config.PassiveSkillSlots[i] ?? new PreviewPassiveSkillSlotConfig();
                config.PassiveSkillSlots[i] = slot;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                slot.DisplayName = EditorGUILayout.TextField("名称", slot.DisplayName);
                if (GUILayout.Button("-", GUILayout.Width(28f)))
                {
                    config.PassiveSkillSlots.RemoveAt(i);
                    markDirty?.Invoke();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                int slotIndex = EditorGUILayout.IntField("槽位编号", slot.SlotIndex);
                if (slotIndex != slot.SlotIndex)
                {
                    slot.SlotIndex = Mathf.Max(1, slotIndex);
                    markDirty?.Invoke();
                }

                string nextSkillAssetName = DrawSkillPopup("技能", skillEntries, slot.SkillAssetName);
                if (!string.Equals(nextSkillAssetName, slot.SkillAssetName, StringComparison.Ordinal))
                {
                    slot.SkillAssetName = nextSkillAssetName;
                    markDirty?.Invoke();
                }

                DrawSkillSummary(allSkillEntries, slot.SkillAssetName, SkillCastCategory.Passive);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("添加被动技能槽", GUILayout.Height(28f)))
            {
                config.PassiveSkillSlots.Add(new PreviewPassiveSkillSlotConfig
                {
                    SlotIndex = config.PassiveSkillSlots.Count + 1,
                    DisplayName = $"被动技能槽{config.PassiveSkillSlots.Count + 1}",
                });
                markDirty?.Invoke();
            }

            EditorGUILayout.EndVertical();
        }

        private static string DrawSkillPopup(string label, List<SkillResourceFileEntry> skillEntries, string currentAssetName)
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
                options[i + 1] = string.IsNullOrEmpty(skillConfig?.SkillId)
                    ? displayName
                    : $"{displayName} ({skillConfig.SkillId})";
                if (SkillResourceRepository.IsMatchingSkillReference(entry, currentAssetName))
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

        private static void DrawSkillSummary(List<SkillResourceFileEntry> skillEntries, string skillAssetName, SkillCastCategory expectedCategory)
        {
            if (string.IsNullOrEmpty(skillAssetName))
            {
                EditorGUILayout.HelpBox("当前槽位还没有装配 Skill。", MessageType.Info);
                return;
            }

            SkillConfig skillConfig = FindSkillConfig(skillEntries, skillAssetName);
            if (skillConfig == null)
            {
                EditorGUILayout.HelpBox($"未找到 Skill 资源: {skillAssetName}", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("skillId", skillConfig.SkillId, EditorStyles.miniLabel);
            EditorGUILayout.LabelField("skillType", skillConfig.SkillCategory.ToString(), EditorStyles.miniLabel);
            if (skillConfig.SkillCategory != expectedCategory)
            {
                EditorGUILayout.HelpBox($"该 Skill 当前被标记为 {skillConfig.SkillCategory}，与当前槽位期望的 {expectedCategory} 不一致。", MessageType.Warning);
            }
        }

        private static SkillConfig FindSkillConfig(List<SkillResourceFileEntry> skillEntries, string skillAssetName)
        {
            if (skillEntries == null || string.IsNullOrEmpty(skillAssetName))
            {
                return null;
            }

            for (int i = 0; i < skillEntries.Count; i++)
            {
                SkillResourceFileEntry entry = skillEntries[i];
                if (entry != null && SkillResourceRepository.IsMatchingSkillReference(entry, skillAssetName))
                {
                    return entry.Config as SkillConfig;
                }
            }

            return null;
        }

        private static List<SkillResourceFileEntry> FilterSkillEntriesByCategory<TSlot>(List<SkillResourceFileEntry> allSkillEntries, SkillCastCategory expectedCategory, List<TSlot> slots)
        {
            List<SkillResourceFileEntry> results = new List<SkillResourceFileEntry>();
            if (allSkillEntries == null)
            {
                return results;
            }

            HashSet<string> equippedAssets = new HashSet<string>(StringComparer.Ordinal);
            if (slots != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    switch (slots[i])
                    {
                        case PreviewActiveSkillSlotConfig activeSlot when !string.IsNullOrEmpty(activeSlot.SkillAssetName):
                            equippedAssets.Add(activeSlot.SkillAssetName);
                            break;
                        case PreviewPassiveSkillSlotConfig passiveSlot when !string.IsNullOrEmpty(passiveSlot.SkillAssetName):
                            equippedAssets.Add(passiveSlot.SkillAssetName);
                            break;
                    }
                }
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
    }
}
