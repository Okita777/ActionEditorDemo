using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_PlayAnim : IActionEventData
    {
        [SerializeField] private string mAnimName = string.Empty;
        [SerializeField] private int mAnimLayer = 0;

        #region property
        public string AnimName
        {
            get { return mAnimName; }
            set { mAnimName = value; }
        }
        public int AnimLayer
        {
            get { return mAnimLayer; }
            set { mAnimLayer = value; }
        }
        #endregion


        public int GetEvenType() => -(int)EEvenTypeInternal.EET_DTD_PlayAnim;
        public IActionEventData Creact() => new Event_PlayAnim();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            float _doubling = MotionEngineConst.TimeDoubling;
            if (_actionState.OffsetTime < 0)
            {
                //按百分比跳转动画
                if (_actionState.CurrentActionState_Last == null)
                {
                    EngineDebug.LogWarning("CurrentActionState_Last");
                }
                float _offsetTime = _actionState.ElapsedTime_last / _actionState.CurrentActionState_Last.TotalTime;
                // EngineDebug.Log("动画百分比: " + _offsetTime);
                _offsetTime *= _actionState.CurrentActionState.TotalTime;
                _actionState.ElapsedTime = _offsetTime;

                float _jumpMix = (float)_actionState.MixTime / MotionEngineConst.TimeDoubling * -1;
                if (_offsetTime + _jumpMix * _doubling > _actionState.CurrentActionState.TotalTime)
                {
                    _actionState.IsJumpEnd = true;
                }//这次跳过结尾循环动画跳转
                _actionState.PlayAnim(mAnimName, _jumpMix,
                    mAnimLayer, _offsetTime / _doubling);
            }
            else
            {
                int _startTime = _actionState.OffsetTime - _actionState.CurrentActionState.AnimEvent.TriggerTime;
                _actionState.PlayAnim(mAnimName, _actionState.MixTime / _doubling,
                    mAnimLayer, _startTime / _doubling);
            }
            // _actionState.ActionStateMachine.CurAnimator.CrossFadeInFixedTime();
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_PlayAnim _event = _eventData as Event_PlayAnim;

            _event.AnimName = mAnimName;
            _event.AnimLayer = mAnimLayer;

            return _event;
        }
    }
}