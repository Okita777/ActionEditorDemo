using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime.Event_Extend;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{

    [System.Serializable]
    public class Event_AttackBox : IActionEventData
    {
        [SerializeField] private AttackBoxInfo mAttackBoxInfo = new AttackBoxInfo();
        [SerializeField] private GValue_Setting mGValue_Setting_NotHit = new GValue_Setting();
        [SerializeField] private GValue_Setting mGValue_Setting_OnHit = new GValue_Setting();
        [SerializeField] private GValue_Setting mGValue_Setting = new GValue_Setting();
        //[SerializeField] private GValue_Ratio mGValueRatio = new GValue_Ratio();
        [SerializeField] private GraphEvent_NoValue_Bool mCheckHit = new GraphEvent_NoValue_Bool(true);
        [SerializeField] private byte mExcludeType = 1;
        [SerializeField] private int mExcludeMask = -1;

        [SerializeField] private int mHitLayer;
        [SerializeReference] private IAttackInfo mAttackInfo;

        [NonSerialized] private float curTime = 0;
        [NonSerialized] private LayerMask layerMask;
        [NonSerialized] private float mHitInterval = 0.0f, mRealyHitInterval = -1;
        private Collider[] Colliders => EngineResourcesManager.Instance.Colliders;
        private RaycastHit[] RaycastHits => EngineResourcesManager.Instance.RaycastHits;

        #region property
        public AttackBoxInfo AttackBoxInfo
        {
            get { return mAttackBoxInfo; }
            set { mAttackBoxInfo = value; }
        }

        public IAttackInfo AttackInfo
        {
            get { return mAttackInfo; }
            set { mAttackInfo = value; }
        }
        //[EditorProperty("排除", EditorPropertyType.EEPT_Enum, EnumNames = new[]
        //{"不做任何排除", "排除自身和源和持有者", "仅排除自身", "仅排除源和持有者"})]
        public byte ExcludeType
        {
            get { return mExcludeType; }
            set { mExcludeType = value; }
        }
        [EditorProperty("排除", EditorPropertyType.EEPT_EnumMask, EnumNames = new[]
        {"自身", "持有者", "源"})]
        public int ExcludeMask
        {
            get { return mExcludeMask; }
            set { mExcludeMask = value; }
        }
        [EditorProperty("检测层级", EditorPropertyType.EEPT_LayerMask)]
        public int HitLayer
        {
            get { return mHitLayer; }
            set { mHitLayer = value; }
        }
        [EditorProperty("修改本地GValue（进入时&命中前）", EditorPropertyType.EEPT_GValueSetting)]
        public GValue_Setting GValue_Setting_NotHit
        {
            get { return mGValue_Setting_NotHit; }
            set { mGValue_Setting_NotHit = value; }
        }
        //[EditorProperty("有效命中判定", EditorPropertyType.EEPT_GValueSRatio, LabelWidth = 0)]
        //public GValue_Ratio GValueRatio
        //{
        //    get { return mGValueRatio; }
        //    set { mGValueRatio = value; } 
        //}
        [EditorProperty("有效命中判定", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Bool CheckHit
        {
            get
            {
#if UNITY_EDITOR
                if (mCheckHit is null) mCheckHit = new GraphEvent_NoValue_Bool(true);
#endif
                return mCheckHit;
            }
            set { mCheckHit = value; }
        }
        [EditorProperty("修改被命中者GValue", EditorPropertyType.EEPT_GValueSetting)]
        public GValue_Setting GValue_Setting
        {
            get { return mGValue_Setting; }
            set { mGValue_Setting = value; }
        }
        [EditorProperty("修改本地GValue（命中后）", EditorPropertyType.EEPT_GValueSetting)]
        public GValue_Setting GValue_Setting_OnHit
        {
            get { return mGValue_Setting_OnHit; }
            set { mGValue_Setting_OnHit = value; }
        }
        #endregion

        public int GetEvenType() => -(int)EEvenTypeInternal.EET_AttackBox;
        public IActionEventData Creact() => new Event_AttackBox();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            _stateMachine.HitUnit = null;
            _stateMachine.OnHitObject = null;
            _actionState.AllHitObject.Clear();
            _actionState.LoopIndex = 0;
            UpdateExclude(_actionState);

            mGValue_Setting_NotHit.OnSet(_stateMachine);
            mHitInterval = 0.0f;

            if (_actionState.IsTem)
            {
                ActionEngine_Unit _unit = _stateMachine.CurUnit.GetSource;
                layerMask = _unit.ActionStateMachine.GetLayer(mHitLayer);
            }
            else
            {
                layerMask = _stateMachine.GetLayer(mHitLayer);
            }
            mAttackInfo.OnHitStart(_actionState);

            _lastHitPartIndex = 0;
            curTime = 0;

            mRealyHitInterval = -1;
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            // RemoteProxy 纯表现：不做无效 Physics 命中查询（省 DS/远端开销）。
            ActionStateMachine sm = _actionState != null ? _actionState.ActionStateMachine : null;
            if (sm != null && sm.Authority == SimulationAuthority.RemoteProxy)
                return;

            if (mRealyHitInterval < -0.5f)
            {
                if (mAttackBoxInfo.HitInterval < -0.5f)
                {
                    float _intervalNumber = ((int)mAttackBoxInfo.HitInterval * -1) - 1;
                    if (_actionTime.Duration < 0)
                    {
                        float actionTime = _actionState.CurrentActionState.TotalTime * MotionEngineConst.TimeDoubling_F;
                        mRealyHitInterval = actionTime / _intervalNumber;
                    }
                    else if ((_actionTime.Duration > 0))
                    {
                        mRealyHitInterval = _actionTime.Duration / _intervalNumber;
                    }
                    else
                    {
                        mRealyHitInterval = -0.2f;
                    }
                }
                else
                {
                    mRealyHitInterval = mAttackBoxInfo.HitInterval;
                }
            }
#if UNITY_EDITOR
            editorDraw--;
#endif
            if (mRealyHitInterval > 0)
            {
                mHitInterval += _actionTime.Deltatime;
                if (mHitInterval >= mRealyHitInterval)
                {
                    mHitInterval = 0;
                    _actionState.AllHitObject.Clear();
                    mAttackInfo.OnHitReStart(_actionState);
                }
            }
            if (!_actionState.IsTem)
            {

                int boxNumber = mAttackBoxInfo.Box.Length;
                if (boxNumber > 0)
                {
                    _eventStartTime = _actionTime.TriggerTime;
                    CheckTime(curTime, _actionState.ElapsedTime, _actionState);
                    curTime = _actionState.ElapsedTime;
                    return;
                }//有烘焙数据，去烘焙数据检验碰撞结果
            }

            ActionMachineTime machineTime = EngineResourcesManager.Instance.MachineTime;
            float effectiveLength = GetEffectiveLength(_actionState, machineTime);
            float anchorShiftZ = GetAnchorReferenceLength(_actionState, machineTime) * mAttackBoxInfo.AnchorZ;
            Vector3 _startPos = Vector3.zero;
            Quaternion _rot = Quaternion.identity;
            Vector3 _endPos = Vector3.zero;
            if (_actionState.IsTem)
            {
                Vector3 refPos = _actionState.Pos + _actionState.Rot * (mAttackBoxInfo.OffsetPos.GetValue());
                _rot = _actionState.Rot * Quaternion.Euler(mAttackBoxInfo.OffsetRot.GetValue());
                _startPos = refPos - _rot * Vector3.forward * anchorShiftZ;
                _endPos = _startPos + _rot * Vector3.forward * effectiveLength;
                CheckBox(_startPos, _endPos, _rot, _actionState);
            }
            else
            {
                if (_actionState.ActionStateMachine.TryGetCharacterLimb(
                        mAttackBoxInfo.ReferPoint,
                        out Transform _transform))
                {
                    Vector3 refPos = _transform.TransformPoint(mAttackBoxInfo.OffsetPos.GetValue());
                    _rot = _transform.rotation * Quaternion.Euler(mAttackBoxInfo.OffsetRot.GetValue());
                    _startPos = refPos - _rot * Vector3.forward * anchorShiftZ;
                    _endPos = _startPos + _rot * Vector3.forward * effectiveLength;
                    CheckBox(_startPos, _endPos, _rot, _actionState);
                    return;
                }

                Transform _unitTrans = _actionState.ActionStateMachine.CurUnit.transform;
                Vector3 refPosFallback = _unitTrans.TransformPoint(mAttackBoxInfo.OffsetPos.GetValue());
                _rot = _unitTrans.rotation * Quaternion.Euler(mAttackBoxInfo.OffsetRot.GetValue());
                _startPos = refPosFallback - _rot * Vector3.forward * anchorShiftZ;
                _endPos = _startPos + _rot * Vector3.forward * effectiveLength;
                CheckBox(_startPos, _endPos, _rot, _actionState);
            }
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            mAttackInfo.OnHitEnd(_actionState);

            if (!_interruot && mAttackBoxInfo.Box.Length > 0)
            {
                CheckTime(curTime, float.MaxValue, _actionState);
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_AttackBox _event = _eventData as Event_AttackBox;

#if UNITY_EDITOR
            if (mCheckHit is null) mCheckHit = new GraphEvent_NoValue_Bool(true);
            _event.AttackBoxInfo = mAttackBoxInfo.Clone();
            _event.AttackInfo = mAttackInfo.Clone();
#else
            _event.AttackBoxInfo = mAttackBoxInfo;
            _event.AttackInfo = mAttackInfo;
#endif
            _event.GValue_Setting_OnHit = mGValue_Setting_OnHit.Clone();
            _event.GValue_Setting_NotHit = mGValue_Setting_NotHit.Clone();
            _event.GValue_Setting = GValue_Setting.Clone();
            _event.CheckHit = mCheckHit.Clone();
            _event.HitLayer = mHitLayer;
            _event.ExcludeType = mExcludeType;
            _event.ExcludeMask = mExcludeMask;
            // _event.HitSpeed = mHitSpeed;
            // _event.Hitduration = mHitduration;

            return _event;
        }

        #region Funtion
        [NonSerialized] private int _lastHitPartIndex = 0;
        [NonSerialized] private int _eventStartTime = 0;
        private HashSet<ActionEngine_Unit> mExcludeUnits(ActionStatePart _part)
        {
            return _part.ExcludeUnits;
        }
        private void UpdateExclude(ActionStatePart _part)
        {
            ActionEngine_Unit _engineUnit = _part.ActionStateMachine.CurUnit;
            mExcludeUnits(_part).Clear();
            if ((ExcludeMask & (1 << 0)) != 0) mExcludeUnits(_part).Add(_engineUnit);
            if ((ExcludeMask & (1 << 1)) != 0) mExcludeUnits(_part).Add(_engineUnit.GetMaster);
            if ((ExcludeMask & (1 << 2)) != 0) mExcludeUnits(_part).Add(_engineUnit.GetSource);
        }
        private void CheckTime(float _startTime, float _endTime, ActionStatePart _actionState)
        {
            int boxNumber = mAttackBoxInfo.Box.Length;
            float effectiveLength = GetEffectiveLength(_actionState, EngineResourcesManager.Instance.MachineTime);

            bool _isFindBox = false;
            for (int i = _lastHitPartIndex; i < boxNumber; i++)
            {
                AttackBoxPart _boxPart = mAttackBoxInfo.Box[i];
                int _boxTriggerTime = _boxPart.TriggerTime + _eventStartTime;
                if (_boxTriggerTime >= _startTime)
                {
                    if (_boxTriggerTime > _endTime)
                    {
                        break;
                    }
                    Transform _transform = _actionState.ActionStateMachine.CurUnit.transform;
                    Vector3 _startPos = _transform.TransformPoint(_boxPart.StartPos.GetValue());
                    Vector3 _endPos =
                        _startPos + _transform.TransformDirection(_boxPart.Dir.GetValue()) * effectiveLength;
                    CheckBox(_startPos, _endPos, _transform.rotation, _actionState);
                    _lastHitPartIndex = i;
                    _isFindBox = true;
                }
            }

            if (!_isFindBox)
            {
                AttackBoxPart _boxPart = mAttackBoxInfo.Box[_lastHitPartIndex];
                Transform _transform = _actionState.ActionStateMachine.CurUnit.transform;
                Vector3 _startPos = _transform.TransformPoint(_boxPart.StartPos.GetValue());
                Vector3 _endPos =
                    _startPos + _transform.TransformDirection(_boxPart.Dir.GetValue()) * effectiveLength;
                CheckBox(_startPos, _endPos, _transform.rotation, _actionState, false);
            }
        }

        private bool CheckBox(Vector3 _startPos, Vector3 _endPos, Quaternion _rot, ActionStatePart _actionState, bool _isDraw = true)
        {

            bool _isHit = false;
            if (mAttackBoxInfo.AttackBoxType == 0)
            {
                float radius = GetEffectiveRadius(_actionState, EngineResourcesManager.Instance.MachineTime);
                GetAdjustedCapsuleEndpoints(_startPos, _endPos, radius, out Vector3 p0, out Vector3 p1);
                int _loop = Physics.OverlapCapsuleNonAlloc(p0, p1, radius, Colliders, layerMask, QueryTriggerInteraction.Collide);

#if UNITY_EDITOR
                if (!Application.isPlaying) return _loop > 0;
#endif
                for (int i = 0; i < _loop; i++)
                {
                    if (BeHitListAdd(Colliders[i].gameObject, _actionState)) _isHit = true;
                }

#if UNITY_EDITOR
                if (_isDraw && editorDraw > -1)
                    EngineDebug.DrawCapsule(p0, p1, radius,
                        _isHit ? Color.green : Color.red, 1);
#endif
            } //胶囊
            else if (mAttackBoxInfo.AttackBoxType == 1)
            {
                Vector3 dir = _endPos - _startPos;
                int _loop = Physics.RaycastNonAlloc(_startPos, dir, RaycastHits, dir.magnitude, layerMask, QueryTriggerInteraction.Collide);

#if UNITY_EDITOR
                if (!Application.isPlaying) return _loop > 0;
#endif
                for (int i = 0; i < _loop; i++)
                {
                    if (BeHitListAdd(RaycastHits[i].collider.gameObject, _actionState)) _isHit = true;
                }

#if UNITY_EDITOR
                if (_isDraw && editorDraw > -1)
                    EngineDebug.DrawLine(_startPos, _endPos, _isHit ? Color.green : Color.red, 1);
#endif
            } //射线
            else if (mAttackBoxInfo.AttackBoxType == 2)
            {
                Quaternion _lrot = _rot * Quaternion.Euler(mAttackBoxInfo.OffsetRot.GetValue());
                Vector3 scale = mAttackBoxInfo.Scale.GetValue();
                if (mAttackBoxInfo.UseBluePrint_Lenth && _actionState != null)
                    scale.z = mAttackBoxInfo.Length.value(_actionState, EngineResourcesManager.Instance.MachineTime);
                Vector3 boxCenter = GetBoxAnchoredCenter(_startPos, _lrot, scale.z);
                int _loop = Physics.OverlapBoxNonAlloc(boxCenter, scale * 0.5f, Colliders, _lrot, layerMask, QueryTriggerInteraction.Collide);

#if UNITY_EDITOR
                if (!Application.isPlaying) return _loop > 0;
#endif
                for (int i = 0; i < _loop; i++)
                {
                    if (BeHitListAdd(Colliders[i].gameObject, _actionState)) _isHit = true;
                }

#if UNITY_EDITOR
                if (_isDraw && editorDraw > -1)
                    EngineDebug.DrawBox(boxCenter, _lrot, scale,
                        _isHit ? Color.green : Color.red, 1);
#endif
            } //方块
            else if (mAttackBoxInfo.AttackBoxType == 3)
            {
                float radius = GetEffectiveRadius(_actionState, EngineResourcesManager.Instance.MachineTime);
                int _loop = Physics.OverlapSphereNonAlloc(_startPos, radius, Colliders, layerMask, QueryTriggerInteraction.Collide);

#if UNITY_EDITOR
                if (!Application.isPlaying) return _loop > 0;
#endif
                for (int i = 0; i < _loop; i++)
                {
                    if (BeHitListAdd(Colliders[i].gameObject, _actionState)) _isHit = true;
                }

#if UNITY_EDITOR
                if (_isDraw && editorDraw > -1)
                    EngineDebug.DrawSphere(_startPos, radius, _isHit ? Color.green : Color.red, 1);
#endif
            } //球

            return _isHit;
        }

        private bool BeHitListAdd(GameObject _obj, ActionStatePart _selfActionState)
        {
            if (!_selfActionState.AllHitObject.Contains(_obj))
            {
                ActionStateMachine _stateMachine = _selfActionState.ActionStateMachine;
                mGValue_Setting_OnHit.OnSet(_stateMachine);

                if (_obj.TryGetComponent(out TargetUnit _unit))
                {
                    if (!mExcludeUnits(_selfActionState).Contains(_unit.GetUnit()))
                    {
                        _stateMachine.HitUnit = _unit;
                        _unit.GetUnit().ActionStateMachine.AttackerUnit = _stateMachine.CurUnit;

                        _stateMachine.CurOnHitValid = CheckHit.value(_selfActionState, EngineResourcesManager.Instance.MachineTime);
                        _selfActionState.LoopIndex++;
                        //if (_stateMachine.CurOnHitValid)
                        //{
                        if (_selfActionState.AllHitObject.Count < 1 || _selfActionState.IsTem)
                        {
                            if (_selfActionState.IsTem)
                            {
                                _selfActionState.HitPoint = new PointData(_selfActionState.Pos, _selfActionState.Rot);
                                UnitOnHit(_selfActionState, mAttackInfo, _obj, _unit, _selfActionState);
                            }
                            else
                            {
                                UnitOnHit(_selfActionState, mAttackInfo, _obj, _unit);
                            }
                        }

                        if (_stateMachine.CurOnHitValid)
                        {
                            ActionEngine_Unit _attacker = _stateMachine.CurUnit;
                            _unit.GetUnit().ActionStateMachine.UnitBehit(mAttackInfo, _attacker, GValue_Setting);
                            if (_stateMachine.TryGetStaticLogic(out Ex_AttackBox _attackBox, nameof(Ex_AttackBox)))
                            {
                                _attackBox.ExtrudEvent(_selfActionState, true, _obj, _unit);
                            }
                        }
                        //}
                        //else
                        //{

                        //}
                    }
                }
                else
                {
                    if (_selfActionState.AllHitObject.Count < 1 || _selfActionState.IsTem)
                    {
                        if (_selfActionState.IsTem)
                        {
                            _selfActionState.HitPoint = new PointData(_selfActionState.Pos, _selfActionState.Rot);
                            UnitOnHit(_selfActionState, mAttackInfo, _obj, null, _selfActionState);
                        }
                        else
                        {
                            UnitOnHit(_selfActionState, mAttackInfo, _obj, null);
                        }
                    }

                    if (_stateMachine.TryGetStaticLogic(out Ex_AttackBox _attackBox, nameof(Ex_AttackBox)))
                    {
                        _attackBox.ExtrudEvent(_selfActionState, false, _obj, null);
                    }
                }

                _selfActionState.AllHitObject.Add(_obj);
                return true;
            }

            return false;
        }

        private void UnitOnHit(ActionStatePart _selfPart, IAttackInfo _attackInfo, GameObject _obj, TargetUnit _BeHitUnit, ActionStatePart _part = null)
        {
            _selfPart.ActionStateMachine.UnitOnHit(_attackInfo, _obj, _BeHitUnit, _part);
        }

        /// <summary>
        /// 将胶囊的配置端点（tip）转换为半球中心，保持 startPos 为近端 tip 位置不变。
        /// 总长度 = Scale.x，两半球中心内缩 radius，使 tip-to-tip = 原始 center-to-center 距离。
        /// </summary>
        private static void GetAdjustedCapsuleEndpoints(Vector3 startPos, Vector3 endPos, float radius,
            out Vector3 p0, out Vector3 p1)
        {
            Vector3 dir = endPos - startPos;
            float dist = dir.magnitude;
            if (dist < 0.0001f)
            {
                p0 = startPos;
                p1 = startPos;
                return;
            }
            Vector3 normalized = dir / dist;
            float clampedR = Mathf.Min(radius, dist * 0.5f);
            p0 = startPos + normalized * clampedR;
            p1 = startPos + normalized * (dist - clampedR);
        }

        /// <summary> 将 Box 锚点从体心移到 -Z 面中心，使长度修改只向 +Z 方向延伸 </summary>
        private static Vector3 GetBoxAnchoredCenter(Vector3 anchorPos, Quaternion boxRot, float forwardExtent)
        {
            return anchorPos + boxRot * Vector3.forward * (forwardExtent * 0.5f);
        }

        private float GetEffectiveLength(ActionStatePart actionState, ActionMachineTime time)
        {
            if (mAttackBoxInfo.UseBluePrint_Lenth && actionState != null)
                return mAttackBoxInfo.Length.value(actionState, time);
            return mAttackBoxInfo.Scale.x;
        }

        private float GetEffectiveRadius(ActionStatePart actionState, ActionMachineTime time)
        {
            if (mAttackBoxInfo.UseBluePrint_Radius && actionState != null)
                return mAttackBoxInfo.Radius.value(actionState, time);
            return mAttackBoxInfo.Scale.y;
        }

        /// <summary>
        /// 返回锚点百分比 Z 轴所对应的参考长度：
        /// 胶囊/射线 → effectiveLength, Box → depth(Scale.z), 球 → diameter(2×radius)
        /// </summary>
        private float GetAnchorReferenceLength(ActionStatePart actionState, ActionMachineTime time)
        {
            if (mAttackBoxInfo.AttackBoxType == 3)
                return 0f;
            if (mAttackBoxInfo.AttackBoxType == 2)
            {
                if (mAttackBoxInfo.UseBluePrint_Lenth && actionState != null)
                    return mAttackBoxInfo.Length.value(actionState, time);
                return mAttackBoxInfo.Scale.z;
            }
            return GetEffectiveLength(actionState, time);
        }
        #endregion

        #region DrawFuntion

#if UNITY_EDITOR
        [NonSerialized] private int editorDraw = 0;
        [NonSerialized] private float m_EffectiveLength;
        [NonSerialized] private float m_EffectiveRadius;
        [NonSerialized] private Vector3 m_EffectiveBoxScale;
        public void EditorDraw(CharacterConfig characterConfig, ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (!Application.isPlaying)
            {
                if (_actionState is null) layerMask = mHitLayer;
                else layerMask = _actionState.ActionStateMachine.GetLayer(mHitLayer);
            }

            editorDraw = 5;
            Update_EventAttackBox(_actionTime, characterConfig, _actionState);
        }

        [NonSerialized] private Vector3 pos;
        [NonSerialized] private Quaternion rot;
        private void Update_EventAttackBox(ActionMachineTime _actionTime, CharacterConfig characterConfig, ActionStatePart _actionState)
        {
            m_EffectiveLength = AttackBoxInfo.Scale.x;
            m_EffectiveRadius = AttackBoxInfo.Scale.y;
            m_EffectiveBoxScale = AttackBoxInfo.Scale.GetValue();
            if (_actionState != null)
            {
                if (AttackBoxInfo.UseBluePrint_Lenth)
                {
                    float bpLength = AttackBoxInfo.Length.value(_actionState, _actionTime);
                    m_EffectiveLength = bpLength;
                    if (AttackBoxInfo.AttackBoxType == 2) m_EffectiveBoxScale.z = bpLength;
                }
                if (AttackBoxInfo.UseBluePrint_Radius)
                    m_EffectiveRadius = AttackBoxInfo.Radius.value(_actionState, _actionTime);
            }

            int _BoxDrawType = AttackBoxInfo.Box.Length > 0 ? BoxDrawType : 2;
            if (_BoxDrawType == 2 || _BoxDrawType == 0)
            {
                if (characterConfig is null)
                {
                    pos = _actionState.Pos;
                    rot = _actionState.Rot;
                }
                else
                {
                    if (!characterConfig.HelpPointDic.TryGetValue(
                            AttackBoxInfo.ReferPoint,
                            out Transform _target) ||
                        _target == null)
                    {
                        _target = characterConfig.transform;
                    }
                    pos = _target.position;
                    rot = _target.rotation;
                }
                DrawDefaultBox(AttackBoxInfo, pos, rot, _actionTime, Color.green, _actionState);
            }

            if (_BoxDrawType == 2 || _BoxDrawType == 1)
            {
                Transform _transform = null;
                if (characterConfig is null) _transform = _actionState.ActionStateMachine.CurUnit.transform;
                else _transform = characterConfig.transform;
                DrawBekaBox(AttackBoxInfo, _actionTime, _transform, Color.blue);
            }
        }

        private void DrawDefaultBox(AttackBoxInfo attackBoxInfo, Vector3 pos, Quaternion rot, ActionMachineTime _actionTime,
            Color color, ActionStatePart _actionState)
        {
            float anchorRefLen;
            if (attackBoxInfo.AttackBoxType == 3) anchorRefLen = 0f;
            else if (attackBoxInfo.AttackBoxType == 2) anchorRefLen = m_EffectiveBoxScale.z;
            else anchorRefLen = m_EffectiveLength;

            Vector3 refPos = pos + rot * attackBoxInfo.OffsetPos.GetValue();
            Quaternion _dir = (rot * Quaternion.Euler(attackBoxInfo.OffsetRot.GetValue()));
            Vector3 _startPos = refPos - _dir * Vector3.forward * (anchorRefLen * attackBoxInfo.AnchorZ);
            Vector3 _endPos = _startPos + (_dir * Vector3.forward * m_EffectiveLength);

            bool _isDraw = _actionTime.CurrentTime >= _actionTime.TriggerTime;
            if (_isDraw && _actionTime.Duration > -1) _isDraw = _actionTime.CurrentTime <= _actionTime.TriggerTime + _actionTime.Duration;
            if (_isDraw)
            {
                bool isHit = false;
                if (!Application.isPlaying)
                {
                    isHit = CheckBox(_startPos, _endPos, rot, _actionState, false);
                }
                DrawBox(_startPos, _endPos, attackBoxInfo, isHit ? Color.red : color, rot);
            }
        }

        private void DrawBekaBox(AttackBoxInfo attackBoxInfo, ActionMachineTime _actionTime, Transform _target, Color color)
        {
            int _curTrigerTime = (int)(_actionTime.CurrentTime - _actionTime.TriggerTime);

            if (_curTrigerTime >= 0)
            {
                for (int i = 0; i < attackBoxInfo.Box.Length; i++)
                {
                    AttackBoxPart _boxPart = attackBoxInfo.Box[i];
                    bool _isDraw = _curTrigerTime >= _boxPart.TriggerTime;
                    if (_isDraw)
                    {
                        int _life = BakerBoxLife;
                        if (i == attackBoxInfo.Box.Length - 1)
                        {
                            _isDraw = _curTrigerTime <= _boxPart.TriggerTime + _life;
                        }//末端
                        else
                        {
                            _isDraw = _curTrigerTime < attackBoxInfo.Box[i + 1].TriggerTime + _life;
                        }//常规

                        if (_isDraw)
                        {
                            Vector3 _startPos = _target.TransformPoint(_boxPart.StartPos.GetValue());
                            Vector3 _endPos = _startPos + _target.TransformDirection(_boxPart.Dir.GetValue())
                                * m_EffectiveLength;
                            DrawBox(_startPos, _endPos, attackBoxInfo, color, _target.rotation);
                        }
                    }
                }
            }
        }

        private void DrawBox(Vector3 _startPos, Vector3 _endPos, AttackBoxInfo attackBoxInfo, Color color, Quaternion _rot)
        {
            if (attackBoxInfo.AttackBoxType == 0)
            {
                GetAdjustedCapsuleEndpoints(_startPos, _endPos, m_EffectiveRadius, out Vector3 p0, out Vector3 p1);
                EngineScenceDraw.Capsule(p0, p1, m_EffectiveRadius, color);
            } //胶囊
            else if (attackBoxInfo.AttackBoxType == 1)
            {
                EngineScenceDraw.Line(_startPos, _endPos, color);
            } //射线
            else if (attackBoxInfo.AttackBoxType == 2)
            {
                Quaternion lrot = _rot * Quaternion.Euler(attackBoxInfo.OffsetRot.GetValue());
                Vector3 boxCenter = GetBoxAnchoredCenter(_startPos, lrot, m_EffectiveBoxScale.z);
                EngineScenceDraw.Box(boxCenter, lrot, m_EffectiveBoxScale, color);
            } //方块
            else if (attackBoxInfo.AttackBoxType == 3)
            {
                EngineScenceDraw.Sphere(_startPos, _rot * Quaternion.Euler(attackBoxInfo.OffsetRot.GetValue()), m_EffectiveRadius, color);
            } //球
        }


        private int BoxDrawType => PlayerPrefs.GetInt("BoxDrawType", 0);

        private int BakerBoxLife => PlayerPrefs.GetInt("BakerBoxLife", 0);

#endif
        #endregion
    }
}