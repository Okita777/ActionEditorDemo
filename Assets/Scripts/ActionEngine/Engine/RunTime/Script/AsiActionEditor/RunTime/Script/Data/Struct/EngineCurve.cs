using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class EngineCurve
    {
        public List<SerializableKeyframe> keys = new List<SerializableKeyframe>();
        public WrapMode preWrapMode;
        public WrapMode postWrapMode;
        public AnimationCurve Data => ConfigManager.Instance.GetAnimationCurve(this);
        [SerializeField] private int mHashCode;
        // 从AnimationCurve转换
        public EngineCurve(AnimationCurve curve)
        {
            foreach (Keyframe key in curve.keys)
            {
                keys.Add(new SerializableKeyframe(key));
            }
            preWrapMode = curve.preWrapMode;
            postWrapMode = curve.postWrapMode;
            SetHashCode();
        }

        public void SetValue(AnimationCurve curve)
        {
            keys.Clear();
            foreach (Keyframe key in curve.keys)
            {
                keys.Add(new SerializableKeyframe(key));
            }
            preWrapMode = curve.preWrapMode;
            postWrapMode = curve.postWrapMode;
            SetHashCode();
        }

        public EngineCurve Clone()
        {
            EngineCurve _curve = new EngineCurve(Data);
            //_curve.preWrapMode = preWrapMode;
            //_curve.postWrapMode = postWrapMode;
            return _curve;
        }

        private void SetHashCode()
        {
            mHashCode = keys.Count * -10000;
            mHashCode += (int)preWrapMode * -100;
            mHashCode += (int)preWrapMode * -1000;
            int _index = 0;
            foreach (SerializableKeyframe key in keys)
            {
                mHashCode += (int)(key.time * 100 * _index);
                mHashCode += (int)(key.value * 110);
                mHashCode += (int)(key.inTangent * 120);
                mHashCode += (int)(key.outTangent * 130);

                mHashCode += (key.tangentMode);
                mHashCode += (key.weightedMode);
                mHashCode += (int)(key.inWeight * 140);
                mHashCode += (int)(key.outWeight * 150);
                _index++;
            }
        }

        public int GetCoustomHashCode()
        {
            return mHashCode;
        }
    }

    [System.Serializable]
    public class SerializableKeyframe
    {
        public float time;
        public float value;
        public float inTangent;
        public float outTangent;
        public int tangentMode;  // 注意：Unity内部用int存储切线模式
        public int weightedMode; // 权重模式（0=None, 1=In, 2=Out, 3=Both）
        public float inWeight;
        public float outWeight;

        // 从Unity的Keyframe转换
        public SerializableKeyframe(Keyframe key)
        {
            time = key.time;
            value = key.value;
            inTangent = key.inTangent;
            outTangent = key.outTangent;
            tangentMode = key.tangentMode; // 需通过反射获取，见下方注意点
            weightedMode = (int)key.weightedMode;
            inWeight = key.inWeight;
            outWeight = key.outWeight;
        }

        // 转换为Unity的Keyframe
        public Keyframe ToKeyframe()
        {
            Keyframe key = new Keyframe(time, value, inTangent, outTangent, inWeight, outWeight);
            key.weightedMode = (WeightedMode)weightedMode;
            // 切线模式需通过反射设置
            return key;
        }
    }
}