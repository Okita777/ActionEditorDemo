using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class CameraWarp : IProperty
    {
        public int ID;
        //public string Name;
        public int DefaultCamID;
        [SerializeField] protected string mModelPath;//相机资源路径

        #region property

        [EditorProperty("预制体: ", EditorPropertyType.EEPT_GameObject)]
        public string ModelPath
        {
            get { return mModelPath; }
            set { mModelPath = value; }
        }


        #endregion
    }
}