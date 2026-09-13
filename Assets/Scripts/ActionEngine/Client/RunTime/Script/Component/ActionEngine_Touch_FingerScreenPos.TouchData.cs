using System.Collections.Generic;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public partial class ActionEngine_Touch_FingerScreenPos : MonoBehaviour
    {
        // public Text debug;
        //注册手指ID
        private void OnEnrollFinger(Touch _finger)
        {
            foreach (FingerInputData _fingerData in mFingerInputData)
            {
                Rect _mRange = _fingerData.mFingerRange;
                _mRange.x = Screen.width * _mRange.x / 100;
                _mRange.y = Screen.height * _mRange.y / 100;
                _mRange.width = Screen.width * _mRange.width / 100;
                _mRange.height = Screen.height * _mRange.height / 100;

                if (_mRange.Contains(_finger.position))
                {
                    if (_fingerData.fingerID_1 < 0)
                    {
                        _fingerData.SetFinger_1(_finger.fingerId, _finger.position, true);
                    }
                    else if (_fingerData.fingerID_2 < 0)
                    {
                        _fingerData.SetFinger_1(_finger.fingerId, _finger.position, false);
                    }
                }
            }
        }

        //移除手指ID
        private void ReMoveFinger(Touch _finger)
        {
            foreach (FingerInputData _fingerData in mFingerInputData)
            {
                if (_finger.fingerId == _fingerData.fingerID_1)
                {
                    _fingerData.SetFinger_1(-1, _finger.position, true);
                }
                if (_finger.fingerId == _fingerData.fingerID_2)
                {
                    _fingerData.SetFinger_1(-1, _finger.position, false);
                }
            }
        }

        //手指移动时
        private void MoveFinger(Touch _finger)
        {
            foreach (FingerInputData _fingerData in mFingerInputData)
            {
                if (_finger.fingerId == _fingerData.fingerID_1)
                {
                    _fingerData.MoveFinger(_finger, true);
                }
                else if (_finger.fingerId == _fingerData.fingerID_2)
                {
                    _fingerData.MoveFinger(_finger, false);
                }
            }
        }
    }

    [System.Serializable]
    public class FingerInputData
    {
        #region Enum
        public enum EFingerInputType
        {
            Move,
            Look,
            None
            // Action,
        }
        public enum ETouchType
        {
            Down,
            Up,
            Click,
            Hold,
            Slid,
            Slid_Up,
            Slid_Down,
            Slid_Left,
            Slid_Right
        }

        #endregion

        public string mName = "Default";

        [Header("3d场景可交互层级")]
        [Header("尝试从点击对象获取【IAction_TuochEvent】接口并执行【OnTouch】事件")]
        public LayerMask mRayMask;
        private float mClikTime;
        private float mMoveRange;
        private float mViewInputSpeed = 1;
        private float mMoveInputLength = 1;
        private float mFingerTime_1, mFingerTime_2;
        private Vector2 mStartPos_1, mStartPos_2;
        private bool mFingerMove_1 = false, mFingerMove_2 = false;
        private bool isInteract = false;

        [Space(10)]
        public EFingerInputType mFingerInputType = EFingerInputType.Move;

        public Rect mFingerRange = new Rect(25, 25, 50, 50);

        public bool mCameZomm = false;

        [HideInInspector] public int fingerID_1 = -1;
        [HideInInspector] public int fingerID_2 = -1;
        public TouchActionData[] mTouchActionData = new TouchActionData[0];

        private Dictionary<ETouchType, ActionData> mTouchAction = new Dictionary<ETouchType, ActionData>();
        private float mFingerDis_Init;

        public void Init(float _ClikTime, float _viewInputSpeed, float _moveInputSpeed, float _moveRange)
        {
            mClikTime = _ClikTime;
            mViewInputSpeed = _viewInputSpeed * 0.1f;
            mMoveInputLength = _moveInputSpeed * 0.1f;
            mMoveRange = _moveRange;
            fingerID_1 = -1;
            fingerID_2 = -1;
            for (int i = 0; i < mTouchActionData.Length; i++)
            {
                mTouchAction.Add(mTouchActionData[i].mTouchType, mTouchActionData[i].mActionData);
            }
        }

        #region StartAndEnd
        public void SetFinger_1(int _id, Vector2 _pos, bool _isID1)
        {
            if (_id < 0)
            {//移除
                bool _FingerMove = _isID1 ? mFingerMove_1 : mFingerMove_2;
                Vector2 _iputDir = _pos - (_isID1 ? mStartPos_1 : mStartPos_2);
                // bool _isMove = _iputDir.sqrMagnitude > (mMoveRange * mMoveRange);
                bool isSlidDir = false;

                if ((_isID1 ? mFingerTime_1 : mFingerTime_2) > 0)
                {
                    if (_FingerMove)
                    {
                        if (mTouchAction.TryGetValue(ETouchType.Slid_Up, out ActionData _actionSlid_U))
                        {
                            if (Vector2.Angle(_iputDir, Vector2.up) <= 45)
                            {
                                isSlidDir = true;
                                SendAction(_actionSlid_U.mActionName, _actionSlid_U.MButtonInputType);
                            }
                        }
                        if (!isSlidDir && mTouchAction.TryGetValue(ETouchType.Slid_Down, out ActionData _actionSlid_D))
                        {
                            if (Vector2.Angle(_iputDir, Vector2.down) <= 45)
                            {
                                isSlidDir = true;
                                SendAction(_actionSlid_D.mActionName, _actionSlid_D.MButtonInputType);
                            }
                        }
                        if (!isSlidDir && mTouchAction.TryGetValue(ETouchType.Slid_Left, out ActionData _actionSlid_L))
                        {
                            if (Vector2.Angle(_iputDir, Vector2.left) < 45)
                            {
                                isSlidDir = true;
                                SendAction(_actionSlid_L.mActionName, _actionSlid_L.MButtonInputType);
                            }
                        }
                        if (!isSlidDir && mTouchAction.TryGetValue(ETouchType.Slid_Right, out ActionData _actionSlid_R))
                        {
                            if (Vector2.Angle(_iputDir, Vector2.right) < 45)
                            {
                                isSlidDir = true;
                                SendAction(_actionSlid_R.mActionName, _actionSlid_R.MButtonInputType);
                            }
                        }

                        if (!isSlidDir)
                        {
                            if (mTouchAction.TryGetValue(ETouchType.Slid, out ActionData _actionSlid))
                            {
                                //滑动
                                SendAction(_actionSlid.mActionName, _actionSlid.MButtonInputType);
                            }
                        }
                    }
                    else
                    {
                        //点击

                        if (!isInteract)
                        {
                            if (mTouchAction.TryGetValue(ETouchType.Click, out ActionData _action))
                            {
                                SendAction(_action.mActionName, _action.MButtonInputType);
                            }
                        }
                    }
                }

                if (mFingerInputType == EFingerInputType.Move)
                {
                    ActionEngineManager_Input.Instance.Input_Move(Vector2.zero);
                }

                //松开
                if (mTouchAction.TryGetValue(ETouchType.Up, out ActionData _action_up))
                {
                    SendAction(_action_up.mActionName, _action_up.MButtonInputType);
                }
                if (mTouchAction.TryGetValue(ETouchType.Hold, out ActionData _actionUpH))
                {
                    foreach (var VARIABLE in ActionEngineManager_Input.Instance.Player.ActionStateMachine.AllActionStatePart)
                    {
                        if (VARIABLE.NowInputHoldKey == _actionUpH.mActionName)
                        {
                            VARIABLE.NowInputHoldKey = "";
                        }
                    }
                }

                if (_isID1)
                {
                    mFingerTime_1 = -1;
                }
                else
                {
                    mFingerTime_2 = -1;

                }
            }
            else
            {
                //是否为场景交互
                isInteract = false;
                if (mRayMask.value != 0)
                {
                    Ray ray = ActionEngineManager_Input.Instance.PlayerCam.ScreenPointToRay(_pos);
                    if (Physics.Raycast(ray, out RaycastHit hit, 99999, mRayMask))
                    {
                        // hit.transform.SendMessage("OnTuochEvent");
                        if (hit.transform.TryGetComponent(out IAction_TouchEvent actionTouch))
                        {
                            actionTouch.OnTouch(hit.point);
                            isInteract = true;
                        }
                    }
                }

                if (!isInteract)
                {
                    //手指接触屏幕时的初始化
                    if (_isID1)
                    {
                        mFingerMove_1 = false;
                        mStartPos_1 = _pos;
                        mFingerTime_1 = mClikTime;
                    }
                    else
                    {
                        mFingerMove_2 = false;
                        mStartPos_2 = _pos;
                        mFingerTime_2 = mClikTime;
                    }

                    //记录按下时机并注册事件
                    if (mTouchAction.TryGetValue(ETouchType.Down, out ActionData _action))
                    {
                        SendAction(_action.mActionName, _action.MButtonInputType);
                    }

                    //双指缩放初始化
                    if (_isID1)
                    {
                        if (fingerID_2 > -1 && fingerID_1 < 0)
                        {
                            mFingerDis_Init =
                                Vector2.Distance(Input.touches[fingerID_2].position, Input.touches[_id].position);
                        }
                    }
                    else
                    {
                        if (fingerID_1 > -1 && fingerID_2 < 0)
                        {
                            mFingerDis_Init =
                                Vector2.Distance(Input.touches[fingerID_1].position, Input.touches[_id].position);
                        }
                    }
                }
            }

            if (_isID1) fingerID_1 = _id;
            else fingerID_2 = _id;
        }

        public void OnUpdate(float _deltaTime)
        {
            //长按判断
            if (mFingerTime_1 > 0)
            {
                mFingerTime_1 -= _deltaTime;
                if (mFingerTime_1 <= 0)
                {
                    mFingerTime_1 = -1;
                    if (!mFingerMove_1 && mTouchAction.TryGetValue(ETouchType.Hold, out ActionData _action))
                    {
                        SendAction(_action.mActionName, _action.MButtonInputType);
                    }
                }
            }

            if (mFingerTime_2 > 0)
            {
                //长按判断
                mFingerTime_2 -= _deltaTime;
                if (mFingerTime_2 <= 0)
                {
                    mFingerTime_2 = -1;
                    if (!mFingerMove_2 && mTouchAction.TryGetValue(ETouchType.Hold, out ActionData _action))
                    {
                        SendAction(_action.mActionName, _action.MButtonInputType);
                    }
                }
            }
        }

        #endregion

        #region MyRegion

        public void MoveFinger(Touch _touch, bool _isFonger01)
        {
            // if (_isFonger01) mFingerMove_1 = true;
            // else mFingerMove_2 = true;
            if (_isFonger01)
            {
                if (!mFingerMove_1)
                {
                    mFingerMove_1 = (_touch.position - mStartPos_1).sqrMagnitude > mMoveRange;
                }
            }
            else
            {
                if (!mFingerMove_2)
                {
                    mFingerMove_2 = (_touch.position - mStartPos_2).sqrMagnitude > mMoveRange;
                }
            }

            if (mCameZomm && fingerID_1 > -1 && fingerID_2 > -1)
            {//双指缩放
                float _nowScale =
                    Vector2.Distance(Input.touches[fingerID_1].position, Input.touches[fingerID_2].position) -
                    mFingerDis_Init;
            }
            else
            {
                Vector2 _startPos = _isFonger01 ? mStartPos_1 : mStartPos_2;
                if (mFingerInputType == EFingerInputType.Look)
                { //视角控制
                    ActionEngineManager_Input.Instance.Input_Look(_touch.deltaPosition * mViewInputSpeed);
                }
                else if (mFingerInputType == EFingerInputType.Move)
                {//位移控制
                    ActionEngineManager_Input.Instance.Input_Move((_touch.position - _startPos) * mMoveInputLength);
                }
            }
        }

        #endregion
        private void SendAction(string _action, ActionEngine_Touch_FingerScreenPos.EButtonInputType _inputType = ActionEngine_Touch_FingerScreenPos.EButtonInputType.Down)
        {
            if (!ActionEngineManager_Input.Instance.UpdateEnble) return;
            ActionEngineManager_Input.Instance.Player.ActionStateMachine.SendKeyDown(_action, (int)_inputType);
        }


    }

    [System.Serializable]
    public struct TouchActionData
    {
        public string mDisPlayName;
        public FingerInputData.ETouchType mTouchType;
        public ActionData mActionData;
    }

    [System.Serializable]
    public struct ActionData
    {
        public string mActionName;
        public ActionEngine_Touch_FingerScreenPos.EButtonInputType MButtonInputType;
    }
}