
#if UNITY_EDITOR
using System.Collections.Generic;
using AsiActionEngine.RunTime.DrawData;
using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using UnityEditor;
#endif
using UnityEngine;

namespace AsiTimeLine.RunTime
{

#if UNITY_EDITOR
    public class ActionTimeLineUpdate : MonoBehaviour
    {
        [Header("是否绘制编辑器图形(高消耗)")]
        public bool DrawEditorEvent = true;

        private GameObject mSelect = null;
        private float deltaTime = 0.0f;
        private ActionEngine_Unit lastUnit = null;
        private float nowUnitTime;
        private int actionID;
        private int onReadDrawGizmos = 10;

        private int scenceLogWidth = 550;
        private int scenceLogHeight = 20;
        private void Start()
        {
            TimeLineWindow.Instance.RunTimeStart();
            mSelect = null;
            Selection.activeGameObject = null;
        }

        private bool GetEditorModeEnble
        {
            get => PlayerPrefs.GetInt("ScenceDraw_EditorModel", 0) > 0;
        }

        private void Update()
        {
            //关闭绘制
            onReadDrawGizmos--;
            if (onReadDrawGizmos > 0)
            {
                if (!EngineDrawListData.Instance.DrawState)
                {
                    EngineDrawListData.Instance.DrawState = true;
                }
            }
            else
            {
                if (EngineDrawListData.Instance.DrawState)
                {
                    EngineDrawListData.Instance.DrawState = false;
                    EngineDrawListData.Instance.Init();
                }
            }
            // Debug.Log("我的Update");
            deltaTime = Time.deltaTime;

            if (ResourcesWindow.Instance.ActionStatePart is null)
            {
                if (ActionEngineManager_Input.Instance.Player is not null)
                {
                    ResourcesWindow.Instance.ActionStatePart =
                        ActionEngineManager_Input.Instance.Player.ActionStateMachine.AllActionStatePart[0];
                }
            }

            if (GetEditorModeEnble)
            {

                GameObject _nowObj = Selection.activeGameObject;
                if (mSelect != _nowObj)
                {
                    mSelect = _nowObj;

                    if (_nowObj == null) return;
                    if (_nowObj.TryGetComponent(out ITargetUnit _ITargetUnit))
                    {
                        ActionEngine_Unit _unit = _ITargetUnit.GetUnit();
                        if (_unit is ActionEngine_Skill) return;//不预览技能
                        if (lastUnit != _unit)
                        {
                            if (lastUnit is not null)
                            {
                                //更新RunTime数据
                                int lastactionID = ResourcesWindow.Instance.GetActionToCurID();
                                int lastTime = TimeLineWindow.Instance.NowTime;
                                lastUnit.ActionStateMachine.AnimEnble = false;
                                lastUnit.ActionStateMachine.ChangeAction(lastactionID, 0, lastTime);
                                lastUnit.ActionStateMachine.AnimEnble = true;
                            }
                            lastUnit = _unit;

                            nowUnitTime = _unit.ActionStateMachine.AllActionStatePart[0].ElapsedTime;
                            actionID = _unit.ActionStateMachine.AllActionStatePart[0].CurrentActionState.ID;

                            //禁止更新动画状态
                            EventUpdate.Instance.UpdateAnim = false;

                            //获取相机注视对象
                            int lastCameID = 0;
                            Transform cam_Follow = null;
                            Transform cam_Look = null;
                            CameraControl curCameraControl = ActionEngineManager_Input.Instance.CurCamera;
                            if (curCameraControl == null)
                            {
                                EngineDebug.LogWarning("获取不到当前正在使用的相机");
                            }
                            else
                            {
                                lastCameID = curCameraControl.CamID;
                                cam_Follow = curCameraControl.followTarget_cam;
                                cam_Look = curCameraControl.lookTarget_cam;
                            }


                            //切换到该Unit单位
                            ResourcesWindow.Instance.OnlySetRole(_nowObj);
                            TimeLineWindow.Instance.RunTimeUpdateUnit(_unit, actionID);
                            ResourcesWindow.Instance.SetRole(_nowObj);

                            //注册Action状态机
                            ResourcesWindow.Instance.ActionStatePart = _unit.ActionStateMachine.AllActionStatePart[0];

                            //更新相机
                            if (ActionEngineManager_Input.Instance.IsPlayer(_unit))
                            {
                                CameraControl _cameraControl = ActionEngineManager_Input.Instance.CurCamera;
                                if (_unit.ActionStateMachine.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig)))
                                {
                                    if (_config.HelpPointDic.TryGetValue(ECharacteLimbType.Cam_Main, out Transform _trans))
                                    {
                                        _cameraControl.OnInit(_trans, _unit.ActionStateMachine, 0);
                                    }
                                }
                                _cameraControl.ChangeCam(lastCameID, cam_Follow, cam_Look);
                                // EngineDebug.Log("相机: " + (_cameraControl != null));
                                ResourcesWindow.Instance.SetPreCamera(_cameraControl);
                            }

                            //切换鼠标状态
                            ActionEngineManager_Input.Instance.SetMoseDisPlay(true);
                            TimeLineWindow.Instance.SetActionEditorState(true, nowUnitTime);

                            //更新状态
                            EventUpdate.Instance.UpdateAnim = true;
                            TimeLineWindow.Instance.SetSelectUnit(_unit, nowUnitTime);

                            ActionEngineManager.Instance.OnUpdateEnble = !Cursor.visible;
                        }
                    }
                }

                //鼠标输入
                ActionEngineManager_Input _input = ActionEngineManager_Input.Instance;
                if (ActionEngineManager_Input.Instance.InputModuleInfo is not null)
                {
                    bool _isValid = false;
                    //退出Runtime  进入编辑模式
                    int _enterKey = ActionEngineManager_Input.Instance.InputModuleInfo.ExitRunTimeKey;
                    if (_input.CheckKey(_enterKey, 0))
                    {
                        if (ActionEngineManager.Instance.OnUpdateEnble)
                        {
                            //bool _DisState = Cursor.visible;

                            if ((int)KeyCode.Escape != _enterKey)
                                ActionEngineManager_Input.Instance.SetMoseDisPlay(true);
                            TimeLineWindow.Instance.SetActionEditorState(true);
                            ActionEngineManager.Instance.OnUpdateEnble = false;
                            _isValid = true;
                            //if (!_DisState)
                            //{
                            //    if (lastUnit is not null)
                            //    {
                            //        int lastactionID = ResourcesWindow.Instance.GetActionToCurID();
                            //        int lastTime = TimeLineWindow.Instance.NowTime;
                            //        lastUnit.ActionStateMachine.ChangeAction(lastactionID,0,lastTime);
                            //    }
                            //}
                        }
                    }

                    //退出Runtime  进入编辑模式
                    if (!_isValid && _input.CheckKey(ActionEngineManager_Input.Instance.InputModuleInfo.EnterRunTimeKey, 0))
                    {
                        if (!ActionEngineManager.Instance.OnUpdateEnble)
                        {
                            //ActionEngineManager_Input.Instance.SetMoseDisPlay(false);
                            TimeLineWindow.Instance.SetActionEditorState(false);
                            ActionEngineManager.Instance.OnUpdateEnble = true;
                            //if (!Cursor.visible)
                            {
                                if (lastUnit is not null)
                                {
                                    int lastactionID = ResourcesWindow.Instance.GetActionToCurID();
                                    int lastTime = TimeLineWindow.Instance.NowTime;
                                    lastUnit.ActionStateMachine.ChangeAction(lastactionID, 0, lastTime);
                                }
                            }
                        }
                        //Debug.LogWarning($"编辑模式: " + TimeLineWindow.Instance.IsEditor);
                    }

                    //鼠标显隐
                    if (_input.CheckKey(ActionEngineManager_Input.Instance.InputModuleInfo.DisMouse, 0))
                    {
                        //ActionEngineManager_Input.Instance.SetMoseDisPlay(!Cursor.visible);
                    }
                }

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (ActionEngineManager.Instance.OnUpdateEnble)
                    {
                        //ActionEngineManager_Input.Instance.SetMoseDisPlay(true);
                        TimeLineWindow.Instance.SetActionEditorState(true);
                        ActionEngineManager.Instance.OnUpdateEnble = false;
                    }
                }
            }
            //每帧更新TimeLine窗口
            TimeLineWindow.Instance.RuntimeUpdateFrame(Time.deltaTime);
            onDraw = true;
        }
        private void OnGUI()
        {
            if (PlayerPrefs.GetInt("ScenceDraw_EngineLog", 0) == 1)
            {
                //Log切换按钮
                int engineLogType = PlayerPrefs.GetInt("LogType", 0);
                Rect button = new Rect(Screen.width - scenceLogWidth - 20, 0, 20, 20);
                if (GUI.Button(button, EditorGUIUtility.IconContent("console.infoicon@2x")))
                {
                    PlayerPrefs.SetInt("LogType", 0);
                    PlayerPrefs.Save();
                }
                if (engineLogType == 0) GUI.Box(button, "");
                button.y += button.height;
                if (GUI.Button(button, EditorGUIUtility.IconContent("console.warnicon@2x")))
                {
                    PlayerPrefs.SetInt("LogType", 1);
                    PlayerPrefs.Save();
                }
                if (engineLogType == 1) GUI.Box(button, "");
                button.y += button.height;
                if (GUI.Button(button, EditorGUIUtility.IconContent("console.erroricon@2x")))
                {
                    PlayerPrefs.SetInt("LogType", 2);
                    PlayerPrefs.Save();
                }
                if (engineLogType == 2) GUI.Box(button, "");

                if (EngineDebug.ScenceLogList().TryGetValue(engineLogType, out List<string> _strList))
                {
                    if (_strList.Count > 0)
                    {
                        Rect _rect = new Rect(Screen.width - scenceLogWidth, 0, scenceLogWidth, _strList.Count * scenceLogHeight);
                        GUI.Box(_rect, "");

                        _rect.height = scenceLogHeight;
                        foreach (string str in _strList)
                        {
                            GUI.Box(_rect, "");
                            GUI.Label(_rect, str);
                            _rect.y += scenceLogHeight;
                        }
                    }
                }
            }
        }
        private bool onDraw = false;
        //private float DrawTime = 0.0f;
        private void OnDrawGizmos()
        {
            if (!DrawEditorEvent) return;
            if (!onDraw)
            {
                EngineDrawListData.Instance.Draw(0);
                return;
            }
            onReadDrawGizmos = 10;//关闭绘制延时
            onDraw = false;
            //运行时不绘制
            if (!ActionWindowMain.ScenceDraw_Runtime) return;
            if (!ActionWindowMain.ScenceDraw_Event && !ActionWindowMain.ScenceDraw_Interrupt) return;

            //Debug.Log($"绘制视图单位数量: [{ActionEngineManager_Unit.Instance.Units.Count}]");
            // if (!TimeLineWindow.Instance.IsEditor)
            {
                //运行中非编辑时执行所有单位的绘制
                // EngineDrawListData.Instance.Init();
                //EngineDebug.LogWarning("绘制的单位有几个: " + ActionEngineManager_Unit.Instance.Units.Count);
                foreach (ActionEngine_Unit _unit in ActionEngineManager_Unit.Instance.Units)
                {
                    bool isDrawEditor = _unit == TimeLineWindow.Instance.SelectUnit;

                    _unit.ActionStateMachine.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig));

                    //if (_config is null)
                    //{
                    //    EngineDebug.LogError($"<color=#ffcc00>Config组件丢失</color> [{_unit.gameObject.name}]");
                    //    continue;
                    //}

                    foreach (ActionStatePart _statePart in _unit.ActionStateMachine.AllActionStatePart)
                    {
                        if (DrawActionStatePart(_unit, _statePart, _config, isDrawEditor)) continue;
                    }
                    foreach (ActionStatePart _statePart in _unit.ActionStateMachine.AllActionStatePart_Tmp)
                    {
                        if (DrawActionStatePart(_unit, _statePart, _config, isDrawEditor)) continue;
                    }
                }
                EngineDrawListData.Instance.Draw(deltaTime);
            }
        }

        private bool DrawActionStatePart(ActionEngine_Unit _unit, ActionStatePart _statePart,
            CharacterConfig _config, bool isDrawEditor)
        {
            if (ActionWindowMain.ScenceDraw_Event && _statePart.CurrentActionState is not null)
            {
                if (!isDrawEditor)
                {
                    foreach (ActionEvent _event in _statePart.CurrentActionEvents)
                    {
                        _event.EventData.EditorDraw(_config, _statePart,
                            new ActionMachineTime(Time.deltaTime, _statePart.ElapsedTime,
                                _event.TriggerTime, _event.Duration));
                    }
                }
                else
                {
                    if (ResourcesWindow.Instance.ActionStateDic.TryGetValue(
                            _statePart.CurrentActionState.ID, out EditorActionState _actionState))
                    {
                        foreach (ActionTrackGroup _actionTrackGroup in _actionState.AllEventTrackGroup)
                        {
                            foreach (IActionTrack _actionTrack in _actionTrackGroup.CurActiontTrack)
                            {
                                if (_actionTrack is EventTrack _eventTrack)
                                {
                                    foreach (EventDisplay _eventDisplay in _eventTrack.CurEventDisplay)
                                    {
                                        EditorActionEvent _actionEvent = _eventDisplay.MainEvent;
                                        _actionEvent.EventData.EditorDraw(_config,
                                            ResourcesWindow.Instance.ActionStatePart,
                                            new ActionMachineTime(Time.deltaTime,
                                                _statePart.ElapsedTime, _actionEvent.TriggerTime,
                                                _actionEvent.Duration));
                                    }
                                }
                            }
                        }
                        // _events = _actionState.EventList;
                        // EngineDebug.Log("事件轨数量: " + _events.Count);
                    }
                    else
                    {
                        EngineDebug.LogError("不存在？？");
                        return true;
                    }
                }
            }

            if (ActionWindowMain.ScenceDraw_Interrupt)
            {
                foreach (ActionInterrupt _interrupt in _statePart.CurActionInterrupt)
                {
                    foreach (IInterruptCondition _condition in _interrupt.InterruptConditionList)
                    {
                        _condition.EditorDraw(_unit, _config,
                            new ActionMachineTime(Time.deltaTime, _statePart.ElapsedTime,
                                _interrupt.TriggerTime, _interrupt.Duration));
                    }
                }
            }
            return false;
        }
    }
#endif

}