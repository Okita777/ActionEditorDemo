#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using Animancer;
using ActionEditor.CharacterMotion;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{   
    //okita:感觉不用继承monobehaviour
    public sealed class SkillCharacterActionBridge : MonoBehaviour, ICharacterAnimationController
    {
        private sealed class AnimationLayerRuntime
        {
            public AnimationLayerType LayerType;
            public AnimancerLayer Layer;
            public AnimancerState CurrentState;
            public AnimationClip CurrentClip;
            public DirectionalMixerState CurrentDirectionalMixer;
            public string CurrentStateId = string.Empty;
            public string CurrentAnimationKey = string.Empty;
            public bool AppliesRootMotion;
            public float BasePlaybackSpeed = 1f;
            public bool MatchLocomotionSpeed;
            public float AuthoredMoveSpeed = 6f;
            public float MinLocomotionPlaybackSpeed = 0.85f;
            public float MaxLocomotionPlaybackSpeed = 1.15f;
            public float SpeedMatchSharpness = 18f;
            public float SpeedMatchDeadZone = 0.01f;
            public float CurrentMatchedSpeed = 1f;
            public float DirectionalParameterSmoothSpeed = 18f;
            public Vector2 DirectionalParameter;
        }

        [SerializeField] private Animator _animator;
        [SerializeField] private AnimancerComponent _animancer;
        [SerializeField] private AvatarMask _upperBodyMask;
        [SerializeField] private AvatarMask _additiveMask;
        [SerializeField] private CustomCharacterController _characterController;

        private readonly Dictionary<AnimationLayerType, AnimationLayerRuntime> _layerRuntimes =
            new Dictionary<AnimationLayerType, AnimationLayerRuntime>();
        private float _playbackScale = 1f;

        private void Awake()
        {
            ResolveAnimationComponents();
            if (_layerRuntimes.Count == 0)
            {
                InitializeDefaultAnimationLayers();
            }
        }

        private void OnDisable()
        {
            StopAllStateAnimations(null);
        }

        private void Update()
        {
            UpdateLocomotionPlaybackSpeed();
            UpdateDirectionalMixerParameters();
        }

        private void OnDestroy()
        {
            StopAllStateAnimations(null);
        }

        public void PlayStateAnimation(SkillContext context, StateConfig stateConfig, StateInterruptConfig interruptConfig)
        {
            if (!TryPlayStateAnimation(stateConfig, interruptConfig))
            {
                 Debug.LogWarning($"SkillCharacterActionBridge: failed to play state animation, stateId='{stateConfig?.StateId ?? "null"}'.", this);
            }
        }

        public void ConfigureAnimationLayers(UnitConfig unitConfig)
        {
            ResolveAnimationComponents();
            if (_animancer == null)
            {
                return;
            }

            StopAllStateAnimations(null);
            _layerRuntimes.Clear();
            if (unitConfig == null || unitConfig.AnimationLayers == null || unitConfig.AnimationLayers.Count == 0)
            {
                InitializeDefaultAnimationLayers();
                return;
            }

            HashSet<AnimationLayerType> configuredLayerTypes = new HashSet<AnimationLayerType>();
            HashSet<int> configuredLayerIndices = new HashSet<int>();
            for (int i = 0; i < unitConfig.AnimationLayers.Count; i++)
            {
                UnitAnimationLayerConfig config = unitConfig.AnimationLayers[i];
                if (config == null || config.Layer == AnimationLayerType.None || config.AnimancerLayerIndex < 0)
                {
                    continue;
                }

                if (!configuredLayerTypes.Add(config.Layer))
                {
                    Debug.LogError($"SkillCharacterActionBridge: duplicate animation layer type '{config.Layer}'.", this);
                    continue;
                }

                if (!configuredLayerIndices.Add(config.AnimancerLayerIndex))
                {
                    Debug.LogError($"SkillCharacterActionBridge: duplicate Animancer layer index '{config.AnimancerLayerIndex}'.", this);
                    continue;
                }

                AvatarMask mask = LoadAvatarMask(config.AvatarMaskAssetPath);
                if (config.Layer == AnimationLayerType.UpperBody && mask == null)
                {
                    Debug.LogError($"SkillCharacterActionBridge: UpperBody layer was not registered because AvatarMask loading failed, assetPath='{config.AvatarMaskAssetPath}', resourcePath='{GetAvatarMaskResourcePath(config.AvatarMaskAssetPath)}'.", this);
                    continue;
                }

                RegisterLayer(
                    config.Layer,
                    config.AnimancerLayerIndex,
                    config.BlendMode == AnimationBlendMode.Additive,
                    mask,
                    Mathf.Clamp01(config.DefaultWeight));
                LogDebug($"SkillCharacterActionBridge: registered animation layer type='{config.Layer}', index={config.AnimancerLayerIndex}, additive={config.BlendMode == AnimationBlendMode.Additive}, mask='{mask?.name ?? "null"}'.");
            }

            EnsureRequiredBaseLayers();
        }

        public void SeekStateAnimation(SkillContext context, StateConfig stateConfig, float animationTime)
        {
            if (stateConfig == null || !TryGetLayerRuntime(GetOutputLayer(stateConfig), out AnimationLayerRuntime runtime))
            {
                return;
            }

            if (runtime.CurrentState == null ||
                !string.Equals(runtime.CurrentStateId, stateConfig.StateId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            runtime.CurrentState.Time = GetStateAnimationSampleTime(stateConfig, runtime.CurrentClip, animationTime);
        }

        public void StopStateAnimation(SkillContext context, StateConfig stateConfig, bool interrupted)
        {
            if (stateConfig == null || !TryGetLayerRuntime(GetOutputLayer(stateConfig), out AnimationLayerRuntime runtime))
            {
                return;
            }

            if (!string.Equals(runtime.CurrentStateId, stateConfig.StateId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            float fadeDuration = GetFadeDuration(GetStateAnimationConfig(stateConfig), runtime.CurrentClip);
            runtime.Layer.StartFade(0f, fadeDuration);
            ClearLayerRuntime(runtime);
            RefreshRootMotionOwner();
        }

        public void StopAllStateAnimations(SkillContext context)
        {
            foreach (KeyValuePair<AnimationLayerType, AnimationLayerRuntime> pair in _layerRuntimes)
            {
                AnimationLayerRuntime runtime = pair.Value;
                runtime?.CurrentState?.Stop();
                if (runtime != null)
                {
                    runtime.Layer.Weight = 0f;
                    ClearLayerRuntime(runtime);
                }
            }

            if (_animator != null)
            {
                _animator.applyRootMotion = false;
            }
        }

        public void SetPlaybackScale(float scale)
        {
            _playbackScale = Mathf.Clamp01(scale);
            ApplyPlaybackSpeeds();
        }

        private void ApplyPlaybackSpeeds()
        {
            foreach (KeyValuePair<AnimationLayerType, AnimationLayerRuntime> pair in _layerRuntimes)
            {
                AnimationLayerRuntime runtime = pair.Value;
                if (runtime?.CurrentState == null)
                {
                    continue;
                }

                runtime.CurrentState.Speed = runtime.BasePlaybackSpeed * runtime.CurrentMatchedSpeed * _playbackScale;
                if (runtime.CurrentDirectionalMixer == null)
                {
                    continue;
                }

                for (int i = 0; i < runtime.CurrentDirectionalMixer.ChildCount; i++)
                {
                    AnimancerState child = runtime.CurrentDirectionalMixer.GetChild(i);
                    if (child != null)
                    {
                        child.Speed = runtime.BasePlaybackSpeed * _playbackScale;
                    }
                }
            }
        }

        private void ResolveAnimationComponents()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
                LogDebug($"SkillCharacterActionBridge.ResolveAnimationComponents: resolvedAnimator='{_animator?.name ?? "null"}'.");
            }

            if (_animancer == null)
            {
                HybridAnimancerComponent hybridAnimancer = GetComponent<HybridAnimancerComponent>() ??
                                                          GetComponentInChildren<HybridAnimancerComponent>(true);
                if (hybridAnimancer != null)
                {
                    _animancer = hybridAnimancer;
                }
                else
                {
                    _animancer = GetComponent<AnimancerComponent>() ??
                                 GetComponentInChildren<AnimancerComponent>(true);
                }

                if (_animancer == null && _animator != null)
                {
                    RuntimeAnimatorController controller = _animator.runtimeAnimatorController;
                    if (controller != null)
                    {
                        hybridAnimancer = _animator.GetOrAddAnimancerComponent<HybridAnimancerComponent>();
                        hybridAnimancer.Controller = controller;
                        _animator.runtimeAnimatorController = null;
                        hybridAnimancer.PlayController();
                        _animancer = hybridAnimancer;
                    }
                    else
                    {
                        _animancer = _animator.GetOrAddAnimancerComponent<AnimancerComponent>();
                    }
                }

                LogDebug($"SkillCharacterActionBridge.ResolveAnimationComponents: resolvedAnimancer='{_animancer?.name ?? "null"}'.");
            }
        }

        private void InitializeDefaultAnimationLayers()
        {
            if (_animancer == null)
            {
                return;
            }

            _layerRuntimes.Clear();
            RegisterLayer(AnimationLayerType.Locomotion, 0, false, null, 1f);
            RegisterLayer(AnimationLayerType.Action, 1, false, null, 0f);
            if (_upperBodyMask != null)
            {
                RegisterLayer(AnimationLayerType.UpperBody, 2, false, _upperBodyMask, 0f);
            }
            else
            {
                Debug.LogWarning("SkillCharacterActionBridge: UpperBody AvatarMask is not configured; UpperBody animation layer is disabled.", this);
            }

            RegisterLayer(AnimationLayerType.Additive, 3, true, _additiveMask, 0f);
        }

        private void EnsureRequiredBaseLayers()
        {
            if (!TryGetLayerRuntime(AnimationLayerType.Locomotion, out _))
            {
                RegisterLayer(AnimationLayerType.Locomotion, 0, false, null, 1f);
            }

            if (!TryGetLayerRuntime(AnimationLayerType.Action, out _))
            {
                RegisterLayer(AnimationLayerType.Action, 1, false, null, 0f);
            }
        }

        private void RegisterLayer(AnimationLayerType layerType, int layerIndex, bool additive, AvatarMask mask, float defaultWeight)
        {
            AnimancerLayer layer = _animancer.Layers[layerIndex];
            layer.IsAdditive = additive;
            if (mask != null)
            {
                layer.SetMask(mask);
            }

            layer.Weight = defaultWeight;
            layer.SetEditorName(layerType.ToString());
            _layerRuntimes[layerType] = new AnimationLayerRuntime
            {
                LayerType = layerType,
                Layer = layer,
            };
        }

        private bool TryPlayStateAnimation(StateConfig stateConfig, StateInterruptConfig interruptConfig)
        {
            string animationKey = GetStateAnimationKey(stateConfig);
            if (stateConfig == null || string.IsNullOrEmpty(animationKey))
            {
                return false;
            }

            ResolveAnimationComponents();
            if (_animancer == null)
            {
                Debug.LogWarning("SkillCharacterActionBridge: AnimancerComponent is missing, cannot play state animation.", this);
                return false;
            }

            if (_layerRuntimes.Count == 0)
            {
                InitializeDefaultAnimationLayers();
            }

            AnimationLayerType outputLayer = GetOutputLayer(stateConfig);
            if (!TryGetLayerRuntime(outputLayer, out AnimationLayerRuntime runtime))
            {
                Debug.LogWarning($"SkillCharacterActionBridge: animation layer '{outputLayer}' is not registered. Check UnitConfig.AnimationLayers and the layer AvatarMask loading log. stateId='{stateConfig.StateId}'.", this);
                return false;
            }

            if (stateConfig.AnimationMode == StateAnimationMode.DirectionalMixer2D)
            {
                if (!TryPlayDirectionalMixer(runtime, stateConfig, interruptConfig))
                {
                    return false;
                }
            }
            else
            {
                AnimationClip clip = SkillAnimationRuntimeCatalog.LoadClip(animationKey);
                if (clip == null)
                {
                    Debug.LogWarning($"SkillCharacterActionBridge: failed to load State AnimationClip '{animationKey}'.", this);
                    return false;
                }

                PlayStateClip(runtime, stateConfig, interruptConfig, clip, animationKey);
            }

            return true;
        }

        private bool TryPlayDirectionalMixer(AnimationLayerRuntime runtime, StateConfig stateConfig, StateInterruptConfig interruptConfig)
        {
            if (runtime == null || stateConfig == null)
            {
                return false;
            }

            StateDirectionalMixer2DConfig mixerConfig = stateConfig.DirectionalMixer2D ?? StateDirectionalMixer2DConfig.CreateDefault();
            if (!TryBuildDirectionalMixerData(mixerConfig, out AnimationClip[] clips, out Vector2[] thresholds, out string missingSlotsMessage))
            {
                Debug.LogWarning($"SkillCharacterActionBridge: DirectionalMixer2D config is invalid, stateId='{stateConfig.StateId}', missing={missingSlotsMessage}.", this);
                return false;
            }

            TimelineAnimationConfig animationConfig = GetStateAnimationConfig(stateConfig);
            float fadeDuration = GetStateFadeDuration(animationConfig, interruptConfig, null);
            FadeMode fadeMode = ConvertFadeMode(animationConfig != null ? animationConfig.FadeMode : AnimancerFadeMode.FixedDuration);
            string stateKey = BuildLayerStateKey(runtime.LayerType, stateConfig, "DirectionalMixer2D");

            DirectionalMixerState mixerState = null;
            if (_animancer.States.TryGet(stateKey, out AnimancerState cachedState))
            {
                mixerState = cachedState as DirectionalMixerState;
                if (mixerState == null)
                {
                    cachedState.Stop();
                    cachedState.Destroy();
                }
            }

            if (mixerState == null)
            {
                mixerState = new DirectionalMixerState();
                runtime.Layer.AddChild(mixerState);
                mixerState.Key = stateKey;
            }

            mixerState.Initialise(clips, thresholds);
            Vector2 initialParameter = ResolveDirectionalMixerParameter();
            mixerState.Parameter = initialParameter;

            AnimancerState playedState = fadeDuration > 0f
                ? runtime.Layer.Play(mixerState, fadeDuration, fadeMode)
                : runtime.Layer.Play(mixerState);
            if (playedState == null)
            {
                return false;
            }

            StateAnimationProfile profile = stateConfig.AnimationProfile ?? new StateAnimationProfile();
            float layerWeight = ResolveLayerWeight(profile);
            runtime.Layer.StartFade(layerWeight, fadeDuration);
            runtime.CurrentState = playedState;
            runtime.CurrentClip = null;
            runtime.CurrentDirectionalMixer = mixerState;
            runtime.CurrentStateId = stateConfig.StateId ?? string.Empty;
            runtime.CurrentAnimationKey = stateKey;
            runtime.AppliesRootMotion = false;
            runtime.BasePlaybackSpeed = Mathf.Max(0f, profile.Speed);
            runtime.MatchLocomotionSpeed = false;
            runtime.AuthoredMoveSpeed = Mathf.Max(0.01f, profile.AuthoredMoveSpeed);
            runtime.MinLocomotionPlaybackSpeed = Mathf.Max(0f, profile.MinLocomotionPlaybackSpeed);
            runtime.MaxLocomotionPlaybackSpeed = Mathf.Max(runtime.MinLocomotionPlaybackSpeed, profile.MaxLocomotionPlaybackSpeed);
            runtime.SpeedMatchSharpness = Mathf.Max(0f, profile.LocomotionSpeedMatchSharpness);
            runtime.SpeedMatchDeadZone = Mathf.Max(0f, profile.LocomotionSpeedMatchDeadZone);
            runtime.CurrentMatchedSpeed = 1f;
            runtime.DirectionalParameterSmoothSpeed = Mathf.Max(0f, mixerConfig.ParameterSmoothSpeed);
            runtime.DirectionalParameter = initialParameter;

            float startTime = GetDirectionalMixerStartTime(animationConfig, clips);
            for (int i = 0; i < mixerState.ChildCount; i++)
            {
                AnimancerState child = mixerState.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                child.Time = startTime;
                child.Speed = runtime.BasePlaybackSpeed * _playbackScale;
            }

            RefreshRootMotionOwner();
            return true;
        }

        private void PlayStateClip(AnimationLayerRuntime runtime, StateConfig stateConfig, StateInterruptConfig interruptConfig, AnimationClip clip, string animationKey)
        {
            ResolveAnimationComponents();
            if (_animancer == null || clip == null || runtime == null || runtime.Layer == null)
            {
                return;
            }

            TimelineAnimationConfig animationConfig = GetStateAnimationConfig(stateConfig);
            float fadeDuration = GetStateFadeDuration(animationConfig, interruptConfig, clip);
            FadeMode fadeMode = ConvertFadeMode(animationConfig != null ? animationConfig.FadeMode : AnimancerFadeMode.FixedDuration);
            string stateKey = BuildLayerStateKey(runtime.LayerType, stateConfig, animationKey);
            AnimancerState animationState = runtime.Layer.GetOrCreateState(stateKey, clip);

            animationState = fadeDuration > 0f
                ? runtime.Layer.Play(animationState, fadeDuration, fadeMode)
                : runtime.Layer.Play(animationState);

            if (animationState == null)
            {
                return;
            }

            StateAnimationProfile profile = stateConfig.AnimationProfile ?? new StateAnimationProfile();
            animationState.Time = GetAnimationSampleTime(animationConfig, clip, 0f);
            animationState.Speed = Mathf.Max(0f, profile.Speed) * _playbackScale;
            float layerWeight = ResolveLayerWeight(profile);
            runtime.Layer.StartFade(layerWeight, fadeDuration);
            runtime.CurrentState = animationState;
            runtime.CurrentClip = clip;
            runtime.CurrentStateId = stateConfig.StateId ?? string.Empty;
            runtime.CurrentAnimationKey = animationKey;
            runtime.AppliesRootMotion = profile.ApplyRootMotion &&
                                        runtime.LayerType != AnimationLayerType.UpperBody &&
                                        runtime.LayerType != AnimationLayerType.Additive;
            runtime.BasePlaybackSpeed = Mathf.Max(0f, profile.Speed);
            runtime.MatchLocomotionSpeed = profile.MatchLocomotionSpeed &&
                                           runtime.LayerType == AnimationLayerType.Locomotion &&
                                           !runtime.AppliesRootMotion;
            runtime.AuthoredMoveSpeed = Mathf.Max(0.01f, profile.AuthoredMoveSpeed);
            runtime.MinLocomotionPlaybackSpeed = Mathf.Max(0f, profile.MinLocomotionPlaybackSpeed);
            runtime.MaxLocomotionPlaybackSpeed = Mathf.Max(
                runtime.MinLocomotionPlaybackSpeed,
                profile.MaxLocomotionPlaybackSpeed);
            runtime.SpeedMatchSharpness = Mathf.Max(0f, profile.LocomotionSpeedMatchSharpness);
            runtime.SpeedMatchDeadZone = Mathf.Max(0f, profile.LocomotionSpeedMatchDeadZone);
            runtime.CurrentMatchedSpeed = 1f;
            RefreshRootMotionOwner();
        }

        private void UpdateLocomotionPlaybackSpeed()
        {
            if (!TryGetLayerRuntime(AnimationLayerType.Locomotion, out AnimationLayerRuntime runtime) ||
                runtime.CurrentState == null || !runtime.MatchLocomotionSpeed)
            {
                return;
            }

            _characterController ??= GetComponent<CustomCharacterController>() ??
                                     GetComponentInParent<CustomCharacterController>() ??
                                     GetComponentInChildren<CustomCharacterController>(true);
            if (_characterController == null)
            {
                return;
            }

            // 步频跟随 Locomotion 的运动驱动速度，而不是碰撞后的实现速度。
            // 碰墙只代表位移被环境阻挡，不代表玩家撤销了奔跑意图；
            // 若仍处于 Run 状态，动画应保持正常跑步节奏，而不是趋近慢动作。
            float speedRatio = _characterController.LocomotionDrivePlanarSpeed / runtime.AuthoredMoveSpeed;
            float matchedSpeed = Mathf.Clamp(
                speedRatio,
                runtime.MinLocomotionPlaybackSpeed,
                runtime.MaxLocomotionPlaybackSpeed);
            if (Mathf.Abs(matchedSpeed - runtime.CurrentMatchedSpeed) > runtime.SpeedMatchDeadZone)
            {
                float t = runtime.SpeedMatchSharpness > 0f
                    ? 1f - Mathf.Exp(-runtime.SpeedMatchSharpness * Mathf.Max(0f, Time.deltaTime))
                    : 1f;
                runtime.CurrentMatchedSpeed = Mathf.Lerp(runtime.CurrentMatchedSpeed, matchedSpeed, t);
            }

            runtime.CurrentState.Speed = runtime.BasePlaybackSpeed * runtime.CurrentMatchedSpeed * _playbackScale;
        }

        private void UpdateDirectionalMixerParameters()
        {
            _characterController ??= GetComponent<CustomCharacterController>() ??
                                     GetComponentInParent<CustomCharacterController>() ??
                                     GetComponentInChildren<CustomCharacterController>(true);
            if (_characterController == null)
            {
                return;
            }

            foreach (KeyValuePair<AnimationLayerType, AnimationLayerRuntime> pair in _layerRuntimes)
            {
                AnimationLayerRuntime runtime = pair.Value;
                if (runtime == null || runtime.CurrentDirectionalMixer == null || runtime.CurrentState == null)
                {
                    continue;
                }

                Vector2 target = ResolveDirectionalMixerParameter();

                float dt = Mathf.Max(0f, Time.deltaTime);
                if (runtime.DirectionalParameterSmoothSpeed > 0f)
                {
                    float t = 1f - Mathf.Exp(-runtime.DirectionalParameterSmoothSpeed * dt);
                    runtime.DirectionalParameter = Vector2.Lerp(runtime.DirectionalParameter, target, t);
                }
                else
                {
                    runtime.DirectionalParameter = target;
                }

                runtime.CurrentDirectionalMixer.Parameter = runtime.DirectionalParameter;
            }
        }

        private Vector2 ResolveDirectionalMixerParameter()
        {
            if (_characterController == null)
            {
                return Vector2.zero;
            }

            Vector3 localVelocity = _characterController.LocomotionAnimationLocalVelocity;
            Vector2 parameter = new Vector2(localVelocity.x, localVelocity.z);
            return parameter.sqrMagnitude > 1f ? parameter.normalized : parameter;
        }

        private static float ResolveLayerWeight(StateAnimationProfile profile)
        {
            if (profile == null)
            {
                return 1f;
            }

            if (profile.LayerWeight <= 0f && profile.OverrideLowerLayers)
            {
                return 1f;
            }

            return Mathf.Clamp01(profile.LayerWeight);
        }

        private static string GetStateAnimationKey(StateConfig stateConfig)
        {
            if (stateConfig == null)
            {
                return string.Empty;
            }

            if (stateConfig.AnimationMode == StateAnimationMode.DirectionalMixer2D)
            {
                return "DirectionalMixer2D";
            }

            return stateConfig.AnimationClipPath;
        }

        private static bool TryBuildDirectionalMixerData(
            StateDirectionalMixer2DConfig config,
            out AnimationClip[] clips,
            out Vector2[] thresholds,
            out string missingSlotsMessage)
        {
            config ??= StateDirectionalMixer2DConfig.CreateDefault();
            List<AnimationClip> clipList = new List<AnimationClip>(9);
            List<Vector2> thresholdList = new List<Vector2>(9);
            List<string> missing = new List<string>();

            AddDirectionalSlot("Idle", config.IdleClipPath, config.IdleThreshold, clipList, thresholdList, missing);
            AddDirectionalSlot("Forward", config.ForwardClipPath, config.ForwardThreshold, clipList, thresholdList, missing);
            AddDirectionalSlot("ForwardRight", config.ForwardRightClipPath, config.ForwardRightThreshold, clipList, thresholdList, missing);
            AddDirectionalSlot("Right", config.RightClipPath, config.RightThreshold, clipList, thresholdList, missing);
            AddDirectionalSlot("BackRight", config.BackRightClipPath, config.BackRightThreshold, clipList, thresholdList, missing);
            AddDirectionalSlot("Back", config.BackClipPath, config.BackThreshold, clipList, thresholdList, missing);
            AddDirectionalSlot("BackLeft", config.BackLeftClipPath, config.BackLeftThreshold, clipList, thresholdList, missing);
            AddDirectionalSlot("Left", config.LeftClipPath, config.LeftThreshold, clipList, thresholdList, missing);
            AddDirectionalSlot("ForwardLeft", config.ForwardLeftClipPath, config.ForwardLeftThreshold, clipList, thresholdList, missing);

            clips = clipList.ToArray();
            thresholds = thresholdList.ToArray();
            missingSlotsMessage = string.Join(", ", missing.ToArray());
            return clips.Length > 0;
        }

        private static void AddDirectionalSlot(
            string slotName,
            string clipPath,
            Vector2 threshold,
            List<AnimationClip> clips,
            List<Vector2> thresholds,
            List<string> missing)
        {
            if (string.IsNullOrWhiteSpace(clipPath))
            {
                missing.Add(slotName);
                return;
            }

            AnimationClip clip = SkillAnimationRuntimeCatalog.LoadClip(clipPath);
            if (clip == null)
            {
                missing.Add(slotName);
                return;
            }

            clips.Add(clip);
            thresholds.Add(threshold);
        }

        private static float GetDirectionalMixerStartTime(TimelineAnimationConfig animationConfig, AnimationClip[] clips)
        {
            if (animationConfig == null || clips == null || clips.Length == 0)
            {
                return 0f;
            }

            float minClipLength = float.MaxValue;
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip == null)
                {
                    continue;
                }

                minClipLength = Mathf.Min(minClipLength, Mathf.Max(0f, clip.length));
            }

            if (minClipLength <= 0f || minClipLength == float.MaxValue)
            {
                return 0f;
            }

            float startTime = Mathf.Max(0f, animationConfig.StartTime);
            if (animationConfig.StartTimeUnit == AnimationStartTimeUnit.NormalizedTime)
            {
                startTime *= minClipLength;
            }

            return Mathf.Clamp(startTime, 0f, minClipLength);
        }

        private static AnimationLayerType GetOutputLayer(StateConfig stateConfig)
        {
            if (stateConfig == null || stateConfig.AnimationProfile == null || stateConfig.AnimationProfile.OutputLayer == AnimationLayerType.None)
            {
                return stateConfig != null && stateConfig.Layer == StateLayerType.Action
                    ? AnimationLayerType.Action
                    : AnimationLayerType.Locomotion;
            }

            return stateConfig.AnimationProfile.OutputLayer;
        }

        private bool TryGetLayerRuntime(AnimationLayerType layerType, out AnimationLayerRuntime runtime)
        {
            return _layerRuntimes.TryGetValue(layerType, out runtime) && runtime != null;
        }

        private static string BuildLayerStateKey(AnimationLayerType layerType, StateConfig stateConfig, string animationKey)
        {
            return $"{layerType}|{stateConfig?.StateId ?? string.Empty}|{animationKey}";
        }

        private static AvatarMask LoadAvatarMask(string assetPath)
        {
            string resourcePath = GetAvatarMaskResourcePath(assetPath);
            return string.IsNullOrWhiteSpace(resourcePath)
                ? null
                : Resources.Load<AvatarMask>(resourcePath);
        }

        private static string GetAvatarMaskResourcePath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return string.Empty;
            }

            const string resourcesSegment = "/Resources/";
            string normalizedPath = assetPath.Replace('\\', '/');
            int resourcesIndex = normalizedPath.IndexOf(resourcesSegment, StringComparison.OrdinalIgnoreCase);
            if (resourcesIndex < 0)
            {
                return string.Empty;
            }

            string resourcePath = normalizedPath.Substring(resourcesIndex + resourcesSegment.Length);
            int extensionIndex = resourcePath.LastIndexOf('.');
            if (extensionIndex >= 0)
            {
                resourcePath = resourcePath.Substring(0, extensionIndex);
            }

            return resourcePath;
        }

        private static TimelineAnimationConfig GetStateAnimationConfig(StateConfig stateConfig)
        {
            return stateConfig != null && stateConfig.Timeline != null ? stateConfig.Timeline.Animation : null;
        }

        private static float GetFadeDuration(TimelineAnimationConfig animationConfig, AnimationClip clip)
        {
            if (animationConfig == null)
            {
                return 0f;
            }

            float duration = Mathf.Max(0f, animationConfig.TransitionDuration);
            if (duration <= 0f)
            {
                return 0f;
            }

            if (animationConfig.TransitionTimeUnit == AnimationTransitionTimeUnit.NormalizedDuration && clip != null)
            {
                duration *= Mathf.Max(0f, clip.length);
            }

            return Mathf.Max(0f, duration);
        }

        private static float GetStateFadeDuration(TimelineAnimationConfig animationConfig, StateInterruptConfig interruptConfig, AnimationClip clip)
        {
            if (interruptConfig == null || !interruptConfig.UseTransitionOverride)
            {
                return GetFadeDuration(animationConfig, clip);
            }

            float duration = Mathf.Max(0f, interruptConfig.TransitionDuration);
            if (interruptConfig.TransitionTimeUnit == AnimationTransitionTimeUnit.NormalizedDuration && clip != null)
            {
                duration *= Mathf.Max(0f, clip.length);
            }

            return duration;
        }

        private static float GetAnimationSampleTime(TimelineAnimationConfig animationConfig, AnimationClip clip, float timelineTime)
        {
            float clipLength = clip != null ? Mathf.Max(0f, clip.length) : 0f;
            if (clipLength <= 0f)
            {
                return 0f;
            }

            float startTime = 0f;
            if (animationConfig != null)
            {
                startTime = Mathf.Max(0f, animationConfig.StartTime);
                if (animationConfig.StartTimeUnit == AnimationStartTimeUnit.NormalizedTime)
                {
                    startTime *= clipLength;
                }
            }

            if (startTime >= clipLength)
            {
                startTime = 0f;
            }

            return Mathf.Clamp(startTime + Mathf.Max(0f, timelineTime), 0f, clipLength);
        }

        private static float GetStateAnimationSampleTime(StateConfig stateConfig, AnimationClip clip, float timelineTime)
        {
            float clipLength = clip != null ? Mathf.Max(0f, clip.length) : 0f;
            if (clipLength <= 0f)
            {
                return 0f;
            }

            TimelineAnimationConfig animationConfig = GetStateAnimationConfig(stateConfig);
            float startTime = 0f;
            if (animationConfig != null)
            {
                startTime = Mathf.Max(0f, animationConfig.StartTime);
                if (animationConfig.StartTimeUnit == AnimationStartTimeUnit.NormalizedTime)
                {
                    startTime *= clipLength;
                }
            }

            startTime = Mathf.Clamp(startTime, 0f, clipLength);
            float localTime = Mathf.Max(0f, timelineTime);
            float playableDuration = Mathf.Max(0f, clipLength - startTime);
            bool hasDefaultNextState = stateConfig != null && !string.IsNullOrWhiteSpace(stateConfig.DefaultNextStateId);
            if (clip.isLooping && !hasDefaultNextState && playableDuration > 0f)
            {
                return startTime + Mathf.Repeat(localTime, playableDuration);
            }

            return Mathf.Clamp(startTime + localTime, 0f, clipLength);
        }

        private static FadeMode ConvertFadeMode(AnimancerFadeMode fadeMode)
        {
            switch (fadeMode)
            {
                case AnimancerFadeMode.FixedSpeed:
                    return FadeMode.FixedSpeed;
                case AnimancerFadeMode.FromStart:
                    return FadeMode.FromStart;
                case AnimancerFadeMode.NormalizedSpeed:
                    return FadeMode.NormalizedSpeed;
                case AnimancerFadeMode.NormalizedDuration:
                    return FadeMode.NormalizedDuration;
                case AnimancerFadeMode.NormalizedFromStart:
                    return FadeMode.NormalizedFromStart;
                default:
                    return FadeMode.FixedDuration;
            }
        }

        private static void ClearLayerRuntime(AnimationLayerRuntime runtime)
        {
            if (runtime == null)
            {
                return;
            }

            runtime.CurrentState = null;
            runtime.CurrentClip = null;
            runtime.CurrentDirectionalMixer = null;
            runtime.CurrentStateId = string.Empty;
            runtime.CurrentAnimationKey = string.Empty;
            runtime.AppliesRootMotion = false;
            runtime.DirectionalParameter = Vector2.zero;
        }

        private void RefreshRootMotionOwner()
        {
            if (_animator == null)
            {
                return;
            }

            bool applyRootMotion = false;
            if (TryGetLayerRuntime(AnimationLayerType.Action, out AnimationLayerRuntime actionRuntime) && actionRuntime.AppliesRootMotion)
            {
                applyRootMotion = true;
            }
            else if (TryGetLayerRuntime(AnimationLayerType.Locomotion, out AnimationLayerRuntime locomotionRuntime) && locomotionRuntime.AppliesRootMotion)
            {
                applyRootMotion = true;
            }

            _animator.applyRootMotion = applyRootMotion;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogDebug(string message)
        {
            Debug.Log(message, this);
        }
    }
}
#pragma warning restore CS0618
