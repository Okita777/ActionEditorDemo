using System;
using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_PlayParticle : IActionEventData
    {
        [SerializeField] protected string m_PartoclePath;
        [SerializeField] protected int m_PartPointType;
        [SerializeField] protected float m_Life = 2;
        [SerializeField] protected float m_Interval = 0;
        [SerializeField] protected bool m_IsUseBluePrintPoint = false;
        [SerializeField] protected GraphEvent_NoValue_Point m_ParticlePoint = new GraphEvent_NoValue_Point();
        //[SerializeField] protected bool m_ExitDestory = false;
        [SerializeField] protected EVector3 m_OffsetPos = new EVector3();
        [SerializeField] protected EVector3 m_OffsetRot = new EVector3();
        [SerializeField] protected EVector3 m_LocalScale = new EVector3(1, 1, 1);
        [SerializeField] protected GraphEvent_NoValue_Float m_CLocalScale = new GraphEvent_NoValue_Float(1);
        [SerializeField] protected bool m_AlwaysFollow = false;
        [SerializeField] protected bool m_UseBluePrint_Scale = false;
        #region Property

        [EditorProperty("粒子特效", EditorPropertyType.EEPT_GameObject, Required = true)]
        public string PartoclePath
        {
            get { return m_PartoclePath; }
            set { m_PartoclePath = value; }
        }

        [EditorProperty("始终跟随", EditorPropertyType.EEPT_Bool)]
        public bool AlwaysFollow
        {
            get { return m_AlwaysFollow; }
            set { m_AlwaysFollow = value; }
        }
        [EditorProperty("粒子寿命(s)", EditorPropertyType.EEPT_Float)]
        public float Life
        {
            get { return m_Life; }
            set { m_Life = value; }
        }
        [EditorProperty("粒子发射间隔(s)(0为仅发射一次)", EditorPropertyType.EEPT_Float, LabelWidth = 180)]
        public float Interval
        {
            get { return m_Interval; }
            set { m_Interval = value; }
        }
        [EditorProperty("使用蓝图定义特效位置", EditorPropertyType.EEPT_Bool)]
        public bool IsUseBluePrintPoint
        {
            get { return m_IsUseBluePrintPoint; }
            set { m_IsUseBluePrintPoint = value; }
        }
        [EditorProperty("粒子位置设定", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Point ParticlePoint
        {
            get
            {
                if (m_ParticlePoint is null) m_ParticlePoint = new GraphEvent_NoValue_Point();
                return m_ParticlePoint;
            }
            set { m_ParticlePoint = value; }
        }
        [EditorProperty("目标挂点", EditorPropertyType.EEPT_CharacteLimbType)]
        public int PartPointType
        {
            get { return m_PartPointType; }
            set { m_PartPointType = value; }
        }
        [EditorProperty("位置偏移", EditorPropertyType.EEPT_Vector3)]
        public EVector3 OffsetPos
        {
            get { return m_OffsetPos; }
            set { m_OffsetPos = value; }
        }

        [EditorProperty("角度偏移", EditorPropertyType.EEPT_Vector3)]
        public EVector3 OffsetRot
        {
            get { return m_OffsetRot; }
            set { m_OffsetRot = value; }
        }
        [EditorProperty("使用蓝图缩放", EditorPropertyType.EEPT_Bool)]
        public bool UseBluePrint_Scale
        {
            get { return m_UseBluePrint_Scale; }
            set { m_UseBluePrint_Scale = value; }
        }
        [EditorProperty("缩放", EditorPropertyType.EEPT_Vector3)]
        public EVector3 LocalScale
        {
            get { return m_LocalScale; }
            set { m_LocalScale = value; }
        }
        [EditorProperty("缩放(全比例)", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Float CLocalScale
        {
            get
            {
#if UNITY_EDITOR
                if (m_CLocalScale is null) m_CLocalScale = new GraphEvent_NoValue_Float(1.0f);
#endif
                return m_CLocalScale;
            }
            set { m_CLocalScale = value; }
        }
        #endregion
        public int GetEvenType() => (int)EEvenType.EET_Partocle;
        public IActionEventData Creact() => new Event_PlayParticle();


        [NonSerialized] bool isOnce = false;
        [NonSerialized] bool isValid = false;
        [NonSerialized] float nowInterval = 0.0f;
        [NonSerialized] ActionEngine_Effects _effects;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            //Debug.LogWarning("生成特效");
            isValid = false;
            nowInterval = 100;
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;

            //EngineDebug.Log($"Action[{_actionState.CurrentActionState.Name}]  读取的特效Key值[<color=#ffcc00>{m_PartoclePath}</color>]");
            //EngineDebug.LogWarning($"Action[{_actionState.CurrentActionState.Name}]  EventHash值[<color=#ffcc00>{GetHashCode()}</color>]");
            isOnce = Mathf.Approximately(Interval, 0);
            EngineResourcesManager.Instance.CreactObjToComponent<ActionEngine_Effects>(m_PartoclePath,
                (Component _obj) =>
                {
                    if (_obj is ActionEngine_Effects _particle)
                    {
                        isValid = true;
                        _effects = _particle;
                        //EngineResourcesManager.Instance.ResetLife(_particle, _particle.Life);

                        if (_actionState.IsTem)
                        {
                            int _instanceId = _actionState.GetHashCode();
                            int _eventID = GetHashCode();
                            _stateMachine.InstanceComponent_Add(_instanceId, _eventID, _particle);

                            Vector3 _pos = _actionState.Pos + _actionState.Rot * m_OffsetPos.GetValue();
                            Quaternion _rot = _actionState.Rot * Quaternion.Euler(OffsetRot.GetValue());
                            _particle.transform.SetPositionAndRotation(_pos, _rot);
                            //_effects.transform.localScale = m_LocalScale.GetValue();
                            SetScale(_actionState, EngineResourcesManager.Instance.MachineTime);
                        }
                        else
                        {
                            SetPosAndRot(_actionState, EngineResourcesManager.Instance.MachineTime);
                            //_effects.transform.localScale = m_LocalScale.GetValue();
                        }

                        //if (UseBluePrint_Scale)
                        //{
                        //    float scaleValue = CLocalScale.value(_actionState, EngineResourcesManager.Instance.MachineTime);
                        //    _effects.transform.localScale = Vector3.one * scaleValue;
                        //}
                        //else
                        //{
                        //    _effects.transform.localScale = m_LocalScale.GetValue();
                        //}

                        if (_isSingle || isOnce)
                            _particle.Play();
                    }
#if UNITY_EDITOR
                    else
                    {
                        if (string.IsNullOrEmpty(m_PartoclePath))
                        {
                            EngineDebug.LogError($"Action[<color=#ff0000>{_actionState.CurrentActionState.Name}</color>] 特效加载失败!!  特效路径读取为空");
                        }
                        else
                        {
                            EngineDebug.Log(_obj is null
                                ? $"特效加载失败 [<color=#ff0000>{m_PartoclePath}</color>]"
                                : $"申请特效类型: 【{_obj.GetType()}】");
                        }

                    }
#endif

                }, 3, 100, m_Life, (int)EObjPoolParent.Effects
            );
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (isValid && !_actionState.IsTem)
            {
                //EngineDebug.LogWarning($"Action[{_actionState.CurrentActionState.Name}]  EventHash值[<color=#ffcc00>{GetHashCode()}</color>]");

                //EngineResourcesManager.Instance.ResetLife(_effects, _effects.Life);
                if (m_Life > 0) EngineResourcesManager.Instance.ResetLife(_effects, m_Life);

                if (m_AlwaysFollow)
                {
                    ActionStateMachine _stateMachine = _actionState.ActionStateMachine;

                    SetPosAndRot(_actionState, _actionTime);
                }

                if (!isOnce)
                {
                    nowInterval += _actionTime.Deltatime;
                    if (nowInterval >= Interval)
                    {
                        _effects.Play();
                        nowInterval = 0;
                    }
                }
            }
        }

        public void LateUpdate(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (_actionState.IsTem)
            {
                if (!isValid) return;

                if (_actionState.ActionStateMachine.InstanceComponent_TryGet(_actionState.GetHashCode(), GetHashCode(), out Component _obj))
                {
                    EngineResourcesManager.Instance.ResetLife(_obj, m_Life);
                    Transform mMain = _obj.transform;
                    mMain.SetPositionAndRotation(_actionState.Pos, _actionState.Rot);
                }
                SetScale(_actionState, EngineResourcesManager.Instance.MachineTime);

                if (!isOnce)
                {
                    nowInterval += _actionTime.Deltatime;
                    if (nowInterval >= Interval)
                    {
                        _effects.Play();
                        nowInterval = 0;
                    }
                }
            }
        }
        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            //Debug.LogWarning($"销毁特效 [{_actionState.CurrentActionState.Name}]");
            if (m_Life <= 0)
            {
                if (_actionState.IsTem)
                {
                    int _instanceId = _actionState.GetHashCode();
                    int _eventID = GetHashCode();
                    if (_actionState.ActionStateMachine.InstanceComponent_TryGet(_actionState.GetHashCode(), GetHashCode(), out Component _obj))
                    {
                        {
                            EngineResourcesManager.Instance.RemoveComponent(m_PartoclePath, _obj);
                        }
                    }
                    _actionState.ActionStateMachine.InstanceComponent_Remove(_instanceId, _eventID);
                }
                else
                {
                    EngineResourcesManager.Instance.RemoveComponent(m_PartoclePath, _effects);
                }
            }
            else
            {
                _effects?.Stop();
            }
        }
        private void SetScale(ActionStatePart _actionState, ActionMachineTime _time)
        {
            if (UseBluePrint_Scale)
            {
                float scaleValue = CLocalScale.value(_actionState, EngineResourcesManager.Instance.MachineTime);
                _effects.transform.localScale = Vector3.one * scaleValue;
            }
            else
            {
                _effects.transform.localScale = m_LocalScale.GetValue();
            }
        }

        private void SetPosAndRot(ActionStatePart _actionState, ActionMachineTime _time)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            if (IsUseBluePrintPoint)
            {
                PointData _pointData = ParticlePoint.value(_actionState, _time);
                _effects.transform.SetPositionAndRotation(_pointData.pos, _pointData.rot);
            }
            else
            {
                bool _isFindPoint = false;
                if (_stateMachine.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig)))
                {
                    if (_config.HelpPointDic.TryGetValue((ECharacteLimbType)m_PartPointType,
                            out Transform _point))
                    {
                        _isFindPoint = true;
                        Vector3 _pos = _point.TransformPoint(m_OffsetPos.GetValue());
                        Quaternion _rot = _point.rotation * Quaternion.Euler(OffsetRot.GetValue());
                        _effects.transform.SetPositionAndRotation(_pos, _rot);
                    }
                }
                if (!_isFindPoint)
                {
                    Vector3 _pos = _stateMachine.CurUnit.transform.TransformPoint(m_OffsetPos.GetValue());
                    Quaternion _rot = _stateMachine.CurUnit.transform.rotation *
                                      Quaternion.Euler(OffsetRot.GetValue());
                    _effects.transform.SetPositionAndRotation(_pos, _rot);
                }
            }
            SetScale(_actionState, _time);
        }
        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_PlayParticle _event = _eventData as Event_PlayParticle;

            _event.PartoclePath = m_PartoclePath;
            _event.PartPointType = m_PartPointType;
            _event.OffsetPos = m_OffsetPos;
            _event.OffsetRot = m_OffsetRot;
            _event.LocalScale = m_LocalScale;
            _event.AlwaysFollow = m_AlwaysFollow;
            _event.Life = m_Life;
            _event.Interval = m_Interval;
            _event.IsUseBluePrintPoint = IsUseBluePrintPoint;
            _event.ParticlePoint = ParticlePoint.Clone();
            _event.CLocalScale = CLocalScale.Clone();
            _event.UseBluePrint_Scale = m_UseBluePrint_Scale;
            return _event;
        }
    }
}