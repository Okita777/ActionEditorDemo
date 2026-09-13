using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using AsiActionEngine.RunTime;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using AsiActionEngine.Editor;
#endif

#if Addressables
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace AsiTimeLine.RunTime
{
    public class ResourceLoaderFuntion : IResourceLoader
    {
        public string DataName_Unit() => ActionEngineRuntimePath.DataName_Unit;
        public string DataName_Action() => ActionEngineRuntimePath.DataName_Action;
        public string DataName_Skill() => ActionEngineRuntimePath.DataName_Skill;
        public string DataName_Cam() => ActionEngineRuntimePath.DataName_Cam;
        public string DataName_Module() => ActionEngineRuntimePath.DataName_Module;
        public string DataName_Item() => ActionEngineRuntimePath.DataName_Item;
        public string DataName_Gvalue() => ActionEngineRuntimePath.DataName_Gvalue;
        public string DataName_Equation() => ActionEngineRuntimePath.DataName_Equation;
        public void Init()
        {
#if UNITY_EDITOR
            InitEditorLoad();
#endif
        }

        //技能加载
        public void LoadSkill(int skillID, Action<ActionEngine_Skill> _callback)
            => ActionEngineManager_Skill.Instance.Create(skillID, _callback);

        //返回当前玩家操作的单位
        public ActionEngine_Unit Player() => ActionEngineManager_Input.Instance.Player;
        public void DestoryUnit(ActionEngine_Unit _Unit) => ActionEngineManager_Unit.Instance.DestoryUnit(_Unit);

        //ActionEngine_Unit 加载（回调收到外层 TargetUnit，需要内部 ActionEngine_Unit 的通过 GetUnit() 获取）
        public void LoadUnit(int assetKey, Action<TargetUnit> _callback, EUnitType _type)
        {
            if (_type == EUnitType.Entity)
            {
                throw new InvalidOperationException(
                    "Bare LoadUnit(Entity) is unsupported. Create gameplay entities through ActionEngineManager_Unit.CreateIdentifiedUnit with a formal stable identity.");
            }
            if (_type != EUnitType.Skill)
            {
                throw new ArgumentOutOfRangeException(nameof(_type));
            }
            ActionEngineManager_Unit.Instance.CreateSkillUnit(
                assetKey,
                _callback);
        }

        //加载Unity对象（仅Runtime调用）
        public void LoadAssets(string assetKey, Action<UnityEngine.Object> onGetAsset)
        {
            // EngineDebug.Log("尝试加载：" + assetKey);
            if (string.IsNullOrEmpty(assetKey))
            {
                onGetAsset(null);
                return;
            }
#if Addressables
            var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(assetKey);
            handle.Completed += (operation) =>
            {
                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    onGetAsset(operation.Result);
                }
                else
                {
                    onGetAsset(null);
#if UNITY_EDITOR
                    if (Application.isPlaying)
                        EngineDebug.LogError($"事件对象加载路径错误，当前加载方案为 【Addressables】\n<color=#FFCC00>{assetKey}</color>， 请检查路径或加载方案");
#endif
                }
                // handle.ReleaseHandleOnCompletion();
            };
            return;
#endif

            UnityEngine.Object _asset = null;

            //Unity默认加载，全平台读取方案
            _asset = Resources.Load(assetKey);
#if UNITY_EDITOR
            if (_asset is null)
            {
                EngineDebug.LogError($"事件对象加载路径错误,当前加载方案为 【Resources.Load】\n<color=#FFCC00>{assetKey}</color>， 请检查路径或加载方案");
            }
#endif
            onGetAsset(Resources.Load(assetKey));
        }

        //加载二进制数据（仅Runtime调用）
        public void LoadBinary(string assetKey, Action<object> onGetAsset)
        {
            if (string.IsNullOrEmpty(assetKey))
            {
                onGetAsset(null);
                return;
            }
#if Addressables
            var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(assetKey);

            handle.Completed += (operation) =>
            {
                try
                {
                    if (operation.Status == AsyncOperationStatus.Succeeded)
                    {
                        // byte[] fileBytes = File.ReadAllBytes(operation.Status);
                        if (operation.Result is TextAsset _asset)
                        {
                            using MemoryStream ms = new MemoryStream(_asset.bytes);
                            BinaryFormatter _binaryFormatter = new BinaryFormatter();
                            object _deserialized;
                            try
                            {
                                _deserialized = _binaryFormatter.Deserialize(ms);
                            }
                            catch (Exception _ex)
                            {
                                // 反序列化失败（如数据引用的类型未编译，缺少对应脚本宏）时，
                                // 必须仍回调，避免 ActionEnginLoadData 的等待队列永久挂起导致单位永远创建不出来。
                                onGetAsset(null);
                                EngineDebug.LogError($"二进制反序列化失败:[<color=#ffcc00>{assetKey}</color>] {_ex.GetType().Name}: {_ex.Message}");
                                return;
                            }
                            onGetAsset(_deserialized);
                        }
#if UNITY_EDITOR
                        else
                        {
                            onGetAsset(null);
                            EngineDebug.LogWarning($"转换失败，加载资产不是TextAsset:[<color=#ffcc00>{assetKey}</color>] type:[{operation.Result.GetType()}]");
                        }
#endif
                    }
                    else
                    {
                        onGetAsset(null);
                        EngineDebug.LogError($"二进制文件加载路径错误，当前加载方案为 【Addressables】\n<color=#FFCC00>{assetKey}</color>， 请检查路径或加载方案");
                    }
                }
                finally
                {
                    // The deserialized runtime object is independent from the TextAsset.
                    // Releasing prevents regenerated binaries from being pinned by a stale handle.
                    Addressables.Release(handle);
                }
            };
            return;
#endif

            //unity常规全平台读取文件的方式
            UnityWebRequest www = UnityWebRequest.Get(assetKey);
            www.SendWebRequest();
            while (!www.isDone) { }

#if UNITY_EDITOR
            if (www.downloadHandler.data is null)
            {
                EngineDebug.LogError($"二进制文件读取错误： 路径：[ <color=#ffcc00>{assetKey}</color> ]");
                return;
            }
#endif

            MemoryStream ms = new MemoryStream(www.downloadHandler.data);
            BinaryFormatter _binaryFormatter = new BinaryFormatter();
            onGetAsset(_binaryFormatter.Deserialize(ms));
        }

        public void ClearLoadAssets(string assetKey)
        {
            //清除字典，以便于重新加载此值的资源
            EngineDebug.LogError("要释放对象池，暂未实现");
        }
        public void ClearLoadBinary(string assetKey)
        {
            //清除字典，以便于重新加载此值的资源
            if (!ActionEnginLoadData.Instance.AllLoadData.Remove(assetKey))
            {
#if UNITY_EDITOR
                //EngineDebug.LogError($"无法清除 [<color=#ffcc00>{assetKey}</color>], 因为从未加载过");
#endif
            }
        }


        //加载Unity对象数据的完整路径转为Runtime用路径（仅Editor调用）
        public string GetRunTimePath(string assetKey)
        {
            if (string.IsNullOrEmpty(assetKey))
            {
                return "";
            }

#if Addressables
            string name = assetKey.Split('/')[^1].Split('.')[0];
            // EngineDebug.LogWarning($"当前输入路径：[{assetKey}] \n输出路径：[{name}]");
            //仅保留文件名
            return name;
#endif

            //保留为Resources格式
            string[] names = assetKey.Split('/');

            int findID = -1;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == "Resources")
                {
                    findID = i + 1;
                    break;
                }
            }

            if (findID < 0)
            {
                EngineDebug.LogError("资产路径错误！！，当前加载方案为Resources.Load，但此资产不在Resources路径下" +
                                     $"\n[<color=#FFCC00>{assetKey}</color>]");
                return string.Empty;
            }

            string _runtimePath = names[findID];
            for (int i = findID + 1; i < names.Length - 1; i++)
            {
                _runtimePath += "/" + names[i];
            }
            _runtimePath += "/" + names[^1].Split('.')[0];

            return _runtimePath;
        }

#if UNITY_EDITOR
        //仅Editor加载
        Dictionary<string, UnityEngine.Object> m_EditorObjectPool = new();
        private void InitEditorLoad()
        {
            m_EditorObjectPool.Clear();
        }

        public void LoadAssetsEditor(string _path, Action<Object> _loadCallBack, bool ondebug)
        {
            if (string.IsNullOrEmpty(_path))
            {
                _loadCallBack(null);
                return;
            }
            if (m_EditorObjectPool.TryGetValue(_path, out UnityEngine.Object _obj))
            {
                _loadCallBack(_obj);
            }
            else
            {
                UnityEngine.Object loader = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(_path);
                if (loader is not null)
                {
                    m_EditorObjectPool.Add(_path, loader);
                }
                else
                {
                    if (ondebug) EngineDebug.LogError($"Editor资产加载失败: [{_path}]");
                }
                _loadCallBack(loader);
            }
        }

        public void ClearLoadAssetsEditor(string assetKey)
        {
            m_EditorObjectPool.Remove(assetKey);
        }

#else
        public void LoadAssetsEditor(string _path, Action<Object> _loadCallBack, bool ondebug){}
        public void ClearLoadAssetsEditor(string assetKey){}
#endif

        public Dictionary<ushort, EngineGValue> GetEngineGValue()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return ResourcesWindow.Instance.EngineGValue;
            }
#endif
            return ActionEngineManager_GValue.Instance.EngineGValueDic;
        }
    }
}
