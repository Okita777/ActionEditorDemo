using SkillEditor.Preview;
using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    internal static class SkillAnimationSelectionUtility
    {
        public static bool IsClipAllowed(AnimationClip clip)
        {
            if (clip == null)
            {
                return false;
            }

            GameUnit previewConfig = SkillPreviewUnitSettings.LoadActivePreviewConfig();
            if (previewConfig == null)
            {
                return true;
            }

            string assetPath = AssetDatabase.GetAssetPath(clip);
            if (!string.IsNullOrEmpty(previewConfig.AnimationSearchRoot) &&
                !assetPath.StartsWith(previewConfig.AnimationSearchRoot))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(previewConfig.AnimationFilterKey) &&
                assetPath.IndexOf(previewConfig.AnimationFilterKey, System.StringComparison.OrdinalIgnoreCase) < 0 &&
                clip.name.IndexOf(previewConfig.AnimationFilterKey, System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            return true;
        }

        public static bool TryExtractClipFromDrag(Object[] references, out AnimationClip clip, out string errorMessage)
        {
            clip = null;
            errorMessage = string.Empty;
            if (references == null || references.Length == 0)
            {
                errorMessage = "没有检测到拖入资源。";
                return false;
            }

            if (references.Length > 1)
            {
                errorMessage = "一次只允许拖入一个动画资源。";
                return false;
            }

            if (!(references[0] is AnimationClip animationClip))
            {
                errorMessage = "这里只接受 AnimationClip。请不要拖 FBX 主资源或模型 prefab。";
                return false;
            }

            if (!IsClipAllowed(animationClip))
            {
                errorMessage = "这个动画不符合当前预览单位的筛选规则。";
                return false;
            }

            clip = animationClip;
            return true;
        }

        public static string SerializeAllowedClip(AnimationClip clip)
        {
            return IsClipAllowed(clip) ? SkillAnimationReferenceUtility.SerializeClip(clip) : string.Empty;
        }
    }
}
