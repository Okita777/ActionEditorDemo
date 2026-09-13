using AsiActionEngine.Editor;
using AsiTimeLine.RunTime;
using UnityEditor;

namespace AsiTimeLine.Editor
{
    [CustomEditor(typeof(ActionEngine_LockUnit))]
    public class ActionEngine_LockUnit_Editor : UnityEditor.Editor
    {

        public override void OnInspectorGUI()
        {
            using (var _chack = new EditorGUI.ChangeCheckScope())
            {
                DrawEditorAttribute.Draw(target);
                if (_chack.changed)
                {
                    EditorUtility.SetDirty(target);
                }
            }
            // base.OnInspectorGUI();
        }
    }
}