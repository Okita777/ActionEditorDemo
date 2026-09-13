using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;
using UnityEngine.Events;

namespace AsiTimeLine.RunTime
{
    public class ActionEngine_InteractObj : MonoBehaviour, IInteractObject
    {
        [Header("交互检测的半径")] public float InteractRadius = 1.0f;
        [Header("交互检测的安全角度")] public float InteractAngle = 360;
        [Header("当前交互类型")] public IInteractObject.EInteractType InteractType = IInteractObject.EInteractType.Prop;
        [Header("当前单位组件")] public ActionEngine_Unit TargetUnit;
        [Header("交互中心位置偏移")] public Vector3 centerOffsetPos = Vector3.zero;
        [Header("交互中心角度偏移")] public Vector3 centerOffsetRot = Vector3.zero;

        public void OnTrigger(ActionStateMachine _actionState)
        {

        }

        public Vector3 Point_Dir()
        {
            return transform.rotation * (Quaternion.Euler(centerOffsetRot) * Vector3.forward);
        }

        public Vector3 Point_Pos()
        {
            return transform.TransformPoint(centerOffsetPos);
        }

        public float Point_Radius() => InteractRadius;
        public ActionEngine_Unit CurUnit() => TargetUnit;

        IInteractObject.EInteractType IInteractObject.InteractType() => InteractType;

        private void OnDrawGizmosSelected()
        {
            Vector3 _center = Point_Pos();
            Vector3 _normal = transform.up;
            if (_normal.sqrMagnitude <= Mathf.Epsilon)
            {
                _normal = Vector3.up;
            }

            _normal.Normalize();

            Vector3 _dir = Vector3.ProjectOnPlane(Point_Dir(), _normal);
            if (_dir.sqrMagnitude <= Mathf.Epsilon)
            {
                _dir = Vector3.ProjectOnPlane(transform.forward, _normal);
            }

            if (_dir.sqrMagnitude <= Mathf.Epsilon)
            {
                _dir = Vector3.forward;
            }

            _dir.Normalize();

            float _radius = Mathf.Max(0.0f, InteractRadius);
            float _angle = Mathf.Clamp(InteractAngle, 0.0f, 360.0f);
            if (_radius <= Mathf.Epsilon)
            {
                return;
            }

            Color _gizmosColor = Gizmos.color;
            Gizmos.color = new Color(0.0f, 0.8f, 1.0f, 1.0f);

            if (_angle >= 360.0f)
            {
                DrawGizmosArc(_center, _normal, _dir, _radius, 360.0f);
            }
            else if (_angle > 0.0f)
            {
                DrawGizmosSector(_center, _normal, _dir, _radius, _angle);
            }

            Gizmos.DrawLine(_center, _center + _dir * _radius);
            Gizmos.DrawWireSphere(_center, Mathf.Min(0.1f, _radius * 0.1f));
            Gizmos.color = _gizmosColor;
        }

        private static void DrawGizmosSector(Vector3 _center, Vector3 _normal, Vector3 _dir, float _radius, float _angle)
        {
            float _halfAngle = _angle * 0.5f;
            Vector3 _leftDir = Quaternion.AngleAxis(-_halfAngle, _normal) * _dir;
            Vector3 _rightDir = Quaternion.AngleAxis(_halfAngle, _normal) * _dir;

            Gizmos.DrawLine(_center, _center + _leftDir * _radius);
            Gizmos.DrawLine(_center, _center + _rightDir * _radius);
            DrawGizmosArc(_center, _normal, _dir, _radius, _angle);
        }

        private static void DrawGizmosArc(Vector3 _center, Vector3 _normal, Vector3 _dir, float _radius, float _angle)
        {
            int _segmentCount = Mathf.Max(2, Mathf.CeilToInt(_angle / 360.0f * 64.0f));
            float _startAngle = -_angle * 0.5f;
            Vector3 _lastPoint = _center + (Quaternion.AngleAxis(_startAngle, _normal) * _dir) * _radius;

            for (int i = 1; i <= _segmentCount; i++)
            {
                float _curAngle = _startAngle + _angle * i / _segmentCount;
                Vector3 _curPoint = _center + (Quaternion.AngleAxis(_curAngle, _normal) * _dir) * _radius;
                Gizmos.DrawLine(_lastPoint, _curPoint);
                _lastPoint = _curPoint;
            }
        }
    }
}