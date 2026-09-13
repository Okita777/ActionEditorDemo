using System;
using AsiActionEngine.RunTime.Graph;
using AsiActionEngine.RunTime.GraphVal;
using AsiTimeLine.RunTime;
using UnityEngine;

namespace AsiActionEngine.RunTime
{

    [System.Serializable]
    public class Event_HitBox_Box : IActionEventData
    {
        [SerializeField] protected GraphEvent_NoValue_Unit mHiter = new GraphEvent_NoValue_Unit();
        [SerializeField] protected int mColliderLaye = 0;
        [SerializeField] protected bool mIsTrigger = false;
        [SerializeField] protected GraphEvent_NoValue_Point mCenterPos = new GraphEvent_NoValue_Point(new GraphEvent_TrackData_ActionStatePartPoint());
        [SerializeField] protected EVector3 mScale = new EVector3(1, 1, 1);

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
        [EditorProperty("受击盒大小", EditorPropertyType.EEPT_Vector3)]
        public EVector3 Scale
        {
            get { return mScale; }
            set { mScale = value; }
        }
        #endregion

        public int GetEvenType() => (int)EEvenType.EET_HitkBox_Box;

        public IActionEventData Creact() => new Event_HitBox_Box();

        [NonSerialized] private bool mIsValid = false;
        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;

            TargetUnit unit = Hiter.value(_actionState, EngineResourcesManager.Instance.MachineTime);
            mIsValid = unit is not null;

            if (!mIsValid) return;
            _stateMachine.TryGetStaticLogic(out Ex_BoxCollider _colliderEvent, nameof(Ex_BoxCollider));
            BoxCollider _collider = _colliderEvent.GetBox(_actionState.GetHashCode(), unit.GetUnit());
            PointData _pointData = mCenterPos.value(_actionState, EngineResourcesManager.Instance.MachineTime);

            _collider.gameObject.layer = mColliderLaye;
            _collider.isTrigger = mIsTrigger;
            _collider.transform.SetPositionAndRotation(_pointData.pos, _pointData.rot);
            _collider.size = mScale.GetValue();
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (!mIsValid) return;

            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;

            TargetUnit unit = Hiter.value(_actionState, EngineResourcesManager.Instance.MachineTime);
            _stateMachine.TryGetStaticLogic(out Ex_BoxCollider _colliderEvent, nameof(Ex_BoxCollider));
            BoxCollider _collider = _colliderEvent.GetBox(_actionState.GetHashCode(), unit.GetUnit());
            PointData _pointData = mCenterPos.value(_actionState, EngineResourcesManager.Instance.MachineTime);

            _collider.gameObject.layer = mColliderLaye;
            _collider.isTrigger = mIsTrigger;
            _collider.transform.SetPositionAndRotation(_pointData.pos, _pointData.rot);
            _collider.size = mScale.GetValue();
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            if (!mIsValid) return;

            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            _stateMachine.TryGetStaticLogic(out Ex_BoxCollider _colliderEvent, nameof(Ex_BoxCollider));
            _colliderEvent.DestoryBoxCollider(_actionState.GetHashCode());
        }

        public void EditorDraw(CharacterConfig characterConfig, ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            //ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            //PointData _pointData = mCenterPos.value(_actionState, EngineResourcesManager.Instance.MachineTime);
            //EngineDebug.DrawBox(_pointData.pos, _pointData.rot, Scale.GetValue(), Color.blue);
            if (_actionTime.IsInRange)
            {
                ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
                PointData _pointData = mCenterPos.value(_actionState, EngineResourcesManager.Instance.MachineTime);
                EngineDebug.DrawBox(_pointData.pos, _pointData.rot, Scale.GetValue(), Color.blue);
                //EngineDebug.DrawBox(Vector3.zero, Quaternion.identity, Scale.GetValue(), Color.blue);
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_HitBox_Box _event = _eventData as Event_HitBox_Box;
            _event.Hiter = mHiter.Clone();
            _event.mColliderLaye = mColliderLaye;
            _event.IsTrigger = mIsTrigger;
            _event.CenterPos = mCenterPos.Clone();
            _event.Scale = mScale;
            return _event;
        }
    }
}