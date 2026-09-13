using System;
using AsiActionEngine.RunTime.Graph;
using AsiActionEngine.RunTime.GraphVal;
using AsiTimeLine.RunTime;
using UnityEngine;

namespace AsiActionEngine.RunTime
{

    [System.Serializable]
    public class Event_HitBox_Capsule : IActionEventData
    {
        [SerializeField] protected GraphEvent_NoValue_Unit mHiter = new GraphEvent_NoValue_Unit();
        [SerializeField] protected int mColliderLaye = 0;
        [SerializeField] protected bool mIsTrigger = false;
        [SerializeField] protected GraphEvent_NoValue_Point mCenterPos = new GraphEvent_NoValue_Point(new GraphEvent_TrackData_ActionStatePartPoint());
        [SerializeField] protected float mHeight = 2.0f;
        [SerializeField] protected float mRadius = 0.5f;

        #region property
        [EditorProperty("受击盒持有者", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Unit Hiter
        {
            get { return mHiter; }
            set { mHiter = value; }
        }
        [EditorProperty("受击盒所属层级", EditorPropertyType.EEPT_ObjectLayer)]
        public int LayerMask
        {
            get { return mColliderLaye; }
            set { mColliderLaye = value; }
        }
        [EditorProperty("IsTrigger(关闭物理碰撞)", EditorPropertyType.EEPT_Bool, LabelWidth = 150)]
        public bool IsTrigger
        {
            get { return mIsTrigger; }
            set { mIsTrigger = value; }
        }
        [EditorProperty("受击盒中心位置", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Point CenterPos
        {
            get { return mCenterPos; }
            set { mCenterPos = value; }
        }
        [EditorProperty("受击盒高度", EditorPropertyType.EEPT_Float)]
        public float Height
        {
            get { return mHeight; }
            set { mHeight = value; }
        }
        [EditorProperty("受击盒半径", EditorPropertyType.EEPT_Float)]
        public float Radius
        {
            get { return mRadius; }
            set { mRadius = value; }
        }
        #endregion

        public int GetEvenType() => (int)EEvenType.EET_HitkBox_Capsule;

        public IActionEventData Creact() => new Event_HitBox_Capsule();

        [NonSerialized] private bool mIsValid = false;
        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;

            TargetUnit unit = Hiter.value(_actionState, EngineResourcesManager.Instance.MachineTime);
            mIsValid = unit is not null;

            if (!mIsValid) return;
            _stateMachine.TryGetStaticLogic(out Ex_BoxCollider _colliderEvent, nameof(Ex_BoxCollider));
            CapsuleCollider _collider = _colliderEvent.GetCapsule(_actionState.GetHashCode(), unit.GetUnit());
            PointData _pointData = mCenterPos.value(_actionState, EngineResourcesManager.Instance.MachineTime);

            _collider.gameObject.layer = mColliderLaye;
            _collider.isTrigger = mIsTrigger;
            _collider.transform.SetPositionAndRotation(_pointData.pos, _pointData.rot);
            _collider.height = mHeight;
            _collider.radius = Radius;
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (!mIsValid) return;

            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;

            TargetUnit unit = Hiter.value(_actionState, EngineResourcesManager.Instance.MachineTime);
            _stateMachine.TryGetStaticLogic(out Ex_BoxCollider _colliderEvent, nameof(Ex_BoxCollider));
            CapsuleCollider _collider = _colliderEvent.GetCapsule(_actionState.GetHashCode(), unit.GetUnit());
            PointData _pointData = mCenterPos.value(_actionState, EngineResourcesManager.Instance.MachineTime);

            _collider.gameObject.layer = mColliderLaye;
            _collider.isTrigger = mIsTrigger;
            _collider.transform.SetPositionAndRotation(_pointData.pos, _pointData.rot);
            _collider.height = mHeight;
            _collider.radius = Radius;
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            if (!mIsValid) return;

            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            _stateMachine.TryGetStaticLogic(out Ex_BoxCollider _colliderEvent, nameof(Ex_BoxCollider));
            _colliderEvent.DestoryCapsuleCollider(_actionState.GetHashCode());
        }
        public void EditorDraw(CharacterConfig characterConfig, ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (_actionTime.IsInRange)
            {
                ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
                PointData _pointData = mCenterPos.value(_actionState, EngineResourcesManager.Instance.MachineTime);

                Vector3 _dir = (_pointData.rot * Vector3.up * (mHeight * 0.5f - mRadius));
                Vector3 _pos1 = _pointData.pos - _dir;
                Vector3 _pos2 = _pointData.pos + _dir;
                EngineDebug.DrawCapsule(_pos1, _pos2, mRadius, Color.blue);
                //EngineDebug.DrawBox(Vector3.zero, Quaternion.identity, Scale.GetValue(), Color.blue);
            }
        }
        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_HitBox_Capsule _event = _eventData as Event_HitBox_Capsule;
            _event.Hiter = mHiter.Clone();
            _event.mColliderLaye = mColliderLaye;
            _event.IsTrigger = mIsTrigger;
            _event.CenterPos = mCenterPos.Clone();
            _event.Radius = mRadius;
            _event.Height = mHeight;
            return _event;
        }
    }
}