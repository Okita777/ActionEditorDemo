using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class UnitWarp : IProperty
    {
        [SerializeField] protected int mID;
        [SerializeField] protected int mCameID;
        //[SerializeField] protected string mName;
        [SerializeField] protected string mModelPath; //模型路径
        [SerializeField] protected int mAction; //动编数据
        [SerializeField] protected GValue_Setting mGValueSetting;
        [SerializeField] protected int[] mTags = new int[0];
        [OptionalField]
        protected List<ServerComponentSnapshot> mServerComponentSnapshots =
            new List<ServerComponentSnapshot>();
        [OptionalField]
        [SerializeField] protected ServerGameObjectSnapshot mServerGameObjectSnapshot;
        [OptionalField]
        [SerializeField] protected List<ServerComponentFlatSnapshot> mServerComponentFlatSnapshots =
            new List<ServerComponentFlatSnapshot>();

        #region Property

        public int ID
        {
            get { return mID; }
            set { mID = value; }
        }
        public int CameID
        {
            get { return mCameID; }
            set { mCameID = value; }
        }
        //public string Name
        //{
        //    get { return mName; }
        //    set { mName = value; }
        //}
        [EditorProperty("角色预制体", EditorPropertyType.EEPT_GameObject)]
        public string ModelPath
        {
            get { return mModelPath; }
            set { mModelPath = value; }
        }
        [EditorProperty("角色默认GValue", EditorPropertyType.EEPT_GValueSetting)]
        public GValue_Setting GValueSetting
        {
            get { return mGValueSetting; }
            set { mGValueSetting = value; }
        }
        [EditorProperty("角色标签", EditorPropertyType.EEPT_UnitTypeTag)]
        public int[] Tags
        {
            get { return mTags; }
            set { mTags = value; }
        }
        public int Action
        {
            get { return mAction; }
            set { mAction = value; }
        }
        public List<ServerComponentSnapshot> ServerComponentSnapshots
        {
            get { return mServerComponentSnapshots; }
            set { mServerComponentSnapshots = value; }
        }
        public ServerGameObjectSnapshot ServerGameObjectSnapshot
        {
            get { return mServerGameObjectSnapshot; }
            set { mServerGameObjectSnapshot = value; }
        }

        public void PrepareServerComponentSnapshotsForJson()
        {
            mServerComponentFlatSnapshots = ServerComponentSnapshotCodec.Encode(mServerComponentSnapshots);
        }

        public void RestoreServerComponentSnapshotsFromJson()
        {
            if (mServerComponentFlatSnapshots != null && mServerComponentFlatSnapshots.Count > 0)
            {
                mServerComponentSnapshots = ServerComponentSnapshotCodec.Decode(mServerComponentFlatSnapshots);
            }
            else if (mServerComponentSnapshots == null)
            {
                mServerComponentSnapshots = new List<ServerComponentSnapshot>();
            }
        }

        #endregion
        public UnitWarp(int _id, GValue_Setting _mGValueSetting)
        {
            ID = _id;
            mGValueSetting = _mGValueSetting;
        }
    }
}