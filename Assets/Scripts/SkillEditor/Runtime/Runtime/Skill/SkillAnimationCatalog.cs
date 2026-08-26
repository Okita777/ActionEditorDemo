using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    [CreateAssetMenu(fileName = "SkillAnimationCatalog", menuName = "SkillEditor/Animation Catalog")]
    public sealed class SkillAnimationCatalog : ScriptableObject
    {
        public List<SkillAnimationCatalogEntry> Entries = new List<SkillAnimationCatalogEntry>();
    }

    [Serializable]
    public sealed class SkillAnimationCatalogEntry
    {
        public string Key = string.Empty;
        public AnimationClip Clip;
    }

    [CreateAssetMenu(fileName = "SkillBulletCatalog", menuName = "SkillEditor/Bullet Catalog")]
    public sealed class SkillBulletCatalog : ScriptableObject
    {
        public List<SkillBulletCatalogEntry> Entries = new List<SkillBulletCatalogEntry>();
    }

    [Serializable]
    public sealed class SkillBulletCatalogEntry
    {
        public string Key = string.Empty;
        public GameObject Prefab;
    }
}