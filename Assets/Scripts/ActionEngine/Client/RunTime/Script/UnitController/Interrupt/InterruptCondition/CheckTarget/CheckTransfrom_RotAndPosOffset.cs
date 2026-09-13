using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class CheckTransfrom_RotAndPosOffset : IInterruptCondition
    {
        #region Enum
        public enum ECheckType
        {
            DistanceAndAngle,
            OnlyDistance,
            OnlyAngle,
        }

        #endregion

        #region Fields
        [SerializeField] protected int m_selectTransform;
        // [SerializeField] protected bool m_IsLocalPlayer = false;
        // [SerializeField] protected SelectTransform m_Target = new SelectTransform();
        [SerializeReference] protected GraphEvent_NoValue_Vector3 m_Target = new GraphEvent_NoValue_Vector3();
        [SerializeField] protected ECheckType m_CheckType = ECheckType.DistanceAndAngle;
        [SerializeField] protected float m_AngleOffset = 0;
        [SerializeField] protected EVector3 m_PosOffset = new EVector3();
        [SerializeField] protected bool m_ConstomHeightS = false;
        [SerializeField] protected float m_ConstomHeight = 1.0f;
        [SerializeField] public float m_Angle = 30;
        [SerializeField] public GFloat m_Distance = new GFloat(2);
        [SerializeField] public bool m_CheckAngleGreater = false;
        [SerializeField] public bool m_CheckDisGreater = false;

        #endregion

        #region property
        [EditorProperty("起始目标", EditorPropertyType.EEPT_CharacteLimbType)]
        public int selectTransform
        {
            get { return m_selectTransform; }
            set { m_selectTransform = value; }
        }
        // [EditorProperty("目标对象为本地玩家",EditorPropertyType.EEPT_Bool, LabelWidth = 120)]
        // public bool IsLocalPlayer
        // {
        //     get { return m_IsLocalPlayer; }
        //     set { m_IsLocalPlayer = value; }
        // }
        [EditorProperty("   目标对象", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Vector3 Target
        {
            get { return m_Target; }
            set { m_Target = value; }
        }
        [EditorProperty("检查类型", EditorPropertyType.EEPT_Enum)]
        public ECheckType CheckType
        {
            get { return m_CheckType; }
            set { m_CheckType = value; }
        }
        [EditorProperty("角度偏移", EditorPropertyType.EEPT_Float)]
        public float AngleOffset
        {
            get { return m_AngleOffset; }
            set { m_AngleOffset = value; }
        }
        [EditorProperty("位置偏移", EditorPropertyType.EEPT_Vector3)]
        public EVector3 PosOffset
        {
            get { return m_PosOffset; }
            set { m_PosOffset = value; }
        }
        [EditorProperty("柱形", EditorPropertyType.EEPT_Bool)]
        public bool ConstomHeightS
        {
            get { return m_ConstomHeightS; }
            set { m_ConstomHeightS = value; }
        }
        [EditorProperty("   高度", EditorPropertyType.EEPT_Float)]
        public float ConstomHeight
        {
            get { return m_ConstomHeight; }
            set { m_ConstomHeight = value; }
        }
        [EditorProperty("", EditorPropertyType.EEPT_GFloat, LabelWidth = 0)]
        public GFloat Distance
        {
            get { return m_Distance; }
            set { m_Distance = value; }
        }
        #endregion

        public int InterruptType => (int)EConditionType.EIT_CheckTransfrom_RotAndPosOffset;

        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            if (unit == null || actionStatePart == null) return false;

            float dis = m_Distance.GetValue(actionStatePart);

            ActionStateMachine _machine = actionStatePart.ActionStateMachine;
            if (_machine == null) return false;
            // m_Target.Init(_machine);
            // if (!m_Target.IsValid()) return false;
            Vector3 targetPos = m_Target.value(actionStatePart, new ActionMachineTime());
            Vector3 referencePosition;
            Quaternion referenceRotation;
            if (actionStatePart.IsTem)
            {
                referencePosition = actionStatePart.Pos;
                referenceRotation = actionStatePart.Rot;
            }
            else if (_machine.TryGetCharacterLimb(
                         (ECharacteLimbType)m_selectTransform,
                         out Transform referenceTransform))
            {
                referencePosition = referenceTransform.position;
                referenceRotation = referenceTransform.rotation;
            }
            else
            {
                return false;
            }

            if (m_CheckType == ECheckType.OnlyDistance || m_CheckType == ECheckType.DistanceAndAngle)
            {
                //位置对比
                Vector3 _stPos = referencePosition + referenceRotation * m_PosOffset.GetValue();
                Vector3 _offsetPos = targetPos - _stPos;

                if (m_CheckType != ECheckType.OnlyAngle)
                {
                    if (ConstomHeightS)
                    {
                        _offsetPos.y = 0;

                        //当前位置和目标位置差距的绝对值
                        float heightOffset = Mathf.Abs(targetPos.y - _stPos.y);

                        if ((_offsetPos.sqrMagnitude >= dis * dis) == m_CheckDisGreater)
                        {
                            if (heightOffset < m_ConstomHeight)
                            {
                                if (m_CheckType == ECheckType.OnlyDistance) return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if ((_offsetPos.sqrMagnitude >= dis * dis) == m_CheckDisGreater)
                        {
                            if (m_CheckType == ECheckType.OnlyDistance) return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                //角度对比
                _offsetPos.y = 0;
                Vector3 _selfFward = Quaternion.Euler(0, m_AngleOffset, 0) *
                                     (referenceRotation * Vector3.forward);
                bool isGreate = Mathf.Abs(Vector3.Angle(_offsetPos, _selfFward)) >= (m_Angle * 0.5f);
                return isGreate == m_CheckAngleGreater;
            }


            return false;
        }


        public void EditorDraw(ActionEngine_Unit unit, CharacterConfig characterConfig, ActionMachineTime _actionTime)
        {
#if UNITY_EDITOR
            //if (characterConfig == null) return;
            float dis = m_Distance.mSerValue;
            if (_actionTime.IsInRange)
            {
                Transform _st = unit.transform;

                if(characterConfig != null)
                {
                    _st = characterConfig.transform;
                    if (characterConfig.HelpPointDic.TryGetValue(
                            (ECharacteLimbType)m_selectTransform,
                            out Transform configuredTransform) &&
                        configuredTransform != null)
                    {
                        _st = configuredTransform;
                    }
                }

                if (_st != null)
                {
                    Vector3 _stPos = _st.position + _st.rotation * m_PosOffset.GetValue();

                    //绘制有效范围
                    if (m_CheckType != ECheckType.OnlyAngle)
                    {
                        if (ConstomHeightS)
                        {
                            EngineScenceDraw.WireDisc(_stPos, _st.up, dis, Color.cyan);
                            EngineScenceDraw.WireDisc(_stPos + Vector3.up * m_ConstomHeight, _st.up, dis, Color.cyan);
                            Vector3 _stP = _stPos + _st.right * (dis);
                            EngineScenceDraw.Line(_stP, _stP + _st.up * ConstomHeight, Color.cyan);
                            _stP = _stPos - _st.right * (dis);
                            EngineScenceDraw.Line(_stP, _stP + _st.up * ConstomHeight, Color.cyan);
                            _stP = _stPos + _st.forward * (dis);
                            EngineScenceDraw.Line(_stP, _stP + _st.up * ConstomHeight, Color.cyan);
                            _stP = _stPos - _st.forward * (dis);
                            EngineScenceDraw.Line(_stP, _stP + _st.up * ConstomHeight, Color.cyan);
                        }
                        else
                        {
                            EngineScenceDraw.Sphere(_stPos, _st.rotation, dis, Color.cyan);
                        }
                    }

                    //绘制有效角度
                    if (m_CheckType != ECheckType.OnlyDistance)
                    {
                        Quaternion _referRot = _st.rotation * Quaternion.Euler(0, AngleOffset, 0);
                        // Vector3 _offsetPos = _st.position + m_PosOffset.GetValue();

                        if (m_CheckAngleGreater)
                        {
                            Vector3 a = _referRot * Quaternion.Euler(0, m_Angle * 0.5f, 0) * Vector3.forward;
                            EngineScenceDraw.SolidArc(_stPos, _st.up, a, 360 - m_Angle, dis,
                                Color.cyan * 0.5f);
                        }
                        else
                        {
                            Vector3 a = _referRot * Quaternion.Euler(0, -m_Angle * 0.5f, 0) * Vector3.forward;
                            EngineScenceDraw.SolidArc(_stPos, _st.up, a, m_Angle, dis, Color.cyan * 0.5f);
                        }
                    }

                    // if (m_Target.IsValid())
                    // {
                    //     EngineScenceDraw.Line(_stPos, m_Target.Get().position, Color.green);
                    // }
                }
            }
#endif
        }

        public IInterruptCondition Clone()
        {
            CheckTransfrom_RotAndPosOffset _check = new CheckTransfrom_RotAndPosOffset();
            _check.selectTransform = m_selectTransform;
            // _check.IsLocalPlayer = m_IsLocalPlayer;
            _check.Target = m_Target.Clone();
            _check.CheckType = m_CheckType;
            _check.AngleOffset = m_AngleOffset;
            _check.PosOffset = m_PosOffset;
            _check.ConstomHeightS = m_ConstomHeightS;
            _check.ConstomHeight = m_ConstomHeight;
            // _check.IsRange = m_IsRange;
            _check.m_Angle = m_Angle;
            _check.m_Distance = (GFloat)m_Distance.Clone();
            _check.m_CheckAngleGreater = m_CheckAngleGreater;
            _check.m_CheckDisGreater = m_CheckDisGreater;
            return _check;
        }
    }
}