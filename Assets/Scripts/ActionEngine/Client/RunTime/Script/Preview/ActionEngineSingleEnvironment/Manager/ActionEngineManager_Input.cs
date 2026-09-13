using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime;
// #if Cinemachine
#if UNITY_6000_0_OR_NEWER
using Unity.Cinemachine;
#else
        using Cinemachine;
#endif
// #endif
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public partial class ActionEngineManager_Input
    {
        #region Instance
        private static ActionEngineManager_Input _instance;
        public static ActionEngineManager_Input Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = new ActionEngineManager_Input();
                }

                return _instance;
            }
        }


        public ActionEngineManager CreateGameManager() => ActionEngineManager.Instance;

        #endregion

        #region 结构体
        //连击元素
        private class ComboKeyElement
        {
            public int keyID;
            public float keyTime;

            public ComboKeyElement(int _keyID, float _keyTime)
            {
                keyID = _keyID;
                keyTime = _keyTime;
            }
        }


        #endregion

        #region 连击表有效按键
        private readonly KeyCode[] KeyMap = new[]
        {
            KeyCode.W,
            KeyCode.A,
            KeyCode.S,
            KeyCode.D,
            KeyCode.J,
            KeyCode.K,
            KeyCode.L,
            KeyCode.I,
            KeyCode.O,
            KeyCode.P,
            KeyCode.U,
            KeyCode.Space,
        };

        private readonly int[] MoustButtonMap = new[]
        {
            0,
            1
        };
        #endregion

        private const float ComboTime = 0.5f;//连击按键最大等待时间

        private int selectID;
        private int comboMaxLenth;
        private bool mouseDis = true;
        private bool mIsController = true;
        private float mCamRotX = 0f;
        private float mCamRotY = 0f;
        private float mCamRotX_L = 0f;
        private float mCamRotY_L = 0f;
        private float mRotSpeed = 1;
        private readonly float mRotSpeed_L = 18;
        private Vector3 mInputMoveDir;
        private Vector2 mInputViewDir;
        private InputModuleInfo mInputModuleInfo = null;

        private Dictionary<KeyCode, string> ActionCheckKey = new Dictionary<KeyCode, string>();
        private Dictionary<int, string> ActionCheckMouseKey = new Dictionary<int, string>();
        private List<Action<ActionEngine_Unit>> mWaitPlayerLoad = new List<Action<ActionEngine_Unit>>();
        private List<KeyCombinations> ActionCombo = new List<KeyCombinations>();
        private List<ComboKeyElement> ActionComboElementList = new List<ComboKeyElement>();

        public CameraControl CurCamera;
        public Vector3 CamOffsetPos;
        public Vector3 CamGlobalOffsetPos;
        public InputModuleInfo InputModuleInfo => mInputModuleInfo;
        private Action mInputModel = null;

        public bool UpdateEnble
        {
            get { return mIsController; }
            set { mIsController = value; }
        }
        public ActionEngine_Unit Player { get; private set; } = null;
        public void WaitPlayerLoad(Action<ActionEngine_Unit> _waitLoad)
        {
            if (Player is not null)
            {
                _waitLoad(Player);
                return;
            }
            mWaitPlayerLoad.Add(_waitLoad);
        }
        public Camera PlayerCam { get; private set; }
        public CinemachineBrain CinemachineBrain { get; private set; }

        public void ChangePlayer(ActionEngine_Unit _unit, bool _inheritanceCam = false)
        {
            //如果是给空值，停止任何逻辑
            if (_unit is null)
            {
                CurCamera = null;
                Player = null;
                return;
            }
            if (Player is not null)
            {
                if (Player.TryGetComponent(out UnitEditorPreview _lasteditorPreview))
                {
                    _lasteditorPreview.mIsPlayer = false;
                }

                if (_inheritanceCam)
                {
                    _unit.ActionStateMachine.SetMouseXY(Player.ActionStateMachine.GetCamPointRot());
                }

                CamGlobalOffsetPos = Player.transform.position - _unit.transform.position;
            }

            if (_unit.TryGetComponent(out UnitEditorPreview _editorPreview))
            {
                _editorPreview.mIsPlayer = true;
            }

            if(_unit.Channel == ERuntimeDataChannel.Local)
            {
                ChangePlayerCamera(Player, _unit);

                mCamRotY = _unit.ActionStateMachine.GetCamPointRot().eulerAngles.y;
                mCamRotX = _unit.ActionStateMachine.GetCamPointRot().eulerAngles.x;
            }

            ApplyInputModuleToPlayer(_unit);

            Player = _unit;
            ActionEngineManager.Instance.InjectAttributesToUnit(Player);
            foreach (Action<ActionEngine_Unit> _waitLoad in mWaitPlayerLoad)
            {
                _waitLoad(Player);
            }
            mWaitPlayerLoad.Clear();
            EngineDebug.Log($"设置了玩家: [{Player.transform.name}]");
        }

        public void SetInputModel(Action _inputModel) => OnSetInputModel(_inputModel);

        public bool IsPlayer(ActionEngine_Unit _unit)
        {
            return Player == _unit;
        }

        public void SetCamRotSpeed(float _speed)
        {
            mRotSpeed = _speed;
        }

        public void Init(Action _inputModel = null)
        {
            //清空数据
            Player = null;

            // 无表现层的端不注册相机（Host 承担服务器职责但需要相机，故按 IsHeadless 而非 IsServer 判定）；
            // 客户端缺少 MainCamera 时保持为空，由后续场景自行补齐。
            if (ActionEngineManager.Instance.IsHeadless)
            {
                PlayerCam = null;
                CinemachineBrain = null;
            }
            else
            {
                PlayerCam = Camera.main;
                CinemachineBrain = PlayerCam == null ? null : PlayerCam.GetComponent<CinemachineBrain>();
            }

            //加载配置表
            ActionEnginLoadData.Instance.LoadInfo(EInfoType.InputModule,
                (_target) =>
                {
                    if (_target is InputModuleInfo _module)
                    {
                        mInputModuleInfo = _module;
                        selectID = mInputModuleInfo.InputSystemID;
                        //注册按键检测
                        SwitchInputInfo(selectID);
                        ApplyInputModuleToPlayer(Player);
                    }
                },
                0
            );

            mInputModel = _inputModel;

            // SetMoseDisPlay(false);
        }

        private void ApplyInputModuleToPlayer(ActionEngine_Unit _unit)
        {
            if (_unit is null || mInputModuleInfo is null)
            {
                return;
            }

            _unit.ActionStateMachine.SetHoldKeyIntervalTime(mInputModuleInfo.LongPressInput);
        }

        public void Update(float _deltaTime)
        {
            // Unity 的 == 重写能检测"已销毁但 C# 引用还在"的对象；is null 不能，故必须用 ==
            if (Player == null)
            {
                return;
            }

            if (!mIsController) return;//禁用玩家单位输入

            if (mInputModel is not null)
            {
                mInputModel();
            }
            else
            {
                //if (Input.GetKeyDown(KeyCode.M))
                //{
                //    //删除测试
                //}

                CheckKey();

                // 切场景会销毁旧 Camera.main，PlayerCam 仍持有已销毁引用，这里重抓一次；仍拿不到就跳过本帧
                if (PlayerCam == null)
                {
                    PlayerCam = Camera.main;
                    if (PlayerCam == null)
                    {
                        return;
                    }
                }

                Player.ActionStateMachine.SetCamRot(Quaternion.Euler(0, PlayerCam.transform.eulerAngles.y, 0));
                Player.ActionStateMachine.SetMouseXY(Quaternion.Euler(mCamRotY, mCamRotX, 0));//playerCam.transform.rotation
                Player.ActionStateMachine.GetCharacterFor = PlayerCam.transform.rotation;

                //工具用的
                if (CheckKey(mInputModuleInfo.SloMotionKey, 0))
                    Time.timeScale = mInputModuleInfo.SloMotionScale;
                if (CheckKey(mInputModuleInfo.SloMotionKey, 1))
                    Time.timeScale = 1;
                //#if !UNITY_EDITOR
                //            if (Input.GetMouseButtonDown(2)) SetMoseDisPlay(!Cursor.visible);
                //#endif
            }

        }

        public void LateUpdate(float _deltaTime)
        {
            if (CurCamera is not null)
                CurCamera.OnUpdate(_deltaTime);
        }

        public void Input_Look(Vector2 _deltaRot)
        {
            if (!mIsController) return;//禁用玩家单位输入
            mInputViewDir = _deltaRot;
        }

        private bool m_IsInputMove_Pre = false;
        public void Input_Move(Vector2 _deltaMove)
        {
            if (!mIsController) return;//禁用玩家单位输入

            mInputMoveDir.x = _deltaMove.x;
            mInputMoveDir.z = _deltaMove.y;
            if (_deltaMove != Vector2.zero)
            {
                if (!m_IsInputMove_Pre)
                {
                    m_IsInputMove_Pre = true;
                }
                if (PlayerCam == null)
                {
                    PlayerCam = Camera.main;
                    if (PlayerCam == null)
                    {
                        return;
                    }
                }
                if (Player == null)
                {
                    return;
                }
                float rotY = PlayerCam.transform.rotation.eulerAngles.y;
                Player.ActionStateMachine.SetMoveInput(mInputMoveDir, Quaternion.Euler(0, rotY, 0) * mInputMoveDir);
            }
            else
            {
                if (m_IsInputMove_Pre)
                {
                    m_IsInputMove_Pre = false;
                    Player.ActionStateMachine.SetMoveInputStop();
                }
            }
        }

        public bool CheckKey(int _key, byte keyType)
        {
            if (_key < 0)
            {
                if (keyType == 0) return Input.GetMouseButtonDown(_key * -1 - 1);
                else if (keyType == 1) return Input.GetMouseButtonUp(_key * -1 - 1);
            }
            else
            {
                if (keyType == 0) return Input.GetKeyDown((KeyCode)_key);
                else if (keyType == 1) return Input.GetKeyUp((KeyCode)_key);
            }
            return false;
        }

        private void CheckKey()
        {
            //if (!mouseDis)
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
                Input_Look(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")));
#endif
                mCamRotX = Player.ActionStateMachine.GetCamPointRot().eulerAngles.y + mInputViewDir.x * mRotSpeed;
                mCamRotY = Player.ActionStateMachine.GetCamPointRot().eulerAngles.x - mInputViewDir.y * mRotSpeed;

                if (mCamRotY < 180)
                {
                    mCamRotY = Mathf.Min(80, mCamRotY);
                }
                else
                {
                    mCamRotY = Mathf.Max(320, mCamRotY);
                }
            }
            //else
            //{
            //    return;
            //}

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
            //位置输入
            // mInputMoveDir = Vector3.zero;
            // if (Input.GetKey(KeyCode.W)) mInputMoveDir.z++;
            // if (Input.GetKey(KeyCode.S)) mInputMoveDir.z--;
            // if (Input.GetKey(KeyCode.A)) mInputMoveDir.x--;
            // if (Input.GetKey(KeyCode.D)) mInputMoveDir.x++;

            Vector2 _InputMoveDir = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) _InputMoveDir.y++;
            if (Input.GetKey(KeyCode.S)) _InputMoveDir.y--;
            if (Input.GetKey(KeyCode.A)) _InputMoveDir.x--;
            if (Input.GetKey(KeyCode.D)) _InputMoveDir.x++;
            Input_Move(_InputMoveDir);


            //连击输入判断
            if (!CheckComboKey())
            {
                //常规键鼠输入
                foreach (var _KeyValue in ActionCheckKey)
                {
                    if (Input.GetKeyDown(_KeyValue.Key)) Player.ActionStateMachine.SetKeyDown(_KeyValue.Value);
                    if (Input.GetKeyUp(_KeyValue.Key)) Player.ActionStateMachine.SetKeyUp(_KeyValue.Value);
                }

                bool _pointerOverUI = IsPointerOverUI();
                foreach (var _KeyValue in ActionCheckMouseKey)
                {
                    // 指针在 UI 上时只放行 Up，避免 UI 点击击穿触发 ActionEngine 的鼠标键 Action（攻击/移动等）。
                    // Up 始终放行，防止 Down 被吃掉后状态机停留在按下态。
                    if (Input.GetMouseButtonDown(_KeyValue.Key) && !_pointerOverUI) Player.ActionStateMachine.SetKeyDown(_KeyValue.Value);
                    if (Input.GetMouseButtonUp(_KeyValue.Key)) Player.ActionStateMachine.SetKeyUp(_KeyValue.Value);
                }
            }
#endif

        }

        /// <summary>
        /// 指针当前是否在某个 UI 元素上（基于 EventSystem + GraphicRaycaster）。
        /// 用于在 UI 命中时屏蔽 Action/Combo 的鼠标键派发，避免 UI 点击击穿触发动作。
        /// </summary>
        private static bool IsPointerOverUI()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }

        public void SetMoseDisPlay(bool _state)
        {
            mouseDis = _state;
            if (mouseDis)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        // private void SetCam(float _deltatime)
        // {
        //     if (!Player)
        //     {
        //         return;
        //     }
        //
        //     mCamRotX_L = Mathf.Lerp(mCamRotX_L, mCamRotX, mRotSpeed_L * _deltatime);
        //     mCamRotY_L = Mathf.Lerp(mCamRotY_L, mCamRotY, mRotSpeed_L * _deltatime);
        //
        //     Transform _camTrans = PlayerCam.transform;
        //     _camTrans.rotation = Quaternion.Euler(mCamRotY_L, mCamRotX_L, 0);
        //     _camTrans.position = Player.transform.TransformPoint(0, CamOffsetPos.y, 0) + CamGlobalOffsetPos;
        //     _camTrans.Translate(CamOffsetPos.x, 0, CamOffsetPos.z);
        // }

        private bool CheckComboKey()
        {
            float _nowTime = Time.time;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR

            foreach (var _keyCode in KeyMap)
            {
                if (Input.GetKeyDown(_keyCode))
                {
                    // EngineDebug.Log($"按下了: {_keyCode}");
                    AddComboElement((int)_keyCode, _nowTime);
                    break;
                }
            }//检查键盘按键

            // 指针在 UI 上时不把鼠标 Down 喂给连击表，避免点 UI 顺带触发武器连击的首键。
            bool _pointerOverUI_Combo = IsPointerOverUI();
            foreach (var _mouseButton in MoustButtonMap)
            {
                if (_pointerOverUI_Combo) break;
                if (Input.GetMouseButtonDown(_mouseButton))
                {
                    // EngineDebug.Log($"按下了: {(_mouseButton == 0 ? "鼠标左键" : "鼠标右键")}");
                    AddComboElement(-(_mouseButton + 1), _nowTime);
                    break;
                }
            }//检查鼠标按键
            for (int i = 0; i < ActionComboElementList.Count; i++)
            {
                ComboKeyElement _ActionComboElement = ActionComboElementList[i];
                float _deltaTime = _nowTime - _ActionComboElement.keyTime;
                if (_deltaTime > ComboTime)
                {
                    ActionComboElementList.RemoveAt(0);
                    i--;
                }
                else
                {
                    break;
                }
            }//清理过时连击按键

            bool _isComboKey = false;//是完整的连击列表！
            foreach (var _keyCombo in ActionCombo)
            {
                int _findFastKey = -1;//找到了连击列表第一个按键
                foreach (var _key in _keyCombo.keyGroup)
                {
                    if (_findFastKey < 0)
                    {
                        for (int i = 0; i < ActionComboElementList.Count; i++)
                        {
                            if (_key == ActionComboElementList[i].keyID)
                            {
                                _findFastKey = i;
                                break;
                            }
                        }
                    }//寻找连击列表中的第一个按键
                    else
                    {
                        if (ActionComboElementList.Count < _findFastKey + _keyCombo.keyGroup.Count)
                        {
                            // EngineDebug.Log("找到了第一个按键，但是长度超出");
                            break;
                        }//检查已有的连击列表数量是否足以支撑当前完整列表

                        _isComboKey = true;
                        for (int i = 1; i < _keyCombo.keyGroup.Count; i++)
                        {
                            if (_keyCombo.keyGroup[i] != ActionComboElementList[_findFastKey + i].keyID)
                            {
                                // string keyGroup = "KeyID\n";
                                // foreach (var VARIABLE in _keyCombo.keyGroup)
                                // {
                                //     keyGroup += $"ID: {VARIABLE}\n";
                                // }
                                // keyGroup += "ElementID\n";
                                // foreach (var VARIABLE in ActionComboElementList)
                                // {
                                //     keyGroup += $"ID: {VARIABLE.keyID}\n";
                                // }
                                //
                                // keyGroup += $"\nfindFastKey: {_findFastKey}\n";
                                // keyGroup += $"\nIndex: {i}\n";
                                // EngineDebug.Log(keyGroup);

                                _isComboKey = false;
                                break;
                            }
                        }

                        if (_isComboKey)
                        {
                            //连击列表成立！！ 发送技能请求
                            // EngineDebug.Log($"连击成立: {_keyCombo.mAction}");
                            Player.ActionStateMachine.SendKeyDown(_keyCombo.mAction);
                            ActionComboElementList.Clear();
                            return true;
                        }
                    }//找到了第一个按键，从第二个按钮开始核对剩余列表
                }//组里面的每个按键

            }//所有的连击组
#endif

            return false;
        }

        private void OnSetInputModel(Action _inputModel)
        {
            mInputModel = _inputModel;
        }
        private void AddComboElement(int _keyID, float _time)
        {
            if (ActionComboElementList.Count > comboMaxLenth)
            {
                ActionComboElementList.RemoveAt(0);
            }//超出连击列表最大上限，从第一个元素开始清理
            ActionComboElementList.Add(new ComboKeyElement(_keyID, _time));
        }

        private void SwitchInputInfo(int _id)
        {
            ActionCheckKey.Clear();
            ActionCheckMouseKey.Clear();
            InputModulePart _modulePart = mInputModuleInfo.InputModulePart[_id];
            foreach (var _action in _modulePart.keyActions)
            {
                ActionCheckKey.TryAdd(_action.KeyCode, _action.mAction);
            }
            foreach (var _action in _modulePart.mouseActions)
            {
                ActionCheckMouseKey.TryAdd(_action.mouseButton, _action.mAction);
            }

            ActionCombo = _modulePart.ComboInputActions;
            ActionComboElementList.Clear();

            comboMaxLenth = 0;
            foreach (var _key in ActionCombo)
            {
                if (_key.keyGroup.Count > comboMaxLenth)
                {
                    comboMaxLenth = _key.keyGroup.Count;
                }
            }//寻找当前输入组合里最长的连击按键数量
        }

        private void ChangePlayerCamera(ActionEngine_Unit _lastPlayer, ActionEngine_Unit _newPlayer)
        {
            ActionEngineManager_Unit.Instance.CreactCamera(_newPlayer.CameraID, _newPlayer, (CameraControl _came) =>
            {
                if (_came is null)
                {
                    EngineDebug.LogError("相机切换失败！！");
                    return;
                }

                if (_lastPlayer is not null)
                {
                    string key = ActionEngineRuntimePath.DataName_Cam + _lastPlayer.CameraID;
                    ActionEngineManager_Unit.Instance.DestoryCam(key, CurCamera);
                }

                CurCamera = _came;
            });
        }
    }
}