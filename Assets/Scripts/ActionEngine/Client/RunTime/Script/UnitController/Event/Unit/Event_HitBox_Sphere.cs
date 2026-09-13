using System;
using AsiActionEngine.RunTime.Graph;
using AsiActionEngine.RunTime.GraphVal;
using AsiTimeLine.RunTime;
using UnityEngine;

namespace AsiActionEngine.RunTime
{

    [System.Serializable]
    public class Event_HitBox_Sphere : IActionEventData
    {
        [SerializeField] protected GraphEvent_NoValue_Unit mHiter = new GraphEvent_NoValue_Unit();
        [SerializeField] protected int mColliderLaye = 0;
        [SerializeField] protected bool mIsTrigger = false;
        [SerializeField] protected GraphEvent_NoValue_Point mCenterPos = new GraphEvent_NoValue_Point(new GraphEvent_TrackData_ActionStatePartPoint());
        [SerializeField] protected bool mUseBluePrint_Scale = false;
        [SerializeField] protected GraphEvent_NoValue_Float mBluePrint_Scale = new GraphEvent_NoValue_Float(1);
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
        [EditorProperty("使用蓝图控制半径", EditorPropertyType.EEPT_Bool)]
        public bool UseBluePrint_Scale
        {
            get { return mUseBluePrint_Scale; }
            set { mUseBluePrint_Scale = value; }
        }
        [EditorProperty("半径", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Float BluePrint_Scale
        {
            get
            {
#if UNITY_EDITOR
                if (mBluePrint_Scale is null) mBluePrint_Scale = new GraphEvent_NoValue_Float(1);
#endif
                return mBluePrint_Scale;
            }
            set { mBluePrint_Scale = value; }
        }
        [EditorProperty("受击盒半径", EditorPropertyType.EEPT_Float)]
        public float Radius
        {
            get { return mRadius; }
            set { mRadius = value; }
        }
        #endregion

        public int GetEvenType() => (int)EEvenType.EET_HitkBox_Sphere;

        public IActionEventData Creact() => new Event_HitBox_Sphere();

        [NonSerialized] private bool mIsValid = false;
        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;

            TargetUnit unit = Hiter.value(_actionState, EngineResourcesManager.Instance.MachineTime);
            mIsValid = unit is not null;

            if (!mIsValid) return;

            //if(unit.TargetUnit() != _actionState.ActionStateMachine.CurUnit)
            //{
            //    Debug.LogError("创建的受击盒所有者不是自身!!!");
            //}
            _stateMachine.TryGetStaticLogic(out Ex_BoxCollider _colliderEvent, nameof(Ex_BoxCollider));
            SphereCollider _collider = _colliderEvent.GetSphere(_actionState.GetHashCode(), unit.GetUnit());
            PointData _pointData = mCenterPos.value(_actionState, EngineResourcesManager.Instance.MachineTime);

            _collider.gameObject.layer = mColliderLaye;
            _collider.isTrigger = mIsTrigger;
            _collider.transform.SetPositionAndRotation(_pointData.pos, _pointData.rot);
            if (UseBluePrint_Scale)
            {
                float _scale = BluePrint_Scale.value(_actionState, EngineResourcesManager.Instance.MachineTime);
                _collider.radius = _scale;
            }
            else
            {
                _collider.radius = mRadius;
            }
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (!mIsValid) return;

            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;

            TargetUnit unit = Hiter.value(_actionState, EngineResourcesManager.Instance.MachineTime);
            _stateMachine.TryGetStaticLogic(out Ex_BoxCollider _colliderEvent, nameof(Ex_BoxCollider));
            SphereCollider _collider = _colliderEvent.GetSphere(_actionState.GetHashCode(), unit.GetUnit());
            PointData _pointData = mCenterPos.value(_actionState, EngineResourcesManager.Instance.MachineTime);

            _collider.gameObject.layer = mColliderLaye;
            _collider.isTrigger = mIsTrigger;
            _collider.transform.SetPositionAndRotation(_pointData.pos, _pointData.rot);
            if (UseBluePrint_Scale)
            {
                float _scale = BluePrint_Scale.value(_actionState, _actionTime);
                _collider.radius = _scale;
            }
            else
            {
                _collider.radius = mRadius;
            }
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            if (!mIsValid) return;

            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            _stateMachine.TryGetStaticLogic(out Ex_BoxCollider _colliderEvent, nameof(Ex_BoxCollider));
            _colliderEvent.DestorySphereCollider(_actionState.GetHashCode());
        }
        public void EditorDraw(CharacterConfig characterConfig, ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (_actionTime.IsInRange)
            {
                ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
                PointData _pointData = mCenterPos.value(_actionState, EngineResourcesManager.Instance.MachineTime);
                EngineDebug.DrawSphere(_pointData.pos, mRadius, Color.blue);
                //EngineDebug.DrawBox(Vector3.zero, Quaternion.identity, Scale.GetValue(), Color.blue);
            }
        }
        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_HitBox_Sphere _event = _eventData as Event_HitBox_Sphere;
            _event.Hiter = mHiter.Clone();
            _event.mColliderLaye = mColliderLaye;
            _event.IsTrigger = mIsTrigger;
            _event.CenterPos = mCenterPos.Clone();
            _event.Radius = mRadius;
            _event.UseBluePrint_Scale = mUseBluePrint_Scale;
            _event.mBluePrint_Scale = BluePrint_Scale.Clone();
            return _event;
        }
    }
}