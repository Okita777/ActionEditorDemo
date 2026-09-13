using System;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [Serializable]
    public class Event_AfterImage : IActionEventData
    {
        [SerializeField] private float m_SpawnInterval = 0.08f;
        [SerializeField] private float m_Duration = 0.5f;
        [SerializeField] private int m_MaxCount = 12;
        [SerializeField] private float m_FresnelPower = 2f;
        [SerializeField] private string m_FresnelGradient;
        [SerializeField] private string m_FresnelIntensity;

        #region Properties

        [EditorProperty("生成间隔(s)", EditorPropertyType.EEPT_Float)]
        public float SpawnInterval
        {
            get => m_SpawnInterval;
            set => m_SpawnInterval = value;
        }

        [EditorProperty("残影持续时间(s)", EditorPropertyType.EEPT_Float)]
        public float Duration
        {
            get => m_Duration;
            set => m_Duration = value;
        }

        [EditorProperty("最大残影数量", EditorPropertyType.EEPT_Int)]
        public int MaxCount
        {
            get => m_MaxCount;
            set => m_MaxCount = value;
        }

        [EditorProperty("菲涅尔Power", EditorPropertyType.EEPT_Float)]
        public float FresnelPower
        {
            get => m_FresnelPower;
            set => m_FresnelPower = value;
        }

        [EditorProperty("菲涅尔颜色渐变", EditorPropertyType.EEPT_Gradient)]
        public string FresnelGradient
        {
            get => m_FresnelGradient;
            set => m_FresnelGradient = value;
        }

        [EditorProperty("菲涅尔Intensity曲线", EditorPropertyType.EEPT_CurveObject)]
        public string FresnelIntensity
        {
            get => m_FresnelIntensity;
            set => m_FresnelIntensity = value;
        }
        
        #endregion

        public int GetEvenType() => (int)EEvenType.EET_AfterImage;
        public IActionEventData Creact() => new Event_AfterImage();

        [NonSerialized] private ActionEngine_External m_External;
        [NonSerialized] private UnitEquip m_UnitEquip;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            var stateMachine = _actionState.IsTem ? _actionState.ActionStateMachine.CurUnit.GetSource.ActionStateMachine : _actionState.ActionStateMachine;
            var unitTransform = stateMachine.CurUnit.transform;

            if (!unitTransform.TryGetComponent(out m_External) || !unitTransform.TryGetComponent(out m_UnitEquip))
            {
                return;
            }

            if (!m_UnitEquip.GetGradientAsset(m_FresnelGradient, out var gradient) || !m_UnitEquip.GetCurveAsset(m_FresnelIntensity, out var curve))
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
            
            RegisterMeshRender(EquipType.Body);
            RegisterMeshRender(EquipType.Head);
            RegisterMeshRender(EquipType.Hair);
            RegisterMeshRender(EquipType.Hand);
            RegisterMeshRender(EquipType.Leg);
            RegisterMeshRender(EquipType.Cape);
            RegisterMeshRender(EquipType.MainWeapon);
            RegisterMeshRender(EquipType.SubWeapon);
            m_External.SetAfterImageConfig(m_SpawnInterval, m_Duration, m_FresnelPower, m_MaxCount, gradient, curve);
            m_External.StartAfterImage();
        }

        private void RegisterMeshRender(EquipType type)
        {
            if (m_UnitEquip.GetMeshByType(type, out var mesh) && mesh.enabled && mesh.gameObject.activeSelf)
            {
                m_External.RegisterAfterImage(mesh);
            }
        }
        
        public void Update(ActionStatePart actionState, ActionMachineTime actionTime)
        {
            if (!m_UnitEquip || !m_External)
            {
                return;
            }

            m_External.UpdateAfterImage(actionTime.Deltatime);
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            if (m_External)
            {
                m_External.StopAfterImage();
            }
            m_External = null;
            m_UnitEquip = null;
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            var e = _eventData as Event_AfterImage;

            e.m_SpawnInterval = m_SpawnInterval;
            e.m_Duration = m_Duration;
            e.m_MaxCount = m_MaxCount;
            e.m_FresnelPower = m_FresnelPower;
            e.m_FresnelGradient = m_FresnelGradient;
            e.m_FresnelIntensity = m_FresnelIntensity;

            return e;
        }
    }
}
