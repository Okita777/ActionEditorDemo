using System;
using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public class SkillLayerConfig
    {
        // 这里的 Layer 不是渲染层，而是充能型技能的逻辑层。
        public int LayerIndex = 0;
        public string DisplayName = "Layer1";
        public bool HasEntryEditorPosition;
        public float EntryEditorPositionX = 360f;
        public float EntryEditorPositionY = 40f;
        public bool HasExitEditorPosition;
        public float ExitEditorPositionX = 360f;
        public float ExitEditorPositionY = 460f;
        public List<MetaSkillNodeConfig> MetaSkillNodes = new List<MetaSkillNodeConfig>();
        public List<SkillEventConfig> SkillEvents = new List<SkillEventConfig>();
    }
}
