using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public partial class ActionEngineManager_Unit
    {
        public void CreactCamera(int _id, ActionEngine_Unit _player, Action<CameraControl> _callback) =>
            OnCreactCamera(_id, _player, _callback);
        public void DestoryCam(string _id, CameraControl _gameObject) =>
            EngineResourcesManager.Instance.RemoveComponent(_id, _gameObject);
        public void CreateSkillUnit(
            int unitWarp,
            Action<TargetUnit> callback,
            ERuntimeDataChannel channel = ERuntimeDataChannel.Local)
        {
            CreateUnitCore(unitWarp, callback, EUnitType.Skill, channel);
        }

        public void CreateTransientPrewarmUnit(
            int unitWarp,
            Action<TargetUnit> callback,
            ERuntimeDataChannel channel = ERuntimeDataChannel.Local)
        {
            CreateUnitCore(unitWarp, callback, EUnitType.Entity, channel);
        }

        private void CreateUnitCore(
            int unitWarp,
            Action<TargetUnit> callback,
            EUnitType type,
            ERuntimeDataChannel channel)
        {
            ERuntimeDataChannel runtimeChannel =
                ActionEngineManager.Instance.ResolveRuntimeChannel(channel);
            OnCreactUnit(unitWarp, callback, type, runtimeChannel);
        }

        public void CreateIdentifiedUnit(
            int unitWarp,
            ulong navigationStableUnitId,
            Action<TargetUnit> callback,
            EUnitType type = EUnitType.Entity,
            ERuntimeDataChannel channel = ERuntimeDataChannel.Local)
        {
            NavigationStableUnitId.Validate(navigationStableUnitId);
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            CreateUnitCore(
                unitWarp,
                target =>
                {
                    ActionEngine_Unit unit = target != null
                        ? target.GetUnit()
                        : null;
                    if (unit != null)
                    {
                        try
                        {
                            NavigationStableUnitIdentity.Bind(
                                unit,
                                navigationStableUnitId);
                        }
                        catch
                        {
                            DestoryUnit(unit);
                            throw;
                        }
                    }
                    callback(target);
                },
                type,
                channel);
        }
        public void CreateProp(int _propWarp, Action<ActionEngine_Prop> _callback) => OnCreateProp(_propWarp, _callback);
        public void DestoryUnit(ActionEngine_Unit _unit) => OnDestoryUnit(_unit);

        private void OnCreactCamera(int _id, ActionEngine_Unit _player, Action<CameraControl> _callback)
        {
            //读取数据
            OnGetCameraWarp(_id, warp =>
            {
                EngineResourcesManager.Instance.CreactObjToComponent<CameraControl>(warp.ModelPath, component =>
                {
                    if (component is CameraControl _camera)
                    {
                        CamControllerInit(_camera, _player, warp);
                        _callback(_camera);
                    }
                }, 1);
            });
        }

        private void CamControllerInit(CameraControl _cameraControl, ActionEngine_Unit _unit, CameraWarp _cameraWarp)
        {
            if (_unit.TryGetComponent(out CharacterConfig _config))
            {
                if (_config.HelpPointDic.TryGetValue(ECharacteLimbType.Cam_Main, out Transform _transform))
                {
                    _cameraControl.OnInit(_transform, _unit.ActionStateMachine, _cameraWarp.DefaultCamID);
                }
                else
                {
                    EngineDebug.LogWarning("未配置相机挂点");

                }
            }
            else
            {
                EngineDebug.LogWarning("未挂载Config组件");
            }
        }

        private void OnDestoryUnit(ActionEngine_Unit _unit)
        {
            if (mUnitHashs.Contains(_unit))
            {
                Units.Remove(_unit);
                mUnitHashs.Remove(_unit);
            }
            else
            {
                EngineDebug.LogWarning($"释放Unit逻辑失败[{_unit.gameObject.name}](已自动处理，可忽视此次警告) <color=#ff0000>尝试释放已经释放过的资产</color> Hash[{_unit.gameObject.GetHashCode()}]");
                return;
            }

            NavigationStableUnitIdentity.Clear(_unit);


            if (_unit is ActionEngine_Skill _skill)
            {
                RemoveSkillToDic(_skill);
                string _loadName = EngineResourcesManager.Instance.DataName_Skill + _skill.UnitWarpID;
                _skill.UnEquipSkill();
                _skill.DestoryAllActionPart();
                EngineResourcesManager.Instance.RemoveComponent(_loadName, _unit);
            }
            else
            {
                //代理关闭
                _unit.AgentUnitRoot.SetActive(false);
                //EngineDebug.LogError($"关闭代理[{_unit.AgentUnitRoot.name}]");

                RemoveUnitToDic(_unit);
                //优先按创建时记录的对象池键归还（服务端逻辑单位键与模型路径不同），缺失时回退到 ModelPath
                string _releaseKey = string.IsNullOrEmpty(_unit.LoadPoolKey) ? _unit.UnitWarp.ModelPath : _unit.LoadPoolKey;
                EngineResourcesManager.Instance.RemoveComponent(_releaseKey, _unit);
                //OnGetUnitWarp(_unit.UnitWarpID, _unitwarp =>
                //{
                //    //EngineDebug.Log($"尝试释放Unit: [{_unit.transform.name}]");
                //    EngineResourcesManager.Instance.RemoveComponent(_unitwarp.ModelPath, _unit);
                //});
            }
        }
        #region 按标签储存单位
        private Dictionary<int, List<ActionEngine_Skill>> mSkillDic = new Dictionary<int, List<ActionEngine_Skill>>();
        private Dictionary<int, List<ActionEngine_Unit>> mUnitDic = new Dictionary<int, List<ActionEngine_Unit>>();
        private void AddUnitToDic(ActionEngine_Unit _unit)
        {
            //EngineDebug.LogError($"添加单位 [{_unit.gameObject.name}(<color=#ffcc00>{_unit.transform.GetSiblingIndex()}</color>)]");
            foreach (int _tag in _unit.UnitWarp.Tags)
            {
                if (mUnitDic.ContainsKey(_tag))
                {
                    mUnitDic[_tag].Add(_unit);
                }
                else
                {
                    List<ActionEngine_Unit> _list = new List<ActionEngine_Unit>();
                    _list.Add(_unit);
                    mUnitDic.Add(_tag, _list);
                }
            }
        }
        private void RemoveUnitToDic(ActionEngine_Unit _unit)
        {
            foreach (int _tag in _unit.UnitWarp.Tags)
            {
                if (mUnitDic.TryGetValue(_tag, out List<ActionEngine_Unit> _list))
                {
                    _list.Remove(_unit);
                }
            }
        }
        private void AddSkillToDic(ActionEngine_Skill _skill)
        {
            //EngineDebug.LogError($"添加技能 [{_skill.gameObject.name}(<color=#ffcc00>{_skill.transform.GetSiblingIndex()}</color>)]");
            ActionEngine_Unit source = _skill.GetSource;
            foreach (int _tag in _skill.SkillWarp.Tags)
            {
                if (mSkillDic.ContainsKey(_tag))
                {
                    mSkillDic[_tag].Add(_skill);
                }
                else
                {
                    List<ActionEngine_Skill> _list = new List<ActionEngine_Skill>();
                    _list.Add(_skill);
                    mSkillDic.Add(_tag, _list);
                }

                if (source.m_SkillDic.ContainsKey(_tag))
                {
                    //EngineDebug.LogError($"<color=#ff0000>{source.gameObject.name}</color>: 添加[{_skill.gameObject.name}]至[{_tag}]");
                    source.m_SkillDic[_tag].Add(_skill);
                }
                else
                {
                    List<ActionEngine_Skill> _list = new List<ActionEngine_Skill>();
                    _list.Add(_skill);
                    source.m_SkillDic.Add(_tag, _list);
                }
            }
        }
        private void RemoveSkillToDic(ActionEngine_Skill _skill)
        {
            ActionEngine_Unit master = _skill.GetSource;

            foreach (int _tag in _skill.SkillWarp.Tags)
            {
                if (mSkillDic.TryGetValue(_tag, out List<ActionEngine_Skill> _list))
                {
                    _list.Remove(_skill);
                }

                if (master.m_SkillDic.TryGetValue(_tag, out List<ActionEngine_Skill> _list2))
                {
                    _list2.Remove(_skill);
                }
            }
        }

        public bool TryGetUnitToTag(int _key, out List<ActionEngine_Unit> _list)
        {
            return mUnitDic.TryGetValue(_key, out _list);
        }
        public bool TryGetSkillToTag(int _key, out List<ActionEngine_Skill> _list)
        {
            return mSkillDic.TryGetValue(_key, out _list);
        }
        #endregion
        //角色异步加载
        //private List<Action<ActionEngine_Unit>> mCreactUnitCallBacks = new List<Action<ActionEngine_Unit>>();


        private void OnCreactUnit(int _UnitWarpID, Action<TargetUnit> _loadCallback, EUnitType _type,
            ERuntimeDataChannel _channel = ERuntimeDataChannel.Local)
        {
            if (_type == EUnitType.Entity)
            {
                OnGetUnitWarp(_UnitWarpID, warp =>
                {
                    if (warp == null)
                    {
                        EngineDebug.LogError($"单位配置加载失败 id=[{_UnitWarpID}]");
                        _loadCallback?.Invoke(null);
                        return;
                    }
                    // 服务端(Server)通道为纯逻辑单位：不加载模型 prefab，仿 Skill 用空 GameObject 承载状态机
                    bool _isServer = _channel == ERuntimeDataChannel.Server;
                    string _loadKey = _isServer
                        ? EngineResourcesManager.Instance.DataName_Unit + warp.ID + RuntimeDataChannel.SuffixServer
                        : warp.ModelPath;

                    Action<Component> _onLoaded = _component =>
                    {
                        if (_component is TargetUnit targetUnit)
                        {
                            if (targetUnit.GetUnit() is ActionEngine_Unit _unit)
                            {
                                _unit.Channel = _channel;
                                //代理打开
                                targetUnit.AgentUnitRoot.SetActive(true);

                                _unit.UnitWarp = warp;
                                // 记录对象池加载键，销毁时按此键归还（服务端逻辑单位与客户端模型键不同）
                                _unit.LoadPoolKey = _loadKey;
                                _unit.SetMaster(_unit, _unit);
                                //AddUnitToDic(_unit);
                                if (_unit.ActionStateMachine is null)
                                {
                                    // EngineDebug.Log($"加载单位: [{warp.Name}], Action[<color=#ffcc00>[{warp.Action}]</color>]");
                                    GetActionList(warp.Action, info =>
                                    {
                                        if (info == null)
                                        {
                                            EngineDebug.LogError($"单位动作配置加载失败 unitId=[{_UnitWarpID}] actionId=[{warp.Action}]");
                                            targetUnit.AgentUnitRoot.SetActive(false);
                                            EngineResourcesManager.Instance.RemoveComponent(_loadKey, targetUnit);
                                            _loadCallback?.Invoke(null);
                                            return;
                                        }

                                        ActionEngineManager_GValue.Instance.GetGValue((gvalue, equation) =>
                                        {
                                            if (gvalue == null || equation == null)
                                            {
                                                EngineDebug.LogError($"单位 GValue 配置加载失败 unitId=[{_UnitWarpID}]");
                                                targetUnit.AgentUnitRoot.SetActive(false);
                                                EngineResourcesManager.Instance.RemoveComponent(_loadKey, targetUnit);
                                                _loadCallback?.Invoke(null);
                                                return;
                                            }

                                            //服务端无动画机：Animator 传 null（mAnimValid=false）以关闭动画播放
                                            Animator _animator = _isServer ? null : _unit.GetComponent<Animator>();
                                            //当该单位状态机为空时，新建状态机给它用
                                            ActionStateMachine statePart = new ActionStateMachine(_unit,
                                                _animator, info.Clone(), gvalue, equation, warp.GValueSetting);
                                            _unit.SetActionStateMachine(statePart);
                                            _unit.UnitWarpID = warp.ID;
                                            _unit.CameraID = warp.CameID;
                                            UnitInit(targetUnit, _unit, warp.ID, _isServer, _loadCallback,
                                                () => { AddUnitToDic(_unit); });
                                        });
                                    }, _channel);
                                }
                                else
                                {
                                    UnitInit(targetUnit, _unit, warp.ID, _isServer, _loadCallback,
                                        () => { AddUnitToDic(_unit); });
                                }
                            }
                            else
                            {
                                EngineDebug.LogError($"单位模型缺少 ActionEngine_Unit id=[{_UnitWarpID}] key=[{_loadKey}]");
                                EngineResourcesManager.Instance.RemoveComponent(_loadKey, targetUnit);
                                _loadCallback?.Invoke(null);
                            }
                        }
                        else
                        {
                            EngineDebug.LogError($"单位模型加载失败或缺少 TargetUnit id=[{_UnitWarpID}] key=[{_loadKey}]");
                            _loadCallback?.Invoke(null);
                        }
                    };

                    if (_isServer)
                    {
                        EngineDebug.Log($"[ActionEngineManager_Unit] 服务端逻辑单位创建 id=[{warp.ID}] key=[{_loadKey}] (无模型/Animator/Camera)");
                        EngineResourcesManager.Instance.CreactObjToComponent<ActionEngine_Entity>(_loadKey, _onLoaded, 1, 10, -1, (int)EObjPoolParent.Unit, true);
                    }
                    else
                    {
                        EngineResourcesManager.Instance.CreactObjToComponent<TargetUnit>(_loadKey, _onLoaded, 1, 10, -1, (int)EObjPoolParent.Unit);
                    }
                });
            }
            else if (_type == EUnitType.Skill)
            {
                OnGetSkillWarp(_UnitWarpID, warp =>
                {
                    string _loadName = EngineResourcesManager.Instance.DataName_Skill + warp.ID;

                    EngineResourcesManager.Instance.CreactObjToComponent<ActionEngine_Skill>(_loadName, _component =>
                    {
                        if (_component is ActionEngine_Skill _unit)
                        {
                            _unit.Channel = _channel;
                            _unit.SkillWarp = warp;
                            //AddSkillToDic(_unit);
                            if (_unit.mNeedInit)
                            {
                                _unit.mNeedInit = false;
                                // EngineDebug.Log($"加载单位: [{warp.Name}], Action[<color=#ffcc00>[{warp.Action}]</color>]");
                                ActionEngineManager_GValue.Instance.GetGValue((gvalue, equation) =>
                                {
                                    //当该单位状态机为空时，新建状态机给它用
                                    ActionStateMachine statePart = new ActionStateMachine(_unit,
                                        null, warp.ActionStateInfo.Clone(), gvalue, equation, warp.GvalueSetting);
                                    _unit.SetActionStateMachine(statePart);
                                    _unit.UnitWarpID = warp.ID;
                                    //_unit.CameraObject = warp.CameName;
                                    //技能自身即 TargetUnit（ActionEngine_Skill : ActionEngine_Unit : TargetUnit）
                                    UnitInit(_unit, _unit, warp.ID, false, _loadCallback,
                                        () => { AddSkillToDic(_unit); });

                                });
                            }
                            else
                            {
                                UnitInit(_unit, _unit, warp.ID, false, _loadCallback,
                                    () => { AddSkillToDic(_unit); });
                            }
                        }
                        else
                        {
                            EngineDebug.LogError("创建的单位  挂载的组件不是 ActionEngine_Skill");
                        }
                    }, 1, 10, -1, (int)EObjPoolParent.Skill, true);
                }, _channel);
            }
        }

        private void OnCreateProp(int _PropWarpID, Action<ActionEngine_Prop> _loadCallback)
        {
            OnGetPropWarp(_PropWarpID, warp =>
            {
                EngineResourcesManager.Instance.CreactObjToComponent<ActionEngine_Prop>(warp.ModelPath, _component =>
                {
                    if (_component is ActionEngine_Prop _prop)
                    {
                        // _prop.mPropWarpName = _PropWarpName;
                        // _prop.mIsEquip = false;
                        _prop.mPropWarp = warp;
                        _loadCallback(_prop);
                    }
                }, 3);
            });
        }

        private void UnitInit(
            TargetUnit _targetUnit,
            ActionEngine_Unit _unit,
            int _unitWarpID,
            bool _applyServerComponentSnapshots,
            Action<TargetUnit> _loadCallback,
            Action _addToDic)
        {
            NavigationStableUnitIdentity.Clear(_unit);
            ActionStateMachine _machine = _unit.ActionStateMachine;
            //临时的ActionStatePart
            if (_machine.FirstStatePart is null)
            {
                //EngineDebug.LogError("正常判空");
                ActionStatePart _part = EngineResourcesManager.Instance.GetPreActionStatePart();
                _part.SetInitValue(_machine, true);
                _machine.FirstStatePart = _part;
            }

            //是否为技能的初始化
            bool isSkillInit = false;

            if (_unit is ActionEngine_Skill _skil)
            {
                //在执行任何后续初始化之前, 先判断当前技能创建的合理性
                if (!_skil.SkillWarp.InstanceJudgment.value(_machine.FirstStatePart, EngineResourcesManager.Instance.MachineTime))
                {
                    //终止技能创建
                    DestoryUnit(_unit);
                    return;
                }
                isSkillInit = true;
            }

            //数据清理
            _machine.InitGValue();//初始化GValue

            _machine.Init();//初始化状态机

            AddUnit(_unit);
            //_unit.ActionStateMachine.EventSystem.RunEvent_OnCreact();
            _unit.UnitWarpID = _unitWarpID;
            _unit.Init();

            if (_applyServerComponentSnapshots)
            {
                ServerComponentSnapshotUtility.ApplyGameObjectSnapshot(
                    _unit.gameObject,
                    _unit.UnitWarp.ServerGameObjectSnapshot);
                ServerComponentSnapshotUtility.ApplySnapshots(
                    _unit.gameObject,
                    _unit.UnitWarp.ServerComponentSnapshots);
            }

            //开始执行客户端实现的初始化, 技能将在这里开始生成跳转action
            _loadCallback(_targetUnit);//回调  可能会被回调释放单位

            // 加载回调允许立即回收单位（预热正是这个语义）。回收后继续执行首状态事件，
            // 会在已经归池的对象上启动玩法逻辑，也会把同步初始化耗时错误地算进预热等待。
            if (!mUnitHashs.Contains(_unit))
            {
                return;
            }

            //if (EngineResourcesManager.Instance.Player == _unit)
            //    EngineDebug.DisplayDialog("提示", "初始化玩家", "OK");

            //常规Action的初始化  在客户端初始化之后
            if (!isSkillInit)
            {
                _machine.ActionStateInfo.PerformActionEvent(_machine.FirstStatePart);
            }

            if (mUnitHashs.Contains(_unit))
            {
                if(_machine.TryGetComponent(out ActionEngine_PuppetMasterWarp warp, nameof(ActionEngine_PuppetMasterWarp)))
                {
                    Transform unitTrans = _unit.transform;
                    warp.Teleport(unitTrans.position, unitTrans.rotation);
                }

                _addToDic();
                _machine.InitState();
            }
        }

        private void OnGetCameraWarp(int _id, Action<CameraWarp> _loadCallback)
        {
            ActionEnginLoadData.Instance.LoadInfo(EInfoType.CameraWarp, _target =>
                {
                    if (_target is CameraWarp _cam)
                    {
                        // UnityEditor.AssetDatabase.GetAssetPath(_cam).
                        // EngineDebug.Log($"我的相机路径: " + _cam.ModelPath);
                        _loadCallback(_cam);
                    }
                },
                _id
            );
        }

        private void OnGetUnitWarp(int _id, Action<UnitWarp> _loadCallback)
        {
            ActionEnginLoadData.Instance.LoadInfo(EInfoType.UnitWarp, _target =>
                {
                    _loadCallback(_target as UnitWarp);
                }, _id
            );
        }
        private void OnGetSkillWarp(int _id, Action<SkillWarp> _loadCallback,
            ERuntimeDataChannel _channel = ERuntimeDataChannel.Local)
        {
            ActionEnginLoadData.Instance.LoadInfo(EInfoType.Skill, _obj =>
            {
                if (_obj is SkillWarp _warp)
                {
                    //获取到技能包装
                    _warp.ID = _id;
                    _loadCallback(_warp);
                }
            }, _id, _channel);
        }
        private void OnGetPropWarp(int _id, Action<PropWarp> _loadCallback)
        {
            ActionEnginLoadData.Instance.LoadInfo(EInfoType.PropWarp, _target =>
                {
                    if (_target is PropWarp _unit)
                    {
                        _unit.ID = _id;
                        _loadCallback(_unit);
                    }
                },
                _id
            );
        }
    }
}
