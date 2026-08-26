using System;
using System.Collections.Generic;
using ActionEditor.TagSystem;
using AsiSkillEditor.RunTime;
using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    internal static class TagEditorUtility
    {
        private const int MaxMaskCount = 30;

        public static List<string> GetAvailableTags()
        {
            HashSet<string> tags = new HashSet<string>(StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets("t:TagDefinitionCatalog");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TagDefinitionCatalog catalog = AssetDatabase.LoadAssetAtPath<TagDefinitionCatalog>(path);
                if (catalog == null || catalog.Tags == null)
                {
                    continue;
                }

                for (int index = 0; index < catalog.Tags.Count; index++)
                {
                    string tag = catalog.Tags[index];
                    if (!string.IsNullOrEmpty(tag))
                    {
                        tags.Add(tag);
                    }
                }
            }

            List<string> result = new List<string>(tags);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        public static bool DrawTagContainer(string label, TagContainer container)
        {
            if (container == null)
            {
                return false;
            }

            container.Tags ??= new List<string>();
            return DrawTagList(label, container.Tags);
        }

        public static bool DrawTagList(string label, List<string> selectedTags)
        {
            selectedTags ??= new List<string>();
            List<string> options = BuildOptions(selectedTags);
            if (options.Count == 0)
            {
                EditorGUILayout.LabelField(label, "(no TagDefinitionCatalog found)");
                return false;
            }

            if (options.Count > MaxMaskCount)
            {
                EditorGUILayout.HelpBox($"Tag options exceed {MaxMaskCount}, only first {MaxMaskCount} are shown.", MessageType.Warning);
                options = options.GetRange(0, MaxMaskCount);
            }

            int mask = BuildMask(options, selectedTags);
            int nextMask = EditorGUILayout.MaskField(label, mask, options.ToArray());
            if (nextMask == mask)
            {
                return false;
            }

            selectedTags.Clear();
            for (int i = 0; i < options.Count; i++)
            {
                if ((nextMask & (1 << i)) != 0)
                {
                    selectedTags.Add(options[i]);
                }
            }

            selectedTags.Sort(StringComparer.Ordinal);
            return true;
        }

        public static bool DrawSingleTagField(string label, ref string tag)
        {
            List<string> selected = new List<string>();
            if (!string.IsNullOrEmpty(tag))
            {
                selected.Add(tag);
            }

            bool changed = DrawTagList(label, selected);
            if (!changed)
            {
                return false;
            }

            tag = selected.Count > 0 ? selected[0] : string.Empty;
            return true;
        }

        private static List<string> BuildOptions(List<string> selectedTags)
        {
            List<string> options = GetAvailableTags();
            for (int i = 0; i < selectedTags.Count; i++)
            {
                string tag = selectedTags[i];
                if (string.IsNullOrEmpty(tag) || options.Contains(tag))
                {
                    continue;
                }

                options.Add(tag);
            }

            options.Sort(StringComparer.Ordinal);
            return options;
        }

        private static int BuildMask(List<string> options, List<string> selectedTags)
        {
            int mask = 0;
            for (int i = 0; i < options.Count; i++)
            {
                if (selectedTags.Contains(options[i]))
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }
    }
}
