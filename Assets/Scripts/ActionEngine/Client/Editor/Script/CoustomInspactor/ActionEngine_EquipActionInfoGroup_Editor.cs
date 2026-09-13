using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    [CustomEditor(typeof(ActionEngine_EquipActionInfoGroup))]
    public class ActionEngine_EquipActionInfoGroup_Editor : UnityEditor.Editor
    {
        private Vector2 mScrollPos = Vector2.zero;
        private ActionEngine_EquipActionInfoGroup main => target as ActionEngine_EquipActionInfoGroup;
        private ReorderableList mReorderableList;

        private SerializedProperty m_TargetUnitProp;
        private SerializedProperty m_AutoEquipProp;
        private SerializedProperty m_UseKeyProp;
        private SerializedProperty m_EquipKeyProp;

        private static readonly Color SectionBgColor = new Color(0f, 0f, 0f, 0.06f);

        private static ActionEngine_Entity GetEntityFromTargetUnit(TargetUnit targetUnit)
        {
            if (targetUnit == null)
            {
                return null;
            }

            ActionEngine_Unit unit = targetUnit.GetUnit();
            if (unit == null)
            {
                return null;
            }

            return unit.GetSource as ActionEngine_Entity;
        }

        private void OnEnable()
        {
            m_TargetUnitProp = serializedObject.FindProperty("targetUnit");
            m_AutoEquipProp = serializedObject.FindProperty("AutoEquip");
            m_UseKeyProp = serializedObject.FindProperty("useKey");
            m_EquipKeyProp = serializedObject.FindProperty("equipKey");

            mReorderableList = new ReorderableList(main.ActionList, null, true, true, true, true);
            mReorderableList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "动作模组列表 (ActionList)");
            };
            mReorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 6f;
            mReorderableList.drawElementCallback = OnDrawElement;
            mReorderableList.onAddCallback = list =>
            {
                main.ActionList.Add(new EquipActionListEntry());
                EditorUtility.SetDirty(main);
            };
            mReorderableList.onRemoveCallback = list =>
            {
                main.ActionList.RemoveAt(list.index);
                EditorUtility.SetDirty(main);
            };
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= main.ActionList.Count)
            {
                return;
            }

            EquipActionListEntry entry = main.ActionList[index];
            if (entry == null)
            {
                return;
            }

            rect.y += 2f;
            float lineH = EditorGUIUtility.singleLineHeight;
            const float toggleW = 18f;
            const float intFieldW = 100f;
            const float slotIdFieldW = 50f;
            const float gap = 4f;

            Rect toggleRect = new Rect(rect.x, rect.y, toggleW, lineH);
            Rect intRect = new Rect(rect.x + toggleW + gap, rect.y, intFieldW, lineH);
            Rect slotIdRect = new Rect(rect.xMax - slotIdFieldW, rect.y, slotIdFieldW, lineH);
            float dropdownX = intRect.xMax + gap;
            Rect dropdownRect = new Rect(dropdownX, rect.y, slotIdRect.x - dropdownX - gap, lineH);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                bool newEnabled = EditorGUI.Toggle(toggleRect, entry.enabled);
                int newVal = EditorGUI.IntField(intRect, entry.actionGroupId);
                int newSlotID = EditorGUI.IntField(slotIdRect, entry.SlotID);
                if (check.changed)
                {
                    entry.enabled = newEnabled;
                    entry.actionGroupId = newVal;
                    entry.SlotID = newSlotID;
                    EditorUtility.SetDirty(main);
                }
            }

            ActionInfoDropdownHelper.DrawActionInfoDropdown(dropdownRect, entry.actionGroupId, id =>
            {
                entry.actionGroupId = id;
                EditorUtility.SetDirty(main);
            });
        }

        private static void BeginSection(string title)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Rect headerRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.boldLabel);
            EditorGUI.DrawRect(headerRect, SectionBgColor);
            EditorGUI.LabelField(headerRect, title, EditorStyles.boldLabel);
            GUILayout.Space(2f);
        }

        private static void EndSection()
        {
            EditorGUILayout.EndVertical();
            GUILayout.Space(4f);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(6);
            using (var _scroll = new GUILayout.ScrollViewScope(mScrollPos))
            {
                mScrollPos = _scroll.scrollPosition;

                // ── 目标与启动 ──
                BeginSection("目标与启动");
                EditorGUILayout.PropertyField(m_TargetUnitProp,
                    new GUIContent("目标单位", "为空时 Start 通过 WaitPlayerLoad 自动等待玩家单位赋值。"));
                EditorGUILayout.PropertyField(m_AutoEquipProp,
                    new GUIContent("启动时自动装备", "勾选后 Start 中 TargetUnit 就绪即自动调用一次 EquipActionInfo。"));
                EndSection();

                // ── 快捷键 ──
                BeginSection("快捷键");
                EditorGUILayout.PropertyField(m_UseKeyProp,
                    new GUIContent("启用快捷键", "勾选后运行时按下「装备快捷键」可触发 EquipActionInfo。"));
                EditorGUI.BeginDisabledGroup(!m_UseKeyProp.boolValue);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_EquipKeyProp,
                    new GUIContent("装备快捷键", "仅在「启用快捷键」开启时生效。"));
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();
                EndSection();

                // ── 动作模组列表 ──
                BeginSection("动作配置");
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    mReorderableList.DoLayoutList();
                    if (check.changed)
                    {
                        EditorUtility.SetDirty(main);
                    }
                }

                EndSection();

                // ── 已装备模组（运行时） ──
                BeginSection("已装备的模组（运行时）");
                ActionEngine_Entity entity = GetEntityFromTargetUnit(main.targetUnit);
                EquippedActionModulesInspectorHelper.DrawEquippedModulesHelpBox(
                    entity,
                    "目标单位未就绪、未赋值，或 GetSource 不是 ActionEngine_Entity，无法显示。");
                EndSection();

                // ── 调试 ──
                BeginSection("调试");
                EditorGUI.BeginDisabledGroup(!Application.isPlaying);
                if (GUILayout.Button("执行 EquipActionInfo", GUILayout.Height(26f)))
                {
                    if (main != null)
                    {
                        main.EquipActionInfo();
                    }
                }

                EditorGUI.EndDisabledGroup();
                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("运行模式下可用：需 TargetUnit 已就绪且 ID 有效。", MessageType.None);
                }

                EndSection();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
