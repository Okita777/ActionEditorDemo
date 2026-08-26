using System;
using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public class SkillEffectConfig
    {
        public string EffectId = Guid.NewGuid().ToString("N");
        public string RootNodeId = string.Empty;
        public List<SkillEffectNodeConfig> Nodes = new List<SkillEffectNodeConfig>();
    }
}
