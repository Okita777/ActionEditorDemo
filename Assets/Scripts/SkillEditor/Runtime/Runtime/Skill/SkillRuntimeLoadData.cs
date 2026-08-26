using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif

namespace AsiSkillEditor.RunTime
{
    /// <summary>
    /// 技能运行时配置加载器。
    /// 负责从 SkillRuntimeDataPath 指定的位置读取编译后的二进制配置，并反序列化为运行时配置对象。
    /// </summary>
    public sealed class SkillRuntimeLoadData
    {
        /// <summary>
        /// 单例实例缓存，统一对外提供配置加载入口。
        /// </summary>
        private static SkillRuntimeLoadData _instance;

        /// <summary>
        /// 全局配置加载入口。
        /// </summary>
        public static SkillRuntimeLoadData Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SkillRuntimeLoadData();
                }

                return _instance;
            }
        }

        /// <summary>
        /// 加载一个 Skill 根配置。
        /// </summary>
        /// <param name="skillName">Skill 名称，不需要附带 .byte 后缀。</param>
        /// <param name="onLoaded">加载成功后的回调，参数为反序列化出的 SkillConfig。</param>
        /// <returns>是否成功加载并触发回调。</returns>
        public bool LoadSkill(string skillName, Action<SkillConfig> onLoaded)
        {
            string path = SkillRuntimeDataPath.Instance.SkillPath(skillName + SkillRuntimeDataPath.Suffix);
            if (!LoadBinary(path, onLoaded))
            {
                UnityEngine.Debug.LogWarning($"SkillRuntimeLoadData.LoadSkill: failed to load skillId '{skillName}' from '{path}'.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 加载一个 MetaSkill 配置。
        /// </summary>
        /// <param name="metaSkillName">MetaSkill 名称，不需要附带 .byte 后缀。</param>
        /// <param name="onLoaded">加载成功后的回调，参数为反序列化出的 MetaSkillConfig。</param>
        /// <returns>是否成功加载并触发回调。</returns>
        public bool LoadMetaSkill(string metaSkillName, Action<MetaSkillConfig> onLoaded)
        {
            return LoadBinary(
                SkillRuntimeDataPath.Instance.MetaSkillPath(metaSkillName + SkillRuntimeDataPath.Suffix),
                onLoaded);
        }

        /// <summary>
        /// 加载一个 Buff 配置。
        /// </summary>
        /// <param name="buffName">Buff 名称，不需要附带 .byte 后缀。</param>
        /// <param name="onLoaded">加载成功后的回调，参数为反序列化出的 BuffConfig。</param>
        /// <returns>是否成功加载并触发回调。</returns>
        public bool LoadBuff(string buffName, Action<BuffConfig> onLoaded)
        {
            return LoadBinary(
                SkillRuntimeDataPath.Instance.BuffPath(buffName + SkillRuntimeDataPath.Suffix),
                onLoaded);
        }

        /// <summary>
        /// 加载一个 Unit 配置。
        /// </summary>
        /// <param name="unitId">Unit 标识，不需要附带 .byte 后缀。</param>
        /// <param name="onLoaded">加载成功后的回调，参数为反序列化出的 UnitConfig。</param>
        /// <returns>是否成功加载并触发回调。</returns>
        public bool LoadUnit(string unitId, Action<UnitConfig> onLoaded)
        {
            return LoadBinary(
                SkillRuntimeDataPath.Instance.UnitPath(unitId + SkillRuntimeDataPath.Suffix),
                onLoaded);
        }

        /// <summary>
        /// 同步加载并返回一个 Unit 配置。
        /// </summary>
        /// <param name="unitId">Unit 标识，不需要附带 .byte 后缀。</param>
        /// <returns>加载成功时返回 UnitConfig；失败或 unitId 无效时返回 null。</returns>
        public UnitConfig LoadUnitConfig(string unitId)
        {
#if UNITY_EDITOR
            UnitConfig editorConfig = LoadEditorUnitConfig(unitId);
            if (editorConfig != null)
            {
                return editorConfig;
            }
#endif
            return string.IsNullOrWhiteSpace(unitId)
                ? null
                : LoadBinaryConfig<UnitConfig>(SkillRuntimeDataPath.Instance.UnitPath(unitId + SkillRuntimeDataPath.Suffix));
        }

        /// <summary>
        /// 加载一个 State 配置。
        /// </summary>
        /// <param name="stateName">State 名称，不需要附带 .byte 后缀。</param>
        /// <param name="onLoaded">加载成功后的回调，参数为反序列化出的 StateConfig。</param>
        /// <returns>是否成功加载并触发回调。</returns>
        public bool LoadState(string stateName, Action<StateConfig> onLoaded)
        {
            return LoadBinary(
                SkillRuntimeDataPath.Instance.StatePath(stateName + SkillRuntimeDataPath.Suffix),
                onLoaded);
        }

        /// <summary>
        /// 加载指定 Unit 下的所有 State 配置。
        /// </summary>
        /// <param name="unitId">Unit 标识。</param>
        /// <returns>该 Unit 状态目录下成功反序列化的 StateConfig 列表。</returns>
        public List<StateConfig> LoadStatesForUnit(string unitId)
        {
#if UNITY_EDITOR
            List<StateConfig> editorConfigs = LoadEditorStateConfigs(unitId);
            if (editorConfigs.Count > 0)
            {
                return editorConfigs;
            }
#endif
            List<StateConfig> results = new List<StateConfig>();
            string folderPath = SkillRuntimeDataPath.Instance.UnitStateFolder(unitId);
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return results;
            }

            string[] files = Directory.GetFiles(folderPath, "*" + SkillRuntimeDataPath.Suffix, SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                StateConfig config = LoadBinaryConfig<StateConfig>(files[i]);
                if (config != null)
                {
                    results.Add(config);
                }
            }

            return results;
        }

#if UNITY_EDITOR
        private static UnitConfig LoadEditorUnitConfig(string unitId)
        {
            if (string.IsNullOrWhiteSpace(unitId))
            {
                return null;
            }

            string[] paths = AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/SkillEditor/Data/Units" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith("/Unit.json", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            for (int i = 0; i < paths.Length; i++)
            {
                UnitConfig config = JsonUtility.FromJson<UnitConfig>(File.ReadAllText(paths[i]));
                if (config != null && string.Equals(config.UnitId, unitId, StringComparison.OrdinalIgnoreCase))
                {
                    return config;
                }
            }

            return null;
        }

        private static List<StateConfig> LoadEditorStateConfigs(string unitId)
        {
            List<StateConfig> results = new List<StateConfig>();
            string unitFolder = FindEditorUnitFolder(unitId);
            string stateFolder = string.IsNullOrEmpty(unitFolder) ? string.Empty : Path.Combine(unitFolder, "States");
            if (string.IsNullOrEmpty(stateFolder) || !Directory.Exists(stateFolder))
            {
                return results;
            }

            string[] paths = Directory.GetFiles(stateFolder, "*.json", SearchOption.AllDirectories);
            for (int i = 0; i < paths.Length; i++)
            {
                StateConfig config = JsonUtility.FromJson<StateConfig>(File.ReadAllText(paths[i]));
                if (config != null)
                {
                    results.Add(config);
                }
            }

            return results;
        }

        private static string FindEditorUnitFolder(string unitId)
        {
            string[] paths = AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/SkillEditor/Data/Units" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith("/Unit.json", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            for (int i = 0; i < paths.Length; i++)
            {
                UnitConfig config = JsonUtility.FromJson<UnitConfig>(File.ReadAllText(paths[i]));
                if (config != null && string.Equals(config.UnitId, unitId, StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetDirectoryName(paths[i]);
                }
            }

            return string.Empty;
        }
#endif

        /// <summary>
        /// 通用二进制配置加载入口。
        /// 加载成功后会把配置对象交给调用方传入的回调。
        /// </summary>
        /// <param name="relativePath">配置文件路径，通常是工程相对路径。</param>
        /// <param name="onLoaded">加载成功后的回调。</param>
        /// <typeparam name="TConfig">期望反序列化出的配置类型。</typeparam>
        /// <returns>是否成功加载并触发回调。</returns>
        private static bool LoadBinary<TConfig>(string relativePath, Action<TConfig> onLoaded) where TConfig : class
        {
            if (onLoaded == null || string.IsNullOrEmpty(relativePath))
            {
                return false;
            }

            TConfig config = LoadBinaryConfig<TConfig>(relativePath);
            if (config == null)
            {
                return false;
            }

            onLoaded(config);
            return true;
        }

        /// <summary>
        /// 从磁盘读取二进制配置文件并反序列化。
        /// </summary>
        /// <param name="relativePath">配置文件路径，通常是工程相对路径。</param>
        /// <typeparam name="TConfig">期望反序列化出的配置类型。</typeparam>
        /// <returns>反序列化成功时返回配置对象；文件不存在或读取失败时返回 null。</returns>
        private static TConfig LoadBinaryConfig<TConfig>(string relativePath) where TConfig : class
        {
            if (string.IsNullOrEmpty(relativePath) || !File.Exists(relativePath))
            {
                return null;
            }

#pragma warning disable SYSLIB0011
            try
            {
                using (FileStream fileStream = new FileStream(relativePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    return binaryFormatter.Deserialize(fileStream) as TConfig;
                }
            }
            catch
            {
                return null;
            }
#pragma warning restore SYSLIB0011
        }
    }
}
