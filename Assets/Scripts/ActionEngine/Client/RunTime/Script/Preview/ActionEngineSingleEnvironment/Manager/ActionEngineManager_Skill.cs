using System;
using AsiActionEngine.RunTime;
// using UnityEditor;

namespace AsiTimeLine.RunTime
{
    public partial class ActionEngineManager_Skill
    {
        #region Instance
        private static ActionEngineManager_Skill mInstance = null;
        public static ActionEngineManager_Skill Instance
        {
            get
            {
                if (mInstance is null)
                {
                    mInstance = new ActionEngineManager_Skill();
                }
                return mInstance;
            }
        }
        #endregion


        public void Create(int skillID, Action<ActionEngine_Skill> _callback)
        {
            ActionEngineManager_Unit.Instance.CreateSkillUnit(skillID, _target =>
            {
                //技能路径下，TargetUnit 自身即为 ActionEngine_Skill
                if (_target is ActionEngine_Skill _skill)
                {
                    _callback(_skill);
                }
                else
                {
                    EngineDebug.LogError($"技能生成失败！！ 请检查资源  技能名称：[<color=#ffcc00>{skillID}</color>]");
                }
            });
        }
    }
}
