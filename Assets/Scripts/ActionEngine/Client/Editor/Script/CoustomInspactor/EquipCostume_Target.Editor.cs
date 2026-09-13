using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    [CustomEditor(typeof(EquipCostume_Target))]
    public class EquipCostume_Target_Editor : UnityEditor.Editor
    {
        private EquipCostume_Target main => target as EquipCostume_Target;
        public override void OnInspectorGUI()
        {
            GUILayout.Label("角色根骨");
            using (var _check = new EditorGUI.ChangeCheckScope())
            {
                main.AnimRoot = (Transform)EditorGUILayout.ObjectField(main.AnimRoot, typeof(Transform), true);
                if (_check.changed)
                {
                    EditorUtility.SetDirty(main);
                }
            }

            if (GUILayout.Button("自动配置骨骼"))
            {
                main.ClothBones.Clear();
                foreach (var VARIABLE in main.AnimRoot.GetComponentsInChildren<Transform>())
                {
                    if (!main.ClothBones.TryAdd(VARIABLE.name, VARIABLE))
                    {
                        EngineDebug.LogWarning($"重名了： {VARIABLE.name}");
                    }
                }
                EditorUtility.SetDirty(main);
            }

            GUILayout.Label("配置骨骼数量: " + main.ClothBones.Count);
        }
    }
}