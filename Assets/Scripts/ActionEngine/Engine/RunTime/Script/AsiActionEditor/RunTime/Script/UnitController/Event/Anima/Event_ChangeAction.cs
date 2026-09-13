using AsiActionEngine.RunTime.GraphVal;
using System;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_ChangeAction : IActionEventData
    {
        //[SerializeField] private byte m_AnimationID = 2;
        [SerializeField] private GraphEvent_NoValue_Unit m_PlayTarget = new GraphEvent_NoValue_Unit();
        [SerializeField] private string m_AnimationName = null;
        [SerializeField] private int m_MixTime = 0;
        [SerializeField] private int m_OffsetTime = 0;

        #region property
        //[EditorProperty("目标", EditorPropertyType.EEPT_Enum, EnumNames = new[] { "自身", "命中单位", "攻击者" })]
        //public byte AnimationID
        //{
        //    get { return m_AnimationID; }
        //    set { m_AnimationID = value; }
        //}
        [EditorProperty("目标", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Unit PlayTarget
        {
            get {
#if UNITY_EDITOR
                if (m_PlayTarget is null) m_PlayTarget = new GraphEvent_NoValue_Unit();
#endif
                return m_PlayTarget; 
            }
            set { m_PlayTarget = value; }
        }
        [EditorProperty("Action名称", EditorPropertyType.EEPT_String)]
        public string AnimationName
        {
            get { return m_AnimationName; }
            set { m_AnimationName = value; }
        }
        [EditorProperty("混入时间", EditorPropertyType.EEPT_Int)]
        public int MixTime
        {
            get { return m_MixTime; }
            set { m_MixTime = value; }
        }
        [EditorProperty("裁剪时间", EditorPropertyType.EEPT_Int)]
        public int OffsetTime
        {
            get { return m_OffsetTime; }
            set { m_OffsetTime = value; }
        }
        #endregion

        [NonSerialized] private bool defaultActive = false;
        public int GetEvenType() => -(int)EEvenTypeInternal.EET_ChangeAction;
        public IActionEventData Creact() => new Event_ChangeAction();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            TargetUnit _tUnit = PlayTarget.value(_actionState, EngineResourcesManager.Instance.MachineTime);

            if(_tUnit is not null)
            {
                ActionEngine_Unit unit = _tUnit.GetUnit();
                ActionStateMachine stateMachine = unit.ActionStateMachine;
                if(stateMachine.TryGetActionState(AnimationName, out ActionState _targetAction))
                {
                    stateMachine.ChangeAction(AnimationName, 0, 0);
                }
                else
                {
                    EngineDebug.LogError($"Action强制切换失败,[<color=#ffcc00>{unit.gameObject.name}</color>]" +
                        $"下不存在 {AnimationName} 的Action.\n{EngineDebug.DebugActionStatePart(_actionState)}");
                }
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_ChangeAction _event = _eventData as Event_ChangeAction;

            //_event.AnimationID = m_AnimationID;
            _event.m_PlayTarget = PlayTarget.Clone();
            _event.AnimationName = m_AnimationName;
            _event.MixTime = m_MixTime;
            _event.OffsetTime = m_OffsetTime;

            return _event;
        }
    }
}