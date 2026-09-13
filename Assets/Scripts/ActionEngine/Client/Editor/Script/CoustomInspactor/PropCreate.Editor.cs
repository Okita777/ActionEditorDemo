using AsiActionEngine.Editor;
using AsiTimeLine.RunTime;
using UnityEditor;

namespace AsiTimeLine.Editor
{
    [CustomEditor(typeof(PropCreate))]
    public class PropCreateEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawEditorAttribute.Draw(target);
        }
    }
}