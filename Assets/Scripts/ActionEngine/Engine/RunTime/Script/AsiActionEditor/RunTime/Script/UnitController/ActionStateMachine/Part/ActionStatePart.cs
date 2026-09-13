using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    // private delegate 
    public partial class ActionStatePart
    {
        public readonly int mCurActionLayer;

        public bool ActionEnble
        {
            get { return mActionEnble; }
            private set { mActionEnble = value; }
        }//当前状态机是否在运行
        public bool IsTem => mIsTem;
        public int LoopIndex = 0;//遍历ID
        public int LoopMax = 0;//遍历总数
        public float ElapsedTime;//当前状态已经经过的时间 毫秒

        /// <summary>本帧推进的时长(毫秒)，已含状态机 TimeScale。需要自行累计时间的逻辑取此值。</summary>
        public float DeltaTime => mActionStateMachine.DeltaTime * MotionEngineConst.TimeDoubling;
        public Vector3 Pos { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Quaternion Rot { get; private set; }
        public float Gravity { get { return mGvavity; } set { mGvavity = value; } }
        public void SetPos(Vector3 _pos) { Pos = _pos; }
        public void SetRot(Quaternion _rot)
        {
            Rot = _rot;
        }
        public void Move(Vector3 _velocity) => OnMove(_velocity);
        public void TransLate(Vector3 _velocity) => OnTransLate(_velocity);
        public List<GameObject> AllHitObject => mAllHitObject;
        //public void Rotate(Quaternion _rot, bool _global) => OnRotate(_rot, _global);


        public ActionStatePart Master => mMaster;
        public float ElapsedTime_last => mElapsedTime_last;//切换Action时，上一个Action的时间

        public ActionState CurrentActionState { get; set; }//当前执行的主要事件
        public ActionState CurrentActionState_Last { get; private set; }//切换Action时，上一个Action
        public ActionState CurrentActionState_ChangeSour { get; private set; }//切换Action时，发出切换指令的Action
        private int mActiveActionGroupID = -1;//当前Action所属的ActionGroupID
        private bool mHasInterruptTargetActionGroupID = false;
        private int mInterruptTargetActionID = -1;//跳转轨条件检查期间的目标ActionID
        private int mInterruptTargetActionGroupID = -1;//跳转轨条件检查期间的目标ActionGroupID
        private bool mHasInterruptTargetActionOverride = false;
        private int mInterruptTargetOverrideActionID = -1;//跳转轨条件检查期间被条件替换后的ActionID
        private int mInterruptTargetOverrideActionGroupID = -1;//跳转轨条件检查期间被条件替换后的ActionGroupID

        public ActionStateMachine ActionStateMachine => mActionStateMachine;
        public List<ActionEvent> CurrentActionEvents => mCurrentActionEvents;
        public List<ActionInterrupt> CurActionInterrupt => mCurActionInterrupt;
        // public Dictionary<int, ActionState> ActionStates => mActionStateMachine.ActionStates;
        // public Dictionary<string, int> ActionStateID => mActionStateMachine.ActionStateID;
        public List<int> JumpLayerList => mJumpLayerList;

        // /// <summary>
        // /// 进入Action
        // /// </summary>
        // /// <param name="_action"></param>
        // public void OnEnter(ActionState _action) => OnEnterState(_action);

        public int ActiveActionGroupID => mActiveActionGroupID;
        public bool TryGetActiveActionGroupID(out int actionGroupID)
        {
            actionGroupID = mActiveActionGroupID;
            return actionGroupID >= 0;
        }

        public bool TryGetInterruptTargetActionGroupID(out int actionGroupID)
        {
            if (mHasInterruptTargetActionOverride && mInterruptTargetOverrideActionGroupID >= 0)
            {
                actionGroupID = mInterruptTargetOverrideActionGroupID;
                return true;
            }

            actionGroupID = mInterruptTargetActionGroupID;
            return mHasInterruptTargetActionGroupID && actionGroupID >= 0;
        }

        public bool TryOverrideInterruptTargetAction(int actionID)
        {
            if (!mHasInterruptTargetActionGroupID) return false;
            if (!ActionStateMachine.TryGetActionStateWithGroupNoLog(actionID, out _, out int actionGroupID))
                return false;

            mHasInterruptTargetActionOverride = true;
            mInterruptTargetOverrideActionID = actionID;
            mInterruptTargetOverrideActionGroupID = actionGroupID;
            return true;
        }

        public void UseOriginalInterruptTargetAction()
        {
            if (!mHasInterruptTargetActionGroupID) return;
            mHasInterruptTargetActionOverride = false;
            mInterruptTargetOverrideActionID = -1;
            mInterruptTargetOverrideActionGroupID = -1;
        }

        public void SetActiveActionGroupID(int actionGroupID)
        {
            mActiveActionGroupID = actionGroupID;
        }

        public void ClearActionGroupContext()
        {
            mActiveActionGroupID = -1;
            mHasInterruptTargetActionGroupID = false;
            mInterruptTargetActionID = -1;
            mInterruptTargetActionGroupID = -1;
            mHasInterruptTargetActionOverride = false;
            mInterruptTargetOverrideActionID = -1;
            mInterruptTargetOverrideActionGroupID = -1;
        }

        private void SetInterruptTargetAction(int actionID, int actionGroupID)
        {
            mHasInterruptTargetActionGroupID = actionGroupID >= 0;
            mInterruptTargetActionID = actionID;
            mInterruptTargetActionGroupID = actionGroupID;
            mHasInterruptTargetActionOverride = false;
            mInterruptTargetOverrideActionID = -1;
            mInterruptTargetOverrideActionGroupID = -1;
        }

        private bool TryGetInterruptTargetActionOverride(out int actionID, out int actionGroupID)
        {
            actionID = mInterruptTargetOverrideActionID;
            actionGroupID = mInterruptTargetOverrideActionGroupID;
            return mHasInterruptTargetActionOverride && actionID >= 0 && actionGroupID >= 0;
        }

        /// <summary>
        /// 逻辑更新(轨道事件、跳转事件、攻击判定等)
        /// </summary>
        /// <param name="_deltaTime">单帧耗时</param>
        public void OnUpdate(float _deltaTime) => OnUpdateState(_deltaTime);
        /// <summary>
        /// 逻辑更新(轨道事件、跳转事件、攻击判定等)
        /// </summary>
        /// <param name="_deltaTime">单帧耗时</param>
        public void OnLateUpdate(float _deltaTime) => OnLateUpdateState(_deltaTime);

        /// <summary>
        /// 动画更新(更新动画、IK、注视等角色相关表现)
        /// </summary>
        /// <param name="_deltaTime">单帧耗时</param>
        public void UpdateAnima(float _deltaTime) => OnUpdateAnim(_deltaTime);

        /// <summary>
        /// 强行切换角色当前行为状态
        /// </summary>
        /// <param name="_actionName">行为状态名称</param>
        /// <param name="_mixTime">融合时间(毫秒)）</param>
        /// <param name="_offsetTime">剪切时间 (毫秒)</param>
        public bool ChangeState(string _actionName, int _mixTime = 0, int _offsetTime = 0, ActionState _changeSource = null) =>
            OnChangeState(_actionName, _mixTime, _offsetTime, _changeSource);
        public bool ChangeState(int _actionID, int _mixTime = 0, int _offsetTime = 0, ActionState _changeSource = null) =>
            OnChangeState(_actionID, _mixTime, _offsetTime, _changeSource);
        public bool ChangeState(ActionState _actionName, int _mixTime = 0, int _offsetTime = 0, ActionState _changeSource = null) =>
            OnChangeState(_actionName, _mixTime, _offsetTime, _changeSource);
        public bool ChangeState(ActionState _actionName, int _mixTime, int _offsetTime, ActionState _changeSource, int _actionGroupID) =>
            OnChangeState(_actionName, _mixTime, _offsetTime, _changeSource, false, _actionGroupID);
        /// <summary>
        /// 播放动画
        /// </summary>
        /// <param name="_name">动画名称</param>
        /// <param name="_mixTime">混合时间</param>
        /// <param name="_layer">所在层级</param>
        /// <param name="_offsetTime">剪切时间</param>
        public void PlayAnim(string _name, float _mixTime, int _layer, float _offsetTime) =>
            OnPlayAnim(_name, _mixTime, _layer, _offsetTime);

        /// <summary>
        /// 设置状态机运行状态
        /// </summary>
        /// <param name="_enble"></param>
        public void SetActionEnble(bool _enble = true)
        {
            ActionEnble = _enble;
            if (!_enble) OnClearAllEvent();
        }

        /// <summary>
        /// 获取状态层级
        /// </summary>
        /// <param name="_action">状态</param>
        /// <returns></returns>
        public string GetActionType(ActionState _action) =>
            mActionStateMachine.ActionStateInfo.mActionType[_action.ActionLable];
        //public string GetActionType() => 
        //    mActionStateMachine.ActionStateInfo.mActionType[CurrentActionState.ActionLable];
        public string GetActionType()
        {
            //Debug.Log($"Length: [{mActionStateMachine.ActionStateInfo.mActionType.Count}]   Get:[{CurrentActionState.ActionLable}]");
            if (mActionStateMachine.ActionStateInfo.mActionType is not null &&
                mActionStateMachine.ActionStateInfo.mActionType.Count > CurrentActionState.ActionLable)
            {
                return mActionStateMachine.ActionStateInfo.mActionType[CurrentActionState.ActionLable];
            }
            return "当前单位不存在 [ActionType]";
        }

        public void ResetState()
        {
            mTargetActionEven.Clear();
            mActionEvenLoop.Clear();
            mActionEvenLoopHash.Clear();
            mActionEvenInitHash.Clear();
            mFindInher.Clear();
            _actionInterrupts.Clear();
        }
    }
}