using System;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_SkillEmitter : IActionEventData
    {
        [SerializeField] protected bool mIsAttackHit = false;

        [SerializeField] protected bool mUseBluePrint = false;
        [SerializeField] protected GraphEvent_NoValue_Int mBluePrintInt = new GraphEvent_NoValue_Int();
        [SerializeField] protected int mSkillID;
        [SerializeField] protected EVector3 mOffsetPos;
        [SerializeField] protected EVector3 mOffsetRot;
        [SerializeField] protected GInt mEmissionRate = new GInt(1);
        [SerializeField] protected GraphEvent_NoValue_Point mCenterPoint = new GraphEvent_NoValue_Point();

        #region property
        [EditorProperty("使用蓝图代替ActionID", EditorPropertyType.EEPT_Bool, LabelWidth = 150)]
        public bool UseBluePrint
        {
            get { return mUseBluePrint; }
            set { mUseBluePrint = value; }
        }
        [EditorProperty("发射的技能ActionID", EditorPropertyType.EEPT_GraphValue, LabelWidth = 150)]
        public GraphEvent_NoValue_Int BluePrintInt
        {
            get { return mBluePrintInt; }
            set { mBluePrintInt = value; }
        }
        [EditorProperty("发射的技能ActionID", EditorPropertyType.EEPT_SkillAction, LabelWidth = 150)]
        public int SkillID
        {
            get { return mSkillID; }
            set { mSkillID = value; }
        }
        [EditorProperty("发射数量", EditorPropertyType.EEPT_GInt)]
        public GInt EmissionRate
        {
            get { return mEmissionRate; }
            set { mEmissionRate = value; }
        }
        //[EditorProperty("攻击命中时发射", EditorPropertyType.EEPT_Bool)]
        //public bool IsAttackHit
        //{
        //    get { return mIsAttackHit; }
        //    set { mIsAttackHit = value; }
        //}
        [EditorProperty("发射位置", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Point CenterPoint
        {
            get { return mCenterPoint; }
            set { mCenterPoint = value; }
        }

        [EditorProperty("偏移位置", EditorPropertyType.EEPT_Vector3)]
        public EVector3 OffsetPos
        {
            get { return mOffsetPos; }
            set { mOffsetPos = value; }
        }
        [EditorProperty("偏移角度", EditorPropertyType.EEPT_Vector3)]
        public EVector3 OffsetRot
        {
            get { return mOffsetRot; }
            set { mOffsetRot = value; }
        }

        #endregion
        public int GetEvenType() => -(int)EEvenTypeInternal.EET_SkillEmitter;

        public IActionEventData Creact() => new Event_SkillEmitter();

        [NonSerialized] private float mInterval;
        [NonSerialized] private int mLoopMax;//Loop总数
        [NonSerialized] private int mLoopIndex;//LoopID
        [NonSerialized] private int mLoopIndex_last;//上一次的LoopID
        [NonSerialized] private ActionStatePart mActionState = null;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            mActionState = _actionState;
            //if (mIsAttackHit)
            //{
            //    _actionState.OnHit += AttackCallback;
            //    return;
            //}
            mLoopMax = mEmissionRate.GetValue(_actionState);

            if (mLoopMax < 1) return;

            mLoopIndex = 0;
            mLoopIndex_last = 0;
            mInterval = (float)1 / mLoopMax;
            if (_isSingle)
            {
                //单帧下, 一口气全部发射
                for (int i = 0; i < mLoopMax; i++)
                {
                    SpawnSkillAction(_actionState, new ActionMachineTime(0, 0, 0, 0), i);
                }
                //EngineDebug.Log("Part数量: " + _actionState.ActionStateMachine.AllActionStatePart_Tmp.Count);
            }
            else
            {
                SpawnSkillAction(_actionState, new ActionMachineTime(0, 0, 0, 0), 0);
                mLoopIndex_last++;
            }
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (mIsAttackHit)
            {
                return;
            }

            if (mLoopMax < 1) return;

            //EngineDebug.Log("事件执行的百分比: " + _actionTime.GetPercentage());
            mLoopIndex = (int)(_actionTime.GetPercentage() / mInterval);

            SpawnSkillAction(_actionState, _actionTime);
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            //if (mIsAttackHit)
            //{
            //    _actionState.OnHit -= AttackCallback;
            //    return;
            //}

            if (mLoopMax < 1) return;

            if (!_interruot)
            {
                mLoopIndex = mLoopMax;
                SpawnSkillAction(_actionState, new ActionMachineTime(0, 0, 0, 0));
            }
        }

        private void SpawnSkillAction(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (mLoopIndex_last < mLoopIndex)
            {
                for (int i = mLoopIndex_last; i < mLoopIndex; i++)
                {
                    SpawnSkillAction(_actionState, _actionTime, i);
                }
                mLoopIndex_last = mLoopIndex;
            }
        }

        private ActionStatePart SpawnSkillAction(ActionStatePart _actionState, ActionMachineTime _actionTime, int _id)
        {
            _actionState.LoopMax = mLoopMax;
            _actionState.LoopIndex = _id;

            //_actionState.SetRot(_actionState.Master.Rot);
            //_actionState.SetPos(_actionState.Master.Pos);

            PointData _center = mCenterPoint.value(_actionState, _actionTime);

            Quaternion _offsetRot = Quaternion.Euler(OffsetRot.GetValue());
            Quaternion _targetRot = _center.rot * _offsetRot;
            Vector3 _offsetPos = _targetRot * OffsetPos.GetValue();

            int _instanceID = mSkillID;
            if (mUseBluePrint) _instanceID = mBluePrintInt.value(_actionState, _actionTime);
            return _actionState.ActionStateMachine.InitState(_instanceID, _actionState, _center.pos + _offsetPos, _targetRot);
            //EngineDebug.Log("发射子弹: " + _id);
        }

        private void AttackCallback(ActionEngine_Unit _SelfUnit, bool _isUnit, GameObject _gameObject, ActionEngine_Unit _onHiter)
        {
            if (mLoopIndex_last < mLoopIndex)
            {
                for (int i = mLoopIndex_last; i < mLoopIndex; i++)
                {
                    ActionStatePart _part = SpawnSkillAction(mActionState, new ActionMachineTime(0, 0, 0, 0), i);
                }
                mLoopIndex_last = mLoopIndex;
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SkillEmitter eventCast = _eventData as Event_SkillEmitter;
            //eventCast.IsAttackHit = mIsAttackHit;
            eventCast.SkillID = mSkillID;
            eventCast.OffsetPos = mOffsetPos;
            eventCast.OffsetRot = mOffsetRot;
            eventCast.UseBluePrint = mUseBluePrint;
            eventCast.BluePrintInt = mBluePrintInt.Clone();

            eventCast.EmissionRate = (GInt)mEmissionRate.Clone();
            eventCast.CenterPoint = mCenterPoint.Clone();

            return eventCast;
        }
    }
}