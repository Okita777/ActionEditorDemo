
namespace AsiActionEngine.RunTime
{
    public partial class EngineResourcesManager : SingletonBase<EngineResourcesManager>
    {
        //public SGValue GetSGValue(GValue _gvalue)
        //{
        //    return new SGValue(_gvalue.mValueGroupIndex, _gvalue.mValueIndex, GetGVType(_gvalue),);
        //}

        //private EGValueType GetGVType (GValue _gvalue)
        //{
        //    if (_gvalue is GInt) return EGValueType.GInt;
        //    return EGValueType.GInt;
        //}

        public void SetUnitGvalue(ActionStatePart _part, ushort _groupIndex, ushort _Index, EGValueType _type, object _value)
        {
            ActionStateMachine _unit = _part.ActionStateMachine;

            switch (_type)
            {
                case EGValueType.GBool:

                    break;
            }
        }

        private ActionStatePart _prePart = null;

        private ActionStatePart OnGetPreActionStatePart()
        {
            if (_prePart is null)
            {
                _prePart = new ActionStatePart(null, 0);
                ActionState actionState = new ActionState();
                actionState.Name = "初始化层级";
                actionState.AnimaLayer = 0;
                actionState.ID = 0;
                _prePart.CurrentActionState = actionState;
            }
            return _prePart;
        }
    }
}