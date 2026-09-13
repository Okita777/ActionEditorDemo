using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public class ConfigManager
    {
        private static ConfigManager instance = null;

        public static ConfigManager Instance
        {
            get { return instance ??= new ConfigManager(); }
        }

        public void Init()
        {
            m_Curves.Clear();
        }
        private Dictionary<int, AnimationCurve> m_Curves = new Dictionary<int, AnimationCurve>();
        public AnimationCurve GetAnimationCurve(EngineCurve curve)
        {
            int _hash = curve.GetCoustomHashCode();
#if UNITY_EDITOR
            if (!Application.isPlaying) _hash = curve.GetHashCode();
#endif
            if (!m_Curves.ContainsKey(_hash))
            {
                m_Curves.Add(_hash, CastToAnimationCurve(curve));
            }
            return m_Curves[_hash];
        }
        public void SetAnimationCurve(EngineCurve curve, AnimationCurve _animCurve)
        {
            int _hash = curve.GetCoustomHashCode();
#if UNITY_EDITOR
            if (!Application.isPlaying) _hash = curve.GetHashCode();
#endif
            if (!m_Curves.TryAdd(_hash, CastToAnimationCurve(curve)))
                m_Curves[_hash] = _animCurve;
            curve.SetValue(_animCurve);
        }

        // 重建AnimationCurve
        public AnimationCurve CastToAnimationCurve(EngineCurve _curve)
        {
            AnimationCurve curve = new AnimationCurve();
            foreach (var skey in _curve.keys)
            {
                curve.AddKey(skey.ToKeyframe());
            }
            curve.preWrapMode = _curve.preWrapMode;
            curve.postWrapMode = _curve.postWrapMode;
            return curve;
        }
    }
}