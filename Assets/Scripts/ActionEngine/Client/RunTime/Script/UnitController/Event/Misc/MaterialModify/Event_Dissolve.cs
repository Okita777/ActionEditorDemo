using System;
using AsiActionEngine.RunTime;

using UnityEngine;
using UnityEngine.Rendering;

namespace AsiTimeLine.RunTime
{
    [Serializable]
    public class Event_Dissolve : IActionEventData
    {
        public const string cDissolveKeyword = "_DISSOLVE";
        public const string cTexturePropertyName = "_DissolveTex";
        public const string cColorPropertyName = "_DissolveColor";
        public const string cThresholdPropertyName = "_DissolveThreshold";
        public const string cEdgePropertyName = "_DissolveEdge";
        public const string cIntensityPropertyName = "_DissolveIntensity";
        private const int cMainWeaponSlotID = 0;
        private const int cSubWeaponSlotID = 1;

        [SerializeField] private int m_EquipMask;
        [SerializeField] private string m_Texture;
        [SerializeField] private string m_Gradient;
        [SerializeField] private string m_ThresholdCurve;
        [SerializeField] private string m_EdgeCurve;
        [SerializeField] private string m_IntensityCurve;

        #region Properties

        //[EditorProperty("校正部位", EditorPropertyType.EEPT_EnumMask, EnumNames = new[] { "头;部位1", "身;部位2", "手;部位3", "脚;部位4", "披风;部位5", "头发;部位6", "脸;部位7", "主武器;部位8", "副武器;部位9" })]
        public int EquipMask
        {
            get => m_EquipMask;
            set => m_EquipMask = value;
        }

        [EditorProperty("边缘光渐变色", EditorPropertyType.EEPT_Texture)]
        public string DissolveTexture
        {
            get => m_Texture;
            set => m_Texture = value;
        }

        [EditorProperty("溶解边缘颜色", EditorPropertyType.EEPT_Gradient)]
        public string Gradient
        {
            get => m_Gradient;
            set => m_Gradient = value;
        }

        [EditorProperty("溶解阈值曲线", EditorPropertyType.EEPT_CurveObject)]
        public string Threshold
        {
            get => m_ThresholdCurve;
            set => m_ThresholdCurve = value;
        }

        [EditorProperty("溶解颜色边缘宽度", EditorPropertyType.EEPT_CurveObject)]
        public string EdgeCurve
        {
            get => m_EdgeCurve;
            set => m_EdgeCurve = value;
        }

        [EditorProperty("溶解边缘颜色强度", EditorPropertyType.EEPT_CurveObject)]
        public string IntensityCurve
        {
            get => m_IntensityCurve;
            set => m_IntensityCurve = value;
        }

        #endregion

        public int GetEvenType() => (int)EEvenType.EET_Dissolve;
        public IActionEventData Creact() => new Event_Dissolve();

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

            m_UnitEquip.ExecuteMaterialModify(m_EquipMask, ShaderPropertyType.Texture, cTexturePropertyName);
            EngineResourcesManager.Instance.AsyncLoadObj(m_Texture, obj =>
            {
                if (obj is Texture texture)
                {
                    m_UnitEquip.UpdateMaterialModify(m_EquipMask, ShaderPropertyType.Texture, cTexturePropertyName, texture);
                }
            });

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
            m_UnitEquip.ExecuteMaterialModify(m_EquipMask, ShaderPropertyType.Float, cThresholdPropertyName);
            m_UnitEquip.ExecuteMaterialModify(m_EquipMask, ShaderPropertyType.Float, cEdgePropertyName);
            m_UnitEquip.ExecuteMaterialModify(m_EquipMask, ShaderPropertyType.Float, cIntensityPropertyName);
            m_UnitEquip.SetKeywordState(m_EquipMask, cDissolveKeyword, true);
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
            if (m_UnitEquip.GetCurveAsset(m_ThresholdCurve, out var curve))
            {
                var threshold = curve.Evaluate(percent);
                m_UnitEquip.UpdateMaterialModify(m_EquipMask, ShaderPropertyType.Float, cThresholdPropertyName, threshold);
            }
            if (m_UnitEquip.GetCurveAsset(m_EdgeCurve, out curve))
            {
                var edge = curve.Evaluate(percent);
                m_UnitEquip.UpdateMaterialModify(m_EquipMask, ShaderPropertyType.Float, cEdgePropertyName, edge);
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
            needRevertKeyword &= m_UnitEquip.RevertMaterialModify(m_EquipMask, ShaderPropertyType.Texture, cTexturePropertyName, interrupt);
            needRevertKeyword &= m_UnitEquip.RevertMaterialModify(m_EquipMask, ShaderPropertyType.Color, cColorPropertyName, interrupt);
            needRevertKeyword &= m_UnitEquip.RevertMaterialModify(m_EquipMask, ShaderPropertyType.Float, cThresholdPropertyName, interrupt);
            needRevertKeyword &= m_UnitEquip.RevertMaterialModify(m_EquipMask, ShaderPropertyType.Float, cEdgePropertyName, interrupt);
            needRevertKeyword &= m_UnitEquip.RevertMaterialModify(m_EquipMask, ShaderPropertyType.Float, cIntensityPropertyName, interrupt);

            if (needRevertKeyword)
            {
                m_UnitEquip.SetKeywordState(m_EquipMask, cDissolveKeyword, false);
            }
        }

        public IActionEventData Clone(IActionEventData source)
        {
            var copy = source as Event_Dissolve;
            copy.m_EquipMask = m_EquipMask;
            copy.m_Texture = m_Texture;
            copy.m_Gradient = m_Gradient;
            copy.m_ThresholdCurve = m_ThresholdCurve;
            copy.m_EdgeCurve = m_EdgeCurve;
            copy.m_IntensityCurve = m_IntensityCurve;

            return copy;
        }
    }
}