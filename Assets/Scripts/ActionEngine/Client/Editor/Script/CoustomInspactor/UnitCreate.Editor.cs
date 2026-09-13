using AsiActionEngine.Editor;
using AsiTimeLine.RunTime;
using UnityEditor;

namespace AsiTimeLine.Editor
{
    [CustomEditor(typeof(UnitCreate))]
    public class UnitCreateEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawEditorAttribute.Draw(target);
        }
    }
}