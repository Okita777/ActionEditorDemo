using System;
using System.Collections.Generic;

//using UnityEditor;
using UnityEngine;
namespace AsiActionEngine.RunTime
{
    //[RequireComponent(typeof(Animator))]
    public class ActionEngine_Unit : TargetUnit
    {
        public Dictionary<int, List<ActionEngine_Skill>> m_SkillDic = new Dictionary<int, List<ActionEngine_Skill>>();
        public override ActionEngine_Unit GetUnit() => this;
        public Action<ActionStateMachine, Vector3, Quaternion> AnimatorMoveCallBack = null;//Root位移与旋转的回调

#if UNITY_EDITOR
        [Header("当前Root权重")] public Vector3 RootWeight = Vector3.one;
        [Header("当前Root位移数据")] public Vector3 RooDelta = Vector3.one;
#endif
        // public int UnitID;//单位ID  生成时赋值
        // public string ActionGroupName;//行为列表组名称
        [HideInInspector] public int UnitWarpID;
        [NonSerialized][HideInInspector] public UnitWarp UnitWarp = null;
        //对象池加载键：客户端存 ModelPath，服务端逻辑单位存逻辑键，销毁时按此键归还对象池
        [NonSerialized][HideInInspector] public string LoadPoolKey = null;
        [NonSerialized][HideInInspector] public bool mNeedInit = true;
        [HideInInspector] public ERuntimeDataChannel Channel = ERuntimeDataChannel.Local;

        private Transform mRootTarget = null;
        public int CameraID { get; set; }
        public Transform RootTarget
        {
            get
            {
                if (mRootTarget is null)
                    mRootTarget = transform;
                return mRootTarget;
            }
            set => mRootTarget = value;
        }
        public ActionStateMachine ActionStateMachine => m_ActionStateMachine;
        private bool isInitialized = false;

        public ActionEngine_Unit GetSource => mSource;
        public ActionEngine_Unit GetMaster => mMaster;
        [NonSerialized] private ActionEngine_Unit mMaster = null;
        [NonSerialized] private ActionEngine_Unit mSource = null;
        [NonSerialized] private Vector3 deltaPos;
        [NonSerialized] private ActionStateMachine m_ActionStateMachine = null;
        [NonSerialized] private float mGetLatencyPosInterval = 0.1f;
        [NonSerialized] private float mGetLatencyPosInterval_t = -1;

        public Vector3 LastFramePos { get; private set; }
        public Vector3 CurFramePos { get; private set; }
        public Vector3 RootMotionDelta { get; private set; }
        public Quaternion RootMotionRotation { get; private set; }
        public Vector3 FinalCharacterMove { get; set; }
        public float UnitSpeed
        {
            get => (CurFramePos - LastFramePos).magnitude / mGetLatencyPosInterval;
        }


        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Init()
        {
            //if (RootTarget is null) RootTarget = transform;
            LastFramePos = CurFramePos = transform.position;
            mGetLatencyPosInterval_t = 0;
        }

        public virtual void OnUpdate(float _time)
        {
            m_ActionStateMachine.OnUpdate(_time);
        }

        public virtual void OnLateUpdate(float _time)
        {
            m_ActionStateMachine.OnLateUpdate(_time);

            if (mGetLatencyPosInterval_t < mGetLatencyPosInterval)
            {
                mGetLatencyPosInterval_t += Time.deltaTime;
            }
            else
            {
                mGetLatencyPosInterval_t = 0;
                //在本帧所有移动结束后采样位置，得到帧间实际位移
                LastFramePos = CurFramePos;
                CurFramePos = transform.position;
            }

        }


        public void SetActionStateMachine(ActionStateMachine part)
        {
            m_ActionStateMachine = part;
            isInitialized = true;
            //初始化帧位置，避免首帧差分出现异常大速度
            LastFramePos = CurFramePos = transform.position;
            mGetLatencyPosInterval_t = 0;
        }
        public void SetMaster(ActionEngine_Unit _unit, ActionEngine_Unit _source)
        {
            //Debug.Log("设置父级");
            mMaster = _unit;
            mSource = _source;
        }
        public bool TryGetActionStateMachine(out ActionStateMachine _actionStateMachine)
        {
            _actionStateMachine = m_ActionStateMachine;
            return isInitialized;
        }

        public void SelfDestroy(UnityEngine.Object _objct)
        {
#if UNITY_EDITOR
            isSelfDextroy = true;
            if (!EngineResourcesManager.Instance.isPlaying)
            {
                if (_objct == AgentUnitRoot)
                {
                    DestroyImmediate(_objct);
                }
                else
                {
                    DestroyImmediate(_objct);
                    DestroyImmediate(AgentUnitRoot);
                }
                return;
            }
#endif
            if (_objct == AgentUnitRoot)
            {
                Destroy(_objct);
            }
            else
            {
                Destroy(_objct);
                Destroy(AgentUnitRoot);
            }
        }

#if UNITY_EDITOR
        private bool isSelfDextroy = false;

        private void OnDestroy()
        {
            if (!isSelfDextroy)
            {
                if (EngineResourcesManager.Instance.isPlaying && gameObject.activeSelf)
                    EngineDebug.LogError($"别删别删！！！！！ ActionEngine对象池炸穿了！！！！ [{gameObject.name}]  Hash[{gameObject.GetHashCode()}]\n [<color=#ffcc00>资源释放交由ActionEngine自管理</color>]");
            }
            //Debug.LogError($"对象被删除了！！: 来自[{GetCallerMethodName()}]");
        }

        ///// <summary>
        ///// 获取调用当前函数的那个函数（父函数）的方法名
        ///// </summary>
        //public static string GetCallerMethodName()
        //{
        //    // 跳过2帧：1.当前方法（GetCallerMethodName）自身，2.调用者的调用者（我们想要的就是这一帧）
        //    System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(2, false);
        //    System.Diagnostics.StackFrame frame = st.GetFrame(0);
        //    return frame?.GetMethod()?.Name ?? "Unknown Caller";
        //}
#endif

        public void OnAnimatorMove()
        {
            if (ActionStateMachine is null)
            {
#if UNITY_EDITOR
                // EngineDebug.LogError($"单位未成功初始化 [{gameObject.name}]");
#endif
                return;
            }
            // 仅 LocalPredict 采 Animator RootMotion；ServerAuthoritative / RemoteProxy 不跑。
            if (ActionStateMachine.Authority != SimulationAuthority.LocalPredict) return;
            if (m_ActionStateMachine.TryGetComponent(out Animator animator, nameof(Animator)))
            {
                //计算Root动画位移（乘Root权重）与旋转增量，仅存储；实际位移与旋转由Ex_Update_CharacterControl读取应用
                deltaPos.x = animator.deltaPosition.x * m_ActionStateMachine.RootWeight.x;
                deltaPos.y = animator.deltaPosition.y * m_ActionStateMachine.RootWeight.y;
                deltaPos.z = animator.deltaPosition.z * m_ActionStateMachine.RootWeight.z;
                RootMotionDelta = deltaPos;
                RootMotionRotation = animator.deltaRotation;

                //通知实际位移逻辑读取并叠加移动/旋转（参数为已乘权重的Root位移与Root旋转增量）
                AnimatorMoveCallBack?.Invoke(m_ActionStateMachine, deltaPos, animator.deltaRotation);
#if UNITY_EDITOR
                RootWeight = m_ActionStateMachine.RootWeight;
                RooDelta = deltaPos;
#endif
            }
        }
    }
}
