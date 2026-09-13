using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_CreateSkill : IActionEventData
    {
        [SerializeField] protected bool mExitDestory;
        //[SerializeField] protected bool mIsAttackHit;
        [SerializeField] protected bool mIsUsGValue = false;
        [SerializeField] protected bool mCreateMul = false;
        [SerializeField] protected int mSkillID;
        [SerializeField] protected int mCharacterPoint;
        [SerializeField] protected GraphEvent_NoValue_Int mGSkillID = new GraphEvent_NoValue_Int();
        [SerializeField] protected GraphEvent_NoValue_GroupInt mGGroupSkillID = new GraphEvent_NoValue_GroupInt();
        [SerializeField] protected EVector3 mOffsetPos;
        [SerializeField] protected EVector3 mOffsetRot;
        [SerializeField] protected bool mInheritanceAttaker = false;
        [SerializeField] protected bool mInheritanceHiter = false;
        [SerializeField] protected bool mUseBlueMasterTarget = false;
        [SerializeField] protected GraphEvent_NoValue_Unit mMasterUnit = new GraphEvent_NoValue_Unit();
        [SerializeField] protected byte mSkillMaster = 0;
        [SerializeField] protected GEnum mSkillType = new GEnum();
        [SerializeField] protected int mPreviewActionIndex = 0;
        #region property
        [EditorProperty("用蓝图创建技能", EditorPropertyType.EEPT_Bool)]
        public bool IsUsGValue
        {
            get { return mIsUsGValue; }
            set { mIsUsGValue = value; }
        }
        [EditorProperty("创建多个技能", EditorPropertyType.EEPT_Bool)]
        public bool CreateMul
        {
            get { return mCreateMul; }
            set { mCreateMul = value; }
        }
        [EditorProperty("创建技能", EditorPropertyType.EEPT_Skill)]
        public int SkillID
        {
            get { return mSkillID; }
            set { mSkillID = value; }
        }
        [EditorProperty("创建技能", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Int GSkillID
        {
            get
            {
                if (mGSkillID is null) mGSkillID = new GraphEvent_NoValue_Int();
                return mGSkillID;
            }
            set { mGSkillID = value; }
        }
        [EditorProperty("创建复数技能", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_GroupInt GGroupSkillID
        {
            get
            {
                if (mGGroupSkillID is null) mGGroupSkillID = new GraphEvent_NoValue_GroupInt();
                return mGGroupSkillID;
            }
            set { mGGroupSkillID = value; }
        }
        [EditorProperty("挂点", EditorPropertyType.EEPT_CharacteLimbType)]
        public int CharacterPoint
        {
            get { return mCharacterPoint; }
            set { mCharacterPoint = value; }
        }
        [EditorProperty("偏移位置", EditorPropertyType.EEPT_Vector3)]
        public EVector3 OffsetPos
        {
            get { return mOffsetPos; }
            set { mOffsetPos = value; }
        }
        [EditorProperty("偏移角度", EditorPropertyType.EEPT_Vector3)]
        public EVector3 OffsetRot
        {
            get { return mOffsetRot; }
            set { mOffsetRot = value; }
        }
        [EditorProperty("技能继承当前攻击者", EditorPropertyType.EEPT_Bool, LabelWidth = 120)]
        public bool InheritanceAttaker
        {
            get { return mInheritanceAttaker; }
            set { mInheritanceAttaker = value; }
        }
        [EditorProperty("技能继承当前命中者", EditorPropertyType.EEPT_Bool, LabelWidth = 120)]
        public bool InheritanceHiter
        {
            get { return mInheritanceHiter; }
            set { mInheritanceHiter = value; }
        }
        [EditorProperty("使用蓝图设定技能父级和源", EditorPropertyType.EEPT_Bool, LabelWidth = 150)]
        public bool UseBlueMasterTarget
        {
            get { return mUseBlueMasterTarget; }
            set { mUseBlueMasterTarget = value; }
        }
        [EditorProperty("设定技能父级和源", EditorPropertyType.EEPT_GraphValue, LabelWidth = 150)]
        public GraphEvent_NoValue_Unit MasterUnit
        {
            get { 
                if(mMasterUnit is null) mMasterUnit = new GraphEvent_NoValue_Unit();
                return mMasterUnit; 
            }
            set { mMasterUnit = value; }
        }
        [EditorProperty("设定技能父级和源", EditorPropertyType.EEPT_Enum, Tooltip = "如果源不是角色单位则会继续向上级找，直到找到源角色单位",
            EnumNames = new[] { "自身", "自身父级", "自身的源", "自身的命中者", "自身的攻击者" }, LabelWidth = 120)]
        public byte SkillMaster
        {
            get { return mSkillMaster; }
            set { mSkillMaster = value; }
        }
        //[EditorProperty("技能类型", EditorPropertyType.EEPT_Enum)]
        //public GEnum SkillType
        //{
        //    get
        //    {
        //        if (mSkillType is null) mSkillType = new GEnum();
        //        return mSkillType;
        //    }
        //    set { mSkillType = value; }
        //}
        [EditorProperty("离开时销毁", EditorPropertyType.EEPT_Bool)]
        public bool ExitDestory
        {
            get { return mExitDestory; }
            set { mExitDestory = value; }
        }
        public int PreviewActionIndex
        {
            get => mPreviewActionIndex;
            set => mPreviewActionIndex = value;
        }

        #endregion
        public int GetEvenType() => -(int)EEvenTypeInternal.EET_CreateSkill;
        [NonSerialized] public ActionStatePart mactionState = null;
        public IActionEventData Creact() => new Event_CreateSkill();

        [NonSerialized] private ActionEngine_Unit mAttaker;
        [NonSerialized] private TargetUnit mHiter;
        [NonSerialized] private Transform mSkill;
        [NonSerialized] private ActionEngine_Skill mAESkill;
        [NonSerialized] private bool mIsDestory = false;
        [NonSerialized] private Vector3 mSetPos = Vector3.zero;
        [NonSerialized] private Quaternion mSetRot = Quaternion.identity;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            mIsDestory = false;
            //Debug.LogWarning($"创建技能 [{(_actionState.IsTem ? "由技能创建" : "由角色创建")}]");
            mactionState = _actionState;
            //if (mIsAttackHit)
            //{
            //    _actionState.OnHit += AttackCallback;
            //    return;
            //}
            //Debug.LogWarning($"执行技能Update事件[{GetHashCode()}]");
            mSkill = null;
            mAESkill = null;

            mAttaker = _actionState.ActionStateMachine.AttackerUnit;
            mHiter = _actionState.ActionStateMachine.HitUnit;

            if(CreateMul && IsUsGValue)
            {
                List<int> skillIDs = GGroupSkillID.value(_actionState, new ActionMachineTime(0, 0, 0, 0));
                foreach (int skillID in skillIDs) {
                    CreateSkill(skillID, _actionState);
                }
            }
            else
            {
                int skillID = mSkillID;
                if (IsUsGValue)
                {
                    skillID = GSkillID.value(_actionState, new ActionMachineTime(0, 0, 0, 0));
                    //EngineDebug.LogError("创建技能: " + skillID);
                }
                CreateSkill(skillID, _actionState);
            }
        }

        public void LateUpdate(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            //Debug.LogError($"执行技能Update事件[{GetHashCode()}]");
            if (mSkill == null) return;//!_actionState.IsTem
            SetSkillPos(_actionState);
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)//interruot 是否因打断轨退出  false代表事件自然结束
        {
            if (CreateMul && IsUsGValue) return;
            mIsDestory = true;
            //Debug.LogWarning("销毁技能");

            if (mExitDestory)
            {
                //Debug.Log("销毁");
                if (mAESkill is not null) EngineResourcesManager.Instance.DestoryUnit(mAESkill);
                mAESkill = null;
                mSkill = null;
            }
        }

        private void CreateSkill(int skillID, ActionStatePart _actionState)
        {
            EngineResourcesManager.Instance.LoadSkill(skillID, _skill =>
            {
                TargetUnit targetUnit = null;

                if (mIsDestory)
                {
                    EngineResourcesManager.Instance.DestoryUnit(_skill);
                    return;
                }
                else
                {
                    targetUnit = GetTargetUnit(_actionState, _skill);
                    if (targetUnit is null)
                    {
                        EngineDebug.LogError($"技能创建时,持有者空了!!\n{EngineDebug.DebugActionStatePart(mactionState)}");
                        EngineResourcesManager.Instance.DestoryUnit(_skill);
                        return;
                    }
                }

                if (mInheritanceAttaker)
                    _skill.ActionStateMachine.AttackerUnit = mAttaker;
                if (mInheritanceHiter)
                    _skill.ActionStateMachine.HitUnit = mHiter;
                mAESkill = _skill;
                mSkill = _skill.transform;
                //_skill.mSkillType = SkillType.mSerValue;
                targetUnit.GetUnit().ActionStateMachine.TryAddSkill(_skill);

                if (_actionState.IsTem)
                {
                    mSetPos = _actionState.Pos;
                    mSetRot = _actionState.Rot;
                }
                else
                {
                    if (_actionState.ActionStateMachine.TryGetCharacterLimb(
                            (ECharacteLimbType)mCharacterPoint,
                            out Transform _trans))
                    {
                        mSetPos = _trans.position;
                        mSetRot = _trans.rotation;
                    }
                }
                mSetPos = mSetPos + mSetRot * OffsetPos.GetValue();
                mSetRot = mSetRot * Quaternion.Euler(OffsetRot.GetValue());

                _skill.SetMaster(targetUnit.GetUnit(), targetUnit.GetUnit().GetSource);

                mSkill.SetPositionAndRotation(mSetPos, mSetRot);
                _actionState.SetPos(mSetPos);
                _actionState.SetRot(mSetRot);

                _skill.ActionStateMachine.InitState(_skill.SkillWarp.ActionStateInfo.mStartActionNames, _actionState, mSetPos, mSetRot);
            });
        }
        private void SetSkillPos(ActionStatePart _actionState)
        {
            if (!_actionState.IsTem)
            {
                if (_actionState.ActionStateMachine.TryGetCharacterLimb(
                        (ECharacteLimbType)mCharacterPoint,
                        out Transform _trans))
                {
                    Vector3 localPos = _trans.TransformPoint(mOffsetPos.GetValue());
                    Quaternion localRot = _trans.rotation * Quaternion.Euler(OffsetRot.GetValue());
#if UNITY_EDITOR
                    if (mSkill == null)
                    {
                        EngineDebug.LogError("重要报错， 在创建技能时技能丢失引用");
                        return;
                    }
#endif
                    mSkill.SetPositionAndRotation(localPos, localRot);
                    _actionState.SetPos(localPos);
                    _actionState.SetRot(localRot);
                }
            }
            else
            {
                Vector3 localPos = _actionState.Pos + _actionState.Rot * OffsetPos.GetValue();
                Quaternion localRot = _actionState.Rot * Quaternion.Euler(OffsetRot.GetValue());
                mSkill.SetPositionAndRotation(localPos, localRot);
                _actionState.SetPos(localPos);
                _actionState.SetRot(localRot);
            }
        }
        //private void AttackCallback(ActionEngine_Unit _SelfUnit, bool _isUnit, GameObject _gameObject, ActionEngine_Unit _onHiter)
        //{
        //    EngineResourcesManager.Instance.LoadSkill(mSkillID, _skill =>
        //    {
        //        mSkill = _skill.transform;
        //        if (mactionState.IsTem)
        //        {
        //            _skill.SetMaster(mactionState.ActionStateMachine.CurUnit, mactionState.ActionStateMachine.CurUnit.GetSource);

        //            _skill.ActionStateMachine.InitState(_skill.SkillWarp.ActionStateInfo.mStartActionNames, mactionState, mactionState.Pos, mactionState.Rot);
        //        }
        //        else
        //        {
        //            _skill.SetMaster(mactionState.ActionStateMachine.CurUnit, mactionState.ActionStateMachine.CurUnit);

        //            SetSkillPos(mactionState);
        //            _skill.ActionStateMachine.InitState(_skill.SkillWarp.ActionStateInfo.mStartActionNames, mactionState, mSkill.position, mSkill.rotation);
        //        }
        //    });
        //}
        private TargetUnit GetTargetUnit(ActionStatePart _part, ActionEngine_Skill _skill)
        {
            if (UseBlueMasterTarget)
            {
                return MasterUnit.value(_part, EngineResourcesManager.Instance.MachineTime); 
            }
            else
            {
                ActionEngine_Unit _self = _part.ActionStateMachine.CurUnit;
                if (SkillMaster == 0)
                    return _self;
                if (SkillMaster == 1)
                    return _self.GetMaster;
                if (SkillMaster == 2)
                    return _self.GetSource;
                if (SkillMaster == 3)
                    return _part.ActionStateMachine.HitUnit;
                if (SkillMaster == 4)
                    return _part.ActionStateMachine.AttackerUnit;
            }
            return null;
        }
        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_CreateSkill eventCast = _eventData as Event_CreateSkill;
            //eventCast.IsAttackHit = mIsAttackHit;
            eventCast.ExitDestory = mExitDestory;
            eventCast.GSkillID = GSkillID.Clone();
            eventCast.IsUsGValue = mIsUsGValue;
            eventCast.SkillID = mSkillID;
            eventCast.CharacterPoint = mCharacterPoint;
            eventCast.OffsetPos = mOffsetPos;
            eventCast.OffsetRot = mOffsetRot;
            eventCast.UseBlueMasterTarget = mUseBlueMasterTarget;
            eventCast.MasterUnit = MasterUnit.Clone();
            eventCast.SkillMaster = SkillMaster;
            //eventCast.SkillType = (GEnum)SkillType.Clone();
            eventCast.mInheritanceAttaker = mInheritanceAttaker;
            eventCast.mInheritanceHiter = mInheritanceHiter;
            eventCast.CreateMul = mCreateMul;
            eventCast.mGGroupSkillID = GGroupSkillID.Clone();
            eventCast.PreviewActionIndex = mPreviewActionIndex;
            return eventCast;
        }
    }
}