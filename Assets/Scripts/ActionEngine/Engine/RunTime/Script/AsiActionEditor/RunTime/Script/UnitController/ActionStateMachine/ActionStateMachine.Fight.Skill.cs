
using System.Collections.Generic;

namespace AsiActionEngine.RunTime
{
    public partial class ActionStateMachine
    {
        private Dictionary<byte, List<ActionEngine_Unit>> mSkillDic = new Dictionary<byte, List<ActionEngine_Unit>>();

        public bool TryAddSkill(ActionEngine_Skill _skill)
        {
            _skill.mParentMachine = this;
            if (mSkillDic.TryGetValue(_skill.mSkillType, out List<ActionEngine_Unit> _list))
            {
                if (!_list.Contains(_skill))
                {
                    _list.Add(_skill);
                }
            }
            else
            {
                List<ActionEngine_Unit> _newList = new List<ActionEngine_Unit>(4);
                _newList.Add(_skill);
                mSkillDic.Add(_skill.mSkillType, _newList);
                return true;
            }
            return false;
        }

        public bool TryRemove(ActionEngine_Skill _skill)
        {
            if (mSkillDic.TryGetValue(_skill.mSkillType, out List<ActionEngine_Unit> _list))
            {
                return _list.Remove(_skill);
            }
            return false;
        }
    }
}