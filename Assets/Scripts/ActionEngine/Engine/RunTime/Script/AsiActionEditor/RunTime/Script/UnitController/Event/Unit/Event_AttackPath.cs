using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime.Event_Extend;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    //所有子弹共用事件

    [System.Serializable]
    public class Event_AttackPath : IActionEventData
    {
        [SerializeField] private AttackPathInfo mAttackPathInfo = new AttackPathInfo();
        [SerializeField] private GValue_Setting mGValue_Setting_NotHit = new GValue_Setting();
        [SerializeField] private GValue_Setting mGValue_Setting_OnHit = new GValue_Setting();
        [SerializeField] private GValue_Setting mGValue_Setting = new GValue_Setting();
        [SerializeField] private GValue_Ratio mGValueRatio = new GValue_Ratio();
        [SerializeField] private byte mExcludeType = 1;
        [SerializeField] private int mHitLayer;
        [SerializeReference] private IAttackInfo mAttackInfo;

        [NonSerialized] private HashSet<ActionEngine_Unit> mExcludeUnits = new HashSet<ActionEngine_Unit>(3);
        [NonSerialized] private ActionStatePart _actionStatePart;
        [NonSerialized] private float mHitInterval;
        #region property
        public AttackPathInfo AttackBoxInfo
        {
            get { return mAttackPathInfo; }
            set { mAttackPathInfo = value; }
        }

        public IAttackInfo AttackInfo
        {
            get { return mAttackInfo; }
            set { mAttackInfo = value; }
        }
        [EditorProperty("排除", EditorPropertyType.EEPT_LayerMask, EnumNames = new[]
            {"不做任何排除", "排除自身和源和持有者", "仅排除自身", "仅排除源", "仅排除持有者", "仅排除源和持有者"})]
        public byte ExcludeType
        {
            get { return mExcludeType; }
            set { mExcludeType = value; }
        }
        [EditorProperty("检测层级", EditorPropertyType.EEPT_Enum)]
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
        [EditorProperty("有效命中判定", EditorPropertyType.EEPT_GValueSRatio, LabelWidth = 0)]
        public GValue_Ratio GValueRatio
        {
            get { return mGValueRatio; }
            set { mGValueRatio = value; }
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


        public int GetEvenType() => -(int)EEvenTypeInternal.EET_AttackPath;
        public IActionEventData Creact() => new Event_AttackPath();

        private RaycastHit[] mHits => EngineResourcesManager.Instance.RaycastHits;
        private List<GameObject> beHitTarget => _actionStatePart.AllHitObject;//已经击中的对象
        private Vector3[] mPosList => _actionStatePart.PosList;
        private Quaternion[] mRotList => _actionStatePart.RotList;
        [NonSerialized] private Vector3 mLastPos, mNewPos;
        [NonSerialized] private Quaternion mLastRot, mNewRot;
        [NonSerialized] private LayerMask layerMask;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            _stateMachine.HitUnit = null;
            _stateMachine.OnHitObject = null;

            ActionEngine_Unit _engineUnit = _stateMachine.CurUnit;
            mExcludeUnits.Clear();
            if (ExcludeType == 1)
            {
                mExcludeUnits.Add(_engineUnit);
                mExcludeUnits.Add(_engineUnit.GetMaster);
                mExcludeUnits.Add(_engineUnit.GetSource);
            }
            else if (ExcludeType == 2)
            {
                mExcludeUnits.Add(_engineUnit.GetSource);
            }
            else if (ExcludeType == 3)
            {
                mExcludeUnits.Add(_engineUnit.GetMaster);
            }
            else if (ExcludeType == 4)
            {
                mExcludeUnits.Add(_engineUnit.GetMaster);
                mExcludeUnits.Add(_engineUnit.GetSource);
            }

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

            mGValue_Setting_NotHit.OnSet(_stateMachine);
            mAttackInfo.OnHitStart(_actionState);

            _actionStatePart = _actionState;
            beHitTarget.Clear();
            for (int i = 0; i < mPosList.Length; i++)
            {
                mPosList[i] = _actionState.Pos;
                mRotList[i] = _actionState.Rot;
            }
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            _actionStatePart = _actionState;

            mNewPos = _actionState.Pos;
            mNewRot = _actionState.Rot;

            mLastPos = mPosList[0];
            mLastRot = mRotList[0];

            for (int i = 0; i < mPosList.Length - 1; i++)
            {
                mPosList[i] = mPosList[i + 1];
                mRotList[i] = mRotList[i + 1];
            }
            mPosList[mPosList.Length - 1] = mNewPos;
            mRotList[mPosList.Length - 1] = mNewRot;
            if (mAttackPathInfo.HitInterval > 0)
            {
                mHitInterval += _actionTime.Deltatime;
                if (mHitInterval >= mAttackPathInfo.HitInterval)
                {
                    mHitInterval = 0;
                    _actionState.AllHitObject.Clear();
                    mAttackInfo.OnHitReStart(_actionState);
                }
            }

            foreach (AttackPath_Point v in mAttackPathInfo.Point)
                CheckPoint(mLastPos, mNewPos, mLastRot, mNewRot, v);
            foreach (AttackPath_Sphere v in mAttackPathInfo.Sphere)
                CheckSphere(mLastPos, mNewPos, mLastRot, mNewRot, v);
            foreach (AttackPath_Capsule v in mAttackPathInfo.Capsule)
                CheckCapsule(mLastPos, mNewPos, mLastRot, mNewRot, v);
            foreach (AttackPath_Box v in mAttackPathInfo.Box)
                CheckBox(mLastPos, mNewPos, mLastRot, mNewRot, v);
        }
        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            mAttackInfo.OnHitEnd(_actionState);
        }


        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_AttackPath _event = _eventData as Event_AttackPath;

#if UNITY_EDITOR
            _event.mAttackPathInfo = mAttackPathInfo.Clone();
            _event.AttackInfo = mAttackInfo.Clone();
#else
            _event.mAttackPathInfo = mAttackPathInfo;
            _event.AttackInfo = mAttackInfo;
#endif
            _event.GValue_Setting_OnHit = mGValue_Setting_OnHit.Clone();
            _event.GValue_Setting_NotHit = mGValue_Setting_NotHit.Clone();
            _event.GValue_Setting = GValue_Setting.Clone();
            _event.GValueRatio = mGValueRatio.Clone();
            _event.HitLayer = mHitLayer;
            _event.ExcludeType = mExcludeType;
            return _event;
        }

        #region Funtion
        private void CheckPoint(Vector3 _old_p, Vector3 _new_p, Quaternion _old_r, Quaternion _new_r, AttackPath_Point _value)
        {
            Vector3 _oldPos = _old_p + _old_r * _value.StartPos.GetValue();
            Vector3 _newPos = _new_p + _new_r * _value.StartPos.GetValue();

            Vector3 _offset = _newPos - _oldPos;
            int checkTargets = Physics.RaycastNonAlloc(_oldPos, _offset, mHits, _offset.magnitude, layerMask);
            CheckHit(checkTargets, mHits);

        }
        private void CheckSphere(Vector3 _old_p, Vector3 _new_p, Quaternion _old_r, Quaternion _new_r, AttackPath_Sphere _value)
        {
            Vector3 _oldPos = _old_p + _old_r * _value.StartPos.GetValue();
            Vector3 _newPos = _new_p + _new_r * _value.StartPos.GetValue();

            Vector3 _offset = _newPos - _oldPos;
            int checkTargets = Physics.SphereCastNonAlloc(_oldPos, _value.Radius, _offset, mHits, _offset.magnitude, layerMask);
            CheckHit(checkTargets, mHits);
        }
        private void CheckCapsule(Vector3 _old_p, Vector3 _new_p, Quaternion _old_r, Quaternion _new_r, AttackPath_Capsule _value)
        {
            Vector3 _oldPos = _old_p + _old_r * _value.StartPos.GetValue();
            Vector3 _newPos = _new_p + _new_r * _value.StartPos.GetValue();
            Vector3 _oldPos2 = _old_p + _old_r * _value.EndPos.GetValue();
            Vector3 _newPos2 = _new_p + _new_r * _value.EndPos.GetValue();

            Vector3 _offset = _newPos - _oldPos;
            //Vector3 _offset2 = _newPos2 - _oldPos2;

            int checkTargets = Physics.CapsuleCastNonAlloc(_oldPos, _oldPos2, _value.Radius, _offset, mHits, _offset.magnitude, layerMask);
            CheckHit(checkTargets, mHits);
        }
        private void CheckBox(Vector3 _old_p, Vector3 _new_p, Quaternion _old_r, Quaternion _new_r, AttackPath_Box _value)
        {

        }

        private void CheckHit(int _number, RaycastHit[] _hits)
        {
            for (int i = 0; i < _number; i++)
            {
                RaycastHit _nowHit = _hits[i];

                GameObject _obj = _nowHit.collider.gameObject;

                if (BeHitListAdd(_obj, _nowHit))
                {
                    //EngineDebug.LogWarning($"命中对象: [<color=#ffcc00>{_obj.name}</color>]");
                    //Debug.DrawLine(_nowHit.point, _actionStatePart.Pos, Color.red, 20);
                }
            }
        }

        private bool BeHitListAdd(GameObject _obj, RaycastHit _hit)
        {
            if (!beHitTarget.Contains(_obj))
            {
                //EngineDebug.LogError("命中对象了！！: " + _obj.name);
                ActionStateMachine _stateMachine = _actionStatePart.ActionStateMachine;
                _actionStatePart.HitPoint = new PointData(_hit.point, Quaternion.LookRotation(_hit.normal));

                mGValue_Setting_OnHit.OnSet(_actionStatePart.ActionStateMachine);

                if (_obj.TryGetComponent(out TargetUnit _unit))
                {
                    //如果命中对象携带Unit组件的话判定为单位
                    if (mGValueRatio.CheckValue(_unit.GetUnit().ActionStateMachine, _actionStatePart.ActionStateMachine))
                    {
                        _stateMachine.UnitOnHit(mAttackInfo, _obj, _unit, _actionStatePart);
                        _unit.GetUnit().ActionStateMachine.UnitBehit(mAttackInfo, _stateMachine.CurUnit, GValue_Setting);
                        if (_stateMachine.TryGetStaticLogic(out Ex_AttackBox _attackBox, nameof(Ex_AttackBox)))
                        {
                            _attackBox.ExtrudEvent(_actionStatePart, true, _obj, _unit);
                        }
                    }
                }
                else
                {
                    _stateMachine.UnitOnHit(mAttackInfo, _obj, null, _actionStatePart);
                    if (_stateMachine.TryGetStaticLogic(out Ex_AttackBox _attackBox, nameof(Ex_AttackBox)))
                    {
                        _attackBox.ExtrudEvent(_actionStatePart, false, _obj, null);
                    }
                }
                beHitTarget.Add(_obj);
                return true;
            }

            return false;
        }
        #endregion
    }
}