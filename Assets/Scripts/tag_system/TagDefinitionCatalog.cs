using System;
using System.Collections.Generic;
using UnityEngine;

namespace ActionEditor.TagSystem
{
    [CreateAssetMenu(fileName = "TagDefinitionCatalog", menuName = "ActionEditor/Tag/DefinitionCatalog")]
    [Serializable]
    public sealed class TagDefinitionCatalog : ScriptableObject
    {
        public List<string> Tags = new List<string>();
    }
}
