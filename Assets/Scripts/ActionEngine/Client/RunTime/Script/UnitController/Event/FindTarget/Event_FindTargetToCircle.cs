using System.Collections.Generic;
using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using AsiTimeLine.RunTime;
using UnityEngine;

namespace AsiActionEditor_Ex.RunTime
{
    [System.Serializable]
    public class Event_FindTargetToCircle : IActionEventData
    {
        #region Fields
        //[SerializeReference] protected GraphEvent_NoValue_Vector3 m_Target = new GraphEvent_NoValue_Vector3();
        [SerializeField] protected GraphEvent_NoValue_Point m_centerPoint = new GraphEvent_NoValue_Point();
        [SerializeField] protected GGroupUnit m_GGroupUnit = new GGroupUnit();
        [SerializeField] protected GGroupPoint m_GroupPoint = new GGroupPoint();
        [SerializeField] protected GGroupTransform m_GroupTransform = new GGroupTransform();
        [SerializeField] protected bool m_ConstomHeightS = false;
        [SerializeField] protected bool m_IsOverrid = true;
        [SerializeField] protected bool m_CheckAngle = true;
        [SerializeField] protected byte m_GroupType;
        [SerializeField] protected int m_ScenceLayer;
        [SerializeField] protected float m_AngleOffset = 0;
        [SerializeField] protected float m_ConstomHeight = 1.0f;
        [SerializeField] protected EVector3 m_PosOffset = new EVector3();
        [SerializeField] protected GValue_SetFloat m_DisP = new GValue_SetFloat(false);
        [SerializeField] public GFloat m_Distance = new GFloat(2);
        //[SerializeField] public bool m_CheckAngleGreater = false;
        //[SerializeField] public bool m_CheckDisGreater = false;
        [SerializeField] public float m_Angle = 30;

        #endregion

        #region property
        [EditorProperty("中心位置", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Point CenterPoint
        {
            get { return m_centerPoint; }
            set { m_centerPoint = value; }
        }
        [EditorProperty("位置偏移", EditorPropertyType.EEPT_Vector3)]
        public EVector3 PosOffset
        {
            get { return m_PosOffset; }
            set { m_PosOffset = value; }
        }
        [EditorProperty("目标层级", EditorPropertyType.EEPT_LayerMask)]
        public int ScenceLayer
        {
            get { return m_ScenceLayer; }
            set { m_ScenceLayer = value; }
        }
        [EditorProperty("写入数组类型", EditorPropertyType.EEPT_Enum, LabelWidth = 80,
            EnumNames = new[] { "GroupPoint", "GroupTransform", "GroupUnit" })]
        public byte GroupType
        {
            get { return m_GroupType; }
            set { m_GroupType = value; }
        }

        [EditorProperty("写入数组(Unit)", EditorPropertyType.EEPT_GGroupUnit, LabelWidth = 120)]
        public GGroupUnit GroupUnit
        {
            get { return m_GGroupUnit; }
            set { m_GGroupUnit = value; }
        }
        [EditorProperty("写入数组(Point)", EditorPropertyType.EEPT_GGroupPoint, LabelWidth = 120)]
        public GGroupPoint GroupPoint
        {
            get { return m_GroupPoint; }
            set { m_GroupPoint = value; }
        }
        [EditorProperty("写入数组(Transform)", EditorPropertyType.EEPT_GGroupTransform, LabelWidth = 120)]
        public GGroupTransform GroupTransform
        {
            get { return m_GroupTransform; }
            set { m_GroupTransform = value; }
        }
        [EditorProperty("覆写目标(否则为添加)", EditorPropertyType.EEPT_Bool, LabelWidth = 120)]
        public bool IsOverrid
        {
            get { return m_IsOverrid; }
            set { m_IsOverrid = value; }
        }
        //[EditorProperty("   目标对象", EditorPropertyType.EEPT_GraphValue)]
        //public GraphEvent_NoValue_Vector3 Target
        //{
        //    get { return m_Target; }
        //    set { m_Target = value; }
        //}
        [EditorProperty("检查角度", EditorPropertyType.EEPT_Bool)]
        public bool CheckAngle
        {
            get { return m_CheckAngle; }
            set { m_CheckAngle = value; }
        }
        [EditorProperty("角度偏移", EditorPropertyType.EEPT_Float)]
        public float AngleOffset
        {
            get { return m_AngleOffset; }
            set { m_AngleOffset = value; }
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
        [EditorProperty("半径", EditorPropertyType.EEPT_GFloat, LabelWidth = 80)]
        public GFloat Distance
        {
            get { return m_Distance; }
            set { m_Distance = value; }
        }
        [EditorProperty("距离衰减", EditorPropertyType.EEPT_SetGFloat, LabelWidth = 80)]
        public GValue_SetFloat DisP
        {
            get { return m_DisP; }
            set { m_DisP = value; }
        }
        #endregion

        public int GetEvenType() => (int)EEvenType.EET_FindTargetToCircle;

        public IActionEventData Creact() => new Event_FindTargetToCircle();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine _actionEngine = _actionState.ActionStateMachine;
            FindTarget(_actionState, new ActionMachineTime());
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            FindTarget(_actionState, _actionTime);
            //ActionStateMachine _actionEngine = _actionState.ActionStateMachine;
        }

        private void FindTarget(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            HashSet<UnityEngine.Object> _hashList_Unit = EngineResourcesManager.Instance.HashSet_Object();
            if (IsOverrid)
            {
                if (GroupType == 0)
                    GroupPoint.Clear(_actionState);
                if (GroupType == 1)
                    GroupTransform.Clear(_actionState);
                if (GroupType == 2)
                    GroupUnit.Clear(_actionState);
            }
            else
            {
                if (GroupType == 2)
                    foreach (ActionEngine_Unit VARIABLE in GroupUnit.GetValue(_actionState))
                    {
                        _hashList_Unit.Add(VARIABLE);
                    }
            }

            ActionStatePart actionStatePart = _actionState;
            float dis = m_Distance.GetValue(actionStatePart);
            ActionStateMachine _machine = actionStatePart.ActionStateMachine;

            PointData _point = m_centerPoint.value(_actionState, _actionTime);
            Vector3 _pointP = _point.pos;
            Quaternion _pointR = _point.rot;
            Vector3 _stPos = _pointP + _pointR * m_PosOffset.GetValue();
            List<Transform> _list = EngineResourcesManager.Instance.CreateTransforms();
            int _layerMask = _machine.GetLayer(m_ScenceLayer);
            int findConst = Physics.OverlapSphereNonAlloc(
                _stPos,
                dis + 2.0f,
                EngineResourcesManager.Instance.Colliders,
                _layerMask);
            for (int i = 0; i < findConst; i++)
            {
                _list.Add(EngineResourcesManager.Instance.Colliders[i].transform);
            }
            DistanceSizer(_actionState, _list, _stPos, dis);
            AngleSizer(_actionState, _list, _stPos, _pointR * Vector3.forward);
            foreach (Transform t in _list)
            {
                if (m_GroupType == 0)
                {
                    GroupPoint.GetValue(_actionState).Add(new PointData(t.position, t.rotation));
                }
                else if (m_GroupType == 1)
                {
                    GroupTransform.GetValue(_actionState).Add(t);
                }
                else
                {
                    if (t.TryGetComponent(out TargetUnit _targetUnit))
                    {
                        ActionEngine_Unit _unit = _targetUnit.GetUnit();
                        if (!_hashList_Unit.Contains(_unit))
                        {
                            _hashList_Unit.Add(_unit);
                            GroupUnit.GetValue(_actionState).Add(_unit);
                        }
                    }
                }
            }

            //Debug.LogWarning($"找到了对象[{GroupUnit.GetValue(_actionState).Count}]");
        }

        private void DistanceSizer(ActionStatePart _actionState, List<Transform> _list, Vector3 _starPos, float _checkDis)
        {
            bool isCheckGV = (m_GroupType == 2) && (DisP.m_IsSet);
            if (isCheckGV)
            {
                for (int i = _list.Count - 1; i >= 0; i--)
                {
                    Transform t = _list[i];
                    if (t.TryGetComponent(out TargetUnit targetUnit))
                    {
                        Vector3 _offsetPos = t.position - _starPos;
                        if (ConstomHeightS) _offsetPos.y = 0;
                        ActionStatePart _targetPart = targetUnit.GetUnit().ActionStateMachine.FirstStatePart;
                        float _realyDis = (_offsetPos.magnitude - DisP.m_Value.GetValue(_targetPart));
                        if (_realyDis > _checkDis)
                        {
                            //剔除不满足条件的
                            _list.RemoveAt(i);
                        }
                        else if (ConstomHeightS)
                        {
                            float heightOffset = t.position.y - _starPos.y;
                            if (heightOffset > m_ConstomHeight || heightOffset < 0)
                            {
                                //剔除不满足条件的
                                _list.RemoveAt(i);
                            }
                        }
                    }
                }
            }
            else
            {
                _checkDis = _checkDis * _checkDis;
                for (int i = _list.Count - 1; i >= 0; i--)
                {
                    Transform t = _list[i];
                    Vector3 _offsetPos = t.position - _starPos;
                    if (ConstomHeightS) _offsetPos.y = 0;
                    if (_offsetPos.sqrMagnitude > _checkDis)
                    {
                        //剔除不满足条件的
                        _list.RemoveAt(i);
                    }
                    else if (ConstomHeightS)
                    {
                        float heightOffset = t.position.y - _starPos.y;
                        if (heightOffset > m_ConstomHeight || heightOffset < 0)
                        {
                            //剔除不满足条件的
                            _list.RemoveAt(i);
                        }
                    }
                }
            }

        }
        private void AngleSizer(ActionStatePart _actionState, List<Transform> _list, Vector3 _starPos, Vector3 _foward)
        {
            if (m_CheckAngle)
            {
                for (int i = _list.Count - 1; i >= 0; i--)
                {
                    Transform t = _list[i];
                    Vector3 _offsetPos = t.position - _starPos;
                    _offsetPos.y = 0;

                    Vector3 _selfFward = Quaternion.Euler(0, m_AngleOffset, 0) * _foward;
                    if (Vector3.Angle(_offsetPos, _selfFward) * 2 > m_Angle)
                    {
                        //剔除不满足条件的
                        _list.RemoveAt(i);
                    }
                }
            }
        }
        public void EditorDraw(CharacterConfig characterConfig, ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
#if UNITY_EDITOR
            if (!_actionTime.IsInRange || _actionState is null) return;

            float dis = m_Distance.mSerValue;
            if (_actionTime.IsInRange)
            {
                {
                    Vector3 _pointP;
                    Quaternion _pointR;
                    if (Application.isPlaying)
                    {
                        PointData _point = m_centerPoint.value(_actionState, _actionTime);
                        _pointP = _point.pos;
                        _pointR = _point.rot;
                    }
                    else
                    {
                        _pointP = _actionState.Pos;
                        _pointR = _actionState.Rot;
                    }

                    Vector3 _stPos = _pointP + _pointR * m_PosOffset.GetValue();

                    //绘制有效范围
                    //if (m_CheckType != ECheckType.OnlyAngle)
                    {
                        if (ConstomHeightS)
                        {
                            EngineScenceDraw.WireDisc(_stPos, _pointR * Vector3.up, dis, Color.cyan);
                            EngineScenceDraw.WireDisc(_stPos + Vector3.up * m_ConstomHeight, _pointR * Vector3.up, dis, Color.cyan);
                            Vector3 _stP = _stPos + _pointR * Vector3.right * (dis);
                            EngineScenceDraw.Line(_stP, _stP + _pointR * Vector3.up * ConstomHeight, Color.cyan);
                            _stP = _stPos - _pointR * Vector3.right * (dis);
                            EngineScenceDraw.Line(_stP, _stP + _pointR * Vector3.up * ConstomHeight, Color.cyan);
                            _stP = _stPos + _pointR * Vector3.forward * (dis);
                            EngineScenceDraw.Line(_stP, _stP + _pointR * Vector3.up * ConstomHeight, Color.cyan);
                            _stP = _stPos - _pointR * Vector3.forward * (dis);
                            EngineScenceDraw.Line(_stP, _stP + _pointR * Vector3.up * ConstomHeight, Color.cyan);
                        }
                        else
                        {
                            EngineScenceDraw.Sphere(_stPos, _pointR, dis, Color.cyan);
                        }
                    }

                    //绘制有效角度
                    if (m_CheckAngle)
                    {
                        Quaternion _referRot = _pointR * Quaternion.Euler(0, AngleOffset, 0);
                        // Vector3 _offsetPos = _st.position + m_PosOffset.GetValue();

                        //if (m_CheckAngleGreater)
                        //{
                        //    Vector3 a = _referRot * Quaternion.Euler(0, m_Angle * 0.5f, 0) * Vector3.forward;
                        //    EngineScenceDraw.SolidArc(_stPos, _st.up, a, 360 - m_Angle, dis,
                        //        Color.cyan * 0.5f);
                        //}
                        //else
                        {
                            Vector3 a = _referRot * Quaternion.Euler(0, -m_Angle * 0.5f, 0) * Vector3.forward;
                            EngineScenceDraw.SolidArc(_stPos, _pointR * Vector3.up, a, m_Angle, dis, Color.cyan * 0.5f);
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

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_FindTargetToCircle _event = _eventData as Event_FindTargetToCircle;
            _event.CenterPoint = m_centerPoint.Clone();
            //_event.Target = m_Target.Clone();
            _event.CheckAngle = m_CheckAngle;
            _event.AngleOffset = m_AngleOffset;
            _event.PosOffset = m_PosOffset;
            _event.ConstomHeightS = m_ConstomHeightS;
            _event.ConstomHeight = m_ConstomHeight;
            _event.m_Angle = m_Angle;
            _event.IsOverrid = m_IsOverrid;
            _event.GroupType = m_GroupType;
            _event.ScenceLayer = m_ScenceLayer;
            _event.GroupUnit = (GGroupUnit)m_GGroupUnit.Clone();
            _event.GroupPoint = (GGroupPoint)m_GroupPoint.Clone();
            _event.m_GroupTransform = (GGroupTransform)GroupTransform.Clone();
            _event.m_Distance = (GFloat)m_Distance.Clone();
            _event.DisP = m_DisP.Clone();
            //_event.m_CheckAngleGreater = m_CheckAngleGreater;
            //_event.m_CheckDisGreater = m_CheckDisGreater;
            return _event;
        }
    }
}