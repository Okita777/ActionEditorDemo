using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public abstract class SerializedAnimCurve : AnimationCurve, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        private List<Keyframe> serKeys = new List<Keyframe>();
        //[SerializeField] private int _hashCode;
        //public override int GetHashCode()
        //{
        //    return _hashCode;
        //}
        //取出
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            //this.ClearKeys();
            foreach (Keyframe VARIABLE in serKeys)
            {
                AddKey(VARIABLE);
            }
        }

        //写入
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            serKeys.Clear();
            foreach (Keyframe VARIABLE in keys)
            {
                serKeys.Add(VARIABLE);
            }
        }
    }
}