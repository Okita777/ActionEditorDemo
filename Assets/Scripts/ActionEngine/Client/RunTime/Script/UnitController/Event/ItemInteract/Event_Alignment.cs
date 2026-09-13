using System;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_Alignment : IActionEventData
    {

        [SerializeField] private byte m_SelfUnit;
        [SerializeField] private ECharacteLimbType m_SelfTarget = ECharacteLimbType.Attach;
        [SerializeField] private byte m_ReferUnit;
        [SerializeField] private ECharacteLimbType m_ReferTarget;
        [SerializeField] private float m_LerpTime = 0.3f;
        [SerializeField] private bool m_IsGroup = true;
        [SerializeField] private byte m_AligType = 0;
        [SerializeField] private float m_Angle = 0;

        #region Property

        [EditorProperty("位移对象", EditorPropertyType.EEPT_Enum, EnumNames = new[] { "攻击者", "命中对象", "自身" })]
        public byte SelfUnit
        {
            get { return m_SelfUnit; }
            set { m_SelfUnit = value; }
        }

        [EditorProperty("位移对象的参考挂点", EditorPropertyType.EEPT_Enum, LabelWidth = 120)]
        public ECharacteLimbType SelfTarget
        {
            get { return m_SelfTarget; }
            set { m_SelfTarget = value; }
        }

        [EditorProperty("参考对象", EditorPropertyType.EEPT_Enum, EnumNames = new[] { "攻击者", "命中对象", "自身" })]
        public byte ReferUnit
        {
            get { return m_ReferUnit; }
            set { m_ReferUnit = value; }
        }

        [EditorProperty("参考对象的参考挂点", EditorPropertyType.EEPT_Enum, LabelWidth = 120)]
        public ECharacteLimbType ReferTarget
        {
            get { return m_ReferTarget; }
            set { m_ReferTarget = value; }
        }

        [EditorProperty("对齐参考类型", EditorPropertyType.EEPT_Enum, EnumNames = new[] { "对角线", "移动对象", "目标参考对象" })]
        public byte AligType
        {
            get { return m_AligType; }
            set { m_AligType = value; }
        }
        [EditorProperty("       角度偏移", EditorPropertyType.EEPT_Float, LabelWidth = 60)]
        public float Angle
        {
            get { return m_Angle; }
            set { m_Angle = value; }
        }

        [EditorProperty("相互角色对齐", EditorPropertyType.EEPT_Bool)]
        public bool IsGroup
        {
            get { return m_IsGroup; }
            set { m_IsGroup = value; }
        }
        [EditorProperty("对齐过渡时间", EditorPropertyType.EEPT_Float)]
        public float LerpTime
        {
            get { return m_LerpTime; }
            set { m_LerpTime = value; }
        }

        #endregion
        public int GetEvenType() => (int)EEvenType.EET_UnitAlignment;
        public IActionEventData Creact() => new Event_Alignment();
        [NonSerialized] private bool m_Valid;
        [NonSerialized] private float m_LerpTime_Local;
        [NonSerialized] private Vector3 m_LastReferTransPos;
        [NonSerialized] private Vector3 m_StartUnitPos;
        [NonSerialized] private Vector3 m_StartReferUnitPos;
        [NonSerialized] private Quaternion m_UnitStartRot;
        [NonSerialized] private Quaternion m_ReferUnitStartRot;
        [NonSerialized] private Vector3 m_StartPos;
        [NonSerialized] private Quaternion m_StartRot;
        [NonSerialized] private Vector3 m_ReferStartPos;
        [NonSerialized] private Quaternion m_ReferStartRot;
        [NonSerialized] private ActionEngine_Unit m_TargetUnit;
        [NonSerialized] private ActionEngine_Unit m_OnReferUnit;
        [NonSerialized] private Transform m_referTrans;
        [NonSerialized] private Transform m_ReferUnitTrans;
        [NonSerialized] private Ex_Update_CharacterControl m_CharacterControl;
        [NonSerialized] private Ex_Update_CharacterControl m_ReferCharacterControl;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            m_Valid = false;
            m_LerpTime_Local = m_LerpTime;
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            if (m_SelfUnit == 0) m_TargetUnit = _stateMachine.AttackerUnit;
            else if (m_SelfUnit == 1) m_TargetUnit = _stateMachine.HitUnit.GetUnit();
            else m_TargetUnit = _stateMachine.CurUnit;

            if (m_ReferUnit == 0) m_OnReferUnit = _stateMachine.AttackerUnit;
            else if (m_ReferUnit == 1) m_OnReferUnit = _stateMachine.HitUnit.GetUnit();
            else m_OnReferUnit = _stateMachine.CurUnit;

            if (m_TargetUnit is null || m_OnReferUnit is null)
            {
                EngineDebug.Log("单位不存在");
                return;
            }
            if (m_OnReferUnit.ActionStateMachine.TryGetLogic(out m_ReferCharacterControl, nameof(Ex_Update_CharacterControl)))
            {
                if (m_OnReferUnit.ActionStateMachine.TryGetCharacterLimb(m_ReferTarget, out Transform targetTrans))
                {
                    m_ReferUnitTrans = targetTrans;
                    m_ReferStartPos = targetTrans.position;
                    m_ReferStartRot = targetTrans.rotation;
                    m_Valid = true;
                }
            }

            if (!m_Valid)
            {
                EngineDebug.Log("单位对齐事件轨执行失败, Action: " + _actionState.CurrentActionState.Name);
                return;
            }

            m_Valid = false;
            if (m_TargetUnit.ActionStateMachine.TryGetLogic(out m_CharacterControl, nameof(Ex_Update_CharacterControl)))
            {
                if (m_TargetUnit.ActionStateMachine.TryGetCharacterLimb(m_SelfTarget, out Transform targetTrans))
                {
                    m_referTrans = targetTrans;
                    m_StartPos = targetTrans.position;
                    m_StartRot = targetTrans.rotation;
                    m_Valid = true;
                }
            }
            if (!m_Valid)
            {
                EngineDebug.Log("单位对齐事件轨执行失败, Action: " + _actionState.CurrentActionState.Name);
                return;
            }
            m_StartUnitPos = m_TargetUnit.transform.position;
            m_UnitStartRot = m_TargetUnit.transform.rotation;
            m_StartReferUnitPos = m_OnReferUnit.transform.position;
            m_ReferUnitStartRot = m_OnReferUnit.transform.rotation;

        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (!m_Valid) return;
            MoveUnit(_actionState, _actionTime);
        }

        private void MoveUnit(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (m_IsGroup)
            {
                //相互位移和对齐
                if (m_LerpTime_Local > 0)
                {
                    m_LerpTime_Local -= _actionTime.Deltatime;
                    float lerpValue = m_LerpTime_Local / m_LerpTime;

                    //移动单位的对角线
                    Vector3 dir = m_StartReferUnitPos - m_StartUnitPos;
                    dir.y = 0;
                    Quaternion quaternion = Quaternion.LookRotation(dir) * Quaternion.Euler(0, m_Angle, 0);
                    m_TargetUnit.transform.rotation = Quaternion.Slerp(quaternion, m_UnitStartRot, lerpValue);

                    //参考单位的对角线
                    dir *= -1;
                    quaternion = Quaternion.LookRotation(dir);
                    m_OnReferUnit.transform.rotation = Quaternion.Slerp(quaternion, m_ReferUnitStartRot, lerpValue);

                    //移动对象未来的理想位置
                    Vector3 localPos = Vector3.Lerp(m_ReferUnitTrans.position, m_StartUnitPos, lerpValue);
                    Vector3 movePos = localPos - m_TargetUnit.transform.position;
                    m_CharacterControl.CharacterMove = movePos;

                    // 参考对象未来理想位置
                    localPos = Vector3.Lerp(m_referTrans.position, m_ReferStartPos, lerpValue);
                    m_ReferCharacterControl.CharacterMove = localPos - m_ReferUnitTrans.position;
                }
                else
                {
                    //常规位移
                    Vector3 deltaPos = m_ReferUnitTrans.position - m_referTrans.position;
                    m_CharacterControl.CharacterMove = deltaPos;

                    //todo: 处理下辅助位移，在交互对象碰到障碍无法移动时补正当前单位位移
                    // deltaPos = m_LastReferTransPos - m_ReferUnitTrans.position;
                    // m_ReferCharacterControl.CharacterMove = deltaPos;
                    // m_LastReferTransPos = m_referTrans.position;

                    m_TargetUnit.transform.rotation = m_ReferUnitTrans.rotation;
                }
            }
            else
            {
                //仅修改【位移对象】坐标变换
                if (m_LerpTime_Local > 0)
                {
                    m_LerpTime_Local -= _actionTime.Deltatime;
                    float lerpValue = m_LerpTime_Local / m_LerpTime;

                    if (m_AligType == 0)
                    {
                        Vector3 dir = m_OnReferUnit.transform.position - m_StartPos;
                        dir.y = 0;
                        Quaternion quaternion = Quaternion.LookRotation(dir) * Quaternion.Euler(0, m_Angle, 0);

                        //未转换，临时方案
                        Vector3 targetPos = Vector3.Lerp(m_ReferUnitTrans.position, m_StartPos, lerpValue);
                        m_CharacterControl.CharacterMove = targetPos - m_referTrans.position;

                        Quaternion targetRot = Quaternion.Slerp(quaternion, m_StartRot, lerpValue);
                        m_TargetUnit.transform.rotation = targetRot;
                    }
                    else
                    {
                        Vector3 targetPos = Vector3.Lerp(m_ReferUnitTrans.position, m_StartPos, lerpValue);
                        m_CharacterControl.CharacterMove = targetPos - m_referTrans.position;
                        Quaternion targetRot = Quaternion.Slerp(m_ReferUnitTrans.rotation, m_StartRot, lerpValue);
                        m_TargetUnit.transform.rotation = targetRot;
                    }

                }
                else
                {
                    if (m_AligType == 0)
                    {
                        m_CharacterControl.CharacterMove = m_ReferUnitTrans.position - m_referTrans.position;
                        m_TargetUnit.transform.rotation = Quaternion.Lerp(m_TargetUnit.transform.rotation, m_ReferUnitTrans.rotation, 7 * _actionTime.Deltatime);
                    }
                    else
                    {
                        m_CharacterControl.CharacterMove = m_ReferUnitTrans.position - m_referTrans.position;
                        m_TargetUnit.transform.rotation = m_ReferUnitTrans.rotation;
                    }

                }
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_Alignment _event = _eventData as Event_Alignment;
            _event.SelfUnit = m_SelfUnit;
            _event.SelfTarget = m_SelfTarget;
            _event.ReferUnit = m_ReferUnit;
            _event.ReferTarget = m_ReferTarget;
            _event.AligType = m_AligType;
            // _event.CustomAngle = m_CustomAngle;
            _event.Angle = m_Angle;
            // _event.TargetPointData = m_TargetPointData.Clone();
            _event.IsGroup = m_IsGroup;
            _event.LerpTime = m_LerpTime;
            return _event;
        }
    }
}