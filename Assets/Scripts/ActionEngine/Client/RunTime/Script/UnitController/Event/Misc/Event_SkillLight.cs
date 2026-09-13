using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [Serializable]
    public class Event_SkillLight : IActionEventData
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

        public int GetEvenType() => (int)EEvenType.EET_SkillLight;
        public IActionEventData Creact() => new Event_SkillLight();

        private class LightSlot
        {
            public LightData Data;
            public Transform Instance;
            public Light Light;
        }

        [NonSerialized] private UnitEquip m_UnitEquip;
        [NonSerialized] private readonly List<LightSlot> m_Slots = new List<LightSlot>();
        [NonSerialized] private bool m_Valid;

        public void Enter(ActionStatePart actionState, bool isSingle)
        {
            if (string.IsNullOrEmpty(m_PrefabPath) || m_Lights == null || m_Lights.Count == 0)
            {
                return;
            }

            var ownerStateMachine = actionState.IsTem
                ? actionState.ActionStateMachine.CurUnit.GetSource.ActionStateMachine
                : actionState.ActionStateMachine;
            ownerStateMachine.TryGetComponent(out m_UnitEquip, nameof(UnitEquip));

            m_Valid = true;
            var capturedActionState = actionState;
            for (int i = 0; i < m_Lights.Count; i++)
            {
                var data = m_Lights[i];
                if (data == null)
                {
                    continue;
                }

                var slot = new LightSlot { Data = data };
                m_Slots.Add(slot);

                EngineResourcesManager.Instance.CreactObjToComponent<Transform>(m_PrefabPath, instance =>
                {
                    if (!m_Valid)
                    {
                        EngineResourcesManager.Instance.RemoveComponent(m_PrefabPath, instance);
                        return;
                    }

                    slot.Instance = (Transform)instance;
                    slot.Instance.SetParent(null, false);
                    ApplyActionPose(slot, capturedActionState);

                    slot.Instance.TryGetComponent(out slot.Light);
                    if (data.PositionMode == ELightPositionMode.GroundSnap)
                    {
                        ApplyGroundSnap(slot);
                    }
                    ApplyLightParams(slot, 0f);
                }, 1);
            }
        }

        public void Update(ActionStatePart actionState, ActionMachineTime actionTime)
        {
            if (m_Slots.Count == 0)
            {
                return;
            }

            var percent = actionTime.GetPercentage();
            for (int i = 0; i < m_Slots.Count; i++)
            {
                var slot = m_Slots[i];
                if (slot.Instance == null)
                {
                    continue;
                }

                var mode = slot.Data.PositionMode;
                if (mode == ELightPositionMode.FollowAttach || mode == ELightPositionMode.GroundSnap)
                {
                    ApplyActionPose(slot, actionState);
                }

                // GroundSnap：先由 ApplyActionPose 算出 x/y/z，再用射线覆盖 y
                if (mode == ELightPositionMode.GroundSnap)
                {
                    ApplyGroundSnap(slot);
                }

                ApplyLightParams(slot, percent);
            }
        }

        public void Exit(ActionStatePart actionState, bool interrupt)
        {
            m_Valid = false;
            for (int i = 0; i < m_Slots.Count; i++)
            {
                var slot = m_Slots[i];
                if (slot.Instance != null)
                {
                    EngineResourcesManager.Instance.RemoveComponent(m_PrefabPath, slot.Instance);
                }
            }
            m_Slots.Clear();
            m_UnitEquip = null;
        }

        private static void ApplyActionPose(LightSlot slot, ActionStatePart actionState)
        {
            if (slot.Instance == null)
            {
                return;
            }

            var worldPos = actionState.Pos + actionState.Rot * slot.Data.OffsetPos.GetValue();
            slot.Instance.SetPositionAndRotation(worldPos, actionState.Rot);
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
            var copy = source as Event_SkillLight;
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
