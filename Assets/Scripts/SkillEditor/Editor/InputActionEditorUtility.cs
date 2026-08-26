using System;
using System.Collections.Generic;
using ActionEditor.InputSystem;
using UnityEditor;

namespace SkillEditor.Editor
{
    internal static class InputActionEditorUtility
    {
        public static string DrawActionPopup(string label, string currentActionName, bool allowNone = true)
        {
            List<string> actionNames = LoadActionNames();
            if (actionNames.Count == 0)
            {
                return EditorGUILayout.TextField(label, currentActionName ?? string.Empty);
            }

            int optionOffset = allowNone ? 1 : 0;
            string[] labels = new string[actionNames.Count + optionOffset];
            string[] values = new string[actionNames.Count + optionOffset];
            int currentIndex = 0;

            if (allowNone)
            {
                labels[0] = "未设置";
                values[0] = string.Empty;
            }

            for (int i = 0; i < actionNames.Count; i++)
            {
                int optionIndex = i + optionOffset;
                labels[optionIndex] = actionNames[i];
                values[optionIndex] = actionNames[i];
                if (string.Equals(currentActionName, actionNames[i], StringComparison.Ordinal))
                {
                    currentIndex = optionIndex;
                }
            }

            int nextIndex = EditorGUILayout.Popup(label, currentIndex, labels);
            return nextIndex >= 0 && nextIndex < values.Length ? values[nextIndex] : currentActionName ?? string.Empty;
        }

        private static List<string> LoadActionNames()
        {
            List<string> actionNames = new List<string>();
            CharacterInputMapConfig config = AssetDatabase.LoadAssetAtPath<CharacterInputMapConfig>(CharacterInputConstants.MainConfigAssetPath);
            if (config == null || config.Actions == null)
            {
                return actionNames;
            }

            HashSet<string> uniqueNames = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < config.Actions.Count; i++)
            {
                CharacterInputActionDefinition action = config.Actions[i];
                if (action == null || action.ValueType != CharacterInputActionValueType.Button || string.IsNullOrWhiteSpace(action.ActionName))
                {
                    continue;
                }

                if (uniqueNames.Add(action.ActionName))
                {
                    actionNames.Add(action.ActionName);
                }
            }

            return actionNames;
        }
    }
}