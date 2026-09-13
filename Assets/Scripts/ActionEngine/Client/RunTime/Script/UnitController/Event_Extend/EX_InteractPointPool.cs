using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class EX_InteractPointPool : StaticActionLogics
    {
        #region Struct
        private struct STransform
        {
            public Vector3 pos;
            public Quaternion rot;

            public STransform(Vector3 pos, Quaternion rot)
            {
                this.pos = pos;
                this.rot = rot;
            }
        }
        #endregion
        //被注册的所有点位
        private Dictionary<string, STransform> m_AllSTransforms = new Dictionary<string, STransform>();
        private Dictionary<int, STransform> m_AllSTransformID = new Dictionary<int, STransform>();

        //被注册的所有变换
        private Dictionary<string, Transform> m_AllTransforms = new Dictionary<string, Transform>();
        private Dictionary<int, Transform> m_AllTransformID = new Dictionary<int, Transform>();

        //点数据
        public void AddOrUpdatePoint(string _name, Vector3 _pos, Quaternion _rot) => OnAddOrUpdatePoint(_name, _pos, _rot);
        public void AddOrUpdatePoint(int _name, Vector3 _pos, Quaternion _rot) => OnAddOrUpdatePoint(_name, _pos, _rot);
        public bool TryGetPoint(string _name, out Vector3 _pos, out Quaternion _rot) => OnTryGetPoint(_name, out _pos, out _rot);
        public bool TryGetPoint(int _name, out Vector3 _pos, out Quaternion _rot) => OnTryGetPoint(_name, out _pos, out _rot);
        public void RemovePoint(string _name) => OnRemovePoint(_name);
        public void RemovePoint(int _name) => OnRemovePoint(_name);

        //变换数据
        public void AddOrUpdateTrans(string _name, Transform _transform) => OnAddOrUpdateTrans(_name, _transform);
        public void AddOrUpdateTrans(int _name, Transform _transform) => OnAddOrUpdateTrans(_name, _transform);
        public bool TryGetTrans(string _name, out Transform _transform) => OnTryGetTrans(_name, out _transform);
        public bool TryGetTrans(int _name, out Transform _transform) => OnTryGetTrans(_name, out _transform);
        public void RemoveTrans(string _name) => OnRemoveTrans(_name);
        public void RemoveTrans(int _name) => OnRemoveTrans(_name);

        #region 点数据
        private void OnAddOrUpdatePoint(string _name, Vector3 _pos, Quaternion _rot)
        {
            STransform _point = new STransform(_pos, _rot);
            if (!m_AllSTransforms.TryAdd(_name, _point)) m_AllSTransforms[_name] = _point;
        }

        private bool OnTryGetPoint(string _name, out Vector3 _pos, out Quaternion _rot)
        {
            if (m_AllSTransforms.TryGetValue(_name, out STransform _point))
            {
                _pos = _point.pos;
                _rot = _point.rot;
                return true;
            }
            _pos = Vector3.zero;
            _rot = Quaternion.identity;
            return false;
        }

        private void OnRemovePoint(string _name)
        {
            if (m_AllSTransforms.ContainsKey(_name)) m_AllSTransforms.Remove(_name);
        }

        private void OnAddOrUpdatePoint(int _name, Vector3 _pos, Quaternion _rot)
        {
            STransform _point = new STransform(_pos, _rot);
            if (!m_AllSTransformID.TryAdd(_name, _point)) m_AllSTransformID[_name] = _point;
        }

        private bool OnTryGetPoint(int _name, out Vector3 _pos, out Quaternion _rot)
        {
            if (m_AllSTransformID.TryGetValue(_name, out STransform _point))
            {
                _pos = _point.pos;
                _rot = _point.rot;
                return true;
            }
            _pos = Vector3.zero;
            _rot = Quaternion.identity;
            return false;
        }

        private void OnRemovePoint(int _name)
        {
            if (m_AllSTransformID.ContainsKey(_name)) m_AllSTransformID.Remove(_name);
        }

        #endregion

        #region Transform数据
        private void OnAddOrUpdateTrans(string _name, Transform _transform)
        {
            if (!m_AllTransforms.TryAdd(_name, _transform)) m_AllTransforms[_name] = _transform;
        }

        private bool OnTryGetTrans(string _name, out Transform _transform)
        {
            if (m_AllTransforms.TryGetValue(_name, out _transform))
            {
                if (_transform is null)
                {
                    m_AllTransforms.Remove(_name);
                    _transform = null;
                    return false;
                }
                return true;
            }

            _transform = null;
            return false;
        }

        private void OnRemoveTrans(string _name)
        {
            if (m_AllTransforms.ContainsKey(_name)) m_AllTransforms.Remove(_name);
        }

        private void OnAddOrUpdateTrans(int _name, Transform _transform)
        {
            if (!m_AllTransformID.TryAdd(_name, _transform)) m_AllTransformID[_name] = _transform;
        }

        private bool OnTryGetTrans(int _name, out Transform _transform)
        {
            if (m_AllTransformID.TryGetValue(_name, out _transform))
            {
                if (_transform is null)
                {
                    m_AllTransformID.Remove(_name);
                    _transform = null;
                    return false;
                }
                return true;
            }

            _transform = null;
            return false;
        }

        private void OnRemoveTrans(int _name)
        {
            if (m_AllTransformID.ContainsKey(_name)) m_AllTransformID.Remove(_name);
        }
        #endregion
    }
}