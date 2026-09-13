using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    [CustomEditor(typeof(ActionEngine_SwitchWeapon))]
    public class ActionEngine_SwitchWeapon_Editor : UnityEditor.Editor
    {
        private Vector2 mScrollPos = Vector2.zero;
        private ActionEngine_Unit mPlayer => ActionEngineManager_Input.Instance.Player;
        private ActionEngine_SwitchWeapon main => target as ActionEngine_SwitchWeapon;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.Space(10);

            using (var _scroll = new GUILayout.ScrollViewScope(mScrollPos))
            {
                mScrollPos = _scroll.scrollPosition;
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    if (GUILayout.Button("添加武器"))
                    {
                        main.mWeaponeList.Add(0);
                    }
                    for (int i = 0; i < main.mWeaponeList.Count; i++)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            int _index = i;
                            GUILayout.Label(_index.ToString(), GUILayout.Width(20));
                            DrawEditorAttribute.DrawProp(main.mWeaponeList[_index], id => { main.mWeaponeList[_index] = id; });
                            using (new GUIColorScope(Color.red))
                            {
                                if (GUILayout.Button("-", GUILayout.Width(20)))
                                {
                                    main.mWeaponeList.RemoveAt(_index);
                                    i--;
                                }
                            }
                        }
                    }

                    if (check.changed)
                    {
                        EditorUtility.SetDirty(main);
                    }
                }

                if (mPlayer is ActionEngine_Entity _Entity)
                {
                    string weaponList = "已装备的武器：";
                    foreach (var item in _Entity.mPropDic_Slot)
                    {
                        weaponList += $"\nID:[{item.Value.mPropWarp.ID}]  Type:[{item.Value.mPropWarp.PropType.mSerValue}]";

                        if (ResourcesWindow.Instance.DicPropWarp.TryGetValue(item.Value.mPropWarp.ID, out EditorPropWarp _unit))
                        {
                            weaponList += $"  {_unit.Name}";
                        }
                    }
                    EditorGUILayout.HelpBox(weaponList, MessageType.Info);
                }
                else
                {
                    if (Application.isPlaying)
                    {
                        EditorGUILayout.HelpBox("当前无操作对象，或者操作对象不为[ActionEngine_Entity]", MessageType.Error);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("仅RunTime下可更新武器装备列表显示", MessageType.Warning);
                    }
                }
            }
        }
    }
}