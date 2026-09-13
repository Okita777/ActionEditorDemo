using System.Collections.Generic;
using AsiActionEngine.RunTime;
// using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace AsiTimeLine.RunTime
{
    public partial class ActionEngineManager_HitBox : SingletonBase<ActionEngineManager_HitBox>
    {
        private ObjectPool<BoxCollider> mBoxColliderPool;
        private ObjectPool<CapsuleCollider> mCapsuleColliderPool;
        private ObjectPool<SphereCollider> mSphereColliderPool;
        private Dictionary<Collider, ActionEngine_HitBox> mColliderDic;
        private Transform mParent;
        private readonly string[] mColliderName
            = new[] { "Collider_Box", "Collider_Capsule", "Collider_Sphere" };

        public void Init(int _boxDefaultNumber, int _capsuleDefaultNumber, int _sphereDefaultNumber, Transform _parent)
        {
            mParent = _parent;
            mColliderDic = new Dictionary<Collider, ActionEngine_HitBox>(128);
            mBoxColliderPool = new ObjectPool<BoxCollider>(
                () => CreateCollider<BoxCollider>(mColliderName[0]),
                _object => { _object.gameObject.SetActive(true); },
                _object => { _object.gameObject.SetActive(false); },
                _object => { mColliderDic.Remove(_object); Object.Destroy(_object); },
                true,
                _boxDefaultNumber,
                1000
            );

            mCapsuleColliderPool = new ObjectPool<CapsuleCollider>(
                () => CreateCollider<CapsuleCollider>(mColliderName[1]),
                _object => { _object.gameObject.SetActive(true); },
                _object => { _object.gameObject.SetActive(false); },
                _object => { mColliderDic.Remove(_object); Object.Destroy(_object); },
                true,
                _capsuleDefaultNumber,
                1000
            );

            mSphereColliderPool = new ObjectPool<SphereCollider>(
                () => CreateCollider<SphereCollider>(mColliderName[2]),
                _object => { _object.gameObject.SetActive(true); },
                _object => { _object.gameObject.SetActive(false); },
                _object => { mColliderDic.Remove(_object); Object.Destroy(_object); },
                true,
                _sphereDefaultNumber,
                1000
            );
        }

        public void ClearAllPool()
        {
            mBoxColliderPool.Clear();
            mCapsuleColliderPool.Clear();
            mSphereColliderPool.Clear();
            mColliderDic.Clear();
        }

        public bool SetTargetUnit(Collider collider, ActionEngine_Unit unit)
        {
            if (mColliderDic.TryGetValue(collider, out ActionEngine_HitBox _hitBox))
            {
                _hitBox.MainUnit = unit;
                return true;
            }
            return false;
        }
        public BoxCollider GetBoxCollider() => mBoxColliderPool.Get();
        public CapsuleCollider GetCapsuleCollider() => mCapsuleColliderPool.Get();
        public SphereCollider GetSphereCollider() => mSphereColliderPool.Get();

        public void Destory(BoxCollider _collider) => mBoxColliderPool.Release(_collider);
        public void Destory(CapsuleCollider _collider) => mCapsuleColliderPool.Release(_collider);
        public void Destory(SphereCollider _collider) => mSphereColliderPool.Release(_collider);


        private T CreateCollider<T>(string name) where T : Collider
        {
            GameObject _object = new GameObject(name);
            ActionEngine_HitBox _hitBox = _object.AddComponent<ActionEngine_HitBox>();
            T _ReturnValue = _object.AddComponent<T>();
            _object.SetActive(false);
            _object.transform.SetParent(mParent);
            if (!mColliderDic.TryAdd(_ReturnValue, _hitBox))
                mColliderDic[_ReturnValue] = _hitBox;
            return _ReturnValue;
        }
    }
}
