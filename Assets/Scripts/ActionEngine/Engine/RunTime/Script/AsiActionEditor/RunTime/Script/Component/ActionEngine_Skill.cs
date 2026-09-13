using UnityEngine;
namespace AsiActionEngine.RunTime
{
    public class ActionEngine_Skill : ActionEngine_Unit
    {
        [HideInInspector] public byte mSkillType = 0;
        [HideInInspector] public ActionStateMachine mParentMachine = null;

        private SkillWarp mSkillWarp = null;

        public SkillWarp SkillWarp
        {
            get { return mSkillWarp; }
            set { mSkillWarp = value; }
        }

        public void DestoryAllActionPart()
        {
            for (int i = ActionStateMachine.AllActionStatePart_Tmp.Count - 1; i >= 0; i--)
            {
                ActionStateMachine.DestoryPart(ActionStateMachine.AllActionStatePart_Tmp[i]);
            }
        }

        public void UnEquipSkill()
        {
            if (mParentMachine is not null)
            {
                mParentMachine.TryRemove(this);
            }
        }
    }
}
