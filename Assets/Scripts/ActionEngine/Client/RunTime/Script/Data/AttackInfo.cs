using System;
using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class AttackInfo : IAttackInfo
    {
        public enum EReferTarget
        {
            LookBeHit,
            Attaker,
            BeHit
        }

        //public static readonly string[] JankEnumNames = new[] 
        //{
        //    ""
        //}; 

        [SerializeField] protected GraphEvent_NoValue_Bool mCheckJank = new GraphEvent_NoValue_Bool(true);
        [SerializeField] protected GFloat mGHitSpeed = new GFloat(1.0f, false);
        [SerializeField] protected GFloat mGHitduration = new GFloat(0.0f, false);
        [SerializeField] protected byte mJankType = 0;
        [SerializeField] protected bool mOnlyFirst = true;

        [NonSerialized] private bool mHitJankValid = true;//是否允许判断命中顿帧

        #region Property
        [EditorProperty("顿帧有效判断: ", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Bool CheckJank
        {
            get
            {
                if (mCheckJank is null) mCheckJank = new GraphEvent_NoValue_Bool(true);
                return mCheckJank;
            }
            set { mCheckJank = value; }
        }
        [EditorProperty("顿帧时间倍率: ", EditorPropertyType.EEPT_GFloat)]
        public GFloat HitSpeed
        {
            get
            {
                if (mGHitSpeed is null) mGHitSpeed = new GFloat(1.0f, false);
                return mGHitSpeed;
            }
            set { mGHitSpeed = value; }
        }

        [EditorProperty("顿帧持续时间(s): ", EditorPropertyType.EEPT_GFloat)]
        public GFloat Hitduration
        {
            get
            {
                if (mGHitduration is null) mGHitduration = new GFloat(0.0f, false);
                return mGHitduration;
            }
            set { mGHitduration = value; }
        }
        [EditorProperty("顿帧对象(s): ", EditorPropertyType.EEPT_Enum, EnumNames = new string[]
        { "源和自身和命中单位", "自身和命中单位", "仅自身" , "仅命中单位", "仅源和自身", })]
        public byte JankType
        {
            get { return mJankType; }
            set { mJankType = value; }
        }
        [EditorProperty("每次攻击事件仅触发一次顿帧", EditorPropertyType.EEPT_Bool, LabelWidth = 160)]
        public bool OnlyFirst
        {
            get { return mOnlyFirst; }
            set { mOnlyFirst = value; }
        }
        //击退、击飞相关
        #endregion
        public IAttackInfo Clone()
        {

#if UNITY_EDITOR
            AttackInfo _attackInfo = new AttackInfo();
            _attackInfo.CheckJank = CheckJank.Clone();
            _attackInfo.HitSpeed = (GFloat)HitSpeed.Clone();
            _attackInfo.Hitduration = (GFloat)Hitduration.Clone();
            _attackInfo.JankType = mJankType;
            _attackInfo.OnlyFirst = mOnlyFirst;
            return _attackInfo;
#endif
            return this;
        }

        //受击参数，受击者的行为状态机，攻击者，受击点
        public void BeHit(IAttackInfo _IattackInfo, ActionStateMachine _stateMachine, ActionEngine_Unit _attacker)
        {
            AttackInfo _attackInfo = (AttackInfo)_IattackInfo;

            //if (_stateMachine.TryGetLogic(out Ex_Update_CharacterControl _characterControl, nameof(Ex_Update_CharacterControl)))
            //{

            //}
            // EngineDebug.Log
            // (
            //     $"受击对象: {_stateMachine.CurUnit.name}" + "\n" +
            //     $"攻击来源: {_attacker.name}" + "\n" +
            //     $"接受受击(HitSpeed): {_attackInfo.HitSpeed}" + "\n" +
            //     $"接受受击(mHitduration): {_attackInfo.mHitduration}" + "\n" +
            //     ""
            // );

            //受击顿帧
            //_stateMachine.SetSpeed(_attackInfo.HitSpeed.GetValue(),_attackInfo.mHitduration);
        }

        //命中参数，攻击者的行为状态机，受击者
        public void OnHit(IAttackInfo _IattackInfo, ActionStateMachine _stateMachine, TargetUnit _hiter)
        {
            AttackInfo _attackInfo = (AttackInfo)_IattackInfo;
            // EngineDebug.Log
            // (
            //     $"受击对象: {_stateMachine.CurUnit.name}" + "\n" +
            //     $"攻击来源: {_attacker.name}" + "\n" +
            //     $"接受受击(HitSpeed): {_attackInfo.HitSpeed}" + "\n" +
            //     $"接受受击(mHitduration): {_attackInfo.mHitduration}" + "\n" +
            //     ""
            // );

            if (mHitJankValid)
            {
                if (CheckJank.value(_stateMachine.FirstStatePart, EngineResourcesManager.Instance.MachineTime))
                {
                    //将数值用于顿帧
                    ActionStatePart _attakerPart = _stateMachine.FirstStatePart;
                    // bool _checkValid = CheckJank.value(_attakerPart, EngineResourcesManager.Instance.MachineTime);
                    //Debug.Log($"获取的值: [{_attackInfo.HitSpeed.mType}]");
                    float _hitSpeed = _attackInfo.HitSpeed.GetValue(_attakerPart);
                    float _hitDuration = _attackInfo.Hitduration.GetValue(_attakerPart);

                    if (JankType == 0)
                    {
                        _stateMachine.CurUnit.GetSource.ActionStateMachine.SetSpeed(_hitSpeed, _hitDuration);//源
                        _stateMachine.SetSpeed(_hitSpeed, _hitDuration);//自身
                        _hiter?.GetUnit().ActionStateMachine.SetSpeed(_hitSpeed, _hitDuration);//命中对象
                    }
                    else if (JankType == 1)
                    {
                        _stateMachine.SetSpeed(_hitSpeed, _hitDuration);//自身
                        _hiter?.GetUnit().ActionStateMachine.SetSpeed(_hitSpeed, _hitDuration);//命中对象
                    }
                    else if (JankType == 2)
                    {
                        _stateMachine.SetSpeed(_hitSpeed, _hitDuration);//自身
                    }
                    else if (JankType == 3)
                    {
                        _hiter?.GetUnit().ActionStateMachine.SetSpeed(_hitSpeed, _hitDuration);//命中对象
                    }
                    else if (JankType == 4)
                    {
                        _stateMachine.CurUnit.GetSource.ActionStateMachine.SetSpeed(_hitSpeed, _hitDuration);//源
                        _stateMachine.SetSpeed(_hitSpeed, _hitDuration);//自身
                    }
                    if (mOnlyFirst) { mHitJankValid = false; }
                }
            }
        }

        #region 攻击盒逻辑
        public void OnHitReStart(ActionStatePart _statePart)
        {
            mHitJankValid = true;
        }
        public void OnHitStart(ActionStatePart _statePart)
        {
            mHitJankValid = true;
        }

        public void OnHitUpdate(ActionStatePart _statePart, Vector3 _point, Quaternion _rotate)
        {

        }

        public void OnHitEnd(ActionStatePart _statePart)
        {

        }
        #endregion
    }
}