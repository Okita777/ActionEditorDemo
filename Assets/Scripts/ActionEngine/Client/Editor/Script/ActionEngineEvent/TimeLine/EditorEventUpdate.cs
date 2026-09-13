using AsiActionEditor_Ex.RunTime;
using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
#if FMOD
using FMOD.Studio;
using FMODUnity;
#endif
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MotionEngineConst = AsiActionEngine.RunTime.MotionEngineConst;
using Object = UnityEngine.Object;

namespace AsiTimeLine.Editor
{
    public class EditorEventUpdate
    {
        public static Dictionary<object, Object> m_ObjectPool = new Dictionary<object, Object>();
        private static GameObject m_ActionPool = null;
        private static int m_LastTime => TimeLineWindow.Instance.mLastTimeLineTime;
        public static void OnInit()
        {
            mEventWeaponPointTime = int.MinValue;
            mEventCameraChangeTime = int.MinValue;
        }//并不是真的初始化  会和事件一起每帧执行  只是在每个事件执行前执行

        public static void OnChangeAction()
        {
#if FMOD
            //FMODUnity.EditorUtils.StopAllPreviews();
            foreach (var item in dicEventInstance)
            {
                EventInstance eventInstance = item.Value;
                if (eventInstance.isValid())
                {
                    FMODUnity.EditorUtils.PreviewStop(eventInstance);
                }
            }
            dicEventInstance.Clear();
#endif
            ClearPool();
            //Debug.Log("ChangeActionAA");
            mParticleDis = false;
        }//切换Action时触发一次

        //_time是毫秒为单位
        public static bool OnUpdate(int _time, EditorActionEvent _actionEvent)
        {
            IActionEventData _eventData = _actionEvent.EventData;
            bool returnValue = true;
            if (_eventData is Event_Attach _eventWeaponPoint)
            {
                Update_EventWeaponPoint(_time, _eventWeaponPoint, _actionEvent);
            }//武器挂点切换
            else if (_eventData is Event_CameraChange _eventCameraChange)
            {
                Update_CameraChange(_time, _eventCameraChange, _actionEvent);
            }
            else if (_eventData is Event_PlayParticle _epp)
            {
                Update_PlayParticle(_time, _actionEvent, _epp);
            }//特效播放
            else if (_eventData is Event_SkillEntity _ese)
            {
                Update_Entity(_time, _actionEvent, _ese);
            }//技能实例加载
            else if (_eventData is Event_TimeScale _ets)
            {
                Update_TimeScale(_time, _actionEvent, _ets);
            }
            // else if (_eventData is Event_RaycastHit _erh)
            // {
            //     Update_TimeScale(_time, _actionEvent, _erh);
            // }
            else if (_eventData is Event_PlayAudio _epa)
            {
                Update_PlayAudio(_time, _actionEvent, _epa);
            }
            else if (_eventData is Event_RimLight _erl)
            {
                Update_RimLight(_time, _actionEvent, _erl);
            }
            else if (_eventData is Event_Dissolve _ed)
            {
                Update_Dissolve(_time, _actionEvent, _ed);
            }
            else if (_eventData is Event_MotionBlur _emb)
            {
                Update_MotionBlur(_time, _actionEvent, _emb);
            }
            else if (_eventData is Event_Light _el)
            {
                Update_Light(_time, _actionEvent, _el);
            }
            else if (_eventData is Event_SkillLight _esl)
            {
                Update_SkillLight(_time, _actionEvent, _esl);
            }
            else if (_eventData is Event_AfterImageEntity _eaie)
            {
                Update_AfterImageEntity(_time, _actionEvent, _eaie);
            }
            else if (_eventData is Event_CreateSkill _ecs)
            {
                Update_CreateSkill(_time, _actionEvent, _ecs);
            }
            else
            {
                returnValue = false;
            }
            return returnValue;
        }

        #region InitEditorFuntion
        private static Transform CreateActionPool(string _poolName)
        {
            if (m_ActionPool == null)
            {
                m_ActionPool = GameObject.Find("ActionEnginePool");
                if (m_ActionPool is not null)
                {
                    Object.DestroyImmediate(m_ActionPool);
                }

                m_ActionPool = new GameObject();
                if (!Application.isPlaying)
                    m_ActionPool.AddComponent<GameObjectDestory>();
                m_ActionPool.name = "ActionEnginePool";
                // Debug.Log("更新");
            }

            Transform _PPtransform = m_ActionPool.transform.Find(_poolName);
            if (_PPtransform is null)
            {
                _PPtransform = new GameObject().transform;
                _PPtransform.name = _poolName;
                _PPtransform.SetParent(m_ActionPool.transform);
            }
            return _PPtransform;
        }

        private static void ClearPool()
        {
            if (m_ActionPool is not null)
            {
                Object.DestroyImmediate(m_ActionPool);
            }
            m_ObjectPool.Clear();
            s_AfterImageElapsed.Clear();
        }
        #endregion

        #region UpdateEditorFuntion
#if FMOD
        private static Dictionary<string, float> paramValues = new Dictionary<string, float>();
        private static Dictionary<int, EventInstance> dicEventInstance = new Dictionary<int, EventInstance>();
        public static void PlayFModAudio(Event_PlayAudio _event)
        {
            EditorUtils.LoadPreviewBanks();

            if (dicEventInstance.TryGetValue(_event.GetHashCode(), out EventInstance _EventInstance))
            {
                FMODUnity.EditorUtils.PreviewStop(_EventInstance);
            }

            EditorEventRef editorEvent = EventManager.EventFromPath(_event.m_EventReference.Path);
            if (editorEvent == null)
            {
                EngineDebug.LogError("音频加载错误: " + _event.m_EventReference.Path);
                return;
            }

            EventInstance _newEventInstance = FMODUnity.EditorUtils.PreviewEvent(editorEvent, paramValues, 1f);
            if (!dicEventInstance.TryAdd(_event.GetHashCode(), _newEventInstance))
            {
                dicEventInstance[_event.GetHashCode()] = _newEventInstance;
            }
        }
        public static void StopFModAudio(Event_PlayAudio _event)
        {
            if (dicEventInstance.TryGetValue(_event.GetHashCode(), out EventInstance _EventInstance))
            {
                if (_EventInstance.isValid())
                    FMODUnity.EditorUtils.PreviewStop(_EventInstance);
            }
            dicEventInstance.Remove(_event.GetHashCode());
        }
#endif

        private static void Update_PlayAudio(int _time, EditorActionEvent _actionEvent, Event_PlayAudio _epp)
        {
            if (_actionEvent.TriggerTime >= m_LastTime && _actionEvent.TriggerTime < _time)
            {
#if FMOD
                PlayFModAudio(_epp);
                //EngineDebug.Log("播放");
#else
                //if (ResourcesWindow.Instance.GetRole().TryGetComponent(out ActionEngine_Audio audio))
                var audioUnit = ResourcesWindow.Instance.GetUnit();
                if (audioUnit && audioUnit.TryGetComponent(out ActionEngine_Audio audio))
                {
                    if (_epp.m_CoustomAudio)
                    {
                        audio.PlayAudio(_epp.m_AudioVolume, _epp.m_AudioSourceIndex, _epp.m_AudioDicID,
                            _epp.m_AudioDicChailID);
                    }
                    else
                    {
                        audio.PlayAudio(_epp.m_AudioVolume, _epp.m_AudioSourceIndex, _epp.m_AudioDicID);
                        EngineDebug.LogWarning("无法在Editor预览时正确播放字典中的音效");
                    }
                }

#endif
            }
        }
        private static void Update_TimeScale(int _time, EditorActionEvent _actionEvent, Event_TimeScale _epp)
        {
            if (_time > _actionEvent.TriggerTime)
            {
                if (_time < _actionEvent.TriggerTime + _actionEvent.Duration)
                {
                    ActionStatePart part = ResourcesWindow.Instance.ActionStatePart;
                    TimeLineWindow.Instance.TimeScale = _epp.TimeScale.GetValue(part);
                }
                else
                {
                    TimeLineWindow.Instance.TimeScale = 1;
                }
            }
        }

        private static bool mParticleDis = false;
        private static void Update_PlayParticle(int _time, EditorActionEvent _actionEvent, Event_PlayParticle _epp)
        {
            if (m_ObjectPool.TryGetValue(_epp, out Object _obj))
            {
                ActionStatePart _part = null;
                if (ActionWindowMain.IsSkillSetting) _part = ResourcesWindow.Instance.SkillActionStatePart;
                else _part = ResourcesWindow.Instance.ActionStatePart;

                if (_obj is ActionEngine_Effects _particleSystem)
                {
                    Transform _target = null;
                    if (ActionWindowMain.IsSkillSetting)
                    {
                        _target = ResourcesWindow.Instance.SkillPre.transform;
                    }
                    else
                    {
                        if (ResourcesWindow.Instance.TryGetCharacterConfig(out CharacterConfig _config))
                        {
                            if (_config.HelpPointDic.TryGetValue((ECharacteLimbType)_epp.PartPointType, out Transform _point))
                            {
                                _target = _point;
                            }
                        }
                    }

                    Vector3 _pos = _target.TransformPoint(_epp.OffsetPos.GetValue());
                    Quaternion _rot = _target.rotation * Quaternion.Euler(_epp.OffsetRot.GetValue());
                    _particleSystem.transform.SetPositionAndRotation(_pos, _rot);

                    if (_epp.UseBluePrint_Scale)
                    {
                        float _scale = _epp.CLocalScale.value(_part, EngineResourcesManager.Instance.MachineTime);
                        _particleSystem.transform.localScale = Vector3.one * _scale;
                    }
                    else
                    {
                        _particleSystem.transform.localScale = _epp.LocalScale.GetValue();
                    }

                    //_particleSystem.transform.localScale = _epp.LocalScale.GetValue();
                    ParicleSystem_Update(_particleSystem, _time, _actionEvent.TriggerTime);
                    //_particleSystem.Simulate(Mathf.Max(0, _nowTime),true,true,true); //更新粒子效果
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(_epp.PartoclePath))
                {
                    Transform _partPool = CreateActionPool("ActionEditor_Effects");
                    EngineResourcesManager.Instance.LoaderObj_Editor(_epp.PartoclePath, _obj =>
                    {
                        if (_obj is GameObject _gameObj && _gameObj.TryGetComponent(out ActionEngine_Effects _loadObject))
                        {
                            if (_loadObject == null)
                            {
                                //_epp.PartoclePath = String.Empty;
                                if (!mParticleDis) EditorUtility.DisplayDialog("警告", "当前对象最父级未挂载 ActionEditor_Effects2", "我知道了");
                                mParticleDis = true;
                                return;
                            }
                            ParicleSystem_Init(_loadObject);
                            ActionEngine_Effects _new = Object.Instantiate(_loadObject, _partPool);
                            ParicleSystem_Init(_new, true);
                            m_ObjectPool.Add(_epp, _new);
                        }
                        else
                        {
                            if (!mParticleDis) EditorUtility.DisplayDialog("警告", "当前对象最父级未挂载 ActionEditor_Effects1", "我知道了");
                            mParticleDis = true;
                            //_epp.PartoclePath = string.Empty;
                        }
                    });
                    // ActionEditor_Effects _loadObject = ActionEnginLoadData.Instance.EditorLoadObject<ActionEditor_Effects>(_epp.PartoclePath);
                }
            }
        }

        private static void ParicleSystem_Init(ActionEngine_Effects _loadObject, bool _isNew = false)
        {
            if (_isNew)
            {
                if (!Application.isPlaying)
                {
                    _loadObject.useAutoRandomSeed = false;
                }

                foreach (ParticleSystem _particleSystem2 in _loadObject.particleSystems)
                {
                    _particleSystem2.Stop();
                }
            }
            else
            {
                bool _checkError = false;
                for (int i = 0; i < _loadObject.particleSystems.Count; i++)
                {
                    if (_loadObject.particleSystems[i] == null)
                    {
                        _checkError = true;
                        _loadObject.particleSystems.RemoveAt(i);
                        i--;
                    }
                }
                if (_checkError)
                {
                    EngineDebug.LogWarning($"在 [<color=#ffcc00>{_loadObject.transform.name}</color>] 下发现空特效配置，已自动修复");
                    EditorUtility.SetDirty(_loadObject);
                }
            }
        }

        private static void ParicleSystem_Update(ActionEngine_Effects _Effects, int _time, int _trigerTime)
        {
            float _nowTime = (_time - _trigerTime) / (float)MotionEngineConst.TimeDoubling;
            foreach (ParticleSystem VARIABLE in _Effects.particleSystems)
            {
                VARIABLE.Simulate(Mathf.Max(0, _nowTime), true, true, true); //更新粒子效果
            }
        }

        private static void Update_RimLight(int _time, EditorActionEvent _actionEvent, Event_RimLight _erl)
        {
            var go = ResourcesWindow.Instance.GetUnit();
            if (!go || !go.TryGetComponent(out UnitEquip unitEquip))
            {
                return;
            }

            var globalAssets = DrawEditorAttribute.GetGlobalAssets();
            if (!globalAssets)
            {
                return;
            }

            var equipRenderers = go.GetComponentsInChildren<Renderer>();

            if (_time < _actionEvent.TriggerTime)
            {
                foreach (var equipRenderer in equipRenderers)
                {
                    foreach (var material in equipRenderer.sharedMaterials)
                    {
                        if (material)
                        {
                            material.DisableKeyword(Event_RimLight.cRimLightKeyword);
                        }
                    }
                    equipRenderer.SetPropertyBlock(null);
                }
                return;
            }

            if (_time < _actionEvent.TriggerTime + _actionEvent.Duration)
            {
                var pct = (_time - _actionEvent.TriggerTime) / (float)_actionEvent.Duration;

                var gradientMap = globalAssets.gradientsMap;
                var curveMap = globalAssets.curvesMap;
                var equipMask = _erl.EquipMask;
                var types = Enum.GetValues(typeof(EquipType));
                foreach (EquipType type in types)
                {
                    if ((equipMask & (int)type) == 0)
                    {
                        continue;
                    }

                    if (!unitEquip.GetMeshByType(type, out var renderer))
                    {
                        continue;
                    }

                    var mbp = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(mbp);

                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material)
                        {
                            material.EnableKeyword(Event_RimLight.cRimLightKeyword);
                        }
                    }
                    if (gradientMap.TryGetValue(_erl.Gradient, out var gradient))
                    {
                        mbp.SetColor(Event_RimLight.cColorPropertyName, gradient.Evaluate(pct));
                    }
                    if (curveMap.TryGetValue(_erl.PowerCurve, out var powerCurve))
                    {
                        mbp.SetFloat(Event_RimLight.cPowerPropertyName, powerCurve.Evaluate(pct));
                    }
                    if (curveMap.TryGetValue(_erl.IntensityCurve, out var intensityCurve))
                    {
                        mbp.SetFloat(Event_RimLight.cIntensityPropertyName, intensityCurve.Evaluate(pct));
                    }

                    renderer.SetPropertyBlock(mbp);
                }
            }
            else
            {
                foreach (var equipRenderer in equipRenderers)
                {
                    foreach (var material in equipRenderer.sharedMaterials)
                    {
                        if (material)
                        {
                            material.DisableKeyword(Event_RimLight.cRimLightKeyword);
                        }
                    }
                    equipRenderer.SetPropertyBlock(null);
                }
            }

            // if(role.TryGetComponent(out))
            // if (.TryGetComponent(out ActionEngine_Audio audio))
            // {
            //     if (_epp.m_CoustomAudio)
            //     {
            //         audio.PlayAudio(_epp.m_AudioVolume, _epp.m_AudioSourceIndex, _epp.m_AudioDicID,
            //             _epp.m_AudioDicChailID);
            //     }
            //     else
            //     {
            //         audio.PlayAudio(_epp.m_AudioVolume, _epp.m_AudioSourceIndex, _epp.m_AudioDicID);
            //         EngineDebug.LogWarning("无法在Editor预览时正确播放字典中的音效");
            //     }
            // }
        }

        private static void Update_Dissolve(int _time, EditorActionEvent _actionEvent, Event_Dissolve _event)
        {
            var go = ResourcesWindow.Instance.GetUnit();
            if (!go || !go.TryGetComponent(out UnitEquip unitEquip))
            {
                return;
            }

            var globalAssets = DrawEditorAttribute.GetGlobalAssets();
            if (!globalAssets)
            {
                return;
            }

            var equipRenderers = go.GetComponentsInChildren<Renderer>();

            if (_time < _actionEvent.TriggerTime)
            {
                foreach (var equipRenderer in equipRenderers)
                {
                    foreach (var material in equipRenderer.sharedMaterials)
                    {
                        if (material)
                        {
                            material.DisableKeyword(Event_Dissolve.cDissolveKeyword);
                        }
                    }
                    equipRenderer.SetPropertyBlock(null);
                }

                return;
            }

            if (_time < _actionEvent.TriggerTime + _actionEvent.Duration)
            {
                var pct = (_time - _actionEvent.TriggerTime) / (float)_actionEvent.Duration;

                var gradientMap = globalAssets.gradientsMap;
                var curveMap = globalAssets.curvesMap;
                var equipMask = _event.EquipMask;
                var types = Enum.GetValues(typeof(EquipType));
                foreach (EquipType type in types)
                {
                    if ((equipMask & (int)type) == 0)
                    {
                        continue;
                    }

                    if (!unitEquip.GetMeshByType(type, out var renderer))
                    {
                        continue;
                    }

                    var mbp = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(mbp);

                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material)
                        {
                            material.EnableKeyword(Event_Dissolve.cDissolveKeyword);
                        }
                    }
                    if (gradientMap.TryGetValue(_event.Gradient, out var gradient))
                    {
                        mbp.SetColor(Event_Dissolve.cColorPropertyName, gradient.Evaluate(pct));
                    }
                    if (curveMap.TryGetValue(_event.Threshold, out var thresholdCurve))
                    {
                        mbp.SetFloat(Event_Dissolve.cThresholdPropertyName, thresholdCurve.Evaluate(pct));
                    }
                    if (curveMap.TryGetValue(_event.EdgeCurve, out var edgeCurve))
                    {
                        mbp.SetFloat(Event_Dissolve.cEdgePropertyName, edgeCurve.Evaluate(pct));
                    }
                    if (curveMap.TryGetValue(_event.IntensityCurve, out var intensityCurve))
                    {
                        mbp.SetFloat(Event_Dissolve.cIntensityPropertyName, intensityCurve.Evaluate(pct));
                    }

                    if (!string.IsNullOrEmpty(_event.DissolveTexture))
                    {
                        mbp.SetTexture(Event_Dissolve.cTexturePropertyName, AssetDatabase.LoadAssetAtPath<Texture>(_event.DissolveTexture));
                    }

                    renderer.SetPropertyBlock(mbp);
                }
            }
            else
            {
                foreach (var equipRenderer in equipRenderers)
                {
                    foreach (var material in equipRenderer.sharedMaterials)
                    {
                        if (material)
                        {
                            material.DisableKeyword(Event_Dissolve.cDissolveKeyword);
                        }
                    }
                    equipRenderer.SetPropertyBlock(null);
                }
            }
        }

        private static void Update_Entity(int _time, EditorActionEvent _actionEvent, Event_SkillEntity _event)
        {
            if (m_ObjectPool.TryGetValue(_event, out Object _obj))
            {
                if (_obj is GameObject _particleSystem)
                {
                    bool _active = (_time >= _actionEvent.TriggerTime);
                    if (_active)
                    {
                        if (_actionEvent.Duration < 0)
                        {
                            _active = true;
                        }
                        else if (_actionEvent.Duration != 0)
                        {
                            _active = (_time <= _actionEvent.TriggerTime + _actionEvent.Duration);
                        }
                        else
                        {
                            _active = false;
                        }
                    }

                    if (_particleSystem.activeSelf != _active)
                    {
                        _particleSystem.SetActive(_active);
                    }
                    if (_particleSystem.activeSelf)
                    {
                        Transform _target = ResourcesWindow.Instance.SkillPre.transform;
                        _particleSystem.transform.SetPositionAndRotation(_target.position, _target.rotation);

                        ActionStatePart _part = ActionWindowMain.IsSkillSetting
                            ? ResourcesWindow.Instance.SkillActionStatePart
                            : ResourcesWindow.Instance.ActionStatePart;
                        if (_part != null)
                        {
                            _particleSystem.transform.localScale = _event.LocalScale.value(_part, EngineResourcesManager.Instance.MachineTime);
                        }
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(_event.SkillEntity))
                {
                    Transform _partPool = CreateActionPool("ActionEditor_Effects");
                    EngineResourcesManager.Instance.LoaderObj_Editor(_event.SkillEntity, _obj =>
                    {
                        if (_obj is GameObject _gameObj)
                        {
                            GameObject _new = Object.Instantiate(_gameObj);
                            _new.transform.SetParent(_partPool);
                            m_ObjectPool.Add(_event, _new);
                        }
                    });
                }
            }
        }
        private static int mEventCameraChangeTime;
        private static CharacterConfig _characterConfig;
        private static void Update_CameraChange(int _time,
            Event_CameraChange _eventPlayAnim, EditorActionEvent _actionEvent)
        {
            if (_time >= _actionEvent.TriggerTime)
            {
                if (_actionEvent.TriggerTime > mEventCameraChangeTime)
                {
                    mEventCameraChangeTime = _actionEvent.TriggerTime;
                }
                else
                {
                    return;
                }

                if (ResourcesWindow.Instance.PreCamControl != null)
                {
                    CameraControl _cameraControl = ResourcesWindow.Instance.PreCamControl;
                    ResourcesWindow.Instance.TryGetCharacterConfig(out CharacterConfig _characterConfig);

                    if (_characterConfig.HelpPointDic.TryGetValue((ECharacteLimbType)_eventPlayAnim.EnterPoint,
                            out Transform _camPoint))
                    {
                        if (_eventPlayAnim.OnHitter)
                        {
                            _cameraControl.ChangeCam(_eventPlayAnim.EnterCam);
                        }
                        else
                        {
                            _cameraControl.ChangeCam(_eventPlayAnim.EnterCam, _camPoint);
                        }
                    }
                    else
                    {
                        EngineDebug.LogWarning($"相机切换失败, 未配置 [{(ECharacteLimbType)_eventPlayAnim.EnterPoint}]");
                    }
                }
                // else
                // {
                //     EngineDebug.LogError(
                //         "Event_CameraChange 事件未执行!!\n" +
                //         "<color=#FF0000>未创建相机预览</color> !!"
                //     );
                // }
            }
        }

        private static int mEventWeaponPointTime;
        private static void Update_EventWeaponPoint(int _time,
            Event_Attach _eventPlayAnim, EditorActionEvent _actionEvent)
        {
            if (_time > _actionEvent.TriggerTime)
            {
                if (_actionEvent.TriggerTime > mEventWeaponPointTime)
                {
                    mEventWeaponPointTime = _actionEvent.TriggerTime;
                }
                else
                {
                    return;
                }
                if (ResourcesWindow.Instance.TryGetCharacterConfig(out CharacterConfig _config))
                {
                    // Transform _refer = _eventPlayAnim.ReferTransform.Get();
                    // Transform _target = _eventPlayAnim.TargetTransform.Get();
                    Transform _refer = null;
                    Transform _target = null;
                    if (!_config.HelpPointDic.TryGetValue((ECharacteLimbType)_eventPlayAnim.ReferTransform.m_Value, out _refer))
                    {
                        EngineDebug.LogError("附加对象时，缺失目标");
                        return;
                    }

                    if (!_config.HelpPointDic.TryGetValue((ECharacteLimbType)_eventPlayAnim.TargetTransform.m_Value, out _target))
                    {
                        EngineDebug.LogError("附加对象时，缺失附加目标");
                        return;
                    }
                    _refer.SetParent(_target);
                    // if (_eventPlayAnim.alignToTarget)
                    {
                        _refer.position = _target.position;
                        _refer.rotation = _target.rotation;
                    }

                    // Transform _weapon = _eventPlayAnim.IsRightWeapon ? _config.WeaponR : _config.WeaponL;
                    // if (_weapon is null)
                    // {
                    //     EngineDebug.LogError(
                    //         "Event_WeaponPointChange 事件未执行!!\n" +
                    //         $"<color=#FF0000>CharacterConfig</color> 的" +
                    //         $"{(_eventPlayAnim.IsRightWeapon ? "右": "左")}手武器配置为空!!"
                    //     );
                    //     return;
                    // }
                    //
                    // ECharacteLimbType _characteLimb = (ECharacteLimbType)_eventPlayAnim.LimbPointType;
                    // if (_config.HelpPointDic.TryGetValue(_characteLimb, out var _target))
                    // {
                    //     _weapon.parent = _target;
                    //     if (_eventPlayAnim.AlignToPoint)
                    //     {
                    //         _weapon.localPosition = Vector3.zero;
                    //         _weapon.rotation = _target.rotation;
                    //     }
                    // }
                }
                else
                {
                    EngineDebug.LogError(
                        "Event_WeaponPointChange 事件未执行!!\n" +
                        "未挂载 <color=#FF0000>CharacterConfig</color> 组件!!"
                    );
                }
            }
        }


        private static void Update_MotionBlur(int _time, EditorActionEvent _actionEvent, Event_MotionBlur _event)
        {
            var go = ResourcesWindow.Instance.GetUnit();
            if (!go || !go.TryGetComponent(out UnitEquip unitEquip))
            {
                return;
            }

            bool inRange = _time >= _actionEvent.TriggerTime
                           && _time < _actionEvent.TriggerTime + _actionEvent.Duration;

            var mode = inRange
                ? MotionVectorGenerationMode.Object
                : MotionVectorGenerationMode.ForceNoMotion;

            var types = (EquipType[])Enum.GetValues(typeof(EquipType));
            foreach (var type in types)
            {
                if (!unitEquip.GetMeshByType(type, out var renderer) || !renderer)
                    continue;

                renderer.motionVectorGenerationMode = mode;

                if (renderer is SkinnedMeshRenderer smr)
                {
                    smr.skinnedMotionVectors = inRange;
                }
            }
        }

        private static void Update_Light(int _time, EditorActionEvent _actionEvent, Event_Light _event)
        {
            if (_event.Lights == null || _event.Lights.Count == 0 || string.IsNullOrEmpty(_event.PrefabPath))
            {
                return;
            }

            bool inRange = _time >= _actionEvent.TriggerTime
                           && _time < _actionEvent.TriggerTime + _actionEvent.Duration;
            float pct = _actionEvent.Duration > 0
                ? (_time - _actionEvent.TriggerTime) / (float)_actionEvent.Duration
                : 0f;
            var globalAssets = DrawEditorAttribute.GetGlobalAssets();
            var unit = ResourcesWindow.Instance.GetUnit();
            Transform rootTransform = unit ? unit.transform : null;
            ResourcesWindow.Instance.TryGetCharacterConfig(out CharacterConfig config);

            for (int i = 0; i < _event.Lights.Count; i++)
            {
                var data = _event.Lights[i];
                if (data == null) continue;

                Transform attachPoint = rootTransform;
                if (config != null
                    && config.HelpPointDic.TryGetValue((ECharacteLimbType)data.PartPointType, out var point)
                    && point)
                {
                    attachPoint = point;
                }

                UpdateOneLightSlot(_event.PrefabPath, data, inRange, pct, attachPoint, globalAssets,
                    useTransformPoint: true, poolTag: "ActionEditor_Light");
            }
        }

        private static void Update_SkillLight(int _time, EditorActionEvent _actionEvent, Event_SkillLight _event)
        {
            if (_event.Lights == null || _event.Lights.Count == 0 || string.IsNullOrEmpty(_event.PrefabPath))
            {
                return;
            }

            bool inRange = _time >= _actionEvent.TriggerTime
                           && _time < _actionEvent.TriggerTime + _actionEvent.Duration;
            float pct = _actionEvent.Duration > 0
                ? (_time - _actionEvent.TriggerTime) / (float)_actionEvent.Duration
                : 0f;
            var globalAssets = DrawEditorAttribute.GetGlobalAssets();
            var unit = ResourcesWindow.Instance.GetUnit();
            // 编辑器预览无 actionState，以角色根节点 position/rotation 模拟 actionState.Pos/Rot
            Transform attachPoint = unit ? unit.transform : null;

            for (int i = 0; i < _event.Lights.Count; i++)
            {
                var data = _event.Lights[i];
                if (data == null) continue;

                UpdateOneLightSlot(_event.PrefabPath, data, inRange, pct, attachPoint, globalAssets,
                    useTransformPoint: false, poolTag: "ActionEditor_SkillLight");
            }
        }

        // data 仅用作 m_ObjectPool 的 key，内部通过反射式属性读取，因此 LightData 和 SkillLightData 都可以传入
        private static void UpdateOneLightSlot(string prefabPath, object data, bool inRange, float pct,
            Transform attachPoint, GlobalAssetsComponent globalAssets, bool useTransformPoint, string poolTag)
        {
            var view = LightDataView.From(data);
            if (view == null) return;

            if (m_ObjectPool.TryGetValue(data, out Object obj))
            {
                if (obj is GameObject instance && instance)
                {
                    if (instance.activeSelf != inRange)
                    {
                        instance.SetActive(inRange);
                    }

                    if (!inRange) return;

                    if (attachPoint)
                    {
                        var offset = view.OffsetPos.GetValue();
                        var worldPos = useTransformPoint
                            ? attachPoint.TransformPoint(offset)
                            : attachPoint.position + attachPoint.rotation * offset;
                        instance.transform.SetPositionAndRotation(worldPos, attachPoint.rotation);
                    }

                    if (view.PositionMode == ELightPositionMode.GroundSnap && view.GroundLayer != 0)
                    {
                        var pos = instance.transform.position;
                        var origin = new Vector3(pos.x, pos.y + 50f, pos.z);
                        if (Physics.Raycast(origin, Vector3.down, out var hit, 200f, view.GroundLayer, QueryTriggerInteraction.Ignore))
                        {
                            instance.transform.position = new Vector3(pos.x, hit.point.y + view.GroundYOffset, pos.z);
                        }
                    }

                    var light = instance.GetComponent<Light>();
                    if (light)
                    {
                        light.renderingLayerMask = view.RenderingLayerMask;
                        if (globalAssets)
                        {
                            if (!string.IsNullOrEmpty(view.Color)
                                && globalAssets.colorsMap.TryGetValue(view.Color, out var color))
                            {
                                light.color = color;
                            }
                            if (!string.IsNullOrEmpty(view.RangeCurve)
                                && globalAssets.curvesMap.TryGetValue(view.RangeCurve, out var rangeCurve))
                            {
                                light.range = rangeCurve.Evaluate(pct) * view.RangeMultiplier;
                            }
                            if (!string.IsNullOrEmpty(view.IntensityCurve)
                                && globalAssets.curvesMap.TryGetValue(view.IntensityCurve, out var intensityCurve))
                            {
                                light.intensity = intensityCurve.Evaluate(pct) * view.IntensityMultiplier;
                            }
                        }
                    }
                }
            }
            else
            {
                // 先占位，避免异步加载期间下一帧重复 Load / Add 冲突
                m_ObjectPool[data] = null;
                Transform partPool = CreateActionPool(poolTag);
                EngineResourcesManager.Instance.LoaderObj_Editor(prefabPath, loaded =>
                {
                    if (loaded is GameObject prefab)
                    {
                        GameObject newInstance = Object.Instantiate(prefab, partPool);
                        newInstance.SetActive(false);
                        m_ObjectPool[data] = newInstance;
                    }
                });
            }
        }

        // 幻影实体编辑器下可复制的装备部位（武器依赖运行时 ActionEngine_Entity.mPropDic_Slot，编辑器通常无数据，预览暂不处理）
        private static readonly EquipType[] s_AfterImageEquipTypes =
        {
            EquipType.Head, EquipType.Body, EquipType.Hand, EquipType.Leg,
            EquipType.Cape, EquipType.Hair, EquipType.Face,
        };

        // 每个 AfterImageEntity 事件 instance 的动画累计已推进秒数，用于增量 Animator.Update 与倒退检测
        private static readonly Dictionary<object, float> s_AfterImageElapsed = new Dictionary<object, float>();

        private static void Update_AfterImageEntity(int _time, EditorActionEvent _actionEvent, Event_AfterImageEntity _event)
        {
            if (string.IsNullOrEmpty(_event.PrefabPath))
            {
                return;
            }

            bool inRange = _time >= _actionEvent.TriggerTime
                           && _time < _actionEvent.TriggerTime + _actionEvent.Duration;
            float elapsedSec = Mathf.Max(0f, (_time - _actionEvent.TriggerTime) / 1000f);

            if (m_ObjectPool.TryGetValue(_event, out Object obj))
            {
                if (obj is GameObject instance && instance)
                {
                    // 状态切换：离开区间隐藏，重新进入区间时重置动画与计时
                    if (instance.activeSelf != inRange)
                    {
                        instance.SetActive(inRange);
                        if (inRange)
                        {
                            s_AfterImageElapsed[_event] = 0f;
                            if (!string.IsNullOrEmpty(_event.AnimationName)
                                && instance.TryGetComponent(out Animator animatorOnEnter))
                            {
                                PrepareEditorAnimator(animatorOnEnter);
                                animatorOnEnter.Play(_event.AnimationName, 0, 0f);
                                animatorOnEnter.Update(0f);
                            }
                        }
                    }

                    if (!inRange) return;

                    // 位姿 / 缩放：每帧按挂点重算，scrub 时也保持挂点跟随
                    ApplyAfterImagePose(instance.transform, _event);
                    instance.transform.localScale = _event.LocalScale.GetValue();

                    // 动画增量推进（编辑器下 Animator 不随主循环 step，需手动 Update）
                    if (!string.IsNullOrEmpty(_event.AnimationName)
                        && instance.TryGetComponent(out Animator animator))
                    {
                        float lastElapsed = s_AfterImageElapsed.TryGetValue(_event, out var v) ? v : 0f;
                        if (elapsedSec + 0.0001f < lastElapsed)
                        {
                            // 时间轴往回拖：重置到 0 再推进 elapsedSec
                            animator.Play(_event.AnimationName, 0, 0f);
                            animator.Update(elapsedSec);
                        }
                        else
                        {
                            float dt = elapsedSec - lastElapsed;
                            if (dt > 0f) animator.Update(dt);
                        }
                        s_AfterImageElapsed[_event] = elapsedSec;
                    }
                }
            }
            else
            {
                // 先占位，避免异步加载期间下一帧重复 Load / Add 冲突
                m_ObjectPool[_event] = null;
                Transform partPool = CreateActionPool("ActionEditor_AfterImageEntity");
                EngineResourcesManager.Instance.LoaderObj_Editor(_event.PrefabPath, loaded =>
                {
                    if (loaded is GameObject prefab)
                    {
                        GameObject newInstance = Object.Instantiate(prefab, partPool);
                        newInstance.SetActive(false);
                        newInstance.transform.localScale = _event.LocalScale.GetValue();
                        ReplaceAfterImageMeshes(newInstance);
                        m_ObjectPool[_event] = newInstance;
                    }
                });
            }
        }

        private static void ApplyAfterImagePose(Transform effect, Event_AfterImageEntity _event)
        {
            var unit = ResourcesWindow.Instance.GetUnit();
            if (!unit) return;

            Transform anchor = unit.transform;
            if (ResourcesWindow.Instance.TryGetCharacterConfig(out CharacterConfig config)
                && config.HelpPointDic.TryGetValue((ECharacteLimbType)_event.PartPointType, out var point)
                && point)
            {
                anchor = point;
            }

            var pos = anchor.TransformPoint(_event.OffsetPos.GetValue());
            var rot = anchor.rotation * Quaternion.Euler(_event.OffsetRot.GetValue());
            effect.SetPositionAndRotation(pos, rot);
        }

        private static void ReplaceAfterImageMeshes(GameObject instance)
        {
            var unit = ResourcesWindow.Instance.GetUnit();
            if (!unit || !unit.TryGetComponent(out UnitEquip sourceEquip)) return;
            if (!instance.TryGetComponent(out UnitEquip instanceEquip)) return;

            foreach (var equipType in s_AfterImageEquipTypes)
            {
                if (!instanceEquip.GetMeshByType(equipType, out var target) || !target)
                {
                    continue;
                }

                if (!sourceEquip.GetMeshByType(equipType, out var sourceRenderer)
                    || !sourceRenderer
                    || !sourceRenderer.enabled
                    || !sourceRenderer.gameObject.activeSelf)
                {
                    target.gameObject.SetActive(false);
                    continue;
                }

                if (sourceRenderer is SkinnedMeshRenderer source && target is SkinnedMeshRenderer copy)
                {
                    if (source.sharedMesh)
                    {
                        copy.sharedMesh = source.sharedMesh;
                        copy.bones = MapBones(instanceEquip, source.bones);
                        if (source.rootBone)
                        {
                            copy.rootBone = instanceEquip.GetBoneByName(source.rootBone.name);
                        }
                        target.gameObject.SetActive(true);
                    }
                    else
                    {
                        target.gameObject.SetActive(false);
                    }
                }
            }
        }

        private static Transform[] MapBones(UnitEquip equip, Transform[] srcBones)
        {
            var count = srcBones.Length;
            var result = new Transform[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = srcBones[i] ? equip.GetBoneByName(srcBones[i].name) : null;
            }
            return result;
        }

        private static void PrepareEditorAnimator(Animator animator)
        {
            // 编辑器下 Animator 默认可能是 CullCompletely/CullUpdateTransforms，Culling 会导致 Update(dt) 跳过更新
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (!animator.enabled) animator.enabled = true;
        }

        // 用于统一访问 LightData / SkillLightData 的编辑器预览字段（两者属性签名完全一致）
        private class LightDataView
        {
            public string Color;
            public string RangeCurve;
            public float RangeMultiplier;
            public string IntensityCurve;
            public float IntensityMultiplier;
            public int RenderingLayerMask;
            public EVector3 OffsetPos;
            public ELightPositionMode PositionMode;
            public int GroundLayer;
            public float GroundYOffset;

            public static LightDataView From(object data)
            {
                switch (data)
                {
                    case LightData l:
                        return new LightDataView
                        {
                            Color = l.Color,
                            RangeCurve = l.RangeCurve,
                            RangeMultiplier = l.RangeMultiplier,
                            IntensityCurve = l.IntensityCurve,
                            IntensityMultiplier = l.IntensityMultiplier,
                            RenderingLayerMask = l.RenderingLayerMask,
                            OffsetPos = l.OffsetPos,
                            PositionMode = l.PositionMode,
                            GroundLayer = l.GroundLayer,
                            GroundYOffset = l.GroundYOffset,
                        };
                    default:
                        return null;
                }
            }
        }

        private static void Update_CreateSkill(int _time, EditorActionEvent _actionEvent, Event_CreateSkill _ecs)
        {
            if (ActionWindowMain.IsSkillSetting) return;
            if (_ecs.IsUsGValue) return;

            int relativeTime = _time - _actionEvent.TriggerTime;
            if (relativeTime < 0) return;

            if (!ResourcesWindow.Instance.GetSkillToID(_ecs.SkillID, out EditorSkillWarp skillWarp)) return;
            if (skillWarp.AllEditorActionState == null || skillWarp.AllEditorActionState.Count == 0) return;

            int actionIndex = Mathf.Clamp(_ecs.PreviewActionIndex, 0, skillWarp.AllEditorActionState.Count - 1);
            EditorActionState action = skillWarp.AllEditorActionState[actionIndex].EditorActionState;
            if (action?.AllEventTrackGroup is null) return;

            ActionStatePart part = ResourcesWindow.Instance.ActionStatePart;
            ResourcesWindow.Instance.TryGetCharacterConfig(out CharacterConfig config);
            bool hasPreviewPose = EventUpdate.TryGetCreateSkillPreviewPose(_ecs, config, part,
                out Vector3 previewPos, out Quaternion previewRot);
            Vector3 oldPos = hasPreviewPose ? part.Pos : Vector3.zero;
            Quaternion oldRot = hasPreviewPose ? part.Rot : Quaternion.identity;
            if (hasPreviewPose)
            {
                part.SetPos(previewPos);
                part.SetRot(previewRot);
            }

            try
            {
                foreach (var trackGroup in action.AllEventTrackGroup)
                {
                    foreach (var actionTrack in trackGroup.CurActiontTrack)
                    {
                        if (actionTrack is EventTrack eventTrack && eventTrack.IsPreview)
                        {
                            foreach (EventDisplay eventDisplay in eventTrack.CurEventDisplay)
                            {
                                if (eventDisplay.MainEvent.EventData is Event_CreateSkill) continue;
                                EventUpdate.Instance.EditorEventUpdate(relativeTime, eventDisplay.MainEvent);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (hasPreviewPose)
                {
                    part.SetPos(oldPos);
                    part.SetRot(oldRot);
                }
            }
        }

        #endregion

        public static bool ReLoadActionData(string _groupName)
        {
            EngineResourcesManager.Instance.ClearLoadBinary(_groupName);
            int _reLoadID = int.Parse(_groupName.Split('_')[^1]);
            List<ActionEngine_Unit> _units = ActionEngineManager_Unit.Instance.Units;

            foreach (ActionEngine_Unit _unit in _units)
            {
                if (_unit.ActionStateMachine.ActionGroupName == _groupName)
                {
                    // 主组命中：重建状态机并恢复主层级，同时保留原有装备模组
                    EngineDebug.Log($"[ReLoadActionData] 主组命中, group=[<color=#ffcc00>{_groupName}</color>], unit=[{_unit.name}]");
                    ReloadMainActionGroup(_unit, _reLoadID);
                }
                else if (HasEquipActionGroup(_unit, _groupName))
                {
                    // 装备组命中：就地热重载该装备模组，不重建整机
                    EngineDebug.Log($"[ReLoadActionData] 装备组命中, group=[<color=#ffcc00>{_groupName}</color>], unit=[{_unit.name}]");
                    ReloadEquipActionGroup(_unit, _reLoadID);
                }
            }
            TimeLineWindow.Instance.UpdateTimeToNow();
            return true;
        }

        // 主组重载：重建 ActionStateMachine，恢复主层级与时间，并在重建后重新装备原有模组
        private static void ReloadMainActionGroup(ActionEngine_Unit _unit, int _reLoadID)
        {
            ActionStateMachine _stateMachine = _unit.ActionStateMachine;

            // 用局部变量捕获，保证异步回调安全（多 Unit / 多次调用不会互相覆盖）
            List<float> savedTimes = new List<float>();
            List<string> savedActions = new List<string>();
            foreach (ActionStatePart _part in _stateMachine.AllActionStatePart)
            {
                savedTimes.Add(_part.ElapsedTime);
                savedActions.Add(_part.ActionEnble ? _part.CurrentActionState.Name : String.Empty);
            }
            float savedTimeScale = _stateMachine.TimeScale;

            // 捕获已装备模组 ID + 槽位（重建前实体槽位映射仍在），重建后按原槽位重新装备：
            // 修复 EquipActionInfoList 丢失，同时显式保留槽位映射而非依赖字典残留
            ActionEngine_Entity _equipEntity = GetEntity(_unit);
            List<int> savedEquipIds = new List<int>();
            List<int> savedEquipSlots = new List<int>();
            foreach (ActionStateInfo _equipInfo in _stateMachine.EquipActionInfoList)
            {
                savedEquipIds.Add(_equipInfo.ActionGroupID);
                int _slot = (_equipEntity != null &&
                             _equipEntity.TryGetSlotIDToActionListID(_equipInfo.ActionGroupID, out int _s)) ? _s : -1;
                savedEquipSlots.Add(_slot);
            }

            ActionEngineManager_Unit.Instance.GetActionList(_reLoadID, list =>
            {
                ActionEngineManager_GValue.Instance.GetGValue((gvalue, equation) =>
                {
                    GValuePool _pool = _unit.ActionStateMachine.GValuePool;
                    ActionStateMachine statePart = new ActionStateMachine(_unit,
                        _unit.GetComponent<Animator>(), list, gvalue, equation, _stateMachine.InitGValue_Setting);
                    _unit.SetActionStateMachine(statePart);
                    //保存ActionList后  重新载入时 GV重置
                    statePart.GValuePool = _pool;

                    if (ActionEngineManager_Input.Instance.IsPlayer(_unit))
                    {
                        CameraControl _cameraControl = ActionEngineManager_Input.Instance.CurCamera;
                        if (statePart.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig)))
                        {
                            if (_config.HelpPointDic.TryGetValue(ECharacteLimbType.Cam_Main, out Transform _trans))
                            {
                                _cameraControl.OnInit(_trans, statePart, 0);
                            }
                        }
                    }

                    // 在新状态机上恢复所有层级
                    statePart.TimeScale = savedTimeScale;
                    int restoreCount = Mathf.Min(savedTimes.Count, statePart.AllActionStatePart.Count);
                    for (int i = 0; i < restoreCount; i++)
                    {
                        string actionName = savedActions[i];
                        if (!string.IsNullOrEmpty(actionName))
                        {
                            statePart.ChangeAction(actionName, 0, 0);
                            statePart.AllActionStatePart[i].ElapsedTime = savedTimes[i];
                        }
                    }

                    // 重建后恢复装备模组（缓存未被清除，按缓存快速重新装备）
                    RestoreEquipActionInfos(_unit, savedEquipIds, savedEquipSlots);

                    // 刷新编辑器引用
                    if (statePart.AllActionStatePart.Count > 0)
                        ResourcesWindow.Instance.ActionStatePart = statePart.AllActionStatePart[0];

                    if (TimeLineWindow.Instance.IsEditor)
                        statePart.SetAnimatorSpeed(0);

                    TimeLineWindow.Instance.UpdateTimeToNow();
                });
            });
        }

        // 重建主机后恢复装备模组：复用实体装备队列（内部异步加载动画覆写）。
        // 按捕获的原槽位重装（_equipSlots 与 _equipIds 对齐，-1 表示无槽位），保留槽位映射。
        private static void RestoreEquipActionInfos(ActionEngine_Unit _unit, List<int> _equipIds, List<int> _equipSlots)
        {
            if (_equipIds == null || _equipIds.Count == 0) return;

            ActionEngine_Entity _entity = GetEntity(_unit);
            if (_entity == null)
            {
                EngineDebug.LogWarning($"[ReLoadActionData] 无法恢复装备模组, unit 非 ActionEngine_Entity, ids=[{string.Join(",", _equipIds)}]");
                return;
            }

            Action<bool> _onDone = success =>
            {
                if (TimeLineWindow.Instance.IsEditor)
                    _unit.ActionStateMachine.SetAnimatorSpeed(0);
                TimeLineWindow.Instance.UpdateTimeToNow();
            };

            // 分类：带槽位的按原槽位重装（保留槽位映射），无槽位的常规重装
            List<int> _slotAG = new List<int>();
            List<int> _slotVal = new List<int>();
            List<int> _noSlotAG = new List<int>();
            for (int i = 0; i < _equipIds.Count; i++)
            {
                int _slot = (_equipSlots != null && i < _equipSlots.Count) ? _equipSlots[i] : -1;
                if (_slot >= 0)
                {
                    _slotAG.Add(_equipIds[i]);
                    _slotVal.Add(_slot);
                }
                else
                {
                    _noSlotAG.Add(_equipIds[i]);
                }
            }

            EngineDebug.Log($"[ReLoadActionData] 重建后恢复装备模组, 带槽位=[{string.Join(",", _slotAG)}], 无槽位=[{string.Join(",", _noSlotAG)}]");

            if (_slotAG.Count > 0)
                _entity.EquipActionInfo(_slotAG, _slotVal, _onDone);
            if (_noSlotAG.Count > 0)
                _entity.EquipActionInfo(_noSlotAG, _onDone);
        }

        // 装备组热重载：卸载旧实例 + 重新装备（拉取最新编辑数据），并恢复属于该组的播放层
        private static void ReloadEquipActionGroup(ActionEngine_Unit _unit, int _reLoadID)
        {
            ActionEngine_Entity _entity = GetEntity(_unit);
            if (_entity == null)
            {
                EngineDebug.LogWarning($"[ReLoadActionData] 装备组热重载失败, unit 非 ActionEngine_Entity, id=[{_reLoadID}]");
                return;
            }

            ActionStateMachine _stateMachine = _unit.ActionStateMachine;

            // 捕获属于该装备组的层的播放状态，重载后重新指向新数据
            List<int> savedLayers = new List<int>();
            List<string> savedActions = new List<string>();
            List<float> savedTimes = new List<float>();
            List<ActionStatePart> _parts = _stateMachine.AllActionStatePart;
            for (int i = 0; i < _parts.Count; i++)
            {
                ActionStatePart _part = _parts[i];
                if (_part.ActionEnble && _part.ActiveActionGroupID == _reLoadID)
                {
                    savedLayers.Add(i);
                    savedActions.Add(_part.CurrentActionState.Name);
                    savedTimes.Add(_part.ElapsedTime);
                }
            }

            // 捕获该装备组当前所在槽位（DicSlotIDToActionListID: key=AG ID, value=槽位）。
            // UnEquipAction 会删除该映射，若随后用无槽位重载重装则槽位绑定丢失，导致
            // GraphEvent_TrackData_GetActionGroupIDBySlotID 报“不存在槽位”。故需用同槽位重装。
            bool _hasSlot = _entity.TryGetSlotIDToActionListID(_reLoadID, out int _savedSlotID);

            Action<bool> _onReequipped = success =>
            {
                EngineDebug.Log($"[ReLoadActionData] 装备组热重载完成, id=[{_reLoadID}], success=[{success}], slot=[{(_hasSlot ? _savedSlotID : -1)}]");

                ActionStateMachine _machine = _unit.ActionStateMachine;
                for (int i = 0; i < savedLayers.Count; i++)
                {
                    int _layer = savedLayers[i];
                    string _name = savedActions[i];
                    if (string.IsNullOrEmpty(_name) || _layer >= _machine.AllActionStatePart.Count) continue;
                    _machine.ChangeAction(_name, 0, 0);
                    _machine.AllActionStatePart[_layer].ElapsedTime = savedTimes[i];
                }

                // 优先把编辑器预览部件指向该装备组正在播放的层
                if (savedLayers.Count > 0 && savedLayers[0] < _machine.AllActionStatePart.Count)
                    ResourcesWindow.Instance.ActionStatePart = _machine.AllActionStatePart[savedLayers[0]];
                else if (_machine.AllActionStatePart.Count > 0)
                    ResourcesWindow.Instance.ActionStatePart = _machine.AllActionStatePart[0];

                if (TimeLineWindow.Instance.IsEditor)
                    _machine.SetAnimatorSpeed(0);

                TimeLineWindow.Instance.UpdateTimeToNow();
            };

            // 缓存已被 ClearLoadBinary 清除：卸载旧实例后重新装备会拉取最新编辑数据；
            // 原本绑定了槽位的，按同槽位重装以保留槽位映射
            _entity.UnEquipAction(_reLoadID);
            if (_hasSlot)
                _entity.EquipActionInfo(_reLoadID, _savedSlotID, _onReequipped);
            else
                _entity.EquipActionInfo(_reLoadID, _onReequipped);
        }

        private static bool HasEquipActionGroup(ActionEngine_Unit _unit, string _groupName)
        {
            foreach (ActionStateInfo _info in _unit.ActionStateMachine.EquipActionInfoList)
            {
                if (_info.ActionGroupName == _groupName) return true;
            }
            return false;
        }

        private static ActionEngine_Entity GetEntity(ActionEngine_Unit _unit)
        {
            if (_unit is ActionEngine_Entity _entity) return _entity;
            return _unit?.GetSource as ActionEngine_Entity;
        }
    }
}