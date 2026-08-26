using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AsiSkillEditor.RunTime
{
    public static class SkillAnimationRuntimeCatalog
    {
        private const char Separator = '|';
        private const string ResourcePath = "SkillEditor/SkillAnimationCatalog";

        private static readonly Dictionary<string, AnimationClip> ClipLookup = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
        private static bool s_initialized;

        public static AnimationClip LoadClip(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            EnsureInitialized();
            if (ClipLookup.TryGetValue(key, out AnimationClip clip) && clip != null)
            {
                return clip;
            }

#if UNITY_EDITOR
            return LoadClipFromSerializedReference(key);
#else
            return null;
#endif
        }

        private static void EnsureInitialized()
        {
            if (s_initialized)
            {
                return;
            }

            s_initialized = true;
            ClipLookup.Clear();

            SkillAnimationCatalog catalog = Resources.Load<SkillAnimationCatalog>(ResourcePath);
            if (catalog == null || catalog.Entries == null)
            {
                return;
            }

            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                SkillAnimationCatalogEntry entry = catalog.Entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.Key) || entry.Clip == null)
                {
                    continue;
                }

                ClipLookup[entry.Key] = entry.Clip;
            }
        }

#if UNITY_EDITOR
        private static AnimationClip LoadClipFromSerializedReference(string serializedReference)
        {
            ParseReference(serializedReference, out string assetPath, out long localId, out string clipName);
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            AnimationClip fallbackClip = null;
            for (int i = 0; i < assets.Length; i++)
            {
                if (!(assets[i] is AnimationClip candidate))
                {
                    continue;
                }

                if (fallbackClip == null)
                {
                    fallbackClip = candidate;
                }

                if (localId != 0 && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate, out _, out long candidateLocalId) && candidateLocalId == localId)
                {
                    ClipLookup[serializedReference] = candidate;
                    return candidate;
                }

                if (!string.IsNullOrEmpty(clipName) && string.Equals(candidate.name, clipName, StringComparison.Ordinal))
                {
                    ClipLookup[serializedReference] = candidate;
                    return candidate;
                }
            }

            if (fallbackClip != null)
            {
                ClipLookup[serializedReference] = fallbackClip;
            }

            return fallbackClip;
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
#endif
    }

    public static class SkillBulletRuntimeCatalog
    {
        private const string ResourcePath = "SkillEditor/SkillBulletCatalog";

        private static readonly Dictionary<string, GameObject> PrefabLookup = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private static bool s_initialized;

        public static GameObject LoadPrefab(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            EnsureInitialized();
            if (PrefabLookup.TryGetValue(key, out GameObject prefab) && prefab != null)
            {
                return prefab;
            }

#if UNITY_EDITOR
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(key);
            if (prefab != null)
            {
                PrefabLookup[key] = prefab;
            }
            return prefab;
#else
            return null;
#endif
        }

        private static void EnsureInitialized()
        {
            if (s_initialized)
            {
                return;
            }

            s_initialized = true;
            PrefabLookup.Clear();

            SkillBulletCatalog catalog = Resources.Load<SkillBulletCatalog>(ResourcePath);
            if (catalog == null || catalog.Entries == null)
            {
                return;
            }

            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                SkillBulletCatalogEntry entry = catalog.Entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.Key) || entry.Prefab == null)
                {
                    continue;
                }

                PrefabLookup[entry.Key] = entry.Prefab;
            }
        }
    }
}