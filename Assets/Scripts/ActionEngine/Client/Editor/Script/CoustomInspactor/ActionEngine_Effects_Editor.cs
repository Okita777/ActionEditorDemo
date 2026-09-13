using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    [CustomEditor(typeof(ActionEngine_Effects))]
    public class ActionEngine_Effects_Editor : UnityEditor.Editor
    {
        ActionEngine_Effects _main => target as ActionEngine_Effects;
        ReorderableList _reorderableList;
        private void OnEnable()
        {
            _reorderableList = new ReorderableList(_main.particleSystems, null, true, true, true, true);
            _reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    _main.particleSystems[index] = (ParticleSystem)EditorGUI.ObjectField(rect, _main.particleSystems[index], typeof(ParticleSystem), false);
                    if (check.changed)
                    {
                        EditorUtility.SetDirty(_main);
                        AssetDatabase.SaveAssetIfDirty(_main);
                    }
                }
            };
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("自动设定粒子列表"))
            {
                _main.particleSystems.Clear();

                foreach (var VARIABLE in _main.transform.GetComponentsInChildren<ParticleSystem>(true))
                {
                    VARIABLE.playOnAwake = false;
                    _main.particleSystems.Add(VARIABLE);
                }

                for (int i = 0; i < _main.particleSystems.Count; i++)
                {
                    ParticleSystem _particleSystem = _main.particleSystems[i];
                    Transform transform = _particleSystem.transform;

                    if (transform.parent && transform.parent.GetComponentsInParent<ParticleSystem>().Length > 0)
                    {
                        _main.particleSystems.RemoveAt(i);
                        i--;
                    }
                }

                EditorUtility.SetDirty(_main);
                AssetDatabase.SaveAssetIfDirty(_main);
            }

            _reorderableList.DoLayoutList();
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                _main.Life = EditorGUILayout.DelayedFloatField("寿命: ", _main.Life);
                if (check.changed)
                {
                    EditorUtility.SetDirty(_main);
                    AssetDatabase.SaveAssetIfDirty(_main);
                }
            }
        }
    }
}