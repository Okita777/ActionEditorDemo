using System;
using System.Collections.Generic;
using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEngine.Windows;

namespace AsiTimeLine.Editor
{
    public class ActionMenuItem
    {
        [MenuItem("Tools/AsitirTool/ActionEditor %`")]
        public static void OnActionWindowMain()
        {
            CreateMainPath();
            ActionWindowMain.MainWindow();//技能编辑器
        }
        [MenuItem("Tools/AsitirTool/ActionEditorDataPath")]
        public static void OnActionDataPathWindow()
        {
            SetActionDataPath.ShowWindow();//技能编辑器
        }

        private static void CreateMainPath()
        {
            //创建必要路径
            //Editor路径
            string[] pathnames = ActionEngineConst.EditorUnitSavePath().Split('/');
            string editorPath = pathnames[0];
            for (int i = 1; i < pathnames.Length - 1; i++)
            {
                editorPath += "/" + pathnames[i];
            }
            CreactPath(editorPath + "/Unit");
            CreactPath(editorPath + "/Action");
            CreactPath(editorPath + "/GValue");
            CreactPath(editorPath + "/Equation");
            CreactPath(editorPath + "/Prop");
            CreactPath(editorPath + "/Camera");
            CreactPath(editorPath + "/Skill");
            CreactPath(editorPath + "/AssetData");
            CreactPath(editorPath + "/InputSetting");

            //RunTime数据
            CreactPath(string.Format(ActionEngineConst.RunTimeSavePath, "Unit", "0"), false);
            CreactPath(string.Format(ActionEngineConst.RunTimeSavePath, "Action", "0"), false);
            CreactPath(string.Format(ActionEngineConst.RunTimeSavePath, "GValue", "0"), false);
            CreactPath(string.Format(ActionEngineConst.RunTimeSavePath, "Equation", "0"), false);
            CreactPath(string.Format(ActionEngineConst.RunTimeSavePath, "Prop", "0"), false);
            CreactPath(string.Format(ActionEngineConst.RunTimeSavePath, "Camera", "0"), false);
            CreactPath(string.Format(ActionEngineConst.RunTimeSavePath, "Skill", "0"), false);
            CreactPath(string.Format(ActionEngineConst.RunTimeSavePath, "AssetData", "0"), false);
            CreactPath(string.Format(ActionEngineConst.RunTimeSavePath, "InputSetting", "0"), false);
            CreactPath(ActionEngineRuntimePath.Instance.InputActionListPath(), false);
        }

        public static void CreactPath(string _path, bool isPath = true)
        {
            string[] paths = _path.Split('/');
            string createPath = paths[0];

            int lastIndex = isPath ? paths.Length : paths.Length - 1;

            for (int i = 1; i < lastIndex; i++)
            {
                createPath += "/" + paths[i];
                if (!Directory.Exists(createPath))
                    Directory.CreateDirectory(createPath);
            }
        }
    }

    [InitializeOnLoad]
    public class ActionEngineInit
    {
        static ActionEngineInit()
        {
            ActionEngineEventInit.Init();
            // EngineDebug.Log("编辑器初始化");
        }
    }

    //注册行为编辑器的初始化
    public static class ActionEngineEventInit
    {
        private static ActionEngineEvent ActionEngineEvent;
        private static List<string> _EnumName = new List<string>();

        // [UnityEditor.Callbacks.DidReloadScripts(0)] 
        public static void Init()
        {
            if (!EventInit())
            {
                EngineDebug.LogError("编辑器尝试初始化失败");
                return;
            }

            EngineResourcesManager.Instance.Init(new ResourceLoaderFuntion(), null, null);

            EditorActionData _editorActionData = new EditorActionData();

            _EnumName.Clear();
            for (int i = 0; i < 99999; i++)
            {
                EEvenType _en = (EEvenType)i;
                string _enumName = _en.ToString();
                if (_enumName.Split('_').Length < 2)
                {
                    break;
                }

                _EnumName.Add(EnumUtinity.GetDescription2(_en));
            }
            _editorActionData.EventDataTypeDes = _EnumName.ToArray();
            _editorActionData.EventDataType = Enum.GetNames(typeof(EEvenType));

            _EnumName.Clear();
            for (int i = 0; i < 99999; i++)
            {
                EConditionType _en = (EConditionType)i;
                string _enumName = _en.ToString();
                if (_enumName.Split('_').Length < 2)
                {
                    break;
                }

                _EnumName.Add(EnumUtinity.GetDescription2(_en));
            }
            _editorActionData.ConditionTypeDes = _EnumName.ToArray();
            _editorActionData.ConditionType = Enum.GetNames(typeof(EConditionType));
            _editorActionData.AsiActionEditorFuntion = ActionEngineEvent;

            ActionWindowMain.InitWindow(_editorActionData);
        }

        private static bool EventInit()
        {
            if (ActionEngineEvent is null)
                ActionEngineEvent = new ActionEngineEvent();

            return true;
        }
    }
}