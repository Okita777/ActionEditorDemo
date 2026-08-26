using UnityEditor;

namespace SkillEditor.Editor
{
    [InitializeOnLoad]
    internal static class SkillAnimationCatalogBootstrap
    {
        static SkillAnimationCatalogBootstrap()
        {
            EditorApplication.delayCall += RebuildCatalog;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                SkillResourceRepository.SaveDirtyEntries();
                RebuildCatalog();
            }
        }

        private static void RebuildCatalog()
        {
            if (EditorApplication.isCompiling)
            {
                return;
            }

            SkillResourceRepository.RebuildAnimationCatalog();
            SkillResourceRepository.RebuildBulletCatalog();
        }
    }
}