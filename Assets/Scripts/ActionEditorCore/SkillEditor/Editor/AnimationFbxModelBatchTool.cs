using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkillEditor.Editor
{
    /// <summary>分步骤批量设置纯动画 FBX 的 Model、Materials 与 Rig 导入参数。</summary>
    public sealed class AnimationFbxModelBatchTool : EditorWindow
    {
        private DefaultAsset _folder;
        private bool _includeSubfolders = true;
        private ModelImporterAnimationType _rigAnimationType = ModelImporterAnimationType.Generic;
        private Avatar _sourceAvatar;
        private Vector2 _scrollPosition;
        private List<string> _fbxPaths = new List<string>();

        [MenuItem("ActionEditor/动画资源/FBX 批量导入设置")]
        private static void Open()
        {
            GetWindow<AnimationFbxModelBatchTool>("FBX 批处理");
        }

        [MenuItem("Tools/ActionEditor/动画资源/FBX 批量导入设置")]
        private static void OpenFromTools()
        {
            Open();
        }

        [MenuItem("Assets/ActionEditor/打开 FBX 批量导入设置", false, 2000)]
        private static void OpenFromSelectedFolder()
        {
            AnimationFbxModelBatchTool window = GetWindow<AnimationFbxModelBatchTool>("FBX 批处理");
            window.SetFolderFromSelection();
        }

        [MenuItem("Assets/ActionEditor/打开 FBX 批量导入设置", true)]
        private static bool ValidateOpenFromSelectedFolder()
        {
            string path = Selection.activeObject != null
                ? AssetDatabase.GetAssetPath(Selection.activeObject)
                : string.Empty;
            return AssetDatabase.IsValidFolder(path);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("纯动画 FBX 批量导入设置", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "三个按钮互相独立，只处理对应页面。不会修改 Animation Clip、Loop 或 Root Motion 设置。" +
                "每次执行都会重新导入目标 FBX，请先用小文件夹测试并确认磁盘空间充足。",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _folder = EditorGUILayout.ObjectField("目标文件夹", _folder, typeof(DefaultAsset), false) as DefaultAsset;
            _includeSubfolders = EditorGUILayout.Toggle("包含子文件夹", _includeSubfolders);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshPaths();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("使用 Project 当前文件夹"))
                {
                    SetFolderFromSelection();
                }

                if (GUILayout.Button("刷新列表"))
                {
                    RefreshPaths();
                }
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"待处理 FBX：{_fbxPaths.Count}", EditorStyles.boldLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MinHeight(120f));
            for (int i = 0; i < _fbxPaths.Count; i++)
            {
                EditorGUILayout.LabelField(_fbxPaths[i], EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(_fbxPaths.Count == 0))
            {
                DrawModelSection();
                EditorGUILayout.Space(6f);
                DrawMaterialsSection();
                EditorGUILayout.Space(6f);
                DrawRigSection();
            }
        }

        private void DrawModelSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("1. Model", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("启用", "Convert Units、Sort Hierarchy、Weld Vertices");
            EditorGUILayout.LabelField("禁用", "BlendShapes、Visibility、Cameras、Lights、Read/Write、Colliders 等");
            EditorGUILayout.LabelField("网格", "Compression=Medium，Optimize=Everything，Normals/Tangents=None");
            if (GUILayout.Button($"批量处理 Model（{_fbxPaths.Count} 个）", GUILayout.Height(30f)))
            {
                ApplyToAll("Model", ApplyModelSettings, "Materials、Rig 和 Animation 设置未修改。");
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawMaterialsSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("2. Materials", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Material Creation Mode", "None（不导入、不创建、不搜索材质）");
            if (GUILayout.Button($"批量处理 Materials（{_fbxPaths.Count} 个）", GUILayout.Height(30f)))
            {
                ApplyToAll("Materials", ApplyMaterialsSettings, "Model、Rig 和 Animation 设置未修改。");
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawRigSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("3. Rig", EditorStyles.boldLabel);
            _rigAnimationType = (ModelImporterAnimationType)EditorGUILayout.EnumPopup("Animation Type", _rigAnimationType);
            _sourceAvatar = EditorGUILayout.ObjectField("正式模型 Avatar", _sourceAvatar, typeof(Avatar), false) as Avatar;
            EditorGUILayout.LabelField("Avatar Definition", "Copy From Other Avatar");
            EditorGUILayout.LabelField("Optimize Game Objects", "关闭");

            bool rigConfigurationValid =
                (_rigAnimationType == ModelImporterAnimationType.Generic ||
                 _rigAnimationType == ModelImporterAnimationType.Human) &&
                _sourceAvatar != null;

            if (!rigConfigurationValid)
            {
                EditorGUILayout.HelpBox("Rig 只支持 Generic 或 Humanoid，并且必须指定正式模型的 Avatar。", MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!rigConfigurationValid))
            {
                if (GUILayout.Button($"批量处理 Rig（{_fbxPaths.Count} 个）", GUILayout.Height(30f)))
                {
                    ApplyToAll("Rig", ApplyRigSettings, "Model、Materials 和 Animation 设置未修改。");
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void SetFolderFromSelection()
        {
            UnityEngine.Object selected = Selection.activeObject;
            string path = selected != null ? AssetDatabase.GetAssetPath(selected) : string.Empty;
            if (AssetDatabase.IsValidFolder(path))
            {
                _folder = selected as DefaultAsset;
                RefreshPaths();
                return;
            }

            EditorUtility.DisplayDialog("未选择文件夹", "请在 Project 窗口选中 Assets 下的文件夹。", "确定");
        }

        private void RefreshPaths()
        {
            _fbxPaths.Clear();
            string folderPath = _folder != null ? AssetDatabase.GetAssetPath(_folder) : string.Empty;
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Repaint();
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]).Replace('\\', '/');
                if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!_includeSubfolders && !string.Equals(Path.GetDirectoryName(path)?.Replace('\\', '/'), folderPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                _fbxPaths.Add(path);
            }

            _fbxPaths.Sort(StringComparer.OrdinalIgnoreCase);
            Repaint();
        }

        private void ApplyToAll(string sectionName, Action<ModelImporter> applySettings, string untouchedSettingsMessage)
        {
            if (!EditorUtility.DisplayDialog(
                    "确认批量重新导入",
                $"将批量处理 {_fbxPaths.Count} 个 FBX 的 {sectionName} 设置并重新导入。\n\n" +
                "该操作可能耗时并增加 Library/Artifacts，请确认磁盘空间充足。",
                    "开始处理",
                    "取消"))
            {
                return;
            }

            int changedCount = 0;
            int failedCount = 0;
            bool canceled = false;
            try
            {
                for (int i = 0; i < _fbxPaths.Count; i++)
                {
                    string path = _fbxPaths[i];
                    if (EditorUtility.DisplayCancelableProgressBar(
                            $"批量处理 FBX {sectionName}",
                            $"{i + 1}/{_fbxPaths.Count}  {path}",
                            (i + 1f) / _fbxPaths.Count))
                    {
                        canceled = true;
                        break;
                    }

                    if (!(AssetImporter.GetAtPath(path) is ModelImporter importer))
                    {
                        failedCount++;
                        continue;
                    }

                    try
                    {
                        applySettings(importer);
                        importer.SaveAndReimport();
                        changedCount++;
                    }
                    catch (Exception exception)
                    {
                        failedCount++;
                        Debug.LogError($"FBX {sectionName} 批处理失败：{path}\n{exception}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog(
                "处理完成",
                $"处理类型：{sectionName}\n成功重新导入：{changedCount}\n失败：{failedCount}\n" +
                $"是否取消：{(canceled ? "是" : "否")}\n\n{untouchedSettingsMessage}",
                "确定");
        }

        private static void ApplyModelSettings(ModelImporter importer)
        {
            importer.globalScale = 1f;
            importer.useFileUnits = true;
            importer.bakeAxisConversion = false;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.preserveHierarchy = false;
            importer.sortHierarchyByName = true;

            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.isReadable = false;
            importer.addCollider = false;
            importer.keepQuads = false;
            importer.weldVertices = true;
            importer.indexFormat = ModelImporterIndexFormat.Auto;
            importer.importNormals = ModelImporterNormals.None;
            importer.importTangents = ModelImporterTangents.None;
            importer.swapUVChannels = false;
            importer.generateSecondaryUV = false;

            // 部分 Unity 版本没有公开 Deform Percent 与 Strict Vertex Data Checks 的 API，
            // 使用序列化字段兼容处理；字段不存在时安全跳过。
            SerializedObject serializedImporter = new SerializedObject(importer);
            SetOptionalBool(serializedImporter, "m_ImportDeformPercent", false);
            SetOptionalBool(serializedImporter, "m_StrictVertexDataChecks", false);
            SetOptionalBool(serializedImporter, "m_OptimizeMeshPolygons", true);
            SetOptionalBool(serializedImporter, "m_OptimizeMeshVertices", true);
            serializedImporter.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyMaterialsSettings(ModelImporter importer)
        {
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
        }

        private void ApplyRigSettings(ModelImporter importer)
        {
            importer.animationType = _rigAnimationType;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = _sourceAvatar;
            importer.optimizeGameObjects = false;
        }

        private static void SetOptionalBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null && property.propertyType == SerializedPropertyType.Boolean)
            {
                property.boolValue = value;
            }
        }
    }
}
