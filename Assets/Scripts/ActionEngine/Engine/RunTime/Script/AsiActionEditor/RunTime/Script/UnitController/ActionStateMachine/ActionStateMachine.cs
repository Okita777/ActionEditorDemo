using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime.Graph;
// using System.Linq;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public partial class ActionStateMachine
    {
        public float TimeScale
        {
            get { return mTimeScale; }
            set { mTimeScale = value; }
        }

        // public List<float> TimeScaleList = new List<float>();
        public int BluePrint_LoopIndex;
        public bool IsCheckBluePrintCon = true;
        public bool CurOnHitValid = false;
        public float HoldKeyIntervalTime { get; private set; } = 0.2f; //长按判定时间
        public void SetHoldKeyIntervalTime(float _holdKeyIntervalTime)
        {
            HoldKeyIntervalTime = _holdKeyIntervalTime;
        }

        public int ActionGroupID => ActionStateInfo.ActionGroupID;
        public string ActionGroupName => ActionStateInfo.ActionGroupName;

        /// <summary>
        /// 单位在本进程中的模拟权威角色（三态）。默认 <see cref="SimulationAuthority.LocalPredict"/> 保持旧单机行为不变。
        /// 由所在 World 的档位（<c>CombatAuthorityConfig</c>）赋值，见服务端权威分档蓝图 §二。
        /// </summary>
        public SimulationAuthority Authority { get; set; } = SimulationAuthority.LocalPredict;

        /// <summary>
        /// <see cref="SimulationAuthority.ServerAuthoritative"/> 单位是否自行跑完整的本地模拟：
        /// CC 逻辑位移与重力、以及旋转轨。
        /// 默认 <c>false</c>：既有 DS 路径的服务端单位由客户端上报位姿驱动，若同时跑逻辑位移会形成两个位姿源互相打架。
        /// 仅由完全承担位姿权威的网络层（AeNet）在建体后置为 <c>true</c>。
        /// 对 <see cref="SimulationAuthority.LocalPredict"/> 与 <see cref="SimulationAuthority.RemoteProxy"/> 无影响。
        /// </summary>
        public bool ServerLogicSimulationEnabled { get; set; } = false;

        /// <summary>
        /// 兼容代理：旧调用点读写 <c>IsLocalClient</c> 映射到 <see cref="Authority"/>，逐步下线。
        /// <c>get</c>：非 <see cref="SimulationAuthority.RemoteProxy"/> 即视为本地驱动（LocalPredict / ServerAuthoritative 都跑输入+打断）；
        /// <c>set</c>：<c>true→LocalPredict, false→RemoteProxy</c>（旧语义只区分本地玩家 vs 远端）。
        /// </summary>
        public bool IsLocalClient
        {
            get => Authority != SimulationAuthority.RemoteProxy;
            set => Authority = value ? SimulationAuthority.LocalPredict : SimulationAuthority.RemoteProxy;
        }

        public bool AnimEnble { get; set; } = true;//是否更新动画
        public bool AnimValid => mAnimValid;//是否存在动画机
        public ActionEventSystem EventSystem { get; private set; } = null;
        //public void SetActionStateInfo(ActionStateInfo _info) => OnSetActionStateInfo(_info);

        public void ReSetAllKey(bool _resetHoldKey = false) => OnReSetAllKey(_resetHoldKey);
        public bool IsMoveInput
        {
            get { return IsMoveInputValue; }
            private set
            {
                IsMoveInputValue = value;
                if (value)
                {
                    foreach (ActionStatePart _part in AllActionStatePart)
                        _part.IsMoveInputPre = true;
                }
            }
        }//是否持续输入位移
        //public bool IsMoveInputPre = false;//位移输入的预输入
        // public bool IsLock;//是否锁定
        public float DeltaTime { get; private set; }
        public Vector3 PlayerInputMoveDir { get; private set; }//玩家输入的方向
        public Vector3 PlayerInputMoveDir_Cam { get; private set; }//玩家输入的方向

        public Vector3 RootWeight
        {
            get { return mRootWeight; }
            set { mRootWeight = value; }
        }
        public bool TryGetCurActionStateInfo(int layer, out int info)
            => mActionStateInfoDic.TryGetValue(layer, out info);
        public Animator CurAnimator { get; private set; }//当前更新的动画
        public void CrossFade(string stateName, float normalizedTransitionDuration, int layer, float normalizedTimeOffset) =>
            OnCrossFade(stateName, normalizedTransitionDuration, layer, normalizedTimeOffset);
        public void CrossFadeInFixedTime(string stateName, float fixedTransitionDuration, int layer, float fixedTimeOffset) =>
            OnCrossFadeInFixedTime(stateName, fixedTransitionDuration, layer, fixedTimeOffset);
        public void UpdateAnimClip(string stateName, int layer, float fixedTimeOffset) =>
            OnUpdateAnimClip(stateName, layer, fixedTimeOffset);

        public ActionEngine_Unit CurUnit { get; private set; }//当前绑定单位

        private TargetUnit m_HitUnit = null;
        public TargetUnit HitUnit
        {
            get => m_HitUnit;
            set
            {
                m_HitUnit = value;
                hasHiter = value != null;
            }
        }//命中的单位
        public ActionEngine_Unit AttackerUnit
        {
            get => onAttacker;
            set
            {
                onAttacker = value;
                hasAttacker = value != null;
            }
        }
        public ActionStatePart AttackerPart { get; set; } = null;
        public ActionStatePart HitPart { get; set; } = null;

        public Transform LockTransform;
        public GameObject OnHitObject { get; set; }//命中的对象
        public ActionStateInfo ActionStateInfo => mActionStateInfo;
        public List<ActionStatePart> AllActionStatePart => mAllActionStatePart;//所有层级
        public List<ActionStatePart> AllActionStatePart_Tmp => mAllActionStatePart_Tmp;//所有生成的层级
        /// <summary>
        /// 当前角色装备的所有Action模组(ActionInfo)
        /// </summary>
        public List<ActionStateInfo> EquipActionInfoList => mEquipActionInfoList;
        public ActionStatePart FirstStatePart { get; set; }
        //private ActionStatePart FirstStatePart_ser;
        //public ActionStatePart FirstStatePart { 
        //    get => FirstStatePart_ser;
        //    set { 
        //        if(CurUnit is ActionEngine_Skill skill)
        //        {
        //            EngineDebug.LogError("技能初始化设定");
        //        }
        //        FirstStatePart_ser = value; 
        //    }
        //}


        public int GetActionInfoIDToLayer(ActionStatePart _part) => OnGetActionInfoIDToLayer(_part);

        public void UnitBehit(IAttackInfo _attackInfo, ActionEngine_Unit _attacker, GValue_Setting _gValueRatio)
            => OnBeHit(_attackInfo, _attacker, _gValueRatio);

        public void UnitOnHit(IAttackInfo _attackInfo, GameObject _BeHit, TargetUnit _BeHitUnit, ActionStatePart _part = null)
            => OnOnHit(_attackInfo, _BeHit, _BeHitUnit, _part);

        public bool EquipActionInfo(ActionStateInfo _info, Action<bool> _loadComplete, out List<SLoadAnimationClip> _loadClips)
            => OnEquipActionInfo(_info, _loadComplete, out _loadClips);
        public bool EquipActionInfo(List<ActionStateInfo> _info, Action<bool> _loadComplete, out List<SLoadAnimationClip> _loadClips)
            => OnEquipActionInfo(_info, _loadComplete, out _loadClips);
        public bool UnEquipActionInfo(ActionStateInfo _info) => OnUnEquipActionInfo(_info);
        public bool UnEquipActionInfo(int _ActionGroupID) => OnUnEquipActionInfo(_ActionGroupID);
        public bool UnEquipActionInfo(List<ActionStateInfo> _info) => OnUnEquipActionInfo(_info);
        public bool UnEquipActionInfo(List<int> _info) => OnUnEquipActionInfo(_info);
        public bool UnEquipAllActionInfo() => OnUnEquipAllActionInfo();


        /// <summary>
        /// 中止Action逻辑的运转
        /// </summary>
        /// <param name="_part">要中止的对象</param>
        public void StopActionState(ActionStatePart _part) => OnStopActionState(_part);
        public void OnUpdate(float _deltaTime) => OnUpdateState(_deltaTime);
        public void OnLateUpdate(float _deltatime) => OnLateUpdateState(_deltatime);
        public void SetKeyDown(string _keyName, int _actionGroupID = -1) => OnSetKeyDown(_keyName, _actionGroupID);
        public void SetKeyUp(string _keyName) => OnSetKeyUp(_keyName);
        public void SendKeyDown(string _keyName, int _inputType = 0) => OnSendKeyDown(_keyName, _inputType);

        /// <summary>
        /// 输入注入观察点，参数为 (注入方式, 按键名, 负载)。负载含义见 <see cref="EActionInputKind"/>。
        ///
        /// 按键边沿落在 <c>ActionStatePart</c> 的当帧输入槽位上，打断系统消费后即清空，事后无法回溯，
        /// 因此需要把输入转成网络意图的场景只能在此截获。订阅方须在单位回收前反注册。
        /// </summary>
        public event Action<EActionInputKind, string, int> InputKeyObserved;

        //Animator
        public void SetAnimatorSpeed(float _speed) => OnSetAnimatorSpeed(_speed);
        public float GetAnimatorFloat(string _name) => OnGetAnimatorFloat(_name);
        public int GetAnimatorInt(string _name) => OnGetAnimatorInt(_name);
        public void SetAnimatorFloat(string _name, float _value) => OnSetAnimatorFloat(_name, _value);
        public void SetAnimatorInt(string _name, int _value) => OnSetAnimatorInt(_name, _value);
        public ActionState GetActionState(string _name) => OnGetActionState(_name);
        public bool TryGetActionState(int _actionID, out ActionState _action, ActionState _sour = null)
            => OnGetActionState(_actionID, out _action, _sour);
        public bool TryGetActionState(string _actionID, out ActionState _action, ActionState _sour = null)
            => OnGetActionState(_actionID, out _action, _sour);
        public bool TryGetActionStateWithGroup(int _actionID, out ActionState _action, out int _actionGroupID, ActionState _sour = null)
            => OnGetActionState(_actionID, out _action, out _actionGroupID, _sour);
        public bool GetActionState(int _id, out ActionState _action, ActionState _sour = null) => OnGetActionState(_id, out _action, _sour);
        public bool GetActionIDToName(string _name, out int _action) => OnGetActionIDToName(_name, out _action);
        public void ChangeAction(string _name, int _mixTime, int _setTime) => OnChangeAction(_name, _mixTime, _setTime);
        public ActionState ChangeAction(int _id, int _mixTime, int _setTime) => OnChangeAction(_id, _mixTime, _setTime);
        public void SpawnAction(ActionState _actionState, ActionStatePart _master, Vector3 _pos, Quaternion _rot)
            => OnSpawnAction(_actionState, _master, _pos, _rot);
        public void SpawnAction(ActionState _actionState, ActionStatePart _master, Vector3 _pos, Quaternion _rot, int _actionGroupID)
            => OnSpawnAction(_actionState, _master, _pos, _rot, _actionGroupID);

        /// <summary>
        /// 设置状态机运行速度(例如放慢或者加快角色速度等)
        /// </summary>
        /// <param name="_speed"></param>
        /// <param name="_duration"></param>
        public void SetSpeed(float _speed, float _duration = -1f) => OnSetSpeed(_speed, _duration);


        /// <summary>
        /// 注册方向输入
        /// </summary>
        /// <param name="_dir">输入的向量</param>
        /// <param name="_dirToCam">相机参考下的输入向量</param>
        public void SetMoveInput(Vector3 _dir, Vector3 _dirToCam) => OnSetMoveInput(_dir, _dirToCam);

        /// <summary>
        /// 注册方向停止输入
        /// </summary>
        public void SetMoveInputStop() => OnSetMoveInputStop();

        /// <summary>
        /// 注册朝向（一般情况请传入相机的 Quaternion ）
        /// </summary>
        /// <param name="_rot">朝向</param>
        public void SetMouseXY(Quaternion _rot) => OnSetHeadRot(_rot);
        public void SetCamRot(Quaternion _rot) { mCamRot = _rot; }
        public Quaternion GetCamPointRot() => mMouseXY;
        public Quaternion GetCamRot() => mCamRot;

        public Quaternion GetCharacterFor;
        public GValue_Setting InitGValue_Setting;
        public bool GetDieState => isDie;
        public void SetDieState(bool _die) { isDie = _die; }

        #region Valid
        public bool OnHitValid
        {
            get
            {
                if (hasHiter)
                {
                    if (HitUnit == null)
                    {
#if UNITY_EDITOR
                        EngineDebug.LogWarning("尝试获取命中单位，但是命中单位已被销毁");
#endif

                        hasHiter = false;
                        return false;
                    }
                    return true;
                }
#if UNITY_EDITOR
                EngineDebug.LogWarning("尝试获取命中单位，但是命中单位为空");
#endif

                return false;
            }
        }

        public bool OnAttackerValid
        {
            get
            {
                if (hasAttacker)
                {
                    if (onAttacker == null)
                    {
#if UNITY_EDITOR
                        EngineDebug.LogWarning("尝试获取攻击单位，但是攻击单位已被销毁");
#endif
                        hasAttacker = false;
                        return false;
                    }
                    return true;
                }
#if UNITY_EDITOR
                EngineDebug.LogWarning("尝试获取攻击单位，但是攻击单位为空");
#endif

                return false;
            }
        }

        public bool OnHitObjectValid
        {
            get
            {
                if (hasHitobj)
                {
                    if (OnHitObject == null)
                    {
#if UNITY_EDITOR
                        EngineDebug.LogWarning("尝试获取命中对象，但是命中对象已被销毁");
#endif
                        hasHitobj = false;
                        return false;
                    }
                    return true;
                }
#if UNITY_EDITOR
                EngineDebug.LogWarning("尝试获取命中对象，但是命中对象为空");
#endif

                return false;
            }
        }

        private HashSet<BluePrint_Value> bigHashSet = new HashSet<BluePrint_Value>(300);
        /// <summary>
        /// 在当前帧使用过该函数运算
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool BluePrintIsUse(BluePrint_Value value)
        {
            //if (!IsCheckBluePrintCon) return false;//跳过检查

            if (bigHashSet.Contains(value)) return true;

            if (IsCheckBluePrintCon)//仅在开启检查时才会添加
                bigHashSet.Add(value);
            return false;
        }

        public void BluePrintClear() { bigHashSet.Clear(); }
        #endregion
    }
}