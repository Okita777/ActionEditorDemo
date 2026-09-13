using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    [CustomEditor(typeof(ActionEngine_HitBoxs))]

    public class ActionEngine_Collider_Editor : UnityEditor.Editor
    {
        private ActionEngine_HitBoxs main => target as ActionEngine_HitBoxs;
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("添加受击盒"))
            {
                main.mAllgitBoxName.Add("Default");
                main.mAllHitBox.Add(null);
                EditorUtility.SetDirty(main);
            }

            for (int i = 0; i < main.mAllgitBoxName.Count; i++)
            {
                using (new GUILayout.HorizontalScope())
                {
                    using (var _check = new EditorGUI.ChangeCheckScope())
                    {
                        main.mAllHitBox[i] = (Collider)EditorGUILayout.ObjectField(main.mAllHitBox[i], typeof(Collider));
                        main.mAllgitBoxName[i] = EditorGUILayout.TextField(main.mAllgitBoxName[i]);
                        if (_check.changed)
                        {
                            EditorUtility.SetDirty(main);
                        }
                    }

                    using (new GUIColorScope(Color.red))
                    {
                        if (GUILayout.Button("-"))
                        {
                            main.mAllgitBoxName.RemoveAt(i);
                            main.mAllHitBox.RemoveAt(i);
                            EditorUtility.SetDirty(main);
                        }
                    }
                }
            }
        }
    }
}