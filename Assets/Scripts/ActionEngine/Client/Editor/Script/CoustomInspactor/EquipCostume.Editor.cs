using System.Collections.Generic;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    [CustomEditor(typeof(EquipCostume))]
    public class EquipCostume_Editor : UnityEditor.Editor
    {
        private ReorderableList ReorderableList;
        private List<GameObject> ClothObjects = new List<GameObject>();
        private EquipCostume main => target as EquipCostume;
        private void OnEnable()
        {
            ClothObjects.Clear();
            foreach (var _colthPath in main.mColthPaths)
            {
                if (string.IsNullOrEmpty(_colthPath.mPath))
                {
                    ClothObjects.Add(null);
                }
                else
                {
                    ClothObjects.Add(Resources.Load(_colthPath.mPath) as GameObject);
                }
            }
            ReorderableList = new ReorderableList(main.mColthPaths, null, true, true, true, true);
            ReorderableList.drawHeaderCallback = rect => { GUI.Label(rect, "服装列表"); };
            ReorderableList.elementHeight = 20;
            ReorderableList.drawElementCallback = ((rect, index, active, focused) =>
            {
                Rect _nameRect = new Rect(rect);
                _nameRect.height = 18;
                _nameRect.y += 2;
                _nameRect.width = rect.width - 152;
                using (var _check = new EditorGUI.ChangeCheckScope())
                {
                    string _name = EditorGUI.TextField(_nameRect, main.mColthPaths[index].mName);
                    if (_check.changed)
                    {
                        main.mColthPaths[index] = new EquipCostume.ColthPaths(_name, main.mColthPaths[index].mPath);
                        EditorUtility.SetDirty(main);
                    }
                }

                Rect _pathRect = new Rect(_nameRect);
                _pathRect.x += _nameRect.width + 2;
                _pathRect.width = 150;
                using (var _check = new EditorGUI.ChangeCheckScope())
                {
                    GameObject _object =
                        (GameObject)EditorGUI.ObjectField(_pathRect, ClothObjects[index], typeof(GameObject), true);
                    if (_check.changed)
                    {
                        string _path = string.Empty;
                        if (_object is not null)
                        {
                            _path = AssetDatabase.GetAssetPath(_object);

                            //string[] _checkPath = _str.Split('/');
                            //if (_checkPath.Length > 1 && _checkPath[1] == "Resources")
                            {
                                //_str = "";
                                //for (int i = 2; i < _checkPath.Length-1; i++)
                                //{
                                //    _str += _checkPath[i] + "/";
                                //}
                                //_str += _checkPath[^1].Split('.')[0];

                                ClothObjects[index] = _object;
                                _path = EngineResourcesManager.Instance.GetRuntimePath(_path);
                                main.mColthPaths[index] = new EquipCostume.ColthPaths(main.mColthPaths[index].mName, _path);
                                EditorUtility.SetDirty(main);
                            }
                            //else
                            //{
                            //    EditorUtility.DisplayDialog("警告", 
                            //        "请拖入Resources文件夹下的资源, 并确保这个Resources文件夹在根目录", "我知道了");
                            //}
                        }
                        else
                        {

                        }
                    }
                }


            });
            ReorderableList.onAddCallback = list =>
            {
                main.mColthPaths.Add(new EquipCostume.ColthPaths("Item", string.Empty));
                ClothObjects.Add(null);
                EditorUtility.SetDirty(main);
            };
            ReorderableList.onRemoveCallback = list =>
            {
                main.mColthPaths.RemoveAt(list.index);
                ClothObjects.RemoveAt(list.index);
                EditorUtility.SetDirty(main);
            };
        }

        public override void OnInspectorGUI()
        {
            ReorderableList.DoLayoutList();
        }
    }
}