using System;
using System.Collections.Generic;
using AsiActionEditor_Ex.RunTime;
using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.DrawData;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEngine;

#if FMOD
using FMODUnity;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace AsiTimeLine.Editor
{
    public partial class DrawInspector
    {
        public static void DrawEventSampleEvent(EditorActionEvent _actionEvent, IActionEventData _eventData, EEvenType _evenType, bool _isInit, out bool _error)
        {
            _error = false;
            //属性面板绘制
            switch (_evenType)
            {
                case EEvenType.EET_CameraChange:
                    DrawCameraChange(_actionEvent);
                    break;
                case EEvenType.EET_Partocle:
                    if (DrawPartocleData((Event_PlayParticle)_eventData, _isInit))
                        _error = true;
                    break;
                case EEvenType.EET_Audio:
                    DrawAudio((Event_PlayAudio)_eventData, _actionEvent, _isInit);
                    break;
                case EEvenType.EET_FindTargetToCircle:
                    DrawFindTrigger((Event_FindTargetToCircle)_eventData);
                    //    DrawFindTargetToSphere((Event_FindTarget)_eventData, _actionEvent);
                    break;
                case EEvenType.EET_SetAnimFloat:
                    DrawSetAnimatorFloat((Event_SetAnimFloat)_eventData);
                    break;
                case EEvenType.EET_SetAnimFloatFromBluePrint:
                    DrawSetAnimatorFloatFromBluePrint((Event_SetAnimFloatFromBluePrint)_eventData);
                    break;
                case EEvenType.EET_SetGValue:
                    DrawSetGValue((Event_SetGValue)_eventData, _actionEvent);
                    break;
                case EEvenType.EET_SetGValueFromGValue:
                    DrawSetGValueFromGValue((Event_SetGValueFromGValue)_eventData, _actionEvent);
                    break;
                case EEvenType.EET_Attach:
                    DrawWeaponPointChange((Event_Attach)_eventData, _actionEvent);
                    break;
                case EEvenType.EET_SceneInteractObject:
                    DrawSceneInteractObject((Event_SceneInteractObject)_eventData);
                    break;
                case EEvenType.EET_CharacterOnMove:
                    DrawCharacterOnMove((Event_CharacterOnMove)_eventData);
                    break;
                case EEvenType.EET_TowBoneIK:
                    DrawTowBoneIK((Event_TowBoneIK)_eventData);
                    // DrawEditorAttribute.Draw((Event_TowBoneIK)_eventData);
                    break;
                case EEvenType.EET_Dissolve:
                case EEvenType.EET_RimLight:
                    DrawRimLight(_eventData);
                    break;
                case EEvenType.EET_WeaponTrail:
                    DrawWeaponTrail((Event_WeaponTrail)_eventData);
                    // DrawEditorAttribute.Draw((Event_TowBoneIK)_eventData);
                    break;
                case EEvenType.EET_SimulatedInput:
                    DrawSimulatedInput((Event_SimulatedInput)_eventData, _isInit, _actionEvent);
                    // DrawEditorAttribute.Draw((Event_TowBoneIK)_eventData);
                    break;
                case EEvenType.EET_HitkBox_Sphere:
                    DrawHitBox_Sphere((Event_HitBox_Sphere)_eventData);
                    break;
                case EEvenType.EET_PathFind:
                    DrawPathFind((Event_PathFind)_eventData, _actionEvent);
                    break;
                case EEvenType.EET_OnGBoolChanged:
                case EEvenType.EET_OnGEnumChanged:
                case EEvenType.EET_OnGFloatChanged:
                case EEvenType.EET_OnGIntChanged:
                case EEvenType.EET_OnGUnitChanged:
                case EEvenType.EET_OnGBoolChanged_Array:
                case EEvenType.EET_OnGEnumChanged_Array:
                case EEvenType.EET_OnGFloatChanged_Array:
                case EEvenType.EET_OnGIntChanged_Array:
                case EEvenType.EET_OnGUnitChanged_Array:
                    DrawOnGValueChanged(_eventData);
                    break;
                case EEvenType.EET_Light:
                    DrawLight((Event_Light)_eventData);
                    break;
                case EEvenType.EET_SkillLight:
                    DrawSkillLight((Event_SkillLight)_eventData);
                    break;

#if PuppetMaster
                case EEvenType.EET_PuppetMaster:
                    DrawPuppetMaster((Event_PuppetMaster)_eventData, _actionEvent);
                    break;
#endif

                default:
                    //绘制 EditorProperty 
                    if (DrawEditorAttribute.Draw(_eventData))
                        _error = true;
                    break;
            }
        }

        #region DrawFuntion
        private static void DrawOnGValueChanged(IActionEventData _event)
        {
            if (_event is IGValueListenEvent listenEvent)
            {
                DrawNames.Add("ListenKind");
                if (listenEvent.ListenKind == EGValueListenKind.GInt) DrawNames.Add("ListenGInt");
                else if (listenEvent.ListenKind == EGValueListenKind.GFloat) DrawNames.Add("ListenGFloat");
                else if (listenEvent.ListenKind == EGValueListenKind.GBool) DrawNames.Add("ListenGBool");
                else if (listenEvent.ListenKind == EGValueListenKind.GEnum) DrawNames.Add("ListenGEnum");
                else if (listenEvent.ListenKind == EGValueListenKind.GUnit) DrawNames.Add("ListenGUnit");
                DrawNames.Add("ListenTargetUnit");
                DrawNames.Add("BluePrint");
                DrawNames.Add("WriteTarget");
                DrawNames.Add("TargetUnit");
                DrawEditorAttribute.Draw(_event, DrawNames);
            }
            else
            {
                EditorGUILayout.HelpBox("当前GValue监听事件不继承自IGValueListenEvent,请联系程序员", MessageType.Error);
                return;
            }
        }
#if PuppetMaster
        private static void DrawPuppetMaster(Event_PuppetMaster _event, EditorActionEvent _actionEvent)
        {
            DrawNames.Add("Op");

            switch (_event.Op)
            {
                case 0: // Kill
                    DrawNames.Add("KillDuration");
                    DrawNames.Add("DeadMuscleWeight");
                    DrawNames.Add("ExitRestore");
                    if (_actionEvent.Duration != 0)
                    {
                        DrawNames.Add("ApplyForce");
                        if (_event.ApplyForce)
                        {
                            DrawNames.Add("Force");
                            DrawNames.Add("UnPin");
                            DrawNames.Add("ForceTarget");
                            if (_event.ForceTarget == 1)
                                DrawNames.Add("MuscleIndex");
                        }
                    }
                    break;

                case 1: // Resurrect
                    break;

                case 2: // Freeze
                    break;

                case 3: // SetMode
                    DrawNames.Add("TargetMode");
                    DrawNames.Add("ExitRestore");
                    break;

                case 4: // Hit
                    DrawNames.Add("Force");
                    DrawNames.Add("UnPin");
                    DrawNames.Add("ForceTarget");
                    if (_event.ForceTarget == 1)
                        DrawNames.Add("MuscleIndex");
                    break;
            }

            if (_actionEvent.Duration != 0)
                DrawNames.Add("RandomDelayMax");

            DrawEditorAttribute.Draw(_event, DrawNames);
        }
#endif

        private static void DrawPathFind(Event_PathFind _event, EditorActionEvent _actionEvent)
        {
            if (_actionEvent.Duration != 0)
            {
                //DrawNames.Add("UpdateInterval");
                {
                    //GUILayout.Label("路径更新间隔:", GUILayout.Width(80));
                    if (_event.UpdateInterval > -1)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("按间隔时间更新路径(s):", GUILayout.Width(160)))
                            {
                                _event.UpdateInterval = -2;
                                return;
                            }
                            _event.UpdateInterval = MathF.Max(EditorGUILayout.FloatField(_event.UpdateInterval), 0);
                        }
                        EditorGUILayout.HelpBox("按设定的时间来决定路径数据更新频率,0为每帧更新" +
                            "\nPS:路径更新次数越频繁, 计算压力越大", MessageType.Info);
                    }
                    else
                    {
                        if (GUILayout.Button("仅在进入时更新一次路径"))
                        {
                            _event.UpdateInterval = 0;
                        }
                        EditorGUILayout.HelpBox("只会在每次进入该事件时更新一次路径", MessageType.Info);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("当前只会在每次进入该事件时更新一次路径", MessageType.Info);
            }
            DrawNames.Add("TimeOut");
            DrawNames.Add("FindPathInterrput");

            DrawNames.Add("PathPoints");
            DrawNames.Add("Radius");
            DrawNames.Add("SoftRadius");
            DrawNames.Add("StartPos");
            DrawNames.Add("TargetPos");
            DrawNames.Add("UesType");
            if (_event.UesType == 1)
                DrawNames.Add("GVelocity");
            else
                DrawNames.Add("GSpeed");

            DrawNames.Add("SetGvalueToFinish");
            if (_event.SetGvalueToFinish)
                DrawNames.Add("GvalueSetting");
            DrawNames.Add("ChangeActionToFinish");
            if (_event.ChangeActionToFinish)
                DrawNames.Add("ActionID");
            DrawNames.Add("IsDraw");
            DrawEditorAttribute.Draw(_event, DrawNames);
        }
        private static void DrawHitBox_Sphere(Event_HitBox_Sphere _event)
        {
            DrawNames.Add((_event.UseBluePrint_Scale ? "Radius" : "BluePrint_Scale"));
            DrawEditorAttribute.Draw(_event, DrawNames, false);
        }
        private static void DrawCharacterOnMove(Event_CharacterOnMove _event)
        {
            DrawNames.Add("IsMoveInput");
            DrawNames.Add("NoveVelocity");
            bool isLerpMove = _event.LerpSpeed.mSerValue > -10;
            if (isLerpMove)
            {
                //Debug.LogError("绘制插值速度: " + DrawNames.Count);
                DrawNames.Add("LerpSpeed");
            }
            DrawEditorAttribute.Draw(_event, DrawNames);
            using (var _check = new EditorGUI.ChangeCheckScope())
            {
                bool _value = EditorGUILayout.Toggle("按轨道长度位移", !isLerpMove);
                if (_check.changed)
                {
                    if (_value) _event.LerpSpeed.mSerValue = -20;
                    else _event.LerpSpeed.mSerValue = 0;
                }
            }
        }
        private static void DrawFindTrigger(Event_FindTargetToCircle _check)
        {
            DrawNames.Add("CenterPoint");
            DrawNames.Add("PosOffset");
            DrawNames.Add("ScenceLayer");
            DrawNames.Add("GroupType");

            if (_check.GroupType == 0)
            {
                DrawNames.Add("GroupPoint");
            }
            else if (_check.GroupType == 1)
            {
                DrawNames.Add("GroupTransform");
            }
            else
            {
                DrawNames.Add("GroupUnit");
                DrawNames.Add("DisP");
            }
            DrawNames.Add("IsOverrid");
            DrawNames.Add("CheckAngle");
            DrawEditorAttribute.Draw(_check, DrawNames);
            DrawNames.Clear();
            if (_check.CheckAngle)
            {
                DrawEditorAttribute.Draw(_check, new[] { "Angle" });

                //using (new GUILayout.HorizontalScope())
                //{
                //    //GUILayout.Label("对比角度", GUILayout.Width(60));
                //    //_check.m_CheckAngleGreater = EditorGUILayout.Popup(_check.m_CheckAngleGreater ? 0 : 1, duibiFH) == 0;
                //    _check.m_Angle = EditorGUILayout.FloatField(_check.m_Angle);
                //    // _check.IsRange = EditorGUILayout.Toggle("居中",_check.IsRange);
                //}
                DrawEditorAttribute.Draw(_check, new[] { "AngleOffset" });

                // DrawNames.Add("AngleOffset");
                // DrawNames.Add("IsRange");
                DrawEditorAttribute.Draw(_check, new[] { "Distance" });
                DrawEditorAttribute.Draw(_check, new[] { "ConstomHeightS" });
                if (_check.ConstomHeightS)
                    DrawEditorAttribute.Draw(_check, new[] { "ConstomHeight" });
            }
            else
            {
                DrawEditorAttribute.Draw(_check, new[] { "Distance" });
                DrawEditorAttribute.Draw(_check, new[] { "ConstomHeightS" });
                if (_check.ConstomHeightS)
                    DrawEditorAttribute.Draw(_check, new[] { "ConstomHeight" });
                //DrawNames.Add("PosOffset");
            }
            //DrawEditorAttribute.Draw(_check, DrawNames.ToArray());
        }
        private static void DrawWeaponTrail(Event_WeaponTrail _eventData)
        {
            //_eventNames.Add("IkTargetID");
            //_eventNames.Add("IsReferTarget");
            //if (_eventData.IsReferTarget) _eventNames.Add("ReferTargetID");
            //_eventNames.Add("IkPoint");
            //_eventNames.Add("Axis_Up");
            //_eventNames.Add("Axis_Forward");

            //_eventNames.Add("EnterTime");
            //_eventNames.Add("ExitTime");
#if AraTrail
            DrawNames.Add("SetToPoint");
            if (_eventData.SetToPoint) DrawNames.Add("HelpPoint");
            else DrawNames.Add("PropGType");
            DrawNames.Add("TrailPath");
            DrawNames.Add("InitTime");
            DrawNames.Add("DelayTime");
            DrawEditorAttribute.Draw(_eventData, DrawNames);
#else
            GUILayout.Label("丢失刀光相关程序集，或未添加宏[AraTrail]定义");
#endif
        }

        private static int inputActionID = -1;
        private static void DrawSimulatedInput(Event_SimulatedInput _eventData, bool _isInit, EditorActionEvent _actionEvent)
        {
            if (_isInit)
            {
                inputActionID = -1;
                if (!string.IsNullOrEmpty(_eventData.CheckKeyName))
                {
                    for (int i = 0; i < InputActionList.Instance.ActionList.Count; i++)
                    {
                        if (_eventData.CheckKeyName == InputActionList.Instance.ActionList[i])
                        {
                            inputActionID = i;
                            break;
                        }
                    }
                }
                else
                {
                    _eventData.CheckKeyName = InputActionList.Instance.ActionList[0];
                    inputActionID = 0;
                }
                // Debug.Log("初始化");
            }

            using (var _check = new EditorGUI.ChangeCheckScope())
            {

                inputActionID =
                    EditorGUILayout.Popup(inputActionID, InputActionList.Instance.ActionList.ToArray());
                if (_check.changed)
                {
                    _eventData.CheckKeyName = InputActionList.Instance.ActionList[inputActionID];
                }
            }

            DrawEditorAttribute.Draw(_eventData);

            if (_actionEvent.Duration != 0)
            {
                EditorGUILayout.HelpBox("事件处于长轨状态时,在事件结束或者退出后自动回收按键", MessageType.Info);
            }
        }

        private static void DrawTowBoneIK(Event_TowBoneIK _eventData)
        {
            DrawNames.Add("IkTargetID");
            DrawNames.Add("IsReferTarget");
            if (_eventData.IsReferTarget) DrawNames.Add("ReferTargetID");
            DrawNames.Add("IkPoint");
            DrawNames.Add("Axis_Up");
            DrawNames.Add("Axis_Forward");

            DrawNames.Add("EnterTime");
            DrawNames.Add("ExitTime");
            DrawEditorAttribute.Draw(_eventData, DrawNames);
        }
        private static List<string> DrawDicNames = new List<string>();
        private static void DrawAudio(Event_PlayAudio _eventData, EditorActionEvent _actionEvent, bool _isInit)
        {
#if FMOD
            DrawAudio_FMod(_eventData, _actionEvent, _isInit);
#else
            DrawAudio_Default(_eventData, _actionEvent);
#endif
            // ResourcesWindow.Instance.TryGetCharacterConfig()
        }

#if FMOD
        private static SerializedObject _serObj = null;
        private static void DrawAudio_FMod(Event_PlayAudio _eventData, EditorActionEvent _actionEvent, bool _isInit)
        {
            if (_isInit)
            {
                ActionEngineManager.Instance.mEventReference = _eventData.m_EventReference;
                if (_serObj == null)
                    _serObj = new SerializedObject(ActionEngineManager.Instance);
                _serObj.Update();
            }

            if (ResourcesWindow.Instance.GetRole().TryGetComponent(out ActionEngine_Audio audio))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("选择音源组件", GUILayout.Width(80));
                    _eventData.m_AudioSourceIndex = (byte)EditorGUILayout.Popup(_eventData.m_AudioSourceIndex, audio.m_AudioSourceNames.ToArray());
                }

                SerializedProperty _fmod = _serObj.FindProperty("mEventReference");
                //SerializedProperty _fmodPath = _fmod.FindPropertyRelative("Path");
                EditorGUILayout.PropertyField(_fmod, new GUIContent(L10n.Tr("Event")));

                _serObj.ApplyModifiedProperties();

                _eventData.m_EventReference = ActionEngineManager.Instance.mEventReference;
                GUILayout.Space(5);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("音频预览", GUILayout.Width(80));
                    using (new GUIColorScope(Color.green))
                    {
                        if (GUILayout.Button("Play"))
                        {
                            EditorEventUpdate.PlayFModAudio(_eventData);
                        }
                    }
                    using (new GUIColorScope(Color.red))
                    {
                        if (GUILayout.Button("Stop"))
                        {
                            EditorEventUpdate.StopFModAudio(_eventData);
                        }
                    }
                }
                //GUILayout.Space(10);
                //GUILayout.TextField(_eventData.m_EventReference.Path);
                //EditorGUILayout.FloatField(_eventData.m_AudioVolume);
            }
            else
            {
                EditorGUILayout.HelpBox("未在角色身上找到 [ActionEditor_Audio] 组件，无法执行", MessageType.Error);
            }
        }
#else
        private static void DrawAudio_Default(Event_PlayAudio _eventData, EditorActionEvent _actionEvent)
        {
            if (ResourcesWindow.Instance.GetUnit().TryGetComponent(out ActionEngine_Audio audio))
            {
                DrawDicNames.Clear();
                AudioClipDicList _list = ActionEngineManager_AudioClip.Instance._audioClipDicList;
                foreach (AudioClipDic VARIABLE in _list.clips)
                {
                    DrawDicNames.Add(VARIABLE.name);
                }
                if (GUILayout.Button("打开音效设定窗口"))
                {
                    AudioClipWindows.Instance.Open();
                }
                GUILayout.Space(10);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("音量大小", GUILayout.Width(60));
                    _eventData.m_AudioVolume = EditorGUILayout.Slider(_eventData.m_AudioVolume, 0.0f, 1.0f);
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("选择音源组件", GUILayout.Width(80));
                    _eventData.m_AudioSourceIndex = (byte)EditorGUILayout.Popup(_eventData.m_AudioSourceIndex, audio.m_AudioSourceNames.ToArray());
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("选择音效字典", GUILayout.Width(80));
                    using (var _check = new EditorGUI.ChangeCheckScope())
                    {
                        _eventData.m_AudioDicID = (byte)EditorGUILayout.Popup(_eventData.m_AudioDicID, DrawDicNames.ToArray());
                        if (_check.changed)
                        {
                            _eventData.m_AudioDicChailID = 0;
                        }
                    }
                }
                _eventData.m_CoustomAudio = GUILayout.Toggle(_eventData.m_CoustomAudio, "自定义具体播放音效");
                if (_eventData.m_CoustomAudio)
                {
                    DrawDicNames.Clear();
                    foreach (AudioClipGroup_Part VARIABLE in _list.clips[_eventData.m_AudioDicID].parts)
                    {
                        DrawDicNames.Add(VARIABLE.ClipName);
                    }
                    _eventData.m_AudioDicChailID = (byte)EditorGUILayout.Popup(_eventData.m_AudioDicChailID, DrawDicNames.ToArray());

                    GUILayout.Space(10);

                    AudioClipGroup_Part _part = _list.clips[_eventData.m_AudioDicID]
                        .parts[_eventData.m_AudioDicChailID];

                    string _type = _part.isRandom ? "随机循环" : "上至下顺序循环";
                    GUILayout.Label($" [{_part.ClipName}]   下的音频列表， 该列表以   [{_type}]   的方式播放");
                    for (int i = 0; i < _part.AudioClips.Count; i++)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            using (var _check = new EditorGUI.ChangeCheckScope())
                            {
                                AudioClip _audioClip =
                                    EditorGUILayout.ObjectField(_part.AudioClips[i], typeof(AudioClip), false) as
                                        AudioClip;
                                if (_check.changed)
                                {
                                    //设置音频资产
                                    _part.SetAudioClip(i, _audioClip);
                                }
                            }
                            if (GUILayout.Button("打开配置窗口", GUILayout.Width(80)))
                            {
                                foreach (AudioClipGroup_Part VARIABLE2 in _list.clips[_eventData.m_AudioDicID].parts)
                                {
                                    VARIABLE2.open = false;
                                }

                                _part.open = true;
                                AudioClipWindows.Instance.selectedTool = _eventData.m_AudioDicID;
                                AudioClipWindows.Instance.Open();
                            }
                        }
                    }
                    if (GUILayout.Button("保存音频列表配置"))
                    {
                        AudioClipWindows.Instance.SaveAudioConfig();
                    }
                }
                else
                {
                    GUILayout.Space(10);
                    GUILayout.Label($" [{DrawDicNames[_eventData.m_AudioDicID]}]  字典的成员");
                    for (int i = 0; i < _list.clips[_eventData.m_AudioDicID].parts.Count; i++)
                    {
                        AudioClipGroup_Part VARIABLE = _list.clips[_eventData.m_AudioDicID].parts[i];
                        if (GUILayout.Button("字典成员" + VARIABLE.ClipName))
                        {
                            AudioClipWindows.Instance.selectedTool = _eventData.m_AudioDicID;
                            foreach (AudioClipGroup_Part VARIABLE2 in _list.clips[_eventData.m_AudioDicID].parts)
                            {
                                VARIABLE2.open = false;
                            }
                            VARIABLE.open = true;
                            AudioClipWindows.Instance.Open();
                        }
                    }

                }


            }
            else
            {
                EditorGUILayout.HelpBox("未在角色身上找到 [ActionEditor_Audio] 组件，无法执行", MessageType.Error);
            }
        }

#endif

        private static readonly Dictionary<LightData, bool> s_LightItemFoldouts = new Dictionary<LightData, bool>();
        private static readonly Dictionary<LightData, bool> s_SkillLightItemFoldouts = new Dictionary<LightData, bool>();

        private static void DrawLight(Event_Light eventData)
        {
            PingGlobalAssetButton();
            // 事件层级属性（共用预制体）
            DrawEditorAttribute.Draw(eventData);

            eventData.Lights ??= new List<LightData>();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"灯光数量：{eventData.Lights.Count}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(28f)))
            {
                eventData.Lights.Add(new LightData());
            }
            GUILayout.EndHorizontal();

            int removeIndex = -1;
            for (int i = 0; i < eventData.Lights.Count; i++)
            {
                var data = eventData.Lights[i];
                if (data == null)
                {
                    eventData.Lights[i] = data = new LightData();
                }

                if (!s_LightItemFoldouts.TryGetValue(data, out var expanded))
                {
                    expanded = true;
                }

                EditorGUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                expanded = EditorGUILayout.Foldout(expanded, $"灯光 #{i}", true);
                s_LightItemFoldouts[data] = expanded;
                GUILayout.FlexibleSpace();
                GUI.enabled = i > 0;
                if (GUILayout.Button("↑", GUILayout.Width(24f)))
                {
                    (eventData.Lights[i - 1], eventData.Lights[i]) = (eventData.Lights[i], eventData.Lights[i - 1]);
                }
                GUI.enabled = i < eventData.Lights.Count - 1;
                if (GUILayout.Button("↓", GUILayout.Width(24f)))
                {
                    (eventData.Lights[i + 1], eventData.Lights[i]) = (eventData.Lights[i], eventData.Lights[i + 1]);
                }
                GUI.enabled = true;
                if (GUILayout.Button("-", GUILayout.Width(24f)))
                {
                    removeIndex = i;
                }
                GUILayout.EndHorizontal();

                if (expanded)
                {
                    EditorGUI.indentLevel++;
                    DrawEditorAttribute.Draw(data);
                    if (data.GroundLayer < 0)
                    {
                        EditorGUILayout.HelpBox("检测到地面层级小于0，请确认", MessageType.Error);
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
            {
                s_LightItemFoldouts.Remove(eventData.Lights[removeIndex]);
                eventData.Lights.RemoveAt(removeIndex);
            }
        }

        private static void PingGlobalAssetButton()
        {
            if (!GUILayout.Button("GlobalAsset"))
            {
                return;
            }

            var globalAssets = AssetDatabase.FindAssets("pre_cfg_global_assets");
            GameObject result = null;
            foreach (var asset in globalAssets)
            {
                var path = AssetDatabase.GUIDToAssetPath(asset);
                var assets = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (assets && assets.TryGetComponent(out GlobalAssetsComponent _))
                {
                    result = assets;
                    break;
                }
            }

            if (result)
            {
                EditorGUIUtility.PingObject(result);
            }
        }

        private static void DrawSkillLight(Event_SkillLight eventData)
        {
            PingGlobalAssetButton();
            // 事件层级属性（共用预制体）
            DrawEditorAttribute.Draw(eventData);

            eventData.Lights ??= new List<LightData>();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"灯光数量：{eventData.Lights.Count}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(28f)))
            {
                eventData.Lights.Add(new LightData());
            }
            GUILayout.EndHorizontal();

            int removeIndex = -1;
            for (int i = 0; i < eventData.Lights.Count; i++)
            {
                var data = eventData.Lights[i];
                if (data == null)
                {
                    eventData.Lights[i] = data = new LightData();
                }

                if (!s_SkillLightItemFoldouts.TryGetValue(data, out var expanded))
                {
                    expanded = true;
                }

                EditorGUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                expanded = EditorGUILayout.Foldout(expanded, $"灯光 #{i}", true);
                s_SkillLightItemFoldouts[data] = expanded;
                GUILayout.FlexibleSpace();
                GUI.enabled = i > 0;
                if (GUILayout.Button("↑", GUILayout.Width(24f)))
                {
                    (eventData.Lights[i - 1], eventData.Lights[i]) = (eventData.Lights[i], eventData.Lights[i - 1]);
                }
                GUI.enabled = i < eventData.Lights.Count - 1;
                if (GUILayout.Button("↓", GUILayout.Width(24f)))
                {
                    (eventData.Lights[i + 1], eventData.Lights[i]) = (eventData.Lights[i], eventData.Lights[i + 1]);
                }
                GUI.enabled = true;
                if (GUILayout.Button("-", GUILayout.Width(24f)))
                {
                    removeIndex = i;
                }
                GUILayout.EndHorizontal();

                if (expanded)
                {
                    EditorGUI.indentLevel++;
                    DrawEditorAttribute.Draw(data);
                    if (data.GroundLayer < 0)
                    {
                        EditorGUILayout.HelpBox("检测到地面层级小于0，请确认", MessageType.Error);
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
            {
                s_SkillLightItemFoldouts.Remove(eventData.Lights[removeIndex]);
                eventData.Lights.RemoveAt(removeIndex);
            }
        }
        
        private static void DrawFindTargetToSphere(Event_FindTargetToSphere _eventData, EditorActionEvent _actionEvent)
        {
            DrawNames.Add("IsFindUnit");
            // if (!_eventData.IsFindUnit)
            // {
            DrawNames.Add("LayerMask");
            // }

            DrawNames.Add("RadiuCenterID");
            DrawNames.Add("Radius");
            DrawNames.Add("ReferDir");
            DrawNames.Add("FindType");
            if (_eventData.FindType == Event_FindTargetToSphere.EFindType.在范围内按分值查找单位)
            {
                DrawNames.Add("PosInttegral");
                DrawNames.Add("RotInttegral");
            }

            DrawNames.Add("IsSetNull");
            GUILayout.Space(5);
            if (_eventData.IsFindUnit) DrawNames.Add("SetUnit");
            else DrawNames.Add("SetTransform");
            DrawEditorAttribute.Draw(_eventData, DrawNames);

            GUILayout.Space(10);
            GUILayout.Label("仅Editor下预览用");
            using (new GUILayout.HorizontalScope())
            {
                // if (GUILayout.Button("在目标位置中心随机撒点"))
                // {
                //     // EngineDrawListData.Instance.Draw_Point_Group.Add();
                // }
                GUILayout.Label("在目标位置中心随机撒点, 数量：");
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    int number =
                        EditorGUILayout.DelayedIntField(EngineDrawListData.Instance.Draw_Point_Group.Count);
                    if (check.changed)
                    {
                        if (ResourcesWindow.Instance.TryGetCharacterConfig(out CharacterConfig config))
                        {
                            if (config.HelpPointDic.TryGetValue((ECharacteLimbType)_eventData.RadiuCenterID,
                                    out Transform _transform))
                            {
                                EngineDrawListData.Instance.Draw_Point_Group.Clear();
                                for (int i = 0; i < number; i++)
                                {
                                    Quaternion rot = Quaternion.Euler(0, 0, 0);
                                    Vector3 pos = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f),
                                        Random.Range(-1.0f, 1.0f));
                                    pos = pos.normalized * _eventData.Radius;
                                    pos += _transform.position;
                                    Draw_PointTransData _point =
                                        new Draw_PointTransData(pos, rot, "point_" + i, Color.red);
                                    EngineDrawListData.Instance.Draw_Point_Group.Add(_point);
                                }
                            }
                        }
                    }
                }
            }

            if (GUILayout.Button("清空点数据"))
            {
                EngineDrawListData.Instance.Draw_Point_Group.Clear();
            }
        }

        private static void DrawWeaponPointChange(Event_Attach _eventData, EditorActionEvent _actionEvent)
        {
            DrawNames.Add("AlignToPoint");
            DrawNames.Add("ReferTransform");

            if (_eventData.AlignToPoint)
            {//对齐至点数据
                DrawNames.Add("AlignPoint");
            }
            else
            {
                DrawNames.Add("TargetTransform");
                DrawNames.Add("SetPrente");
                DrawNames.Add("alignToTarget");
            }
            DrawNames.Add("LerpTime");
            DrawNames.Add("OffsetPos");
            DrawNames.Add("OffsetRot");
            DrawEditorAttribute.Draw(_eventData, DrawNames);
        }

        private static void DrawSceneInteractObject(Event_SceneInteractObject _event)
        {
            DrawEditorAttribute.Draw(_event);
            EditorGUILayout.HelpBox("不管是单帧还是长轨,都只执行一次", MessageType.Warning);
            //EditorGUILayout.LabelField("读取交互数据", EditorStyles.boldLabel);
            //DrawEditorAttribute.Draw(_event, new[]
            //{
            //    "ReadMousePos",
            //    "ReadInteractPoint",
            //    "ReadInteractObjPoint",
            //    "ReadSetTarget",
            //    "ReadSetType",
            //    "ReadInteractType",
            //    "ReadIsGround",
            //    "ReadIsCanInteract",
            //    "ReadInteractActionID",
            //    "ReadInteractRange",
            //});

            //EditorGUILayout.Space();
            //EditorGUILayout.LabelField("写入交互数据", EditorStyles.boldLabel);
            //DrawEditorAttribute.Draw(_event, new[]
            //{
            //    "WriteMousePos",
            //    "WriteInteractPoint",
            //    "WriteInteractObjPoint",
            //    "WriteSetTarget",
            //    "WriteSetType",
            //    "WriteInteractType",
            //    "WriteIsGround",
            //    "WriteIsCanInteract",
            //    "WriteInteractActionID",
            //    "WriteInteractRange",
            //});
        }

        private static void DrawSetGValue(Event_SetGValue _event, EditorActionEvent _actionEvent)
        {
            if (_actionEvent.Duration == 0)
            {
                DrawNames.Add("GValueSet");
            }
            else
            {
                DrawNames.Add("GValueSet");
                DrawNames.Add("UpdateValue");
                DrawNames.Add("ExitGValueSet");
            }
            DrawNames.Add("TargetUnit");

            DrawEditorAttribute.Draw(_event, DrawNames);
        }

        private static void DrawSetGValueFromGValue(Event_SetGValueFromGValue _event, EditorActionEvent _actionEvent)
        {
            DrawNames.Add("GValueSet");
            if (_actionEvent.Duration != 0)
            {
                DrawNames.Add("ExitGValueSet");
            }
            DrawNames.Add("TargetUnit");

            DrawEditorAttribute.Draw(_event, DrawNames);
        }
        private static void DrawUnitRot(Event_UnitRot _event, EditorActionEvent _actionEvent)
        {
            DrawNames.Add("IsMove");
            if (_event.IsMove)
            {
                DrawNames.Add("IsMovePre");
            }
            if (_actionEvent.Duration == 0)
            {
                DrawNames.Add("RotaType");
                DrawNames.Add("RotLerp");
                DrawNames.Add("OffsetRotY");
                DrawNames.Add("RotPriority");
                DrawNames.Add("TotalTime");
            }
            else
            {
                DrawNames.Add("RotaType");
                DrawNames.Add("RotLerp");
                DrawNames.Add("OffsetRotY");
                DrawNames.Add("RotPriority");
            }
            DrawEditorAttribute.Draw(_event, DrawNames);
        }

        private static void DrawSetAnimatorFloat(Event_SetAnimFloat _event)
        {
            DrawNames.Add("ValueType");
            if (_event.ValueType == Event_SetAnimFloat.EValueType._1D)
            {
                //1d参数绘制
                DrawNames.Add("AnimaFloatName");
                DrawNames.Add("AnimFloatFor");
                DrawNames.Add("AnimaFloatSpeed");
            }
            else
            {
                //2d参数绘制
                DrawNames.Add("AnimaFloatName");
                DrawNames.Add("AnimaFloatName2");
                DrawNames.Add("AnimFloatFor");
                DrawNames.Add("AnimaFloatSpeed");
            }
            DrawEditorAttribute.Draw(_event, DrawNames);

        }
        private static void DrawSetAnimatorFloatFromBluePrint(Event_SetAnimFloatFromBluePrint _event)
        {
            DrawNames.Add("ValueType");
            if (_event.ValueType == Event_SetAnimFloatFromBluePrint.EValueType._1D)
            {
                //1d参数绘制：float蓝图
                DrawNames.Add("AnimaFloatName");
                DrawNames.Add("FloatValue");
                DrawNames.Add("AnimaFloatSpeed");
            }
            else
            {
                //2d参数绘制：Vector3蓝图(仅使用x和y值)
                DrawNames.Add("AnimaFloatName");
                DrawNames.Add("AnimaFloatName2");
                DrawNames.Add("Vector3Value");
                DrawNames.Add("AnimaFloatSpeed");
            }
            DrawEditorAttribute.Draw(_event, DrawNames);
        }
        private static void DrawCameraChange(EditorActionEvent _actionEvent)
        {
            Event_CameraChange _eventCameraChange = (Event_CameraChange)_actionEvent.EventData;
            DrawNames.Add("OnlyPlayer");
            DrawNames.Add("OnHitter");
            if (!_eventCameraChange.OnHitter) DrawNames.Add("Ratio");
            // DrawEditorAttribute.Draw(_eventCameraChange, _eventNames);

            // _eventNames.Clear();
            DrawNames.Add("EnterCam");
            // _eventNames.Add("ResetPlayerCam");
            DrawNames.Add("ChangeCamPoint");

            DrawNames.Add("EnterPoint");
            DrawNames.Add("SwitchLookAt");
            if (_eventCameraChange.SwitchLookAt)
            {
                DrawNames.Add("GUnit");
                DrawNames.Add("TargetPoint");
            }

            DrawNames.Add("EnterTime");

            DrawNames.Add("ExitCam");

            // if (_actionEvent.Duration > 0)
            // {
            //     using (var _check = new EditorGUI.ChangeCheckScope())
            //     {
            //
            //         // _eventNames.Add("ExitCam");
            //         // _eventNames.Add("ExitCamPoint");
            //         // _eventNames.Add("CheckLable");
            //         if (_check.changed)
            //         {
            //             TimeLineWindow.Instance.UpdateTimeToNow();
            //             // SceneView.RepaintAll();
            //         }
            //     }
            //     // if (_eventCameraChange.CheckLable)
            //     // {
            //     //     _eventNames.Add("ActionLable");
            //     //     _eventNames.Add("IsContain");
            //     // }
            // }
            // else
            // {
            //     using (var _check = new EditorGUI.ChangeCheckScope())
            //     {
            //         _eventNames.Add("EnterCam");
            //         _eventNames.Add("EnterPoint");
            //         _eventNames.Add("EnterTime");
            //         // _eventNames.Add("CheckLable");
            //         if (_check.changed)
            //         {
            //             TimeLineWindow.Instance.UpdateTimeToNow();
            //         }
            //     }
            //
            //     // if (_eventCameraChange.CheckLable)
            //     // {
            //     //     _eventNames.Add("ActionLable");
            //     //     _eventNames.Add("IsContain");
            //     // }
            // }
            // _eventNames.Add("IsLockCam");
            DrawEditorAttribute.Draw(_eventCameraChange, DrawNames);

        }
        private static bool m_IsEditingParticleOffset;
        private static Event_PlayParticle m_EditingParticleEvent;
        private static bool DrawPartocleData(Event_PlayParticle _event, bool _init)
        {
            bool _isError = false;
            if (_init)
            {
                if (m_IsEditingParticleOffset)
                    EndParticleOffsetEdit();
            }

            using (var _check = new EditorGUI.ChangeCheckScope())
            {
                DrawNames.Add("PartoclePath");
                if (DrawEditorAttribute.Draw(_event, DrawNames))
                    _isError = true;
                if (_check.changed)
                {
                    if (string.IsNullOrEmpty(_event.PartoclePath))
                    {
                        _event.PartoclePath = String.Empty;
                        _isError = true;
                    }
                    else
                    {
                        EngineResourcesManager.Instance.LoaderObj_Editor(_event.PartoclePath, _obj =>
                        {
                            if (_obj is GameObject _loadObject)
                            {
                                if (!_loadObject.TryGetComponent(out ActionEngine_Effects _particleSystem))
                                {
                                    EditorUtility.DisplayDialog("警告", "当前对象最父级未挂载 ActionEditor_Effects", "我知道了");
                                    //_event.PartoclePath = String.Empty;
                                }
                            }
                        });
                    }
                }
            }
            DrawNames.Clear();

            DrawNames.Add("Interval");
            if (ActionWindowMain.IsSkillSetting)
            {
                DrawNames.Add("UseBluePrint_Scale");
                if (_event.UseBluePrint_Scale) DrawNames.Add("CLocalScale");
                else DrawNames.Add("LocalScale");

                if (_event.Life > 0)
                {
                    DrawNames.Add("Life");
                    DrawEditorAttribute.Draw(_event, DrawNames);
                }
                else
                {
                    DrawNames.Clear();
                    DrawNames.Add("UseBluePrint_Scale");
                    if (_event.UseBluePrint_Scale) DrawNames.Add("CLocalScale");
                    else DrawNames.Add("LocalScale");
                    DrawEditorAttribute.Draw(_event, DrawNames);
                }
                using (var _check = new EditorGUI.ChangeCheckScope())
                {
                    bool _toggle = EditorGUILayout.Toggle("离开时立刻销毁", _event.Life <= 0);
                    if (_check.changed) _event.Life = _toggle ? -1.0f : 1.0f;
                }

                return _isError;
            }
            if (!_event.IsUseBluePrintPoint)
                DrawNames.Add("PartPointType");
            DrawNames.Add("AlwaysFollow");

            if (_event.Life > 0) DrawNames.Add("Life");
            DrawEditorAttribute.Draw(_event, DrawNames);

            GUILayout.Space(10);

            _event.IsUseBluePrintPoint = EditorGUILayout.Toggle("使用蓝图设置位置", _event.IsUseBluePrintPoint);
            //DrawNames.Add("CLocalScale");

            if (_event.IsUseBluePrintPoint)
            {
                DrawNames.Clear();
                DrawNames.Add("UseBluePrint_Scale");
                if (_event.UseBluePrint_Scale) DrawNames.Add("CLocalScale");
                else DrawNames.Add("LocalScale");
                DrawNames.Add("ParticlePoint");
                DrawEditorAttribute.Draw(_event, DrawNames);
            }
            else
            {
                if (!m_IsEditingParticleOffset || m_EditingParticleEvent != _event)
                {
                    if (GUILayout.Button("编辑坐标偏移"))
                    {
                        Transform testPoint = GetParticleAttachPoint(_event);
                        if (testPoint != null)
                            BeginParticleOffsetEdit(_event);
                        else
                            EditorUtility.DisplayDialog("警告", "无法获取挂点，请检查 CharacterConfig 配置", "确定");
                    }
                }
                else
                {
                    using (new GUIColorScope(Color.green))
                    {
                        if (GUILayout.Button("完成编辑"))
                        {
                            EndParticleOffsetEdit();
                        }
                    }
                    EditorGUILayout.HelpBox("在场景视图中拖拽坐标轴调整偏移\n按 W 切换位移，按 E 切换旋转", MessageType.Info);
                }

                using (var _check = new EditorGUI.ChangeCheckScope())
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("位置偏移: ", GUILayout.Width(60));
                        _event.OffsetPos = new EVector3(EditorGUILayout.Vector3Field("", _event.OffsetPos.GetValue()));
                    }
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("角度偏移: ", GUILayout.Width(60));
                        _event.OffsetRot = new EVector3(EditorGUILayout.Vector3Field("", _event.OffsetRot.GetValue()));
                    }
                    if (_check.changed)
                    {
                        TimeLineWindow.Instance.UpdateTimeToNow();
                    }
                }

                DrawNames.Clear();
                DrawNames.Add("UseBluePrint_Scale");
                if (_event.UseBluePrint_Scale) DrawNames.Add("CLocalScale");
                else DrawNames.Add("LocalScale");
                DrawEditorAttribute.Draw(_event, DrawNames);
            }

            using (var _check = new EditorGUI.ChangeCheckScope())
            {
                bool _toggle = EditorGUILayout.Toggle("离开时立刻销毁", _event.Life <= 0);
                if (_check.changed) _event.Life = _toggle ? -1.0f : 1.0f;
            }
            return _isError;
        }

        #region Particle Offset Virtual Handles

        private static Transform GetParticleAttachPoint(Event_PlayParticle evt)
        {
            if (ActionWindowMain.IsSkillSetting)
                return ResourcesWindow.Instance.SkillPre?.transform;

            if (ResourcesWindow.Instance.TryGetCharacterConfig(out CharacterConfig config) &&
                config.HelpPointDic.TryGetValue((ECharacteLimbType)evt.PartPointType, out Transform point))
                return point;

            return ResourcesWindow.Instance.GetUnit()?.transform;
        }

        private static void BeginParticleOffsetEdit(Event_PlayParticle evt)
        {
            if (m_IsEditingParticleOffset)
                EndParticleOffsetEdit();

            m_IsEditingParticleOffset = true;
            m_EditingParticleEvent = evt;
            SceneView.duringSceneGui += OnParticleOffsetSceneGUI;
            SceneView.RepaintAll();
        }

        private static void EndParticleOffsetEdit()
        {
            m_IsEditingParticleOffset = false;
            m_EditingParticleEvent = null;
            SceneView.duringSceneGui -= OnParticleOffsetSceneGUI;
            SceneView.RepaintAll();
        }

        private static void OnParticleOffsetSceneGUI(SceneView sceneView)
        {
            if (!m_IsEditingParticleOffset || m_EditingParticleEvent == null)
            {
                EndParticleOffsetEdit();
                return;
            }

            if (!EditorEventUpdate.m_ObjectPool.ContainsKey(m_EditingParticleEvent))
            {
                EndParticleOffsetEdit();
                return;
            }

            Transform attachPoint = GetParticleAttachPoint(m_EditingParticleEvent);
            if (attachPoint == null) return;

            Vector3 worldPos = attachPoint.TransformPoint(m_EditingParticleEvent.OffsetPos.GetValue());
            Quaternion worldRot = attachPoint.rotation * Quaternion.Euler(m_EditingParticleEvent.OffsetRot.GetValue());

            EditorGUI.BeginChangeCheck();

            if (Tools.current == Tool.Rotate)
                worldRot = Handles.RotationHandle(worldRot, worldPos);
            else
                worldPos = Handles.PositionHandle(worldPos, worldRot);

            if (EditorGUI.EndChangeCheck())
            {
                m_EditingParticleEvent.OffsetPos = new EVector3(attachPoint.InverseTransformPoint(worldPos));
                Quaternion localRot = Quaternion.Inverse(attachPoint.rotation) * worldRot;
                m_EditingParticleEvent.OffsetRot = new EVector3(localRot.eulerAngles);

                TimeLineWindow.Instance.UpdateTimeToNow();
            }
        }

        #endregion

        private static readonly string[] EnumNames = new[] { "头;部位1", "身;部位2", "手;部位3", "脚;部位4", "披风;部位5", "头发;部位6", "脸;部位7", "主武器;部位8", "副武器;部位9" };
        private static void DrawRimLight(IActionEventData _event)
        {
            var go = ResourcesWindow.Instance.GetUnit();
            var player = go.name.ToLower().Contains("player");
            var display = new string[EnumNames.Length];
            for (var index = 0; index < EnumNames.Length; index++)
            {
                var enumName = EnumNames[index];
                var info = enumName.Split(';');
                if (info.Length > 1)
                {
                    display[index] = player ? info[0] : info[1];
                }
                else
                {
                    display[index] = enumName;
                }
            }
            if (_event is Event_Dissolve _edl)
                _edl.EquipMask = EditorGUILayout.MaskField(_edl.EquipMask, display);
            else if (_event is Event_RimLight _erl)
                _erl.EquipMask = EditorGUILayout.MaskField(_erl.EquipMask, display);
            DrawEditorAttribute.Draw(_event);
            if (GUILayout.Button("跳转到资源"))
            {
                var globalAssets = AssetDatabase.FindAssets("pre_cfg_global_assets");
                if (globalAssets.Length < 1)
                {
                    Debug.LogError("未找到全局资源");
                    return;
                }

                foreach (var asset in globalAssets)
                {
                    var assets = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(asset));
                    if (assets && assets.TryGetComponent(out GlobalAssetsComponent _))
                    {
                        Selection.activeObject = assets;
                        EditorGUIUtility.PingObject(assets);
                        break;
                    }
                }
            }
        }

        #endregion


    }
}