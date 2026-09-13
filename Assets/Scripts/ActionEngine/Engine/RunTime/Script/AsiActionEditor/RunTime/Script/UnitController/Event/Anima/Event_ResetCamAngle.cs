using System;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_ResetCamAngle : IActionEventData
    {
        [SerializeField] private GValue_Ratio m_GValueRatio = new GValue_Ratio();
        [SerializeField] private int m_ReferPointID = 0;
        [SerializeField] private byte m_AxisType = 2;
        [SerializeField] private float m_AxisX = 0;
        [SerializeField] private float m_AxisY = 0;

        #region property
        [EditorProperty("重置条件", EditorPropertyType.EEPT_GValueSRatio)]
        public GValue_Ratio GValueRatio
        {
            get { return m_GValueRatio; }
            set { m_GValueRatio = value; }
        }
        [EditorProperty("轴向参考挂点", EditorPropertyType.EEPT_CharacteLimbType)]
        public int ReferPointID
        {
            get { return m_ReferPointID; }
            set { m_ReferPointID = value; }
        }
        [EditorProperty("重置轴向", EditorPropertyType.EEPT_Enum, EnumNames = new[] { "XY", "X", "Y" })]
        public byte AxisType
        {
            get { return m_AxisType; }
            set { m_AxisType = value; }
        }

        [EditorProperty("X", EditorPropertyType.EEPT_Float)]
        public float AxisX
        {
            get { return m_AxisX; }
            set { m_AxisX = value; }
        }
        [EditorProperty("Y", EditorPropertyType.EEPT_Float)]
        public float AxisY
        {
            get { return m_AxisY; }
            set { m_AxisY = value; }
        }
        #endregion

        [NonSerialized] private bool defaultActive = false;
        public int GetEvenType() => -(int)EEvenTypeInternal.Event_ResetCamAngle;
        public IActionEventData Creact() => new Event_ResetCamAngle();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine stateMachine = _actionState.ActionStateMachine;
            if (m_GValueRatio.CheckValue(stateMachine))
            {
                if (stateMachine.TryGetCharacterLimb(
                        (ECharacteLimbType)ReferPointID,
                        out Transform transform))
                {
                    Quaternion rot = transform.rotation * Quaternion.Euler(m_AxisX, m_AxisY, 0);
                    if (AxisType == 0)
                    {
                        stateMachine.SetMouseXY(rot);
                    }
                    else if (AxisType == 1)
                    {
                        float nowRot = stateMachine.GetCamPointRot().eulerAngles.y;
                        stateMachine.SetMouseXY(Quaternion.Euler(rot.eulerAngles.x, nowRot, 0));
                    }
                    else
                    {
                        float nowRot = stateMachine.GetCamPointRot().eulerAngles.x;
                        stateMachine.SetMouseXY(Quaternion.Euler(nowRot, rot.eulerAngles.y, 0));
                    }
                }
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_ResetCamAngle _event = _eventData as Event_ResetCamAngle;

            _event.GValueRatio = m_GValueRatio.Clone();
            _event.ReferPointID = m_ReferPointID;
            _event.AxisType = m_AxisType;
            _event.AxisX = m_AxisX;
            _event.AxisY = m_AxisY;

            return _event;
        }
    }
}