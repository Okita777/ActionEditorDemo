using System;
using AsiActionEngine.RunTime;
using UnityEngine;
using UnityEngine.Rendering;

namespace AsiTimeLine.RunTime
{
    [Serializable]
    public class Event_RimLight : IActionEventData
    {
        public const string cRimLightKeyword = "_RIMLIGHT";
        public const string cColorPropertyName = "_RimLightColor";
        public const string cPowerPropertyName = "_RimLightPower";
        public const string cIntensityPropertyName = "_RimLightIntensity";
        private const int cMainWeaponSlotID = 0;
        private const int cSubWeaponSlotID = 1;

        [SerializeField] private int m_EquipMask;
        [SerializeField] private string m_Gradient;
        [SerializeField] private string m_PowerCurve;
        [SerializeField] private string m_IntensityCurve;

        #region Properties
        //[EditorProperty("校正部位", EditorPropertyType.EEPT_EnumMask, EnumNames = new[] { "头;部位1", "身;部位2", "手;部位3", "脚;部位4", "披风;部位5", "头发;部位6", "脸;部位7" ,"主武器;部位8", "副武器;部位9" })]
        public int EquipMask
        {
            get => m_EquipMask;
            set => m_EquipMask = value;
        }

        [EditorProperty("边缘光渐变色", EditorPropertyType.EEPT_Gradient)]
        public string Gradient
        {
            get => m_Gradient;
            set => m_Gradient = value;
        }

        [EditorProperty("边缘光Power曲线", EditorPropertyType.EEPT_CurveObject)]
        public string PowerCurve
        {
            get => m_PowerCurve;
            set => m_PowerCurve = value;
        }

        [EditorProperty("边缘光强度曲线", EditorPropertyType.EEPT_CurveObject)]
        public string IntensityCurve
        {
            get => m_IntensityCurve;
            set => m_IntensityCurve = value;
        }

        #endregion

        public int GetEvenType() => (int)EEvenType.EET_RimLight;
        public IActionEventData Creact() => new Event_RimLight();

        [NonSerialized] private UnitEquip m_UnitEquip;

        public void Enter(ActionStatePart actionState, bool isSingle)
        {
            ActionStateMachine _stateMachine;
            if (actionState.IsTem) _stateMachine = actionState.ActionStateMachine.CurUnit.GetSource.ActionStateMachine;
            else _stateMachine = actionState.ActionStateMachine;
            if (!_stateMachine.TryGetComponent(out m_UnitEquip, nameof(UnitEquip)))
            {
                return;
            }

            if (_stateMachine.TryGetComponent(out ActionEngine_Entity entity, nameof(ActionEngine_Entity)))
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

            m_UnitEquip.ExecuteMaterialModify(m_EquipMask, ShaderPropertyType.Color, cColorPropertyName);
            m_UnitEquip.ExecuteMaterialModify(m_EquipMask, ShaderPropertyType.Float, cPowerPropertyName);
            m_UnitEquip.ExecuteMaterialModify(m_EquipMask, ShaderPropertyType.Float, cIntensityPropertyName);
            m_UnitEquip.SetKeywordState(m_EquipMask, cRimLightKeyword, true);
            UpdateModifyValue(0);
        }

        public void Update(ActionStatePart actionState, ActionMachineTime actionTime)
        {
            if (!m_UnitEquip)
            {
                return;
            }

            var pct = actionTime.GetPercentage();
            UpdateModifyValue(pct);
        }

        private void UpdateModifyValue(float percent)
        {
            if (m_UnitEquip.GetGradientAsset(m_Gradient, out var gradient))
            {
                var color = gradient.Evaluate(percent);
                m_UnitEquip.UpdateMaterialModify(m_EquipMask, ShaderPropertyType.Color, cColorPropertyName, color);
            }
            if (m_UnitEquip.GetCurveAsset(m_PowerCurve, out var curve))
            {
                var power = curve.Evaluate(percent);
                m_UnitEquip.UpdateMaterialModify(m_EquipMask, ShaderPropertyType.Float, cPowerPropertyName, power);
            }
            if (m_UnitEquip.GetCurveAsset(m_IntensityCurve, out curve))
            {
                var intensity = curve.Evaluate(percent);
                m_UnitEquip.UpdateMaterialModify(m_EquipMask, ShaderPropertyType.Float, cIntensityPropertyName, intensity);
            }
        }

        public void Exit(ActionStatePart actionState, bool interrupt)
        {
            if (!m_UnitEquip)
            {
                return;
            }

            var needRevertKeyword = true;
            needRevertKeyword &= m_UnitEquip.RevertMaterialModify(m_EquipMask, ShaderPropertyType.Color, cColorPropertyName, interrupt);
            needRevertKeyword &= m_UnitEquip.RevertMaterialModify(m_EquipMask, ShaderPropertyType.Float, cPowerPropertyName, interrupt);
            needRevertKeyword &= m_UnitEquip.RevertMaterialModify(m_EquipMask, ShaderPropertyType.Float, cIntensityPropertyName, interrupt);

            if (needRevertKeyword)
            {
                m_UnitEquip.SetKeywordState(m_EquipMask, cRimLightKeyword, false);
            }
        }

        public IActionEventData Clone(IActionEventData source)
        {
            var copy = source as Event_RimLight;
            copy.m_EquipMask = m_EquipMask;
            copy.m_Gradient = m_Gradient;
            copy.m_PowerCurve = m_PowerCurve;
            copy.m_IntensityCurve = m_IntensityCurve;

            return copy;
        }
    }
}