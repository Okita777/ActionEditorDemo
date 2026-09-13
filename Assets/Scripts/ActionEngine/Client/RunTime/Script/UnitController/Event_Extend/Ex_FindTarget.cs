using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class Ex_FindTarget : StaticActionLogics
    {
        private List<ComponentIntegral> components = new List<ComponentIntegral>();
        public override void OnGet(ActionStateMachine _actionState)
        {
            components.Clear();
        }

        public void AddChild(Component _component, Vector3 _pos, Vector3 _forward, float _posIntegral, float _angleIntegral)
        {
            Vector3 _position = _component.transform.position - _pos;
            float _posI = _position.sqrMagnitude * _posIntegral;//距离越大，分值越大
            float _angleI = Vector3.Dot(_forward, _position.normalized) * _angleIntegral;
            components.Add(new ComponentIntegral(_component, _posI + _angleI));
        }

        public bool TryGetTarget<T>(out T _target) where T : Component
        {
            if (components.Count > 0)
            {
                components.Sort((x, y) => { return x.m_integral.CompareTo(y.m_integral); });
                _target = components[0].m_component as T;
                return true;
            }

            _target = null;
            return false;
        }

        private struct ComponentIntegral
        {
            public Component m_component;
            public float m_integral;

            public ComponentIntegral(Component _component, float _integral)
            {
                m_component = _component;
                m_integral = _integral;
            }
        }
    }
}