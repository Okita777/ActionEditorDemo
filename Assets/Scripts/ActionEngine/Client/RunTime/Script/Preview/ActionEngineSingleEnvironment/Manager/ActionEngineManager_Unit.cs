using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime;
// using UnityEditor;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public partial class ActionEngineManager_Unit
    {
        #region Instance

        private static ActionEngineManager_Unit _instance;
        public static ActionEngineManager_Unit Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = new ActionEngineManager_Unit();
                }
                return _instance;
            }
        }

        #endregion

        //public bool OnUpdateEnble = true;
        private List<ActionEngine_Unit> mUnits = new List<ActionEngine_Unit>();
        public HashSet<ActionEngine_Unit> mUnitHashs = new HashSet<ActionEngine_Unit>();

        // private Dictionary<string, ActionStateInfo> mActionInfo = new Dictionary<string, ActionStateInfo>();//角色行为列表数据

        public List<ActionEngine_Unit> Units => mUnits;
        //public HashSet<ActionEngine_Unit> UnitHashs => mUnitHashs;

        [Obsolete("无法只释放或则只保留某个对象")]
        public void DestoryAllUnit(bool isDestoryPlayer)
        {
            EngineDebug.LogError("删除全对象(这个不是报错，只是检查对象删除时序)");

            string _debug = "当前安全释放对象:";
            for (int i = mUnits.Count - 1; i >= 0; i--)
            {
                ActionEngine_Unit unit = mUnits[i];
                if (!isDestoryPlayer)
                {
                    if (ActionEngineManager_Input.Instance.IsPlayer(unit))
                    {
                        continue;
                    }
                }
                _debug += $"\n[{unit.gameObject.name}]  Hash[{unit.gameObject.GetHashCode()}]";
                DestoryUnit(unit);
            }

            //ClearAllActionEngineObj();
            EngineDebug.LogError(_debug);
            //DestoryAllUnit();
        }

        /// <summary>
        /// 切换场景清理对象时调用此函数，会清理所有对象池和单位逻辑，注意在新场景中需要重新预载对象
        /// </summary>
        public void DestoryAllUnit()
        {
            EngineDebug.LogWarning("删除全对象(这个不是报错，只是检查对象删除时序)");

            string _debug = "当前安全释放对象:";
            for (int i = mUnits.Count - 1; i >= 0; i--)
            {
                ActionEngine_Unit unit = mUnits[i];
                _debug += $"\n[{unit.gameObject.name}]  Hash[{unit.gameObject.GetHashCode()}]";
                DestoryUnit(unit);
                //unit.SelfDestroy(unit.gameObject);
            }

            //清理对象池
            ClearAllActionEngineObj();
            EngineDebug.LogWarning(_debug);
        }


        private void ClearAllActionEngineObj()
        {
            NavigationQueryBudgetRuntime.Shutdown();
            NavigationStableUnitIdentity.Shutdown();
            ActionEngineManager_Input.Instance.ChangePlayer(null);
            mUnits.Clear();
            mUnitHashs.Clear();
            EngineResourcesManager.Instance.ClearAllObjPool();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init() => OnStart();
        /// <summary>
        /// 每帧执行
        /// </summary>
        /// <param name="_deltatime">每帧间隔时间</param>
        public void Update(float _deltatime) => OnUpdate(_deltatime);
        /// <summary>
        /// 每帧执行（Late）
        /// </summary>
        /// <param name="_deltatime">每帧间隔时间</param>
        public void LateUpdate(float _deltatime) => OnLateUpdate(_deltatime);
        /// <summary>
        /// 添加单位
        /// </summary>
        /// <param name="_unit"></param>
        public void AddUnit(ActionEngine_Unit _unit) => OnAddUnit(_unit);

        #region PublicFunction

        /// <summary>
        /// 获取单位的封装数据
        /// </summary>
        /// <param name="_name">单位名称</param>
        /// <param name="_loadCallback">加载结束后的回调</param>
        /// <returns></returns>
        public void GetUnitWarp(int _id, Action<UnitWarp> _loadCallback) => OnGetUnitWarp(_id, _loadCallback);

        public void GetPropWarp(int _id, Action<PropWarp> _loadCallback) => OnGetPropWarp(_id, _loadCallback);
        public void GetActionList(int _id, Action<ActionStateInfo> _loadCallback,
            ERuntimeDataChannel _channel = ERuntimeDataChannel.Local) => OnGetActionList(_id, _loadCallback, _channel);

        // public void GetActionList_Editor(string _name, Action<ActionStateInfo> _loadCallback) => OnGetActionList_Editor(_name, _loadCallback);
        #endregion 

        #region Function
        private void OnStart()
        {
            //初始化列表和字典
            NavigationQueryBudgetRuntime.Shutdown();
            NavigationStableUnitIdentity.Shutdown();
            mUnits.Clear();
            mUnitHashs.Clear();
            // mActionInfo.Clear();

            CreactTimeLineUpdateMode();
        }

        private void OnUpdate(float _deltatime)
        {
#if UNITY_EDITOR
            EngineDebug.ResetScenceLog();
#endif

            //if(!OnUpdateEnble)return;
            for (int i = 0; i < mUnits.Count; i++)
            {
                ActionEngine_Unit _unit = mUnits[i];
                _unit.OnUpdate(_deltatime);
            }
            NavigationQueryBudgetRuntime.Drain();
        }

        private void OnLateUpdate(float _deltatime)
        {
            //if(!OnUpdateEnble)return;
            for (int i = 0; i < mUnits.Count; i++)
            {
                ActionEngine_Unit _unit = mUnits[i];
                _unit.OnLateUpdate(_deltatime);
            }
        }

        private void OnAddUnit(ActionEngine_Unit _unit)
        {
            mUnits.Add(_unit);
            mUnitHashs.Add(_unit);
        }

        private List<Action<ActionStateInfo>> _loadCallbacks = new List<Action<ActionStateInfo>>();
        private void OnGetActionList(int _name, Action<ActionStateInfo> _loadCallback,
            ERuntimeDataChannel _channel = ERuntimeDataChannel.Local)
        {
            ActionEnginLoadData.Instance.LoadInfo(EInfoType.UnitAction, _target =>
                {
                    if (_target is ActionStateInfo _info)
                    {
                        _loadCallback(_info);
                        return;
                    }

                    EngineDebug.LogError($"动作配置加载失败 actionId=[{_name}] channel=[{_channel}]");
                    _loadCallback(null);
                },
                _name,
                _channel
            );
        }
        // private void OnGetActionList_Editor(string _name, Action<ActionStateInfo> _loadCallback)
        // {
        //     if (mActionInfo.ContainsKey(_name))
        //     {
        //         mActionInfo.Remove(_name);
        //         ActionEnginLoadData.Instance.LoadInfo(ActionEnginLoadData.EInfoType.UnitAction,
        //             _target =>
        //             {
        //                 if (_target is ActionStateInfo _stateInfo)
        //                 {
        //                     mActionInfo.Add(_name,_stateInfo);
        //                     _loadCallback(_stateInfo);
        //                 }
        //             },
        //             _name
        //         );
        //         EngineDebug.LogWarning("加载Json");
        //     }
        //     else
        //     {
        //         EngineDebug.DisplayDialog("警告", "未加载过此Action", "我知道了");
        //     }
        // }


#if UNITY_EDITOR  
        //收集Unit文件
        List<string> mUnitInfo = new List<string>();
#endif

        #endregion

        #region TimeLineUpdate

        private void CreactTimeLineUpdateMode()
        {
#if UNITY_EDITOR
            GameObject _TimeLineObj = new GameObject();
            _TimeLineObj.name = "TimeLineObj";
            _TimeLineObj.AddComponent<ActionTimeLineUpdate>();
#endif
        }


        #endregion
    }
}
