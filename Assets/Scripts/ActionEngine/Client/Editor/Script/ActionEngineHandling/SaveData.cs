using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    public partial class SaveData
    {
        public const string mDefaultSettingPath = "Assets/Editor/ActionEditorConfig/WindowSetting.asset";
        public const string mSkillSettingPath = "Assets/Editor/ActionEditorConfig/WindowSetting_Skill.asset";

        private static string mRunTimeSavePath => ActionEngineConst.RunTimeSavePath;//Resources下Json资源保存路径

        #region 加载数据 是Editor下的资源加载 直接同步加载即可
        public static ActionEngineSetting GetDefaultSetting()
        {
            ActionEngineSetting _setting = null;
            if (File.Exists(mDefaultSettingPath))
            {
                _setting = AssetDatabase.LoadAssetAtPath<ActionEngineSetting>(mDefaultSettingPath);
            }
            else
            {
                _setting = CreateActionEngineSetting();

                ActionMenuItem.CreactPath(mDefaultSettingPath, false);
                AssetDatabase.CreateAsset(_setting, mDefaultSettingPath);
                EngineDebug.Log($"未发现编辑器默认设置，已自动创建至:[<color=#ffcc00>{mDefaultSettingPath}</color>]");
            }
            return _setting;
        }
        public static ActionEngineSetting GetSkillSetting()
        {
            ActionEngineSetting _setting = null;
            if (File.Exists(mSkillSettingPath))
            {
                _setting = AssetDatabase.LoadAssetAtPath<ActionEngineSetting>(mSkillSettingPath);
            }
            else
            {
                _setting = CreateSkillActionEngineSetting();

                ActionMenuItem.CreactPath(mSkillSettingPath, false);
                AssetDatabase.CreateAsset(_setting, mSkillSettingPath);
                EngineDebug.Log($"未发现技能编辑器默认设置，已自动创建至:[<color=#ffcc00>{mSkillSettingPath}</color>]");
            }
            return _setting;
        }


        public static bool LoadActionData(out EditorActionStateInfo _editorActionState, string _actionName)
        {
            string _path = ActionEngineConst.EditorActionSavePath(_actionName);
            if (!File.Exists(_path))
            {
                _editorActionState = null;
                EngineDebug.LogWarning($"客户端加载 Action 数据失败，\n加载路径：{_path}");
                return false;
            }

            string _str = File.ReadAllText(_path);
            EditorActionStateInfo _LoadInfo = new EditorActionStateInfo(null, _actionName);
            JsonUtility.FromJsonOverwrite(_str, _LoadInfo);
            _LoadInfo.EnsureListGuid();
            _editorActionState = _LoadInfo;
            return true;
        }

        public static bool LoadUnitData(out EditorUnitWarp _editorActionState, string _unitName)
        {
            string _path = ActionEngineConst.EditorUnitSavePath(_unitName);
            if (!File.Exists(_path))
            {
                EngineDebug.Log($"客户端加载 Unit 数据失败，\n加载路径：{_path}");
                _editorActionState = null;
                return false;
            }

            string _str = File.ReadAllText(_path);
            EditorUnitWarp _LoadInfo = new EditorUnitWarp(0, "null", new GValue_Setting());
            try
            {
                JsonUtility.FromJsonOverwrite(_str, _LoadInfo);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("警告", $"Json文件读取失败！！，请检查 [{_path}]", "ok");
            }

            _editorActionState = _LoadInfo;
            return true;
        }

        public static bool LoadSkillData(out EditorSkillWarp _editorSkill, string _unitName)
        {
            string _path = ActionEngineConst.EditorSkillSavePath(_unitName);
            if (!File.Exists(_path))
            {
                EngineDebug.Log($"客户端加载 Skill 数据失败，\n加载路径：{_path}");
                _editorSkill = null;
                return false;
            }

            string _str = File.ReadAllText(_path);
            EditorSkillWarp _LoadInfo = new EditorSkillWarp("null", 0, new GValue_Setting());
            try
            {
                JsonUtility.FromJsonOverwrite(_str, _LoadInfo);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("警告", $"Json文件读取失败！！，请检查 [{_path}]", "ok");
            }

            _editorSkill = _LoadInfo;
            return true;
        }

        public static bool LoadCameraWarp(out EditorCameraWarp _editorCameraWarp, string _name)
        {
            string _path = ActionEngineConst.EditorCamSavePath(_name);
            if (!File.Exists(_path))
            {
                EngineDebug.Log($"客户端加载 Camera 数据失败，\n加载路径：{_path}");
                _editorCameraWarp = null;
                return false;
            }

            string _str = File.ReadAllText(_path);
            EditorCameraWarp _LoadInfo = new EditorCameraWarp(0, "");
            JsonUtility.FromJsonOverwrite(_str, _LoadInfo);
            _editorCameraWarp = _LoadInfo;
            return true;
        }
        public static bool LoadPropWarp(out EditorPropWarp _editorCameraWarp, string _name)
        {
            string _path = ActionEngineConst.EditorPropSavePath(_name);
            if (!File.Exists(_path))
            {
                EngineDebug.Log($"客户端加载 Prop 数据失败，\n加载路径：{_path}");
                _editorCameraWarp = null;
                return false;
            }

            string _str = File.ReadAllText(_path);
            EditorPropWarp _LoadInfo = new EditorPropWarp(0, "");
            JsonUtility.FromJsonOverwrite(_str, _LoadInfo);
            _editorCameraWarp = _LoadInfo;
            return true;
        }
        public static bool LoadGValueState(out EditorEngineGValue _editorGValue, string _gvalueName)
        {
            string _path = ActionEngineConst.EditorGValueSavePath(_gvalueName);
            if (!File.Exists(_path))
            {
                EngineDebug.Log($"客户端加载 GValue 数据失败，\n加载路径：{_path}");
                _editorGValue = null;
                return false;
            }
            string _str = File.ReadAllText(_path);
            EditorEngineGValue _LoadInfo = new EditorEngineGValue();
            JsonUtility.FromJsonOverwrite(_str, _LoadInfo);
            _editorGValue = _LoadInfo;
            return true;
        }
        public static bool LoadEquation(out EditorGValueEquation _editorGValue, string _gvalueName)
        {
            string _path = ActionEngineConst.EditorEquationSavePath(_gvalueName);
            if (!File.Exists(_path))
            {
                EngineDebug.Log($"客户端加载 Equation 数据失败，\n加载路径：{_path}");
                _editorGValue = null;
                return false;
            }
            string _str = File.ReadAllText(_path);
            EditorGValueEquation _LoadInfo = new EditorGValueEquation();
            JsonUtility.FromJsonOverwrite(_str, _LoadInfo);
            _editorGValue = _LoadInfo;
            return true;
        }
        public static bool LoadAssetData<T>(out T _editorValue, string _name) where T : new()
        {
            string _path = ActionEngineConst.EditorAssetDataSavePath(_name);
            if (!File.Exists(_path))
            {
                EngineDebug.Log($"客户端加载 AssetData 数据失败，\n加载路径：{_path}");
                _editorValue = new T();
                return false;
            }
            string _str = File.ReadAllText(_path);
            T _LoadInfo = new T();
            JsonUtility.FromJsonOverwrite(_str, _LoadInfo);
            _editorValue = _LoadInfo;
            return true;
        }
        #endregion

        #region 保存数据

        public static bool SaveEditorActionData(EditorActionStateInfo _editorActionState, string _actionName)
        {
            _editorActionState.EnsureListGuid();
            SaveObjData(_editorActionState, ActionEngineConst.EditorActionSavePath(_actionName), true);
            return true;
        }

        public static bool SaveActionData(EditorActionStateInfo _editorActionState, string _actionName)
        {
            _editorActionState.EnsureListGuid();

            // 编辑器 JSON：始终保存完整数据；warmup 基于完整数据收集
            ActionStateInfo _fullState = _editorActionState.GetActionStateInfo();
            WarmUpCollectResult _fullWarmUp = CollectWarmUpResult(_fullState);
            _editorActionState.mCollectWarmUp = new List<string>(_fullWarmUp.Paths);
            SaveEditorActionData(_editorActionState, _actionName);

            // Runtime 二进制：按三通道分别过滤保留的事件类型后各存一份（本地无后缀，兼容历史数据）
            ActionEngineSetting _setting = GetDefaultSetting();
            // 新建事件默认三端勾选：存档前同步，确保未打开设置面板时新增事件类型也不会被静默丢弃
            if (_setting != null && _setting.SyncDefaultRetain(ActionWindowMain.EventTypes))
            {
                EditorUtility.SetDirty(_setting);
                AssetDatabase.SaveAssetIfDirty(_setting);
            }
            for (int c = 0; c < 3; c++)
            {
                ERuntimeDataChannel _channel = (ERuntimeDataChannel)c;
                HashSet<string> _retain = GetChannelRetainSet(_setting, c);

                ActionStateInfo _actionState = _editorActionState.GetActionStateInfo();
                FilterForChannel(_actionState, _retain);
                _actionState.mCollectWarmUp = CollectWarmUpResult(_actionState).Paths;

                string _suffix = RuntimeDataChannel.GetChannelSuffix(_channel);
                SaveObjData(_actionState, string.Format(mRunTimeSavePath, "Action", _actionName + _suffix), false);
            }

            EngineDebug.Log($"成功储存  [ <color=#FFF100>{_actionName}</color> ]  Action数据(本地/远端/服务器三通道)  完整预热特效数=[<color=#FFF100>{(_fullWarmUp.Paths?.Count ?? 0)}</color>]  来自技能=[<color=#FFF100>{_fullWarmUp.SkillPathCount}</color>]");
            return true;
        }

        public static bool SaveUnitData(EditorUnitWarp _editorUnitWarp, string _unitName)
        {
            SaveObjData(_editorUnitWarp, ActionEngineConst.EditorUnitSavePath(_unitName), true);
            SaveObjData(_editorUnitWarp.GetUnitWarp(), string.Format(mRunTimeSavePath, "Unit", _unitName), false);

            EngineDebug.Log($"成功储存  [ <color=#FFF100>{_unitName}</color> ]  Unit数据 ");
            return true;
        }
        public static bool SaveSkillData(EditorSkillWarp _editorSkill, string _unitName)
        {
            // 编辑器 JSON：始终保存完整数据
            SaveObjData(_editorSkill, ActionEngineConst.EditorSkillSavePath(_unitName), true);

            // Runtime 二进制：按三通道分别过滤内嵌 ActionStateInfo 的事件类型后各存一份
            ActionEngineSetting _setting = GetSkillSetting();
            // 新建事件默认三端勾选：存档前同步，确保未打开设置面板时新增事件类型也不会被静默丢弃
            if (_setting != null && _setting.SyncDefaultRetain(ActionWindowMain.EventTypes))
            {
                EditorUtility.SetDirty(_setting);
                AssetDatabase.SaveAssetIfDirty(_setting);
            }
            for (int c = 0; c < 3; c++)
            {
                ERuntimeDataChannel _channel = (ERuntimeDataChannel)c;
                HashSet<string> _retain = GetChannelRetainSet(_setting, c);

                SkillWarp _warp = _editorSkill.GetSkillWarp();
                FilterForChannel(_warp.ActionStateInfo, _retain);

                string _suffix = RuntimeDataChannel.GetChannelSuffix(_channel);
                SaveObjData(_warp, string.Format(mRunTimeSavePath, "Skill", _unitName + _suffix), false);
            }

            EngineDebug.Log($"成功储存  [ <color=#FFF100>{_unitName}</color> ]  Skill数据(本地/远端/服务器三通道) ");
            return true;
        }
        public static bool SaveCameraWarp(EditorCameraWarp _editorCameraWarp, string _name)
        {
            SaveObjData(_editorCameraWarp, ActionEngineConst.EditorCamSavePath(_name), true);
            SaveObjData(_editorCameraWarp.GetCameraWarp(), string.Format(mRunTimeSavePath, "Camera", _name), false);

            EngineDebug.Log($"成功储存  [ <color=#FFF100>{_name}</color> ]  Camera数据 ");
            return true;
        }
        public static bool SavePropWarp(EditorPropWarp editorPropWarp, string _name)
        {
            SaveObjData(editorPropWarp, ActionEngineConst.EditorPropSavePath(_name), true);
            SaveObjData(editorPropWarp.GetPropWarp(), string.Format(mRunTimeSavePath, "Prop", _name), false);

            EngineDebug.Log($"成功储存  [ <color=#FFF100>{_name}</color> ]  Prop数据 ");
            return true;
        }
        public static bool SaveGValueData(EditorEngineGValue _engineGValue, string _gvalueName)
        {
            SaveObjData(_engineGValue, ActionEngineConst.EditorGValueSavePath(_gvalueName), true);
            SaveObjData(_engineGValue.GetEngineGValue(), string.Format(mRunTimeSavePath, "GValue", _gvalueName), false);
            EngineDebug.Log($"成功储存  [ <color=#FFF100>{_gvalueName}</color> ]  GValue数据 ");
            return true;
        }
        public static bool SaveEquationData(EditorGValueEquation _equation, string _gvalueName)
        {
            SaveObjData(_equation, ActionEngineConst.EditorEquationSavePath(_gvalueName), true);
            SaveObjData(_equation.GetGValueEquation(), string.Format(mRunTimeSavePath, "Equation", _gvalueName), false);
            EngineDebug.Log($"成功储存  [ <color=#FFF100>{_gvalueName}</color> ]  Equation数据 ");
            return true;
        }
        public static bool SaveInputModuleData(InputModuleInfo _engineInput, string _name)
        {
            SaveObjData(_engineInput, ActionEngineConst.EditorInputSavePath(_name), true);
            SaveObjData(_engineInput, string.Format(mRunTimeSavePath, "InputSetting", _name), false);
            EngineDebug.Log($"成功储存  [ <color=#FFF100>{_name}</color> ]  InputSetting数据 ");
            return true;
        }

        public static bool SaveAssetData(object _asset, string _name)
        {
            SaveObjData(_asset, ActionEngineConst.EditorAssetDataSavePath(_name), true);
            SaveObjData(_asset, string.Format(mRunTimeSavePath, "AssetData", _name), false);
            // EngineDebug.Log($"成功储存AssetData:  [ <color=#FFF100>{_name}</color> ]  数据 ");
            return true;
        }
        #endregion

        //数据保存函数
        private static void SaveObjData(object _data, string _path, bool _saveEditorData)
        {
            if (_saveEditorData)
            {
                //保存编辑器数据（Json），缩进换行以便 SVN 逐行 diff
                string _str = JsonUtility.ToJson(_data, true);
                File.WriteAllText(_path, _str);
            }
            else
            {
                //保存Runtime数据（二进制）
                using (FileStream _fileStream = new FileStream(_path, FileMode.Create))
                {
                    BinaryFormatter _binaryFormatter = new BinaryFormatter();
                    _binaryFormatter.Serialize(_fileStream, _data);
                }
            }
        }

        // 取某通道的保留事件类型集合；返回 null 表示该通道未配置 → 视为全部保留（不过滤，兼容旧数据）
        private static HashSet<string> GetChannelRetainSet(ActionEngineSetting _setting, int _channelIndex)
        {
            if (_setting == null || _setting.mRuntimeChannels == null || _channelIndex >= _setting.mRuntimeChannels.Count)
                return null;
            RuntimeChannelRetain _ch = _setting.mRuntimeChannels[_channelIndex];
            if (_ch == null || _ch.retainedEventTypes == null)
                return null;
            return new HashSet<string>(_ch.retainedEventTypes);
        }

        // 按通道保留集裁剪 ActionStateInfo：移除未保留事件类型的事件；跳转由 EET_Interrupt 开关整体控制。
        // _retainKeys 为 null 表示不过滤（全部保留）。mAnimEvent（动画事件）为播放基础，不参与过滤。
        private static void FilterForChannel(ActionStateInfo _info, HashSet<string> _retainKeys)
        {
            if (_info == null || _retainKeys == null || _info.mActionState == null) return;

            bool _keepInterrupt = _retainKeys.Contains(nameof(EEvenTypeInternal.EET_Interrupt));

            foreach (ActionState _state in _info.mActionState)
            {
                if (_state == null) continue;

                FilterEventList(_state.EventList, _retainKeys);
                FilterEventList(_state.EventList_Anim, _retainKeys);

                if (!_keepInterrupt)
                {
                    _state.InterruptList?.Clear();
                    _state.InterruptList_BeHit?.Clear();
                    _state.InterruptList_OnHit?.Clear();
                    _state.InterruptGroupList?.Clear();
                }
            }
        }

        private static void FilterEventList(List<ActionEvent> _list, HashSet<string> _retainKeys)
        {
            if (_list == null) return;
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                ActionEvent _ev = _list[i];
                if (_ev == null || _ev.EventData == null) continue; // 无事件数据，保守保留
                string _key = GetEventTypeKey(_ev.EventData.GetEvenType());
                if (_key != null && !_retainKeys.Contains(_key))
                    _list.RemoveAt(i);
            }
        }

        // runtime 事件 GetEvenType() int → 事件类型 key（与 ActionWindowMain.EventTypes 一致，复用 EditorActionEvent.EventName 换算）
        private static string GetEventTypeKey(int _evenType)
        {
            if (ActionWindowMain.EventTypes == null) return null;
            int _engineLen = ActionWindowMain.EventType_m?.Length ?? 0;
            int _idx = _evenType < 0 ? -_evenType : _evenType + _engineLen;
            if (_idx >= 0 && _idx < ActionWindowMain.EventTypes.Count)
                return ActionWindowMain.EventTypes[_idx];
            return null;
        }

        //创建Action轨道配置
        private static ActionEngineSetting CreateActionEngineSetting()
        {
            ActionEngineSetting _setting = ScriptableObject.CreateInstance<ActionEngineSetting>();
            _setting.mTrackEvents.Add(new TrackEvents("攻击盒", "攻击轨道", new[] { "EET_AttackBox" }));
            _setting.mTrackEvents.Add(new TrackEvents("单位事件", "事件"));
            _setting.mTrackEvents.Add(new TrackEvents("动作组", "动作轨道", new[] { "EET_Interrupt" }));
            _setting.mTrackEvents.Add(new TrackEvents("镜头组", "镜头轨道", new[] { "EET_CameraChange", "EET_CameraShake" }));
            _setting.mTrackEvents.Add(new TrackEvents("特效组", "特效轨道", new[] { "EET_Partocle" }));
            _setting.mTrackEvents.Add(new TrackEvents("音效组", "音效轨道", new[] { "EET_Audio" }));

            return _setting;
        }

        //创建技能Action轨道配置
        private static ActionEngineSetting CreateSkillActionEngineSetting()
        {
            ActionEngineSetting _setting = ScriptableObject.CreateInstance<ActionEngineSetting>();
            _setting.mTrackEvents.Add(new TrackEvents("实体组", "实体轨道", new[] { "EET_AttackBox" }));
            _setting.mTrackEvents.Add(new TrackEvents("行为组", "行为轨道", new[] { "EET_Interrupt" }));
            _setting.mTrackEvents.Add(new TrackEvents("跳转组", "跳转轨道", new[] { "EET_Interrupt" }));
            _setting.mTrackEvents.Add(new TrackEvents("控制组", "控制轨道", new[] { "EET_Partocle" }));
            return _setting;
        }
    }
}
