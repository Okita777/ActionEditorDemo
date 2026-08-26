using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    internal static class SkillAnimationReferenceUtility
    {
        private const char Separator = '|';

        public static string SerializeClip(AnimationClip clip)
        {
            if (clip == null)
            {
                return string.Empty;
            }

            string assetPath = AssetDatabase.GetAssetPath(clip);
            if (string.IsNullOrEmpty(assetPath))
            {
                return string.Empty;
            }

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out _, out long localId))
            {
                return assetPath;
            }

            return $"{assetPath}{Separator}{localId}{Separator}{clip.name}";
        }

        public static AnimationClip LoadClip(string serializedReference)
        {
            if (string.IsNullOrEmpty(serializedReference))
            {
                return null;
            }

            ParseReference(serializedReference, out string assetPath, out long localId, out string clipName);
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            AnimationClip fallbackClip = null;
            for (int i = 0; i < assets.Length; i++)
            {
                if (!(assets[i] is AnimationClip clip))
                {
                    continue;
                }

                if (fallbackClip == null)
                {
                    fallbackClip = clip;
                }

                if (localId != 0 &&
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out _, out long candidateLocalId) &&
                    candidateLocalId == localId)
                {
                    return clip;
                }

                if (!string.IsNullOrEmpty(clipName) && clip.name == clipName)
                {
                    return clip;
                }
            }

            return fallbackClip;
        }

        public static string GetDisplayPath(string serializedReference)
        {
            ParseReference(serializedReference, out string assetPath, out _, out _);
            return assetPath;
        }

        public static List<AnimationClip> CollectClips(string[] searchFolders)
        {
            List<AnimationClip> clips = new List<AnimationClip>();
            HashSet<string> seen = new HashSet<string>();
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", searchFolders);
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
                {
                    if (!(assets[assetIndex] is AnimationClip clip))
                    {
                        continue;
                    }

                    string key = SerializeClip(clip);
                    if (string.IsNullOrEmpty(key) || !seen.Add(key))
                    {
                        continue;
                    }

                    clips.Add(clip);
                }
            }

            return clips;
        }

        private static void ParseReference(string serializedReference, out string assetPath, out long localId, out string clipName)
        {
            assetPath = serializedReference;
            localId = 0;
            clipName = string.Empty;

            int firstSeparator = serializedReference.IndexOf(Separator);
            if (firstSeparator < 0)
            {
                return;
            }

            int secondSeparator = serializedReference.IndexOf(Separator, firstSeparator + 1);
            assetPath = serializedReference.Substring(0, firstSeparator);

            string localIdString = secondSeparator < 0
                ? serializedReference.Substring(firstSeparator + 1)
                : serializedReference.Substring(firstSeparator + 1, secondSeparator - firstSeparator - 1);
            long.TryParse(localIdString, out localId);

            if (secondSeparator >= 0 && secondSeparator + 1 < serializedReference.Length)
            {
                clipName = serializedReference.Substring(secondSeparator + 1);
            }
        }
    }
}
