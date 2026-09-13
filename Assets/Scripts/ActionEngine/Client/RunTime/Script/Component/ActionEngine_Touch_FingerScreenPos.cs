using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AsiTimeLine.RunTime
{
    [ExecuteAlways]
    public partial class ActionEngine_Touch_FingerScreenPos : MonoBehaviour
    {
        #region Enum
        public enum EButtonInputType
        {
            Down,
            Up,
            Click,
            Hold
        }
        #endregion
        [Header("点击触发间隔")]
        public float mClikTime = 0.2f;

        [Header("是否绘制触屏范围")]
        public bool drawTouchRange = true;

        [Header("视角控制速度和位移控制速度")]
        public float viewInputSpeed = 1;
        public float moveInputLength = 1;

        [Header("滑动触发距离")]
        public float mMoveRange = 10;

        [Header("绘制FPS")]
        public bool drawFps = false;

        [Header("最大帧数上限")]
        public int systemFPS = 120;

        private GUIStyle fpsGUIStyle;
        private bool UGUI_Self = false;

        [Header("触屏输入的范围性功能")]
        public FingerInputData[] mFingerInputData = new FingerInputData[0];

        [Header("拟态UI按钮")]
        public ButtonData[] mButtonData = new ButtonData[0];

        [Header("UGUI相关的必要环境配置")]
        public GraphicRaycaster m_GraphicRay;
        public EventSystem m_EventSystem;
        private List<RaycastResult> m_RaycastResult = new List<RaycastResult>();
        private void Start()
        {
            Application.targetFrameRate = systemFPS;
            fpsGUIStyle = new GUIStyle();
            fpsGUIStyle.alignment = TextAnchor.UpperLeft;
            fpsGUIStyle.normal.textColor = Color.green;

            if (m_GraphicRay == null) m_GraphicRay = GameObject.FindObjectOfType<GraphicRaycaster>();
            if (m_EventSystem == null) m_EventSystem = GameObject.FindObjectOfType<EventSystem>();

            UGUI_Self = m_GraphicRay != null && m_EventSystem != null;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            foreach (FingerInputData _fingerData in mFingerInputData)
            {
                _fingerData.Init(mClikTime, viewInputSpeed, moveInputLength, mMoveRange * mMoveRange);
            }

            ButtonDataInit();
        }

        private void Update()
        {
            // #if UNITY_STANDALONE_WIN
            //             return;
            // #endif
#if UNITY_EDITOR
            if (!Cursor.visible)
                return;
            if (!Application.isPlaying)
                return;
#endif
            ActionEngineManager_Input.Instance.Input_Look(Vector2.zero);

            foreach (Touch mTouch in Input.touches)
            {
                //手指刚接触屏幕时
                if (mTouch.phase == TouchPhase.Began)
                {
                    if (TouchSelf(mTouch))
                    {
                        OnEnrollFinger(mTouch);
                    }
                }

                //手指刚离开屏幕时
                if (mTouch.phase == TouchPhase.Ended)
                {
                    ReMoveFinger(mTouch);
                    UnLoadButton(mTouch);
                }

                //手指移动时
                if (mTouch.phase == TouchPhase.Moved)
                {
                    MoveFinger(mTouch);
                }
            }

            ButtonDataUpdate(Time.deltaTime);
            foreach (FingerInputData _fingerData in mFingerInputData)
            {
                _fingerData.OnUpdate(Time.deltaTime);
            }
        }

        private float fpsTime;
        private int fpsConst;
        private int finishFps;
        private void OnGUI()
        {
#if UNITY_EDITOR
            //绘制各个触屏输入范围
            if (drawTouchRange && !Application.isPlaying)
            {
                foreach (FingerInputData _fingerData in mFingerInputData)
                {
                    float _weight = Screen.width * _fingerData.mFingerRange.width / 100;
                    float _height = Screen.height * _fingerData.mFingerRange.height / 100;
                    float _pos_x = Screen.width * _fingerData.mFingerRange.x / 100;
                    float _pos_y = Screen.height * _fingerData.mFingerRange.y / 100;
                    Rect _DrawGUI = new Rect(_pos_x, _pos_y, _weight, _height);

                    GUI.Box(_DrawGUI, _fingerData.mName);
                }
            }
#endif
            if (drawFps)
            {
                fpsGUIStyle.fontSize = (int)(Screen.width / 30);

                fpsTime += Time.deltaTime;
                fpsConst++;
                if (fpsTime >= 1)
                {
                    finishFps = fpsConst;
                    fpsTime = 0;
                    fpsConst = 0;
                }
                Rect _fps = new Rect(10, Screen.height / 10, 100, 25);
                GUI.Box(_fps, $"FPS: {finishFps}", fpsGUIStyle);
            }
        }

        private void SendAction(string _action, EButtonInputType _inputType = EButtonInputType.Down)
        {
            if (!ActionEngineManager_Input.Instance.UpdateEnble) return;
            ActionEngineManager_Input.Instance.Player.ActionStateMachine.SendKeyDown(_action, (int)_inputType);
        }


    }

}