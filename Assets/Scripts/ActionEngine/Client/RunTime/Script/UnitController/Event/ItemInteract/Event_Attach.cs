using System;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_Attach : IActionEventData
    {
        [SerializeField] private bool m_AlignToPoint = false;
        [SerializeField] private SelectTransform m_ReferTransform = new SelectTransform();
        [SerializeField] private SelectTransform m_TargetTransform = new SelectTransform();
        [SerializeField] private GPoint m_AlignPoint = new GPoint();
        [SerializeField] private bool m_AlignToTarget = true;
        [SerializeField] private bool m_SetPrente = true;
        [SerializeField] private float m_LerpTime = 0.2f;
        [SerializeField] private EVector3 m_OffsetPos = new EVector3();
        [SerializeField] private EVector3 m_OffsetRot = new EVector3();

        #region Property
        [EditorProperty("对齐至点数据", EditorPropertyType.EEPT_Bool)]
        public bool AlignToPoint
        {
            get { return m_AlignToPoint; }
            set { m_AlignToPoint = value; }
        }
        [EditorProperty("目标", EditorPropertyType.EEPT_SelectTransform, LabelWidth = 50)]
        public SelectTransform ReferTransform
        {
            get { return m_ReferTransform; }
            set { m_ReferTransform = value; }
        }
        [EditorProperty("附加至", EditorPropertyType.EEPT_SelectTransform, LabelWidth = 50)]
        public SelectTransform TargetTransform
        {
            get { return m_TargetTransform; }
            set { m_TargetTransform = value; }
        }
        [EditorProperty("目标点数据", EditorPropertyType.EEPT_GPoint)]
        public GPoint AlignPoint
        {
            get { return m_AlignPoint; }
            set { m_AlignPoint = value; }
        }
        [EditorProperty("设置父子级", EditorPropertyType.EEPT_Bool)]
        public bool SetPrente
        {
            get { return m_SetPrente; }
            set { m_SetPrente = value; }
        }
        // [EditorProperty("对齐至附加对象", EditorPropertyType.EEPT_Bool)]
        // public bool alignToTarget
        // {
        //     get { return m_AlignToTarget; }
        //     set { m_AlignToTarget = value; }
        // }
        [EditorProperty("对齐过渡时间", EditorPropertyType.EEPT_Float)]
        public float LerpTime
        {
            get { return m_LerpTime; }
            set { m_LerpTime = value; }
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
        #endregion
        public int GetEvenType() => (int)EEvenType.EET_Attach;
        public IActionEventData Creact() => new Event_Attach();
        [NonSerialized] private float m_LerpTime_Local;
        [NonSerialized] private bool m_IsValid;
        [NonSerialized] public Vector3 m_StartPos;
        [NonSerialized] public Quaternion m_StartRot;
        [NonSerialized] public Transform m_Refer;
        [NonSerialized] public Transform m_Target;
        [NonSerialized] public float m_LerpTime_t;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine _actionStateMachine = _actionState.ActionStateMachine;
            // m_AlignPoint.Init(_actionStateMachine);
            m_Refer = m_ReferTransform.Get(_actionStateMachine);
            m_LerpTime_Local = m_LerpTime;
            m_IsValid = true;
            // #if UNITY_EDITOR
            if (m_Refer is null)
            {
                EngineDebug.LogError("挂点切换事件出错！！！！，参考挂点为空");
                m_IsValid = false;
                return;
            }
            // #endif
            // Physics.OverlapBox()
            m_StartPos = m_Refer.position;
            m_StartRot = m_Refer.rotation;
            if (m_AlignToPoint)
            {
                //对齐至点数据
                if (_isSingle)
                {
                    if (m_AlignPoint.IsValid(_actionState))
                    {
                        if (m_LerpTime > 0)
                        {
                            if (_actionStateMachine.TryGetLogic(out Ex_Update_Attach ex_UpdateAttach, nameof(Ex_Update_Attach)))
                            {
                                ex_UpdateAttach.StartAttach(this);
                            }
                        }
                        else
                        {
                            m_Refer.position = m_AlignPoint.GetValue(_actionState).pos;
                            m_Refer.rotation = m_AlignPoint.GetValue(_actionState).rot;
                        }
                    }
                }
                return;
            }
            m_Target = m_TargetTransform.Get(_actionStateMachine);

            // #if UNITY_EDITOR
            if (m_Target is null)
            {
                EngineDebug.LogError("挂点切换事件出错！！！！，目标挂点为空");
                m_IsValid = false;
                return;
            }
            // #endif
            // EngineDebug.LogWarning($"L:[{m_Refer.name}], R:[{m_Target.name}]");
            if (m_SetPrente && m_Refer != m_Target) m_Refer.SetParent(m_Target);
            if (!m_AlignToPoint)
            {
                if (_isSingle)
                {
                    if (m_LerpTime > 0)
                    {
                        if (_actionStateMachine.TryGetLogic(out Ex_Update_Attach ex_UpdateAttach, nameof(Ex_Update_Attach)))
                        {
                            ex_UpdateAttach.StartAttach(this);
                        }
                    }
                    else
                    {
                        if (m_AlignToTarget)
                        {
                            m_Refer.position = m_Target.position;
                            m_Refer.rotation = m_Target.rotation;
                        }
                    }
                }
            }
            else
            {
                m_IsValid = false;
            }
        }

        public void LateUpdate(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (!m_IsValid) return;
            // ActionStateMachine _actionStateMachine = _actionState.ActionStateMachine;
            if (m_LerpTime_Local > 0)
            {
                m_LerpTime_Local -= _actionTime.Deltatime;
                if (m_LerpTime_Local > 0)
                {
                    float _property = m_LerpTime_Local / m_LerpTime;
                    if (m_AlignToPoint)
                    {
                        //对齐至点数据
                        m_Refer.position = Vector3.Lerp(m_AlignPoint.GetValue(_actionState).pos, m_StartPos, _property);
                        m_Refer.rotation = Quaternion.Lerp(m_AlignPoint.GetValue(_actionState).rot, m_StartRot, _property);
                    }
                    else
                    {
                        //对齐至变换
                        if (m_AlignToTarget)
                        {
                            m_Refer.position = Vector3.Lerp(m_Target.position, m_StartPos, _property);
                            m_Refer.rotation = Quaternion.Lerp(m_Target.rotation, m_StartRot, _property);
                        }
                    }
                }

                return;
            }

            if (m_AlignToPoint)
            {
                //对齐至点数据
                m_Refer.position = m_AlignPoint.GetValue(_actionState).pos;
                m_Refer.rotation = m_AlignPoint.GetValue(_actionState).rot;
            }
            else
            {
                if (m_AlignToTarget)
                {
                    m_Refer.position = m_Target.position;
                    m_Refer.rotation = m_Target.rotation;
                }
            }

        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            if (!m_IsValid) return;
            ActionStateMachine _actionStateMachine = _actionState.ActionStateMachine;
            if (m_AlignToPoint)
            {
                //对齐至点数据
                m_Refer.position = m_AlignPoint.GetValue(_actionState).pos;
                m_Refer.rotation = m_AlignPoint.GetValue(_actionState).rot;
            }
            else
            {
                if (m_AlignToTarget)
                {
                    m_Refer.position = m_Target.position;
                    m_Refer.rotation = m_Target.rotation;
                }
            }
        }

        public Vector3 GetPos_P(ActionStatePart _actionState)
        {
            return m_AlignPoint.GetValue(_actionState).pos;
        }
        public Quaternion GetRot_P(ActionStatePart _actionState)
        {
            return m_AlignPoint.GetValue(_actionState).rot;
        }

        public Vector3 GetPos_T()
        {
            return m_Target.position;
        }
        public Quaternion GetRot_T()
        {
            return m_Target.rotation;
        }
        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_Attach _event = _eventData as Event_Attach;
            _event.AlignToPoint = m_AlignToPoint;
            _event.ReferTransform = m_ReferTransform.Clone();
            _event.TargetTransform = m_TargetTransform.Clone();
            _event.AlignPoint = (GPoint)m_AlignPoint.Clone();
            // _event.alignToTarget = m_AlignToTarget;
            _event.SetPrente = m_SetPrente;
            _event.LerpTime = m_LerpTime;
            _event.OffsetPos = m_OffsetPos;
            _event.OffsetRot = m_OffsetRot;
            return _event;
        }
    }
}