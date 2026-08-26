using System;
using System.IO;
using System.Text;
using AsiSkillEditor.RunTime;
using SkillEditor.Preview;
using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    [Serializable]
    internal sealed class SkillPreviewWeaponSettingsData
    {
        public SkillWeaponType WeaponType = SkillWeaponType.OneHandSword;
        public string WeaponPrefabPath = string.Empty;
        public string WeaponBindingName = string.Empty;
    }

    [Serializable]
    internal sealed class SkillPreviewUnitSettingsData
    {
        public string ActivePrefabPath = string.Empty;
        public string ActiveUnitId = string.Empty;
        public SkillPreviewWeaponSettingsData[] PreviewWeapons = Array.Empty<SkillPreviewWeaponSettingsData>();
    }

    internal static class SkillPreviewUnitSettings
    {
        private static SkillPreviewUnitSettingsData _cachedData;

        public static string ActivePrefabPath
        {
            get => Load().ActivePrefabPath;
            set
            {
                SkillPreviewUnitSettingsData data = Load();
                data.ActivePrefabPath = value ?? string.Empty;
            }
        }

        public static GameObject LoadActivePrefab()
        {
            string assetPath = ActivePrefabPath;
            return string.IsNullOrEmpty(assetPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }

        public static string ActiveUnitId
        {
            get => Load().ActiveUnitId ?? string.Empty;
            set
            {
                SkillPreviewUnitSettingsData data = Load();
                data.ActiveUnitId = value ?? string.Empty;
            }
        }

        public static GameUnit LoadActivePreviewConfig()
        {
            GameObject prefab = LoadActivePrefab();
            return prefab == null ? null : prefab.GetComponent<GameUnit>();
        }

        public static SkillPreviewWeaponSettingsData[] LoadPreviewWeapons()
        {
            SkillPreviewWeaponSettingsData[] previewWeapons = Load().PreviewWeapons;
            return previewWeapons ?? Array.Empty<SkillPreviewWeaponSettingsData>();
        }

        public static GameObject LoadPreviewWeaponPrefab(SkillWeaponType weaponType)
        {
            SkillPreviewWeaponSettingsData entry = GetPreviewWeaponSetting(weaponType);
            return entry == null || string.IsNullOrEmpty(entry.WeaponPrefabPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(entry.WeaponPrefabPath);
        }

        public static void SetPreviewWeaponPrefab(SkillWeaponType weaponType, GameObject prefab)
        {
            SkillPreviewWeaponSettingsData entry = GetOrCreatePreviewWeaponSetting(weaponType);
            string prefabPath = prefab == null ? string.Empty : AssetDatabase.GetAssetPath(prefab);
            entry.WeaponPrefabPath = prefabPath;
        }

        public static string LoadPreviewWeaponBindingName(SkillWeaponType weaponType)
        {
            SkillPreviewWeaponSettingsData entry = GetPreviewWeaponSetting(weaponType);
            return entry != null ? entry.WeaponBindingName ?? string.Empty : string.Empty;
        }

        public static void SetPreviewWeaponBindingName(SkillWeaponType weaponType, string bindingName)
        {
            SkillPreviewWeaponSettingsData entry = GetOrCreatePreviewWeaponSetting(weaponType);
            entry.WeaponBindingName = bindingName ?? string.Empty;
        }

        public static SkillPreviewWeaponSettingsData GetPreviewWeaponSetting(SkillWeaponType weaponType)
        {
            SkillPreviewWeaponSettingsData[] previewWeapons = LoadPreviewWeapons();
            for (int i = 0; i < previewWeapons.Length; i++)
            {
                SkillPreviewWeaponSettingsData current = previewWeapons[i];
                if (current == null || current.WeaponType != weaponType)
                {
                    continue;
                }

                return current;
            }

            return null;
        }

        private static SkillPreviewWeaponSettingsData GetOrCreatePreviewWeaponSetting(SkillWeaponType weaponType)
        {
            SkillPreviewUnitSettingsData data = Load();
            SkillPreviewWeaponSettingsData[] previewWeapons = data.PreviewWeapons ?? Array.Empty<SkillPreviewWeaponSettingsData>();
            for (int i = 0; i < previewWeapons.Length; i++)
            {
                SkillPreviewWeaponSettingsData entry = previewWeapons[i];
                if (entry == null || entry.WeaponType != weaponType)
                {
                    continue;
                }

                return entry;
            }

            Array.Resize(ref previewWeapons, previewWeapons.Length + 1);
            SkillPreviewWeaponSettingsData created = new SkillPreviewWeaponSettingsData
            {
                WeaponType = weaponType,
            };
            previewWeapons[previewWeapons.Length - 1] = created;
            data.PreviewWeapons = previewWeapons;
            return created;
        }

        public static void Save()
        {
            SkillResourceRepository.EnsureFolders();
            string json = JsonUtility.ToJson(Load(), true);
            File.WriteAllText(ToAbsolutePath(SkillEditorResourcePaths.PreviewUnitSettingsFile), json, Encoding.UTF8);
            AssetDatabase.Refresh();
        }

        public static void Clear()
        {
            ActivePrefabPath = string.Empty;
            ActiveUnitId = string.Empty;
            Load().PreviewWeapons = Array.Empty<SkillPreviewWeaponSettingsData>();
            Save();
        }

        public static void ClearActivePreviewCarrier()
        {
            Load().ActivePrefabPath = string.Empty;
            Save();
        }

        private static SkillPreviewUnitSettingsData Load()
        {
            if (_cachedData != null)
            {
                return _cachedData;
            }

            string absolutePath = ToAbsolutePath(SkillEditorResourcePaths.PreviewUnitSettingsFile);
            if (!File.Exists(absolutePath))
            {
                _cachedData = new SkillPreviewUnitSettingsData();
                return _cachedData;
            }

            string json = File.ReadAllText(absolutePath, Encoding.UTF8);
            _cachedData = new SkillPreviewUnitSettingsData();
            if (!string.IsNullOrWhiteSpace(json))
            {
                JsonUtility.FromJsonOverwrite(json, _cachedData);
            }

            return _cachedData;
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }
    }
}
