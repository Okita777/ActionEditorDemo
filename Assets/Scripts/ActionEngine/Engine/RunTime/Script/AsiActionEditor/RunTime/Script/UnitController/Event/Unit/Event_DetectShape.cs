using System.Collections.Generic;
using AsiActionEngine.RunTime.Graph;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;


namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_DetectShape : IActionEventData
    {
        public enum EDetectShapeType
        {
            球形,
            胶囊,
            扇形,
        }

        [SerializeField] private EDetectShapeType m_ShapeType = EDetectShapeType.球形;
        [SerializeField] private int m_CenterLimbID = 0;
        [SerializeField] private int m_LayerMask = 0;
        [SerializeField] private bool m_IsOverride = true;
        [SerializeField] private GraphEvent_NoValue_Float m_Radius = CreateDefaultRadius();
        [SerializeField] private GraphEvent_NoValue_Float m_ShapeHeight = CreateDefaultShapeHeight();
        [SerializeField] private GraphEvent_NoValue_Float m_SectorHalfAngle = CreateDefaultSectorHalfAngle();
        [SerializeField] private GGroupUnit m_SetUnit = new GGroupUnit();
        [SerializeField] private GraphEvent_NoValue_Point m_OffsetPoint = CreateDefaultOffsetPoint();

        #region DefaultNodeFactories

        private static GraphEvent_NoValue_Float CreateDefaultRadius()
        {
            return new GraphEvent_NoValue_Float()
            {
                BluePrint_Val = new GraphEvent_LocalParam_Read_Float() { ParamIndex = 0 },
                LocalFloatParams = new List<float>() { 3f },
#if UNITY_EDITOR
                m_NodeEdiData = new NodeEdiData()
                {
                    nodeTitle = "半径",
                    localParams = new List<BluePrintLocalParamDef>()
                    {
                        new BluePrintLocalParamDef { paramName = "半径", paramType = EBluePrintLocalParamType.Float, drawToInspector = true },
                    }
                }
#endif
            };
        }

        private static GraphEvent_NoValue_Float CreateDefaultShapeHeight()
        {
            return new GraphEvent_NoValue_Float()
            {
                BluePrint_Val = new GraphEvent_LocalParam_Read_Float() { ParamIndex = 0 },
                LocalFloatParams = new List<float>() { 2f },
#if UNITY_EDITOR
                m_NodeEdiData = new NodeEdiData()
                {
                    nodeTitle = "高度",
                    localParams = new List<BluePrintLocalParamDef>()
                    {
                        new BluePrintLocalParamDef { paramName = "高度", paramType = EBluePrintLocalParamType.Float, drawToInspector = true },
                    }
                }
#endif
            };
        }

        private static GraphEvent_NoValue_Float CreateDefaultSectorHalfAngle()
        {
            return new GraphEvent_NoValue_Float()
            {
                BluePrint_Val = new GraphEvent_LocalParam_Read_Float() { ParamIndex = 0 },
                LocalFloatParams = new List<float>() { 60f },
#if UNITY_EDITOR
                m_NodeEdiData = new NodeEdiData()
                {
                    nodeTitle = "扇形半角",
                    localParams = new List<BluePrintLocalParamDef>()
                    {
                        new BluePrintLocalParamDef { paramName = "半角(度)", paramType = EBluePrintLocalParamType.Float, drawToInspector = true },
                    }
                }
#endif
            };
        }

        private static GraphEvent_NoValue_Point CreateDefaultOffsetPoint()
        {
            return new GraphEvent_NoValue_Point()
            {
                BluePrint_Val = new GraphEvent_Make_Point()
                {
                    Position = new GraphEvent_Make_Vector3()
                    {
                        X = new GraphEvent_LocalParam_Read_Float() { ParamIndex = 0 },
                        Y = new GraphEvent_LocalParam_Read_Float() { ParamIndex = 1 },
                        Z = new GraphEvent_LocalParam_Read_Float() { ParamIndex = 2 },
                    },
                    Rotation = new GraphEvent_Make_Vector3()
                    {
                        X = new GraphEvent_LocalParam_Read_Float() { ParamIndex = 3 },
                        Y = new GraphEvent_LocalParam_Read_Float() { ParamIndex = 4 },
                        Z = new GraphEvent_LocalParam_Read_Float() { ParamIndex = 5 },
                    },
                },
                LocalFloatParams = new List<float>() { 0f, 0f, 0f, 0f, 0f, 0f },
#if UNITY_EDITOR
                m_NodeEdiData = new NodeEdiData()
                {
                    nodeTitle = "中心偏移",
                    localParams = new List<BluePrintLocalParamDef>()
                    {
                        new BluePrintLocalParamDef { paramName = "Pos X", paramType = EBluePrintLocalParamType.Float, drawToInspector = true },
                        new BluePrintLocalParamDef { paramName = "Pos Y", paramType = EBluePrintLocalParamType.Float, drawToInspector = true },
                        new BluePrintLocalParamDef { paramName = "Pos Z", paramType = EBluePrintLocalParamType.Float, drawToInspector = true },
                        new BluePrintLocalParamDef { paramName = "Rot X", paramType = EBluePrintLocalParamType.Float, drawToInspector = true },
                        new BluePrintLocalParamDef { paramName = "Rot Y", paramType = EBluePrintLocalParamType.Float, drawToInspector = true },
                        new BluePrintLocalParamDef { paramName = "Rot Z", paramType = EBluePrintLocalParamType.Float, drawToInspector = true },
                    }
                }
#endif
            };
        }

        #endregion

        // 复用缓冲，避免每帧分配，用于收集去重后的单位
        [System.NonSerialized] private List<ActionEngine_Unit> m_DetectedUnits;

        private Collider[] Colliders => EngineResourcesManager.Instance.Colliders;

        #region Properties

        [EditorProperty("检测形状", EditorPropertyType.EEPT_Enum)]
        public EDetectShapeType ShapeType
        {
            get => m_ShapeType;
            set => m_ShapeType = value;
        }

        [EditorProperty("中心位置挂点", EditorPropertyType.EEPT_CharacteLimbType)]
        public int CenterLimbID
        {
            get => m_CenterLimbID;
            set => m_CenterLimbID = value;
        }

        [EditorProperty("检测层级", EditorPropertyType.EEPT_LayerMask)]
        public int LayerMask
        {
            get => m_LayerMask;
            set => m_LayerMask = value;
        }

        [EditorProperty("半径", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Float Radius
        {
            get => m_Radius;
            set => m_Radius = value;
        }

        [EditorProperty("高度 (胶囊/扇形)", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Float ShapeHeight
        {
            get => m_ShapeHeight;
            set => m_ShapeHeight = value;
        }

        [EditorProperty("扇形半角度 (仅扇形)", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Float SectorHalfAngle
        {
            get => m_SectorHalfAngle;
            set => m_SectorHalfAngle = value;
        }

        [EditorProperty("输出检测到的单位(组)", EditorPropertyType.EEPT_GGroupUnit)]
        public GGroupUnit SetUnit
        {
            get => m_SetUnit;
            set => m_SetUnit = value;
        }

        [EditorProperty("是否覆写(否为叠加)", EditorPropertyType.EEPT_Bool, LabelWidth = 150)]
        public bool IsOverride
        {
            get => m_IsOverride;
            set => m_IsOverride = value;
        }

        [EditorProperty("中心偏移 (相对空间)", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Point OffsetPoint
        {
            get => m_OffsetPoint;
            set => m_OffsetPoint = value;
        }

        #endregion

        public int GetEvenType() => -(int)EEvenTypeInternal.EET_DetectShape;
        public IActionEventData Creact() => new Event_DetectShape();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            if (_isSingle)
                OnDetect(_actionState, new ActionMachineTime(0, 0, 0, 0));
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            OnDetect(_actionState, _actionTime);
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_DetectShape _event = _eventData as Event_DetectShape;
            _event.ShapeType = m_ShapeType;
            _event.CenterLimbID = m_CenterLimbID;
            _event.LayerMask = m_LayerMask;
            _event.IsOverride = m_IsOverride;
            _event.Radius = m_Radius.Clone();
            _event.ShapeHeight = m_ShapeHeight.Clone();
            _event.SectorHalfAngle = m_SectorHalfAngle.Clone();
            _event.SetUnit = (GGroupUnit)m_SetUnit.Clone();
            _event.OffsetPoint = m_OffsetPoint.Clone();
            return _event;
        }

        public void EditorDraw(CharacterConfig characterConfig, ActionStatePart _actionState,
            ActionMachineTime _actionTime)
        {
            if (!_actionTime.IsInRange || _actionState is null) return;

            float radius = m_Radius.value(_actionState, _actionTime);
            int layerMask = _actionState.ActionStateMachine.GetLayer(m_LayerMask);

            Vector3 center;
            Vector3 forward;
            Vector3 up;

            if (characterConfig is null)
            {
                // 技能编辑器环境：无挂点，使用实体的 Pos/Rot
                ApplyOffset(_actionState.Pos, _actionState.Rot, _actionState, _actionTime,
                    out center, out forward, out up);
            }
            else
            {
                if (!characterConfig.HelpPointDic.TryGetValue(
                        (ECharacteLimbType)m_CenterLimbID,
                        out Transform _center) ||
                    _center == null)
                {
                    _center = characterConfig.transform;
                }
                ApplyOffset(_center, _actionState, _actionTime, out center, out forward, out up);
            }

            bool isHit = false;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                isHit = EditorCheckHit(center, forward, up, radius, _actionState, _actionTime, layerMask);
#endif
            Color drawColor = isHit ? Color.red : Color.cyan;

            switch (m_ShapeType)
            {
                case EDetectShapeType.球形:
                    EngineScenceDraw.Sphere(center, Quaternion.LookRotation(forward), radius, drawColor);
                    break;

                case EDetectShapeType.胶囊:
                    DrawCapsuleShape(center, up, radius,
                        m_ShapeHeight.value(_actionState, _actionTime), drawColor);
                    break;

                case EDetectShapeType.扇形:
                    DrawSectorShape(center, forward,
                        radius,
                        m_SectorHalfAngle.value(_actionState, _actionTime),
                        m_ShapeHeight.value(_actionState, _actionTime),
                        drawColor);
                    break;
            }
        }

#if UNITY_EDITOR
        private bool EditorCheckHit(Vector3 center, Vector3 forward, Vector3 up, float radius,
            ActionStatePart _actionState, ActionMachineTime _actionTime, int layerMask)
        {
            switch (m_ShapeType)
            {
                case EDetectShapeType.球形:
                    return Physics.OverlapSphereNonAlloc(center, radius, Colliders, layerMask,
                        QueryTriggerInteraction.Collide) > 0;

                case EDetectShapeType.胶囊:
                {
                    float height = m_ShapeHeight.value(_actionState, _actionTime);
                    float halfSeg = Mathf.Max(0f, height * 0.5f - radius);
                    return Physics.OverlapCapsuleNonAlloc(
                        center + up * halfSeg,
                        center - up * halfSeg,
                        radius, Colliders, layerMask, QueryTriggerInteraction.Collide) > 0;
                }

                case EDetectShapeType.扇形:
                {
                    float halfAngle = m_SectorHalfAngle.value(_actionState, _actionTime);
                    float sectorHeight = m_ShapeHeight.value(_actionState, _actionTime);
                    int count = Physics.OverlapSphereNonAlloc(center, radius, Colliders, layerMask,
                        QueryTriggerInteraction.Collide);

                    float halfH = sectorHeight * 0.5f;
                    Vector3 flatFwd = new Vector3(forward.x, 0f, forward.z).normalized;
                    if (flatFwd.sqrMagnitude < 0.0001f) flatFwd = Vector3.forward;

                    for (int i = 0; i < count; i++)
                    {
                        Vector3 toCol = Colliders[i].transform.position - center;
                        if (Mathf.Abs(toCol.y) > halfH) continue;
                        Vector3 flatToCol = new Vector3(toCol.x, 0f, toCol.z);
                        if (flatToCol.sqrMagnitude < 0.0001f) return true;
                        if (Vector3.Angle(flatFwd, flatToCol) <= halfAngle) return true;
                    }
                    return false;
                }
            }
            return false;
        }
#endif

        private void OnDetect(ActionStatePart _part, ActionMachineTime _time)
        {
            m_DetectedUnits = EngineResourcesManager.Instance.CreateUnits();
            float radius = m_Radius.value(_part, _time);
            ActionStateMachine _machine = _part.ActionStateMachine;

            int layerMask;
            Vector3 center;
            Vector3 forward;
            Vector3 up;

            if (_part.IsTem)
            {
                // 技能实体环境：位置/朝向来自 ActionStatePart，LayerMask 取发射源单位的层配置
                layerMask = _machine.CurUnit.GetSource.ActionStateMachine.GetLayer(m_LayerMask);
                ApplyOffset(_part.Pos, _part.Rot, _part, _time, out center, out forward, out up);
            }
            else
            {
                layerMask = _machine.GetLayer(m_LayerMask);
                if (_machine.TryGetCharacterLimb(
                        (ECharacteLimbType)m_CenterLimbID,
                        out Transform _center))
                {
                    ApplyOffset(_center, _part, _time, out center, out forward, out up);
                }
                else
                {
                    // 无挂点配置时回退到单位根节点
                    Transform _unitTrans = _machine.CurUnit.transform;
                    ApplyOffset(_unitTrans.position, _unitTrans.rotation, _part, _time, out center, out forward, out up);
                }
            }

            CollectAllUnits(center, forward, up, radius, _part, _time, layerMask);

            List<ActionEngine_Unit> groupList = m_SetUnit.GetValue(_part);
            if (m_IsOverride) groupList.Clear();
            foreach (ActionEngine_Unit unit in m_DetectedUnits)
                groupList.Add(unit);
        }

        private void CollectAllUnits(Vector3 center, Vector3 forward, Vector3 up, float radius,
            ActionStatePart _part, ActionMachineTime _time, int layerMask)
        {
            m_DetectedUnits.Clear();
            switch (m_ShapeType)
            {
                case EDetectShapeType.球形:
                    CollectFromBuffer(Physics.OverlapSphereNonAlloc(center, radius, Colliders, layerMask,
                        QueryTriggerInteraction.Collide));
                    break;

                case EDetectShapeType.胶囊:
                {
                    float height = m_ShapeHeight.value(_part, _time);
                    float halfSeg = Mathf.Max(0f, height * 0.5f - radius);
                    CollectFromBuffer(Physics.OverlapCapsuleNonAlloc(
                        center + up * halfSeg,
                        center - up * halfSeg,
                        radius, Colliders, layerMask, QueryTriggerInteraction.Collide));
                    break;
                }

                case EDetectShapeType.扇形:
                    CollectInSector(center, forward, radius,
                        m_SectorHalfAngle.value(_part, _time),
                        m_ShapeHeight.value(_part, _time),
                        layerMask);
                    break;
            }
        }

        /// <summary>
        /// 从 NonAlloc 缓冲中提取所有带 TargetUnit 的不重复单位。
        /// </summary>
        private void CollectFromBuffer(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!Colliders[i].TryGetComponent(out TargetUnit tu)) continue;
                ActionEngine_Unit unit = tu.GetUnit();
                if (!m_DetectedUnits.Contains(unit))
                    m_DetectedUnits.Add(unit);
            }
        }

        /// <summary>
        /// 球形粗筛后按高度和水平角度精确过滤扇形范围内的不重复单位。
        /// </summary>
        private void CollectInSector(Vector3 center, Vector3 forward, float radius,
            float halfAngle, float sectorHeight, int layerMask)
        {
            int count = Physics.OverlapSphereNonAlloc(center, radius, Colliders, layerMask,
                QueryTriggerInteraction.Collide);

            float halfH = sectorHeight * 0.5f;
            Vector3 flatForward = new Vector3(forward.x, 0f, forward.z).normalized;
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.forward;

            for (int i = 0; i < count; i++)
            {
                Collider col = Colliders[i];
                Vector3 toCol = col.transform.position - center;

                if (Mathf.Abs(toCol.y) > halfH) continue;

                Vector3 flatToCol = new Vector3(toCol.x, 0f, toCol.z);
                // 位于中心点的对象直接视为在扇形内
                if (flatToCol.sqrMagnitude >= 0.0001f &&
                    Vector3.Angle(flatForward, flatToCol) > halfAngle) continue;

                if (!col.TryGetComponent(out TargetUnit tu)) continue;

                ActionEngine_Unit unit = tu.GetUnit();
                if (!m_DetectedUnits.Contains(unit))
                    m_DetectedUnits.Add(unit);
            }
        }

        /// <summary>
        /// 将挂点变换与蓝图偏移合并，输出世界空间中心点和朝向。
        /// offsetPoint.pos 视为挂点本地空间偏移，offsetPoint.rot 视为本地旋转偏移。
        /// </summary>
        /// <summary>
        /// 基于挂点 Transform 计算带偏移的世界坐标。仅用于非技能实体（非 IsTem）的正常单位挂点。
        /// </summary>
        private void ApplyOffset(Transform limbTransform, ActionStatePart _part, ActionMachineTime _time,
            out Vector3 worldCenter, out Vector3 worldForward, out Vector3 worldUp)
        {
            PointData offset = m_OffsetPoint.value(_part, _time);
            worldCenter = limbTransform.TransformPoint(offset.pos);
            Quaternion worldRot = limbTransform.rotation * offset.rot;
            worldForward = worldRot * Vector3.forward;
            worldUp = worldRot * Vector3.up;
        }

        /// <summary>
        /// 基于世界坐标 + 旋转计算带偏移的世界坐标。
        /// 用于技能实体环境（IsTem=true）或编辑器无挂点时（characterConfig==null），
        /// 此时以 ActionStatePart.Pos/Rot 作为基础变换。
        /// </summary>
        private void ApplyOffset(Vector3 basePos, Quaternion baseRot, ActionStatePart _part, ActionMachineTime _time,
            out Vector3 worldCenter, out Vector3 worldForward, out Vector3 worldUp)
        {
            PointData offset = m_OffsetPoint.value(_part, _time);
            worldCenter = basePos + baseRot * offset.pos;
            Quaternion worldRot = baseRot * offset.rot;
            worldForward = worldRot * Vector3.forward;
            worldUp = worldRot * Vector3.up;
        }

        private static void DrawCapsuleShape(Vector3 center, Vector3 up, float radius, float height, Color color)
        {
            float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
            EngineScenceDraw.Capsule(
                center + up * halfSegment,
                center - up * halfSegment,
                radius, color);
        }

        private static void DrawSectorShape(Vector3 center, Vector3 forward, float radius,
            float halfAngle, float sectorHeight, Color color)
        {
            float halfH = sectorHeight * 0.5f;
            Vector3 flatForward = new Vector3(forward.x, 0f, forward.z).normalized;
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.forward;

            Vector3 startDir = Quaternion.AngleAxis(-halfAngle, Vector3.up) * flatForward;
            Vector3 endDir = Quaternion.AngleAxis(halfAngle, Vector3.up) * flatForward;

            Vector3 topCenter = center + Vector3.up * halfH;
            Vector3 botCenter = center - Vector3.up * halfH;

            EngineScenceDraw.WireArc(topCenter, Vector3.up, startDir, halfAngle * 2f, radius, color);
            EngineScenceDraw.WireArc(botCenter, Vector3.up, startDir, halfAngle * 2f, radius, color);

            Vector3 topLeft = topCenter + startDir * radius;
            Vector3 topRight = topCenter + endDir * radius;
            Vector3 botLeft = botCenter + startDir * radius;
            Vector3 botRight = botCenter + endDir * radius;

            EngineScenceDraw.Line(topCenter, topLeft, color);
            EngineScenceDraw.Line(topCenter, topRight, color);
            EngineScenceDraw.Line(botCenter, botLeft, color);
            EngineScenceDraw.Line(botCenter, botRight, color);

            EngineScenceDraw.Line(topCenter, botCenter, color);
            EngineScenceDraw.Line(topLeft, botLeft, color);
            EngineScenceDraw.Line(topRight, botRight, color);
        }
    }
}
