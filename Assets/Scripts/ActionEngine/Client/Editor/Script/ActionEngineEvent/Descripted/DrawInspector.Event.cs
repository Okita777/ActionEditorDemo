using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEngine;

#if FMOD
using FMODUnity;
#endif


namespace AsiTimeLine.Editor
{
    public partial class DrawInspector
    {

        public static bool DrawEvent(EditorActionEvent _actionEvent, bool _isInit)
        {
            bool _isError = false;//是否存在错误
            IActionEventData _eventData = _actionEvent.EventData;
            ResourcesWindow.Instance.Active_EditorActionEvent = _actionEvent;
            ResourcesWindow.Instance.Active_ActionEventData = _eventData;

            EEvenType _evenType = (EEvenType)_eventData.GetEvenType();

            DrawNames.Clear();

            using (new GUIColorScope(Color.gray))
            {
                GUILayout.Label(_evenType.ToString());
            }
            GUILayout.Space(10);

            //属性面板绘制
            switch (_evenType)
            {
                //在这里决定事件的属性面板绘制,示例如下
                //case EEvenType.EET_CameraChange:
                //    DrawCameraChange(_actionEvent);
                //    break;

                default:
                    //这里是具体示例事件
                    DrawEventSampleEvent(_actionEvent, _eventData, _evenType, _isInit, out _isError);
                    break;
            }
            return _isError;
        }

    }
}