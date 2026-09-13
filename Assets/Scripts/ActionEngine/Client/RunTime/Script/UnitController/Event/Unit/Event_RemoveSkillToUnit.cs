using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_RemoveSkillToUnit : IActionEventData
    {
        [SerializeField] protected GraphEvent_NoValue_GroupUnit mTargetUnit = new GraphEvent_NoValue_GroupUnit();
        [SerializeField] protected GraphEvent_NoValue_GroupUnit mRemoveSkill = new GraphEvent_NoValue_GroupUnit();

        #region property
        [EditorProperty("目标单位", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_GroupUnit TargetUnit
        {
            get { return mTargetUnit; }
            set { mTargetUnit = value; }
        }
        [EditorProperty("要清除的技能", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_GroupUnit RemoveSkill
        {
            get { return mRemoveSkill; }
            set { mRemoveSkill = value; }
        }

        #endregion
        public int GetEvenType() => (int)EEvenType.EET_RemoveSkillToUnit;
        [NonSerialized] public ActionStatePart mactionState = null;
        public IActionEventData Creact() => new Event_RemoveSkillToUnit();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionMachineTime _time = EngineResourcesManager.Instance.MachineTime;
            List<ActionEngine_Unit> _list = mTargetUnit.value(_actionState, _time);
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                List<ActionEngine_Unit> _skillList = RemoveSkill.value(_list[i].ActionStateMachine.FirstStatePart, _time);
                for (int j = _skillList.Count - 1; j >= 0; j--)
                {
                    ActionEngineManager_Unit.Instance.DestoryUnit(_skillList[j]);
                }
            }
        }

        //public void LateUpdate(ActionStatePart _actionState, ActionMachineTime _actionTime)
        //{
        //}

        //public void Exit(ActionStatePart _actionState, bool _interruot)//interruot 是否因打断轨退出  false代表事件自然结束
        //{
        //}
        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_RemoveSkillToUnit eventCast = _eventData as Event_RemoveSkillToUnit;

            eventCast.TargetUnit = mTargetUnit.Clone();
            eventCast.RemoveSkill = mRemoveSkill.Clone();

            return eventCast;
        }
    }
}