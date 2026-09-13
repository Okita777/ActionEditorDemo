using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class Ex_Update_TargetingMove : ActionLogics
    {
        private float m_Weight = 0f;
        private bool m_IsActive = false;
        private Transform m_Target;
        private Transform m_ReferBone;
        private Vector3 m_ScriptLookDir;
        private Event_TargetingMove m_EventTargetingMove;

        public Vector3 ScriptLookDir
        {
            get { return m_ScriptLookDir; }
            set { m_ScriptLookDir = value; }
        }

        public void OnSetRot(Event_TargetingMove _EventTargetingMove, Transform _target, Transform _referBone)
        {
            if (_EventTargetingMove == null || _target == null || _referBone == null) return;
            m_EventTargetingMove = _EventTargetingMove;
            if (!m_EventTargetingMove.FreeToX && !m_EventTargetingMove.FreeToY) return;

#if UNITY_EDITOR
            // 参考骨必须位于目标骨子树内，否则旋转目标骨链无法带动参考骨，两朝向永远无法重合
            if (!IsDescendantOrSelf(_referBone, _target))
            {
                EngineDebug.LogWarning($"[Ex_Update_TargetingMove] 参考骨[{_referBone.name}]不在目标骨[{_target.name}]的子级链内，瞄准朝向将无法生效，请检查骨骼配置");
            }
#endif

            m_IsActive = true;
            m_Target = _target;
            m_ReferBone = _referBone;
        }

        // 混出交由 LateUpdate 的权重过渡完成（降权速率取 ExitTime），此处仅切换激活状态
        public void OnExit(float _enterTime = 0)
        {
            m_IsActive = false;
        }

        public override void LateUpdate(ActionStateMachine _actionState, float _deltaTime)
        {
            if (m_EventTargetingMove == null || m_Target == null || m_ReferBone == null)
            {
                m_Weight = 0f;
                m_IsActive = false;
                return;
            }

            Vector3 _targetDir = m_EventTargetingMove.LookTarget.value(_actionState.AllActionStatePart[0], new ActionMachineTime(Time.deltaTime, 0, 0, 0));
            Vector3 _referDir = GetReferDir(m_ReferBone);

            Quaternion _delta = ComputeAlignDelta(_actionState, _referDir, _targetDir, out bool _inValidRange);

            // 目标权重：事件激活中且目标落在有效角度范围内 → 1；已退出或超出有效范围 → 0（过渡回影响前状态）
            float _targetWeight = (m_IsActive && _inValidRange) ? 1f : 0f;
            m_Weight = StepWeight(m_Weight, _targetWeight, _deltaTime);

            if (m_Weight <= 0f) return;
            ApplyDeltaToChain(_delta, m_Weight);
        }

        // 计算将参考朝向对齐到目标朝向所需的世界旋转（已按 AngleLock 硬夹），并输出目标是否落在 SelfAngle 有效范围内
        private Quaternion ComputeAlignDelta(ActionStateMachine _actionState, Vector3 _referDir, Vector3 _targetDir, out bool _inValidRange)
        {
            _inValidRange = false;
            if (m_EventTargetingMove == null) return Quaternion.identity;

            // 目标方向叠加 Y 轴世界偏移
            float _worldAngle = m_EventTargetingMove.WorldAngle.GetValue(_actionState.AllActionStatePart[0]);
            _targetDir = Quaternion.Euler(0f, _worldAngle, 0f) * _targetDir;

            if (_targetDir.sqrMagnitude < 1e-6f || _referDir.sqrMagnitude < 1e-6f) return Quaternion.identity;

            Vector3 _up = Vector3.up;
            Vector3 _referDirN = _referDir.normalized;
            Vector3 _targetDirN = _targetDir.normalized;

            // 未启用的轴不参与有效范围判定，默认视为在范围内
            bool _rangeY = true;
            bool _rangeX = true;

            // 1) 偏航（左右）：绕世界 Up，将参考朝向的水平投影对齐到目标水平投影
            float _yaw = 0f;
            if (m_EventTargetingMove.FreeToY)
            {
                Vector3 _referFlat = Vector3.ProjectOnPlane(_referDirN, _up);
                Vector3 _targetFlat = Vector3.ProjectOnPlane(_targetDirN, _up);
                if (_referFlat.sqrMagnitude > 1e-6f && _targetFlat.sqrMagnitude > 1e-6f)
                {
                    // 有效范围判定用未夹取的原始几何角（否则永远落在 AngleLock 内、SelfAngle 失去意义）
                    float _yawRaw = Vector3.SignedAngle(_referFlat, _targetFlat, _up);
                    _rangeY = _yawRaw >= m_EventTargetingMove.SelfAngle_Y.y && _yawRaw <= m_EventTargetingMove.SelfAngle_Y.x;
                    _yaw = Mathf.Clamp(_yawRaw, m_EventTargetingMove.AngleLock_Y.y, m_EventTargetingMove.AngleLock_Y.x);
                }
            }
            Quaternion _yawRot = Quaternion.AngleAxis(_yaw, _up);
            Vector3 _referYawed = _yawRot * _referDirN;

            // 2) 俯仰（上下）：绕（已偏航后的）水平右轴，将参考朝向抬升/俯下到目标俯仰
            Quaternion _pitchRot = Quaternion.identity;
            if (m_EventTargetingMove.FreeToX)
            {
                Vector3 _referYawedFlat = Vector3.ProjectOnPlane(_referYawed, _up);
                Vector3 _pitchAxis = Vector3.Cross(_up, _referYawedFlat).normalized;
                if (_pitchAxis.sqrMagnitude > 1e-6f)
                {
                    // 目标投影到俯仰平面（垂直于 pitchAxis），使夹角为纯俯仰量，避免偏航被禁用时的耦合误差
                    Vector3 _targetInPitchPlane = Vector3.ProjectOnPlane(_targetDirN, _pitchAxis);
                    if (_targetInPitchPlane.sqrMagnitude > 1e-6f)
                    {
                        float _pitchRaw = Vector3.SignedAngle(_referYawed, _targetInPitchPlane, _pitchAxis);
                        _rangeX = _pitchRaw >= m_EventTargetingMove.SelfAngle_X.y && _pitchRaw <= m_EventTargetingMove.SelfAngle_X.x;
                        float _pitch = Mathf.Clamp(_pitchRaw, m_EventTargetingMove.AngleLock_X.y, m_EventTargetingMove.AngleLock_X.x);
                        _pitchRot = Quaternion.AngleAxis(_pitch, _pitchAxis);
                    }
                }
            }

            _inValidRange = _rangeY && _rangeX;
            // 组合总旋转：先偏航后俯仰
            return _pitchRot * _yawRot;
        }

        // 将对齐旋转按权重缩放后沿骨骼链均分：每根骨骼绕同一世界轴旋转 1/(链数+1)，累积到参考骨即为完整 delta
        private void ApplyDeltaToChain(Quaternion _delta, float _weight)
        {
            if (m_Target == null || m_EventTargetingMove == null) return;

            _delta = Quaternion.Slerp(Quaternion.identity, _delta, Mathf.Clamp01(_weight));
            if (_delta == Quaternion.identity) return;

            int _links = Mathf.Max(0, m_EventTargetingMove.TargetBoneLinks);
            Quaternion _perBone = Quaternion.Slerp(Quaternion.identity, _delta, 1f / (_links + 1));

            Transform _node = m_Target;
            _node.rotation = _perBone * _node.rotation;
            for (int i = 0; i < _links; i++)
            {
                _node = _node.parent;
                if (_node == null) break;
                _node.rotation = _perBone * _node.rotation;
            }
        }

        // 权重朝目标平滑逼近：升权（混入/重回有效范围）用 EnterTime，降权（混出/超出有效范围）用 ExitTime；时间<=0 立即到位
        private float StepWeight(float _cur, float _target, float _deltaTime)
        {
            if (_cur == _target) return _target;
            int _time = _target > _cur ? m_EventTargetingMove.EnterTime : m_EventTargetingMove.ExitTime;
            float _dur = _time * MotionEngineConst.TimeDoubling_F;
            if (_dur <= 0f) return _target;
            return Mathf.MoveTowards(_cur, _target, _deltaTime / _dur);
        }

#if UNITY_EDITOR
        private static bool IsDescendantOrSelf(Transform _node, Transform _ancestor)
        {
            Transform _cur = _node;
            while (_cur != null)
            {
                if (_cur == _ancestor) return true;
                _cur = _cur.parent;
            }
            return false;
        }
#endif


        private Vector3 GetReferDir(Transform _referBone)
        {
            if (_referBone == null) return Vector3.forward;
            if (m_EventTargetingMove.TargetForward == 0) return _referBone.right;
            if (m_EventTargetingMove.TargetForward == 1) return _referBone.up;
            if (m_EventTargetingMove.TargetForward == 2) return _referBone.forward;
            if (m_EventTargetingMove.TargetForward == 3) return _referBone.right * -1;
            if (m_EventTargetingMove.TargetForward == 4) return _referBone.up * -1;
            if (m_EventTargetingMove.TargetForward == 5) return _referBone.forward * -1;
            return Vector3.forward;
        }
    }
}