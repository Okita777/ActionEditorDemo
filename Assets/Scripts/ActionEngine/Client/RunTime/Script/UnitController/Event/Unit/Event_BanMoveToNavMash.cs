using System;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_BanMoveToNavMash : IActionEventData
    {
        [SerializeField] protected float m_MinRadius = 0.5f;
        [SerializeField] protected float m_SelfDistance = 1;
        [SerializeField] protected bool m_IsDraw = false;

        #region Property
        [EditorProperty("最小检查半径: ", EditorPropertyType.EEPT_Float)]
        public float MinRadius
        {
            get { return m_MinRadius; }
            set { m_MinRadius = value; }
        }
        [EditorProperty("目标位置检索的安全距离: ", EditorPropertyType.EEPT_Float, LabelWidth = 160)]
        public float SelfDistance
        {
            get { return m_SelfDistance; }
            set { m_SelfDistance = value; }
        }
        [EditorProperty("绘制图形(Editor有效): ", EditorPropertyType.EEPT_Bool, LabelWidth = 160)]
        public bool IsDraw
        {
            get { return m_IsDraw; }
            set { m_IsDraw = value; }
        }
        #endregion
        [NonSerialized] private ActionStateMachine mStateMachine;
        [NonSerialized] private Vector3 mFixVector;


        public int GetEvenType() => (int)EEvenType.EET_BanMoveToNavMash;

        public IActionEventData Creact() => new Event_BanMoveToNavMash();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            mStateMachine = _actionState.ActionStateMachine;
            mStateMachine.TryGetLogic(out Ex_Update_CharacterControl _control, nameof(Ex_Update_CharacterControl));
            _control.MoveCorrection = ReSetMove;
        }
        //public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)//deltaTime(ms)
        //{
        //}
        public void Exit(ActionStatePart _actionState, bool _interruot)//interruot 是否因打断轨退出  false代表事件自然结束
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            _stateMachine.TryGetLogic(out Ex_Update_CharacterControl _control, nameof(Ex_Update_CharacterControl));
            if (_control.MoveCorrection == ReSetMove)
            {
                _control.MoveCorrection = null;
            }
        }

        public void EditorDraw(CharacterConfig characterConfig, ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (IsDraw)
            {
                ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
                Vector3 _centerPos = _stateMachine.CurUnit.transform.position;
                EngineScenceDraw.WireDisc(_centerPos, Vector3.up, MinRadius, Color.black);
                EngineScenceDraw.Line(_centerPos, _centerPos + mFixVector, Color.black);
            }
        }

        private Vector3 ReSetMove(Vector3 _orgMove)
        {
            mFixVector = _orgMove;
            //if ((mFixVector).sqrMagnitude < 0.00000001f) return _orgMove;
            mStateMachine.TryGetStaticLogic(out Ex_NavMash _value, nameof(Ex_NavMash));
            float checkDis = MinRadius + SelfDistance;
            if (mFixVector.sqrMagnitude < ((checkDis * checkDis))) mFixVector = mFixVector.normalized * (MinRadius + SelfDistance);
            if (_value.CheckPointToNavMash(mFixVector + mStateMachine.CurUnit.transform.position, SelfDistance)) return _orgMove;
            return Vector3.zero;
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_BanMoveToNavMash _event = _eventData as Event_BanMoveToNavMash;

            _event.MinRadius = m_MinRadius;
            _event.IsDraw = m_IsDraw;
            _event.m_SelfDistance = m_SelfDistance;

            return _event;
            // throw new System.NotImplementedException();
        }
    }
}