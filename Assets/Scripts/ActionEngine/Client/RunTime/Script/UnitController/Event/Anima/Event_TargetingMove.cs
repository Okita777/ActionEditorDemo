using System;
using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_TargetingMove : IActionEventData
    {
        [SerializeField] protected int m_RefertBone;
        [SerializeField] protected byte m_targetForward = 2;
        [SerializeField] protected bool m_FreeToX = true;
        [SerializeField] protected bool m_FreeToY = true;
        [SerializeField] protected GraphEvent_NoValue_Vector3 m_lookTarget = new GraphEvent_NoValue_Vector3();
        [SerializeField] protected GFloat m_WorldAngle = new GFloat();
        [SerializeField] protected int m_TargetBone;
        [SerializeField] protected int m_TargetBoneLinks;
        [SerializeField] protected int m_EnterTime;
        [SerializeField] protected int m_ExitTime;
        [SerializeField] protected EVector2 m_AngleLock_X = new EVector2(60, -60);
        [SerializeField] protected EVector2 m_SelfAngle_X = new EVector2(100, -90);
        [SerializeField] protected EVector2 m_AngleLock_Y = new EVector2(60, -60);
        [SerializeField] protected EVector2 m_SelfAngle_Y = new EVector2(100, -90);

        #region Serproperty

        [EditorProperty("参考朝向: ", EditorPropertyType.EEPT_CharacteLimbType)]
        public int RefertBone
        {
            get { return m_RefertBone; }
            set { m_RefertBone = value; }
        }
        [EditorProperty("参考朝向轴: ", EditorPropertyType.EEPT_Enum, EnumNames = new[] { "X", "Y", "Z", "-X", "-Y", "-Z" })]
        public byte TargetForward
        {
            get { return m_targetForward; }
            set { m_targetForward = value; }
        }

        [EditorProperty("目标骨骼: ", EditorPropertyType.EEPT_CharacteLimbType)]
        public int TargetBone
        {
            get { return m_TargetBone; }
            set { m_TargetBone = value; }
        }

        [EditorProperty("目标朝向: ", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Vector3 LookTarget
        {
            get { return m_lookTarget; }
            set { m_lookTarget = value; }
        }
        [EditorProperty("Y轴角度偏移: ", EditorPropertyType.EEPT_GFloat)]
        public GFloat WorldAngle
        {
            get { return m_WorldAngle; }
            set { m_WorldAngle = value; }
        }
        [EditorProperty("骨骼链数: ", EditorPropertyType.EEPT_Int)]
        public int TargetBoneLinks
        {
            get { return m_TargetBoneLinks; }
            set { m_TargetBoneLinks = value; }
        }

        [EditorProperty("允许上下旋转: ", EditorPropertyType.EEPT_Bool)]
        public bool FreeToX
        {
            get { return m_FreeToX; }
            set { m_FreeToX = value; }
        }
        [EditorProperty("允许左右旋转: ", EditorPropertyType.EEPT_Bool)]
        public bool FreeToY
        {
            get { return m_FreeToY; }
            set { m_FreeToY = value; }
        }
        [EditorProperty("混入时间: ", EditorPropertyType.EEPT_Int)]
        public int EnterTime
        {
            get { return m_EnterTime; }
            set { m_EnterTime = value; }
        }
        [EditorProperty("混出时间: ", EditorPropertyType.EEPT_Int)]
        public int ExitTime
        {
            get { return m_ExitTime; }
            set { m_ExitTime = value; }
        }

        [EditorProperty("骨骼可偏移范围_上下: ", EditorPropertyType.EEPT_Vector2, LabelWidth = 130)]
        public EVector2 AngleLock_X
        {
            get { return m_AngleLock_X; }
            set { m_AngleLock_X = value; }
        }
        [EditorProperty("大于范围触发复位_上下: ", EditorPropertyType.EEPT_Vector2, LabelWidth = 130)]
        public EVector2 SelfAngle_X
        {
            get { return m_SelfAngle_X; }
            set { m_SelfAngle_X = value; }
        }
        [EditorProperty("骨骼可偏移范围_左右: ", EditorPropertyType.EEPT_Vector2, LabelWidth = 130)]
        public EVector2 AngleLock_Y
        {
            get { return m_AngleLock_Y; }
            set { m_AngleLock_Y = value; }
        }
        [EditorProperty("大于范围触发复位_左右: ", EditorPropertyType.EEPT_Vector2, LabelWidth = 130)]
        public EVector2 SelfAngle_Y
        {
            get { return m_SelfAngle_Y; }
            set { m_SelfAngle_Y = value; }
        }
        // [EditorProperty("骨骼链权重曲线: ", EditorPropertyType.EEPT_AnimationCurve)]
        // public SerAnimationCurve LinkWeights
        // {
        //     get { return m_LinkWeights; }
        //     set { m_LinkWeights = value; }
        // }
        // [NonSerialized] private Transform m_SerReferTransform;
        // [NonSerialized] private Transform m_SerTransform;
        [NonSerialized] private bool m_IsValid;
        #endregion

        public int GetEvenType() => (int)EEvenType.EET_TargetingMove;
        public IActionEventData Creact() => new Event_TargetingMove();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            // Debug.Log($"接入瞄准，[{_actionState.AnimaLayer}]");
            m_IsValid = false;
            ActionStateMachine _actionStateMachine = _actionState.ActionStateMachine;
            // m_TargetSpace.Init(_actionStateMachine);
            // m_WorldAngle.Init(_actionStateMachine);
            // LookTarget.value(_actionState, new ActionMachineTime());
            if (_actionStateMachine.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig)))
            {
                if (_config.HelpPointDic.TryGetValue((ECharacteLimbType)TargetBone, out Transform _transform))
                {
                    if (_config.HelpPointDic.TryGetValue((ECharacteLimbType)RefertBone, out Transform _referTransform))
                    {
                        m_IsValid = true;
                        if (_actionStateMachine.TryGetLogic(out Ex_Update_TargetingMove _exTargetingMove, nameof(Ex_Update_TargetingMove)))
                        {
                            _exTargetingMove.OnSetRot(this, _transform, _referTransform);
                        }
                    }
                }
            }
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            ActionStateMachine _actionStateMachine = _actionState.ActionStateMachine;
            // Debug.Log($"退出瞄准，[{_actionState.AnimaLayer}]: [{m_IsValid}]");
            if (m_IsValid)
            {
                if (_actionStateMachine.TryGetLogic(out Ex_Update_TargetingMove _exTargetingMove, nameof(Ex_Update_TargetingMove)))
                {
                    _exTargetingMove.OnExit(m_ExitTime);
                }
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_TargetingMove _eventDataClone = _eventData as Event_TargetingMove;

            _eventDataClone.RefertBone = m_RefertBone;
            _eventDataClone.TargetBone = m_TargetBone;

            _eventDataClone.AngleLock_X = m_AngleLock_X;
            _eventDataClone.SelfAngle_X = m_SelfAngle_X;
            _eventDataClone.AngleLock_Y = m_AngleLock_Y;
            _eventDataClone.SelfAngle_Y = m_SelfAngle_Y;

            _eventDataClone.TargetBoneLinks = m_TargetBoneLinks;
            _eventDataClone.LookTarget = m_lookTarget.Clone();
            _eventDataClone.TargetForward = m_targetForward;
            // _eventDataClone.TargetSpace = (GEnum)m_TargetSpace.Clone();
            _eventDataClone.WorldAngle = (GFloat)m_WorldAngle.Clone();
            _eventDataClone.FreeToX = m_FreeToX;
            _eventDataClone.FreeToY = m_FreeToY;
            _eventDataClone.EnterTime = m_EnterTime;
            _eventDataClone.ExitTime = m_ExitTime;

            return _eventDataClone;
        }
    }
}