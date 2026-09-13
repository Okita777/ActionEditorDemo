using System;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;

#if Cinemachine
#if UNITY_6000_0_OR_NEWER
#else
        using Cinemachine;
#endif
#endif
using UnityEngine;
using AsiActionEngine.RunTime.Event_Extend;

namespace AsiActionEditor_Ex.RunTime
{
    [System.Serializable]
    public class Event_CameraChange : IActionEventData
    {
        [SerializeField] private bool mOnHitter = false;
        [SerializeField] private bool mOnlyPlayer = false;
        [SerializeField] private bool mChangeCamPoint = false;

        // [SerializeField] private bool mResetPlayerCam = false;
        [SerializeField] private int mEnterCam = 0;
        [SerializeField] private int mEnterPoint = (int)ECharacteLimbType.Cam_Main;
        [SerializeField] private float mEnterTime = 1.0f;
        [SerializeField] private bool mSwitchLookAt = false;
        [SerializeField] private GUnit mGUnit = new GUnit();
        [SerializeField] private ECharacteLimbType mTargetPoint = ECharacteLimbType.Cam_Main;
        [SerializeField] private GValue_Ratio gValue_Ratio = new GValue_Ratio();
        [SerializeField] private bool mExitCam = true;

        [NonSerialized] private bool lastRatioState = false;
        [NonSerialized] private bool RatioState = false;

        #region Property
        [EditorProperty("仅当前单位为Player（操作对象）时有效： ", EditorPropertyType.EEPT_Bool, LabelWidth = 230)]
        public bool OnlyPlayer
        {
            get { return mOnlyPlayer; }
            set { mOnlyPlayer = value; }
        }

        [EditorProperty("命中时进入相机： ", EditorPropertyType.EEPT_Bool)]
        public bool OnHitter
        {
            get { return mOnHitter; }
            set { mOnHitter = value; }
        }

        [EditorProperty("相机进入条件： ", EditorPropertyType.EEPT_GValueSRatio)]
        public GValue_Ratio Ratio
        {
            get { return gValue_Ratio; }
            set { gValue_Ratio = value; }
        }
        // [EditorProperty("进入时重置玩家相机旋转", EditorPropertyType.EEPT_Bool)]
        // public bool ResetPlayerCam
        // {
        //     get { return mResetPlayerCam; }
        //     set { mResetPlayerCam = value; }
        // }
        [EditorProperty("进入时触发相机： ", EditorPropertyType.EEPT_Camera)]
        public int EnterCam
        {
            get { return mEnterCam; }
            set { mEnterCam = value; }
        }

        [EditorProperty("将相机绑定点来源设为自身： ", EditorPropertyType.EEPT_Bool, LabelWidth = 160)]
        public bool ChangeCamPoint
        {
            get { return mChangeCamPoint; }
            set { mChangeCamPoint = value; }
        }
        [EditorProperty("进入相机绑定点位： ", EditorPropertyType.EEPT_CharacteLimbType)]
        public int EnterPoint
        {
            get { return mEnterPoint; }
            set { mEnterPoint = value; }
        }

        //[EditorProperty("相机进入时间： ", EditorPropertyType.EEPT_Float)]
        public float EnterTime
        {
            get { return mEnterTime; }
            set { mEnterTime = value; }
        }

        [EditorProperty("切换LookAt： ", EditorPropertyType.EEPT_Bool)]
        public bool SwitchLookAt
        {
            get { return mSwitchLookAt; }
            set { mSwitchLookAt = value; }
        }
        // [EditorProperty("目标单位： ", EditorPropertyType.EEPT_Unit)]
        public GUnit GUnit
        {
            get { return mGUnit; }
            set { mGUnit = value; }
        }
        [EditorProperty("目标挂点： ", EditorPropertyType.EEPT_Enum)]
        public ECharacteLimbType TargetPoint
        {
            get { return mTargetPoint; }
            set { mTargetPoint = value; }
        }

        [EditorProperty("退出时回到默认相机： ", EditorPropertyType.EEPT_Bool, LabelWidth = 120)]
        public bool ExitCam
        {
            get { return mExitCam; }
            set { mExitCam = value; }
        }
        // [EditorProperty("退出时触发相机： ", EditorPropertyType.EEPT_Camera)]
        // public int ExitCam
        // {
        //     get { return mExitCam; }
        //     set { mExitCam = value; }
        // }
        // [EditorProperty("退出相机绑定点位： ", EditorPropertyType.EEPT_CharacteLimbType)]
        // public int ExitCamPoint
        // {
        //     get { return mExitCamPoint; }
        //     set { mExitCamPoint = value; }
        // }
        // [EditorProperty("状态标签: ", EditorPropertyType.EEPT_ActionLable)]
        // public int ActionLable
        // {
        //     get { return mActionLable; }
        //     set { mActionLable = value; }
        // }
        // [EditorProperty("检查标签： ", EditorPropertyType.EEPT_Bool)]
        // public bool CheckLable
        // {
        //     get { return mCheckLable; }
        //     set { mCheckLable = value; }
        // }
        //
        // [EditorProperty("包含： ", EditorPropertyType.EEPT_Bool)]
        // public bool IsContain
        // {
        //     get { return mIsContain; }
        //     set { mIsContain = value; }
        // }
        //
        // [EditorProperty("为锁定相机： ", EditorPropertyType.EEPT_Bool)]
        // public bool IsLockCam
        // {
        //     get { return mIsLockCam; }
        //     set { mIsLockCam = value; }
        // }
        #endregion
        [NonSerialized] private ActionStateMachine _stateMachine;
        [NonSerialized] private ActionStatePart _mainActionState;

        [NonSerialized] private bool isCallBack = false;
        [NonSerialized] private bool isEnterCam = false;
        [NonSerialized] private bool isValid = true;

        public int GetEvenType() => (int)EEvenType.EET_CameraChange;
        public IActionEventData Creact() => new Event_CameraChange();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            _stateMachine = _actionState.ActionStateMachine;
            isCallBack = false;
            isEnterCam = false;
            _mainActionState = _actionState;

            isValid = !(mOnlyPlayer && !ActionEngineManager_Input.Instance.IsPlayer(_stateMachine.CurUnit.GetSource));
            //if (_actionState.IsTem) EngineDebug.LogError($"屏幕震动事件 [{isValid}]");
            if (!isValid) return;
            if (mSwitchLookAt && !mGUnit.IsValid(_actionState)) return;

            CameraControl _control = ActionEngineManager_Input.Instance.CurCamera;

            if (_control is not null)
            {
                if (mOnHitter)
                {
                    if (_stateMachine.TryGetStaticLogic(out Ex_AttackBox _attackBox, nameof(Ex_AttackBox)))
                    {
                        isCallBack = true;
                        _stateMachine.EventSystem.OnHit += EnterCamCallback;
                    }
                }
                else
                {
                    RatioState = gValue_Ratio.CheckValue(_stateMachine);
                    // EngineDebug.Log("RatioState: " + RatioState);
                    if (RatioState) EnterCamInit(_stateMachine);
                    lastRatioState = RatioState;
                }
            }
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (!isValid) return;
            if (mOnHitter) return;

            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;

            if (mSwitchLookAt && !mGUnit.IsValid(_actionState)) return;

            RatioState = gValue_Ratio.CheckValue(_stateMachine);

            if (RatioState != lastRatioState)
            {//锁定状态变化时切换相机
                if (RatioState) EnterCamInit(_stateMachine);

                lastRatioState = RatioState;
            }
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            if (!isValid) return;

            if (isCallBack)
            {
                if (_stateMachine.TryGetStaticLogic(out Ex_AttackBox _attackBox, nameof(Ex_AttackBox)))
                {
                    _stateMachine.EventSystem.OnHit -= EnterCamCallback;
                }
            }

            //if (mOnHitter)
            //{
            //    if (_actionState.ActionStateMachine.TryGetStaticLogic(out Ex_CamChange _camChange, nameof(Ex_CamChange)))
            //    {
            //        _camChange.OnReset(_actionState);
            //    }
            //}
            //else
            {
                //Debug.LogWarning($"尝试退出相机: isEnterCam[{isEnterCam}]  ExitCam[{ExitCam}]");
                if (isEnterCam && ExitCam)
                {
                    //Debug.LogWarning($"尝试退出相机: [{_actionState.CurrentActionState.Name}]  [{_actionState.ActionStateMachine.CurUnit.gameObject}]");
                    //ActionStateMachine _nstateMachine = _mainActionState.IsTem ? _mainActionState.ActionStateMachine.CurUnit.GetMaster.ActionStateMachine : _stateMachine;

                    ActionStateMachine _pointSour = ActionEngineManager_Input.Instance.Player.ActionStateMachine;
                    //if (mChangeCamPoint) _pointSour = _nstateMachine;
                    //else _pointSour
                    if (_pointSour.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig)))
                    {
                        if (_config.HelpPointDic.TryGetValue(ECharacteLimbType.Cam_Main, out Transform _camePoint))
                        {
                            CameraControl _control = ActionEngineManager_Input.Instance.CurCamera;
                            _control.ChangeCam(0, _camePoint);
                            //Debug.LogWarning($"退出相机: [{_actionState.CurrentActionState.Name}]  [{_actionState.ActionStateMachine.CurUnit.gameObject}]");
                        }
                    }
                }
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_CameraChange _event = _eventData as Event_CameraChange;

            _event.EnterCam = mEnterCam;
            _event.EnterPoint = mEnterPoint;
            _event.EnterTime = mEnterTime;
            _event.Ratio = gValue_Ratio.Clone();
            _event.SwitchLookAt = mSwitchLookAt;
            _event.GUnit = (GUnit)mGUnit.Clone();
            _event.TargetPoint = mTargetPoint;
            _event.OnHitter = mOnHitter;
            _event.ExitCam = mExitCam;
            _event.OnlyPlayer = mOnlyPlayer;
            _event.ChangeCamPoint = mChangeCamPoint;
            return _event;
        }

        private void EnterCamCallback(ActionEngine_Unit _unit, bool _isUnit, GameObject _gameObject, TargetUnit _onHiter)
        {
            EnterCamInit(_unit.ActionStateMachine, true, _isUnit, _gameObject, _isUnit ? _onHiter.GetUnit() : null);

            //if (_onHiter == null)
            //{
            //    EngineDebug.LogError("命中对象是空的");
            //}
            //else
            //{
            //}
        }

        private void EnterCamInit(ActionStateMachine _actionState, bool isAttack = false, bool _isUnit = false, GameObject _gameObject = null, ActionEngine_Unit _onHiter = null)
        {
            // if(mIsLockCam != _stateMachine.IsLock)return;
            ActionStateMachine _nstateMachine = _mainActionState.IsTem ? _mainActionState.ActionStateMachine.CurUnit.GetSource.ActionStateMachine : _stateMachine;

            //if (!ActionEngineManager_Input.Instance.IsPlayer(_nstateMachine.CurUnit))
            //{
            //    return;
            //}

            if (!mOnHitter)
            {
                if (_actionState.TryGetStaticLogic(out Ex_CamChange _camChange, nameof(Ex_CamChange)))
                {
                    _camChange.UpdateCameEvent(this);
                }
            }

            ActionStateMachine _pointSour;
            if (mChangeCamPoint)
                _pointSour = _nstateMachine;
            else
                _pointSour = ActionEngineManager_Input.Instance.Player.ActionStateMachine;

            if (_pointSour.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig)))
            {
                if (_config.HelpPointDic.TryGetValue((ECharacteLimbType)mEnterPoint, out Transform _camePoint))
                {
                    isEnterCam = true;
                    //Debug.Log($"进入相机:[{_actionState.CurUnit.gameObject}]");

                    CameraControl _control = ActionEngineManager_Input.Instance.CurCamera;
                    if (isAttack)
                    {
                        // checkTarget = _stateMachine.HitUnit;
                        // Debug.Log("HitUnit: " + _stateMachine.HitUnit.gameObject.name);
                        // _stateMachine.SetMouseXY(Quaternion.LookRotation(_stateMachine.CurUnit.transform.forward));
                        if (_isUnit && mSwitchLookAt)
                        {
                            if (_onHiter.ActionStateMachine.TryGetComponent(out CharacterConfig _config2, nameof(CharacterConfig)))
                            {
                                if (_config2.HelpPointDic.TryGetValue(TargetPoint, out Transform _camePoint2))
                                {
                                    // _stateMachine.SetMouseXY(Quaternion.LookRotation(_stateMachine.CurUnit.transform.forward));
                                    _control.ChangeCam(mEnterCam, _camePoint, _camePoint2);
                                    return;
                                }
                            }
                            _control.ChangeCam(mEnterCam, _camePoint, _onHiter.transform);
                        }
                        else
                        {
                            if (mSwitchLookAt)
                                _control.ChangeCam(mEnterCam, _camePoint, _gameObject.transform);
                            else
                                _control.ChangeCam(mEnterCam, _camePoint, _camePoint);
                        }
                        return;
                    }

                    if (mSwitchLookAt)
                    {
                        ActionEngine_Unit checkTarget = mGUnit.GetValue(_actionState.AllActionStatePart[0]).GetUnit();
                        if (checkTarget.ActionStateMachine.TryGetComponent(out CharacterConfig _config2, nameof(CharacterConfig)))
                        {
                            if (_config2.HelpPointDic.TryGetValue(TargetPoint, out Transform _camePoint2))
                            {
                                // _stateMachine.SetMouseXY(Quaternion.LookRotation(_stateMachine.CurUnit.transform.forward));
                                _control.ChangeCam(mEnterCam, _camePoint, _camePoint2);
                                return;
                            }
                        }
                        // _stateMachine.SetMouseXY(Quaternion.LookRotation(_stateMachine.CurUnit.transform.forward));
                        _control.ChangeCam(mEnterCam, _camePoint, checkTarget.transform);
                    }
                    else
                    {
                        _control.ChangeCam(mEnterCam, _camePoint);
                    }
                }
            }
        }
    }
}