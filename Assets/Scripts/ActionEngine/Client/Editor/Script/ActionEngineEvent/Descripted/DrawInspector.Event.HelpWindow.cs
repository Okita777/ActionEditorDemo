using System;
using AsiActionEditor_Ex.RunTime;
using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;

namespace AsiTimeLine.Editor
{
    public partial class DrawInspectorHelpWindow
    {
        public static bool DrawEventHelpWindows(EditorActionEvent _actionEvent, out Action _drawCallBack)
        {
            _drawCallBack = null;
            IActionEventData _eventData = _actionEvent.EventData;

            EEvenType _evenType = (EEvenType)_eventData.GetEvenType();

            switch (_evenType)
            {
                case EEvenType.EET_CameraChange:
                    var camData = (Event_CameraChange)_eventData;
                    _drawCallBack = () => DrawCameraChange(camData);
                    break;

                case EEvenType.EET_Partocle:
                    var particleData = (Event_PlayParticle)_eventData;
                    _drawCallBack = () => DrawParticle(particleData);
                    break;
                case EEvenType.EET_WeaponTrail:
                    break;
                case EEvenType.EET_SimulatedInput:
                    break;

                case EEvenType.EET_SetAnimFloat:
                    break;
                case EEvenType.EET_UnitRot:
                    break;
                case EEvenType.EET_SetGValue:
                    break;
                case EEvenType.EET_RimLight:
                case EEvenType.EET_Dissolve:
                case EEvenType.EET_AfterImage:
                    break;
                default:
                    return false;

            }

            return true;
        }
    }
}
