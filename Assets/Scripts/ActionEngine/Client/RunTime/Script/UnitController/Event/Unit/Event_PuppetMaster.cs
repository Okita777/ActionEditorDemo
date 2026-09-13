#if PuppetMaster
using System;
using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using RootMotion.Dynamics;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [Serializable]
    public class Event_PuppetMaster : IActionEventData
    {
        [SerializeField] private byte m_Op;
        [SerializeField] private byte m_TargetMode;
        [SerializeField] private float m_Random = 0f;
        [SerializeField] private float m_KillDuration = 1f;
        [SerializeField] private float m_DeadMuscleWeight = 0.01f;
        [SerializeField] private bool m_ExitRestore = true;

        [SerializeField] private bool m_ApplyForce;
        [SerializeField] private GraphEvent_NoValue_Vector3 m_Force = new GraphEvent_NoValue_Vector3();
        [SerializeField] private float m_UnPin = 1f;
        [SerializeField] private byte m_ForceTarget;
        [SerializeField] private int m_MuscleIndex;

        #region Properties

        [EditorProperty("操作类型", EditorPropertyType.EEPT_Enum,
            EnumNames = new[] { "Kill(布娃娃死亡)", "Resurrect(复活)", "Freeze(冻结最低开销)", "SetMode(设置模式)", "Hit(施加力/冲量)" })]
        public byte Op
        {
            get => m_Op;
            set => m_Op = value;
        }

        [EditorProperty("目标模式", EditorPropertyType.EEPT_Enum,
            EnumNames = new[] { "Active", "Kinematic", "Disabled" }, LabelWidth = 120)]
        public byte TargetMode
        {
            get => m_TargetMode;
            set => m_TargetMode = value;
        }

        [EditorProperty("Kill过渡时长(s)", EditorPropertyType.EEPT_Float)]
        public float KillDuration
        {
            get => m_KillDuration;
            set => m_KillDuration = value;
        }

        [EditorProperty("死亡肌肉残余权重", EditorPropertyType.EEPT_Float, LabelWidth = 120)]
        public float DeadMuscleWeight
        {
            get => m_DeadMuscleWeight;
            set => m_DeadMuscleWeight = value;
        }

        [EditorProperty("退出时恢复", EditorPropertyType.EEPT_Bool, LabelWidth = 120)]
        public bool ExitRestore
        {
            get => m_ExitRestore;
            set => m_ExitRestore = value;
        }

        [EditorProperty("随机延迟上限(s)", EditorPropertyType.EEPT_Float,
            Tooltip = "大于 0 时在 Enter 后随机等待 [0, 上限) 秒再执行；为 0 则立即执行")]
        public float RandomDelayMax
        {
            get => m_Random;
            set => m_Random = value;
        }

        [EditorProperty("附加施力(延迟一帧)", EditorPropertyType.EEPT_Bool, LabelWidth = 120,
            Tooltip = "Kill 后延迟一帧施力，等待 PuppetMaster 完成状态过渡后再施加冲量")]
        public bool ApplyForce { get => m_ApplyForce; set => m_ApplyForce = value; }

        [EditorProperty("力(方向+大小)", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Vector3 Force { get => m_Force; set => m_Force = value; }

        [EditorProperty("解钉力度(UnPin)", EditorPropertyType.EEPT_Float,
            Tooltip = "传递给 broadcaster.Hit 的 unPin 参数，值越大被击中的肌肉越快脱离动画约束（0=不解钉，1=完全解钉）；仅影响 BehaviourPuppet 的 pin 权重衰减速度，不影响力的大小")]
        public float UnPin { get => m_UnPin; set => m_UnPin = value; }

        [EditorProperty("施力目标", EditorPropertyType.EEPT_Enum,
            EnumNames = new[] { "All(所有肌肉)", "Specific(指定索引)", "Root(根骨骼)" }, LabelWidth = 120)]
        public byte ForceTarget { get => m_ForceTarget; set => m_ForceTarget = value; }

        [EditorProperty("肌肉索引", EditorPropertyType.EEPT_Int, LabelWidth = 120)]
        public int MuscleIndex { get => m_MuscleIndex; set => m_MuscleIndex = value; }

        #endregion

        [NonSerialized] private PuppetMaster m_CachedPM;
        [NonSerialized] private ActionStatePart m_CachedState;
        [NonSerialized] private PuppetMaster.Mode m_PreviousMode;
        [NonSerialized] private bool m_HasAppliedPuppetOp;
        [NonSerialized] private int m_PendingForce;
        [NonSerialized] private float m_RandomDelayTarget;
        [NonSerialized] private float m_RandomDelayElapsed;

        private static readonly string[] OpNames = { "Kill", "Resurrect", "Freeze", "SetMode", "Hit" };
        private static readonly string[] ModeNames = { "Active", "Kinematic", "Disabled" };
        private static readonly string[] ForceTargetNames = { "All", "Specific", "Root" };

        public int GetEvenType() => (int)EEvenType.EET_PuppetMaster;
        public IActionEventData Creact() => new Event_PuppetMaster();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            m_HasAppliedPuppetOp = false;
            m_PendingForce = -1;
            m_RandomDelayElapsed = 0f;
            m_RandomDelayTarget = 0f;
            m_CachedState = _actionState;

            ActionStateMachine stateMachine = _actionState.ActionStateMachine;
            if (!stateMachine.TryGetComponent(out ActionEngine_PuppetMasterWarp _CachedPM, nameof(ActionEngine_PuppetMasterWarp)))
            {
                EngineDebug.LogError("当前单位好像并未挂载 <color=#ffcc00>[ActionEngine_PuppetMasterWarp]</color> 组件");
                return;
            }
            m_CachedPM = _CachedPM.PuppetMaster;

            if (!_isSingle && m_Random > 0f)
            {
                m_RandomDelayTarget = UnityEngine.Random.Range(0f, m_Random);
                if (m_RandomDelayTarget <= 0f)
                {
                    ApplyPuppetOperation();
                    m_HasAppliedPuppetOp = true;
                }
            }
            else
            {
                ApplyPuppetOperation();
                m_HasAppliedPuppetOp = true;
            }
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (m_CachedPM == null) return;

            if (m_PendingForce > 0)
            {
                m_PendingForce--;
                if (m_PendingForce <= 0)
                {
                    Vector3 forceVec = m_Force.value(m_CachedState, EngineResourcesManager.Instance.MachineTime);
                    ApplyHit(forceVec);
                }
            }

            if (m_HasAppliedPuppetOp || m_Random <= 0f) return;

            m_RandomDelayElapsed += _actionTime.Deltatime;
            if (m_RandomDelayElapsed < m_RandomDelayTarget) return;

            ApplyPuppetOperation();
            m_HasAppliedPuppetOp = true;
        }

        private void ApplyPuppetOperation()
        {
            switch (m_Op)
            {
                case 0: // Kill
                    var settings = new PuppetMaster.StateSettings(
                        m_KillDuration,
                        m_DeadMuscleWeight);
                    m_CachedPM.Kill(settings);
                    if (m_ApplyForce)
                        m_PendingForce = 2;
                    break;

                case 1: // Resurrect
                    m_CachedPM.Resurrect();
                    break;

                case 2: // Freeze
                    m_CachedPM.Freeze();
                    break;

                case 3: // SetMode
                    m_PreviousMode = m_CachedPM.mode;
                    m_CachedPM.mode = (PuppetMaster.Mode)m_TargetMode;
                    break;

                case 4: // Hit
                    Vector3 hitForce = m_Force.value(m_CachedState, EngineResourcesManager.Instance.MachineTime);
                    ApplyHit(hitForce);
                    break;
            }
        }

        /// <summary>
        /// 复刻 BehaviourPuppet.OnMuscleHitBehaviour 的核心逻辑:
        /// 1. 强制 Active 模式  2. 解钉(pinWeightMlp)  3. 取消 Kinematic  4. AddForceAtPosition
        /// 不依赖 BehaviourPuppet / broadcaster 管线，兼容无 Behaviour 配置的 PuppetMaster。
        /// </summary>
        private void ApplyHitToMuscle(int muscleIndex, Vector3 force)
        {
            Muscle[] muscles = m_CachedPM.muscles;
            if (muscleIndex < 0 || muscleIndex >= muscles.Length) return;
            if (muscles[muscleIndex].state.isDisconnected) return;

            m_CachedPM.mode = PuppetMaster.Mode.Active;
            muscles[muscleIndex].state.pinWeightMlp = Mathf.Clamp01(muscles[muscleIndex].state.pinWeightMlp - m_UnPin);
            muscles[muscleIndex].rigidbody.isKinematic = false;
            muscles[muscleIndex].rigidbody.AddForceAtPosition(force, muscles[muscleIndex].rigidbody.position);
        }

        private void ApplyHit(Vector3 force)
        {
            Muscle[] muscles = m_CachedPM.muscles;
            if (muscles.Length == 0) return;

            switch (m_ForceTarget)
            {
                case 0: // All
                    for (int i = 0; i < muscles.Length; i++)
                        ApplyHitToMuscle(i, force);
                    break;

                case 1: // Specific
                    ApplyHitToMuscle(Mathf.Clamp(m_MuscleIndex, 0, muscles.Length - 1), force);
                    break;

                case 2: // Root
                    ApplyHitToMuscle(0, force);
                    break;
            }
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            if (m_CachedPM == null || !m_HasAppliedPuppetOp)
            {
                return;
            }

            if (m_ExitRestore)
            {
                if(m_Op == 3)
                {
                    m_CachedPM.mode = m_PreviousMode;
                }
                else if (m_Op == 0)
                {
                    m_CachedPM.Resurrect();
                    m_CachedPM.mode = PuppetMaster.Mode.Disabled;
                }
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_PuppetMaster _event = _eventData as Event_PuppetMaster;
            _event.m_Op = m_Op;
            _event.m_TargetMode = m_TargetMode;
            _event.m_Random = m_Random;
            _event.m_KillDuration = m_KillDuration;
            _event.m_DeadMuscleWeight = m_DeadMuscleWeight;
            _event.m_ExitRestore = m_ExitRestore;
            _event.m_ApplyForce = m_ApplyForce;
            _event.m_Force = m_Force.Clone();
            _event.m_UnPin = m_UnPin;
            _event.m_ForceTarget = m_ForceTarget;
            _event.m_MuscleIndex = m_MuscleIndex;
            return _event;
        }
    }
}
#endif
