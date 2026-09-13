using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;

namespace AsiTimeLine.Editor
{
    public partial class CreactActionEvent
    {
        public static IInterruptCondition Condition(int _id)
        {
            EConditionType _interruptType = (EConditionType)_id;

            switch (_interruptType)
            {
                case EConditionType.EIT_CheckGround:
                    return new CheckGround();

                case EConditionType.EIT_CheckIsPlayer:
                    return new CheckIsPlayer();

                case EConditionType.EIT_CheckHeight:
                    return new CheckHeight();

                case EConditionType.EIT_CheckTransfrom_RotAndPosOffset:
                    return new CheckTransfrom_RotAndPosOffset();

                case EConditionType.EIT_CheckBarrier:
                    return new CheckBarrierData();

                case EConditionType.EIT_CheckInputToTranDir:
                    return new CheckInputToTranDir();

                case EConditionType.EIT_CheckActionLable:
                    return new CheckUnitActionStateLable();

                case EConditionType.EIT_CheckActionState:
                    return new CheckUnitActionState();

                case EConditionType.EIT_CheckInputDir:
                    return new CheckInputDir();

                case EConditionType.EIT_CheckGValue:
                    return new CheckGValue();

                case EConditionType.EIT_CheckDirOffset:
                    return new CheckDirOffset();

                case EConditionType.EIT_CheckRadiusObj:
                    return new CheckRadiusObj();

                default:
                    EngineDebug.LogError("客户端未配置实例化!! " + _interruptType);
                    return null;
            }
        }
    }
}