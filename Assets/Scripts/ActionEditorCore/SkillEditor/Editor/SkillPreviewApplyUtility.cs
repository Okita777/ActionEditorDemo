using System.Collections.Generic;
using AsiSkillEditor.RunTime;
using SkillEditor.Preview;
using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    internal static class SkillPreviewApplyUtility
    {
        public static bool TryApplyUnit(UnitConfig unitConfig, out string errorMessage)
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

            if (!TryGetAnimator(prefab, out _))
            {
                errorMessage = "当前 prefab 缺少 Animator。";
                return false;
            }

            if (!TryGetPreviewConfig(prefab, out GameUnit previewConfig) || previewConfig == null)
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

            if (HasInvalidPreviewWeaponSelection(previewConfig))
            {
                errorMessage = "当前 prefab 的测试武器配置无效，请先修正武器挂载。";
                return false;
            }

            if (!SkillPreviewSceneInstanceUtility.ValidateCameraConfiguration(unitConfig, prefab, out errorMessage))
            {
                return false;
            }

            ApplyUnitResourceToPreview(previewConfig, unitConfig);
            EditorUtility.SetDirty(previewConfig);
            AssetDatabase.SaveAssets();

            SkillPreviewUnitSettings.ActiveUnitId = unitConfig.UnitId;
            SkillPreviewUnitSettings.ActivePrefabPath = unitConfig.PrefabAssetPath;
            SkillPreviewUnitSettings.Save();
            SkillPreviewSceneInstanceUtility.CreateOrReplace(prefab, unitConfig.CameraResourcePath);
            return true;
        }

        public static void ClearPreviewCarrier()
        {
            SkillPreviewSceneInstanceUtility.RemoveCurrentInstances();
            SkillPreviewUnitSettings.ClearActivePreviewCarrier();
        }

        private static void ApplyUnitResourceToPreview(GameUnit previewConfig, UnitConfig unitConfig)
        {
            previewConfig.UnitId = unitConfig.UnitId;
            if (!string.IsNullOrEmpty(unitConfig.AnimationDirectory))
            {
                previewConfig.AnimationSearchRoot = unitConfig.AnimationDirectory;
            }

            previewConfig.ActiveSkillSlots = new List<PreviewActiveSkillSlotConfig>();
            if (unitConfig.ActiveSkillSlots != null)
            {
                for (int i = 0; i < unitConfig.ActiveSkillSlots.Count; i++)
                {
                    UnitActiveSkillSlotConfig slot = unitConfig.ActiveSkillSlots[i];
                    if (slot == null)
                    {
                        continue;
                    }

                    previewConfig.ActiveSkillSlots.Add(new PreviewActiveSkillSlotConfig
                    {
                        SlotIndex = slot.SlotIndex,
                        DisplayName = slot.DisplayName,
                        ActionName = slot.ActionName,
                        SkillAssetName = slot.SkillId,
                    });
                }
            }

            previewConfig.PassiveSkillSlots = new List<PreviewPassiveSkillSlotConfig>();
            if (unitConfig.PassiveSkillSlots != null)
            {
                for (int i = 0; i < unitConfig.PassiveSkillSlots.Count; i++)
                {
                    UnitPassiveSkillSlotConfig slot = unitConfig.PassiveSkillSlots[i];
                    if (slot == null)
                    {
                        continue;
                    }

                    previewConfig.PassiveSkillSlots.Add(new PreviewPassiveSkillSlotConfig
                    {
                        SlotIndex = slot.SlotIndex,
                        DisplayName = slot.DisplayName,
                        SkillAssetName = slot.SkillId,
                    });
                }
            }
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

            HashSet<SkillWeaponType> checkedTypes = new HashSet<SkillWeaponType>();
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

                if (!string.IsNullOrEmpty(selectedBinding.EquipSocketName) && !HasMountPoint(previewConfig.MountPoints, selectedBinding.EquipSocketName))
                {
                    return true;
                }

                PreviewWeaponConfig weaponConfig = previewWeaponPrefab.GetComponent<PreviewWeaponConfig>();
                if (weaponConfig == null || weaponConfig.MountPoints == null)
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

        private static bool HasMountPoint(IList<PreviewMountPoint> mountPoints, string socketName)
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