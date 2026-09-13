using AsiActionEngine.Editor;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    [CustomEditor(typeof(ActionEngine_InteractRayCast))]
    public class ActionEngine_InteractRayCast_Editor : UnityEditor.Editor
    {
        private ActionEngine_InteractRayCast main => target as ActionEngine_InteractRayCast;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            GUILayout.Label("交互数据写入", EditorStyles.boldLabel);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                DrawEditorAttribute.Draw(main);
                if (check.changed)
                {
                    EditorUtility.SetDirty(main);
                }
            }
        }
    }
}
