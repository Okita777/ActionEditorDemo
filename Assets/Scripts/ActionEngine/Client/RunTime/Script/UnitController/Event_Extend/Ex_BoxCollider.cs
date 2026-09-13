using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class Ex_BoxCollider : StaticActionLogics
    {
        private Dictionary<int, BoxCollider> mBoxDic;
        private Dictionary<int, CapsuleCollider> mCapsuleDic;
        private Dictionary<int, SphereCollider> mSphereDic;
        public override void OnStart(ActionStateMachine _actionState)
        {
            mBoxDic = new Dictionary<int, BoxCollider>(8);
            mCapsuleDic = new Dictionary<int, CapsuleCollider>(8);
            mSphereDic = new Dictionary<int, SphereCollider>(8);
        }

        public BoxCollider GetBox(int key, ActionEngine_Unit unit)
        {
            BoxCollider collider;
            if (!mBoxDic.TryGetValue(key, out collider))
            {
                collider = ActionEngineManager_HitBox.Instance.GetBoxCollider();
                mBoxDic.Add(key, collider);
            }
            ActionEngineManager_HitBox.Instance.SetTargetUnit(collider, unit);
            return collider;
        }

        public CapsuleCollider GetCapsule(int key, ActionEngine_Unit unit)
        {
            CapsuleCollider collider;
            if (!mCapsuleDic.TryGetValue(key, out collider))
            {
                collider = ActionEngineManager_HitBox.Instance.GetCapsuleCollider();
                mCapsuleDic.Add(key, collider);
            }
            ActionEngineManager_HitBox.Instance.SetTargetUnit(collider, unit);
            return collider;
        }

        public SphereCollider GetSphere(int key, ActionEngine_Unit unit)
        {
            SphereCollider collider;
            if (!mSphereDic.TryGetValue(key, out collider))
            {
                collider = ActionEngineManager_HitBox.Instance.GetSphereCollider();
                mSphereDic.Add(key, collider);
            }
            ActionEngineManager_HitBox.Instance.SetTargetUnit(collider, unit);
            return collider;
        }

        public bool DestoryBoxCollider(int key)
        {
            if (mBoxDic.TryGetValue(key, out BoxCollider collider))
            {
                ActionEngineManager_HitBox.Instance.Destory(collider);
                mBoxDic.Remove(key);
                return true;
            }
            return false;
        }

        public bool DestoryCapsuleCollider(int key)
        {
            if (mCapsuleDic.TryGetValue(key, out CapsuleCollider collider))
            {
                ActionEngineManager_HitBox.Instance.Destory(collider);
                mCapsuleDic.Remove(key);
                return true;
            }
            return false;
        }

        public bool DestorySphereCollider(int key)
        {
            if (mSphereDic.TryGetValue(key, out SphereCollider collider))
            {
                ActionEngineManager_HitBox.Instance.Destory(collider);
                mSphereDic.Remove(key);
                return true;
            }
            return false;
        }
    }
}