
namespace AsiActionEngine.RunTime
{
    public class MotionEngineRuntimePath
    {
        #region Instance
        private static MotionEngineRuntimePath _instance = null;
        public static MotionEngineRuntimePath Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MotionEngineRuntimePath();
                }

                return _instance;
            }
        }


        #endregion

        private const string LocalPath = "Assets/Resources/Script/AsiActionEditor/RunTime/";

#if UNITY_EDITOR
        private string mRuntimeLocalPath = null;
#endif
        public string RuntimeLocalPath
        {
            get
            {
#if UNITY_EDITOR
                if (string.IsNullOrEmpty(mRuntimeLocalPath))
                {
                    string[] _guid = UnityEditor.AssetDatabase.FindAssets(GetType().Name);
                    string[] _nowPath = UnityEditor.AssetDatabase.GUIDToAssetPath(_guid[0]).Split('/');
                    for (int i = 0; i < _nowPath.Length - 3; i++)
                        mRuntimeLocalPath += (_nowPath[i] + "/");
                }

                // if (LocalPath != mRuntimeLocalPath)
                // {
                //     Debug.LogError("Runtime路径错误！！！ " +
                //                    "请打开此脚本手动将变量 [ <color=#FFF100>LocalPath</color> ] 的值设置为: " +
                //                    mRuntimeLocalPath
                //     );
                // }
                // return mRuntimeLocalPath;
#endif
                return LocalPath;
            }
        }

        public string ResourcesPath
        {
            get { return RuntimeLocalPath + "Resources/"; }
        }

        public string DataPath
        {
            get { return ResourcesPath + "Data/"; }
        }

        //单位保存路径
        public string UnitPath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return DataPath + "Unit";
            }
            return DataPath + $"Unit/{_name}.json";
        }

        //Action保存路径
        public string ActionPath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return DataPath + "Action";
            }
            return DataPath + $"Action/{_name}.json";
        }

        //输入系统设置保存路径
        public string InputModulePath()
        {
            return ResourcesPath + $"InputSetting/InputModule.json";
        }

        //输入系统的行为列表保存路径
        public string InputActionListPath()
        {
            return ResourcesPath + $"InputSetting/InputAction.json";
        }
    }
}