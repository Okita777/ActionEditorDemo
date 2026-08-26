namespace SkillEditor.Editor
{
    internal static class SkillEditorResourcePaths
    {
        public const string RuntimeBinarySuffix = ".byte";
        public const string DataRoot = "Assets/SkillEditor/Data";
        public const string CompiledRoot = "Assets/SkillEditor/Compiled";
        public const string ResourcesRoot = "Assets/Resources";
        public const string SkillEditorResourcesRoot = ResourcesRoot + "/SkillEditor";
        public const string EditorSettingsRoot = DataRoot + "/Editor";
        public const string UnitFolder = DataRoot + "/Units";

        public const string SkillFolder = DataRoot + "/Skills";
        public const string MetaSkillFolder = DataRoot + "/MetaSkills";
        public const string BuffFolder = DataRoot + "/Buffs";
        public const string BuffIconFolder = ResourcesRoot + "/ui/buffIcon";

        public const string CompiledUnitFolder = CompiledRoot + "/Units";
        public const string CompiledSkillFolder = CompiledRoot + "/Skills";
        public const string CompiledMetaSkillFolder = CompiledRoot + "/MetaSkills";
        public const string CompiledStateFolder = CompiledRoot + "/States";
        public const string CompiledBuffFolder = CompiledRoot + "/Buffs";
        public const string PreviewUnitSettingsFile = EditorSettingsRoot + "/PreviewUnitSettings.json";
        public const string RuntimeAnimationCatalogAssetPath = SkillEditorResourcesRoot + "/SkillAnimationCatalog.asset";
        public const string RuntimeBulletCatalogAssetPath = SkillEditorResourcesRoot + "/SkillBulletCatalog.asset";
    }
}
