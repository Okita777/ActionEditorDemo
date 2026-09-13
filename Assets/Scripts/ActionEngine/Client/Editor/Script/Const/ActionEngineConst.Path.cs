using AsiTimeLine.RunTime;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    public partial class ActionEngineConst
    {
        private const string EditorResourcesPath = "Assets/Editor/ActionEditorConfig/";
        private const string EditorSetting = EditorResourcesPath + "Setting/WindowSetting.asset";//编辑器界面配置数据
#if Addressables
        public static string EditorDataPath => "Assets/Editor/ActionEditorConfig/Data/";//编辑器资产路径
        public static string RunTimeDataPath => "Assets/AddressableData/ActionEngineData/";
#else
        public static string EditorDataPath => PlayerPrefs.GetString("ADPE", "Assets/Editor/ActionEditorConfig/Data/");//编辑器资产路径
        public static string RunTimeDataPath => PlayerPrefs.GetString("ADPR", "Assets/StreamingAssets/ActionData/");
#endif

        public static string EditorDataPartPath = EditorDataPath + "{0}/{1}.json";//编辑器配置路径

        public static string RunTimeSavePath
        {
            get
            {
#if Addressables
                return RunTimeDataPath + "{0}/{1}" + ActionEngineRuntimePath.Suffixes;
#endif
                //Resources下数据资源保存路径
                return ActionEngineRuntimePath.Instance.DataPath + "{0}/{1}" + ActionEngineRuntimePath.Suffixes;
            }
        }
        public static string EditorUnitSavePath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return EditorDataPath + "Unit";
            }
            return EditorDataPath + $"Unit/{_name}.json";
        }
        public static string EditorSkillSavePath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return EditorDataPath + "Skill";
            }
            return EditorDataPath + $"Skill/{_name}.json";
        }
        public static string EditorActionSavePath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return EditorDataPath + "Action";
            }
            return EditorDataPath + $"Action/{_name}.json";
        }
        public static string EditorGValueSavePath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return EditorDataPath + "GValue";
            }
            return EditorDataPath + $"GValue/{_name}.json";
        }
        public static string EditorEquationSavePath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return EditorDataPath + "Equation";
            }
            return EditorDataPath + $"Equation/{_name}.json";
        }
        public static string EditorInputSavePath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return EditorDataPath + "InputSetting";
            }
            return EditorDataPath + $"InputSetting/{_name}.json";
        }
        public static string EditorPropSavePath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return EditorDataPath + "Prop";
            }
            return EditorDataPath + $"Prop/{_name}.json";
        }

        public static string EditorCamSavePath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return EditorDataPath + "Camera";
            }
            return EditorDataPath + $"Camera/{_name}.json";
        }
        public static string EditorAssetDataSavePath(string _name = "")
        {
            if (string.IsNullOrEmpty(_name))
            {
                return EditorDataPath + "AssetData";
            }
            return EditorDataPath + $"AssetData/{_name}.json";
        }
        public static string EditorInputModuleSavePath()
        {
            return EditorDataPath + $"InputSetting/data_ae_input_module.json"; //InputSetting/InputModule.json
        }
        public static string EditorInputActionSavePath()
        {
            return EditorDataPath + $"InputSetting/data_ae_input_action.json";//InputSetting/InputAction
        }
    }
}