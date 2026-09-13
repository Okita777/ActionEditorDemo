using AsiActionEngine.Editor;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Plugins.AsiActionEditor.Client.Editor.Script
{
    [CustomEditor(typeof(ActionEngine_GvalueGUI))]
    public class ActionEngine_GvalueGUI_Editor : UnityEditor.Editor
    {
        private ActionEngine_GvalueGUI main => (ActionEngine_GvalueGUI)target;

        private Rect _rect;
        public override void OnInspectorGUI()
        {
            main.mText = EditorGUILayout.ObjectField("文本显示(可为空)", main.mText, typeof(Text), true) as Text;
            main.mTrans = EditorGUILayout.ObjectField("UI条显示(可为空)", main.mTrans, typeof(RectTransform), true) as RectTransform;

            main.mIsDisFloat = EditorGUILayout.Toggle("GetFloat", main.mIsDisFloat);
            //GUILayout.Label("");
            //_rect = GUILayoutUtility.GetLastRect();
            if (main.mIsDisFloat) DrawEditorAttribute.Draw(main, new[] { "GFloat" });
            else DrawEditorAttribute.Draw(main, new[] { "GInt" });
            //if (main.mIsDisFloat) DrawEditorAttribute.DrawGValue(_rect, "GFloat", main.GFloat, EGValueType.GFloat);
            //else DrawEditorAttribute.DrawGValue(_rect, "GInt", main.GInt, EGValueType.GInt);

            GUILayout.Space(10);
            main.mValueRange = EditorGUILayout.Vector2Field("value值范围", main.mValueRange);
            main.mTransRange = EditorGUILayout.Vector2Field("UI宽度范围", main.mTransRange);
            main.mBoundRange = EditorGUILayout.Toggle("按范围限制UI宽度", main.mBoundRange);

            GUILayout.Space(10);
            main.mFillAmountTime = EditorGUILayout.FloatField("过渡时间", main.mFillAmountTime);
            main.mFillAmountSpeed = EditorGUILayout.FloatField("过渡速度倍率", main.mFillAmountSpeed);

        }
    }
}