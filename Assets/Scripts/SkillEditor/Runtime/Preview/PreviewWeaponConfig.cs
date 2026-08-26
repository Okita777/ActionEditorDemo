using System.Collections.Generic;
using AsiSkillEditor.RunTime;
using UnityEngine;

namespace SkillEditor.Preview
{
    [DisallowMultipleComponent]
    public sealed class PreviewWeaponConfig : MonoBehaviour
    {
        public SkillWeaponType WeaponType = SkillWeaponType.OneHandSword;
        public List<PreviewMountPoint> MountPoints = new List<PreviewMountPoint>();
    }
}
