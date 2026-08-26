using System;
using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public class SkillEffectNodeConfig
    {
        public string NodeId = Guid.NewGuid().ToString("N");
        public SkillEffectNodeType NodeType = SkillEffectNodeType.Action;
        public List<string> Children = new List<string>();
        public bool HasEditorPosition;
        public float EditorPositionX;
        public float EditorPositionY;

        public SkillConditionConfig Condition = new SkillConditionConfig();
        public SkillActionConfig Action = new SkillActionConfig();
    }
}
