using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_Cast_Ray : IActionEventData
    {
        [SerializeField] protected RayCastData m_RayCastData = new RayCastData();
        [SerializeField] protected GValue_Ratio gValue_Ratio = new GValue_Ratio();
        public RayCastData RayCastData
        {
            get { return m_RayCastData; }
            set { m_RayCastData = value; }
        }
        [EditorProperty("有效射线", EditorPropertyType.EEPT_GValueSRatio)]
        public GValue_Ratio Value_Ratio
        {
            get { return gValue_Ratio; }
            set { gValue_Ratio = value; }
        }
        public int GetEvenType() => -(int)EEvenTypeInternal.EET_RayCast;
        public IActionEventData Creact() => new Event_Cast_Ray();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            // if (_isSingle)
            {
                m_RayCastData.OnCheck(_actionState, new ActionMachineTime(0, 0, 0, 0));
            }
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            m_RayCastData.OnCheck(_actionState, _actionTime);
        }

        public void EditorDraw(CharacterConfig characterConfig, ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (_actionTime.IsInRange)
            {
                // SelectTransform _selectTransform = m_RayCastData.m_SelectTransform;
                // Transform _referTrans = characterConfig.transform;
                // if (_selectTransform.m_IsCharacterLimb)
                //     characterConfig.HelpPointDic.TryGetValue((ECharacteLimbType)_selectTransform.m_Value,
                //         out _referTrans);
                //todo: 处理下ActionStatePart
                if (_actionState is null) return;

                PointData pointData = m_RayCastData.m_point.value(_actionState, _actionTime);
                float length = m_RayCastData.m_length.value(_actionState, _actionTime);

                Vector3 _startPos = pointData.pos;
                Vector3 _dir = pointData.rot * Vector3.forward;
                Vector3 _endPos = _dir * length + _startPos;

                int layermask = _actionState.ActionStateMachine.GetLayer(RayCastData.m_LayerMask);
                if (Physics.Raycast(_startPos, _dir, out RaycastHit _hit, length, layermask,
                        QueryTriggerInteraction.Ignore))
                {
                    EngineDebug.DrawLine(_startPos, _hit.point, Color.red);
                    EngineDebug.DrawSphere(_hit.point, 0.01f, Color.cyan);
                    EngineDebug.DrawLine(_endPos, _hit.point, Color.green);
                }
                else
                {
                    EngineDebug.DrawLine(_startPos, _endPos, Color.red);
                }
                // throw new System.NotImplementedException();
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_Cast_Ray eventCast = _eventData as Event_Cast_Ray;
            eventCast.RayCastData = m_RayCastData.Clone();
            eventCast.Value_Ratio = gValue_Ratio.Clone();
            return eventCast;
        }
    }
}