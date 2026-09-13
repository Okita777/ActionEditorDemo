using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AsiTimeLine.RunTime
{
    public partial class ActionEngine_Touch_FingerScreenPos
    {
        //所有的按钮
        private Dictionary<int, ButtonData> mRaycastResultDic = new Dictionary<int, ButtonData>();
        //所有按下的按钮
        private Dictionary<int, ButtonData> mAllButton = new Dictionary<int, ButtonData>();

        private void ButtonDataInit()
        {
            mRaycastResultDic.Clear();
            mAllButton.Clear();

            foreach (var VARIABLE in mButtonData)
            {
                VARIABLE.Init();
                mRaycastResultDic.Add(VARIABLE.mButton.GetHashCode(), VARIABLE);
            }
        }

        private void ButtonDataUpdate(float _deltaTime)
        {
            foreach (var VARIABLE in mAllButton)
            {
                if (mAllButton.TryGetValue(VARIABLE.Key, out ButtonData _ButtonData))
                {
                    if (_ButtonData.timing > 0)
                    {
                        _ButtonData.timing -= _deltaTime;
                        if (_ButtonData.timing <= 0)
                        {
                            //触发长按
                            if (_ButtonData.TryGetActionData(FingerInputData.ETouchType.Hold, out ActionData _action))
                            {
                                SendAction(_action.mActionName, _action.MButtonInputType);
                            }
                        }
                    }
                }
            }
        }

        //检查当前坐标下是否存在有效按钮
        private bool TouchSelf(Touch _touch)
        {
            if (!UGUI_Self) return true;
            PointerEventData p = new PointerEventData(m_EventSystem);
            p.position = _touch.position;
            m_RaycastResult.Clear();
            m_GraphicRay.Raycast(p, m_RaycastResult);

            foreach (var VARIABLE in m_RaycastResult)
            {
                if (mRaycastResultDic.TryGetValue(VARIABLE.gameObject.GetHashCode(), out ButtonData _ButtonData))
                {
                    _ButtonData.mInitPos = _touch.position;
                    _ButtonData.timing = mClikTime;
                    if (mAllButton.TryAdd(_touch.fingerId, _ButtonData))
                    {
                        //按下
                        if (_ButtonData.TryGetActionData(FingerInputData.ETouchType.Down, out ActionData _action))
                        {
                            SendAction(_action.mActionName, _action.MButtonInputType);
                        }
                    }

                    return false;
                }
            }

            if (m_RaycastResult.Count > 0) return false;
            return true;
        }

        private void UnLoadButton(Touch _touch)
        {
            //手指离开屏幕时
            if (mAllButton.TryGetValue(_touch.fingerId, out ButtonData _ButtonData))
            {
                mAllButton.Remove(_touch.fingerId);

                //滑动方向
                Vector2 _SlidDir = _touch.position - _ButtonData.mInitPos;
                //滑动的判断死亡区
                bool _isMove = _SlidDir.sqrMagnitude > (mMoveRange * mMoveRange);
                bool _isSlid = false;
                if (_isMove)
                {
                    //滑动触发

                    if (_ButtonData.TryGetActionData(FingerInputData.ETouchType.Slid_Up, out ActionData _actionSlid_up))
                    {
                        if (Vector2.Angle(_SlidDir, Vector2.up) <= 45)
                        {
                            _isSlid = true;
                            SendAction(_actionSlid_up.mActionName, _actionSlid_up.MButtonInputType);
                        }
                    }
                    if (!_isSlid && _ButtonData.TryGetActionData(FingerInputData.ETouchType.Slid_Down, out ActionData _actionSlid_dwon))
                    {
                        if (Vector2.Angle(_SlidDir, Vector2.down) <= 45)
                        {
                            _isSlid = true;
                            SendAction(_actionSlid_dwon.mActionName, _actionSlid_dwon.MButtonInputType);
                        }
                    }
                    if (!_isSlid && _ButtonData.TryGetActionData(FingerInputData.ETouchType.Slid_Left, out ActionData _actionSlid_left))
                    {
                        if (Vector2.Angle(_SlidDir, Vector2.left) < 45)
                        {
                            _isSlid = true;
                            SendAction(_actionSlid_left.mActionName, _actionSlid_left.MButtonInputType);
                        }
                    }
                    if (!_isSlid && _ButtonData.TryGetActionData(FingerInputData.ETouchType.Slid_Right, out ActionData _actionSlid_right))
                    {
                        if (Vector2.Angle(_SlidDir, Vector2.right) < 45)
                        {
                            _isSlid = true;
                            SendAction(_actionSlid_right.mActionName, _actionSlid_right.MButtonInputType);
                        }
                    }
                    if (!_isSlid && _ButtonData.TryGetActionData(FingerInputData.ETouchType.Slid, out ActionData _actionSlid))
                    {
                        _isSlid = true;
                        SendAction(_actionSlid.mActionName, _actionSlid.MButtonInputType);
                    }
                }

                if (!_isSlid)
                {
                    //非滑动触发
                    if (_ButtonData.timing > 0)
                    {
                        //点击
                        if (_ButtonData.TryGetActionData(FingerInputData.ETouchType.Click, out ActionData _action))
                        {
                            SendAction(_action.mActionName, _action.MButtonInputType);
                        }
                    }
                }

                //卸载长按触发判断
                if (_ButtonData.TryGetActionData(FingerInputData.ETouchType.Hold, out ActionData _actionUpH))
                {
                    foreach (var VARIABLE in ActionEngineManager_Input.Instance.Player.ActionStateMachine.AllActionStatePart)
                    {
                        if (VARIABLE.NowInputHoldKey == _actionUpH.mActionName)
                        {
                            VARIABLE.NowInputHoldKey = "";
                        }
                    }

                }

                //抬起
                if (_ButtonData.TryGetActionData(FingerInputData.ETouchType.Up, out ActionData _actionUp))
                {
                    SendAction(_actionUp.mActionName, _actionUp.MButtonInputType);
                }

            }
        }
    }

    [System.Serializable]
    public class ButtonData
    {
        public string mName;
        public GameObject mButton;
        public TouchActionData[] mActionData;
        [HideInInspector] public Vector2 mInitPos;
        [HideInInspector] public float timing;

        private Dictionary<FingerInputData.ETouchType, ActionData> actionDic;
        public void Init()
        {
            actionDic = new Dictionary<FingerInputData.ETouchType, ActionData>();
            if (mActionData == null) return;
            foreach (var VARIABLE in mActionData)
            {
                actionDic.Add(VARIABLE.mTouchType, VARIABLE.mActionData);
            }
        }

        public bool TryGetActionData(FingerInputData.ETouchType _touchType, out ActionData _action)
        {
            if (actionDic.TryGetValue(_touchType, out ActionData _actionName2))
            {
                _action = _actionName2;
                return true;
            }

            _action = new ActionData();
            return false;
        }
    }
}