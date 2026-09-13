using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using System;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_UnitRot : IActionEventData
    {
        [SerializeField] protected bool mIsMove = false;
        [SerializeField] protected bool mIsMovePre = false;

        [SerializeField] protected GraphEvent_NoValue_Vector3 mUnitRot = new GraphEvent_NoValue_Vector3();
        [SerializeField] protected bool m_IsEuler = false;
        [SerializeField] protected bool m_InitTargetRot;
        // [SerializeField] protected ERotType mRotType;
        // [SerializeField] protected GEnum mRotaType = new GEnum();
        [SerializeField] protected int mRotLerp = 0;
        // [SerializeField] protected int mOffsetRotY = 0;
        // [SerializeField] protected int mRotPriority = 0;
        [SerializeField] protected GInt mRotPriority = new GInt(1);
        [SerializeField] protected float mTotalTimeD = 0.0f;

        #region Property
        [EditorProperty("仅移动输入时转向: ", EditorPropertyType.EEPT_Bool)]
        public bool IsMove
        {
            get { return mIsMove; }
            set { mIsMove = value; }
        }
        [EditorProperty("    预输入: ", EditorPropertyType.EEPT_Bool)]
        public bool IsMovePre
        {
            get { return mIsMovePre; }
            set { mIsMovePre = value; }
        }
        [EditorProperty("最终朝向 ", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Vector3 UnitRot
        {
            get { return mUnitRot; }
            set { mUnitRot = value; }
        }
        [EditorProperty("仅进入时更新一次 [最终朝向] ", EditorPropertyType.EEPT_Bool, LabelWidth = 170)]
        public bool InitTargetRot
        {
            get { return m_InitTargetRot; }
            set { m_InitTargetRot = value; }
        }
        [EditorProperty("输入是否为Euler: ", EditorPropertyType.EEPT_Bool)]
        public bool IsEuler
        {
            get { return m_IsEuler; }
            set { m_IsEuler = value; }
        }
        // [EditorProperty("朝向类型: ", EditorPropertyType.EEPT_Enum, EnumNames = new []{"面向相机前方向","面向移动输入方向","面向锁定目标","面向攻击者", "面向玩家"})]
        // public GEnum RotaType
        // {
        //     get { return mRotaType; }
        //     set { mRotaType = value; }
        // }
        [EditorProperty("转向速度: ", EditorPropertyType.EEPT_Int, LabelWidth = 150)]//(负为线性：ms)
        public int RotLerp
        {
            get { return mRotLerp; }
            set { mRotLerp = value; }
        }
        // [EditorProperty("Y轴角度偏移(度): ", EditorPropertyType.EEPT_Int)]
        // public int OffsetRotY
        // {
        //     get { return mOffsetRotY; }
        //     set { mOffsetRotY = value; }
        // }
        [EditorProperty("旋转优先等级", EditorPropertyType.EEPT_GInt)]
        public GInt RotPriority
        {
            get { return mRotPriority; }
            set { mRotPriority = value; }
        }
        [EditorProperty("旋转持续时长", EditorPropertyType.EEPT_Float)]
        public float TotalTime
        {
            get { return mTotalTimeD; }
            set { mTotalTimeD = value; }
        }
        #endregion

        public int GetEvenType() => (int)EEvenType.EET_UnitRot;
        public IActionEventData Creact() => new Event_UnitRot();

        [NonSerialized] private Vector3 _unitRot;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            //if (!_actionState.IsTem)
            //    Debug.LogWarning($"[{_actionState.CurrentActionState.Name}] <color=#ffcc00>进入旋转事件</color>: " + this.GetHashCode() + $"  [{Time.realtimeSinceStartup}]");
            _unitRot = mUnitRot.value(_actionState, new ActionMachineTime(0, 0, 0, 0));
            if (_isSingle)
            {
                ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
                if (_stateMachine == null) return;
                // RemoteProxy 纯表现，朝向由网络下行接管
                if (_stateMachine.Authority == SimulationAuthority.RemoteProxy) return;
                // ServerAuthoritative 需显式开启：未开启时位姿由外部上报驱动，跑旋转轨会与之形成两个朝向源
                if (_stateMachine.Authority == SimulationAuthority.ServerAuthoritative
                    && !_stateMachine.ServerLogicSimulationEnabled) return;

                if (mIsMove && !(mIsMovePre ? _actionState.IsMoveInputPre : _stateMachine.IsMoveInput)) return;//不执行
                EX_Update_UnitRot _exUnitRot = null;
                if (!_actionState.IsTem) _stateMachine.TryGetLogic(out _exUnitRot, nameof(EX_Update_UnitRot));

                Quaternion _quaternion = Quaternion.identity;
                if (m_IsEuler)
                {
                    _quaternion = Quaternion.Euler(_unitRot);
                }
                else
                {
                    _quaternion = Quaternion.LookRotation(_unitRot);
                }
                if (TotalTime <= 0)
                {
                    // Transform _transform = _stateMachine.CurUnit.RootTarget;
                    // Quaternion _quaternion = _exUnitRot.GetTargetRot(_stateMachine, _transform, (ERotType)mRotaType.value);

                    // _transform.rotation = _quaternion;
                    //_exUnitRot.ResetLifeTime();

                    if (_actionState.IsTem)
                    {
                        _actionState.SetRot(_quaternion);
                    }
                    else
                    {
                        _exUnitRot.ResetLifeTime();

                        if (_stateMachine.TryGetLogic(out Ex_Update_CharacterControl _characterControl, nameof(Ex_Update_CharacterControl)))
                        {
                            _characterControl.SetRot(_quaternion, mRotPriority.GetValue(_actionState));
                        }
                    }
                }
                else if (!_actionState.IsTem)
                {
                    _exUnitRot.SetRot(_quaternion, mRotLerp, mRotPriority.GetValue(_actionState), TotalTime);
                }
            }
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {

            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            if (_stateMachine == null) return;
            if (_stateMachine.Authority == SimulationAuthority.RemoteProxy) return;
            if (_stateMachine.Authority == SimulationAuthority.ServerAuthoritative
                && !_stateMachine.ServerLogicSimulationEnabled) return;
            if (mIsMove && !(mIsMovePre ? _actionState.IsMoveInputPre : _stateMachine.IsMoveInput)) return;//不执行

            //_stateMachine.TryGetLogic(out EX_Update_UnitRot _exUnitRot, nameof(EX_Update_UnitRot));
            //_exUnitRot.ResetLifeTime();

            Quaternion _targetRot = Quaternion.identity;
            if (!InitTargetRot) _unitRot = mUnitRot.value(_actionState, _actionTime);
            if (m_IsEuler)
            {
                _targetRot = Quaternion.Euler(_unitRot);
            }
            else
            {
                Vector3 lockDir = _unitRot;
                if (lockDir != Vector3.zero)
                {
                    _targetRot = Quaternion.LookRotation(lockDir);
                }
                //EngineDebug.LogWarning($"旋转: [{_actionState.GetHashCode()}]" + _targetRot.eulerAngles.y);
            }
            //EngineDebug.LogWarning($"旋转: [{_actionState.GetHashCode()}]" + _targetRot.eulerAngles.y);

            if (_actionState.IsTem)
            {
                if (mRotLerp == 0)
                {
                    _actionState.SetRot(_targetRot);
                }
                else if (mRotLerp > 0)
                {
                    _actionState.SetRot(Quaternion.Lerp(_actionState.Rot, _targetRot, mRotLerp * _actionTime.Deltatime));
                }
                else
                {
                    float _rotSpeed = -mRotLerp;
                    _actionState.SetRot(Quaternion.RotateTowards(_actionState.Rot, _targetRot,
                        _rotSpeed * _actionTime.Deltatime));
                }
            }
            else
            {
                if (_stateMachine.TryGetLogic(out Ex_Update_CharacterControl _characterControl, nameof(Ex_Update_CharacterControl)))
                {
                    // 检查 CurUnit 和 RootTarget 是否有效
                    var curUnit = _stateMachine.CurUnit;
                    if (curUnit == null || curUnit.RootTarget == null) return;

                    //Debug.LogWarning($"更新旋转事件[{_targetRot.eulerAngles.y}]  [{_actionState.CurrentActionState.Name}]: " + this.GetHashCode() + $"  [{Time.realtimeSinceStartup}]");
                    //Vector3 _centerPos = _actionState.ActionStateMachine.CurUnit.transform.TransformPoint(0, 1, 0);
                    //Debug.DrawRay(_centerPos, _targetRot * Vector3.forward * 2, Color.bisque, 3);
                    if (mRotLerp == 0)
                    {
                        _characterControl.SetRot(_targetRot, mRotPriority.GetValue(_actionState));
                    }
                    else if (mRotLerp > 0)
                    {
                        _characterControl.SetRot(
                            Quaternion.Lerp(curUnit.RootTarget.rotation, _targetRot, mRotLerp * _actionTime.Deltatime),
                            mRotPriority.GetValue(_actionState));

                    }
                    else
                    {
                        float _rotSpeed = -mRotLerp;
                        _characterControl.SetRot(Quaternion.RotateTowards(curUnit.RootTarget.rotation, _targetRot,
                            _rotSpeed * _actionTime.Deltatime), mRotPriority.GetValue(_actionState));
                    }
                }
            }
        }

        private void RotTarget(ActionStateMachine _machine, EX_Update_UnitRot _exUnitRot, Ex_Update_CharacterControl _characterControl, Quaternion _targetRot)
        {
            if (TotalTime <= 0)
            {

            }
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            if (_actionState.IsTem) return;
            //if (!_actionState.IsTem)
            //    Debug.LogWarning("<color=#ff0000>离开旋转事件:</color> " + this.GetHashCode() + $"  [{Time.realtimeSinceStartup}]");
            if (TotalTime > 0)
            {
                Quaternion _quaternion = Quaternion.identity;
                if (m_IsEuler)
                {
                    _quaternion = Quaternion.Euler(mUnitRot.value(_actionState, new ActionMachineTime(0, 0, 0, 0)));
                }
                else
                {
                    _quaternion = Quaternion.LookRotation(mUnitRot.value(_actionState, new ActionMachineTime(0, 0, 0, 0)));
                }
                ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
                if (_stateMachine == null) return;
                _stateMachine.TryGetLogic(out EX_Update_UnitRot _exUnitRot, nameof(EX_Update_UnitRot));
                if (_exUnitRot == null) return;
                _exUnitRot.SetRot(_quaternion, mRotLerp, mRotPriority.GetValue(_actionState), TotalTime);
                //_exUnitRot.ResetLifeTime();
            }
        }
        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_UnitRot _event = _eventData as Event_UnitRot;

            _event.IsMove = mIsMove;
            _event.IsMovePre = mIsMovePre;
            _event.RotLerp = mRotLerp;
            _event.UnitRot = mUnitRot.Clone();
            _event.IsEuler = m_IsEuler;
            _event.InitTargetRot = m_InitTargetRot;
            // _event.OffsetRotY = mOffsetRotY;
            _event.TotalTime = mTotalTimeD;
            // _event.RotaType = (GEnum)mRotaType.Clone();
            _event.RotPriority = (GInt)mRotPriority.Clone();


            return _event;
        }
    }
}