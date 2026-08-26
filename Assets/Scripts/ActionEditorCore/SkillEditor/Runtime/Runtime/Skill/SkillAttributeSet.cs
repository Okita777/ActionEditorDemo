using UnityEngine;
using SkillEditor.Preview;

namespace AsiSkillEditor.RunTime
{
    [DisallowMultipleComponent]
    public sealed class SkillAttributeSet : MonoBehaviour, ISkillAttributeSource, ISkillResourceService
    {
        private sealed class AttributeModifierEntry
        {
            public SkillAttributeType AttributeType;
            public AttributeModifyMode ModifyMode;
            public float Value;
            public string SourceId;
        }

        public float MaxHp = 100f;
        public float CurrentHp = 100f;
        public float MaxMana = 100f;
        public float CurrentMana = 100f;
        public float Attack = 10f;
        public float BreakValue = 0f;
        public float JumpHeightBonus = 0f;

        private readonly System.Collections.Generic.List<AttributeModifierEntry> _modifiers = new System.Collections.Generic.List<AttributeModifierEntry>();

        public float GetAttribute(SkillAttributeType attributeType)
        {
            switch (attributeType)
            {
                case SkillAttributeType.CurrentHp:
                    return Mathf.Clamp(ApplyModifiers(attributeType, CurrentHp), 0f, Mathf.Max(0f, GetAttribute(SkillAttributeType.MaxHp)));
                case SkillAttributeType.MaxHp:
                    return Mathf.Max(0f, ApplyModifiers(attributeType, MaxHp));
                case SkillAttributeType.Attack:
                    return Mathf.Max(0f, ApplyModifiers(attributeType, Attack));
                case SkillAttributeType.BreakValue:
                    return Mathf.Max(0f, ApplyModifiers(attributeType, BreakValue));
                case SkillAttributeType.JumpHeightBonus:
                    return ApplyModifiers(attributeType, JumpHeightBonus);
                default:
                    return 0f;
            }
        }

        public void SetAttribute(SkillAttributeType attributeType, float value)
        {
            switch (attributeType)
            {
                case SkillAttributeType.CurrentHp:
                    CurrentHp = Mathf.Clamp(value, 0f, Mathf.Max(0f, GetAttribute(SkillAttributeType.MaxHp)));
                    break;
                case SkillAttributeType.MaxHp:
                    MaxHp = Mathf.Max(0f, value);
                    ClampMutableResources();
                    break;
                case SkillAttributeType.Attack:
                    Attack = Mathf.Max(0f, value);
                    break;
                case SkillAttributeType.BreakValue:
                    BreakValue = Mathf.Max(0f, value);
                    break;
                case SkillAttributeType.JumpHeightBonus:
                    JumpHeightBonus = value;
                    break;
            }
        }

        public void AddAttributeDelta(SkillAttributeType attributeType, float delta)
        {
            SetAttribute(attributeType, GetBaseAttribute(attributeType) + delta);
        }

        public void ApplyPermanentChange(SkillAttributeType attributeType, AttributeModifyMode modifyMode, float value)
        {
            float baseValue = GetBaseAttribute(attributeType);
            switch (modifyMode)
            {
                case AttributeModifyMode.AddPercent:
                    SetAttribute(attributeType, baseValue * (1f + value));
                    break;
                case AttributeModifyMode.AddValue:
                default:
                    SetAttribute(attributeType, baseValue + value);
                    break;
            }
        }

        public void AddModifier(SkillAttributeType attributeType, AttributeModifyMode modifyMode, float value, string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId))
            {
                return;
            }

            _modifiers.Add(new AttributeModifierEntry
            {
                AttributeType = attributeType,
                ModifyMode = modifyMode,
                Value = value,
                SourceId = sourceId,
            });

            ClampMutableResources();
        }

        public void RemoveModifiersFromSource(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId) || _modifiers.Count == 0)
            {
                return;
            }

            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                AttributeModifierEntry entry = _modifiers[i];
                if (entry != null && string.Equals(entry.SourceId, sourceId, System.StringComparison.Ordinal))
                {
                    _modifiers.RemoveAt(i);
                }
            }

            ClampMutableResources();
        }

        public void ApplyDamage(float amount)
        {
            float clampedAmount = Mathf.Max(0f, amount);
            float maxHp = Mathf.Max(0f, GetAttribute(SkillAttributeType.MaxHp));
            CurrentHp = Mathf.Clamp(CurrentHp - clampedAmount, 0f, maxHp <= 0f ? CurrentHp : maxHp);
        }

        public void ApplyToughnessDamage(float amount)
        {
            BreakValue = Mathf.Max(0f, BreakValue - Mathf.Max(0f, amount));
        }

        public bool HasResource(GameUnit caster, SkillCostResourceType resourceType, float amount)
        {
            if (!ReferencesCaster(caster))
            {
                return false;
            }

            return GetResource(caster, resourceType) >= Mathf.Max(0f, amount);
        }

        public bool TryConsumeResource(GameUnit caster, SkillCostResourceType resourceType, float amount)
        {
            if (!ReferencesCaster(caster))
            {
                return false;
            }

            float clampedAmount = Mathf.Max(0f, amount);
            if (clampedAmount <= 0f)
            {
                return true;
            }

            switch (resourceType)
            {
                case SkillCostResourceType.Mana:
                    if (CurrentMana < clampedAmount)
                    {
                        return false;
                    }

                    CurrentMana = Mathf.Clamp(CurrentMana - clampedAmount, 0f, Mathf.Max(0f, MaxMana));
                    return true;
                case SkillCostResourceType.Hp:
                    if (CurrentHp < clampedAmount)
                    {
                        return false;
                    }

                    CurrentHp = Mathf.Clamp(CurrentHp - clampedAmount, 0f, Mathf.Max(0f, MaxHp));
                    return true;
                default:
                    return false;
            }
        }

        public float GetResource(GameUnit caster, SkillCostResourceType resourceType)
        {
            if (!ReferencesCaster(caster))
            {
                return 0f;
            }

            switch (resourceType)
            {
                case SkillCostResourceType.Mana:
                    return Mathf.Max(0f, CurrentMana);
                case SkillCostResourceType.Hp:
                    return Mathf.Max(0f, CurrentHp);
                default:
                    return 0f;
            }
        }

        private bool ReferencesCaster(GameUnit caster)
        {
            return caster == null || caster.UnitObject == this.gameObject;
        }

        private void Reset()
        {
            CurrentHp = MaxHp;
        }

        private float GetBaseAttribute(SkillAttributeType attributeType)
        {
            switch (attributeType)
            {
                case SkillAttributeType.CurrentHp:
                    return CurrentHp;
                case SkillAttributeType.MaxHp:
                    return MaxHp;
                case SkillAttributeType.Attack:
                    return Attack;
                case SkillAttributeType.BreakValue:
                    return BreakValue;
                case SkillAttributeType.JumpHeightBonus:
                    return JumpHeightBonus;
                default:
                    return 0f;
            }
        }

        private float ApplyModifiers(SkillAttributeType attributeType, float baseValue)
        {
            float flatDelta = 0f;
            float percentDelta = 0f;
            for (int i = 0; i < _modifiers.Count; i++)
            {
                AttributeModifierEntry entry = _modifiers[i];
                if (entry == null || entry.AttributeType != attributeType)
                {
                    continue;
                }

                if (entry.ModifyMode == AttributeModifyMode.AddPercent)
                {
                    percentDelta += entry.Value;
                }
                else
                {
                    flatDelta += entry.Value;
                }
            }

            return (baseValue + flatDelta) * (1f + percentDelta);
        }

        private void ClampMutableResources()
        {
            float effectiveMaxHp = Mathf.Max(0f, GetAttribute(SkillAttributeType.MaxHp));
            CurrentHp = Mathf.Clamp(CurrentHp, 0f, effectiveMaxHp <= 0f ? CurrentHp : effectiveMaxHp);
            MaxMana = Mathf.Max(0f, MaxMana);
            CurrentMana = Mathf.Clamp(CurrentMana, 0f, MaxMana <= 0f ? CurrentMana : MaxMana);
        }

        private void OnValidate()
        {
            MaxHp = Mathf.Max(0f, MaxHp);
            ClampMutableResources();
            Attack = Mathf.Max(0f, Attack);
            BreakValue = Mathf.Max(0f, BreakValue);
        }
    }

    public static class SkillAttributeSourceUtility
    {
        public static ISkillAttributeSource Resolve(object source)
        {
            IGameUnit characterOwner = GameUnitResolver.Resolve(source);
            if (characterOwner != null)
            {
                return characterOwner;
            }

            switch (source)
            {
                case ISkillAttributeSource attributeSource:
                    return attributeSource;
                case GameObject gameObject:
                    return gameObject.GetComponent<ISkillAttributeSource>() ?? gameObject.GetComponentInChildren<ISkillAttributeSource>(true);
                case Component component:
                    return component.GetComponent<ISkillAttributeSource>() ?? component.GetComponentInChildren<ISkillAttributeSource>(true);
                default:
                    return null;
            }
        }
    }

    public static class SkillResourceServiceUtility
    {
        public static ISkillResourceService Resolve(object source)
        {
            IGameUnit characterOwner = GameUnitResolver.Resolve(source);
            if (characterOwner != null)
            {
                return characterOwner;
            }

            switch (source)
            {
                case ISkillResourceService resourceService:
                    return resourceService;
                case GameObject gameObject:
                    return gameObject.GetComponent<ISkillResourceService>() ?? gameObject.GetComponentInChildren<ISkillResourceService>(true);
                case Component component:
                    return component.GetComponent<ISkillResourceService>() ?? component.GetComponentInChildren<ISkillResourceService>(true);
                default:
                    return null;
            }
        }
    }
}