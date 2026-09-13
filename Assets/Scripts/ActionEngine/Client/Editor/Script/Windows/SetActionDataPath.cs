using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if Addressables
#endif

namespace AsiTimeLine.Editor
{
    public class SetActionDataPath : EditorWindow
    {
        private string actionEditorPath = string.Empty;
        private string actionRunTimePath = string.Empty;
        private bool isChange = false;
        private bool isResourcesPath = false;
        private Vector2 scrolPos;
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(SetActionDataPath));
            window.titleContent = new GUIContent("Set Action Data Path");
            window.Show();
        }

        private void OnEnable()
        {
            actionRunTimePath = ActionEngineConst.RunTimeDataPath;
#if !Addressables
            isResourcesPath = CheckResourcesPath(actionRunTimePath);
#endif
            actionEditorPath = ActionEngineConst.EditorDataPath;
            isChange = false;
        }

        private void OnGUI()
        {
            // using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Editor数据存取路径:", GUILayout.Width(132));

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    Object _target = EditorGUILayout.ObjectField("选择文件夹", null, typeof(Object), false);
                    if (check.changed)
                    {
                        string _pickPath = AssetDatabase.GetAssetPath(_target) + "/";
                        if (_pickPath.Split('.').Length > 1)
                        {
                            EditorUtility.DisplayDialog("警告", "不能直接拖入资产，请选择文件夹！！", "我知道了");
                            // return;
                        }
                        else
                        {
                            if (actionEditorPath != _pickPath)
                            {
                                actionEditorPath = _pickPath;
                                isChange = true;
                            }
                        }
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        string newPath = EditorGUILayout.DelayedTextField(actionEditorPath);
                        if (check.changed)
                        {
                            actionEditorPath = newPath;
                            isChange = true;
                        }
                    }

                    if (GUILayout.Button("选择路径", GUILayout.Width(80)))
                    {
                        string[] pathGroup = actionEditorPath.Split('/');
                        string path = pathGroup[0];
                        for (int i = 1; i < pathGroup.Length - 1; i++)
                        {
                            path += "/" + pathGroup[i];
                        }

                        Object folderObj = AssetDatabase.LoadAssetAtPath<Object>(path);
                        if (folderObj != null)
                        {
                            Selection.activeObject = folderObj; // 选中目标文件夹
                            EditorUtility.FocusProjectWindow(); // 聚焦Project窗口
                            EditorGUIUtility.PingObject(folderObj); // 高亮显示文件夹
                        }
                        else
                        {
                            Debug.LogError($"路径不存在: {path}");
                        }
                    }
                }
            }

            GUILayout.Space(10);
            // using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("RunTime数据存取路径:", GUILayout.Width(132));

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    Object _target = EditorGUILayout.ObjectField("选择文件夹", null, typeof(Object), false);
                    if (check.changed)
                    {
                        string _pickPath = AssetDatabase.GetAssetPath(_target) + "/";
                        if (_pickPath.Split('.').Length > 1)
                        {
                            EditorUtility.DisplayDialog("警告", "不能直接拖入资产，请选择文件夹！！", "我知道了");
                            // return;
                        }
                        else
                        {
                            if (actionRunTimePath != _pickPath)
                            {
                                actionRunTimePath = _pickPath;
                                isChange = true;
                            }
                        }
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        string newPath = EditorGUILayout.DelayedTextField(actionRunTimePath);
                        if (check.changed)
                        {
                            actionRunTimePath = newPath;
                            isChange = true;
                        }
                    }

                    if (GUILayout.Button("选择路径", GUILayout.Width(80)))
                    {
                        string[] pathGroup = actionRunTimePath.Split('/');
                        string path = pathGroup[0];
                        for (int i = 1; i < pathGroup.Length - 1; i++)
                        {
                            path += "/" + pathGroup[i];
                        }

                        Object folderObj = AssetDatabase.LoadAssetAtPath<Object>(path);
                        if (folderObj != null)
                        {
                            Selection.activeObject = folderObj; // 选中目标文件夹
                            EditorUtility.FocusProjectWindow(); // 聚焦Project窗口
                            EditorGUIUtility.PingObject(folderObj); // 高亮显示文件夹
                        }
                        else
                        {
                            Debug.LogError($"路径不存在: {path}");
                        }
                    }
                }


#if !Addressables
                if (!isResourcesPath)
                {
                    using (new GUIColorScope(Color.red))
                    {
                        GUILayout.Label("当前是以【StreamingAssets】方案加载，请在跟目录创建StreamingAssets文件夹并设置到此文件夹内或其子文件夹");
                    }
                }
#endif
            }

            GUILayout.Space(10);
#if Addressables
            using (new GUIColorScope(Color.green))
            {
                GUILayout.Label("检查到Addressables插件，启用Addressables加载方式");
            }
#else
            using (new GUIColorScope(Color.yellow))
            {
                GUILayout.Label("没有检查到Addressables插件");
                GUILayout.Label("以StreamingAssets方式加载");
                GUILayout.Label("安装Addressables后将启用Addressables的加载方式");
            }
#endif

            GUILayout.Space(10);
            using (new GUIColorScope(Color.red, isChange))
            {
                if (GUILayout.Button("应用当前路径设置")) //并迁移文件内数据
                {
                    bool isAddressables = true;
                    string _RTPath = actionRunTimePath;
#if !Addressables
                    
                    isAddressables = ActionEnginLoadData.Instance.FullPathToResource(actionRunTimePath,
                        out _RTPath,
                        "StreamingAssets");
#endif
                    if (isAddressables)
                    {
                        string _path = "Assets/StreamingAssets" + ActionEngineRuntimePath.RTDataPath;
                        ActionMenuItem.CreactPath(_path, false);

                        PlayerPrefs.SetString("ADPE", actionEditorPath);
                        PlayerPrefs.SetString("ADPR", actionRunTimePath);

                        // ActionEngineRuntimePath.Instance.RuntimeData.DataPath = actionRunTimePath;
                        // ActionEngineRuntimePath.Instance.RuntimeData.DataPathRT = _RTPath;
                        // Debug.LogWarning("路径: " + _RTPath);

                        //创建必要路径
                        //Editor路径
                        string editorPath = actionEditorPath;

                        ActionMenuItem.CreactPath(editorPath + "/Unit");
                        ActionMenuItem.CreactPath(editorPath + "/Action");
                        ActionMenuItem.CreactPath(editorPath + "/GValue");
                        ActionMenuItem.CreactPath(editorPath + "/Prop");
                        ActionMenuItem.CreactPath(editorPath + "/Camera");
                        ActionMenuItem.CreactPath(editorPath + "/AssetData");
                        ActionMenuItem.CreactPath(editorPath + "/InputSetting");

                        //RunTime数据
                        ActionMenuItem.CreactPath(actionRunTimePath + "/Unit");
                        ActionMenuItem.CreactPath(actionRunTimePath + "/Action");
                        ActionMenuItem.CreactPath(actionRunTimePath + "/GValue");
                        ActionMenuItem.CreactPath(actionRunTimePath + "/Prop");
                        ActionMenuItem.CreactPath(actionRunTimePath + "/Camera");
                        ActionMenuItem.CreactPath(actionRunTimePath + "/AssetData");
                        ActionMenuItem.CreactPath(actionRunTimePath + "/InputSetting");

                        // ActionEngineManager_Unit.Instance.CheckAndUpdateUnitList(false);

                        // using (FileStream _fileStream = new FileStream(_path + ActionEngineRuntimePath.Suffixes,
                        //            FileMode.Create))
                        // {
                        //     BinaryFormatter _binaryFormatter = new BinaryFormatter();
                        //     _binaryFormatter.Serialize(_fileStream, ActionEngineRuntimePath.Instance.RuntimeData);
                        // }



                        ActionMenuItem.CreactPath(ActionEngineRuntimePath.Instance.InputActionListPath(), false);

                        isChange = false;

#if !Addressables
                        isResourcesPath = CheckResourcesPath(actionRunTimePath);
#endif
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("警告", "设置失败！！", "我知道了");
                    }

                }
            }

            if (isChange)
            {
                if (GUILayout.Button("还原路径")) //并迁移文件内数据
                {
                    actionRunTimePath = ActionEngineConst.RunTimeDataPath;
                    actionEditorPath = ActionEngineConst.EditorDataPath;
                    isChange = false;
                }
            }
        }

        private bool CheckResourcesPath(string path)
        {
            // return path.Contains("Resources");
            string[] paths = path.Split('/');
            return paths.Length > 1 && paths[1] == "StreamingAssets";
            return path.Contains("StreamingAssets");
        }
    }
}