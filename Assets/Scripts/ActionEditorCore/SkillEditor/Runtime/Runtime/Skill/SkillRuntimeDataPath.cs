namespace AsiSkillEditor.RunTime
{
    /// <summary>
    /// 技能运行时二进制数据的路径解析器。
    /// 统一维护 Skill、MetaSkill、Buff、Unit、State 等编译产物在工程内的相对路径。
    /// </summary>
    public sealed class SkillRuntimeDataPath
    {
        /// <summary>
        /// 单例实例缓存，避免外部重复创建路径解析器。
        /// </summary>
        private static SkillRuntimeDataPath _instance;

        /// <summary>
        /// 全局路径解析入口。
        /// </summary>
        public static SkillRuntimeDataPath Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SkillRuntimeDataPath();
                }

                return _instance;
            }
        }

        /// <summary>
        /// 编译后运行时数据所在的根目录。
        /// </summary>
        private const string RelativeDataPath = "Assets/SkillEditor/Compiled/";

        /// <summary>
        /// 编译后二进制配置文件的后缀名，包含点号。
        /// </summary>
        public const string Suffixes = ".byte";

        /// <summary>
        /// 编译后二进制配置文件的后缀名，不包含点号。
        /// </summary>
        public const string SuffixesP = "byte";

        /// <summary>
        /// 当前推荐使用的文件后缀别名，包含点号。
        /// </summary>
        public const string Suffix = Suffixes;

        /// <summary>
        /// 当前推荐使用的文件后缀别名，不包含点号。
        /// </summary>
        public const string SuffixWithoutDot = SuffixesP;

        /// <summary>
        /// 运行时数据根路径。
        /// </summary>
        public string DataPath => RelativeDataPath;

        /// <summary>
        /// 获取 Skill 配置文件或默认 Skill 目录路径。
        /// </summary>
        /// <param name="name">Skill 文件名；为空时返回默认目录。</param>
        /// <returns>Skill 配置的工程相对路径。</returns>
        public string SkillPath(string name = "")
        {
            if (string.IsNullOrEmpty(name))
            {
                return DataPath + "Units";
            }

            string scopedPath = ResolveScopedResourcePath("Skills", name);
            if (!string.IsNullOrEmpty(scopedPath))
            {
                return scopedPath;
            }

            return DataPath + $"Skills/{name}";
        }

        /// <summary>
        /// 获取 MetaSkill 配置文件或默认 MetaSkill 目录路径。
        /// 会优先查找 Unit 作用域下的同名资源。
        /// </summary>
        /// <param name="name">MetaSkill 文件名；为空时返回默认目录。</param>
        /// <returns>MetaSkill 配置的工程相对路径。</returns>
        public string MetaSkillPath(string name = "")
        {
            if (string.IsNullOrEmpty(name))
            {
                return DataPath + "Units";
            }

            string scopedPath = ResolveScopedResourcePath("MetaSkills", name);
            if (!string.IsNullOrEmpty(scopedPath))
            {
                return scopedPath;
            }

            return DataPath + $"MetaSkills/{name}";
        }

        /// <summary>
        /// 获取 Buff 配置文件或 Buff 目录路径。
        /// </summary>
        /// <param name="name">Buff 文件名；为空时返回 Buff 目录。</param>
        /// <returns>Buff 配置的工程相对路径。</returns>
        public string BuffPath(string name = "")
        {
            if (string.IsNullOrEmpty(name))
            {
                return DataPath + "Buffs";
            }

            return DataPath + $"Buffs/{name}";
        }

        /// <summary>
        /// 获取 Unit 配置文件或 Unit 目录路径。
        /// </summary>
        /// <param name="name">Unit 文件名；为空时返回 Unit 目录。</param>
        /// <returns>Unit 配置的工程相对路径。</returns>
        public string UnitPath(string name = "")
        {
            if (string.IsNullOrEmpty(name))
            {
                return DataPath + "Units";
            }

            return DataPath + $"Units/{name}";
        }

        /// <summary>
        /// 获取 State 配置文件或默认 State 目录路径。
        /// 会优先查找 Unit 作用域下的同名资源。
        /// </summary>
        /// <param name="name">State 文件名；为空时返回默认目录。</param>
        /// <returns>State 配置的工程相对路径。</returns>
        public string StatePath(string name = "")
        {
            if (string.IsNullOrEmpty(name))
            {
                return DataPath + "Units";
            }

            string scopedPath = ResolveScopedResourcePath("States", name);
            if (!string.IsNullOrEmpty(scopedPath))
            {
                return scopedPath;
            }

            return DataPath + $"States/{name}";
        }

        /// <summary>
        /// 获取指定 Unit 下的 State 配置目录。
        /// </summary>
        /// <param name="unitId">Unit 标识。</param>
        /// <returns>Unit 专属 State 目录路径；unitId 无效时返回空字符串。</returns>
        public string UnitStateFolder(string unitId)
        {
            return string.IsNullOrWhiteSpace(unitId)
                ? string.Empty
                : DataPath + $"Units/{unitId}/States";
        }

        /// <summary>
        /// 在所有 Unit 子目录中查找作用域资源。
        /// 用于让不同 Unit 可以拥有同名 Skill、MetaSkill 或 State 的专属配置。
        /// </summary>
        /// <param name="childFolderName">Unit 目录下的资源子文件夹名，例如 Skills、MetaSkills、States。</param>
        /// <param name="fileName">要查找的文件名，通常已包含后缀。</param>
        /// <returns>找到时返回资源路径；找不到时返回空字符串。</returns>
        private string ResolveScopedResourcePath(string childFolderName, string fileName)
        {
            if (string.IsNullOrEmpty(childFolderName) || string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }

            string unitsRoot = DataPath + "Units";
            if (!System.IO.Directory.Exists(unitsRoot))
            {
                return string.Empty;
            }

            string[] unitDirectories = System.IO.Directory.GetDirectories(unitsRoot, "*", System.IO.SearchOption.TopDirectoryOnly);
            for (int i = 0; i < unitDirectories.Length; i++)
            {
                string candidate = System.IO.Path.Combine(unitDirectories[i], childFolderName, fileName).Replace("\\", "/");
                if (System.IO.File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }
    }
}
