using System;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_SoftLock : IActionEventData
    {
        [SerializeField] protected float m_Radius = 6;
        [SerializeField] protected float m_LockRadius = 2.0f;
        [SerializeField] protected float m_SelfAngle = 120;
        [SerializeField] protected float m_LerpSpeed = 12;
        [SerializeField] protected int m_CheckLayer;
        [SerializeField] protected int m_Priority = 1;

        [SerializeField] protected bool m_ReferToCam = false;
        [SerializeField] protected GValue_SetUnit m_UnitSet = new GValue_SetUnit();

        #region Property
        [EditorProperty("锁定层级: ", EditorPropertyType.EEPT_LayerMask)]
        public int CheckLayer
        {
            get { return m_CheckLayer; }
            set { m_CheckLayer = value; }
        }
        [EditorProperty("锁定优先参考相机朝向(仅玩家): ", EditorPropertyType.EEPT_Bool, LabelWidth = 180)]
        public bool ReferToCam
        {
            get { return m_ReferToCam; }
            set { m_ReferToCam = value; }
        }
        [EditorProperty("最大半径: ", EditorPropertyType.EEPT_Float)]
        public float Radius
        {
            get { return m_Radius; }
            set { m_Radius = value; }
        }
        [EditorProperty("硬锁半径: ", EditorPropertyType.EEPT_Float)]
        public float LockRadius
        {
            get { return m_LockRadius; }
            set { m_LockRadius = value; }
        }
        [EditorProperty("最大角度差: ", EditorPropertyType.EEPT_Float)]
        public float SelfAngle
        {
            get { return m_SelfAngle; }
            set { m_SelfAngle = value; }
        }

        [EditorProperty("将锁定对象写入GValue: ", EditorPropertyType.EEPT_SetGUnit, LabelWidth = 150)]
        public GValue_SetUnit UnitSet
        {
            get { return m_UnitSet; }
            set { m_UnitSet = value; }
        }
        [EditorProperty("旋转速度: ", EditorPropertyType.EEPT_Float)]
        public float LerpSpeed
        {
            get { return m_LerpSpeed; }
            set { m_LerpSpeed = value; }
        }
        [EditorProperty("旋转优先级: ", EditorPropertyType.EEPT_Int)]
        public int Priority
        {
            get { return m_Priority; }
            set { m_Priority = value; }
        }
        #endregion
        public int GetEvenType() => (int)EEvenType.EET_SoftLock;
        public IActionEventData Creact() => new Event_SoftLock();

        [NonSerialized] private Transform m_LockTarget = null;
        [NonSerialized] private bool m_OnLock = false;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            m_LockTarget = null;
            // EngineDebug.Log("软锁定");
            m_OnLock = false;
            ActionStateMachine _actionStateMachine = _actionState.ActionStateMachine;
            if (m_UnitSet.m_IsSet) m_UnitSet.Set(_actionState);

            Transform _center = _actionStateMachine.CurUnit.transform;
            Collider[] _colliders = Physics.OverlapSphere(_center.position, m_Radius,
                _actionStateMachine.GetLayer(m_CheckLayer), QueryTriggerInteraction.Ignore);

            Vector3 FindDir = _actionStateMachine.CurUnit.transform.forward;

            if (_actionStateMachine.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig)))
            {
                if (!_config.HelpPointDic.TryGetValue(ECharacteLimbType.Cam_Main, out _center))
                {
                    FindDir = _center.forward;
                    EngineDebug.LogWarning("软锁定运行错误：角色未配置相机挂点");
                }
            }
            else
            {
                EngineDebug.LogWarning("软锁定运行错误：角色未配置相机挂点");
            }


            Transform _findTarget = null;
            float _findMinAngle = 360; //最小角度
            // bool _isFindLockToCam = (!_actionState.ActionStateMachine.IsMoveInput) &&
            //                         ActionEngineManager_Input.Instance.IsPlayer(_actionStateMachine.CurUnit);

            if (ActionEngineManager_Input.Instance.IsPlayer(_actionStateMachine.CurUnit))
            {
                //玩家软锁规则
                if (_actionState.ActionStateMachine.IsMoveInput)
                {
                    //按玩家输入方向锁定
                    Vector3 inputDir = _actionStateMachine.PlayerInputMoveDir_Cam;
                    _findTarget = FindLockTarget_MinAngle(_center, inputDir, _colliders, out _findMinAngle);
                }
                else
                {
                    if (m_ReferToCam)
                    {
                        //按镜头方向锁定
                        Vector3 camDir = ActionEngineManager_Input.Instance.PlayerCam.transform.forward;
                        _findTarget = FindLockTarget_MinAngle(_center, camDir, _colliders, out _findMinAngle);

                        if (!(_findMinAngle < m_SelfAngle * 0.5f))
                        {
                            //按角色前方锁定
                            _findTarget = FindLockTarget_MinAngle(_center, FindDir, _colliders, out _findMinAngle);
                        }
                    }
                    else
                    {
                        //按角色前方锁定
                        _findTarget = FindLockTarget_MinAngle(_center, FindDir, _colliders, out _findMinAngle);
                        if (!(_findMinAngle < m_SelfAngle * 0.5f))
                        {
                            //按镜头方向锁定
                            Vector3 camDir = ActionEngineManager_Input.Instance.PlayerCam.transform.forward;
                            _findTarget = FindLockTarget_MinAngle(_center, camDir, _colliders, out _findMinAngle);
                        }
                    }
                }
            }
            else
            {
                //npc软锁规则

                //按角色前方锁定
                _findTarget = FindLockTarget_MinAngle(_center, FindDir, _colliders, out _findMinAngle);
            }


            if (_findMinAngle < m_SelfAngle * 0.5f)
            {
                //角锁定
                m_OnLock = true;
                m_LockTarget = _findTarget;
                if (m_UnitSet.m_IsSet)
                {
                    if (m_LockTarget is not null)
                    {
                        if (m_LockTarget.TryGetComponent(out ActionEngine_Unit _unit))
                        {
                            m_UnitSet.Set(_actionState, _unit);
                            return;
                        }
                    }
                }
                // EngineDebug.Log("找到了");
            }
            else
            {
                //角锁失败  尝试距离锁定

                _findTarget = FindLockTarget_MinDis(_center, _colliders, out float minDis);
                if (minDis < m_LockRadius * m_LockRadius)
                {
                    // EngineDebug.Log("成功软锁定");
                    m_OnLock = true;
                    m_LockTarget = _findTarget;
                    if (m_UnitSet.m_IsSet)
                    {
                        if (m_LockTarget is not null)
                        {
                            if (m_LockTarget.TryGetComponent(out ActionEngine_Unit _unit))
                            {
                                m_UnitSet.Set(_actionState, _unit);
                                return;
                            }
                        }
                    }
                }
                // EngineDebug.Log("距离锁定: " + minDis);

            }

            // m_UnitSet.Set(_actionState, _actionStateMachine.CurUnit);
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (!m_OnLock) return;
            // EngineDebug.Log("成功软锁定");
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            if (_stateMachine.TryGetLogic(out Ex_Update_CharacterControl _characterControl, nameof(Ex_Update_CharacterControl)))
            {
                Transform _transform = _stateMachine.CurUnit.transform;
                Vector3 _lockDir = m_LockTarget.position - _transform.position;
                _lockDir.y = 0;
                if (m_LerpSpeed > 0)
                {
                    Quaternion _lerpRot = Quaternion.Lerp(_stateMachine.CurUnit.transform.rotation,
                        Quaternion.LookRotation(_lockDir.normalized), _actionTime.Deltatime * m_LerpSpeed);
                    _characterControl.SetRot(_lerpRot, m_Priority);
                }
                else
                {
                    _characterControl.SetRot(Quaternion.LookRotation(_lockDir.normalized), m_Priority);
                }
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SoftLock _event = _eventData as Event_SoftLock;

            _event.Radius = m_Radius;
            _event.SelfAngle = m_SelfAngle;
            _event.LerpSpeed = m_LerpSpeed;
            _event.CheckLayer = m_CheckLayer;
            _event.LockRadius = m_LockRadius;

            _event.ReferToCam = m_ReferToCam;
            _event.Priority = m_Priority;
            _event.UnitSet = m_UnitSet.Clone();

            return _event;
        }

        private Transform FindLockTarget_MinAngle(Transform _pos, Vector3 _forward, Collider[] _colliders, out float minAngle)
        {
            minAngle = 360;
            Transform _target = null;
            _forward.y = 0;

            for (int i = 0; i < _colliders.Length; i++)
            {
                Collider _collider = _colliders[i];
                Transform _transform = _collider.transform;
                if (_pos != _transform)
                {
                    Vector3 _transDir = _transform.position - _pos.position;
                    _transDir.y = 0;
                    float _angleOffset = Vector3.Angle(_forward, _transDir.normalized);
                    if (_angleOffset < minAngle)
                    {
                        minAngle = _angleOffset;
                        _target = _transform;
                    }
                }
            }
            return _target;
        }

        private Transform FindLockTarget_MinDis(Transform _pos, Collider[] _colliders, out float minDis)
        {
            Transform _target = null;
            minDis = float.MaxValue;

            for (int i = 0; i < _colliders.Length; i++)
            {
                Collider _collider = _colliders[i];

                Transform _transform = _collider.transform;
                if (_pos != _transform)
                {
                    Vector3 _transDir = _transform.position - _pos.position;
                    _transDir.y = 0;
                    // EngineDebug.Log($"{_transform.name}: {_transDir.magnitude}");
                    // EngineDebug.DrawSphere(_transform.position,0.5f, Color.blue, 2);
                    // EngineDebug.DrawSphere(_pos.position,0.5f, Color.red, 2);
                    // Debug.DrawRay(_transform.position, Vector3.up,Color.blue, 10);
                    // Debug.DrawRay(_pos.position, Vector3.up,Color.red, 10);


                    float dis = _transDir.sqrMagnitude;
                    if (dis < minDis)
                    {
                        //找最小距离
                        minDis = dis;
                        _target = _transform;
                    }
                }
            }
            return _target;
        }
    }
}