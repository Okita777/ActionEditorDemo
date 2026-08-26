using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AsiSkillEditor.RunTime
{
    public static class FeedbackAssetRuntimeCatalog
    {
        private static readonly Dictionary<string, GameObject> VfxLookup = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private static readonly Dictionary<string, AudioClip> AudioLookup = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        private static readonly Dictionary<string, AudioMixerGroup> MixerLookup = new Dictionary<string, AudioMixerGroup>(StringComparer.Ordinal);

        public static GameObject LoadVfxPrefab(string assetPath)
        {
            if (VfxLookup.TryGetValue(assetPath ?? string.Empty, out GameObject prefab) && prefab != null)
            {
                return prefab;
            }
#if UNITY_EDITOR
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null) VfxLookup[assetPath] = prefab;
            return prefab;
#else
            return null;
#endif
        }

        public static AudioClip LoadAudioClip(string assetPath)
        {
            if (AudioLookup.TryGetValue(assetPath ?? string.Empty, out AudioClip clip) && clip != null)
            {
                return clip;
            }
#if UNITY_EDITOR
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip != null) AudioLookup[assetPath] = clip;
            return clip;
#else
            return null;
#endif
        }

        public static AudioMixerGroup LoadMixerGroup(string mixerPath, string groupName)
        {
            if (string.IsNullOrEmpty(mixerPath) || string.IsNullOrEmpty(groupName)) return null;
            string key = mixerPath + "|" + groupName;
            if (MixerLookup.TryGetValue(key, out AudioMixerGroup group) && group != null)
            {
                return group;
            }
#if UNITY_EDITOR
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
            if (mixer != null)
            {
                AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
                if (groups.Length > 0)
                {
                    MixerLookup[key] = groups[0];
                    return groups[0];
                }
            }
#endif
            return null;
        }

    }
}
