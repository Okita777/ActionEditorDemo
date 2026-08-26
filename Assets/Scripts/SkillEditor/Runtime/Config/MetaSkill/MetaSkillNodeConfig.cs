using System;
using UnityEngine.Serialization;

namespace AsiSkillEditor.RunTime
{
    [Serializable]
    public class MetaSkillNodeConfig
    {
        public string NodeId = Guid.NewGuid().ToString("N");
        public string DisplayName = "元技能节点";
        [FormerlySerializedAs("MetaSkillId")]
        public string MetaSkillAssetName = string.Empty;
        public bool HasEditorPosition;
        public float EditorPositionX;
        public float EditorPositionY;
        public TagContainer Tags = new TagContainer();
    }
}
