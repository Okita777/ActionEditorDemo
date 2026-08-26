using System;
using System.Collections.Generic;
using ActionEditor.TagSystem;
using SkillEditor.Preview;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    public interface ICharacterSkillRuntime
    {
        SkillSlotGroup SlotGroup { get; }
        int SlotIndex { get; }
        string DisplayName { get; }
        string SkillAssetName { get; }
        SkillConfig Config { get; }
        bool IsDynamic { get; }
    }

    public interface ICharacterBuff
    {
        string BuffId { get; }
        string BuffName { get; }
        IReadOnlyList<string> Tags { get; }
        int StackCount { get; }
        float RemainingDuration { get; }
        BuffType BuffType { get; }
    }

    public interface IGameUnit : ISkillAttributeSource, ISkillResourceService, IRuntimeTagContainerOwner
    {
        string name { get; }
        GameObject gameObject { get; }
        GameObject UnitObject { get; }
        SkillAttributeSet Attributes { get; }
        SkillPlayerController SkillPlayer { get; }
        CharacterBuffContainer Buffs { get; }
        void SetAttribute(SkillAttributeType attributeType, float value);
        void AddAttributeDelta(SkillAttributeType attributeType, float delta);
        void SetPrimaryTarget(SkillEditor.Preview.GameUnit target);
        SkillEditor.Preview.GameUnit GetPrimaryTarget();
        void ReloadSkills();
        IReadOnlyList<CharacterSkillSlot> GetSkillConfigs();
        IReadOnlyList<CharacterSkillSlot> GetSkillConfigs(SkillSlotGroup group);
        IReadOnlyList<ICharacterSkillRuntime> GetSkills();
        IReadOnlyList<ICharacterSkillRuntime> GetSkills(SkillSlotGroup group);
        IReadOnlyList<ICharacterBuff> GetAllBuff();
        IReadOnlyList<ICharacterBuff> GetBuffByTags(IReadOnlyList<string> tags);
        bool HasBuff(string buffId);
        IReadOnlyList<string> GetTags();
    }

    [Serializable]
    public sealed class CharacterSkillSlot
    {
        public SkillSlotGroup SlotGroup = SkillSlotGroup.Active;
        public int SlotIndex = 1;
        public string DisplayName = string.Empty;
        public string ActionName = string.Empty;
        public string SkillAssetName = string.Empty;
    }

    public sealed class CharacterSkillRuntimeInfo : ICharacterSkillRuntime
    {
        public CharacterSkillRuntimeInfo(
            SkillSlotGroup slotGroup,
            int slotIndex,
            string displayName,
            string skillAssetName,
            SkillConfig config,
            bool isDynamic)
        {
            SlotGroup = slotGroup;
            SlotIndex = slotIndex;
            DisplayName = displayName ?? string.Empty;
            SkillAssetName = skillAssetName ?? string.Empty;
            Config = config;
            IsDynamic = isDynamic;
        }

        public SkillSlotGroup SlotGroup { get; }
        public int SlotIndex { get; }
        public string DisplayName { get; }
        public string SkillAssetName { get; }
        public SkillConfig Config { get; }
        public bool IsDynamic { get; }
    }
}