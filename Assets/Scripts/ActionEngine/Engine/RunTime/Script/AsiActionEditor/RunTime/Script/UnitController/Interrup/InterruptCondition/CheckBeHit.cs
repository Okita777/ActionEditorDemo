using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class CheckBeHit : IInterruptCondition
    {
        [SerializeField] protected byte _targetType = HitTargetMatch.Self;

        #region Property
        [EditorProperty("目标", EditorPropertyType.EEPT_Enum,
            EnumNames = new[] { "自身", "父级", "源" })]
        public byte TargetType
        {
            get { return _targetType; }
            set { _targetType = value; }
        }
        #endregion

        public int InterruptType => -(int)EInterruptTypeInternal.EIT_CheckBeHit;

        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            // unit = 被击单位(initiator), actionStatePart 持有者 = host
            ActionEngine_Unit host = actionStatePart.ActionStateMachine.CurUnit;
            return HitTargetMatch.Match(host, unit, _targetType);
        }

        public IInterruptCondition Clone()
        {
            CheckBeHit check = new CheckBeHit();
            check._targetType = _targetType;
            return check;
        }
    }
}
