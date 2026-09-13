using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_CharacterPos : IActionEventData
    {
        [SerializeReference] protected GraphEvent_NoValue_Point mNoveVelocity = new GraphEvent_NoValue_Point();

        #region property
        [EditorProperty("单位坐标", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Point NoveVelocity
        {
            get { return mNoveVelocity; }
            set { mNoveVelocity = value; }
        }
        #endregion
        public int GetEvenType() => (int)EEvenType.EET_CharacterPos;
        public IActionEventData Creact() => new Event_CharacterPos();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            PointData _point = mNoveVelocity.value(_actionState, new ActionMachineTime(0, 0, 0, 0));
            if (_actionState.IsTem)
            {
                //EngineDebug.LogWarning($"设定坐标: [{_point.pos}]");
                _actionState.SetPos(_point.pos);
                _actionState.SetRot(_point.rot);
            }
            else
            {
                Transform _stateMachine = _actionState.ActionStateMachine.CurUnit.transform;
                _stateMachine.SetPositionAndRotation(_point.pos, _point.rot);
            }
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            PointData _point = mNoveVelocity.value(_actionState, new ActionMachineTime(0, 0, 0, 0));
            if (_actionState.IsTem)
            {
                _actionState.SetPos(_point.pos);
                _actionState.SetRot(_point.rot);
            }
            else
            {
                Transform _stateMachine = _actionState.ActionStateMachine.CurUnit.transform;
                _stateMachine.SetPositionAndRotation(_point.pos, _point.rot);
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_CharacterPos _characterGravity = _eventData as Event_CharacterPos;
            _characterGravity.NoveVelocity = mNoveVelocity.Clone();
            return _characterGravity;
        }
    }
}