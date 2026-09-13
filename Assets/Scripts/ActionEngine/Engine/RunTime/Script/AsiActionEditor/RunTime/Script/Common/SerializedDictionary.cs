using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    //unity自身无法序列化字典  用List代替
    public abstract class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        private List<TKey> _keyData = new List<TKey>();
        [SerializeField, HideInInspector]
        private List<TValue> _valueData = new List<TValue>();

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            this.Clear();
            for (int i = 0; i < this._keyData.Count; i++)
            {
                this[this._keyData[i]] = this._valueData[i];
                // this.Add(this._keyData[i],this._valueData[i]);
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            this._keyData.Clear();
            this._valueData.Clear();
            foreach (var VARIABLE in this)
            {
                this._keyData.Add(VARIABLE.Key);
                this._valueData.Add(VARIABLE.Value);
            }
        }
    }


}