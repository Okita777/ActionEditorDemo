using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public enum ELightPositionMode
    {
        // 触发时锁定一次世界位置，之后不再更新
        LockOnEnter = 0,
        // 作为挂点子节点跟随移动（3 轴全跟）
        FollowAttach = 1,
        // x/z 每帧跟随挂点，y 每帧贴地
        GroundSnap = 2,
    }

    [Serializable]
    public class LightData
    {
        [SerializeField] private string m_RangeCurve;
        [SerializeField] private float m_RangeMultiplier = 1f;
        [SerializeField] private string m_Color = "col_white_1001";
        [SerializeField] private string m_IntensityCurve = "val_curve_1_0";
        [SerializeField] private float m_IntensityMultiplier = 1f;
        [SerializeField] private int m_RenderingLayerMask = -1;
        [SerializeField] private int m_PartPointType;
        [SerializeField] private EVector3 m_OffsetPos;
        [SerializeField] private ELightPositionMode m_PositionMode = ELightPositionMode.FollowAttach;
        [SerializeField] private int m_GroundLayer;
        [SerializeField] private float m_GroundYOffset;

        [EditorProperty("范围曲线", EditorPropertyType.EEPT_CurveObject)]
        public string RangeCurve
        {
            get => m_RangeCurve;
            set => m_RangeCurve = value;
        }

        [EditorProperty("范围倍率", EditorPropertyType.EEPT_Float, Tooltip = "最终范围 = 范围曲线值 × 该倍率，用于在不换曲线形状的前提下整体缩放范围")]
        public float RangeMultiplier
        {
            get => m_RangeMultiplier;
            set => m_RangeMultiplier = value;
        }

        [EditorProperty("颜色", EditorPropertyType.EEPT_ColorObject)]
        public string Color
        {
            get => m_Color;
            set => m_Color = value;
        }

        [EditorProperty("强度曲线", EditorPropertyType.EEPT_CurveObject)]
        public string IntensityCurve
        {
            get => m_IntensityCurve;
            set => m_IntensityCurve = value;
        }

        [EditorProperty("强度倍率", EditorPropertyType.EEPT_Float, Tooltip = "最终强度 = 强度曲线值 × 该倍率，用于在不换曲线形状的前提下整体缩放亮度")]
        public float IntensityMultiplier
        {
            get => m_IntensityMultiplier;
            set => m_IntensityMultiplier = value;
        }

        [EditorProperty("渲染层级", EditorPropertyType.EEPT_RenderingLayerMask, Tooltip = "Light.renderingLayerMask（URP/HDRP 的 Rendering Layer，非 Physics Layer）")]
        public int RenderingLayerMask
        {
            get => m_RenderingLayerMask;
            set => m_RenderingLayerMask = value;
        }

        [EditorProperty("挂点", EditorPropertyType.EEPT_CharacteLimbType)]
        public int PartPointType
        {
            get => m_PartPointType;
            set => m_PartPointType = value;
        }

        [EditorProperty("位置偏移", EditorPropertyType.EEPT_Vector3)]
        public EVector3 OffsetPos
        {
            get => m_OffsetPos;
            set => m_OffsetPos = value;
        }

        [EditorProperty("位置模式", EditorPropertyType.EEPT_Enum, Tooltip = "LockOnEnter=触发时锁定一次；FollowAttach=挂点子节点持续跟随；GroundSnap=x/z 每帧跟随挂点、y 每帧贴地")]
        public ELightPositionMode PositionMode
        {
            get => m_PositionMode;
            set => m_PositionMode = value;
        }

        [EditorProperty("地面层级", EditorPropertyType.EEPT_LayerMask, Tooltip = "仅 GroundSnap 模式生效")]
        public int GroundLayer
        {
            get => m_GroundLayer;
            set => m_GroundLayer = value;
        }

        [EditorProperty("贴地Y偏移", EditorPropertyType.EEPT_Float, Tooltip = "仅 GroundSnap 模式生效")]
        public float GroundYOffset
        {
            get => m_GroundYOffset;
            set => m_GroundYOffset = value;
        }

        public LightData CloneTo(LightData dst)
        {
            dst.m_RangeCurve = m_RangeCurve;
            dst.m_RangeMultiplier = m_RangeMultiplier;
            dst.m_Color = m_Color;
            dst.m_IntensityCurve = m_IntensityCurve;
            dst.m_IntensityMultiplier = m_IntensityMultiplier;
            dst.m_RenderingLayerMask = m_RenderingLayerMask;
            dst.m_PartPointType = m_PartPointType;
            dst.m_OffsetPos = m_OffsetPos;
            dst.m_PositionMode = m_PositionMode;
            dst.m_GroundLayer = m_GroundLayer;
            dst.m_GroundYOffset = m_GroundYOffset;
            return dst;
        }
    }

    [Serializable]
    public class Event_Light : IActionEventData
    {
        [SerializeField] private string m_PrefabPath;
        [SerializeField] private List<LightData> m_Lights = new List<LightData> { new LightData() };

        private const float c_RayStartHeight = 50f;
        private const float c_RayMaxDistance = 200f;

        [EditorProperty("灯光预制体", EditorPropertyType.EEPT_GameObject, Required = true, Tooltip = "所有灯光共用同一个预制体，每份参数独立")]
        public string PrefabPath
        {
            get => m_PrefabPath;
            set => m_PrefabPath = value;
        }

        public List<LightData> Lights
        {
            get => m_Lights;
            set => m_Lights = value;
        }

        public int GetEvenType() => (int)EEvenType.EET_Light;
        public IActionEventData Creact() => new Event_Light();

        private class LightSlot
        {
            public LightData Data;
            public Transform Instance;
            public Light Light;
            public Transform AttachPoint;
        }

        [NonSerialized] private UnitEquip m_UnitEquip;
        [NonSerialized] private List<LightSlot> m_Slots;
        [NonSerialized] private bool m_Valid;

        public void Enter(ActionStatePart actionState, bool isSingle)
        {
            if (string.IsNullOrEmpty(m_PrefabPath) || m_Lights == null || m_Lights.Count == 0)
            {
                return;
            }

            var stateMachine = actionState.IsTem
                ? actionState.ActionStateMachine.CurUnit.GetSource.ActionStateMachine
                : actionState.ActionStateMachine;
            var self = stateMachine.CurUnit;
            if (!self)
            {
                return;
            }

            stateMachine.TryGetComponent(out m_UnitEquip, nameof(UnitEquip));
            stateMachine.TryGetComponent(out CharacterConfig config, nameof(CharacterConfig));
            var rootTransform = self.transform;

            m_Valid = true;
            foreach (var data in m_Lights)
            {
                if (data == null)
                {
                    continue;
                }

                var attachPoint = rootTransform;
                if (config && config.HelpPointDic.TryGetValue((ECharacteLimbType)data.PartPointType, out var point) && point)
                {
                    attachPoint = point;
                }

                var slot = new LightSlot { Data = data, AttachPoint = attachPoint };
                m_Slots ??= new List<LightSlot>();
                m_Slots.Add(slot);

                var data1 = data;
                EngineResourcesManager.Instance.CreactObjToComponent<Transform>(m_PrefabPath, instance =>
                {
                    if (!m_Valid)
                    {
                        EngineResourcesManager.Instance.RemoveComponent(m_PrefabPath, instance);
                        return;
                    }

                    slot.Instance = (Transform)instance;
                    ApplyInitialPose(slot);
                    slot.Instance.TryGetComponent(out slot.Light);

                    if (data1.PositionMode == ELightPositionMode.GroundSnap)
                    {
                        ApplyGroundSnap(slot);
                    }
                    ApplyLightParams(slot, 0f);
                }, 1);
            }
        }

        public void Update(ActionStatePart actionState, ActionMachineTime actionTime)
        {
            if (m_Slots == null || m_Slots.Count == 0)
            {
                return;
            }

            var percent = actionTime.GetPercentage();
            foreach (var slot in m_Slots)
            {
                if (!slot.Instance)
                {
                    continue;
                }

                // GroundSnap：每帧先按挂点重算 x/y/z（等效 FollowAttach），再用射线覆盖 y
                if (slot.Data.PositionMode == ELightPositionMode.GroundSnap)
                {
                    ApplyFollowPose(slot);
                    ApplyGroundSnap(slot);
                }
                ApplyLightParams(slot, percent);
            }
        }

        public void Exit(ActionStatePart actionState, bool interrupt)
        {
            m_Valid = false;
            foreach (var slot in m_Slots)
            {
                if (slot.Instance)
                {
                    EngineResourcesManager.Instance.RemoveComponent(m_PrefabPath, slot.Instance);
                }
            }
            m_Slots.Clear();
            m_UnitEquip = null;
        }

        private static void ApplyInitialPose(LightSlot slot)
        {
            var offset = slot.Data.OffsetPos.GetValue();
            switch (slot.Data.PositionMode)
            {
                case ELightPositionMode.FollowAttach:
                    slot.Instance.SetParent(slot.AttachPoint, false);
                    slot.Instance.localPosition = offset;
                    slot.Instance.localRotation = Quaternion.identity;
                    break;
                case ELightPositionMode.LockOnEnter:
                case ELightPositionMode.GroundSnap:
                default:
                    slot.Instance.SetParent(null, false);
                    ApplyFollowPose(slot);
                    break;
            }
        }

        private static void ApplyFollowPose(LightSlot slot)
        {
            if (slot.Instance == null || slot.AttachPoint == null)
            {
                return;
            }
            var worldPos = slot.AttachPoint.TransformPoint(slot.Data.OffsetPos.GetValue());
            slot.Instance.SetPositionAndRotation(worldPos, slot.AttachPoint.rotation);
        }

        private static void ApplyGroundSnap(LightSlot slot)
        {
            if (slot.Instance == null || slot.Data.GroundLayer == 0)
            {
                return;
            }

            var pos = slot.Instance.position;
            var origin = new Vector3(pos.x, pos.y + c_RayStartHeight, pos.z);
            if (Physics.Raycast(origin, Vector3.down, out var hit, c_RayMaxDistance, slot.Data.GroundLayer, QueryTriggerInteraction.Ignore))
            {
                slot.Instance.position = new Vector3(pos.x, hit.point.y + slot.Data.GroundYOffset, pos.z);
            }
        }

        private void ApplyLightParams(LightSlot slot, float percent)
        {
            if (slot.Light == null)
            {
                return;
            }

            var data = slot.Data;
            slot.Light.renderingLayerMask = data.RenderingLayerMask;

            if (m_UnitEquip == null)
            {
                return;
            }
            if (m_UnitEquip.GetColorAsset(data.Color, out var color))
            {
                slot.Light.color = color;
            }
            if (m_UnitEquip.GetCurveAsset(data.RangeCurve, out var rangeCurve))
            {
                slot.Light.range = rangeCurve.Evaluate(percent) * data.RangeMultiplier;
            }
            if (m_UnitEquip.GetCurveAsset(data.IntensityCurve, out var intensityCurve))
            {
                slot.Light.intensity = intensityCurve.Evaluate(percent) * data.IntensityMultiplier;
            }
        }

        public IActionEventData Clone(IActionEventData source)
        {
            var copy = source as Event_Light;
            copy.m_PrefabPath = m_PrefabPath;
            copy.m_Lights = new List<LightData>(m_Lights.Count);
            for (int i = 0; i < m_Lights.Count; i++)
            {
                copy.m_Lights.Add(m_Lights[i].CloneTo(new LightData()));
            }
            return copy;
        }
    }
}
