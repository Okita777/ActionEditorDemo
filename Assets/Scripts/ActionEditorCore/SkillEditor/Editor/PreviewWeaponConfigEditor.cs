using SkillEditor.Preview;
using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    [CustomEditor(typeof(PreviewWeaponConfig))]
    public sealed class PreviewWeaponConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            PreviewWeaponConfig config = (PreviewWeaponConfig)target;

            EditorGUILayout.LabelField("预览武器挂点配置", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("这里用于配置武器上的攻击盒挂点、发射器挂点等命名节点。", MessageType.Info);
            EditorGUILayout.Space(6f);

            config.WeaponType = (AsiSkillEditor.RunTime.SkillWeaponType)EditorGUILayout.EnumPopup("武器类型", config.WeaponType);

            for (int i = 0; i < config.MountPoints.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                config.MountPoints[i].SocketName = EditorGUILayout.TextField("挂点名", config.MountPoints[i].SocketName);
                if (GUILayout.Button("-", GUILayout.Width(28f)))
                {
                    config.MountPoints.RemoveAt(i);
                    EditorUtility.SetDirty(config);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }

                EditorGUILayout.EndHorizontal();
                config.MountPoints[i].MountTransform =
                    EditorGUILayout.ObjectField("Transform", config.MountPoints[i].MountTransform, typeof(Transform), true) as Transform;
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("添加武器挂点", GUILayout.Height(28f)))
            {
                config.MountPoints.Add(new PreviewMountPoint());
                EditorUtility.SetDirty(config);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(config);
            }
        }
    }
}
