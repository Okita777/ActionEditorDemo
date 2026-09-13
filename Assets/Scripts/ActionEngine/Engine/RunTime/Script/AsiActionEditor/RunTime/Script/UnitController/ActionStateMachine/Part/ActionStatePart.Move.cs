using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    // private delegate 
    public partial class ActionStatePart
    {
        public Vector3[] PosList = new Vector3[3];
        public Quaternion[] RotList = new Quaternion[3];
        public PointData HitPoint;
        public bool IsMoveInputPre = false;//位移输入的预输入
        public HashSet<ActionEngine_Unit> ExcludeUnits = new HashSet<ActionEngine_Unit>(3);
        public float GetPrivateFloat(int _hash, float _defaultValue = 0.0f)
        {
            if (mDic_Float.TryGetValue(_hash, out float _value)) return _value;
            mDic_Float.Add(_hash, _defaultValue);
            return _defaultValue;
        }
        public void SetPrivateFloat(int _hash, float value)
        {
            if (!mDic_Float.TryAdd(_hash, value))
                mDic_Float[_hash] = value;
        }
        public Vector3 GetPrivateVector3(int _hash, Vector3 _defaultValue)
        {
            if (mDic_Vector.TryGetValue(_hash, out Vector3 _value)) return _value;
            mDic_Vector.Add(_hash, _defaultValue);
            return _defaultValue;
        }
        public void SetPrivateVector3(int _hash, Vector3 value)
        {
            if (!mDic_Vector.TryAdd(_hash, value))
                mDic_Vector[_hash] = value;
        }
        //public float HitInterval = -1;
        //public float HitInterval_r;

        //public event OnAttackData OnHit; //命中时调用
        //public event OnBeHitData OnBeHit; //受击时调用
        //public delegate void OnAttackData(ActionEngine_Unit _SelfUnit, bool _isUnit, GameObject _gameObject, ActionEngine_Unit _onHiter);
        //public delegate void OnBeHitData(ActionEngine_Unit _SelfUnit);

        private Vector3 mDeltaPos;
        private Vector3 mVelocity;
        private Quaternion mDeltaRot;
        private float mGvavity;
        private float mVelocityY;
        private Dictionary<int, float> mDic_Float = new Dictionary<int, float>();
        private Dictionary<int, Vector3> mDic_Vector = new Dictionary<int, Vector3>();

        private void OnTransLate(Vector3 _deltaPos)
        {
            mDeltaPos += _deltaPos;
        }
        private void OnMove(Vector3 _velocity)
        {
            Debug.DrawRay(Pos, _velocity.normalized, Color.green, 1f);
            mVelocity += _velocity;
        }

        private void OnRotate(Quaternion _rot, bool _global)
        {
            if (_global)
            {
                mDeltaRot = _rot * mDeltaRot;
            }
            else
            {
                mDeltaRot = mDeltaRot * _rot;
            }
        }
        private void UpdateMove(float _deltaTime)
        {
            if (IsTem)
            {
                mVelocityY -= mGvavity * _deltaTime;
                if (mVelocityY < 0) mVelocityY = Mathf.Max(-mGvavity * 2, mVelocityY);
                else mVelocityY = Mathf.Min(mGvavity * 2, mVelocityY);

                Pos += mDeltaPos + (mVelocity + Vector3.up * mVelocityY) * _deltaTime;

                //Rot *= mDeltaRot;

                mDeltaPos = Vector3.zero;
                mVelocity = Vector3.zero;
            }
        }
    }
}