using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class EngineGValue
    {
        public ushort mID;

        public bool[] mEngineBool;
        public int[] mEngineInt;
        public float[] mEngineFloat;
        public string[] mEngineString;
        public byte[] mEngineEnum;
        public byte[] mEnginePointData;
        public byte[] mEngineUnit;
        public byte[] mEngineTransform;

        public int[][] mEngineGroupInt;
        public float[][] mEngineGroupFloat;
        public bool[][] mEngineGroupBool;
        public string[][] mEngineGroupString;
        //public byte[] mEngineGroupPointData;
        //public byte[] mEngineGroupUnit;
        //public byte[] mEngineGroupTransform;
        public GDictionaryDefinition[] mEngineDictionary;
        public Vector3[] mEngineVector3;

        public int[] EditorID;//??????????ID  ????????ID
        public ushort[] RunTimeID;
        public EGValueType[] ValueType;

        /// <summary>
        /// ??????????????????????? <see cref="EditorID"/> / <see cref="RunTimeID"/> / <see cref="ValueType"/> ?????
        /// ? null ???????????????????????????????????????????????????????????????
        /// </summary>
        public bool[] NetSync;

        [NonSerialized] private bool mIsInit = false;
        [NonSerialized] Dictionary<int, int> mDic = null;

        [NonSerialized] private Dictionary<(EGValueType, ushort), bool> mNetSyncDic = null;
        //public EngineGValue Clone()
        //{
        //    EngineGValue _gValue = new EngineGValue();
        //    _gValue.mEngineBool = mEngineBool.ToArray();
        //    _gValue.mEngineInt = mEngineInt.ToArray();
        //    _gValue.mEngineFloat = mEngineFloat.ToArray();
        //    _gValue.mEngineString = mEngineString.ToArray();
        //    _gValue.mEngineEnum = mEngineEnum.ToArray();
        //    _gValue.mEnginePointData = mEnginePointData.ToArray();
        //    _gValue.mEngineUnit = mEngineUnit.ToArray();
        //    _gValue.mEngineTransform = mEngineTransform.ToArray();

        //    _gValue.mEngineGroupInt = mEngineGroupInt.ToArray();
        //    _gValue.mEngineGroupFloat = mEngineGroupFloat.ToArray();
        //    _gValue.mEngineGroupBool = mEngineGroupBool.ToArray();
        //    _gValue.mEngineGroupString = mEngineGroupString.ToArray();
        //    _gValue.mEngineGroupPointData = mEngineGroupPointData.ToArray();
        //    _gValue.mEngineGroupUnit = mEngineGroupUnit.ToArray();
        //    _gValue.mEngineGroupTransform = mEngineGroupTransform.ToArray();
        //    _gValue.mEngineVector3 = mEngineVector3.ToArray();

        //    return _gValue;
        //}

        public bool GetRealyID(int _editorID, out int _realyID)
        {
            if (!mIsInit)
            {
                mDic = new Dictionary<int, int>();
                for (int i = 0; i < EditorID.Length; i++)
                {
                    if (!mDic.TryAdd(EditorID[i], i))
                    {
                        EngineDebug.LogWarning($"GV?????????, ????????ID[<color=#ff0000>{EditorID[i]}</color>], ??????ID[{mID}], ???ID[{i}]");
                    }
                }

                mIsInit = true;
            }
            return mDic.TryGetValue(_editorID, out _realyID);
        }

        /// <summary>
        /// ?????????????��??????????????????? (????, ?????ID) ?????��?��?
        /// ??????????????????????RunTimeID ??????????��??????????????????��??
        /// </summary>
        public bool IsNetSync(EGValueType _type, ushort _runTimeID)
        {
            if (NetSync == null || EditorID == null || ValueType == null || RunTimeID == null) return true;
            if (NetSync.Length != EditorID.Length ||
                ValueType.Length != EditorID.Length ||
                RunTimeID.Length != EditorID.Length) return true;

            if (mNetSyncDic == null)
            {
                mNetSyncDic = new Dictionary<(EGValueType, ushort), bool>(EditorID.Length);
                for (int i = 0; i < EditorID.Length; i++)
                {
                    mNetSyncDic[(ValueType[i], RunTimeID[i])] = NetSync[i];
                }
            }

            // ?��??????��?��??????�E???????????????
            return !mNetSyncDic.TryGetValue((_type, _runTimeID), out bool _sync) || _sync;
        }
    }

    [System.Serializable]
    public class EngineEquation
    {
        public ushort mID;
        [SerializeReference] public GValueEquation.GValueEquation_Part[] mGValueEquation;
    }
}