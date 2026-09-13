using AsiActionEngine.Editor;
using AsiTimeLine.RunTime;
using UnityEditor;

namespace AsiTimeLine.Editor
{
    [CustomEditor(typeof(CinemachineZoom))]
    public class CinemachineZoom_Editor : UnityEditor.Editor
    {
        CinemachineZoom main => target as CinemachineZoom;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawEditorAttribute.Draw(main);
        }
    }
}