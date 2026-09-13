using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public partial class ActionStateMachine
    {
        public delegate void DOnGIntChanged(int _old, int _new);
        public delegate void DOnGFloatChanged(float _old, float _new);
        public delegate void DOnGEnumChanged(byte _old, byte _new);
        public delegate void DOnGBoolChanged(bool _old, bool _new);
        public delegate void DOnGUnitChanged(TargetUnit _old, TargetUnit _new);

        public Dictionary<(ushort, ushort), List<DOnGIntChanged>> m_OnGIntChanged = new();
        private Dictionary<(ushort, ushort), List<DOnGFloatChanged>> m_OnGFloatChanged = new();
        private Dictionary<(ushort, ushort), List<DOnGEnumChanged>> m_OnGEnumChanged = new();
        private Dictionary<(ushort, ushort), List<DOnGBoolChanged>> m_OnGBoolChanged = new();
        private Dictionary<(ushort, ushort, byte), List<DOnGUnitChanged>> m_OnGUnitChanged = new();

        #region SendChangeMessage

        public void SendChangeMessage_GInt(GInt gv, int _old, int _new)
            => SendChangeMessage_GInt(gv.mValueGroupIndex, gv.mValueIndex, _old, _new);

        public void SendChangeMessage_GInt(ushort group, ushort id, int _old, int _new)
        {
            if (_old == _new) return;
            if (m_OnGIntChanged.TryGetValue((group, id), out var list))
                for (int i = 0; i < list.Count; i++) list[i].Invoke(_old, _new);
        }

        public void SendChangeMessage_GFloat(GFloat gv, float _old, float _new)
            => SendChangeMessage_GFloat(gv.mValueGroupIndex, gv.mValueIndex, _old, _new);

        public void SendChangeMessage_GFloat(ushort group, ushort id, float _old, float _new)
        {
            if (_old == _new) return;
            if (m_OnGFloatChanged.TryGetValue((group, id), out var list))
                for (int i = 0; i < list.Count; i++) list[i].Invoke(_old, _new);
        }

        public void SendChangeMessage_GEnum(GEnum gv, byte _old, byte _new)
            => SendChangeMessage_GEnum(gv.mValueGroupIndex, gv.mValueIndex, _old, _new);

        public void SendChangeMessage_GEnum(ushort group, ushort id, byte _old, byte _new)
        {
            if (_old == _new) return;
            if (m_OnGEnumChanged.TryGetValue((group, id), out var list))
                for (int i = 0; i < list.Count; i++) list[i].Invoke(_old, _new);
        }

        public void SendChangeMessage_GBool(GBool gv, bool _old, bool _new)
            => SendChangeMessage_GBool(gv.mValueGroupIndex, gv.mValueIndex, _old, _new);

        public void SendChangeMessage_GBool(ushort group, ushort id, bool _old, bool _new)
        {
            if (_old == _new) return;
            if (m_OnGBoolChanged.TryGetValue((group, id), out var list))
                for (int i = 0; i < list.Count; i++) list[i].Invoke(_old, _new);
        }

        public void SendChangeMessage_GUnit(GUnit gv, TargetUnit _old, TargetUnit _new)
            => SendChangeMessage_GUnit(gv.mValueGroupIndex, gv.mValueIndex, gv.mSerValue, _old, _new);

        public void SendChangeMessage_GUnit(ushort group, ushort id, byte ser, TargetUnit _old, TargetUnit _new)
        {
            if (_old == _new) return;
            if (m_OnGUnitChanged.TryGetValue((group, id, ser), out var list))
                for (int i = 0; i < list.Count; i++) list[i].Invoke(_old, _new);
        }


        #endregion

        #region Register / Remove

        public void OnGIntChanged(GInt gv, DOnGIntChanged callback)
        {
            var key = gv.GetKey();
            if (!m_OnGIntChanged.TryGetValue(key, out var list))
            {
                list = new List<DOnGIntChanged>(4);
                m_OnGIntChanged.Add(key, list);
            }
            list.Add(callback);
        }

        public void RemoveGIntChanged(GInt gv, DOnGIntChanged callback)
        {
            var key = gv.GetKey();
            if (m_OnGIntChanged.TryGetValue(key, out var list))
            {
                list.Remove(callback);
                //if (list.Count == 0) m_OnGIntChanged.Remove(key);
            }
        }

        public void OnGFloatChanged(GFloat gv, DOnGFloatChanged callback)
        {
            var key = gv.GetKey();
            if (!m_OnGFloatChanged.TryGetValue(key, out var list))
            {
                list = new List<DOnGFloatChanged>(4);
                m_OnGFloatChanged.Add(key, list);
            }
            list.Add(callback);
        }

        public void RemoveGFloatChanged(GFloat gv, DOnGFloatChanged callback)
        {
            var key = gv.GetKey();
            if (m_OnGFloatChanged.TryGetValue(key, out var list))
            {
                list.Remove(callback);
                //if (list.Count == 0) m_OnGFloatChanged.Remove(key);
            }
        }

        public void OnGEnumChanged(GEnum gv, DOnGEnumChanged callback)
        {
            var key = gv.GetKey();
            if (!m_OnGEnumChanged.TryGetValue(key, out var list))
            {
                list = new List<DOnGEnumChanged>(4);
                m_OnGEnumChanged.Add(key, list);
            }
            list.Add(callback);
        }

        public void RemoveGEnumChanged(GEnum gv, DOnGEnumChanged callback)
        {
            var key = gv.GetKey();
            if (m_OnGEnumChanged.TryGetValue(key, out var list))
            {
                list.Remove(callback);
                //if (list.Count == 0) m_OnGEnumChanged.Remove(key);
            }
        }

        public void OnGBoolChanged(GBool gv, DOnGBoolChanged callback)
        {
            var key = gv.GetKey();
            if (!m_OnGBoolChanged.TryGetValue(key, out var list))
            {
                list = new List<DOnGBoolChanged>(4);
                m_OnGBoolChanged.Add(key, list);
            }
            list.Add(callback);
        }

        public void RemoveGBoolChanged(GBool gv, DOnGBoolChanged callback)
        {
            var key = gv.GetKey();
            if (m_OnGBoolChanged.TryGetValue(key, out var list))
            {
                list.Remove(callback);
                //if (list.Count == 0) m_OnGBoolChanged.Remove(key);
            }
        }

        public void OnGUnitChanged(GUnit gv, DOnGUnitChanged callback)
        {
            var key = gv.GetKey();
            if (!m_OnGUnitChanged.TryGetValue(key, out var list))
            {
                list = new List<DOnGUnitChanged>(4);
                m_OnGUnitChanged.Add(key, list);
            }
            list.Add(callback);
        }

        public void RemoveGUnitChanged(GUnit gv, DOnGUnitChanged callback)
        {
            var key = gv.GetKey();
            if (m_OnGUnitChanged.TryGetValue(key, out var list))
            {
                list.Remove(callback);
                //if (list.Count == 0) m_OnGUnitChanged.Remove(key);
            }
        }


        #endregion

        private void ClearGValueOnChangeEvent()
        {
            foreach (var list in m_OnGIntChanged.Values) list.Clear();
            //m_OnGIntChanged.Clear();
            foreach (var list in m_OnGFloatChanged.Values) list.Clear();
            //m_OnGFloatChanged.Clear();
            foreach (var list in m_OnGEnumChanged.Values) list.Clear();
            //m_OnGEnumChanged.Clear();
            foreach (var list in m_OnGBoolChanged.Values) list.Clear();
            //m_OnGBoolChanged.Clear();
            foreach (var list in m_OnGUnitChanged.Values) list.Clear();
            //m_OnGUnitChanged.Clear();
        }

        // public EngineGValue GValue;
        //public Dictionary<ushort, EngineGValue> GValues;
        public GValuePool GValuePool = new GValuePool();
        public Dictionary<ushort, EngineEquation> Equations;

        public Dictionary<(ushort, ushort, byte), PointData> PointDic = new();
        public Dictionary<(ushort, ushort, byte), Transform> TransfromDic = new();
        public Dictionary<(ushort, ushort, byte), TargetUnit> UnitDic = new();


        public int GetLayer(int _curLayer)
        {
            if (_curLayer < 0)
            {
                int selectID = _curLayer * -1 - 1;
                if (TryGetComponent(out ActionEngine_LayerMask layerMask, nameof(ActionEngine_LayerMask)))
                {
#if UNITY_EDITOR
                    if (selectID < 0)
                    {
                        EngineDebug.LogError("LayerMask序号小于0了");
                        return layerMask.mLayers[0];
                    }
                    else if (selectID >= layerMask.mLayers.Length)
                    {
                        EngineDebug.LogError($"LayerMask序号大于数组长度: Length:[{layerMask.mLayers.Length}], SelectID:[{selectID}]");
                        return layerMask.mLayers[^1];
                    }
#endif

                    return layerMask.mLayers[selectID];
                }
                return _curLayer * -1 - 1;
            }
            return _curLayer;
        }
        public PointData GetPoint(ushort _groupID, ushort ID, byte Val)
        {
            if (PointDic.TryGetValue((_groupID, ID, Val), out PointData pointData)) return pointData;
            return new PointData();
        }
        public void SetPoint(ushort _groupID, ushort ID, byte Val, PointData pointData)
        {
            if (!PointDic.TryAdd((_groupID, ID, Val), pointData))
                PointDic[(_groupID, ID, Val)] = pointData;
        }
        public Transform GetTransform(ushort _groupID, ushort ID, byte Val)
        {
            if (TransfromDic.TryGetValue((_groupID, ID, Val), out Transform pointData))
            {
                if (pointData is null)
                {
                    TransfromDic.Remove((_groupID, ID, Val));
                    return null;
                }
                return pointData;
            }
            return null;
        }
        public void SetTransform(ushort _groupID, ushort ID, byte Val, Transform pointData)
        {
            if (!TransfromDic.TryAdd((_groupID, ID, Val), pointData))
                TransfromDic[(_groupID, ID, Val)] = pointData;
        }
        public TargetUnit GetUnit(ushort _groupID, ushort ID, byte Val)
        {
            if (UnitDic.TryGetValue((_groupID, ID, Val), out TargetUnit pointData))
            {
                if (pointData is null)
                {
                    UnitDic.Remove((_groupID, ID, Val));
                    return null;
                }
                return pointData;
            }
            return null;
        }
        public void SetUnit(ushort _groupID, ushort ID, byte Val, TargetUnit pointData)
        {
            if (!UnitDic.TryAdd((_groupID, ID, Val), pointData))
                UnitDic[(_groupID, ID, Val)] = pointData;
        }

        public bool RemovePoint(ushort _groupID, ushort ID, byte Val)
        {
            return PointDic.Remove((_groupID, ID, Val));
        }
        public bool RemoveTransform(ushort _groupID, ushort ID, byte Val)
        {
            return TransfromDic.Remove((_groupID, ID, Val));
        }
        public bool RemoveUnit(ushort _groupID, ushort ID, byte Val)
        {
            return UnitDic.Remove((_groupID, ID, Val));
        }
        private void GValueInit(Dictionary<ushort, EngineGValue> _gValue, Dictionary<ushort, EngineEquation> _equation)
        {
            //GValues = _gValue;
            Equations = _equation;
        }
    }
}