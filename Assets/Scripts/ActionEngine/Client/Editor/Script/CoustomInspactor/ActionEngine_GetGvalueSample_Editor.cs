using AsiActionEngine.Editor;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    [CustomEditor(typeof(ActionEngine_GetGvalueSample))]
    public class ActionEngine_GetGvalueSample_Editor : UnityEditor.Editor
    {
        ActionEngine_GetGvalueSample main => target as ActionEngine_GetGvalueSample;
        public override void OnInspectorGUI()
        {
            //必须在参数变动后记录为脏   不然unity不会缓存数据
            using (var chack = new EditorGUI.ChangeCheckScope())
            {
                DrawEditorAttribute.Draw(main);//绘制所有的 EditorProperty 
                //DrawEditorAttribute.Draw(main,new []{"mGFloat", "mGInt"});//仅按名称绘制 EditorProperty元素
                if (chack.changed)
                {
                    EditorUtility.SetDirty(main);
                }
            }


            GUILayout.Space(10);
            GUILayout.Label("GFloat: " + main.m_DisPlayGFloat);
            GUILayout.Label("GInt: " + main.m_DisPlayGInt);
        }
    }
}