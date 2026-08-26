using System;
using System.Collections.Generic;
using AsiSkillEditor.RunTime;
using ActionEditor.TagSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace SkillEditor.Preview
{
    [DisallowMultipleComponent]
    public class GameUnit : MonoBehaviour, IGameUnit, ITagQueryTarget
    {
        public string UnitId = string.Empty;
        public string AnimationSearchRoot = string.Empty;
        public string AnimationFilterKey = string.Empty;
        public List<PreviewMountPoint> MountPoints = new List<PreviewMountPoint>();
        [FormerlySerializedAs("PreviewWeapons")]
        public List<PreviewWeaponBinding> WeaponBindings = new List<PreviewWeaponBinding>();
        public List<PreviewActiveSkillSlotConfig> ActiveSkillSlots = new List<PreviewActiveSkillSlotConfig>();
        public List<PreviewPassiveSkillSlotConfig> PassiveSkillSlots = new List<PreviewPassiveSkillSlotConfig>();

        [SerializeField] private SkillAttributeSet _attributes;
        [SerializeField] private CharacterBattleManager _battleManager;
        [SerializeField] private TagContainer _tags = new TagContainer();
        [SerializeField] private CharacterBuffContainer _buffs = new CharacterBuffContainer();

        private const string DefaultTagSourceId = "__character_default__";

        [NonSerialized] private RuntimeTagContainer _runtimeTags;
        [NonSerialized] private bool _runtimeTagsInitialized;

        public SkillAttributeSet Attributes => _attributes != null ? _attributes : (_attributes = GetComponent<SkillAttributeSet>());

        // [AICode] UnitObject is the canonical runtime entity object behind this GameUnit.
        public GameObject UnitObject => gameObject;

        public SkillPlayerController SkillPlayer => _battleManager != null ? _battleManager.SkillPlayerController : null;

        public RuntimeTagContainer RuntimeTags => EnsureRuntimeTags();

        public CharacterBuffContainer Buffs => EnsureBuffs();

        public void SetPrimaryTarget(SkillEditor.Preview.GameUnit target)
        {
            if (SkillPlayer != null)
            {
                SkillPlayer.PrimaryTarget = target;
            }
        }

        public SkillEditor.Preview.GameUnit GetPrimaryTarget()
        {
            return SkillPlayer != null ? SkillPlayer.PrimaryTarget : null;
        }

        public void ReloadSkills()
        {
            SkillPlayer?.Reload();
        }

        public float GetAttribute(SkillAttributeType attributeType)
        {
            return Attributes != null ? Attributes.GetAttribute(attributeType) : 0f;
        }

        public void SetAttribute(SkillAttributeType attributeType, float value)
        {
            Attributes?.SetAttribute(attributeType, value);
        }

        public void AddAttributeDelta(SkillAttributeType attributeType, float delta)
        {
            Attributes?.AddAttributeDelta(attributeType, delta);
        }

        public void ApplyDamage(float amount)
        {
            Attributes?.ApplyDamage(amount);
        }

        public void ApplyToughnessDamage(float amount)
        {
            Attributes?.ApplyToughnessDamage(amount);
        }

        public bool HasResource(GameUnit caster, SkillCostResourceType resourceType, float amount)
        {
            return Attributes != null && Attributes.HasResource(caster, resourceType, amount);
        }

        public bool TryConsumeResource(GameUnit caster, SkillCostResourceType resourceType, float amount)
        {
            return Attributes != null && Attributes.TryConsumeResource(caster, resourceType, amount);
        }

        public float GetResource(GameUnit caster, SkillCostResourceType resourceType)
        {
            return Attributes != null ? Attributes.GetResource(caster, resourceType) : 0f;
        }

        public IReadOnlyList<CharacterSkillSlot> GetSkillConfigs()
        {
            List<CharacterSkillSlot> result = new List<CharacterSkillSlot>();
            CollectSkillConfigs(result, includeActive: true, includePassive: true);
            return result;
        }

        public IReadOnlyList<CharacterSkillSlot> GetSkillConfigs(SkillSlotGroup group)
        {
            List<CharacterSkillSlot> result = new List<CharacterSkillSlot>();
            CollectSkillConfigs(result, includeActive: group == SkillSlotGroup.Active, includePassive: group == SkillSlotGroup.Passive);
            return result;
        }

        public IReadOnlyList<ICharacterSkillRuntime> GetSkills()
        {
            return GetSkillsInternal(null);
        }

        public IReadOnlyList<ICharacterSkillRuntime> GetSkills(SkillSlotGroup group)
        {
            return GetSkillsInternal(group);
        }

        public IReadOnlyList<ICharacterBuff> GetAllBuff()
        {
            return EnsureBuffs().GetAllBuff();
        }

        public IReadOnlyList<ICharacterBuff> GetBuffByTags(IReadOnlyList<string> tags)
        {
            return EnsureBuffs().GetBuffByTags(tags);
        }

        public bool HasBuff(string buffId)
        {
            return EnsureBuffs().HasBuff(buffId);
        }

        public IReadOnlyList<string> GetTags()
        {
            List<string> result = new List<string>();
            AppendUniqueTags(result, EnsureRuntimeTags().Tags);

            IReadOnlyList<ICharacterBuff> buffs = EnsureBuffs().GetAllBuff();
            for (int i = 0; i < buffs.Count; i++)
            {
                ICharacterBuff buff = buffs[i];
                if (buff != null)
                {
                    AppendUniqueTags(result, buff.Tags);
                }
            }

            return result;
        }

        public bool HasTag(string tag)
        {
            return GetTagCount(tag) > 0;
        }

        public int GetTagCount(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return 0;
            }

            return EnsureRuntimeTags().GetTagCount(tag) + EnsureBuffs().GetTagCount(tag);
        }

        private void Reset()
        {
            AutoBind();
        }

        private void Update()
        {
            EnsureBuffs().Tick(Time.deltaTime);
        }

        private void OnValidate()
        {
            AutoBind();
            _tags ??= new TagContainer();
            _tags.Tags ??= new List<string>();
            _buffs ??= new CharacterBuffContainer();
            _buffs.Bind(this);
            _runtimeTagsInitialized = false;
        }

        private IReadOnlyList<ICharacterSkillRuntime> GetSkillsInternal(SkillSlotGroup? group)
        {
            List<ICharacterSkillRuntime> result = new List<ICharacterSkillRuntime>();
            IReadOnlyList<CharacterSkillSlot> slots = group.HasValue ? GetSkillConfigs(group.Value) : GetSkillConfigs();
            for (int i = 0; i < slots.Count; i++)
            {
                CharacterSkillSlot slot = slots[i];
                if (slot == null || string.IsNullOrEmpty(slot.SkillAssetName))
                {
                    continue;
                }

                SkillConfig loadedConfig = null;
                SkillRuntimeLoadData.Instance.LoadSkill(slot.SkillAssetName, config => loadedConfig = config);
                result.Add(new CharacterSkillRuntimeInfo(
                    slot.SlotGroup,
                    slot.SlotIndex,
                    slot.DisplayName,
                    slot.SkillAssetName,
                    loadedConfig,
                    false));
            }

            return result;
        }

        private void AutoBind()
        {
            if (_attributes == null)
            {
                _attributes = GetComponent<SkillAttributeSet>();
            }

            if (_battleManager == null)
            {
                _battleManager = GetComponent<CharacterBattleManager>();
            }

            _buffs ??= new CharacterBuffContainer();
            _buffs.Bind(this);
        }

        private RuntimeTagContainer EnsureRuntimeTags()
        {
            _tags ??= new TagContainer();
            _tags.Tags ??= new List<string>();
            _runtimeTags ??= new RuntimeTagContainer();

            if (_runtimeTagsInitialized)
            {
                return _runtimeTags;
            }

            for (int i = 0; i < _tags.Tags.Count; i++)
            {
                string tag = _tags.Tags[i];
                if (!string.IsNullOrEmpty(tag))
                {
                    _runtimeTags.AddTag(tag, 1, DefaultTagSourceId);
                }
            }

            _runtimeTagsInitialized = true;
            return _runtimeTags;
        }

        private CharacterBuffContainer EnsureBuffs()
        {
            _buffs ??= new CharacterBuffContainer();
            _buffs.Bind(this);
            return _buffs;
        }

        private static void AppendUniqueTags(List<string> target, IReadOnlyList<string> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                string tag = source[i];
                if (!string.IsNullOrEmpty(tag) && !target.Contains(tag))
                {
                    target.Add(tag);
                }
            }
        }

        private void CollectSkillConfigs(List<CharacterSkillSlot> result, bool includeActive, bool includePassive)
        {
            if (result == null)
            {
                return;
            }

            if (HasConfiguredActiveSlots(this) || HasConfiguredPassiveSlots(this))
            {
                if (includeActive && ActiveSkillSlots != null)
                {
                    for (int i = 0; i < ActiveSkillSlots.Count; i++)
                    {
                        PreviewActiveSkillSlotConfig slot = ActiveSkillSlots[i];
                        if (slot == null)
                        {
                            continue;
                        }

                        result.Add(new CharacterSkillSlot
                        {
                            SlotGroup = SkillSlotGroup.Active,
                            SlotIndex = Mathf.Max(1, slot.SlotIndex),
                            DisplayName = slot.DisplayName,
                            ActionName = slot.ActionName,
                            SkillAssetName = slot.SkillAssetName,
                        });
                    }
                }

                if (includePassive && PassiveSkillSlots != null)
                {
                    for (int i = 0; i < PassiveSkillSlots.Count; i++)
                    {
                        PreviewPassiveSkillSlotConfig slot = PassiveSkillSlots[i];
                        if (slot == null)
                        {
                            continue;
                        }

                        result.Add(new CharacterSkillSlot
                        {
                            SlotGroup = SkillSlotGroup.Passive,
                            SlotIndex = Mathf.Max(1, slot.SlotIndex),
                            DisplayName = slot.DisplayName,
                            SkillAssetName = slot.SkillAssetName,
                        });
                    }
                }
            }
        }

        private static bool HasConfiguredActiveSlots(GameUnit unit)
        {
            return unit != null && unit.ActiveSkillSlots != null && unit.ActiveSkillSlots.Count > 0;
        }

        private static bool HasConfiguredPassiveSlots(GameUnit unit)
        {
            return unit != null && unit.PassiveSkillSlots != null && unit.PassiveSkillSlots.Count > 0;
        }
    }

    [Serializable]
    public sealed class PreviewActiveSkillSlotConfig
    {
        public int SlotIndex = 1;
        public string DisplayName = "主动技能槽";
        public string ActionName = string.Empty;
        public string SkillAssetName = string.Empty;
    }

    [Serializable]
    public sealed class PreviewPassiveSkillSlotConfig
    {
        public int SlotIndex = 1;
        public string DisplayName = "被动技能槽";
        public string SkillAssetName = string.Empty;
    }

    [Serializable]
    public sealed class PreviewWeaponBinding
    {
        public string DisplayName = "武器挂载";
        public SkillWeaponType WeaponType = SkillWeaponType.OneHandSword;
        public string EquipSocketName = string.Empty;
        public GameObject WeaponPrefab;
        public Vector3 LocalPosition = Vector3.zero;
        public Vector3 LocalRotation = Vector3.zero;
    }

    [Serializable]
    public sealed class PreviewMountPoint
    {
        public string SocketName = string.Empty;
        public Transform MountTransform;
    }
}
