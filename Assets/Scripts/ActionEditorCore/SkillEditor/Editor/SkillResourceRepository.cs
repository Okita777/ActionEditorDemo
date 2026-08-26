using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using AsiSkillEditor.RunTime;
using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    internal sealed class SkillResourceFileEntry
    {
        public SkillResourceType ResourceType;
        public string UnitId = string.Empty;
        public string BaseName = string.Empty;
        public string JsonAssetPath = string.Empty;
        public string ByteAssetPath = string.Empty;
        public object Config;
    }

    internal sealed class SkillResourceFolderSource
    {
        public string FolderAssetPath = string.Empty;
        public string UnitId = string.Empty;
        public string SearchPattern = "*.json";
    }

    internal static class SkillResourceRepository
    {
        private static readonly Dictionary<string, SkillResourceFileEntry> DirtyEntries = new Dictionary<string, SkillResourceFileEntry>(StringComparer.OrdinalIgnoreCase);

        public static List<SkillResourceFileEntry> LoadUnits()
        {
            return LoadEntries(
                SkillResourceType.Unit,
                GetUnitFolderSources(),
                SkillEditorResourcePaths.CompiledUnitFolder,
                () => new UnitConfig(),
                config => string.IsNullOrEmpty(config.DisplayName) ? config.UnitId : config.DisplayName);
        }

        public static List<SkillResourceFileEntry> LoadSkills(string unitId = "")
        {
            return LoadEntries(
                SkillResourceType.Skill,
                GetSkillFolderSources(unitId),
                SkillEditorResourcePaths.CompiledSkillFolder,
                () => new SkillConfig(),
                config => string.IsNullOrEmpty(config.SkillName) ? config.SkillId : config.SkillName);
        }

        public static List<SkillResourceFileEntry> LoadMetaSkills(string unitId = "")
        {
            return LoadEntries(
                SkillResourceType.MetaSkill,
                GetMetaSkillFolderSources(unitId),
                SkillEditorResourcePaths.CompiledMetaSkillFolder,
                () => new MetaSkillConfig(),
                config => string.IsNullOrEmpty(config.MetaSkillName) ? config.MetaSkillId : config.MetaSkillName);
        }

        public static List<SkillResourceFileEntry> LoadStates(string unitId = "")
        {
            return LoadEntries(
                SkillResourceType.State,
                GetStateFolderSources(unitId),
                SkillEditorResourcePaths.CompiledStateFolder,
                () => new AsiSkillEditor.RunTime.StateConfig(),
                config => string.IsNullOrEmpty(config.StateName) ? config.StateId : config.StateName);
        }

        public static List<SkillResourceFileEntry> LoadBuffs()
        {
            return LoadEntries(
                SkillResourceType.Buff,
                new[]
                {
                    new SkillResourceFolderSource
                    {
                        FolderAssetPath = SkillEditorResourcePaths.BuffFolder,
                    }
                },
                SkillEditorResourcePaths.CompiledBuffFolder,
                () => new BuffConfig(),
                config => string.IsNullOrEmpty(config.BuffName) ? config.BuffId : config.BuffName);
        }

        public static SkillResourceFileEntry Create(SkillResourceType resourceType, string unitId = "")
        {
            EnsureFolders();

            switch (resourceType)
            {
                case SkillResourceType.Unit:
                    return CreateUnitEntry();
                case SkillResourceType.Skill:
                    return CreateScopedEntry(resourceType, unitId, "Skills", SkillEditorResourcePaths.CompiledSkillFolder, "NewSkill", new SkillConfig());
                case SkillResourceType.MetaSkill:
                    return CreateScopedEntry(resourceType, unitId, "MetaSkills", SkillEditorResourcePaths.CompiledMetaSkillFolder, "NewMetaSkill", new MetaSkillConfig());
                case SkillResourceType.State:
                    return CreateScopedEntry(resourceType, unitId, "States", SkillEditorResourcePaths.CompiledStateFolder, "NewState", new AsiSkillEditor.RunTime.StateConfig());
                case SkillResourceType.Buff:
                    return CreateEntry(resourceType, SkillEditorResourcePaths.BuffFolder, SkillEditorResourcePaths.CompiledBuffFolder, "NewBuff", new BuffConfig());
                default:
                    throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, null);
            }
        }

        public static SkillResourceFileEntry Duplicate(SkillResourceFileEntry source)
        {
            if (source == null || source.Config == null)
            {
                return null;
            }

            EnsureFolders();
            switch (source.ResourceType)
            {
                case SkillResourceType.Unit:
                    UnitConfig sourceUnitConfig = DeepCloneEditorConfig((UnitConfig)source.Config, () => new UnitConfig());
                    sourceUnitConfig.UnitId = GenerateUniqueUnitId(sourceUnitConfig.UnitId + "_Copy");
                    if (!string.IsNullOrEmpty(sourceUnitConfig.DisplayName))
                    {
                        sourceUnitConfig.DisplayName += " Copy";
                    }

                    return CreateUnitEntry(source.BaseName + "_Copy", sourceUnitConfig);
                case SkillResourceType.Skill:
                    return CreateScopedEntry(source.ResourceType, source.UnitId, "Skills", SkillEditorResourcePaths.CompiledSkillFolder, source.BaseName + "_Copy", DeepCloneEditorConfig((SkillConfig)source.Config, () => new SkillConfig()));
                case SkillResourceType.MetaSkill:
                    return CreateScopedEntry(source.ResourceType, source.UnitId, "MetaSkills", SkillEditorResourcePaths.CompiledMetaSkillFolder, source.BaseName + "_Copy", DeepCloneEditorConfig((MetaSkillConfig)source.Config, () => new MetaSkillConfig()));
                case SkillResourceType.State:
                    return CreateScopedEntry(source.ResourceType, source.UnitId, "States", SkillEditorResourcePaths.CompiledStateFolder, source.BaseName + "_Copy", DeepCloneEditorConfig((AsiSkillEditor.RunTime.StateConfig)source.Config, () => new AsiSkillEditor.RunTime.StateConfig()));
                case SkillResourceType.Buff:
                    return CreateEntry(source.ResourceType, SkillEditorResourcePaths.BuffFolder, SkillEditorResourcePaths.CompiledBuffFolder, source.BaseName + "_Copy", DeepCloneEditorConfig((BuffConfig)source.Config, () => new BuffConfig()));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void Save(SkillResourceFileEntry entry)
        {
            if (entry == null || entry.Config == null)
            {
                return;
            }

            EnsureFolders();
            WriteEntryFiles(entry);
            ClearDirty(entry);
            RebuildAnimationCatalog();
            AssetDatabase.Refresh();
        }

        public static void Delete(SkillResourceFileEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (entry.ResourceType == SkillResourceType.Unit)
            {
                DeleteUnitFolder(entry);
                return;
            }

            ClearDirty(entry);
            DeleteIfExists(entry.JsonAssetPath);
            DeleteIfExists(entry.ByteAssetPath);
            DeleteIfExists(GetLegacyByteAssetPath(entry.ByteAssetPath));
            RebuildAnimationCatalog();
            AssetDatabase.Refresh();
        }

        public static string GetDisplayName(SkillResourceFileEntry entry)
        {
            if (entry == null || entry.Config == null)
            {
                return string.Empty;
            }

            switch (entry.ResourceType)
            {
                case SkillResourceType.Unit:
                    UnitConfig unitConfig = (UnitConfig)entry.Config;
                    return string.IsNullOrEmpty(unitConfig.DisplayName) ? unitConfig.UnitId : unitConfig.DisplayName;
                case SkillResourceType.Skill:
                    SkillConfig skillConfig = (SkillConfig)entry.Config;
                    return string.IsNullOrEmpty(skillConfig.SkillName) ? entry.BaseName : skillConfig.SkillName;
                case SkillResourceType.MetaSkill:
                    MetaSkillConfig metaSkillConfig = (MetaSkillConfig)entry.Config;
                    return string.IsNullOrEmpty(metaSkillConfig.MetaSkillName) ? entry.BaseName : metaSkillConfig.MetaSkillName;
                case SkillResourceType.State:
                    AsiSkillEditor.RunTime.StateConfig stateConfig = (AsiSkillEditor.RunTime.StateConfig)entry.Config;
                    return string.IsNullOrEmpty(stateConfig.StateName) ? entry.BaseName : stateConfig.StateName;
                case SkillResourceType.Buff:
                    BuffConfig buffConfig = (BuffConfig)entry.Config;
                    return string.IsNullOrEmpty(buffConfig.BuffName) ? entry.BaseName : buffConfig.BuffName;
                default:
                    return entry.BaseName;
            }
        }

        public static void EnsureFolders()
        {
            EnsureFolder(SkillEditorResourcePaths.DataRoot);
            EnsureFolder(SkillEditorResourcePaths.CompiledRoot);
            EnsureFolder(SkillEditorResourcePaths.ResourcesRoot);
            EnsureFolder(SkillEditorResourcePaths.SkillEditorResourcesRoot);
            EnsureFolder(SkillEditorResourcePaths.EditorSettingsRoot);
            EnsureFolder(SkillEditorResourcePaths.UnitFolder);
            EnsureFolder(SkillEditorResourcePaths.BuffFolder);
            EnsureFolder(SkillEditorResourcePaths.CompiledUnitFolder);
            EnsureFolder(SkillEditorResourcePaths.CompiledBuffFolder);
            DeleteDirectoryIfExists(SkillEditorResourcePaths.CompiledSkillFolder);
            DeleteDirectoryIfExists(SkillEditorResourcePaths.CompiledMetaSkillFolder);
            DeleteDirectoryIfExists(SkillEditorResourcePaths.CompiledStateFolder);
            DeleteDirectoryIfExists(SkillEditorResourcePaths.CompiledRoot + "/PreviewUnits");
        }

        public static UnitConfig LoadUnitConfig(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
            {
                return null;
            }

            List<SkillResourceFileEntry> units = LoadUnits();
            for (int i = 0; i < units.Count; i++)
            {
                UnitConfig config = units[i].Config as UnitConfig;
                if (config != null && string.Equals(config.UnitId, unitId, StringComparison.Ordinal))
                {
                    return config;
                }
            }

            return null;
        }

        public static void MarkDirty(SkillResourceFileEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.JsonAssetPath))
            {
                return;
            }

            DirtyEntries[entry.JsonAssetPath] = entry;
        }

        public static void ClearDirty(SkillResourceFileEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.JsonAssetPath))
            {
                return;
            }

            DirtyEntries.Remove(entry.JsonAssetPath);
        }

        public static bool IsDirty(SkillResourceFileEntry entry)
        {
            return entry != null &&
                   !string.IsNullOrEmpty(entry.JsonAssetPath) &&
                   DirtyEntries.ContainsKey(entry.JsonAssetPath);
        }

        public static bool HasDirtyEntries()
        {
            return DirtyEntries.Count > 0;
        }

        public static List<SkillResourceFileEntry> GetDirtyEntries()
        {
            return new List<SkillResourceFileEntry>(DirtyEntries.Values);
        }

        internal static int SaveDirtyEntries()
        {
            List<SkillResourceFileEntry> dirtyEntries = GetDirtyEntries();
            for (int i = 0; i < dirtyEntries.Count; i++)
            {
                Save(dirtyEntries[i]);
            }

            return dirtyEntries.Count;
        }

        private static List<SkillResourceFileEntry> LoadEntries<TConfig>(
            SkillResourceType resourceType,
            IEnumerable<SkillResourceFolderSource> sourceFolders,
            string byteFolderAssetPath,
            Func<TConfig> createDefaultConfig,
            Func<TConfig, string> getDisplayName) where TConfig : class
        {
            EnsureFolders();

            List<SkillResourceFileEntry> entries = new List<SkillResourceFileEntry>();
            if (sourceFolders == null)
            {
                return entries;
            }

            foreach (SkillResourceFolderSource sourceFolder in sourceFolders)
            {
                if (sourceFolder == null || string.IsNullOrEmpty(sourceFolder.FolderAssetPath))
                {
                    continue;
                }

                string jsonFolder = ToAbsolutePath(sourceFolder.FolderAssetPath);
                if (!Directory.Exists(jsonFolder))
                {
                    continue;
                }

                string[] jsonFiles = Directory.GetFiles(jsonFolder, sourceFolder.SearchPattern, SearchOption.TopDirectoryOnly);
                for (int i = 0; i < jsonFiles.Length; i++)
                {
                    string absoluteJsonPath = jsonFiles[i];
                    string baseName = Path.GetFileNameWithoutExtension(absoluteJsonPath);
                    string jsonAssetPath = ToAssetPath(absoluteJsonPath);
                    SkillResourceFileEntry entry;
                    if (DirtyEntries.TryGetValue(jsonAssetPath, out SkillResourceFileEntry dirtyEntry) &&
                        dirtyEntry != null &&
                        dirtyEntry.Config is TConfig)
                    {
                        dirtyEntry.ResourceType = resourceType;
                        dirtyEntry.UnitId = sourceFolder.UnitId ?? string.Empty;
                        dirtyEntry.BaseName = baseName;
                        dirtyEntry.JsonAssetPath = jsonAssetPath;
                        dirtyEntry.ByteAssetPath = ResolveByteAssetPath(resourceType, byteFolderAssetPath, baseName, dirtyEntry.Config, sourceFolder.UnitId ?? string.Empty);
                        entry = dirtyEntry;
                    }
                    else
                    {
                        string json = File.ReadAllText(absoluteJsonPath, Encoding.UTF8);
                        TConfig config = DeserializeEditorConfig(json, createDefaultConfig);
                        entry = new SkillResourceFileEntry
                        {
                            ResourceType = resourceType,
                            UnitId = sourceFolder.UnitId ?? string.Empty,
                            BaseName = baseName,
                            JsonAssetPath = jsonAssetPath,
                            ByteAssetPath = ResolveByteAssetPath(resourceType, byteFolderAssetPath, baseName, config, sourceFolder.UnitId ?? string.Empty),
                            Config = config,
                        };
                    }

                    EnsureRuntimeByte(entry);
                    entries.Add(entry);
                }
            }

            entries.Sort((left, right) => string.CompareOrdinal(getDisplayName((TConfig)left.Config), getDisplayName((TConfig)right.Config)));
            return entries;
        }

        private static SkillResourceFileEntry CreateUnitEntry()
        {
            string folderName = GenerateUniqueUnitFolderName("NewUnit");
            UnitConfig config = new UnitConfig
            {
                UnitId = folderName,
                DisplayName = folderName,
            };
            return CreateUnitEntry(folderName, config);
        }

        private static SkillResourceFileEntry CreateUnitEntry(string folderName, UnitConfig config)
        {
            string unitFolderAssetPath = Path.Combine(SkillEditorResourcePaths.UnitFolder, folderName).Replace("\\", "/");
            EnsureFolder(unitFolderAssetPath);

            SkillResourceFileEntry entry = new SkillResourceFileEntry
            {
                ResourceType = SkillResourceType.Unit,
                UnitId = config != null ? config.UnitId : string.Empty,
                BaseName = folderName,
                JsonAssetPath = unitFolderAssetPath + "/Unit.json",
                ByteAssetPath = ResolveByteAssetPath(SkillResourceType.Unit, SkillEditorResourcePaths.CompiledUnitFolder, folderName, config, config != null ? config.UnitId : string.Empty),
                Config = config ?? new UnitConfig(),
            };

            WriteEntryFiles(entry);
            AssetDatabase.Refresh();
            return entry;
        }

        private static SkillResourceFileEntry CreateScopedEntry<TConfig>(SkillResourceType resourceType, string unitId, string childFolderName, string byteFolderAssetPath, string desiredBaseName, TConfig config)
        {
            if (string.IsNullOrEmpty(unitId))
            {
                throw new InvalidOperationException($"{resourceType} requires a current UnitResource.");
            }

            if (!TryGetUnitFolderAssetPath(unitId, out string unitFolderAssetPath))
            {
                throw new InvalidOperationException($"UnitResource '{unitId}' does not exist.");
            }

            string jsonFolderAssetPath = Path.Combine(unitFolderAssetPath, childFolderName).Replace("\\", "/");
            EnsureFolder(jsonFolderAssetPath);
            return CreateEntry(resourceType, jsonFolderAssetPath, byteFolderAssetPath, desiredBaseName, config, unitId);
        }

        private static SkillResourceFileEntry CreateEntry<TConfig>(SkillResourceType resourceType, string jsonFolderAssetPath, string byteFolderAssetPath, string desiredBaseName, TConfig config, string unitId = "")
        {
            string baseName = GenerateUniqueBaseName(jsonFolderAssetPath, desiredBaseName);
            string jsonAssetPath = Path.Combine(jsonFolderAssetPath, baseName + ".json").Replace("\\", "/");

            SkillResourceFileEntry entry = new SkillResourceFileEntry
            {
                ResourceType = resourceType,
                UnitId = unitId ?? string.Empty,
                BaseName = baseName,
                JsonAssetPath = jsonAssetPath,
                ByteAssetPath = ResolveByteAssetPath(resourceType, byteFolderAssetPath, baseName, config, unitId),
                Config = config,
            };

            WriteEntryFiles(entry);
            AssetDatabase.Refresh();
            return entry;
        }

        private static void WriteEntryFiles(SkillResourceFileEntry entry)
        {
            string previousByteAssetPath = entry.ByteAssetPath;
            if (entry.ResourceType == SkillResourceType.Unit && entry.Config is UnitConfig unitConfig)
            {
                entry.UnitId = unitConfig.UnitId ?? string.Empty;
            }

            entry.ByteAssetPath = ResolveByteAssetPath(entry);
            WriteEditorJson(entry.JsonAssetPath, entry.Config);
            WriteRuntimeBinary(entry.ByteAssetPath, BuildRuntimeConfig(entry));
            DeleteIfExists(GetLegacyByteAssetPath(entry.ByteAssetPath));
            if (!string.IsNullOrEmpty(previousByteAssetPath) &&
                !string.Equals(previousByteAssetPath, entry.ByteAssetPath, StringComparison.OrdinalIgnoreCase))
            {
                DeleteIfExists(previousByteAssetPath);
                DeleteIfExists(GetLegacyByteAssetPath(previousByteAssetPath));
            }
        }

        internal static void RebuildAnimationCatalog()
        {
            EnsureFolders();

            ScriptableObject catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SkillEditorResourcePaths.RuntimeAnimationCatalogAssetPath);
            if (catalog == null)
            {
                Type catalogType = ResolveType("AsiSkillEditor.RunTime.SkillAnimationCatalog");
                if (catalogType == null || !typeof(ScriptableObject).IsAssignableFrom(catalogType))
                {
                    return;
                }

                catalog = ScriptableObject.CreateInstance(catalogType);
                AssetDatabase.CreateAsset(catalog, SkillEditorResourcePaths.RuntimeAnimationCatalogAssetPath);
            }

            SerializedObject serializedCatalog = new SerializedObject(catalog);
            SerializedProperty entriesProperty = serializedCatalog.FindProperty("Entries");
            if (entriesProperty == null || !entriesProperty.isArray)
            {
                return;
            }

            entriesProperty.ClearArray();
            HashSet<string> seenKeys = new HashSet<string>(StringComparer.Ordinal);
            string[] jsonFiles = EnumerateMetaSkillJsonFiles();
            for (int i = 0; i < jsonFiles.Length; i++)
            {
                string json = File.ReadAllText(jsonFiles[i], Encoding.UTF8);
                MetaSkillConfig config = DeserializeEditorConfig(json, () => new MetaSkillConfig());
                if (config == null)
                {
                    continue;
                }

                TryAddAnimationCatalogEntry(entriesProperty, seenKeys, config.GetExecuteAnimationClipPath());
                TryAddAnimationCatalogEntry(entriesProperty, seenKeys, config.GetRecoveryAnimationClipPath());
                AddStateAnimationEntries(entriesProperty, seenKeys, config.SkillStateTimeLineState);
                AddStateAnimationEntries(entriesProperty, seenKeys, config.RecoverySkillStateTimeLineState);
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static void TryAddAnimationCatalogEntry(SerializedProperty entriesProperty, HashSet<string> seenKeys, string animationKey)
        {
            if (entriesProperty == null || seenKeys == null || string.IsNullOrEmpty(animationKey) || !seenKeys.Add(animationKey))
            {
                return;
            }

            AnimationClip clip = SkillAnimationReferenceUtility.LoadClip(animationKey);
            if (clip == null)
            {
                return;
            }

            int newIndex = entriesProperty.arraySize;
            entriesProperty.InsertArrayElementAtIndex(newIndex);
            SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(newIndex);
            SerializedProperty keyProperty = entryProperty.FindPropertyRelative("Key");
            SerializedProperty clipProperty = entryProperty.FindPropertyRelative("Clip");
            if (keyProperty == null || clipProperty == null)
            {
                entriesProperty.DeleteArrayElementAtIndex(newIndex);
                return;
            }

            keyProperty.stringValue = animationKey;
            clipProperty.objectReferenceValue = clip;
        }

        private static void AddStateAnimationEntries(SerializedProperty entriesProperty, HashSet<string> seenKeys, StateConfig state)
        {
            if (state == null)
            {
                return;
            }

            if (state.AnimationMode == StateAnimationMode.DirectionalMixer2D)
            {
                StateDirectionalMixer2DConfig directional = state.DirectionalMixer2D;
                if (directional == null)
                {
                    return;
                }

                TryAddAnimationCatalogEntry(entriesProperty, seenKeys, directional.IdleClipPath);
                TryAddAnimationCatalogEntry(entriesProperty, seenKeys, directional.ForwardClipPath);
                TryAddAnimationCatalogEntry(entriesProperty, seenKeys, directional.ForwardRightClipPath);
                TryAddAnimationCatalogEntry(entriesProperty, seenKeys, directional.RightClipPath);
                TryAddAnimationCatalogEntry(entriesProperty, seenKeys, directional.BackRightClipPath);
                TryAddAnimationCatalogEntry(entriesProperty, seenKeys, directional.BackClipPath);
                TryAddAnimationCatalogEntry(entriesProperty, seenKeys, directional.BackLeftClipPath);
                TryAddAnimationCatalogEntry(entriesProperty, seenKeys, directional.LeftClipPath);
                TryAddAnimationCatalogEntry(entriesProperty, seenKeys, directional.ForwardLeftClipPath);
                return;
            }

            TryAddAnimationCatalogEntry(entriesProperty, seenKeys, state.AnimationClipPath);
        }

        internal static void RebuildBulletCatalog()
        {
            EnsureFolders();

            ScriptableObject catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SkillEditorResourcePaths.RuntimeBulletCatalogAssetPath);
            if (catalog == null)
            {
                Type catalogType = ResolveType("AsiSkillEditor.RunTime.SkillBulletCatalog");
                if (catalogType == null || !typeof(ScriptableObject).IsAssignableFrom(catalogType))
                {
                    return;
                }

                catalog = ScriptableObject.CreateInstance(catalogType);
                AssetDatabase.CreateAsset(catalog, SkillEditorResourcePaths.RuntimeBulletCatalogAssetPath);
            }

            SerializedObject serializedCatalog = new SerializedObject(catalog);
            SerializedProperty entriesProperty = serializedCatalog.FindProperty("Entries");
            if (entriesProperty == null || !entriesProperty.isArray)
            {
                return;
            }

            entriesProperty.ClearArray();
            HashSet<string> seenKeys = new HashSet<string>(StringComparer.Ordinal);
            string[] jsonFiles = EnumerateMetaSkillJsonFiles();
            for (int i = 0; i < jsonFiles.Length; i++)
            {
                string json = File.ReadAllText(jsonFiles[i], Encoding.UTF8);
                MetaSkillConfig config = DeserializeEditorConfig(json, () => new MetaSkillConfig());
                List<TimelineTrackConfig> tracks = GetMetaSkillTracks(config);
                if (tracks == null || tracks.Count == 0)
                {
                    continue;
                }

                for (int trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
                {
                    TimelineTrackConfig track = tracks[trackIndex];
                    if (track == null || track.Bullets == null)
                    {
                        continue;
                    }

                    for (int bulletIndex = 0; bulletIndex < track.Bullets.Count; bulletIndex++)
                    {
                        BulletConfig bullet = track.Bullets[bulletIndex];
                        string prefabPath = bullet != null && bullet.SpawnArgs != null ? bullet.SpawnArgs.BulletPrefabPath : string.Empty;
                        if (string.IsNullOrEmpty(prefabPath) || !seenKeys.Add(prefabPath))
                        {
                            continue;
                        }

                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (prefab == null)
                        {
                            continue;
                        }

                        int newIndex = entriesProperty.arraySize;
                        entriesProperty.InsertArrayElementAtIndex(newIndex);
                        SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(newIndex);
                        SerializedProperty keyProperty = entryProperty.FindPropertyRelative("Key");
                        SerializedProperty prefabProperty = entryProperty.FindPropertyRelative("Prefab");
                        if (keyProperty == null || prefabProperty == null)
                        {
                            entriesProperty.DeleteArrayElementAtIndex(newIndex);
                            continue;
                        }

                        keyProperty.stringValue = prefabPath;
                        prefabProperty.objectReferenceValue = prefab;
                    }
                }
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static List<TimelineTrackConfig> GetMetaSkillTracks(MetaSkillConfig config)
        {
            if (config == null)
            {
                return null;
            }

            List<TimelineTrackConfig> tracks = new List<TimelineTrackConfig>();
            AppendStateTimelineTracks(tracks, config.SkillStateTimeLineState);
            AppendStateTimelineTracks(tracks, config.RecoverySkillStateTimeLineState);
            return tracks;
        }

        private static void AppendStateTimelineTracks(List<TimelineTrackConfig> tracks, StateConfig stateConfig)
        {
            if (tracks == null || stateConfig == null || stateConfig.Timeline == null || stateConfig.Timeline.Tracks == null)
            {
                return;
            }

            for (int index = 0; index < stateConfig.Timeline.Tracks.Count; index++)
            {
                tracks.Add(stateConfig.Timeline.Tracks[index]);
            }
        }

        private static Type ResolveType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                return null;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static void EnsureRuntimeByte(SkillResourceFileEntry entry)
        {
            string previousByteAssetPath = entry.ByteAssetPath;
            entry.ByteAssetPath = ResolveByteAssetPath(entry);
            if (!string.IsNullOrEmpty(previousByteAssetPath) &&
                !string.Equals(previousByteAssetPath, entry.ByteAssetPath, StringComparison.OrdinalIgnoreCase))
            {
                DeleteIfExists(previousByteAssetPath);
                DeleteIfExists(GetLegacyByteAssetPath(previousByteAssetPath));
            }

            string byteAbsolutePath = ToAbsolutePath(entry.ByteAssetPath);
            string legacyByteAbsolutePath = ToAbsolutePath(GetLegacyByteAssetPath(entry.ByteAssetPath));
            object runtimeConfig = BuildRuntimeConfig(entry);
            if (File.Exists(byteAbsolutePath) &&
                !File.Exists(legacyByteAbsolutePath) &&
                CanDeserializeRuntimeByte(byteAbsolutePath, runtimeConfig.GetType()))
            {
                return;
            }

            WriteRuntimeBinary(entry.ByteAssetPath, runtimeConfig);
            DeleteIfExists(GetLegacyByteAssetPath(entry.ByteAssetPath));
        }

        private static string ResolveByteAssetPath(SkillResourceFileEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            string byteFolderAssetPath = GetCompiledFolder(entry.ResourceType);
            return ResolveByteAssetPath(entry.ResourceType, byteFolderAssetPath, entry.BaseName, entry.Config, entry.UnitId);
        }

        private static string ResolveByteAssetPath(SkillResourceType resourceType, string byteFolderAssetPath, string baseName, object config, string unitId)
        {
            string fileName = baseName;
            if (resourceType == SkillResourceType.Unit && config is UnitConfig unitConfig && !string.IsNullOrEmpty(unitConfig.UnitId))
            {
                fileName = unitConfig.UnitId;
            }
            else if (resourceType == SkillResourceType.Skill && config is SkillConfig skillConfig && !string.IsNullOrEmpty(skillConfig.SkillId))
            {
                fileName = BuildScopedRuntimeId(unitId, skillConfig.SkillId);
            }
            else if (resourceType == SkillResourceType.MetaSkill && config is MetaSkillConfig metaSkillConfig && !string.IsNullOrEmpty(metaSkillConfig.MetaSkillId))
            {
                fileName = BuildScopedRuntimeId(unitId, metaSkillConfig.MetaSkillId);
            }
            else if (resourceType == SkillResourceType.State && config is AsiSkillEditor.RunTime.StateConfig stateConfig && !string.IsNullOrEmpty(stateConfig.StateId))
            {
                fileName = BuildScopedRuntimeId(unitId, stateConfig.StateId);
            }
            else if (resourceType == SkillResourceType.Buff && config is BuffConfig buffConfig && !string.IsNullOrEmpty(buffConfig.BuffId))
            {
                fileName = buffConfig.BuffId;
            }

            string resolvedFolder = ResolveCompiledFolder(resourceType, byteFolderAssetPath, unitId);
            EnsureFolder(resolvedFolder);
            return Path.Combine(resolvedFolder, fileName + SkillEditorResourcePaths.RuntimeBinarySuffix).Replace("\\", "/");
        }

        private static string GetCompiledFolder(SkillResourceType resourceType)
        {
            switch (resourceType)
            {
                case SkillResourceType.Unit:
                    return SkillEditorResourcePaths.CompiledUnitFolder;
                case SkillResourceType.Skill:
                    return SkillEditorResourcePaths.CompiledSkillFolder;
                case SkillResourceType.MetaSkill:
                    return SkillEditorResourcePaths.CompiledMetaSkillFolder;
                case SkillResourceType.State:
                    return SkillEditorResourcePaths.CompiledStateFolder;
                case SkillResourceType.Buff:
                    return SkillEditorResourcePaths.CompiledBuffFolder;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, null);
            }
        }

        private static string ResolveCompiledFolder(SkillResourceType resourceType, string byteFolderAssetPath, string unitId)
        {
            if (resourceType == SkillResourceType.Skill || resourceType == SkillResourceType.MetaSkill || resourceType == SkillResourceType.State)
            {
                if (!string.IsNullOrEmpty(unitId))
                {
                    string childFolder = resourceType == SkillResourceType.Skill
                        ? "Skills"
                        : resourceType == SkillResourceType.MetaSkill
                            ? "MetaSkills"
                            : "States";
                    return Path.Combine(SkillEditorResourcePaths.CompiledUnitFolder, unitId, childFolder).Replace("\\", "/");
                }
            }
            else if (resourceType == SkillResourceType.Unit && !string.IsNullOrEmpty(unitId))
            {
                return Path.Combine(SkillEditorResourcePaths.CompiledUnitFolder, unitId).Replace("\\", "/");
            }

            return byteFolderAssetPath;
        }

        private static void WriteEditorJson(string jsonAssetPath, object config)
        {
            string json = SerializeEditorConfig(config);
            File.WriteAllText(ToAbsolutePath(jsonAssetPath), json, Encoding.UTF8);
        }

        private static object BuildRuntimeConfig(SkillResourceFileEntry entry)
        {
            switch (entry.ResourceType)
            {
                case SkillResourceType.Unit:
                    return BuildRuntimeUnitConfig(entry.UnitId, (UnitConfig)entry.Config);
                case SkillResourceType.Skill:
                    return BuildRuntimeSkillConfig(entry.UnitId, (SkillConfig)entry.Config);
                case SkillResourceType.MetaSkill:
                    return (MetaSkillConfig)entry.Config;
                case SkillResourceType.State:
                    return (AsiSkillEditor.RunTime.StateConfig)entry.Config;
                case SkillResourceType.Buff:
                    return (BuffConfig)entry.Config;
                default:
                    throw new ArgumentOutOfRangeException(nameof(entry.ResourceType), entry.ResourceType, null);
            }
        }

        private static string SerializeEditorConfig(object config)
        {
            return JsonUtility.ToJson(config, true);
        }

        private static TConfig DeserializeEditorConfig<TConfig>(string json, Func<TConfig> createDefaultConfig) where TConfig : class
        {
            TConfig config = createDefaultConfig();
            if (string.IsNullOrWhiteSpace(json))
            {
                return config;
            }

            JsonUtility.FromJsonOverwrite(json, config);
            return config;
        }

        private static TConfig DeepCloneEditorConfig<TConfig>(TConfig source, Func<TConfig> createDefaultConfig) where TConfig : class
        {
            if (source == null)
            {
                return createDefaultConfig();
            }

            string json = SerializeEditorConfig(source);
            return DeserializeEditorConfig(json, createDefaultConfig);
        }

        private static void WriteRuntimeBinary(string byteAssetPath, object runtimeConfig)
        {
            string absolutePath = ToAbsolutePath(byteAssetPath);
            string temporaryPath = absolutePath + ".tmp";
#pragma warning disable SYSLIB0011
            try
            {
                using (FileStream fileStream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    binaryFormatter.Serialize(fileStream, runtimeConfig);
                }

                File.Copy(temporaryPath, absolutePath, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
#pragma warning restore SYSLIB0011
        }

        private static bool CanDeserializeRuntimeByte(string absolutePath, Type expectedType)
        {
            if (string.IsNullOrEmpty(absolutePath) || expectedType == null || !File.Exists(absolutePath))
            {
                return false;
            }

#pragma warning disable SYSLIB0011
            try
            {
                using (FileStream fileStream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    object config = binaryFormatter.Deserialize(fileStream);
                    return config != null && expectedType.IsInstanceOfType(config);
                }
            }
            catch
            {
                return false;
            }
#pragma warning restore SYSLIB0011
        }

        private static string GetLegacyByteAssetPath(string byteAssetPath)
        {
            if (string.IsNullOrEmpty(byteAssetPath) || !byteAssetPath.EndsWith(SkillEditorResourcePaths.RuntimeBinarySuffix, StringComparison.OrdinalIgnoreCase))
            {
                return byteAssetPath;
            }

            return byteAssetPath.Substring(0, byteAssetPath.Length - SkillEditorResourcePaths.RuntimeBinarySuffix.Length) + ".bytes";
        }

        private static string GenerateUniqueBaseName(string jsonFolderAssetPath, string desiredBaseName)
        {
            string folder = ToAbsolutePath(jsonFolderAssetPath);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string baseName = desiredBaseName;
            int index = 1;
            while (File.Exists(Path.Combine(folder, baseName + ".json")))
            {
                baseName = desiredBaseName + "_" + index;
                index++;
            }

            return baseName;
        }

        private static string GenerateUniqueUnitFolderName(string desiredBaseName)
        {
            string unitsRoot = ToAbsolutePath(SkillEditorResourcePaths.UnitFolder);
            if (!Directory.Exists(unitsRoot))
            {
                Directory.CreateDirectory(unitsRoot);
            }

            string baseName = desiredBaseName;
            int index = 1;
            while (Directory.Exists(Path.Combine(unitsRoot, baseName)))
            {
                baseName = desiredBaseName + "_" + index;
                index++;
            }

            return baseName;
        }

        private static void EnsureFolder(string assetPath)
        {
            string absolutePath = ToAbsolutePath(assetPath);
            if (!Directory.Exists(absolutePath))
            {
                Directory.CreateDirectory(absolutePath);
            }
        }

        private static void DeleteIfExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            string absolutePath = ToAbsolutePath(assetPath);
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            string metaPath = absolutePath + ".meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
        }

        private static void DeleteDirectoryIfExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            string absolutePath = ToAbsolutePath(assetPath);
            if (Directory.Exists(absolutePath))
            {
                Directory.Delete(absolutePath, true);
            }

            string metaPath = absolutePath + ".meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private static string ToAssetPath(string absolutePath)
        {
            string projectRoot = (Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty).Replace("\\", "/");
            string normalizedPath = Path.GetFullPath(absolutePath).Replace("\\", "/");
            if (!normalizedPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedPath;
            }

            string relativePath = normalizedPath.Substring(projectRoot.Length).TrimStart('/');
            return relativePath;
        }

        private static List<SkillResourceFolderSource> GetUnitFolderSources()
        {
            List<SkillResourceFolderSource> results = new List<SkillResourceFolderSource>();
            string unitsRoot = ToAbsolutePath(SkillEditorResourcePaths.UnitFolder);
            if (!Directory.Exists(unitsRoot))
            {
                return results;
            }

            string[] directories = Directory.GetDirectories(unitsRoot, "*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < directories.Length; i++)
            {
                results.Add(new SkillResourceFolderSource
                {
                    FolderAssetPath = ToAssetPath(directories[i]),
                    SearchPattern = "Unit.json",
                });
            }

            return results;
        }

        private static List<SkillResourceFolderSource> GetSkillFolderSources(string unitId)
        {
            List<SkillResourceFolderSource> results = new List<SkillResourceFolderSource>();
            AddScopedFolders(results, unitId, "Skills");
            return results;
        }

        private static List<SkillResourceFolderSource> GetMetaSkillFolderSources(string unitId)
        {
            List<SkillResourceFolderSource> results = new List<SkillResourceFolderSource>();
            AddScopedFolders(results, unitId, "MetaSkills");
            return results;
        }

        private static List<SkillResourceFolderSource> GetStateFolderSources(string unitId)
        {
            List<SkillResourceFolderSource> results = new List<SkillResourceFolderSource>();
            AddScopedFolders(results, unitId, "States");
            return results;
        }

        private static void AddScopedFolders(List<SkillResourceFolderSource> results, string unitId, string childFolderName)
        {
            List<SkillResourceFileEntry> units = LoadUnits();
            for (int i = 0; i < units.Count; i++)
            {
                UnitConfig unitConfig = units[i].Config as UnitConfig;
                if (unitConfig == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(unitId) && !string.Equals(unitConfig.UnitId, unitId, StringComparison.Ordinal))
                {
                    continue;
                }

                string unitFolderAssetPath = Path.GetDirectoryName(units[i].JsonAssetPath)?.Replace("\\", "/");
                if (string.IsNullOrEmpty(unitFolderAssetPath))
                {
                    continue;
                }

                results.Add(new SkillResourceFolderSource
                {
                    FolderAssetPath = unitFolderAssetPath + "/" + childFolderName,
                    UnitId = unitConfig.UnitId,
                });
            }
        }

        private static bool TryGetUnitFolderAssetPath(string unitId, out string unitFolderAssetPath)
        {
            unitFolderAssetPath = string.Empty;
            if (string.IsNullOrEmpty(unitId))
            {
                return false;
            }

            List<SkillResourceFileEntry> units = LoadUnits();
            for (int i = 0; i < units.Count; i++)
            {
                UnitConfig config = units[i].Config as UnitConfig;
                if (config == null || !string.Equals(config.UnitId, unitId, StringComparison.Ordinal))
                {
                    continue;
                }

                unitFolderAssetPath = Path.GetDirectoryName(units[i].JsonAssetPath)?.Replace("\\", "/") ?? string.Empty;
                return !string.IsNullOrEmpty(unitFolderAssetPath);
            }

            return false;
        }

        private static string GenerateUniqueUnitId(string desiredUnitId)
        {
            string candidate = string.IsNullOrEmpty(desiredUnitId) ? "NewUnit" : desiredUnitId;
            HashSet<string> existingIds = new HashSet<string>(StringComparer.Ordinal);
            List<SkillResourceFileEntry> units = LoadUnits();
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].Config is UnitConfig config && !string.IsNullOrEmpty(config.UnitId))
                {
                    existingIds.Add(config.UnitId);
                }
            }

            if (!existingIds.Contains(candidate))
            {
                return candidate;
            }

            int index = 1;
            string nextCandidate;
            do
            {
                nextCandidate = candidate + "_" + index;
                index++;
            }
            while (existingIds.Contains(nextCandidate));

            return nextCandidate;
        }

        private static string[] EnumerateMetaSkillJsonFiles()
        {
            List<string> results = new List<string>();
            List<SkillResourceFileEntry> units = LoadUnits();
            for (int i = 0; i < units.Count; i++)
            {
                string unitFolder = Path.GetDirectoryName(units[i].JsonAssetPath);
                if (string.IsNullOrEmpty(unitFolder))
                {
                    continue;
                }

                string metaSkillFolder = Path.Combine(unitFolder, "MetaSkills");
                if (Directory.Exists(metaSkillFolder))
                {
                    results.AddRange(Directory.GetFiles(metaSkillFolder, "*.json", SearchOption.TopDirectoryOnly));
                }
            }

            return results.ToArray();
        }

        private static void DeleteUnitFolder(SkillResourceFileEntry entry)
        {
            ClearDirty(entry);

            string unitFolder = Path.GetDirectoryName(entry.JsonAssetPath);
            if (!string.IsNullOrEmpty(unitFolder))
            {
                string absoluteFolder = ToAbsolutePath(unitFolder);
                if (Directory.Exists(absoluteFolder))
                {
                    Directory.Delete(absoluteFolder, true);
                }

                string metaPath = absoluteFolder + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
            }

            DeleteIfExists(entry.ByteAssetPath);
            DeleteIfExists(GetLegacyByteAssetPath(entry.ByteAssetPath));
            if (string.Equals(SkillPreviewUnitSettings.ActiveUnitId, entry.UnitId, StringComparison.Ordinal))
            {
                SkillPreviewUnitSettings.ActiveUnitId = string.Empty;
                SkillPreviewUnitSettings.Save();
            }

            RebuildAnimationCatalog();
            AssetDatabase.Refresh();
        }

        internal static string GetSkillRuntimeId(SkillResourceFileEntry entry)
        {
            if (entry == null || entry.ResourceType != SkillResourceType.Skill)
            {
                return string.Empty;
            }

            SkillConfig config = entry.Config as SkillConfig;
            string localSkillId = !string.IsNullOrEmpty(config?.SkillId) ? config.SkillId : entry.BaseName;
            return BuildScopedRuntimeId(entry.UnitId, localSkillId);
        }

        internal static string GetMetaSkillRuntimeId(SkillResourceFileEntry entry)
        {
            if (entry == null || entry.ResourceType != SkillResourceType.MetaSkill)
            {
                return string.Empty;
            }

            MetaSkillConfig config = entry.Config as MetaSkillConfig;
            string localMetaSkillId = !string.IsNullOrEmpty(config?.MetaSkillId) ? config.MetaSkillId : entry.BaseName;
            return BuildScopedRuntimeId(entry.UnitId, localMetaSkillId);
        }

        internal static bool IsMatchingSkillReference(SkillResourceFileEntry entry, string skillReference)
        {
            if (entry == null || string.IsNullOrEmpty(skillReference))
            {
                return false;
            }

            SkillConfig skillConfig = entry.Config as SkillConfig;
            return string.Equals(GetSkillRuntimeId(entry), skillReference, StringComparison.Ordinal) ||
                   string.Equals(entry.BaseName, skillReference, StringComparison.Ordinal) ||
                   (skillConfig != null && string.Equals(skillConfig.SkillId, skillReference, StringComparison.Ordinal));
        }

        internal static bool IsMatchingMetaSkillReference(SkillResourceFileEntry entry, string metaSkillReference)
        {
            if (entry == null || string.IsNullOrEmpty(metaSkillReference))
            {
                return false;
            }

            MetaSkillConfig metaSkillConfig = entry.Config as MetaSkillConfig;
            return string.Equals(GetMetaSkillRuntimeId(entry), metaSkillReference, StringComparison.Ordinal) ||
                   string.Equals(entry.BaseName, metaSkillReference, StringComparison.Ordinal) ||
                   (metaSkillConfig != null && string.Equals(metaSkillConfig.MetaSkillId, metaSkillReference, StringComparison.Ordinal));
        }

        internal static string NormalizeSkillReference(string unitId, string skillReference)
        {
            if (string.IsNullOrEmpty(skillReference))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(unitId) || skillReference.StartsWith(unitId + "_", StringComparison.Ordinal))
            {
                return skillReference;
            }

            return BuildScopedRuntimeId(unitId, skillReference);
        }

        internal static string NormalizeMetaSkillReference(string unitId, string metaSkillReference)
        {
            if (string.IsNullOrEmpty(metaSkillReference))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(unitId) || metaSkillReference.StartsWith(unitId + "_", StringComparison.Ordinal))
            {
                return metaSkillReference;
            }

            return BuildScopedRuntimeId(unitId, metaSkillReference);
        }

        private static string BuildScopedRuntimeId(string unitId, string localId)
        {
            if (string.IsNullOrEmpty(localId))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(unitId))
            {
                return localId;
            }

            return unitId + "_" + localId;
        }

        private static UnitConfig BuildRuntimeUnitConfig(string unitId, UnitConfig source)
        {
            UnitConfig runtimeConfig = DeepCloneEditorConfig(source, () => new UnitConfig());
            if (runtimeConfig.ActiveSkillSlots != null)
            {
                for (int i = 0; i < runtimeConfig.ActiveSkillSlots.Count; i++)
                {
                    if (runtimeConfig.ActiveSkillSlots[i] != null)
                    {
                        runtimeConfig.ActiveSkillSlots[i].SkillId = NormalizeSkillReference(unitId, runtimeConfig.ActiveSkillSlots[i].SkillId);
                    }
                }
            }

            if (runtimeConfig.PassiveSkillSlots != null)
            {
                for (int i = 0; i < runtimeConfig.PassiveSkillSlots.Count; i++)
                {
                    if (runtimeConfig.PassiveSkillSlots[i] != null)
                    {
                        runtimeConfig.PassiveSkillSlots[i].SkillId = NormalizeSkillReference(unitId, runtimeConfig.PassiveSkillSlots[i].SkillId);
                    }
                }
            }

            return runtimeConfig;
        }

        private static SkillConfig BuildRuntimeSkillConfig(string unitId, SkillConfig source)
        {
            SkillConfig runtimeConfig = DeepCloneEditorConfig(source, () => new SkillConfig());
            if (runtimeConfig.Layers == null)
            {
                return runtimeConfig;
            }

            for (int layerIndex = 0; layerIndex < runtimeConfig.Layers.Count; layerIndex++)
            {
                SkillLayerConfig layer = runtimeConfig.Layers[layerIndex];
                if (layer == null || layer.MetaSkillNodes == null)
                {
                    continue;
                }

                for (int nodeIndex = 0; nodeIndex < layer.MetaSkillNodes.Count; nodeIndex++)
                {
                    MetaSkillNodeConfig node = layer.MetaSkillNodes[nodeIndex];
                    if (node == null)
                    {
                        continue;
                    }

                    node.MetaSkillAssetName = NormalizeMetaSkillReference(unitId, node.MetaSkillAssetName);
                }
            }

            return runtimeConfig;
        }
    }

}
