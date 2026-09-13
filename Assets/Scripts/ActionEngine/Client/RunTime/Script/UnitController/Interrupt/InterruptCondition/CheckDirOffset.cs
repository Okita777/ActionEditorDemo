using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class CheckDirOffset : IInterruptCondition
    {
        [SerializeField] protected EAngleType mForAngleType;
        [SerializeField] protected EAngleType mToAngleType;
        [SerializeField] protected GEnum mfh = new GEnum();
        [SerializeField] protected SelectTransform mSTransform_r = new SelectTransform();
        [SerializeField] protected SelectTransform mSTransform_t = new SelectTransform();

        [SerializeField] protected float mSelfAngle;
        [SerializeField] protected bool mSelfAngleABS = false;
        [SerializeField] protected float mOffsetAngle;

        #region Property
        [EditorProperty("参考朝向", EditorPropertyType.EEPT_Enum)]
        public EAngleType ForAngleType
        {
            get { return mForAngleType; }
            set { mForAngleType = value; }
        }
        [EditorProperty("      ->目标对象", EditorPropertyType.EEPT_SelectTransform)]
        public SelectTransform STransform_r
        {
            get { return mSTransform_r; }
            set { mSTransform_r = value; }
        }
        [EditorProperty("偏移参考角度", EditorPropertyType.EEPT_Float)]
        public float OffsetAngle
        {
            get { return mOffsetAngle; }
            set { mOffsetAngle = value; }
        }
        [EditorProperty("目标朝向", EditorPropertyType.EEPT_Enum)]
        public EAngleType ToAngleType
        {
            get { return mToAngleType; }
            set { mToAngleType = value; }
        }
        [EditorProperty("      ->目标对象", EditorPropertyType.EEPT_SelectTransform)]
        public SelectTransform STransform_t
        {
            get { return mSTransform_t; }
            set { mSTransform_t = value; }
        }
        [EditorProperty("角度差为绝对值", EditorPropertyType.EEPT_Bool)]
        public bool SelfAngleABS
        {
            get { return mSelfAngleABS; }
            set { mSelfAngleABS = value; }
        }
        [EditorProperty("对比符号", EditorPropertyType.EEPT_Enum, EnumNames = new[] { "<=", ">=" })]
        public GEnum FH
        {
            get { return mfh; }
            set { mfh = value; }
        }

        [EditorProperty("对比角度", EditorPropertyType.EEPT_Float)]
        public float SelfAngle
        {
            get { return mSelfAngle; }
            set { mSelfAngle = value; }
        }

        #endregion
        public int InterruptType => (int)EConditionType.EIT_CheckDirOffset;
        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            ActionStateMachine _stateMachine = actionStatePart.ActionStateMachine;

            if (mForAngleType == EAngleType.LookToOnHit || mForAngleType == EAngleType.OnHitForward)
            {
                if (!_stateMachine.OnHitValid) return false;
            }

            if (mForAngleType == EAngleType.AttackerForward || mForAngleType == EAngleType.LookToAttacker)
            {
                if (!_stateMachine.OnAttackerValid) return false;
            }

            if (mForAngleType == EAngleType.GTransForward || mForAngleType == EAngleType.LookToGTrans)
            {
                if (mSTransform_r.Get(_stateMachine) is null) return false;
            }
            if (mToAngleType == EAngleType.GTransForward || mToAngleType == EAngleType.LookToGTrans)
            {
                if (mSTransform_t.Get(_stateMachine) is null) return false;
            }
            if (_stateMachine.TryGetStaticLogic(out Ex_GetAngleToType _getAngleToType, nameof(Ex_GetAngleToType)))
            {
                float _angle = _getAngleToType.GetOffsetAngle(_stateMachine, mForAngleType, mToAngleType, mOffsetAngle,
                    mSTransform_r, mSTransform_t);
                if (mSelfAngleABS)
                {
                    if (mfh.GetValue(actionStatePart) == 0) return Mathf.Abs(_angle) * 2 <= mSelfAngle;
                    return Mathf.Abs(_angle) * 2 >= mSelfAngle;
                }
                else
                {
                    if (mfh.GetValue(actionStatePart) == 0) return _angle <= mSelfAngle;
                    return _angle >= mSelfAngle;
                }

            }
            return false;
        }

        public IInterruptCondition Clone()
        {
            CheckDirOffset _check = new CheckDirOffset();
            _check.ForAngleType = mForAngleType;
            _check.ToAngleType = mToAngleType;
            _check.FH = (GEnum)mfh.Clone();
            _check.SelfAngle = mSelfAngle;
            _check.mSTransform_r = mSTransform_r.Clone();
            _check.mSTransform_t = mSTransform_t.Clone();
            _check.OffsetAngle = mOffsetAngle;
            _check.SelfAngleABS = mSelfAngleABS;
            return _check;
        }
    }
}