using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    //将骨骼固定到空间
    [System.Serializable]
    public class Event_FixationBoneToSpace : IActionEventData
    {
        #region Enum
        public enum ETriggerCondition
        {
            不使用触发条件,
            使用射线对比地面高度,
            参考挂点对比挂点高度
        }
        #endregion
        #region Struct

        [System.Serializable]
        public struct FixationBoneData
        {
            [SerializeField] public float m_TriggerHeight;//触发高度
            [SerializeField] public int m_FixationBoneID;//要固定的骨骼的挂点ID

            [System.NonSerialized] public bool m_IsValid;//是否有效
            [System.NonSerialized] public bool m_IsTriggered;//是否触发过
            [System.NonSerialized] public Transform m_FixationBone;//要固定的骨骼
            [System.NonSerialized] public EVector3 m_FixationPosition;//要固定的世界空间
            [System.NonSerialized] public EVector3 m_TargetEulerAngles;//要固定的世界欧拉角
        }
        #endregion
        [SerializeField] ETriggerCondition m_TriggerCondition;

        #region Property
        [EditorProperty("触发条件", EditorPropertyType.EEPT_Enum)]
        public ETriggerCondition TriggerCondition
        {
            get { return m_TriggerCondition; }
            set { m_TriggerCondition = value; }
        }

        #endregion
        public int GetEvenType() => 1;
        public IActionEventData Creact() => new Event_FixationBoneToSpace();


        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            _actionState.ActionStateMachine.RootWeight = Vector3.zero;
        }

        public void LateUpdate(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {

        }
        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            _actionState.ActionStateMachine.RootWeight = Vector3.one;
        }
        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_FixationBoneToSpace _eventDataClone = (Event_FixationBoneToSpace)_eventData;
            return _eventDataClone;
        }
    }
}