using System;
using System.Collections.Generic;

using AsiActionEngine.RunTime;

#if UNITY_EDITOR
using AsiActionEngine.Editor;
#endif
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class ActionEngine_DrawActionState : MonoBehaviour
    {
        private class SActionStateDrawData
        {
            public ActionState actionState;
            public int jumpStateIndex;
            public float jumpTime;
            public bool isCur;
            public List<string> actionStateNames;

            public float setAnimTime;
            public float setAnimTimeNow;
            public float setAnimTime_2;
            public float setAnimTimeNow_2;

            public SActionStateDrawData(ActionState actionState, int jumpStateIndex, float jumpTime, bool isCur,
                List<string> actionStateNames)
            {
                this.actionState = actionState;
                this.jumpStateIndex = jumpStateIndex;
                this.jumpTime = jumpTime;
                this.isCur = isCur;
                this.actionStateNames = actionStateNames;

                setAnimTime = -1;
                setAnimTimeNow = -1;
                setAnimTime_2 = -1;
                setAnimTimeNow_2 = -1;
            }

            public void Clone(SActionStateDrawData other)
            {
                this.actionState = other.actionState;
                this.jumpStateIndex = other.jumpStateIndex;
                this.jumpTime = other.jumpTime;
                this.isCur = other.isCur;
                this.actionStateNames.Clear();
                this.actionStateNames.AddRange(other.actionStateNames);
                this.setAnimTime = other.setAnimTime;
                this.setAnimTimeNow = other.setAnimTimeNow;
                this.setAnimTime_2 = other.setAnimTime_2;
                this.setAnimTimeNow_2 = other.setAnimTimeNow_2;
            }

            public void StartAnim(float animTime)
            {
                setAnimTimeNow += animTime;
                if (isCur)
                {
                    setAnimTime_2 = 0.5f;
                    setAnimTimeNow_2 = setAnimTime_2;
                }
            }

            public float AnimTime(float updateTime)
            {
                if (setAnimTimeNow > 0)
                {
                    setAnimTimeNow -= updateTime;
                    if (setAnimTimeNow > 0)
                        return setAnimTimeNow / setAnimTime;
                }
                return 0;
            }

            public float AnimTime2(float updateTime)
            {
                if (setAnimTimeNow_2 > 0)
                {
                    setAnimTimeNow_2 -= updateTime;
                    if (setAnimTimeNow_2 > 0)
                        return setAnimTimeNow_2 / setAnimTime_2;
                }
                return 0;
            }
        }

        private class ThisActionStateDrawData
        {
            public string mName;
            public int mLayerID;
            public float mTime;

            private Color mChangeColor = Color.green;
            private float mColorTime;
            private float mAnimTime;
            private float sourActionAnimPosX;
            private string sourActionName;
            public void ReStartChangeAnim(ActionState _lastAction)
            {
                mColorTime = 2f;
                mAnimTime = 2f;
                sourActionAnimPosX = 20;
                if (_lastAction is null)
                {
                    sourActionName = " <= 外部或者初始化";
                }
                else
                {
                    sourActionName = $" <= 由【<color=#ffcc00>{_lastAction.Name}</color>】 切换";
                }
            }

            public void DrawButton(Rect _rect, float _deltaTime, Action _click = null)
            {
                Rect _layerName = new Rect(_rect);
                _layerName.width = _layerName.height;
                GUI.Box(_layerName, mLayerID.ToString());

                _layerName.x += _layerName.width;
                _layerName.width = _rect.width - _layerName.width;

                Rect _timeLine = new Rect(_layerName);
                _timeLine.width = 0;
                _timeLine.x = _layerName.x + _layerName.width * mTime;
                GUI.Box(_timeLine, "");

                if (mAnimTime > 0)
                {
                    Color color = GUI.backgroundColor;

                    if (ActionEngineManager.Instance.OnUpdateEnble)
                    {
                        mAnimTime -= _deltaTime;
                        mColorTime = Mathf.Lerp(mColorTime, 0, 2 * _deltaTime);
                        sourActionAnimPosX = Mathf.Lerp(sourActionAnimPosX, 0, 3 * _deltaTime);
                        GUI.backgroundColor = Color.Lerp(color, mChangeColor, mColorTime);
                    }

                    if (GUI.Button(_layerName, mName))
                    {
                        _click?.Invoke();
                    }

                    _layerName.x += _layerName.width + sourActionAnimPosX;
                    _layerName.width += 80;
                    GUI.Label(_layerName, sourActionName);

                    GUI.backgroundColor = color;
                }
                else
                {
                    if (GUI.Button(_layerName, mName))
                    {
                        _click?.Invoke();
                    }
                }
            }
        }
        //绘制UI的配置
        private const int childHeight = 20; //成员高度
        private const int interal = 3; //成员间隔
        private const int windowsWidth = 200;
        private const int windowsWidthInteral = 10;
        private const float layerLabelMinWidth = 22f; //层级名称标签最小宽度
        private const float layerLabelMaxWidth = 90f; //层级名称标签自适应上限


        //公开参数
        [Header("绘制到自身位置(仅Editor有效)")] public bool drawTothis = false;
        [Header("在打包后也绘制")] public bool updateDraw = false;
        private Vector2 mthisButtonScale = new Vector2(200, 22);//场景中跟随角色绘制的按钮宽高

        [Space(10)]
        [Header("绘制指定层级下的Action或事件")]
        public int m_CheckLayerID = 0;
        public int m_DrawHistory = 4;
        public float m_AnimTime = 0.2f;
        [Header("高度偏移，在勾选【绘制到自身位置】后参数为角色3d空间高度偏移，单位M")]
        public float drawHeight = 0;

        [Header("位置偏移，仅在勾选【绘制到自身位置】时有效")]
        public Vector2 drawOffsetPos = Vector2.zero;
        //[Header("绘制为事件，仅在勾选【绘制到自身位置】时有效")]
        //public bool m_isDrawActionEvent = false;
        //[Header("绘制事件详细描述，仅在勾选【绘制到自身位置&绘制为事件】时有效")]
        //public bool m_isDrawDes = false;
        [Header("折叠UI，仅在勾选【绘制到自身位置】时有效")]
        public bool m_isDrawActionUFlod = false;
        private Material lineMaterial; // 需要一个材质，可以使用默认的UI/Default材质

        private string[] mModeNames = new string[] {
            "Draw_Action",
            "Draw_Event",
            "Draw_Event(detail)",
            "Draw_Event(ALL)"
        };
        private ActionEngine_Unit unit;
        private bool isActive = false;
        private bool isBindingToRender = false;
        private bool isDrawDesEvent = true;
        private int onDrawToThis = 0;
        private SActionStateDrawData[] actionStates;
        private ThisActionStateDrawData[] thisActionStates;
        //private ActionEvent[] thisActionEvents;
        private int thisDrawConst = 0;
        private int ActionStateJumpCount = 0;
        private float ActionLastTime = 0;
        private ActionState lastActionState = null;
        private ActionStatePart statePart;
        private List<string> stateNames = new List<string>();
        private string[] mEventExtrudNames_Main;
        private string[] mEventExtrudNames;
        private int mEventExtrudNames_Length;
        private Transform targetTrans;
        private Transform referTrans;
        private Rect selfRect = new Rect();
        private Rect selfRect2 = new Rect();
        private float startDelay = 2;
        [Header("绘制模式")] public int mDisMode = 0;
        private List<ActionEvent> mVestigitalAction = new List<ActionEvent>();

#if UNITY_EDITOR
        private EditorActionEvent mEditorAction = new EditorActionEvent(null, 0, 0);
#endif

        private GUIStyle mGUIStyle = new GUIStyle();
        private GUIStyle mGUIStyleEvent = new GUIStyle();

        private GUIStyle mGUIStyle_R = new GUIStyle();
        private GUIStyle mGUIStyle_L = new GUIStyle();
        private void Awake()
        {
            isActive = false;
            startDelay = 2;
        }
        private void Init()
        {
            //#if UNITY_EDITOR
#if !UNITY_EDITOR
            if (!updateDraw) return;
#endif
            //默认最大层级同时绘制的数量为10
            //thisActionEvents = new ActionEvent[30];
            thisActionStates = new ThisActionStateDrawData[15];

            //最大事件绘制数量
            mEventExtrudNames = new string[15];
            mEventExtrudNames_Main = new string[mEventExtrudNames.Length];
            for (int i = 0; i < thisActionStates.Length; i++)
            {
                thisActionStates[i] = new ThisActionStateDrawData();
            }
            thisDrawConst = 0;

            actionStates = new SActionStateDrawData[m_DrawHistory + 2];
            for (int i = 0; i < actionStates.Length; i++)
            {
                actionStates[i] = new SActionStateDrawData(null, -1, -1, false, new List<string>());
                actionStates[i].setAnimTime = m_AnimTime;
            }

            if (drawTothis)
            {
                isBindingToRender = TryGetComponent(out SkinnedMeshRenderer _skinRender);
                if (!isBindingToRender) isBindingToRender = TryGetComponent(out MeshRenderer _render);
            }
            else
            {
                isBindingToRender = false;
            }

            Transform unitTrans = transform;
            while (unitTrans is not null && !unitTrans.TryGetComponent(out unit))
            {
                unitTrans = unitTrans.parent;
            }

            isActive = unit is not null;
            //if (drawTothis) isActive = transform.parent.TryGetComponent(out unit);
            //else isActive = TryGetComponent(out unit);
            if (!isActive)
            {
                EngineDebug.LogWarning($"未获取到unit组件: {name}");
                return;
            }
            else
            {
                targetTrans = unit.transform;
#if UNITY_EDITOR
                if (unit.ActionStateMachine != null && unit.ActionStateMachine.EventSystem != null)
                {//注册事件监听
                    unit.ActionStateMachine.EventSystem.OnExecuteActionEvent += (_selfPart, _event, _type) =>
                    {
                        //if (drawTothis)
                        {
                            if (mEventExtrudNames_Length < mEventExtrudNames.Length)
                            {
                                if (_selfPart.AnimaLayer == m_CheckLayerID)
                                {
                                    string _curActionName = _selfPart.CurrentActionState.Name;

                                    string actionEventName = EngineResourcesManager.Instance.GetEventName(_event.EventData);
                                    if (ActionWindowMain.DicActionEventName_Des.TryGetValue(actionEventName, out string _str))
                                        actionEventName = _str;

                                    if (mEventExtrudNames_Length == 0)
                                    {
                                        mEventExtrudNames[mEventExtrudNames_Length] =
                                        $"<color=#ffcc00>->[ {_curActionName} ]<-</color>";
                                        mEventExtrudNames_Length++;
                                    }

                                    string _strColor;
                                    if (_type == EActionEventTriggerType.Enter)
                                    {
                                        _strColor = "7CFC00";
                                    }
                                    else if (_type == EActionEventTriggerType.Trigger)
                                    {
                                        _strColor = "1E90FF";
                                    }
                                    else
                                    {
                                        _strColor = "006400";
                                    }

                                    string _otherDes = string.Empty;
                                    //检查进入和退出
                                    if (_type == EActionEventTriggerType.Enter)
                                    {
                                        //EngineDebug.LogWarning($"执行进入事件[{actionEventName}] {(_event.IsTem ? "Tem" : "")}");
                                        if (!mVestigitalAction.Contains(_event))
                                        {
                                            mVestigitalAction.Add(_event);
                                        }
                                        else
                                        {
                                            _otherDes = "  [<color=#ff0000>重复执行进入事件</color>]";
                                            //EngineDebug.LogWarning($"重复执行进入事件[{actionEventName}] {(_event.IsTem ? "Tem" : "")}");
                                        }
                                    }
                                    else if (_type == EActionEventTriggerType.Exit)
                                    {
                                        if (!mVestigitalAction.Remove(_event))
                                        {
                                            _otherDes = "  [<color=#ff0000>未进入就退出了</color>]";
                                            //EngineDebug.LogWarning($"事件删除错误，未进入就退出了[{actionEventName}({mEventExtrudNames_Length})] {(_event.IsTem ? "Tem" : "")}  Hash:{_event.GetHashCode()}" +
                                            //    $"\n当前[{_selfPart.CurrentActionState.Name}]  前任[{_selfPart.CurrentActionState_Last.Name}]");
                                        }
                                    }
                                    else
                                    {

                                    }

                                    mEventExtrudNames[mEventExtrudNames_Length] =
                                    $"{actionEventName} : <color=#{_strColor}>{_type.ToString()}</color>" + _otherDes;//({_event.GetHashCode()})
                                    mEventExtrudNames_Length++;
                                }
                            }
                        }
                    };
                }
                else
                {
                    EngineDebug.LogError("[编辑器报错]部分角色绘制组件配置出错: ActionStateMachine or EventSystem is null，可忽略");
                }
#endif
            }

            mGUIStyle.alignment = TextAnchor.MiddleCenter;
            mGUIStyle.normal.textColor = Color.white;
            mGUIStyleEvent.alignment = TextAnchor.MiddleCenter;
            mGUIStyleEvent.normal.textColor = Color.black;
            mGUIStyleEvent.fontSize = 10;
            mGUIStyle_R.alignment = TextAnchor.MiddleRight;
            mGUIStyle_R.normal.textColor = Color.white;
            mGUIStyle_L.alignment = TextAnchor.MiddleLeft;
            mGUIStyle_L.normal.textColor = Color.white;

            referTrans = unit.transform;
            if (unit.transform.parent is not null && unit.transform.parent.TryGetComponent(out ITargetUnit IUnit))
            {
                referTrans = unit.transform.parent;
            }
            //#endif
        }

        //#if UNITY_EDITOR

        private void OnGUI()
        {
            float deltaTime = Time.deltaTime;

            //延时启动
            if (startDelay > 0)
            {
                startDelay -= deltaTime;
                if (startDelay <= 0)
                {
                    Init();
                }
            }
            //if (drawTothis) return;
            if (!isActive) return;

#if UNITY_EDITOR
            if (!AsiActionEngine.Editor.ActionWindowMain.ScenceDraw_EditorAC) return;
#else
            if (!updateDraw) return;
#endif


            if (drawTothis)
            {
                if (!isBindingToRender) onDrawToThis = 3;
                DrawToThis(deltaTime);
            }
            else
            {
                DrawToScence(deltaTime);
            }
        }

        private void OnWillRenderObject()
        {
            onDrawToThis = 3;
        }
        private void DrawToThis(float deltaTime)
        {
            if (onDrawToThis < 0) return;
            onDrawToThis--;
            if (unit.ActionStateMachine is null || unit.ActionStateMachine.AllActionStatePart is null) return;

            Vector3 _pos = targetTrans.TransformPoint(0, drawHeight, 0);
            _pos = ActionEngineManager_Input.Instance.PlayerCam.WorldToScreenPoint(_pos);
            _pos.y = (Screen.height - _pos.y) + drawOffsetPos.y;
            if (m_isDrawActionUFlod)
                _pos.x -= drawOffsetPos.x;
            else
                _pos.x -= mthisButtonScale.x / 2 + drawOffsetPos.x;


            bool isDraw = _pos.y > 0 && _pos.y < Screen.height;
            if (isDraw)
            {
                isDraw = _pos.x > 0 && _pos.x < Screen.width;
            }

            if (isDraw)
            {
                selfRect.position = _pos;
                if (m_isDrawActionUFlod)
                {
                    selfRect.width = mthisButtonScale.y;
                    selfRect.height = mthisButtonScale.y;

                    using (new GUIColorScope(Color.green))
                    {
                        if (GUI.Button(selfRect, "O"))
                        {
                            m_isDrawActionUFlod = false;
                        }
                    }
                    return;
                }
                //绘制标题按钮
                selfRect.width = mthisButtonScale.x - 40;
                selfRect.height = mthisButtonScale.y;
                string disName = mModeNames[mDisMode];
                Rect nameRect = new Rect(selfRect);
                nameRect.y -= nameRect.height;
                nameRect.width = 999;
                GUI.Label(nameRect, $"<color=#ccff00>{referTrans.name}({referTrans.GetSiblingIndex()})</color>");
                if (GUI.Button(selfRect, disName))
                {
                    mDisMode++;
                    if (mDisMode >= mModeNames.Length) mDisMode = 0;

                    //if (mDisMode == 0)
                    //{
                    //    m_isDrawActionEvent = false;
                    //    m_isDrawDes = false;
                    //}
                    //else if (mDisMode == 1)
                    //{
                    //    m_isDrawActionEvent = true;
                    //    m_isDrawDes = false;
                    //}
                    //else if (mDisMode == 2)
                    //{
                    //    m_isDrawActionEvent = true;
                    //    m_isDrawDes = true;
                    //}
                }
                selfRect.x += selfRect.width;
                selfRect.width = 20;
                if (GUI.Button(selfRect, "-"))
                {
                    m_isDrawActionUFlod = true;
                }
                selfRect.x += selfRect.width;
                using (new GUIColorScope(Color.red))
                {
                    if (GUI.Button(selfRect, "x"))
                    {
                        this.enabled = false;
                    }
                }


                Vector2 _drawEventPos = _pos;
                _pos.y += mthisButtonScale.y;
                //Debug.Log($"位置绘制: " + _pos);
                thisDrawConst = 0;
                bool m_isDrawActionEvent = (mDisMode != 0);
                if (m_isDrawActionEvent)
                {
                    //绘制Action事件
                    #region 绘制Action事件
#if UNITY_EDITOR
                    bool m_isDrawDes = mDisMode == 2;

                    if (mDisMode == 3)
                    {
                        selfRect2.position = _pos;
                        selfRect2.width = mthisButtonScale.x;
                        selfRect2.height = mthisButtonScale.y;

                        selfRect.position = _pos;
                        selfRect.width = mthisButtonScale.x;
                        selfRect.height = mthisButtonScale.y;
                        for (int i = 0; i < unit.ActionStateMachine.AllActionStatePart.Count; i++)
                        {
                            int layerID = unit.ActionStateMachine.RealyLayer(i);
                            ActionStatePart _curActionStatePart = unit.ActionStateMachine.AllActionStatePart[layerID];
                            if (_curActionStatePart.ActionEnble)
                            {
                                ActionState _actionState = _curActionStatePart.CurrentActionState;
                                float _timeLine = _curActionStatePart.ElapsedTime / _actionState.TotalTime;

                                Vector2 _startPos = _pos;
                                //ThisActionStateDrawData nowPartData = thisActionStates[0];
                                foreach (ActionEvent actionState in _curActionStatePart.CurrentActionEvents)
                                {
                                    selfRect.position = _pos;
                                    selfRect.width = mthisButtonScale.x;
                                    selfRect2.position = _pos;
                                    //绘制背景
                                    GUI.Box(selfRect, "");

                                    DrawEventTrack(selfRect2, actionState, _actionState, mthisButtonScale, false);
                                    _pos.y += selfRect2.height;

                                    //绘制事件层级
                                    string _layerTag = $"[{GetLayerName(_curActionStatePart.AnimaLayer) ?? i.ToString()}]";
                                    selfRect.width = CalcLayerLabelWidth(_layerTag);
                                    selfRect.x -= selfRect.width + 2;
                                    GUI.Box(selfRect, _layerTag);
                                }
                                _startPos.x += _timeLine * mthisButtonScale.x;
                                Vector2 _endPos = _startPos;
                                _endPos.y = _pos.y;
                                DrawLine(_startPos, _endPos, Color.red, 1.0f);
                            }
                        }
                    }
                    else
                    {
                        int realyLayer = m_CheckLayerID;
                        ActionStatePart _curActionStatePart = unit.ActionStateMachine.AllActionStatePart[realyLayer];
                        if (_curActionStatePart.ActionEnble)
                        {
                            #region 绘制Action当前事件
                            ActionState _actionState = _curActionStatePart.CurrentActionState;
                            float _timeLine = _curActionStatePart.ElapsedTime / _actionState.TotalTime;

                            selfRect.position = _pos;
                            selfRect.width = mthisButtonScale.x;
                            selfRect.height = mthisButtonScale.y;
                            ThisActionStateDrawData nowPartData = thisActionStates[0];
                            if (!string.Equals(nowPartData.mName, _actionState.Name))
                            {
                                nowPartData.mName = _actionState.Name;
                                nowPartData.ReStartChangeAnim(_curActionStatePart.CurrentActionState_ChangeSour);
                            }
                            nowPartData.mLayerID = _curActionStatePart.AnimaLayer;
                            nowPartData.mTime = _timeLine;

                            nowPartData.DrawButton(selfRect, deltaTime, () =>
                            {
                                //事件显示开关
                                isDrawDesEvent = !isDrawDesEvent;
                            });

                            _pos.y += mthisButtonScale.y;

                            Vector2 _scale = mthisButtonScale;
                            _scale.x += (m_isDrawDes ? 120 : 0);
                            _scale.y += (m_isDrawDes ? 8 : 0);

                            ////未退出的Action绘制
                            //selfRect.position = _pos;
                            //selfRect.x += _scale.x;
                            //selfRect.width = 200;
                            //selfRect.height = 200;
                            //GUI.Box(selfRect, "");
                            //selfRect.height = 20;
                            //GUI.Label(selfRect, "<color=#ffcc00>未退出的Action</color>(不分前后)", mGUIStyle);
                            //foreach (var _event in mVestigitalAction)
                            //{
                            //    selfRect.y += 20;
                            //    string actionEventName = EngineResourcesManager.Instance.GetEventName(_event.EventData);
                            //    if (ActionWindowMain.DicActionEventName_Des.TryGetValue(actionEventName, out string _str))
                            //        actionEventName = _str;
                            //    GUI.Label(selfRect, $"[{actionEventName}] isTem:{_event.IsTem}", mGUIStyle);
                            //}

                            //mVestigitalAction
                            foreach (ActionEvent actionState in _curActionStatePart.CurrentActionEvents)
                            {
                                selfRect.position = _pos;
                                selfRect.width = _scale.x;
                                selfRect.height = _scale.y;
                                GUI.Box(selfRect, "");

                                selfRect2.position = _pos;
                                selfRect2.height = selfRect.height;
                                DrawEventTrack(selfRect2, actionState, _actionState, _scale, m_isDrawDes);
                                _pos.y += _scale.y;
                            }
                            _pos.x += _timeLine * _scale.x;
                            DrawLine(_pos, new Vector2(_pos.x, _pos.y - _curActionStatePart.CurrentActionEvents.Count * _scale.y), Color.red, 1.0f);
                            #endregion

                            #region 绘制Action事件执行历史
                            if (isDrawDesEvent)
                            {
                                _drawEventPos.x -= 220;
                                _drawEventPos.y -= 150;
                                selfRect.position = _drawEventPos;
                                selfRect.width = 220;
                                selfRect.height = mEventExtrudNames_Main.Length * 20;
                                GUI.Box(selfRect, "");

                                selfRect.height = 20;
                                for (int i = 0; i < mEventExtrudNames_Main.Length; i++)
                                {
                                    GUI.Label(selfRect, mEventExtrudNames_Main[i], mGUIStyle);
                                    selfRect.y += 20;
                                }
                            }

                            //foreach (string _str in mEventExtrudNames_Main)
                            //{

                            //}
                            #endregion
                        }
                        else
                        {

                        }
                    }
#endif
                    #endregion
                }
                else
                {
                    //绘制Action行为
                    #region 绘制Action行为
                    //foreach (ActionStatePart actionState in unit.ActionStateMachine.AllActionStatePart)
                    foreach (int _index in unit.ActionStateMachine.ActionStateInfo.mLayerOrder)
                    {
                        ActionStatePart actionState = unit.ActionStateMachine.AllActionStatePart[_index];
                        if (actionState.ActionEnble)// && actionState.CurrentActionState is not null
                        {
                            ThisActionStateDrawData nowPartData = thisActionStates[thisDrawConst];

                            if (actionState.CurrentActionState is null)
                            {
                                nowPartData.mLayerID = actionState.AnimaLayer;
                                nowPartData.mName = "空Action";
                                nowPartData.mTime = 0;
                            }
                            else
                            {
                                if (!string.Equals(nowPartData.mName, actionState.CurrentActionState.Name))
                                {
                                    nowPartData.mName = actionState.CurrentActionState.Name;
                                    nowPartData.ReStartChangeAnim(actionState.CurrentActionState_ChangeSour);
                                }
                                nowPartData.mLayerID = actionState.AnimaLayer;
                                nowPartData.mTime = actionState.ElapsedTime / actionState.CurrentActionState.TotalTime;
                            }


                            thisDrawConst++;
                            if (thisDrawConst >= thisActionStates.Length)
                            {
                                break;
                            }
                        }
                    }

                    for (int i = 0; i < thisDrawConst; i++)
                    {
                        ThisActionStateDrawData nowPartData = thisActionStates[i];

                        selfRect.position = _pos;
                        selfRect.width = mthisButtonScale.x;
                        selfRect.height = mthisButtonScale.y;
                        nowPartData.DrawButton(selfRect, deltaTime, () =>
                        {
                            //切换层级
                            mDisMode = 1;
                            m_isDrawActionEvent = true;
                            //m_isDrawDes = false;

                            m_CheckLayerID = nowPartData.mLayerID;
                        });

                        _pos.y += mthisButtonScale.y;
                    }
                    #endregion
                }
            }

#if UNITY_EDITOR
            //监听事件执行
            if (mEventExtrudNames_Length > 0)
            {
                //存留旧事件名称
                int lastLenth = mEventExtrudNames_Main.Length - mEventExtrudNames_Length;
                for (int i = 0; i < lastLenth; i++)
                {
                    mEventExtrudNames_Main[i] = mEventExtrudNames_Main[i + mEventExtrudNames_Length];
                }
                //这批为新事件数据
                for (int i = 0; i < mEventExtrudNames_Length; i++)
                {
                    mEventExtrudNames_Main[lastLenth + i] = mEventExtrudNames[i];
                }
                mEventExtrudNames_Length = 0;
            }
#endif
        }

        public void DrawEventTrack(Rect _rect, ActionEvent actionEvent, ActionState _actionState, Vector2 _scale, bool m_isDrawDes)
        {
#if UNITY_EDITOR
            string _disName;
            string actionEventName = EngineResourcesManager.Instance.GetEventName(actionEvent.EventData);
            if (ActionWindowMain.DicActionEventName_Des.TryGetValue(actionEventName, out string _str))
            {
                _disName = _str;
            }
            else
            {
                _disName = actionEventName;
            }
            float _startTime = (float)actionEvent.TriggerTime / _actionState.TotalTime;
            _startTime = _scale.x * _startTime;
            //selfRect2.x += _startTime;
            _rect.x += _startTime;

            if (actionEvent.Duration < 0 || (actionEvent.TriggerTime + actionEvent.Duration) >= _actionState.TotalTime)
            {
                _rect.width = _scale.x - _startTime;
                //GUI.Box(_rect, "");
                EngineDraw.DrawEventTrack(_rect, actionEventName, Color.grey,
                    false, actionEvent.Inheritable, actionEvent.TriggerTime == 0, true, false);
            }
            else if (actionEvent.Duration == 0)
            {
                _rect.width = 30;
                EngineDraw.DrawEventTrack(_rect, actionEventName, Color.grey,
                    false, actionEvent.Inheritable, actionEvent.TriggerTime == 0, false, true);
                //GUI.Box(_rect, "");
            }
            else
            {
                float _endTime = (float)actionEvent.Duration / _actionState.TotalTime;
                _rect.width = _scale.x * _endTime;
                //Debug.LogWarning($"[{_disName}]: {actionState.Duration}");
                EngineDraw.DrawEventTrack(_rect, actionEventName, Color.grey,
                    false, actionEvent.Inheritable, actionEvent.TriggerTime == 0, false, false);
                //GUI.Box(_rect, "");
            }

            if (m_isDrawDes)
            {
                //将显示名称改为详细参数
                mEditorAction.IsTem = true;
                mEditorAction.TriggerTime = actionEvent.TriggerTime;
                mEditorAction.Duration = actionEvent.Duration;
                mEditorAction.EventData = actionEvent.EventData;
                string _dataStr = ActionEventDescripted.Instance.GetTitle(mEditorAction);
                if (!string.IsNullOrEmpty(_dataStr))
                {
                    _disName = _dataStr;
                }
            }

            GUI.Label(selfRect, _disName, m_isDrawDes ? mGUIStyleEvent : mGUIStyleEvent);//mGUIStyleEvent
                                                                                         //GUI.Label(selfRect, $"Hash:{actionState.GetHashCode()}  ", mGUIStyle_R);
            if (actionEvent.IsTem)
                GUI.Label(selfRect, "   In", mGUIStyle_L);
            //Vector2 _pos = selfRect.position;
            //DrawLine(_pos, new Vector2(_pos.x, _pos.y - _curActionStatePart.CurrentActionEvents.Count * _scale.y), Color.red, 1.0f);
#endif
        }
        private void DrawToScence(float deltaTime)
        {
            if (unit is null || unit.ActionStateMachine is null || unit.ActionStateMachine.AllActionStatePart is null)
                return;
            if (m_CheckLayerID >= unit.ActionStateMachine.AllActionStatePart.Count) return;

            //获取当前Action
            int realyLayer = m_CheckLayerID;
            statePart = unit.ActionStateMachine.AllActionStatePart[realyLayer];
            ActionState _curAction = statePart.CurrentActionState;

            //检查当前Action变化
            if (lastActionState != _curAction)
            {
                ActionStateJumpCount++;
                ChangeActionState(_curAction, statePart);
                lastActionState = _curAction;
            }

            //更新当前经过的时间
            ActionLastTime = statePart.ElapsedTime;

            //绘制
            for (int i = 0; i < actionStates.Length; i++)
            {
                if (ActionStateJumpCount > i)
                {
                    float _pos_X = (actionStates.Length - 2 - i) * (windowsWidth + windowsWidthInteral);
                    DrawAction(actionStates[i], statePart, new Vector2((_pos_X), drawHeight), windowsWidth, deltaTime);
                }
                else
                {
                    break;
                }
            }
            // DrawAction(actionStates[0], statePart, new Vector2(windowsWidth * m_DrawHistory,0), windowsWidth);
        }

        private void DrawAction(SActionStateDrawData _curAction, ActionStatePart _statePart, Vector2 _pos, float _width,
            float _deltaTime)
        {
            Color color = GUI.color;

            int actionCount = _curAction.actionStateNames.Count;

            int mainHeight = childHeight + interal;

            //X轴位置
            _pos.x += _width * (_curAction.AnimTime(_deltaTime));

            //头部高度
            float _headHeight = 20;
            GUI.color = Color.blue;
            GUI.Label(new Rect(_pos.x, _pos.y, _width, mainHeight), $"  层级({m_CheckLayerID})  " + _curAction.actionState.Name);
            GUI.color = color;


            //获得宽高
            Vector2 windowSize = new Vector2(_width, actionCount * mainHeight + _headHeight);

            //绘制背景
            float pintAnim = _curAction.AnimTime2(_deltaTime) * 50;
            GUI.Box(new Rect(_pos.x - pintAnim, _pos.y, windowSize.x + pintAnim * 2, windowSize.y + pintAnim), "");

            //绘制TimeLine
            if (_curAction.isCur)
            {
                float time = ActionLastTime > 0 ? ActionLastTime / _curAction.actionState.TotalTime : 0;
                GUI.Box(
                    new Rect(_pos.x + _width * time, _pos.y + _headHeight, 0,
                        actionCount * mainHeight),
                    "");
            }
            else
            {
                if (_curAction.jumpTime > -0.001f)
                {
                    GUI.Box(
                        new Rect(_pos.x + _width * _curAction.jumpTime, _pos.y + _headHeight, 0,
                            actionCount * mainHeight),
                        "");
                }
            }

            //绘制组内成员
            for (int i = 0; i < actionCount; i++)
            {
                if (i == _curAction.jumpStateIndex)
                {
                    GUI.color = Color.red;
                    Vector2 _childPos = new Vector2(_pos.x, mainHeight * i + 20);
                    GUI.Box(new Rect(_childPos.x, _pos.y + _childPos.y, _width, mainHeight),
                        _curAction.actionStateNames[i]);
                    GUI.color = color;
                }
                else
                {
                    Vector2 _childPos = new Vector2(_pos.x, mainHeight * i + 20);
                    GUI.Box(new Rect(_childPos.x, _pos.y + _childPos.y, _width, mainHeight),
                        _curAction.actionStateNames[i]);
                }
            }
            // Handles.
        }

        private string GetActionName(int _actionID)
        {
            if (unit.ActionStateMachine.GetActionState(_actionID, out ActionState _action))
            {
                return _action.Name;
            }

            return $"Action获取失败[<color=#ffcc00>{_actionID}</color>]";
        }

        //从运行时数据获取层级名称；旧数据未保存名称时返回 null，由调用方回退到层级数字
        private string GetLayerName(int _layerID)
        {
            ActionStateInfo _info = unit?.ActionStateMachine?.ActionStateInfo;
            List<string> _names = _info?.mLayerNames;
            if (_names != null && _layerID >= 0 && _layerID < _names.Count)
            {
                string _name = _names[_layerID];
                if (!string.IsNullOrEmpty(_name)) return _name;
            }
            return null;
        }

        //按内容自适应层级标签宽度，并 clamp 到上下限，避免长名挤占按钮、短名过窄
        private static float CalcLayerLabelWidth(string _text)
        {
            float _width = GUI.skin.box.CalcSize(new GUIContent(_text)).x + 6f;
            return Mathf.Clamp(_width, layerLabelMinWidth, layerLabelMaxWidth);
        }
        private void ChangeActionState(ActionState newActionState, ActionStatePart _part)
        {
            for (int i = actionStates.Length - 1; i > 0; i--)
            {
                actionStates[i].Clone(actionStates[i - 1]);
            }

            stateNames.Clear();
            if (newActionState is null)
            {
                // EngineDebug.LogError("跳转轨为空");
                return;
            }
            //获取当前所有打断轨
            foreach (ActionInterrupt actionState in _part.CurActionInterrupt)
            {
                stateNames.Add(GetActionName(actionState.ActionID));
            }
            //            foreach (ActionInterrupt actionState in newActionState.InterruptList)
            //            {
            //                stateNames.Add(GetActionName(actionState.ActionID));
            //            }

            //            foreach (ActionInterruptGroup interruptGroup in newActionState.InterruptGroupList)
            //            {
            //                if (statePart.ActionStateMachine.GetActionState(interruptGroup.ActionID, out ActionState actionState))
            //                {
            //                    stateNames.Add("打断组：" + actionState.Name);
            //                    if (actionState.InterruptList.Count != interruptGroup.ActionHide.Count)
            //                    {
            //                        foreach (var _target in actionState.InterruptList)
            //                        {
            //                            stateNames.Add(GetActionName(_target.ActionID));
            //                        }
            //// #if UNITY_EDITOR
            ////                         EngineDebug.LogWarning("序列化可能存在错误，打断组配置丢失");
            //// #endif
            //                        continue;
            //                    }
            //                    for (int i = 0; i < actionState.InterruptList.Count; i++)
            //                    {
            //                        // if (!interruptGroup.ActionHide[i])
            //                        // {
            //                        //     stateNames.Add(GetActionName(actionState.InterruptList[i].ActionID));
            //                        // }
            //                        if (!interruptGroup.ActionHide.Contains(actionState.InterruptList[i].ActionID))
            //                        {
            //                            stateNames.Add(GetActionName(actionState.InterruptList[i].ActionID));
            //                        }
            //                    }
            //                }
            //            }

            if (!string.IsNullOrEmpty(newActionState.DefaultAction))
            {
                stateNames.Add("结束后默认衔接的动画：");
                stateNames.Add(newActionState.DefaultAction);
            }


            actionStates[1].isCur = false;
            actionStates[1].jumpTime =
                ActionLastTime > 0 ? ActionLastTime / actionStates[1].actionState.TotalTime : 0;
            actionStates[1].jumpStateIndex = -1;
            int findID = 0;
            foreach (string name in actionStates[1].actionStateNames)
            {
                if (name == newActionState.Name)
                {
                    actionStates[1].jumpStateIndex = findID;
                    break;
                }

                findID++;
            }

            actionStates[0].actionState = newActionState;
            actionStates[0].actionStateNames.Clear();
            actionStates[0].actionStateNames.AddRange(stateNames);
            actionStates[0].isCur = true;
            actionStates[0].jumpTime = -1;
            actionStates[0].jumpStateIndex = -1;

            foreach (SActionStateDrawData VARIABLE in actionStates)
            {
                VARIABLE.StartAnim(m_AnimTime);
            }
        }
        //#endif
        void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            // 保存当前模型视图矩阵
            GL.PushMatrix();

            // 设置绘制模式为线条
            GL.Begin(GL.LINES);

            // 设置材质和颜色
            if (!lineMaterial)
            {
                lineMaterial = new Material(Shader.Find("UI/Default"));
            }
            lineMaterial.SetPass(0);
            GL.Color(color);

            // 设置线宽（GL本身不支持线宽，但可以通过绘制多条线模拟）
            // 这里我们简单绘制一条线，如需宽度可扩展为绘制矩形
            GL.Vertex3(start.x, start.y, 0);
            GL.Vertex3(end.x, end.y, 0);

            GL.End();
            GL.PopMatrix();
        }
    }
}
