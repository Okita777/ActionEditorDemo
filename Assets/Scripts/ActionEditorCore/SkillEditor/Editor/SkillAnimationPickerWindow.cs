using System;
using System.Collections.Generic;
using SkillEditor.Preview;
using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    public sealed class SkillAnimationPickerWindow : EditorWindow
    {
        private Action<string> _onSelected;
        private readonly List<AnimationClip> _clips = new List<AnimationClip>();
        private string _searchKeyword = string.Empty;
        private Vector2 _scrollPosition;
        private string _filterRoot = string.Empty;
        private string _filterKey = string.Empty;

        internal static void Open(Action<string> onSelected)
        {
            SkillAnimationPickerWindow window = CreateInstance<SkillAnimationPickerWindow>();
            window.titleContent = new GUIContent("Select Animation");
            window.minSize = new Vector2(420f, 420f);
            window._onSelected = onSelected;
            window.RefreshClips();
            window.ShowUtility();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(6f);
            DrawSearchBar();
            EditorGUILayout.Space(6f);
            DrawClipList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("动画筛选", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("搜索目录", string.IsNullOrEmpty(_filterRoot) ? "Assets" : _filterRoot);
            EditorGUILayout.LabelField("过滤关键字", string.IsNullOrEmpty(_filterKey) ? "无" : _filterKey);
            EditorGUILayout.EndVertical();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            _searchKeyword = EditorGUILayout.TextField("搜索", _searchKeyword);
            if (GUILayout.Button("刷新", GUILayout.Width(56f)))
            {
                RefreshClips();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawClipList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            List<AnimationClip> filtered = GetVisibleClips();
            if (filtered.Count == 0)
            {
                EditorGUILayout.HelpBox("当前筛选条件下没有可用动画。", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            for (int i = 0; i < filtered.Count; i++)
            {
                AnimationClip clip = filtered[i];
                string assetPath = AssetDatabase.GetAssetPath(clip);
                if (GUILayout.Button($"{clip.name}\n{assetPath}", GUILayout.Height(40f)))
                {
                    _onSelected?.Invoke(SkillAnimationReferenceUtility.SerializeClip(clip));
                    Close();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void RefreshClips()
        {
            _clips.Clear();
            GameUnit previewConfig = SkillPreviewUnitSettings.LoadActivePreviewConfig();
            _filterRoot = previewConfig != null ? previewConfig.AnimationSearchRoot : string.Empty;
            _filterKey = previewConfig != null ? previewConfig.AnimationFilterKey : string.Empty;

            string[] searchFolders = string.IsNullOrEmpty(_filterRoot) ? null : new[] { _filterRoot };
            List<AnimationClip> collectedClips = SkillAnimationReferenceUtility.CollectClips(searchFolders);
            for (int i = 0; i < collectedClips.Count; i++)
            {
                AnimationClip clip = collectedClips[i];
                if (clip != null && SkillAnimationSelectionUtility.IsClipAllowed(clip))
                {
                    _clips.Add(clip);
                }
            }

            _clips.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        }

        private List<AnimationClip> GetVisibleClips()
        {
            List<AnimationClip> result = new List<AnimationClip>();
            for (int i = 0; i < _clips.Count; i++)
            {
                AnimationClip clip = _clips[i];
                if (string.IsNullOrEmpty(_searchKeyword))
                {
                    result.Add(clip);
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(clip);
                if (clip.name.IndexOf(_searchKeyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    assetPath.IndexOf(_searchKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(clip);
                }
            }

            return result;
        }
    }
}
