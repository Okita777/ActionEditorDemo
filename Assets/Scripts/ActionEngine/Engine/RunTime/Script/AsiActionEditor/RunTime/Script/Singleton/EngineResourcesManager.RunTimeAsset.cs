using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public partial class EngineResourcesManager : SingletonBase<EngineResourcesManager>
    {
        //当前执行状态
        public ActionState Active_ActionState;
        public ActionEvent Active_ActionEvent;
        public ActionStateInfo Active_ActionStateInfo;
        public IInterruptCondition Active_InterruptCondition;
        public IActionEventData Active_ActionEventData;

        private RaycastHit[] mRaycastHits;
        private Collider[] mColliders;
        private HashSet<Object> mHashSet_Object;
        private List<int>[] mIntListArry;
        private List<bool>[] mBoolListArry;
        private List<float>[] mFloatListArry;
        private List<string>[] mStringListArry;
        private List<Transform>[] mTransListArry;
        private List<ActionEngine_Unit>[] mUnitArry;
        private List<PointData>[] mPointDataArry;
        private int mTransIndex = 0, mUnitIndex = 0, mPointIndex = 0;
        private int mIntIndex = 0, mBoolIndex = 0, mFloatIndex = 0, mStringIndex = 0;
        private int[] mNullGroupInt = new int[0];
        private float[] mNullGroupFloat = new float[0];

        private int mBluePrint_MaxListNumber => MotionEngineConst.BluePrint_MaxListNumber;
        private int mBluePrint_ListMaxCount => MotionEngineConst.BluePrint_ListMaxCount;

        public RaycastHit[] RaycastHits => mRaycastHits;
        public Collider[] Colliders => mColliders;
        private void OnInit_RT()
        {
            mRaycastHits = new RaycastHit[300];
            mColliders = new Collider[300];

            mIntIndex = -1;
            mBoolIndex = -1;
            mFloatIndex = -1;
            mStringIndex = -1;
            mTransIndex = -1;
            mUnitIndex = -1;
            mPointIndex = -1;

            mPointDataArry = new List<PointData>[mBluePrint_MaxListNumber];
            mTransListArry = new List<Transform>[mBluePrint_MaxListNumber];
            mUnitArry = new List<ActionEngine_Unit>[mBluePrint_MaxListNumber];

            mIntListArry = new List<int>[mBluePrint_MaxListNumber];
            mBoolListArry = new List<bool>[mBluePrint_MaxListNumber];
            mFloatListArry = new List<float>[mBluePrint_MaxListNumber];
            mStringListArry = new List<string>[mBluePrint_MaxListNumber];
            mHashSet_Object = new HashSet<Object>(1024);
            for (int i = 0; i < mPointDataArry.Length; i++)
                mPointDataArry[i] = new List<PointData>(mBluePrint_ListMaxCount);
            for (int i = 0; i < mTransListArry.Length; i++)
                mTransListArry[i] = new List<Transform>(mBluePrint_ListMaxCount);
            for (int i = 0; i < mUnitArry.Length; i++)
                mUnitArry[i] = new List<ActionEngine_Unit>(mBluePrint_ListMaxCount);

            for (int i = 0; i < mIntListArry.Length; i++)
                mIntListArry[i] = new List<int>(mBluePrint_ListMaxCount);
            for (int i = 0; i < mBoolListArry.Length; i++)
                mBoolListArry[i] = new List<bool>(mBluePrint_ListMaxCount);
            for (int i = 0; i < mFloatListArry.Length; i++)
                mFloatListArry[i] = new List<float>(mBluePrint_ListMaxCount);
            for (int i = 0; i < mStringListArry.Length; i++)
                mStringListArry[i] = new List<string>(mBluePrint_ListMaxCount);
        }

        public int[] GetGroupIntNull => mNullGroupInt;
        public float[] GetGroupFloatNull => mNullGroupFloat;

        public HashSet<Object> HashSet_Object()
        {
            mHashSet_Object.Clear();
            return mHashSet_Object;
        }
        /// <summary>
        /// 避免重复实例List，在此可自动分配干净的List，长度为200，如果需要更长的长度请另外独自实现。
        /// </summary>
        /// <returns>返回长度为200的干净List</returns>
        public List<Transform> CreateTransforms()
        {
            mTransIndex++;
            if (mTransIndex >= mTransListArry.Length) mTransIndex = 0;
            mTransListArry[mTransIndex].Clear();
            return mTransListArry[mTransIndex];
        }
        /// <summary>
        ///  避免重复实例List，在此可自动分配干净的List，长度为200，如果需要更长的长度请另外独自实现。
        /// </summary>
        /// <returns>返回长度为200的干净List</returns>
        public List<PointData> CreatePoints()
        {
            mPointIndex++;
            if (mPointIndex >= mPointDataArry.Length) mPointIndex = 0;
            mPointDataArry[mPointIndex].Clear();
            return mPointDataArry[mPointIndex];
        }
        /// <summary>
        ///  避免重复实例List，在此可自动分配干净的List，长度为200，如果需要更长的长度请另外独自实现。
        /// </summary>
        /// <returns>返回长度为200的干净List</returns>
        public List<ActionEngine_Unit> CreateUnits()
        {
            mUnitIndex++;
            if (mUnitIndex >= mUnitArry.Length) mUnitIndex = 0;
            mUnitArry[mUnitIndex].Clear();
            //EngineDebug.LogError($"申请可复用列表[{mUnitArry[mUnitIndex].GetHashCode()}] [{mUnitIndex}]");
            return mUnitArry[mUnitIndex];
        }

        /// <summary>
        ///  避免重复实例List，在此可自动分配干净的List，长度为200，如果需要更长的长度请另外独自实现。
        /// </summary>
        /// <returns>返回长度为200的干净List</returns>
        public List<int> CreateInts()
        {
            mIntIndex++;
            if (mIntIndex >= mIntListArry.Length) mIntIndex = 0;
            mIntListArry[mIntIndex].Clear();
            return mIntListArry[mIntIndex];
        }
        /// <summary>
        ///  避免重复实例List，在此可自动分配干净的List，长度为200，如果需要更长的长度请另外独自实现。
        /// </summary>
        /// <returns>返回长度为200的干净List</returns>
        public List<bool> CreateBools()
        {
            mBoolIndex++;
            if (mBoolIndex >= mBoolListArry.Length) mBoolIndex = 0;
            mBoolListArry[mBoolIndex].Clear();
            return mBoolListArry[mBoolIndex];
        }
        /// <summary>
        ///  避免重复实例List，在此可自动分配干净的List，长度为200，如果需要更长的长度请另外独自实现。
        /// </summary>
        /// <returns>返回长度为200的干净List</returns>
        public List<float> CreateFloats()
        {
            mFloatIndex++;
            if (mFloatIndex >= mFloatListArry.Length) mFloatIndex = 0;
            mFloatListArry[mFloatIndex].Clear();
            return mFloatListArry[mFloatIndex];
        }
        /// <summary>
        ///  避免重复实例List，在此可自动分配干净的List，长度为200，如果需要更长的长度请另外独自实现。
        /// </summary>
        /// <returns>返回长度为200的干净List</returns>
        public List<string> CreateStrings()
        {
            mStringIndex++;
            if (mStringIndex >= mStringListArry.Length) mStringIndex = 0;
            mStringListArry[mStringIndex].Clear();
            return mStringListArry[mStringIndex];
        }
    }
}