using System.Collections.Generic;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    [CustomEditor(typeof(EquipCostume_Default))]
    public class EquipCostume_Default_Editor : UnityEditor.Editor
    {
        private EquipCostume_Default main => target as EquipCostume_Default;
        private ReorderableList mDisList;
        private List<UnitClothPool.ClothType> mClothType = new List<UnitClothPool.ClothType>();
        private List<string> mClothName = new List<string>();

        private void OnEnable()
        {
            mDisList = new ReorderableList(main.mClothType, null, true, true, true, true);
            mDisList.drawHeaderCallback = rect => { GUI.Label(rect, "默认加载的装备"); };
            mDisList.drawElementCallback = (rect, index, active, focused) =>
            {
                float _width = 80f;
                Rect _rect = new Rect(rect);
                _rect.y += 2f;
                _rect.width = _width;
                using (var _check = new EditorGUI.ChangeCheckScope())
                {
                    UnitClothPool.ClothType _clothType =
                        (UnitClothPool.ClothType)EditorGUI.EnumPopup(_rect, main.mClothType[index]);
                    if (_check.changed)
                    {
                        main.mClothType[index] = _clothType;
                        EditorUtility.SetDirty(main);
                    }
                }

                Rect _rect2 = new Rect(rect);
                _rect2.x += _rect.width + 2;
                _rect2.width = rect.width - _width - 2;
                using (var _check = new EditorGUI.ChangeCheckScope())
                {
                    string _clothName = EditorGUI.TextField(_rect2, main.mClothName[index]);
                    if (_check.changed)
                    {
                        main.mClothName[index] = _clothName;
                        EditorUtility.SetDirty(main);
                    }
                }
            };
            mDisList.onAddCallback = (list) =>
            {
                main.mClothType.Add(UnitClothPool.ClothType.手套);
                main.mClothName.Add("Default");
            };
            mDisList.onRemoveCallback = (list) =>
            {
                main.mClothType.RemoveAt(list.index);
                main.mClothName.RemoveAt(list.index);
            };
        }

        public override void OnInspectorGUI()
        {
            using (var _check = new EditorGUI.ChangeCheckScope())
            {
                GUIStyle _headText = new GUIStyle();
                _headText.alignment = TextAnchor.MiddleCenter;
                _headText.normal.textColor = Color.white;
                _headText.fontSize = 24;

                GUILayout.Space(10);
                GUILayout.Label("角色默认装扮设置", _headText);

                mDisList.DoLayoutList();

                if (_check.changed)
                {
                    EditorUtility.SetDirty(main);
                }
            }
        }
    }
}