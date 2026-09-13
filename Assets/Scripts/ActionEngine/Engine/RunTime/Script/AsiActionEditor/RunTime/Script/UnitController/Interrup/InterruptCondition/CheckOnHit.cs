using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class CheckOnHit : IInterruptCondition
    {
        [SerializeField] protected byte _targetType = HitTargetMatch.Self;
        [SerializeField] protected bool _isOnHit = true;

        #region Property
        [EditorProperty("目标", EditorPropertyType.EEPT_Enum,
            EnumNames = new[] { "自身", "父级", "源" })]
        public byte TargetType
        {
            get { return _targetType; }
            set { _targetType = value; }
        }

        [EditorProperty("有效命中", EditorPropertyType.EEPT_Bool)]
        public bool IsOnHit
        {
            get { return _isOnHit; }
            set { _isOnHit = value; }
        }
        #endregion

        public int InterruptType => -(int)EInterruptTypeInternal.EIT_CheckOnHit;

        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            // unit = 发起命中的单位(initiator), actionStatePart 持有者 = host
            ActionEngine_Unit host = actionStatePart.ActionStateMachine.CurUnit;
            if (!HitTargetMatch.Match(host, unit, _targetType)) return false;
            return unit.ActionStateMachine.CurOnHitValid == _isOnHit;
        }

        public IInterruptCondition Clone()
        {
            CheckOnHit check = new CheckOnHit();
            check._targetType = _targetType;
            check._isOnHit = _isOnHit;
            return check;
        }
    }
}
