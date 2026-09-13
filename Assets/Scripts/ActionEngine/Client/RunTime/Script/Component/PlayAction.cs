using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime;
// using AsiTimeLine.RunTime;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace AsiTimeLine.RunTime
{
    [RequireComponent(typeof(ActionEngine_Unit))]
    public class PlayAction : MonoBehaviour
    {
        public List<PlayActionInfo> ActionList = new List<PlayActionInfo>();
        public List<string> ActionUI = new List<string>();
        public List<string> ActionTrigger = new List<string>();
        public KeyCode OpenActionUIKey = KeyCode.LeftAlt;
        public int mTextSize = 25;
        public int mRadius = 100;
        public int mRadius2 = 150;
        public int mClosRange = 20;

        private ActionEngine_Unit mPlayer;
        private bool mUIDisplay = false;
        private string mSelectAction = string.Empty;
        private GUIStyle mDefaultFont;

        private void Start()
        {
            mPlayer = GetComponent<ActionEngine_Unit>();
            mDefaultFont = new GUIStyle();
            mDefaultFont.fontStyle = FontStyle.Bold;
            mDefaultFont.alignment = TextAnchor.MiddleCenter;
        }

        private void Update()
        {
            if (!ActionEngineManager_Input.Instance.IsPlayer(mPlayer))
                return;

            if (mPlayer.ActionStateMachine is null)
            {
                EngineDebug.LogWarning($"状态机未初始化: {gameObject.name}");
                return;
            }
            foreach (var _action in ActionList)
            {
                _action.Check(mPlayer.ActionStateMachine);
            }

            //根据Action类型筛选
            // string _lable = mPlayer.GetActionLable(mPlayer.AllActionStatePart[0].CurrentActionState.ActionLable);

            //根据具体ID筛选
            if (!ActionTrigger.Contains(mPlayer.ActionStateMachine.AllActionStatePart[0].CurrentActionState.Name)) return;

            if (Input.GetKeyDown(OpenActionUIKey))
            {
                mUIDisplay = true;
                ActionEngineManager_Input.Instance.SetMoseDisPlay(mUIDisplay);
            }

            if (Input.GetKeyUp(OpenActionUIKey))
            {
                mUIDisplay = false;
                ActionEngineManager_Input.Instance.SetMoseDisPlay(mUIDisplay);

                if (!string.IsNullOrEmpty(mSelectAction))
                {
                    mPlayer.ActionStateMachine.ChangeAction(mSelectAction, 200, 0);
                }
            }
            mDefaultFont.fontSize = mTextSize;
        }

        private void OnGUI()
        {
            if (ActionUI.Count < 1) return;

            mSelectAction = string.Empty;

            if (mUIDisplay)
            {
                Vector2 _scenter = new Vector2(Screen.width / 2, Screen.height / 2);

                Vector2 _SfindPos = Vector2.zero;
                Vector2 _EfindPos = Vector2.zero;
                Vector2 _findPos_F = Vector2.zero;
                string _findFont = String.Empty;

                float _minDis = float.MaxValue;
                bool _radius = false;
                float _angle = (float)360 / ActionUI.Count;
                mDefaultFont.normal.textColor = Color.black;
#if UNITY_EDITOR
                Handles.color = Color.gray;
#endif
                for (int i = 0; i < ActionUI.Count; i++)
                {
                    float _mRadius = _radius ? mRadius : mRadius2;
                    _radius = !_radius;
                    Quaternion _rot = Quaternion.Euler(0, 0, i * _angle);
                    Vector2 _startPos = _rot * Vector2.up * mClosRange;
                    Vector2 _endPos = _rot * Vector2.up * _mRadius;
                    _startPos += _scenter;
                    _endPos += _scenter;
#if UNITY_EDITOR
                    Handles.DrawLine(_startPos, _endPos);
#endif
                    Vector2 _fontPos = (_rot * Vector2.up * (_mRadius + 50));
                    _fontPos += _scenter;
                    GUI.Label(new Rect(_fontPos.x - 50, _fontPos.y - 50, 100, 100), ActionUI[i], mDefaultFont);

                    float _findDis = (Event.current.mousePosition - _fontPos).sqrMagnitude;
                    if (_findDis < _minDis)
                    {
                        _minDis = _findDis;
                        _SfindPos = _startPos;
                        _EfindPos = _endPos;
                        _findPos_F = _fontPos;
                        _findFont = ActionUI[i];
                    }
                }

                if ((Event.current.mousePosition - _scenter).sqrMagnitude > mClosRange * mClosRange)
                {
                    mDefaultFont.normal.textColor = Color.red;
#if UNITY_EDITOR
                    Handles.color = Color.white;
                    Handles.DrawLine(_SfindPos, _EfindPos);
#endif
                    GUI.Label(new Rect(_findPos_F.x - 50, _findPos_F.y - 50, 100, 100), _findFont, mDefaultFont);
                    mSelectAction = _findFont;
                }
            }
        }
    }

    [System.Serializable]
    public class PlayActionInfo
    {
        public string mName;
        public KeyCode mKey;
        public int mMixTime;
        public int mOffsetTime;

        public void Check(ActionStateMachine _stateMachine)
        {
            if (Input.GetKeyDown(mKey))
            {
                _stateMachine.ChangeAction(mName, mMixTime, mOffsetTime);
            }
        }
    }
}