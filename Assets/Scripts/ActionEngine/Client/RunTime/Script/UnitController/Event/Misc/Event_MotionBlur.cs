using System;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [Serializable]
    public class Event_MotionBlur : IActionEventData
    {
        [SerializeField] private byte skillMaster = 0;

        [EditorProperty("设定技能父级和源", EditorPropertyType.EEPT_Enum, Tooltip = "如果源不是角色单位则会继续向上级找，直到找到源角色单位", EnumNames = new[] { "自身", "自身父级", "自身的源", "自身的命中者", "自身的攻击者" }, LabelWidth = 120)]
        public byte SkillMaster
        {
            get => skillMaster;
            set => skillMaster = value;
        }
        
        public int GetEvenType() => (int)EEvenType.EET_MotionBlur;
        public IActionEventData Creact() => new Event_MotionBlur();
        
        [NonSerialized] private UnitEquip m_UnitEquip;

        public void Enter(ActionStatePart actionState, bool isSingle)
        {
            var stateMachine = actionState.IsTem ? actionState.ActionStateMachine.CurUnit.GetSource.ActionStateMachine : actionState.ActionStateMachine;
            var self = stateMachine.CurUnit;
            var unitTransform = skillMaster switch
            {
                1 => self.GetMaster,
                2 => self.GetSource,
                3 => self.ActionStateMachine.HitUnit,
                4 => self.ActionStateMachine.AttackerUnit,
                _ => self,
            };

            if (!unitTransform || !unitTransform.TryGetComponent(out m_UnitEquip))
            {
                return;
            }

            if (stateMachine.TryGetComponent(out ActionEngine_Entity entity, nameof(ActionEngine_Entity)))
            {
                const int mainWeaponSlotID = 0;
                const int subWeaponSlotID = 1;
                if (entity.mPropDic_Slot.TryGetValue(mainWeaponSlotID, out var prop) && (!m_UnitEquip.GetMeshByType(EquipType.MainWeapon, out var renderer) || !Equals(renderer, prop.rendererMesh)))
                {
                    m_UnitEquip.SetMeshByType(EquipType.MainWeapon, prop.rendererMesh);
                }

                if (entity.mPropDic_Slot.TryGetValue(subWeaponSlotID, out prop) && (!m_UnitEquip.GetMeshByType(EquipType.SubWeapon, out renderer) || !Equals(renderer, prop.rendererMesh)))
                {
                    m_UnitEquip.SetMeshByType(EquipType.SubWeapon, prop.rendererMesh);
                }
            }

            SetMotionBlur(EquipType.Body, MotionVectorGenerationMode.Object);
            SetMotionBlur(EquipType.Head, MotionVectorGenerationMode.Object);
            SetMotionBlur(EquipType.Hair, MotionVectorGenerationMode.Object);
            SetMotionBlur(EquipType.Hand, MotionVectorGenerationMode.Object);
            SetMotionBlur(EquipType.Leg, MotionVectorGenerationMode.Object);
            SetMotionBlur(EquipType.Cape, MotionVectorGenerationMode.Object);
            SetMotionBlur(EquipType.MainWeapon, MotionVectorGenerationMode.Object);
            SetMotionBlur(EquipType.SubWeapon, MotionVectorGenerationMode.Object);
        }

        private void SetMotionBlur(EquipType type, MotionVectorGenerationMode mode)
        {
            if (!m_UnitEquip.GetMeshByType(type, out var mesh))
            {
                return;
            }

            if (mesh is MeshRenderer meshRenderer)
            {
                meshRenderer.motionVectorGenerationMode = mode;
            }
            else if (mesh is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                skinnedMeshRenderer.motionVectorGenerationMode = mode;
            }
        }

        public void Exit(ActionStatePart actionState, bool interrupt)
        {
            if (!m_UnitEquip)
            {
                return;
            }

            SetMotionBlur(EquipType.Body, MotionVectorGenerationMode.ForceNoMotion);
            SetMotionBlur(EquipType.Head, MotionVectorGenerationMode.ForceNoMotion);
            SetMotionBlur(EquipType.Hair, MotionVectorGenerationMode.ForceNoMotion);
            SetMotionBlur(EquipType.Hand, MotionVectorGenerationMode.ForceNoMotion);
            SetMotionBlur(EquipType.Leg, MotionVectorGenerationMode.ForceNoMotion);
            SetMotionBlur(EquipType.Cape, MotionVectorGenerationMode.ForceNoMotion);
            SetMotionBlur(EquipType.MainWeapon, MotionVectorGenerationMode.ForceNoMotion);
            SetMotionBlur(EquipType.SubWeapon, MotionVectorGenerationMode.ForceNoMotion);
        }

        public IActionEventData Clone(IActionEventData source)
        {
            var copy = source as Event_MotionBlur;
            copy.skillMaster = skillMaster;
            
            return copy;
        }
    }
}
