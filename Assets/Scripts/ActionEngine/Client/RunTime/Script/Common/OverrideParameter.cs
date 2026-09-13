// using UnityEngine;
//
// #if UNITY_EDITOR
// using UnityEditor;
// #endif
//
// namespace ActionEngine.Runtime
// {
//     [System.Serializable]
//     public class OverrideParameter<T>
//     {
//         public T value;
//         public bool overrideState;
//
//         public OverrideParameter()
//         {
//             value = default;
//             overrideState = false;
//         }
//
//         public OverrideParameter(T value, bool overrideState = false)
//         {
//             this.value = value;
//             this.overrideState = overrideState;
//         }
//
//         public void SetParameter(T value)
//         {
//             overrideState = true;
//             this.value = value;
//         }
//
//         public void TryOverride(ref T value)
//         {
//             if (overrideState)
//             {
//                 value = this.value;
//             }
//         }
//     }
//     
// #if UNITY_EDITOR
//     [CustomPropertyDrawer(typeof(OverrideParameter<>))]
//     public class OverrideParameterDrawer : PropertyDrawer
//     {
//         public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//         {
//             EditorGUI.BeginProperty(position, label, property);
//             
//             SerializedProperty overrideStateProperty = property.FindPropertyRelative("overrideState");
//             SerializedProperty valueProperty = property.FindPropertyRelative("value");
//
//             float line = EditorGUIUtility.singleLineHeight;
//             Rect toggleRect = new Rect(position.x, position.y, line, line);
//             overrideStateProperty.boolValue = EditorGUI.Toggle(toggleRect, overrideStateProperty.boolValue);
//
//             Rect labelRect = new Rect(position.x + line, position.y, EditorGUIUtility.labelWidth - line, line);
//             EditorGUI.LabelField(labelRect, label);
//
//             float offset = EditorGUIUtility.labelWidth + 2;
//             Rect valueRect = new Rect(position.x + offset, position.y, position.width - offset, line);
//             EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);
//
//             EditorGUI.EndProperty();
//         }
//     }
// #endif
// }
