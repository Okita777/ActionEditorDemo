using System;
using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    [CustomEditor(typeof(ActionEngine_SwitchActionInfo))]
    public class ActionEngine_SwitchActionInfo_Editor : UnityEditor.Editor
    {
        private Vector2 mScrollPos = Vector2.zero;
        private ActionEngine_Unit mPlayer => ActionEngineManager_Input.Instance.Player;
        private ActionEngine_SwitchActionInfo main => target as ActionEngine_SwitchActionInfo;
        private ReorderableList mReorderableList;

        private void OnEnable()
        {
            mReorderableList = new ReorderableList(main.ActionInfoID, null, true, true, true, true);
            mReorderableList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "动作模组列表 (ActionInfoID)");
            };
            mReorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 6f;
            mReorderableList.drawElementCallback = OnDrawElement;
            mReorderableList.onAddCallback = list =>
            {
                main.ActionInfoID.Add(0);
                EditorUtility.SetDirty(main);
            };
            mReorderableList.onRemoveCallback = list =>
            {
                main.ActionInfoID.RemoveAt(list.index);
                EditorUtility.SetDirty(main);
            };
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= main.ActionInfoID.Count)
            {
                return;
            }

            rect.y += 2f;
            float lineH = EditorGUIUtility.singleLineHeight;
            const float intFieldW = 100f;
            const float gap = 4f;

            Rect intRect = new Rect(rect.x, rect.y, intFieldW, lineH);
            Rect dropdownRect = new Rect(rect.x + intFieldW + gap, rect.y, rect.width - intFieldW - gap, lineH);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                int newVal = EditorGUI.IntField(intRect, main.ActionInfoID[index]);
                if (check.changed)
                {
                    main.ActionInfoID[index] = newVal;
                    EditorUtility.SetDirty(main);
                }
            }

            int curId = main.ActionInfoID[index];
            ActionInfoDropdownHelper.DrawActionInfoDropdown(dropdownRect, curId, id =>
            {
                main.ActionInfoID[index] = id;
                EditorUtility.SetDirty(main);
            });

            if (index == main.mSwitchID)
            {
                Rect highlight = new Rect(rect.x, rect.y, rect.width, lineH);
                EditorGUI.DrawRect(highlight, Color.red * 0.3f);
            }
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Space(10);
            using (var _scroll = new GUILayout.ScrollViewScope(mScrollPos))
            {
                mScrollPos = _scroll.scrollPosition;
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    base.OnInspectorGUI();

                    GUILayout.Space(6);
                    mReorderableList.DoLayoutList();

                    if (check.changed)
                    {
                        EditorUtility.SetDirty(main);
                    }
                }

                ActionEngine_Entity entity = mPlayer is ActionEngine_Entity e ? e : null;
                EquippedActionModulesInspectorHelper.DrawEquippedModulesHelpBox(entity);
            }

            GUI.changed = true;
        }
    }

    /// <summary>
    /// Rect 版 ActionInfo 下拉（替代 <c>DrawEditorAttribute.DrawAtionList</c>），可在 ReorderableList 等 Rect 绘制回调中使用。
    /// </summary>
    public static class ActionInfoDropdownHelper
    {
        public static void DrawActionInfoDropdown(Rect rect, int currentId, Action<int> onSelect)
        {
            bool isValid = ResourcesWindow.Instance.TryGetActionGroup(currentId, out EditorActionStateInfo unit);
            string label = isValid ? $"[{currentId}] {unit.Name}" : "已丢失ActionInfo索引，请重新选择";

            Color prevColor = GUI.color;
            if (!isValid)
            {
                GUI.color = Color.red;
            }

            if (GUI.Button(rect, label, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();
                foreach (int id in ResourcesWindow.Instance.AllActionGroup_ID)
                {
                    EditorActionStateInfo info = ResourcesWindow.Instance.GetActionGroup(id);
                    int capturedId = id;
                    menu.AddItem(new GUIContent($"{info.Name}  [{id}]"), id == currentId, () =>
                    {
                        onSelect(capturedId);
                    });
                }

                menu.ShowAsContext();
            }

            GUI.color = prevColor;
        }
    }

    /// <summary>
    /// 运行时展示 <see cref="ActionEngine_Entity"/> 已装备动作模组（与 ResourcesWindow 名称一致），供多个 CustomEditor 复用。
    /// </summary>
    public static class EquippedActionModulesInspectorHelper
    {
        public static void DrawEquippedModulesHelpBox(ActionEngine_Entity entity)
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("仅 RunTime 下可更新 Action 装备列表显示", MessageType.Warning);
                return;
            }

            if (entity == null)
            {
                EditorGUILayout.HelpBox("当前无操作对象，或者操作对象不为[ActionEngine_Entity]", MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(BuildEquippedModulesText(entity), MessageType.Info);
        }

        public static void DrawEquippedModulesHelpBox(ActionEngine_Entity entity, string entityNullMessage)
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("仅 RunTime 下可更新 Action 装备列表显示", MessageType.Warning);
                return;
            }

            if (entity == null)
            {
                string msg = string.IsNullOrEmpty(entityNullMessage)
                    ? "当前无操作对象，或者操作对象不为[ActionEngine_Entity]"
                    : entityNullMessage;
                EditorGUILayout.HelpBox(msg, MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(BuildEquippedModulesText(entity), MessageType.Info);
        }

        public static string BuildEquippedModulesText(ActionEngine_Entity entity)
        {
            if (entity == null)
            {
                return string.Empty;
            }

            string weaponList = "已装备的模组：" +
                $"\n角色默认模组[{entity.ActionStateMachine.ActionGroupID}]   {entity.ActionStateMachine.ActionGroupName}";
            foreach (ActionStateInfo actionInfo in entity.ActionStateMachine.EquipActionInfoList)
            {
                weaponList += $"\nID:[{actionInfo.ActionGroupID}]";
                if (ResourcesWindow.Instance.TryGetActionGroup(actionInfo.ActionGroupID, out EditorActionStateInfo _unit))
                {
                    weaponList += $"  {_unit.Name}";
                }
            }

            return weaponList;
        }
    }
}
