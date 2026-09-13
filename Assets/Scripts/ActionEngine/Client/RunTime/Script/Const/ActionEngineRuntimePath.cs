using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class ActionEngineRuntimePath
    {
        //运行时配置文件加载的固定路径
        public const string RTDataPath = "/ActionEngineConstData/ActionEngineConfig";//Assets/StreamingAssets

        #region Instance
        private static ActionEngineRuntimePath _instance = null;
        public static ActionEngineRuntimePath Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ActionEngineRuntimePath();
                }
                return _instance;
            }
        }
        #endregion

        private string AssetsPath = string.Empty;
        private string AssetsLoadPath = string.Empty;

        public const string Suffixes = ".bytes";
        public const string SuffixesP = "bytes";

        public const string DataName_Asset = "data_ae_asset_";
        public const string DataName_Input = "data_ae_input_";
        public const string DataName_Unit = "data_ae_unit_";
        public const string DataName_Action = "data_ae_action_";
        public const string DataName_Skill = "data_ae_skill_";
        public const string DataName_Cam = "data_ae_cam_";
        public const string DataName_Module = "data_ae_module_";
        public const string DataName_Item = "data_ae_Item_";
        public const string DataName_Gvalue = "data_ae_gvalue_";
        public const string DataName_Equation = "data_ae_equation_";

        public void init()
        {
            AssetsPath = string.Empty;
            AssetsLoadPath = string.Empty;
        }
        public string DataPath => ActionEngineRuntimeData.DataPath;

        public string DataLoadPath => Application.streamingAssetsPath + "/" + ActionEngineRuntimeData.DataPathRT;

        //单位路径
        public string UnitPath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return DataPath + "Unit";
            }
            return DataPath + $"Unit/{_name}";//.json
        }
        public string UnitLoadPath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return DataLoadPath + "Unit";
            }
            return DataLoadPath + $"Unit/{_name}";//.json
            // return DataLoadPath + $"Unit/{_name}";
        }

        //Action保存路径
        public string ActionPath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return DataPath + "Action";
            }
            return DataPath + $"Action/{_name}";//.json
        }
        public string ActionLoadPath(string _name)
        {
            return DataLoadPath + $"Action/{_name}";
        }

        //世界变量路径
        public string GValuePath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return DataPath + "GValue";
            }
            return DataPath + $"GValue/{_name}";//.json
        }
        public string GValueLoadPath(string _name)
        {
            return DataLoadPath + $"GValue/{_name}";
        }
        public string EquationLoadPath(string _name)
        {
            return DataLoadPath + $"Equation/{_name}";
        }
        public string SkillLoadPath(string _name)
        {
            return DataLoadPath + $"Skill/{_name}";
        }
        //Camera保存路径
        public string CameraPath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return DataPath + "Camera";
            }
            return DataPath + $"Camera/{_name}";//.json
        }
        public string CameraLoadPath(string _name)
        {
            return DataLoadPath + $"Camera/{_name}";
        }

        //UnitWarp保存路径
        public string UnitWarpPath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return DataPath + "Unit";
            }
            return DataPath + $"Unit/{_name}";//.json
        }
        public string UnitWarpLoadPath(string _name)
        {
            return DataLoadPath + $"Unit/{_name}";
        }

        //Prop保存路径
        public string PropWarpLoadPath(string _name)
        {
            return DataLoadPath + $"Prop/{_name}";
        }

        //资产保存路径
        public string AssetDataPath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return DataPath + "AssetData";
            }
            return DataPath + $"AssetData/{_name}";//.json
        }
        public string AssetDataLoadPath(string _name)
        {
            return DataLoadPath + $"AssetData/{_name}";
        }

        //输入系统设置保存路径
        public string InputModulePath()
        {
            return DataPath + $"InputSetting/InputModule" + Suffixes;//.json
        }
        public string InputModuleLoadPath()
        {
            return DataLoadPath + $"InputSetting/data_ae_input_module";//
        }

        //输入系统的行为列表保存路径
        public string InputActionListPath()
        {
            return DataPath + $"InputSetting/InputAction.json";//.json
        }
        public string InputActionListLoadPath()
        {
            return DataLoadPath + $"InputSetting/InputAction";
        }

    }

    //写死的配置
    [System.Serializable]
    public class ActionEngineRuntimeData
    {
        public const string DataPath = "Assets/StreamingAssets/ActionData/";

        public const string DataPathRT = "ActionData/";
    }
}