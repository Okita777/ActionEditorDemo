using AsiActionEditor_Ex.RunTime;
using AsiActionEngine.RunTime;

namespace AsiTimeLine.RunTime
{
    public class Ex_CamChange : StaticActionLogics
    {
        private Event_CameraChange mEventCamera;
        private bool mIsInit = false;

        public override void OnStart(ActionStateMachine _actionState)
        {
            mIsInit = false;
        }

        public void UpdateCameEvent(Event_CameraChange _eventCamera)
        {
            mEventCamera = _eventCamera;
            mIsInit = true;
        }

        public void OnReset(ActionStatePart _part)
        {
            if (!mIsInit) return;
            mEventCamera.Enter(_part, false);
        }

    }
}