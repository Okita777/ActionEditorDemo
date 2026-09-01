using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ActionEditor.TagSystem;
using AsiSkillEditor.RunTime;
using SkillEditor.Preview;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SkillEditor.Editor
{
    internal interface ISkillEditorInspectorPanel
    {
        string Header { get; }

        void Bind(EditorWindow owner);

        void OnGUI();
    }

    public sealed class SkillEditorInspectorWindow : EditorWindow
    {
        private ISkillEditorInspectorPanel _panel;
        private Vector2 _scrollPosition;

        internal static void OpenMetaSkill(SkillResourceFileEntry entry)
        {
            OpenWithPanel(new MetaSkillInspectorPanel(entry));
        }

        internal static void OpenSkill(SkillResourceFileEntry entry)
        {
            OpenWithPanel(new SkillInspectorPanel(entry));
        }

        internal static void OpenUnit(SkillResourceFileEntry entry)
        {
            OpenWithPanel(new UnitInspectorPanel(entry));
        }

        internal static void OpenState(SkillResourceFileEntry entry)
        {
            OpenWithPanel(new StateInspectorPanel(entry));
        }

        internal static void OpenBuff(SkillResourceFileEntry entry)
        {
            OpenWithPanel(new BuffInspectorPanel(entry));
        }

        internal static void OpenSkillNodeSelection(SkillResourceFileEntry entry, MetaSkillNodeConfig node, Action onModified)
        {
            if (node == null)
            {
                return;
            }

            OpenWithPanel(new SkillNodeInspectorPanel(entry, node, onModified));
        }

        internal static void OpenSkillEventSelection(SkillResourceFileEntry entry, SkillEventConfig skillEvent, Action onModified)
        {
            if (skillEvent == null)
            {
                return;
            }

            OpenWithPanel(new SkillEventInspectorPanel(entry, skillEvent, onModified));
        }

        internal static void OpenPreviewUnit()
        {
            OpenWithPanel(new PreviewUnitInspectorPanel());
        }

        internal static void OpenTimelineSelection(SkillResourceFileEntry entry, TimelineTrackConfig track, object item, Action onModified)
        {
            OpenWithPanel(new TimelineInspectorPanel(entry, track, item, onModified));
        }

        internal static void OpenStateInterruptSelection(SkillResourceFileEntry entry, StateInterruptTrackConfig track, StateInterruptConfig interrupt, Action onModified)
        {
            if (interrupt == null)
            {
                return;
            }

            OpenWithPanel(new StateInterruptInspectorPanel(entry, track, interrupt, onModified));
        }

        internal static void OpenEffectNodeSelection(SkillResourceFileEntry entry, SkillEffectNodeConfig node, Action onModified)
        {
            if (node == null)
            {
                return;
            }

            OpenWithPanel(new EffectNodeInspectorPanel(entry, node, onModified));
        }

        private static void OpenWithPanel(ISkillEditorInspectorPanel panel)
        {
            SkillEditorInspectorWindow window = GetWindow<SkillEditorInspectorWindow>();
            window.titleContent = new GUIContent("Inspector");
            window.minSize = new Vector2(420f, 320f);
            window.BindPanel(panel);
            window.Show();
        }

        private static string BuildEffectSummary(SkillEffectConfig effectConfig)
        {
            if (effectConfig == null || effectConfig.Nodes == null || effectConfig.Nodes.Count == 0 || string.IsNullOrEmpty(effectConfig.RootNodeId))
            {
                return "空效果树";
            }

            return $"Root={effectConfig.RootNodeId}  Nodes={effectConfig.Nodes.Count}";
        }

        private static void OpenEffectEditor(SkillResourceFileEntry entry, SkillEffectConfig effectConfig, string targetTitle, Action onModified)
        {
            Type windowType = Type.GetType("SkillEditor.Editor.SkillEffectEditorWindow, Assembly-CSharp-Editor");
            if (windowType == null)
            {
                Debug.LogError("SkillEffectEditorWindow 未能加载。");
                return;
            }

            MethodInfo openMethod = windowType.GetMethod("OpenForEffect", BindingFlags.Static | BindingFlags.NonPublic);
            if (openMethod == null)
            {
                Debug.LogError("SkillEffectEditorWindow.OpenForEffect 未找到。");
                return;
            }

            openMethod.Invoke(null, new object[] { entry, effectConfig, targetTitle, onModified });
        }

        private static void OpenSkillGraphWindow(SkillResourceFileEntry entry)
        {
            Type windowType = Type.GetType("SkillEditor.Editor.SkillGraphEditorWindow, Assembly-CSharp-Editor");
            if (windowType == null)
            {
                Debug.LogError("SkillGraphEditorWindow 未能加载。");
                return;
            }

            MethodInfo openMethod = windowType.GetMethod("OpenForEntry", BindingFlags.Static | BindingFlags.NonPublic);
            if (openMethod == null)
            {
                Debug.LogError("SkillGraphEditorWindow.OpenForEntry 未找到。");
                return;
            }

            openMethod.Invoke(null, new object[] { entry });
        }

        private void BindPanel(ISkillEditorInspectorPanel panel)
        {
            _panel = panel;
            _panel?.Bind(this);
            Repaint();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_panel == null)
            {
                EditorGUILayout.HelpBox("当前没有可显示的 Inspector 内容。", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.LabelField(_panel.Header, EditorStyles.boldLabel);
            EditorGUILayout.Space(6f);
            _panel.OnGUI();

            EditorGUILayout.EndScrollView();
        }

        private sealed class SkillInspectorPanel : ISkillEditorInspectorPanel
        {
            private readonly SkillResourceFileEntry _entry;
            private readonly SkillConfig _config;

            public SkillInspectorPanel(SkillResourceFileEntry entry)
            {
                _entry = entry;
                _config = entry != null ? entry.Config as SkillConfig : null;
                EnsureDefaultLayer();
            }

            public string Header => "Skill";

            public void Bind(EditorWindow owner)
            {
            }

            public void OnGUI()
            {
                if (_config == null)
                {
                    EditorGUILayout.HelpBox("当前没有选中的 Skill。", MessageType.Info);
                    return;
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginVertical("box");
                _config.SkillId = EditorGUILayout.TextField("skillId", _config.SkillId);
                _config.SkillName = EditorGUILayout.TextField("skillName", _config.SkillName);
                SkillCastCategory skillCategory = (SkillCastCategory)EditorGUILayout.EnumPopup("skillType", _config.SkillCategory);
                if (skillCategory != _config.SkillCategory)
                {
                    _config.SkillCategory = skillCategory;
                }
                _config.Cooldown = Mathf.Max(0f, EditorGUILayout.FloatField("cd", _config.Cooldown));
                _config.ComboContinuationTimeout = Mathf.Max(0f, EditorGUILayout.FloatField("comboTimeout", _config.ComboContinuationTimeout));
                DrawResourceCosts();
                EditorGUILayout.LabelField("layers", _config.Layers != null ? _config.Layers.Count.ToString() : "0");
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(8f);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Tags", EditorStyles.boldLabel);
                if (TagSelectionEditorUtility.DrawTagContainer("Skill Tags", _config.Tags))
                {
                    SkillResourceRepository.MarkDirty(_entry);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(10f);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("OnUpdate", EditorStyles.boldLabel);
                if (GUILayout.Button("SkillEditor", GUILayout.Height(40f)))
                {
                    OpenSkillGraphWindow(_entry);
                }
                EditorGUILayout.EndVertical();

                if (EditorGUI.EndChangeCheck())
                {
                    SkillResourceRepository.MarkDirty(_entry);
                }
            }

            private void EnsureDefaultLayer()
            {
                if (_config == null)
                {
                    return;
                }

                if (_config.Layers == null)
                {
                    _config.Layers = new List<SkillLayerConfig>();
                }

                if (_config.Layers.Count == 0)
                {
                    _config.Layers.Add(new SkillLayerConfig());
                    SkillResourceRepository.MarkDirty(_entry);
                }
            }

            private void DrawResourceCosts()
            {
                _config.ResourceCosts ??= new List<SkillResourceCostConfig>();
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("costs", EditorStyles.boldLabel);

                for (int i = 0; i < _config.ResourceCosts.Count; i++)
                {
                    SkillResourceCostConfig cost = _config.ResourceCosts[i] ?? (_config.ResourceCosts[i] = new SkillResourceCostConfig());
                    EditorGUILayout.BeginHorizontal();
                    cost.ResourceType = (SkillCostResourceType)EditorGUILayout.EnumPopup(cost.ResourceType, GUILayout.Width(140f));
                    cost.Amount = Mathf.Max(0f, EditorGUILayout.FloatField(cost.Amount));
                    if (GUILayout.Button("删除", GUILayout.Width(52f)))
                    {
                        _config.ResourceCosts.RemoveAt(i);
                        GUIUtility.ExitGUI();
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("新增 cost"))
                {
                    _config.ResourceCosts.Add(new SkillResourceCostConfig());
                }
            }
        }

        private sealed class UnitInspectorPanel : ISkillEditorInspectorPanel
        {
            private readonly SkillResourceFileEntry _entry;
            private readonly UnitConfig _config;

            public UnitInspectorPanel(SkillResourceFileEntry entry)
            {
                _entry = entry;
                _config = entry != null ? entry.Config as UnitConfig : null;
            }

            public string Header => "Unit";

            public void Bind(EditorWindow owner)
            {
            }

            public void OnGUI()
            {
                if (_config == null)
                {
                    EditorGUILayout.HelpBox("当前没有选中的 Unit。", MessageType.Info);
                    return;
                }

                EditorGUI.BeginChangeCheck();
                UnitConfigEditorUtility.Draw(_config);
                if (EditorGUI.EndChangeCheck())
                {
                    SkillResourceRepository.MarkDirty(_entry);
                }
            }
        }

        private static void DrawMovementProfile(StateConfig state, Action markDirty)
        {
            if (state == null)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("movement policy", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            state.AffectsLocomotion = EditorGUILayout.Toggle("affectsLocomotion", state.AffectsLocomotion);
            if (EditorGUI.EndChangeCheck())
            {
                markDirty?.Invoke();
            }

            if (!state.AffectsLocomotion)
            {
                EditorGUILayout.HelpBox("该状态不提交运动策略；其他活动状态（通常是 Locomotion 层）继续控制角色运动。", MessageType.Info);
                return;
            }

            state.MovementProfile ??= StateMovementProfile.CreateDefault();
            StateMovementProfile profile = state.MovementProfile;
            EditorGUI.BeginChangeCheck();

            profile.TranslationMode = (StateTranslationMode)EditorGUILayout.EnumPopup("translationMode", profile.TranslationMode);
            profile.RotationMode = (StateRotationMode)EditorGUILayout.EnumPopup("rotationMode", profile.RotationMode);
            bool usesInputTranslation = profile.TranslationMode == StateTranslationMode.Input ||
                                        profile.TranslationMode == StateTranslationMode.Hybrid;
            if (usesInputTranslation)
            {
                profile.InputSpeedMultiplier = Mathf.Max(0f, EditorGUILayout.FloatField("inputSpeedMultiplier", profile.InputSpeedMultiplier));
                profile.AccelerationMultiplier = Mathf.Max(0f, EditorGUILayout.FloatField("accelerationMultiplier", profile.AccelerationMultiplier));
                profile.DecelerationMultiplier = Mathf.Max(0f, EditorGUILayout.FloatField("decelerationMultiplier", profile.DecelerationMultiplier));
            }
            else if (profile.TranslationMode == StateTranslationMode.RootMotion)
            {
                EditorGUILayout.HelpBox("当前平移由动画 Root Motion 驱动。Unit 最大移动速度、地面加减速和 inputSpeedMultiplier 均不会控制该状态的位移速度。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("当前状态锁定平移，不读取 Unit/Input 移动速度。", MessageType.Info);
            }

            if (profile.TranslationMode == StateTranslationMode.RootMotion || profile.TranslationMode == StateTranslationMode.Hybrid)
            {
                profile.RootMotionForwardWeight = Mathf.Max(0f, EditorGUILayout.FloatField("rootMotionForwardWeight", profile.RootMotionForwardWeight));
                profile.RootMotionSideWeight = Mathf.Max(0f, EditorGUILayout.FloatField("rootMotionSideWeight", profile.RootMotionSideWeight));
                profile.RootMotionVerticalWeight = Mathf.Max(0f, EditorGUILayout.FloatField("rootMotionVerticalWeight", profile.RootMotionVerticalWeight));
                profile.AllowBackwardRootMotion = EditorGUILayout.Toggle("allowBackwardRootMotion", profile.AllowBackwardRootMotion);
                if (!profile.AllowBackwardRootMotion)
                {
                    EditorGUILayout.HelpBox("过滤角色局部 Z 轴上的负向 Root Motion。适合 Run End 等只允许向前制动、不允许根骨回弹带动角色后退的动画。", MessageType.Info);
                }
            }

            if (profile.RotationMode == StateRotationMode.RootMotion)
            {
                profile.RootMotionRotationWeight = Mathf.Clamp01(EditorGUILayout.Slider("rootMotionRotationWeight", profile.RootMotionRotationWeight, 0f, 1f));
            }
            else if (profile.RotationMode == StateRotationMode.KeepCurrent)
            {
                EditorGUILayout.HelpBox("RotationMode=KeepCurrent 时，Input 平移仍生效，但不会提交移动朝向，角色保持当前朝向。", MessageType.Info);
            }
            else if (profile.RotationMode == StateRotationMode.CameraForward)
            {
                EditorGUILayout.HelpBox("RotationMode=CameraForward 时，角色持续朝向相机平面正方向；移动仍使用相机相对输入。DirectionalMixer2D 中 S 稳定对应 (0, -1)。", MessageType.Info);
            }

            bool animationAppliesRootMotion = state.AnimationProfile != null && state.AnimationProfile.ApplyRootMotion;
            if (profile.TranslationMode == StateTranslationMode.RootMotion && !animationAppliesRootMotion)
            {
                EditorGUILayout.HelpBox("TranslationMode 是 RootMotion，但动画未开启 applyRootMotion：该状态不会获得平移。", MessageType.Error);
            }
            else if (profile.TranslationMode == StateTranslationMode.Input && animationAppliesRootMotion)
            {
                EditorGUILayout.HelpBox("TranslationMode 是 Input，动画 Root Motion 平移不会参与 KCC。建议关闭 applyRootMotion，避免配置语义混乱。", MessageType.Warning);
            }

            profile.MaxTurnSpeed = Mathf.Max(0f, EditorGUILayout.FloatField("maxTurnSpeed", profile.MaxTurnSpeed));
            profile.AllowGravity = EditorGUILayout.Toggle("allowGravity", profile.AllowGravity);
            profile.AirControl = Mathf.Max(0f, EditorGUILayout.FloatField("airControl", profile.AirControl));

            if (EditorGUI.EndChangeCheck())
            {
                markDirty?.Invoke();
            }
        }

        private static void DrawLocomotionSpeedMatching(StateAnimationProfile profile, Action markDirty)
        {
            if (profile == null || profile.OutputLayer != AnimationLayerType.Locomotion || profile.ApplyRootMotion)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            profile.MatchLocomotionSpeed = EditorGUILayout.Toggle("matchLocomotionSpeed", profile.MatchLocomotionSpeed);
            if (profile.MatchLocomotionSpeed)
            {
                profile.AuthoredMoveSpeed = Mathf.Max(0.01f, EditorGUILayout.FloatField("authoredMoveSpeed", profile.AuthoredMoveSpeed));
                profile.MinLocomotionPlaybackSpeed = Mathf.Max(0f, EditorGUILayout.FloatField("minPlaybackSpeed", profile.MinLocomotionPlaybackSpeed));
                profile.MaxLocomotionPlaybackSpeed = Mathf.Max(
                    profile.MinLocomotionPlaybackSpeed,
                    EditorGUILayout.FloatField("maxPlaybackSpeed", profile.MaxLocomotionPlaybackSpeed));
                profile.LocomotionSpeedMatchSharpness = Mathf.Max(0f, EditorGUILayout.FloatField("speedMatchSharpness", profile.LocomotionSpeedMatchSharpness));
                profile.LocomotionSpeedMatchDeadZone = Mathf.Max(0f, EditorGUILayout.FloatField("speedMatchDeadZone", profile.LocomotionSpeedMatchDeadZone));
                EditorGUILayout.HelpBox("播放倍率 = animSpeed × Clamp(KCC Locomotion 驱动速度 / authoredMoveSpeed)。碰墙不会拖慢 Run；Idle 不要启用。", MessageType.Info);
            }

            if (EditorGUI.EndChangeCheck())
            {
                markDirty?.Invoke();
            }
        }

        private static readonly string[] DirectionalSlotLabels =
        {
            "Idle",
            "Forward",
            "ForwardRight",
            "Right",
            "BackRight",
            "Back",
            "BackLeft",
            "Left",
            "ForwardLeft",
        };

        private static bool DrawStateAnimationModeAndDirectionalConfig(
            StateConfig stateConfig,
            Action markDirty,
            Action repaint,
            Func<string, AnimationClip> clipLoader)
        {
            if (stateConfig == null)
            {
                return false;
            }

            stateConfig.DirectionalMixer2D ??= StateDirectionalMixer2DConfig.CreateDefault();
            StateAnimationMode nextMode = (StateAnimationMode)EditorGUILayout.EnumPopup("animMode", stateConfig.AnimationMode);
            if (nextMode != stateConfig.AnimationMode)
            {
                stateConfig.AnimationMode = nextMode;
                markDirty?.Invoke();
            }

            if (stateConfig.AnimationMode == StateAnimationMode.SingleClip)
            {
                return true;
            }

            DrawDirectionalMixer2DConfig(stateConfig, markDirty, repaint, clipLoader);
            return false;
        }

        private static void DrawDirectionalMixer2DConfig(
            StateConfig stateConfig,
            Action markDirty,
            Action repaint,
            Func<string, AnimationClip> clipLoader)
        {
            if (stateConfig == null)
            {
                return;
            }

            StateDirectionalMixer2DConfig config = stateConfig.DirectionalMixer2D ?? StateDirectionalMixer2DConfig.CreateDefault();
            stateConfig.DirectionalMixer2D = config;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("DirectionalMixer2D", EditorStyles.boldLabel);
            DrawDirectionalSlotField("Idle", () => config.IdleClipPath, value => config.IdleClipPath = value, ref config.IdleThreshold, markDirty, repaint, clipLoader);
            DrawDirectionalSlotField("Forward", () => config.ForwardClipPath, value => config.ForwardClipPath = value, ref config.ForwardThreshold, markDirty, repaint, clipLoader);
            DrawDirectionalSlotField("ForwardRight", () => config.ForwardRightClipPath, value => config.ForwardRightClipPath = value, ref config.ForwardRightThreshold, markDirty, repaint, clipLoader);
            DrawDirectionalSlotField("Right", () => config.RightClipPath, value => config.RightClipPath = value, ref config.RightThreshold, markDirty, repaint, clipLoader);
            DrawDirectionalSlotField("BackRight", () => config.BackRightClipPath, value => config.BackRightClipPath = value, ref config.BackRightThreshold, markDirty, repaint, clipLoader);
            DrawDirectionalSlotField("Back", () => config.BackClipPath, value => config.BackClipPath = value, ref config.BackThreshold, markDirty, repaint, clipLoader);
            DrawDirectionalSlotField("BackLeft", () => config.BackLeftClipPath, value => config.BackLeftClipPath = value, ref config.BackLeftThreshold, markDirty, repaint, clipLoader);
            DrawDirectionalSlotField("Left", () => config.LeftClipPath, value => config.LeftClipPath = value, ref config.LeftThreshold, markDirty, repaint, clipLoader);
            DrawDirectionalSlotField("ForwardLeft", () => config.ForwardLeftClipPath, value => config.ForwardLeftClipPath = value, ref config.ForwardLeftThreshold, markDirty, repaint, clipLoader);

            float smoothSpeed = Mathf.Max(0f, EditorGUILayout.FloatField("paramSmoothSpeed", config.ParameterSmoothSpeed));
            if (!Mathf.Approximately(smoothSpeed, config.ParameterSmoothSpeed))
            {
                config.ParameterSmoothSpeed = smoothSpeed;
                markDirty?.Invoke();
            }

            string missingSlots = BuildDirectionalMissingSlotsSummary(config, clipLoader);
            if (!string.IsNullOrEmpty(missingSlots))
            {
                EditorGUILayout.HelpBox($"DirectionalMixer2D 缺少有效动画槽: {missingSlots}", MessageType.Warning);
            }
        }

        private static void DrawDirectionalSlotField(
            string slotLabel,
            Func<string> getClipPath,
            Action<string> setClipPath,
            ref SerializableVector2 threshold,
            Action markDirty,
            Action repaint,
            Func<string, AnimationClip> clipLoader)
        {
            string clipPath = getClipPath != null ? getClipPath() : string.Empty;
            AnimationClip current = string.IsNullOrEmpty(clipPath) ? null : clipLoader?.Invoke(clipPath);
            Rect rowRect = EditorGUILayout.GetControlRect(false, 40f);
            Rect labelRect = new Rect(rowRect.x, rowRect.y, 92f, rowRect.height);
            Rect fieldRect = new Rect(rowRect.x + 96f, rowRect.y, rowRect.width - 228f, rowRect.height);
            Rect pickRect = new Rect(rowRect.xMax - 124f, rowRect.y, 60f, rowRect.height);
            Rect clearRect = new Rect(rowRect.xMax - 60f, rowRect.y, 60f, rowRect.height);

            EditorGUI.LabelField(labelRect, slotLabel);
            string displayText = current != null ? current.name : "拖入/选择动画";
            EditorGUI.HelpBox(fieldRect, displayText, MessageType.None);

            Event evt = Event.current;
            if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && fieldRect.Contains(evt.mousePosition))
            {
                bool canAccept = SkillAnimationSelectionUtility.TryExtractClipFromDrag(DragAndDrop.objectReferences, out AnimationClip clip, out string errorMessage);
                DragAndDrop.visualMode = canAccept ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    if (canAccept)
                    {
                        string nextPath = SkillAnimationSelectionUtility.SerializeAllowedClip(clip);
                        if (!string.Equals(nextPath, clipPath, StringComparison.Ordinal))
                        {
                            setClipPath?.Invoke(nextPath);
                            markDirty?.Invoke();
                        }
                    }
                    else if (!string.IsNullOrEmpty(errorMessage))
                    {
                        EditorUtility.DisplayDialog("动画选择无效", errorMessage, "确定");
                    }
                }

                evt.Use();
            }

            if (GUI.Button(pickRect, "选择"))
            {
                SkillAnimationPickerWindow.Open(path =>
                {
                    string nextPath = path ?? string.Empty;
                    if (!string.Equals(nextPath, clipPath, StringComparison.Ordinal))
                    {
                        setClipPath?.Invoke(nextPath);
                        markDirty?.Invoke();
                    }

                    repaint?.Invoke();
                });
            }

            if (GUI.Button(clearRect, "清空"))
            {
                if (!string.IsNullOrEmpty(clipPath))
                {
                    setClipPath?.Invoke(string.Empty);
                    markDirty?.Invoke();
                }
            }

            Vector2 currentThreshold = threshold;
            Vector2 nextThreshold = EditorGUILayout.Vector2Field($"{slotLabel} threshold", currentThreshold);
            if (nextThreshold != currentThreshold)
            {
                threshold = nextThreshold;
                markDirty?.Invoke();
            }
        }

        private static string BuildDirectionalMissingSlotsSummary(StateDirectionalMixer2DConfig config, Func<string, AnimationClip> clipLoader)
        {
            if (config == null)
            {
                return string.Join(", ", DirectionalSlotLabels);
            }

            StringBuilder builder = new StringBuilder();
            AppendMissingSlot(builder, "Idle", config.IdleClipPath, clipLoader);
            AppendMissingSlot(builder, "Forward", config.ForwardClipPath, clipLoader);
            AppendMissingSlot(builder, "ForwardRight", config.ForwardRightClipPath, clipLoader);
            AppendMissingSlot(builder, "Right", config.RightClipPath, clipLoader);
            AppendMissingSlot(builder, "BackRight", config.BackRightClipPath, clipLoader);
            AppendMissingSlot(builder, "Back", config.BackClipPath, clipLoader);
            AppendMissingSlot(builder, "BackLeft", config.BackLeftClipPath, clipLoader);
            AppendMissingSlot(builder, "Left", config.LeftClipPath, clipLoader);
            AppendMissingSlot(builder, "ForwardLeft", config.ForwardLeftClipPath, clipLoader);
            return builder.ToString();
        }

        private static void AppendMissingSlot(StringBuilder builder, string slotName, string clipPath, Func<string, AnimationClip> clipLoader)
        {
            bool missing = string.IsNullOrWhiteSpace(clipPath) || clipLoader == null || clipLoader(clipPath) == null;
            if (!missing)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(slotName);
        }

        private sealed class MetaSkillInspectorPanel : ISkillEditorInspectorPanel
        {
            private readonly SkillResourceFileEntry _entry;
            private readonly MetaSkillConfig _config;
            private EditorWindow _owner;

            public MetaSkillInspectorPanel(SkillResourceFileEntry entry)
            {
                _entry = entry;
                _config = entry != null ? entry.Config as MetaSkillConfig : null;
            }

            public string Header => "MetaSkill";

            public void Bind(EditorWindow owner)
            {
                _owner = owner;
            }

            public void OnGUI()
            {
                if (_config == null)
                {
                    EditorGUILayout.HelpBox("当前没有选中的 MetaSkill。", MessageType.Info);
                    return;
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginVertical("box");
                _config.MetaSkillId = EditorGUILayout.TextField("metaSkillId", _config.MetaSkillId);
                _config.MetaSkillName = EditorGUILayout.TextField("metaSkillName", _config.MetaSkillName);
                if (TagSelectionEditorUtility.DrawTagContainer("tag", _config.Tags))
                {
                    SkillResourceRepository.MarkDirty(_entry);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(10f);
                DrawEmbeddedStateSection("skill state", ref _config.SkillStateTimeLineState, false);

                EditorGUILayout.Space(10f);
                DrawEmbeddedStateSection("recovery state", ref _config.RecoverySkillStateTimeLineState, true);

                EditorGUILayout.Space(10f);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);
                if (GUILayout.Button("OnAdd    effects(BT)", GUILayout.Height(34f)))
                {
                    _config.OnAddEffect ??= new SkillEffectConfig();
                    OpenEffectEditor(_entry, _config.OnAddEffect, "MetaSkill / OnAdd", MarkDirtyAndRepaint);
                }
                EditorGUILayout.LabelField(BuildEffectSummary(_config.OnAddEffect), EditorStyles.miniLabel);

                if (GUILayout.Button("OnEnd    effects(BT)", GUILayout.Height(34f)))
                {
                    _config.OnEndEffect ??= new SkillEffectConfig();
                    OpenEffectEditor(_entry, _config.OnEndEffect, "MetaSkill / OnEnd", MarkDirtyAndRepaint);
                }
                EditorGUILayout.LabelField(BuildEffectSummary(_config.OnEndEffect), EditorStyles.miniLabel);

                EditorGUILayout.EndVertical();

                if (EditorGUI.EndChangeCheck())
                {
                    SkillResourceRepository.MarkDirty(_entry);
                }
            }

            private static string FormatPreviewValue(string value)
            {
                return string.IsNullOrEmpty(value) ? "未设置" : value;
            }

            private void DrawEmbeddedStateSection(string title, ref StateConfig stateConfig, bool recoveryPhase)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

                bool hasState = stateConfig != null;
                bool enabled = EditorGUILayout.Toggle("enabled", hasState);
                if (enabled != hasState)
                {
                    stateConfig = enabled ? CreateDefaultEmbeddedState(recoveryPhase) : null;
                    SyncEmbeddedStateIdentity(recoveryPhase);
                    SkillResourceRepository.MarkDirty(_entry);
                }

                if (stateConfig == null)
                {
                    EditorGUILayout.HelpBox("未配置该阶段 State。", MessageType.Info);
                    EditorGUILayout.EndVertical();
                    return;
                }

                SyncEmbeddedStateIdentity(recoveryPhase);
                DrawStateLayerSettings(stateConfig);
                DrawEmbeddedStateAnimationField(stateConfig, "anim");
                DrawEmbeddedStateAnimationTransitionSettings(
                    stateConfig,
                    recoveryPhase ? "recovery transition" : "skill transition");

                DrawEmbeddedDefaultNextStateField(stateConfig);

                if (TagSelectionEditorUtility.DrawTagContainer("tag", stateConfig.Tags))
                {
                    SkillResourceRepository.MarkDirty(_entry);
                }

                if (GUILayout.Button("timeline", GUILayout.Height(34f)))
                {
                    OpenEmbeddedStateTimeline(stateConfig, title);
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawEmbeddedStateAnimationField(StateConfig stateConfig, string label)
            {
                bool drawSingleClipPicker = DrawStateAnimationModeAndDirectionalConfig(
                    stateConfig,
                    () => SkillResourceRepository.MarkDirty(_entry),
                    () => _owner?.Repaint(),
                    path => string.IsNullOrEmpty(path) ? null : SkillAnimationReferenceUtility.LoadClip(path));
                if (!drawSingleClipPicker)
                {
                    return;
                }

                AnimationClip current = string.IsNullOrEmpty(stateConfig.AnimationClipPath)
                    ? null
                    : SkillAnimationReferenceUtility.LoadClip(stateConfig.AnimationClipPath);

                GameUnit previewConfig = SkillPreviewUnitSettings.LoadActivePreviewConfig();
                EditorGUILayout.LabelField(label);
                Rect rowRect = EditorGUILayout.GetControlRect(false, 42f);
                Rect fieldRect = new Rect(rowRect.x, rowRect.y, rowRect.width - 130f, rowRect.height);
                Rect pickRect = new Rect(rowRect.xMax - 124f, rowRect.y, 60f, rowRect.height);
                Rect clearRect = new Rect(rowRect.xMax - 60f, rowRect.y, 60f, rowRect.height);

                string displayText = current != null ? current.name : "拖入 AnimationClip 或点击选择";
                EditorGUI.HelpBox(fieldRect, displayText, MessageType.None);

                Event currentEvent = Event.current;
                if ((currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform) && fieldRect.Contains(currentEvent.mousePosition))
                {
                    bool canAccept = SkillAnimationSelectionUtility.TryExtractClipFromDrag(DragAndDrop.objectReferences, out AnimationClip clip, out string errorMessage);
                    DragAndDrop.visualMode = canAccept ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                    if (currentEvent.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        if (canAccept)
                        {
                            stateConfig.AnimationClipPath = SkillAnimationSelectionUtility.SerializeAllowedClip(clip);
                            EnsureEmbeddedStateAnimationConfig(stateConfig);
                            SkillResourceRepository.MarkDirty(_entry);
                        }
                        else if (!string.IsNullOrEmpty(errorMessage))
                        {
                            EditorUtility.DisplayDialog("动画选择无效", errorMessage, "确定");
                        }
                    }

                    currentEvent.Use();
                }

                if (GUI.Button(pickRect, "选择"))
                {
                    SkillAnimationPickerWindow.Open(animationPath =>
                    {
                        stateConfig.AnimationClipPath = animationPath ?? string.Empty;
                        EnsureEmbeddedStateAnimationConfig(stateConfig);
                        SkillResourceRepository.MarkDirty(_entry);
                        _owner?.Repaint();
                    });
                }

                if (GUI.Button(clearRect, "清空"))
                {
                    stateConfig.AnimationClipPath = string.Empty;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                if (previewConfig != null)
                {
                    EditorGUILayout.LabelField(
                        $"筛选: root={FormatPreviewValue(previewConfig.AnimationSearchRoot)} | key={FormatPreviewValue(previewConfig.AnimationFilterKey)}",
                        EditorStyles.miniLabel);
                }
            }

            private void DrawEmbeddedStateAnimationTransitionSettings(StateConfig stateConfig, string header)
            {
                TimelineAnimationConfig animationConfig = EnsureEmbeddedStateAnimationConfig(stateConfig);
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

                float transitionDuration = Mathf.Max(0f, EditorGUILayout.FloatField("transition duration", animationConfig.TransitionDuration));
                if (!Mathf.Approximately(transitionDuration, animationConfig.TransitionDuration))
                {
                    animationConfig.TransitionDuration = transitionDuration;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                AnimationTransitionTimeUnit transitionTimeUnit = (AnimationTransitionTimeUnit)EditorGUILayout.EnumPopup("transition time unit", animationConfig.TransitionTimeUnit);
                if (transitionTimeUnit != animationConfig.TransitionTimeUnit)
                {
                    animationConfig.TransitionTimeUnit = transitionTimeUnit;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                AnimancerFadeMode fadeMode = (AnimancerFadeMode)EditorGUILayout.EnumPopup("fade mode", animationConfig.FadeMode);
                if (fadeMode != animationConfig.FadeMode)
                {
                    animationConfig.FadeMode = fadeMode;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                float startTime = Mathf.Max(0f, EditorGUILayout.FloatField("start time", animationConfig.StartTime));
                if (!Mathf.Approximately(startTime, animationConfig.StartTime))
                {
                    animationConfig.StartTime = startTime;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                AnimationStartTimeUnit startTimeUnit = (AnimationStartTimeUnit)EditorGUILayout.EnumPopup("start time unit", animationConfig.StartTimeUnit);
                if (startTimeUnit != animationConfig.StartTimeUnit)
                {
                    animationConfig.StartTimeUnit = startTimeUnit;
                    SkillResourceRepository.MarkDirty(_entry);
                }
            }

            private string DrawStatePopup(string label, string currentStateId, List<SkillResourceFileEntry> stateEntries, bool includeEmpty, StateLayerType filterLayer, string excludedStateId = "")
            {
                List<StateConfig> candidateStates = new List<StateConfig>();
                for (int i = 0; i < (stateEntries != null ? stateEntries.Count : 0); i++)
                {
                    StateConfig config = stateEntries[i] != null ? stateEntries[i].Config as StateConfig : null;
                    if (config == null || string.IsNullOrWhiteSpace(config.StateId))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(excludedStateId) && string.Equals(config.StateId, excludedStateId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (config.Layer != filterLayer)
                    {
                        continue;
                    }

                    candidateStates.Add(config);
                }

                int baseOffset = includeEmpty ? 1 : 0;
                int optionCount = candidateStates.Count + baseOffset;
                string[] optionLabels = new string[Mathf.Max(1, optionCount)];
                string[] optionValues = new string[Mathf.Max(1, optionCount)];
                int currentIndex = 0;

                if (includeEmpty)
                {
                    optionLabels[0] = "未设置";
                    optionValues[0] = string.Empty;
                }

                for (int i = 0; i < candidateStates.Count; i++)
                {
                    StateConfig config = candidateStates[i];
                    string stateId = config.StateId;
                    string stateName = config.StateName;
                    int optionIndex = i + baseOffset;
                    optionLabels[optionIndex] = string.IsNullOrEmpty(stateName) ? stateId : $"{stateName} ({stateId})";
                    optionValues[optionIndex] = stateId;
                    if (!string.IsNullOrEmpty(stateId) && string.Equals(currentStateId, stateId, StringComparison.Ordinal))
                    {
                        currentIndex = optionIndex;
                    }
                }

                if (optionLabels.Length == 0)
                {
                    optionLabels = new[] { "无可选状态" };
                    optionValues = new[] { string.Empty };
                }

                int nextIndex = EditorGUILayout.Popup(label, currentIndex, optionLabels);
                return nextIndex >= 0 && nextIndex < optionValues.Length ? optionValues[nextIndex] : string.Empty;
            }

            private StateConfig CreateDefaultEmbeddedState(bool recoveryPhase)
            {
                return new StateConfig
                {
                    Layer = StateLayerType.Action,
                    AffectsLocomotion = true,
                    MovementProfile = StateMovementProfile.CreateLocked(),
                    AnimationProfile = new StateAnimationProfile
                    {
                        OutputLayer = AnimationLayerType.Action,
                        OverrideLowerLayers = true,
                    },
                    PresentationMode = StatePresentationMode.FullBodyOverride,
                    PrimaryAnimationSlot = StateAnimationSlot.Action,
                    Timeline = new StateTimelineConfig
                    {
                        Animation = new TimelineAnimationConfig(),
                    },
                    Tags = new TagContainer(),
                };
            }

            private void SyncEmbeddedStateIdentity(bool recoveryPhase)
            {
                StateConfig target = recoveryPhase ? _config.RecoverySkillStateTimeLineState : _config.SkillStateTimeLineState;
                if (target == null)
                {
                    return;
                }

                string suffix = recoveryPhase ? "_recovery" : "_skill";
                target.StateId = string.IsNullOrEmpty(_config.MetaSkillId) ? suffix.TrimStart('_') : _config.MetaSkillId + suffix;
                target.StateName = string.IsNullOrEmpty(_config.MetaSkillName)
                    ? (recoveryPhase ? "RecoveryState" : "SkillState")
                    : _config.MetaSkillName + (recoveryPhase ? "_RecoveryState" : "_SkillState");
                target.AnimationProfile ??= new StateAnimationProfile();

                target.Timeline ??= new StateTimelineConfig();
                target.Timeline.Animation ??= new TimelineAnimationConfig();
                target.Tags ??= new TagContainer();
            }

            private void DrawEmbeddedDefaultNextStateField(StateConfig stateConfig)
            {
                if (stateConfig == null)
                {
                    return;
                }

                string unitId = _entry != null ? _entry.UnitId : string.Empty;
                List<SkillResourceFileEntry> stateEntries = SkillResourceRepository.LoadStates(unitId);
                string nextStateId = DrawStatePopup("defaultNextState", stateConfig.DefaultNextStateId, stateEntries, includeEmpty: true, stateConfig.Layer, stateConfig.StateId);
                if (!string.Equals(stateConfig.DefaultNextStateId, nextStateId, StringComparison.Ordinal))
                {
                    stateConfig.DefaultNextStateId = nextStateId;
                    SkillResourceRepository.MarkDirty(_entry);
                }
            }

            private static TimelineAnimationConfig EnsureEmbeddedStateAnimationConfig(StateConfig stateConfig)
            {
                stateConfig.Timeline ??= new StateTimelineConfig();
                stateConfig.Timeline.Animation ??= new TimelineAnimationConfig();
                stateConfig.Timeline.Tracks ??= new List<TimelineTrackConfig>();
                stateConfig.Timeline.InterruptTracks ??= new List<StateInterruptTrackConfig>();
                stateConfig.Timeline.Interrupts ??= new List<StateInterruptConfig>();
                return stateConfig.Timeline.Animation;
            }

            private void DrawStateLayerSettings(StateConfig stateConfig)
            {
                if (stateConfig == null)
                {
                    return;
                }

                stateConfig.AnimationProfile ??= new StateAnimationProfile();
                stateConfig.MovementProfile ??= StateMovementProfile.CreateLocked();

                StateLayerType nextStateLayer = (StateLayerType)EditorGUILayout.EnumPopup("stateLayer", stateConfig.Layer);
                if (nextStateLayer != stateConfig.Layer)
                {
                    stateConfig.Layer = nextStateLayer;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                AnimationLayerType nextAnimationLayer = (AnimationLayerType)EditorGUILayout.EnumPopup("animLayer", stateConfig.AnimationProfile.OutputLayer);
                if (nextAnimationLayer != stateConfig.AnimationProfile.OutputLayer)
                {
                    stateConfig.AnimationProfile.OutputLayer = nextAnimationLayer;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                float layerWeight = Mathf.Clamp01(EditorGUILayout.Slider("layerWeight", stateConfig.AnimationProfile.LayerWeight, 0f, 1f));
                if (!Mathf.Approximately(layerWeight, stateConfig.AnimationProfile.LayerWeight))
                {
                    stateConfig.AnimationProfile.LayerWeight = layerWeight;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                float speed = Mathf.Max(0f, EditorGUILayout.FloatField("animSpeed", stateConfig.AnimationProfile.Speed));
                if (!Mathf.Approximately(speed, stateConfig.AnimationProfile.Speed))
                {
                    stateConfig.AnimationProfile.Speed = speed;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                bool applyRootMotion = EditorGUILayout.Toggle("applyRootMotion", stateConfig.AnimationProfile.ApplyRootMotion);
                if (applyRootMotion != stateConfig.AnimationProfile.ApplyRootMotion)
                {
                    stateConfig.AnimationProfile.ApplyRootMotion = applyRootMotion;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                DrawLocomotionSpeedMatching(stateConfig.AnimationProfile, () => SkillResourceRepository.MarkDirty(_entry));

                DrawMovementProfile(stateConfig, () => SkillResourceRepository.MarkDirty(_entry));
            }

            private void MarkDirtyAndRepaint()
            {
                SkillResourceRepository.MarkDirty(_entry);
                _owner?.Repaint();
            }

            private void OpenEmbeddedStateTimeline(StateConfig stateConfig, string title)
            {
                Type windowType = typeof(SkillEditorInspectorWindow).Assembly.GetType("SkillEditor.Editor.StateTimelineEditorWindow");
                if (windowType == null)
                {
                    Debug.LogError("StateTimelineEditorWindow 未能加载。");
                    return;
                }

                MethodInfo openMethod = windowType.GetMethod("OpenForEmbeddedState", BindingFlags.Static | BindingFlags.NonPublic);
                if (openMethod == null)
                {
                    Debug.LogError("StateTimelineEditorWindow.OpenForEmbeddedState 未找到。");
                    return;
                }

                openMethod.Invoke(null, new object[] { _entry, stateConfig, title, (Action)MarkDirtyAndRepaint });
            }
        }

        private sealed class StateInspectorPanel : ISkillEditorInspectorPanel
        {
            private readonly SkillResourceFileEntry _entry;
            private readonly StateConfig _config;
            private EditorWindow _owner;

            public StateInspectorPanel(SkillResourceFileEntry entry)
            {
                _entry = entry;
                _config = entry != null ? entry.Config as StateConfig : null;
            }

            public string Header => "State";

            public void Bind(EditorWindow owner)
            {
                _owner = owner;
            }

            public void OnGUI()
            {
                if (_config == null)
                {
                    EditorGUILayout.HelpBox("当前没有选中的 State。", MessageType.Info);
                    return;
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginVertical("box");
                _config.StateId = EditorGUILayout.TextField("stateId", _config.StateId);
                _config.StateName = EditorGUILayout.TextField("stateName", _config.StateName);
                DrawStateLayerSettings();
                DrawDefaultNextStateField();
                if (TagSelectionEditorUtility.DrawTagContainer("State Tags", _config.Tags))
                {
                    SkillResourceRepository.MarkDirty(_entry);
                }
                DrawAnimationField("state anim");
                DrawAnimationTransitionSettings("state anim transition");
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(10f);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("OnUpdate", EditorStyles.boldLabel);
                if (GUILayout.Button("StateTimeline", GUILayout.Height(40f)))
                {
                    Type windowType = typeof(SkillEditorInspectorWindow).Assembly.GetType("SkillEditor.Editor.StateTimelineEditorWindow");
                    MethodInfo openMethod = windowType?.GetMethod("OpenForEntry", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    openMethod?.Invoke(null, new object[] { _entry });
                }
                EditorGUILayout.EndVertical();

                if (EditorGUI.EndChangeCheck())
                {
                    SkillResourceRepository.MarkDirty(_entry);
                }
            }

            private void DrawDefaultNextStateField()
            {
                string unitId = _entry != null ? _entry.UnitId : string.Empty;
                List<SkillResourceFileEntry> stateEntries = SkillResourceRepository.LoadStates(unitId);
                string nextStateId = DrawLayerFilteredStatePopup("defaultNextState", _config.DefaultNextStateId, stateEntries, includeEmpty: true, _config.Layer, _config.StateId);
                if (!string.Equals(_config.DefaultNextStateId, nextStateId, StringComparison.Ordinal))
                {
                    _config.DefaultNextStateId = nextStateId;
                    SkillResourceRepository.MarkDirty(_entry);
                }
            }

            private string DrawLayerFilteredStatePopup(string label, string currentStateId, List<SkillResourceFileEntry> stateEntries, bool includeEmpty, StateLayerType filterLayer, string excludedStateId = "")
            {
                List<StateConfig> candidateStates = new List<StateConfig>();
                for (int i = 0; i < (stateEntries != null ? stateEntries.Count : 0); i++)
                {
                    StateConfig config = stateEntries[i] != null ? stateEntries[i].Config as StateConfig : null;
                    if (config == null || string.IsNullOrWhiteSpace(config.StateId))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(excludedStateId) && string.Equals(config.StateId, excludedStateId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (config.Layer != filterLayer)
                    {
                        continue;
                    }

                    candidateStates.Add(config);
                }

                int baseOffset = includeEmpty ? 1 : 0;
                int optionCount = candidateStates.Count + baseOffset;
                string[] optionLabels = new string[Mathf.Max(1, optionCount)];
                string[] optionValues = new string[Mathf.Max(1, optionCount)];
                int currentIndex = 0;

                if (includeEmpty)
                {
                    optionLabels[0] = "未设置";
                    optionValues[0] = string.Empty;
                }

                for (int i = 0; i < candidateStates.Count; i++)
                {
                    StateConfig config = candidateStates[i];
                    string stateId = config.StateId;
                    string stateName = config.StateName;
                    int optionIndex = i + baseOffset;
                    optionLabels[optionIndex] = string.IsNullOrEmpty(stateName) ? stateId : $"{stateName} ({stateId})";
                    optionValues[optionIndex] = stateId;
                    if (!string.IsNullOrEmpty(stateId) && string.Equals(currentStateId, stateId, StringComparison.Ordinal))
                    {
                        currentIndex = optionIndex;
                    }
                }

                if (optionLabels.Length == 0)
                {
                    optionLabels = new[] { "无可选状态" };
                    optionValues = new[] { string.Empty };
                }

                int nextIndex = EditorGUILayout.Popup(label, currentIndex, optionLabels);
                return nextIndex >= 0 && nextIndex < optionValues.Length ? optionValues[nextIndex] : string.Empty;
            }

            private void DrawStateLayerSettings()
            {
                _config.AnimationProfile ??= new StateAnimationProfile();
                _config.MovementProfile ??= StateMovementProfile.CreateDefault();

                StateLayerType nextStateLayer = (StateLayerType)EditorGUILayout.EnumPopup("stateLayer", _config.Layer);
                if (nextStateLayer != _config.Layer)
                {
                    _config.Layer = nextStateLayer;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                AnimationLayerType nextAnimationLayer = (AnimationLayerType)EditorGUILayout.EnumPopup("animLayer", _config.AnimationProfile.OutputLayer);
                if (nextAnimationLayer != _config.AnimationProfile.OutputLayer)
                {
                    _config.AnimationProfile.OutputLayer = nextAnimationLayer;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                float layerWeight = Mathf.Clamp01(EditorGUILayout.Slider("layerWeight", _config.AnimationProfile.LayerWeight, 0f, 1f));
                if (!Mathf.Approximately(layerWeight, _config.AnimationProfile.LayerWeight))
                {
                    _config.AnimationProfile.LayerWeight = layerWeight;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                float speed = Mathf.Max(0f, EditorGUILayout.FloatField("animSpeed", _config.AnimationProfile.Speed));
                if (!Mathf.Approximately(speed, _config.AnimationProfile.Speed))
                {
                    _config.AnimationProfile.Speed = speed;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                bool applyRootMotion = EditorGUILayout.Toggle("applyRootMotion", _config.AnimationProfile.ApplyRootMotion);
                if (applyRootMotion != _config.AnimationProfile.ApplyRootMotion)
                {
                    _config.AnimationProfile.ApplyRootMotion = applyRootMotion;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                DrawLocomotionSpeedMatching(_config.AnimationProfile, () => SkillResourceRepository.MarkDirty(_entry));

                DrawMovementProfile(_config, () => SkillResourceRepository.MarkDirty(_entry));
            }

            private void DrawAnimationField(string label)
            {
                bool drawSingleClipPicker = DrawStateAnimationModeAndDirectionalConfig(
                    _config,
                    () => SkillResourceRepository.MarkDirty(_entry),
                    () => _owner?.Repaint(),
                    path => string.IsNullOrEmpty(path) ? null : SkillAnimationReferenceUtility.LoadClip(path));
                if (!drawSingleClipPicker)
                {
                    return;
                }

                AnimationClip current = string.IsNullOrEmpty(_config.AnimationClipPath)
                    ? null
                    : SkillAnimationReferenceUtility.LoadClip(_config.AnimationClipPath);

                GameUnit previewConfig = SkillPreviewUnitSettings.LoadActivePreviewConfig();
                EditorGUILayout.LabelField(label);
                Rect rowRect = EditorGUILayout.GetControlRect(false, 42f);
                Rect fieldRect = new Rect(rowRect.x, rowRect.y, rowRect.width - 130f, rowRect.height);
                Rect pickRect = new Rect(rowRect.xMax - 124f, rowRect.y, 60f, rowRect.height);
                Rect clearRect = new Rect(rowRect.xMax - 60f, rowRect.y, 60f, rowRect.height);

                string displayText = current != null ? current.name : "拖入 AnimationClip 或点击选择";
                EditorGUI.HelpBox(fieldRect, displayText, MessageType.None);
                HandleAnimationDrag(fieldRect);

                if (GUI.Button(pickRect, "选择"))
                {
                    SkillAnimationPickerWindow.Open(OnAnimationPicked);
                }

                if (GUI.Button(clearRect, "清空"))
                {
                    SetAnimationPath(string.Empty);
                }

                if (previewConfig != null)
                {
                    EditorGUILayout.LabelField(
                        $"筛选: root={FormatPreviewValue(previewConfig.AnimationSearchRoot)} | key={FormatPreviewValue(previewConfig.AnimationFilterKey)}",
                        EditorStyles.miniLabel);
                }

                if (current != null && !SkillAnimationSelectionUtility.IsClipAllowed(current))
                {
                    EditorGUILayout.HelpBox("当前动画不符合当前预览单位的筛选规则，请重新选择。", MessageType.Warning);
                }
            }

            private void DrawAnimationTransitionSettings(string header)
            {
                AnimationClip current = null;
                if (_config.AnimationMode == StateAnimationMode.SingleClip)
                {
                    current = string.IsNullOrEmpty(_config.AnimationClipPath)
                        ? null
                        : SkillAnimationReferenceUtility.LoadClip(_config.AnimationClipPath);

                    if (current == null)
                    {
                        return;
                    }
                }

                TimelineAnimationConfig animationConfig = EnsureTimelineAnimationConfig();

                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

                float transitionDuration = Mathf.Max(0f, EditorGUILayout.FloatField("transition duration", animationConfig.TransitionDuration));
                if (!Mathf.Approximately(transitionDuration, animationConfig.TransitionDuration))
                {
                    animationConfig.TransitionDuration = transitionDuration;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                AnimationTransitionTimeUnit transitionTimeUnit = (AnimationTransitionTimeUnit)EditorGUILayout.EnumPopup("transition time unit", animationConfig.TransitionTimeUnit);
                if (transitionTimeUnit != animationConfig.TransitionTimeUnit)
                {
                    animationConfig.TransitionTimeUnit = transitionTimeUnit;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                AnimancerFadeMode fadeMode = (AnimancerFadeMode)EditorGUILayout.EnumPopup("fade mode", animationConfig.FadeMode);
                if (fadeMode != animationConfig.FadeMode)
                {
                    animationConfig.FadeMode = fadeMode;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                float startTime = Mathf.Max(0f, EditorGUILayout.FloatField("start time", animationConfig.StartTime));
                if (!Mathf.Approximately(startTime, animationConfig.StartTime))
                {
                    animationConfig.StartTime = startTime;
                    SkillResourceRepository.MarkDirty(_entry);
                }

                AnimationStartTimeUnit startTimeUnit = (AnimationStartTimeUnit)EditorGUILayout.EnumPopup("start time unit", animationConfig.StartTimeUnit);
                if (startTimeUnit != animationConfig.StartTimeUnit)
                {
                    animationConfig.StartTimeUnit = startTimeUnit;
                    SkillResourceRepository.MarkDirty(_entry);
                }
            }

            private void HandleAnimationDrag(Rect fieldRect)
            {
                Event current = Event.current;
                if ((current.type != EventType.DragUpdated && current.type != EventType.DragPerform) ||
                    !fieldRect.Contains(current.mousePosition))
                {
                    return;
                }

                bool canAccept = SkillAnimationSelectionUtility.TryExtractClipFromDrag(DragAndDrop.objectReferences, out AnimationClip clip, out string errorMessage);
                DragAndDrop.visualMode = canAccept ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

                if (current.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    if (canAccept)
                    {
                        SetAnimationPath(SkillAnimationSelectionUtility.SerializeAllowedClip(clip));
                    }
                    else if (!string.IsNullOrEmpty(errorMessage))
                    {
                        EditorUtility.DisplayDialog("动画选择无效", errorMessage, "确定");
                    }
                }

                current.Use();
            }

            private static string FormatPreviewValue(string value)
            {
                return string.IsNullOrEmpty(value) ? "未设置" : value;
            }

            private void OnAnimationPicked(string animationPath)
            {
                SetAnimationPath(animationPath);
                _owner?.Repaint();
            }

            private void SetAnimationPath(string animationPath)
            {
                _config.AnimationClipPath = animationPath ?? string.Empty;
                EnsureTimelineAnimationConfig();
                SkillResourceRepository.MarkDirty(_entry);
            }

            private TimelineAnimationConfig EnsureTimelineAnimationConfig()
            {
                _config.Timeline ??= new StateTimelineConfig();
                _config.Timeline.Animation ??= new TimelineAnimationConfig();
                return _config.Timeline.Animation;
            }
        }

        private sealed class BuffInspectorPanel : ISkillEditorInspectorPanel
        {
            private readonly SkillResourceFileEntry _entry;
            private readonly BuffConfig _config;

            public BuffInspectorPanel(SkillResourceFileEntry entry)
            {
                _entry = entry;
                _config = entry != null ? entry.Config as BuffConfig : null;
            }

            public string Header => "Buff";

            public void Bind(EditorWindow owner)
            {
            }

            public void OnGUI()
            {
                if (_config == null)
                {
                    EditorGUILayout.HelpBox("当前没有选中的 Buff。", MessageType.Info);
                    return;
                }

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.BeginVertical("box");
                string sanitizedBuffId = NumericIdUtility.Sanitize(_config.BuffId);
                string nextBuffId = NumericIdUtility.Sanitize(EditorGUILayout.TextField("buffId", sanitizedBuffId));
                if (nextBuffId != _config.BuffId)
                {
                    _config.BuffId = nextBuffId;
                }

                _config.BuffName = EditorGUILayout.TextField("buffName", _config.BuffName);
                _config.Duration = Mathf.Max(0f, EditorGUILayout.FloatField("duration", _config.Duration));
                _config.IsStackable = EditorGUILayout.Toggle("isStackable", _config.IsStackable);
                _config.StackMode = (BuffStackMode)EditorGUILayout.EnumPopup("stackMode", _config.StackMode);
                _config.BuffType = (BuffType)EditorGUILayout.EnumPopup("buffType", _config.BuffType);
                _config.UpdateInterval = Mathf.Max(0f, EditorGUILayout.FloatField("updateInterval", _config.UpdateInterval));

                string iconPath = BuffIconSelectionUtility.DrawIconField("icon", _config.IconAssetPath);
                if (iconPath != _config.IconAssetPath)
                {
                    _config.IconAssetPath = iconPath;
                }

                if (TagSelectionEditorUtility.DrawTagContainer("Buff Tags", _config.Tags))
                {
                    SkillResourceRepository.MarkDirty(_entry);
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(10f);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);

                if (GUILayout.Button("OnAdd    effects(BT)", GUILayout.Height(34f)))
                {
                    _config.OnAddEffect ??= new SkillEffectConfig();
                    OpenEffectEditor(_entry, _config.OnAddEffect, "Buff / OnAdd", MarkDirty);
                }
                EditorGUILayout.LabelField(BuildEffectSummary(_config.OnAddEffect), EditorStyles.miniLabel);

                if (GUILayout.Button("OnUpdate effects(BT)", GUILayout.Height(34f)))
                {
                    _config.OnUpdateEffect ??= new SkillEffectConfig();
                    OpenEffectEditor(_entry, _config.OnUpdateEffect, "Buff / OnUpdate", MarkDirty);
                }
                EditorGUILayout.LabelField(BuildEffectSummary(_config.OnUpdateEffect), EditorStyles.miniLabel);

                if (GUILayout.Button("OnRemove effects(BT)", GUILayout.Height(34f)))
                {
                    _config.OnRemoveEffect ??= new SkillEffectConfig();
                    OpenEffectEditor(_entry, _config.OnRemoveEffect, "Buff / OnRemove", MarkDirty);
                }
                EditorGUILayout.LabelField(BuildEffectSummary(_config.OnRemoveEffect), EditorStyles.miniLabel);

                EditorGUILayout.EndVertical();

                if (EditorGUI.EndChangeCheck())
                {
                    SkillResourceRepository.MarkDirty(_entry);
                }
            }

            private void MarkDirty()
            {
                SkillResourceRepository.MarkDirty(_entry);
            }
        }

        private sealed class PreviewUnitInspectorPanel : ISkillEditorInspectorPanel
        {
            private GameObject _candidatePrefab;

            public string Header => "预览单位";

            public void Bind(EditorWindow owner)
            {
                _candidatePrefab = SkillPreviewUnitSettings.LoadActivePrefab();
            }

            public void OnGUI()
            {
                EditorGUILayout.HelpBox("这里只负责编辑器预览载体，不参与技能正式数据开发。Apply 之后，会在当前 Scene 创建一个解包后的预览复制体，后续 StateTimeline 预览会以它为载体。", MessageType.Info);
                _candidatePrefab = EditorGUILayout.ObjectField("预览 Prefab", _candidatePrefab, typeof(GameObject), false) as GameObject;

                EditorGUILayout.Space(8f);
                DrawValidation();
                EditorGUILayout.Space(8f);
                DrawPreviewWeapons();
                EditorGUILayout.Space(8f);
                DrawPreviewSkills();
                EditorGUILayout.Space(8f);
                DrawActiveSelection();
                EditorGUILayout.Space(8f);
                DrawActions();
            }

            private void DrawValidation()
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("校验结果", EditorStyles.boldLabel);

                if (_candidatePrefab == null)
                {
                    EditorGUILayout.HelpBox("请先拖入一个用于预览技能的 prefab。", MessageType.Warning);
                    EditorGUILayout.EndVertical();
                    return;
                }

                bool hasAnimator = TryGetAnimator(_candidatePrefab, out Animator animator);
                bool hasPreviewConfig = TryGetPreviewConfig(_candidatePrefab, out GameUnit previewConfig);
                bool hasMountPoints = hasPreviewConfig && previewConfig.MountPoints.Count > 0;
                bool hasMissingMountTransform = false;
                bool hasInvalidPreviewWeapon = false;

                if (hasPreviewConfig)
                {
                    for (int i = 0; i < previewConfig.MountPoints.Count; i++)
                    {
                        if (previewConfig.MountPoints[i].MountTransform == null)
                        {
                            hasMissingMountTransform = true;
                            break;
                        }
                    }

                    hasInvalidPreviewWeapon = HasInvalidPreviewWeaponSelection(previewConfig);
                }

                DrawCheckLine("Animator", hasAnimator, hasAnimator ? animator.name : "缺少 Animator");
                DrawCheckLine("GameUnit", hasPreviewConfig, hasPreviewConfig ? "已挂载" : "缺少 GameUnit 组件");
                DrawCheckLine("挂点", hasMountPoints && !hasMissingMountTransform, hasPreviewConfig
                    ? (hasMissingMountTransform ? "存在空挂点引用" : previewConfig.MountPoints.Count + " 个挂点")
                    : "未配置挂点");
                DrawCheckLine("预览武器", !hasInvalidPreviewWeapon, hasPreviewConfig
                    ? (hasInvalidPreviewWeapon ? "存在无效预览武器配置" : "已通过或未配置")
                    : "未配置预览单位");

                if (!hasAnimator || !hasPreviewConfig || !hasMountPoints || hasMissingMountTransform || hasInvalidPreviewWeapon)
                {
                    EditorGUILayout.HelpBox("当前 prefab 还不能作为技能预览载体。至少需要 Animator、GameUnit、完整的角色挂点；如果配置了预览武器，则武器 prefab 和装备挂点也必须有效。", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("当前 prefab 可以作为技能预览载体。", MessageType.Info);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("定位 Prefab"))
                {
                    Selection.activeObject = _candidatePrefab;
                    EditorGUIUtility.PingObject(_candidatePrefab);
                }

                using (new EditorGUI.DisabledScope(previewConfig == null))
                {
                    if (GUILayout.Button("打开挂点配置"))
                    {
                        Selection.activeObject = previewConfig;
                        EditorGUIUtility.PingObject(previewConfig);
                    }
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            private void DrawPreviewWeapons()
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("预览武器", EditorStyles.boldLabel);

                if (!TryGetPreviewConfig(_candidatePrefab, out GameUnit previewConfig) ||
                    previewConfig == null ||
                    previewConfig.WeaponBindings == null ||
                    previewConfig.WeaponBindings.Count == 0)
                {
                    EditorGUILayout.HelpBox("当前角色还没有武器挂载配置。请先在角色 prefab 的 `GameUnit` 中添加武器挂载。", MessageType.Info);
                    EditorGUILayout.EndVertical();
                    return;
                }

                HashSet<SkillWeaponType> drawnTypes = new HashSet<SkillWeaponType>();
                for (int i = 0; i < previewConfig.WeaponBindings.Count; i++)
                {
                    PreviewWeaponBinding binding = previewConfig.WeaponBindings[i];
                    if (binding == null || !drawnTypes.Add(binding.WeaponType))
                    {
                        continue;
                    }

                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField(binding.WeaponType.ToString(), EditorStyles.boldLabel);
                    GameObject currentPreviewWeapon = SkillPreviewUnitSettings.LoadPreviewWeaponPrefab(binding.WeaponType);
                    GameObject nextPreviewWeapon = EditorGUILayout.ObjectField("预览武器", currentPreviewWeapon, typeof(GameObject), false) as GameObject;
                    if (nextPreviewWeapon != currentPreviewWeapon)
                    {
                        SkillPreviewUnitSettings.SetPreviewWeaponPrefab(binding.WeaponType, nextPreviewWeapon);
                        SkillPreviewUnitSettings.Save();
                    }

                    string currentBindingName = SkillPreviewUnitSettings.LoadPreviewWeaponBindingName(binding.WeaponType);
                    string nextBindingName = DrawWeaponBindingField(previewConfig, binding.WeaponType, currentBindingName, "武器挂载");
                    if (nextBindingName != currentBindingName)
                    {
                        SkillPreviewUnitSettings.SetPreviewWeaponBindingName(binding.WeaponType, nextBindingName);
                        SkillPreviewUnitSettings.Save();
                    }

                    PreviewWeaponConfig weaponConfig = nextPreviewWeapon == null ? null : nextPreviewWeapon.GetComponent<PreviewWeaponConfig>();
                    if (nextPreviewWeapon == null)
                    {
                        EditorGUILayout.HelpBox("未配置该武器类型的预览武器。Apply 时会跳过这个类型。", MessageType.Info);
                    }
                    else if (string.IsNullOrEmpty(nextBindingName))
                    {
                        EditorGUILayout.HelpBox("请为该测试武器选择一个武器挂载。", MessageType.Warning);
                    }
                    else if (weaponConfig == null)
                    {
                        EditorGUILayout.HelpBox("该 prefab 缺少 PreviewWeaponConfig。", MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("武器挂点数", weaponConfig.MountPoints != null ? weaponConfig.MountPoints.Count.ToString() : "0");
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawActiveSelection()
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("当前生效载体", EditorStyles.boldLabel);

                GameObject activePrefab = SkillPreviewUnitSettings.LoadActivePrefab();
                if (activePrefab == null)
                {
                    EditorGUILayout.LabelField("未设置");
                }
                else
                {
                    EditorGUILayout.ObjectField("Active Prefab", activePrefab, typeof(GameObject), false);
                    EditorGUILayout.LabelField("路径", SkillPreviewUnitSettings.ActivePrefabPath);
                }

                GameObject sceneInstance = SkillPreviewSceneInstanceUtility.GetCurrentInstance();
                GameObject primaryWeaponInstance = SkillPreviewSceneInstanceUtility.GetCurrentPrimaryWeaponInstance();
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Scene 预览实例", EditorStyles.boldLabel);
                if (sceneInstance == null)
                {
                    EditorGUILayout.LabelField("未创建");
                }
                else
                {
                    EditorGUILayout.ObjectField("Instance", sceneInstance, typeof(GameObject), true);
                    EditorGUILayout.LabelField("状态", PrefabUtility.IsPartOfPrefabInstance(sceneInstance) ? "Prefab Instance" : "已解包 Scene 对象");
                }

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("主预览武器", EditorStyles.boldLabel);
                if (primaryWeaponInstance == null)
                {
                    EditorGUILayout.LabelField("未挂载");
                }
                else
                {
                    EditorGUILayout.ObjectField("Weapon", primaryWeaponInstance, typeof(GameObject), true);
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawPreviewSkills()
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("预览技能槽", EditorStyles.boldLabel);

                if (!TryGetPreviewConfig(_candidatePrefab, out GameUnit previewConfig) || previewConfig == null)
                {
                    EditorGUILayout.HelpBox("请先选择一个带 GameUnit 的预览单位 prefab。", MessageType.Info);
                    EditorGUILayout.EndVertical();
                    return;
                }

                PreviewSkillSlotInspectorUtility.DrawSkillSlots(previewConfig, () => EditorUtility.SetDirty(previewConfig));
                EditorGUILayout.EndVertical();
            }

            private void DrawActions()
            {
                bool canApply = IsCandidateValid(_candidatePrefab);

                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(!canApply))
                {
                    if (GUILayout.Button("Apply", GUILayout.Height(34f)))
                    {
                        SkillPreviewUnitSettings.ActivePrefabPath = AssetDatabase.GetAssetPath(_candidatePrefab);
                        SkillPreviewUnitSettings.Save();
                        SkillPreviewSceneInstanceUtility.CreateOrReplace(_candidatePrefab);
                    }
                }

                using (new EditorGUI.DisabledScope(SkillPreviewUnitSettings.LoadActivePrefab() == null))
                {
                    if (GUILayout.Button("Clear", GUILayout.Height(34f)))
                    {
                        SkillPreviewSceneInstanceUtility.RemoveCurrentInstance();
                        SkillPreviewUnitSettings.Clear();
                        _candidatePrefab = null;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            private static void DrawCheckLine(string label, bool passed, string detail)
            {
                Color previousColor = GUI.color;
                GUI.color = passed ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.85f, 0.35f, 0.35f);
                EditorGUILayout.LabelField(label, passed ? "OK" : "Missing");
                GUI.color = previousColor;
                EditorGUILayout.LabelField("说明", detail);
            }

            private static bool IsCandidateValid(GameObject prefab)
            {
                if (!TryGetAnimator(prefab, out _))
                {
                    return false;
                }

                if (!TryGetPreviewConfig(prefab, out GameUnit previewConfig))
                {
                    return false;
                }

                if (previewConfig.MountPoints.Count == 0)
                {
                    return false;
                }

                for (int i = 0; i < previewConfig.MountPoints.Count; i++)
                {
                    if (previewConfig.MountPoints[i].MountTransform == null)
                    {
                        return false;
                    }
                }

                return !HasInvalidPreviewWeaponSelection(previewConfig);
            }

            private static bool TryGetAnimator(GameObject prefab, out Animator animator)
            {
                animator = prefab == null ? null : prefab.GetComponentInChildren<Animator>(true);
                return animator != null;
            }

            private static bool TryGetPreviewConfig(GameObject prefab, out GameUnit previewConfig)
            {
                previewConfig = prefab == null ? null : prefab.GetComponent<GameUnit>();
                return previewConfig != null;
            }

            private static bool HasInvalidPreviewWeaponSelection(GameUnit previewConfig)
            {
                if (previewConfig == null || previewConfig.WeaponBindings == null)
                {
                    return false;
                }

                HashSet<SkillWeaponType> checkedTypes = new HashSet<SkillWeaponType>();
                for (int i = 0; i < previewConfig.WeaponBindings.Count; i++)
                {
                    PreviewWeaponBinding binding = previewConfig.WeaponBindings[i];
                    if (binding == null || !checkedTypes.Add(binding.WeaponType))
                    {
                        continue;
                    }

                    GameObject previewWeaponPrefab = SkillPreviewUnitSettings.LoadPreviewWeaponPrefab(binding.WeaponType);
                    if (previewWeaponPrefab == null)
                    {
                        continue;
                    }

                    string bindingName = SkillPreviewUnitSettings.LoadPreviewWeaponBindingName(binding.WeaponType);
                    if (string.IsNullOrEmpty(bindingName))
                    {
                        return true;
                    }

                    if (!TryGetWeaponBinding(previewConfig, binding.WeaponType, bindingName, out PreviewWeaponBinding selectedBinding))
                    {
                        return true;
                    }

                    if (!string.IsNullOrEmpty(selectedBinding.EquipSocketName) &&
                        !HasMountPoint(previewConfig.MountPoints, selectedBinding.EquipSocketName))
                    {
                        return true;
                    }

                    PreviewWeaponConfig weaponConfig = previewWeaponPrefab.GetComponent<PreviewWeaponConfig>();
                    if (weaponConfig == null)
                    {
                        return true;
                    }

                    if (weaponConfig.MountPoints == null)
                    {
                        return true;
                    }

                    for (int mountIndex = 0; mountIndex < weaponConfig.MountPoints.Count; mountIndex++)
                    {
                        if (weaponConfig.MountPoints[mountIndex] != null && weaponConfig.MountPoints[mountIndex].MountTransform == null)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private static string DrawWeaponBindingField(GameUnit previewConfig, SkillWeaponType weaponType, string currentValue, string label)
            {
                List<string> options = CollectWeaponBindingNames(previewConfig, weaponType);
                if (options.Count == 0)
                {
                    EditorGUILayout.LabelField(label, "无可用挂载");
                    return string.Empty;
                }

                string[] popupOptions = new string[options.Count + 1];
                popupOptions[0] = "未指定";
                for (int i = 0; i < options.Count; i++)
                {
                    popupOptions[i + 1] = options[i];
                }

                int currentIndex = 0;
                for (int i = 1; i < popupOptions.Length; i++)
                {
                    if (string.Equals(popupOptions[i], currentValue, StringComparison.Ordinal))
                    {
                        currentIndex = i;
                        break;
                    }
                }

                int nextIndex = EditorGUILayout.Popup(label, currentIndex, popupOptions);
                return nextIndex <= 0 ? string.Empty : popupOptions[nextIndex];
            }

            private static List<string> CollectWeaponBindingNames(GameUnit previewConfig, SkillWeaponType weaponType)
            {
                List<string> results = new List<string>();
                if (previewConfig == null || previewConfig.WeaponBindings == null)
                {
                    return results;
                }

                for (int i = 0; i < previewConfig.WeaponBindings.Count; i++)
                {
                    PreviewWeaponBinding binding = previewConfig.WeaponBindings[i];
                    if (binding == null || binding.WeaponType != weaponType)
                    {
                        continue;
                    }

                    string displayName = string.IsNullOrEmpty(binding.DisplayName) ? $"武器挂载 {i + 1}" : binding.DisplayName;
                    if (!results.Contains(displayName))
                    {
                        results.Add(displayName);
                    }
                }

                return results;
            }

            private static bool TryGetWeaponBinding(GameUnit previewConfig, SkillWeaponType weaponType, string bindingName, out PreviewWeaponBinding binding)
            {
                binding = null;
                if (previewConfig == null || previewConfig.WeaponBindings == null || string.IsNullOrEmpty(bindingName))
                {
                    return false;
                }

                for (int i = 0; i < previewConfig.WeaponBindings.Count; i++)
                {
                    PreviewWeaponBinding current = previewConfig.WeaponBindings[i];
                    if (current == null || current.WeaponType != weaponType)
                    {
                        continue;
                    }

                    string displayName = string.IsNullOrEmpty(current.DisplayName) ? $"武器挂载 {i + 1}" : current.DisplayName;
                    if (string.Equals(displayName, bindingName, StringComparison.Ordinal))
                    {
                        binding = current;
                        return true;
                    }
                }

                return false;
            }

            private static bool HasMountPoint(IList<PreviewMountPoint> mountPoints, string socketName)
            {
                if (mountPoints == null || string.IsNullOrEmpty(socketName))
                {
                    return false;
                }

                for (int i = 0; i < mountPoints.Count; i++)
                {
                    PreviewMountPoint mountPoint = mountPoints[i];
                    if (mountPoint == null || mountPoint.MountTransform == null)
                    {
                        continue;
                    }

                    if (string.Equals(mountPoint.SocketName, socketName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static class PreviewSkillSlotInspectorUtility
        {
            public static void DrawSkillSlots(GameUnit config, Action markDirty)
            {
                if (config == null)
                {
                    return;
                }

                DrawActiveSkillSlots(config, markDirty);
                EditorGUILayout.Space(8f);
                DrawPassiveSkillSlots(config, markDirty);
            }

            private static void DrawActiveSkillSlots(GameUnit config, Action markDirty)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("主动技能槽位", EditorStyles.boldLabel);
                config.ActiveSkillSlots ??= new List<PreviewActiveSkillSlotConfig>();

                List<SkillResourceFileEntry> allSkillEntries = SkillResourceRepository.LoadSkills(config.UnitId);
                List<SkillResourceFileEntry> skillEntries = FilterSkillEntriesByCategory(allSkillEntries, SkillCastCategory.Active, config.ActiveSkillSlots);
                for (int i = 0; i < config.ActiveSkillSlots.Count; i++)
                {
                    PreviewActiveSkillSlotConfig slot = config.ActiveSkillSlots[i] ?? new PreviewActiveSkillSlotConfig();
                    config.ActiveSkillSlots[i] = slot;

                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();
                    slot.DisplayName = EditorGUILayout.TextField("名称", slot.DisplayName);
                    if (GUILayout.Button("-", GUILayout.Width(28f)))
                    {
                        config.ActiveSkillSlots.RemoveAt(i);
                        markDirty?.Invoke();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    int slotIndex = EditorGUILayout.IntField("槽位编号", slot.SlotIndex);
                    if (slotIndex != slot.SlotIndex)
                    {
                        slot.SlotIndex = Mathf.Max(1, slotIndex);
                        markDirty?.Invoke();
                    }

                    string actionName = InputActionEditorUtility.DrawActionPopup("输入动作", slot.ActionName);
                    if (!string.Equals(actionName, slot.ActionName, StringComparison.Ordinal))
                    {
                        slot.ActionName = actionName;
                        markDirty?.Invoke();
                    }

                    string nextSkillAssetName = DrawSkillPopup("技能", skillEntries, slot.SkillAssetName);
                    if (!string.Equals(nextSkillAssetName, slot.SkillAssetName, StringComparison.Ordinal))
                    {
                        slot.SkillAssetName = nextSkillAssetName;
                        markDirty?.Invoke();
                    }

                    DrawSkillSummary(allSkillEntries, slot.SkillAssetName, SkillCastCategory.Active);
                    EditorGUILayout.EndVertical();
                }

                if (GUILayout.Button("添加主动技能槽", GUILayout.Height(28f)))
                {
                    config.ActiveSkillSlots.Add(new PreviewActiveSkillSlotConfig
                    {
                        SlotIndex = config.ActiveSkillSlots.Count + 1,
                        DisplayName = $"主动技能槽{config.ActiveSkillSlots.Count + 1}",
                    });
                    markDirty?.Invoke();
                }

                EditorGUILayout.EndVertical();
            }

            private static void DrawPassiveSkillSlots(GameUnit config, Action markDirty)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("被动技能槽位", EditorStyles.boldLabel);
                config.PassiveSkillSlots ??= new List<PreviewPassiveSkillSlotConfig>();

                List<SkillResourceFileEntry> allSkillEntries = SkillResourceRepository.LoadSkills(config.UnitId);
                List<SkillResourceFileEntry> skillEntries = FilterSkillEntriesByCategory(allSkillEntries, SkillCastCategory.Passive, config.PassiveSkillSlots);
                for (int i = 0; i < config.PassiveSkillSlots.Count; i++)
                {
                    PreviewPassiveSkillSlotConfig slot = config.PassiveSkillSlots[i] ?? new PreviewPassiveSkillSlotConfig();
                    config.PassiveSkillSlots[i] = slot;

                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();
                    slot.DisplayName = EditorGUILayout.TextField("名称", slot.DisplayName);
                    if (GUILayout.Button("-", GUILayout.Width(28f)))
                    {
                        config.PassiveSkillSlots.RemoveAt(i);
                        markDirty?.Invoke();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    int slotIndex = EditorGUILayout.IntField("槽位编号", slot.SlotIndex);
                    if (slotIndex != slot.SlotIndex)
                    {
                        slot.SlotIndex = Mathf.Max(1, slotIndex);
                        markDirty?.Invoke();
                    }

                    string nextSkillAssetName = DrawSkillPopup("技能", skillEntries, slot.SkillAssetName);
                    if (!string.Equals(nextSkillAssetName, slot.SkillAssetName, StringComparison.Ordinal))
                    {
                        slot.SkillAssetName = nextSkillAssetName;
                        markDirty?.Invoke();
                    }

                    DrawSkillSummary(allSkillEntries, slot.SkillAssetName, SkillCastCategory.Passive);
                    EditorGUILayout.EndVertical();
                }

                if (GUILayout.Button("添加被动技能槽", GUILayout.Height(28f)))
                {
                    config.PassiveSkillSlots.Add(new PreviewPassiveSkillSlotConfig
                    {
                        SlotIndex = config.PassiveSkillSlots.Count + 1,
                        DisplayName = $"被动技能槽{config.PassiveSkillSlots.Count + 1}",
                    });
                    markDirty?.Invoke();
                }

                EditorGUILayout.EndVertical();
            }

            private static string DrawSkillPopup(string label, List<SkillResourceFileEntry> skillEntries, string currentAssetName)
            {
                if (skillEntries == null || skillEntries.Count == 0)
                {
                    EditorGUILayout.LabelField(label, "<暂无 Skill 资源>");
                    return string.Empty;
                }

                string[] options = new string[skillEntries.Count + 1];
                options[0] = "<未装配>";
                int currentIndex = 0;
                for (int i = 0; i < skillEntries.Count; i++)
                {
                    SkillResourceFileEntry entry = skillEntries[i];
                    SkillConfig skillConfig = entry.Config as SkillConfig;
                    string displayName = SkillResourceRepository.GetDisplayName(entry);
                    options[i + 1] = string.IsNullOrEmpty(skillConfig?.SkillId)
                        ? displayName
                        : $"{displayName} ({skillConfig.SkillId})";
                    if (SkillResourceRepository.IsMatchingSkillReference(entry, currentAssetName))
                    {
                        currentIndex = i + 1;
                    }
                }

                int nextIndex = EditorGUILayout.Popup(label, currentIndex, options);
                if (nextIndex <= 0)
                {
                    return string.Empty;
                }

                SkillConfig selectedConfig = skillEntries[nextIndex - 1].Config as SkillConfig;
                string runtimeSkillId = SkillResourceRepository.GetSkillRuntimeId(skillEntries[nextIndex - 1]);
                return !string.IsNullOrEmpty(runtimeSkillId)
                    ? runtimeSkillId
                    : (!string.IsNullOrEmpty(selectedConfig?.SkillId) ? selectedConfig.SkillId : skillEntries[nextIndex - 1].BaseName);
            }

            private static void DrawSkillSummary(List<SkillResourceFileEntry> skillEntries, string skillAssetName, SkillCastCategory expectedCategory)
            {
                if (string.IsNullOrEmpty(skillAssetName))
                {
                    EditorGUILayout.HelpBox("当前槽位还没有装配 Skill。", MessageType.Info);
                    return;
                }

                SkillConfig skillConfig = FindSkillConfig(skillEntries, skillAssetName);
                if (skillConfig == null)
                {
                    EditorGUILayout.HelpBox($"未找到 Skill 资源: {skillAssetName}", MessageType.Warning);
                    return;
                }

                EditorGUILayout.LabelField("skillId", skillConfig.SkillId, EditorStyles.miniLabel);
                EditorGUILayout.LabelField("skillType", skillConfig.SkillCategory.ToString(), EditorStyles.miniLabel);
                if (skillConfig.SkillCategory != expectedCategory)
                {
                    EditorGUILayout.HelpBox($"该 Skill 当前被标记为 {skillConfig.SkillCategory}，与当前槽位期望的 {expectedCategory} 不一致。", MessageType.Warning);
                }
            }

            private static SkillConfig FindSkillConfig(List<SkillResourceFileEntry> skillEntries, string skillAssetName)
            {
                if (skillEntries == null || string.IsNullOrEmpty(skillAssetName))
                {
                    return null;
                }

                for (int i = 0; i < skillEntries.Count; i++)
                {
                    SkillResourceFileEntry entry = skillEntries[i];
                    if (entry != null && SkillResourceRepository.IsMatchingSkillReference(entry, skillAssetName))
                    {
                        return entry.Config as SkillConfig;
                    }
                }

                return null;
            }

            private static List<SkillResourceFileEntry> FilterSkillEntriesByCategory<TSlot>(List<SkillResourceFileEntry> allSkillEntries, SkillCastCategory expectedCategory, List<TSlot> slots)
            {
                List<SkillResourceFileEntry> results = new List<SkillResourceFileEntry>();
                if (allSkillEntries == null)
                {
                    return results;
                }

                HashSet<string> equippedAssets = new HashSet<string>(StringComparer.Ordinal);
                if (slots != null)
                {
                    for (int i = 0; i < slots.Count; i++)
                    {
                        switch (slots[i])
                        {
                            case PreviewActiveSkillSlotConfig activeSlot when !string.IsNullOrEmpty(activeSlot.SkillAssetName):
                                equippedAssets.Add(activeSlot.SkillAssetName);
                                break;
                            case PreviewPassiveSkillSlotConfig passiveSlot when !string.IsNullOrEmpty(passiveSlot.SkillAssetName):
                                equippedAssets.Add(passiveSlot.SkillAssetName);
                                break;
                        }
                    }
                }

                for (int i = 0; i < allSkillEntries.Count; i++)
                {
                    SkillResourceFileEntry entry = allSkillEntries[i];
                    SkillConfig skillConfig = entry != null ? entry.Config as SkillConfig : null;
                    if (entry == null || skillConfig == null)
                    {
                        continue;
                    }

                    if (skillConfig.SkillCategory == expectedCategory ||
                        equippedAssets.Contains(SkillResourceRepository.GetSkillRuntimeId(entry)) ||
                        equippedAssets.Contains(skillConfig.SkillId) ||
                        equippedAssets.Contains(entry.BaseName))
                    {
                        results.Add(entry);
                    }
                }

                return results;
            }
        }

        private sealed class EffectNodeInspectorPanel : ISkillEditorInspectorPanel
        {
            private readonly SkillResourceFileEntry _entry;
            private readonly SkillEffectNodeConfig _node;
            private readonly Action _onModified;
            private EditorWindow _owner;

            public EffectNodeInspectorPanel(SkillResourceFileEntry entry, SkillEffectNodeConfig node, Action onModified)
            {
                _entry = entry;
                _node = node;
                _onModified = onModified;
            }

            public string Header => "Effect Node";

            public void Bind(EditorWindow owner)
            {
                _owner = owner;
            }

            public void OnGUI()
            {
                if (_node == null)
                {
                    EditorGUILayout.HelpBox("当前没有选中的节点。", MessageType.Info);
                    return;
                }

                EditorGUILayout.LabelField("NodeType", _node.NodeType.ToString());
                EditorGUILayout.Space(6f);

                switch (_node.NodeType)
                {
                    case SkillEffectNodeType.Sequence:
                        EditorGUILayout.HelpBox("Sequence 节点只负责顺序执行子节点，具体连线关系请直接在图上编辑。", MessageType.None);
                        break;
                    case SkillEffectNodeType.Condition:
                        DrawConditionInfo();
                        break;
                    case SkillEffectNodeType.Action:
                        DrawActionInfo();
                        break;
                }
            }

            private void DrawConditionInfo()
            {
                _node.Condition ??= new SkillConditionConfig();
                SkillConditionInspectorUtility.DrawCondition(_node.Condition, NotifyModified);
            }

            private void DrawActionInfo()
            {
                _node.Action ??= new SkillActionConfig();
                SkillActionType nextType = (SkillActionType)EditorGUILayout.EnumPopup("ActionType", _node.Action.ActionType);
                if (nextType != _node.Action.ActionType)
                {
                    _node.Action.CreateData(nextType);
                    NotifyModified();
                    return;
                }

                if (TagSelectionEditorUtility.DrawTagContainer("Action Tags", _node.Action.Tags))
                {
                    NotifyModified();
                }

                switch (_node.Action.Data)
                {
                    case DealDamage_SkillActionData dealDamage:
                        DrawDamageAction(dealDamage.Args);
                        break;
                    case AddToughnessDamage_SkillActionData addToughnessDamage:
                        DrawDamageAction(addToughnessDamage.Args);
                        break;
                    case AddAttribute_SkillActionData addAttribute:
                        DrawAttributeAction(addAttribute.Args);
                        break;
                    case AddTag_SkillActionData addTag:
                        DrawTagAction(addTag.Args);
                        break;
                    case AddBuff_SkillActionData addBuff:
                        DrawBuffAction(addBuff.Args);
                        break;
                    case RemoveBuff_SkillActionData removeBuff:
                        DrawBuffAction(removeBuff.Args);
                        break;
                }
            }

            private void DrawDamageAction(DamageActionArgs args)
            {
                SkillQueryTargetType target = (SkillQueryTargetType)EditorGUILayout.EnumPopup("Targets", args.QueryTarget);
                SkillAttributeType sourceAttribute = (SkillAttributeType)EditorGUILayout.EnumPopup("SourceAttribute", args.SourceAttribute);
                string actionId = EditorGUILayout.TextField("ActionId", args.ActionId);
                string description = EditorGUILayout.TextField("Description", args.Description);
                float ratio = EditorGUILayout.FloatField("Ratio", args.Ratio);
                if (target != args.QueryTarget || sourceAttribute != args.SourceAttribute || actionId != args.ActionId || description != args.Description || !Mathf.Approximately(ratio, args.Ratio))
                {
                    args.QueryTarget = target;
                    args.SourceAttribute = sourceAttribute;
                    args.ActionId = actionId;
                    args.Description = description;
                    args.Ratio = ratio;
                    NotifyModified();
                }
            }

            private void DrawBuffAction(BuffActionArgs args)
            {
                SkillQueryTargetType target = (SkillQueryTargetType)EditorGUILayout.EnumPopup("Targets", args.QueryTarget);
                string buffId = EditorGUILayout.TextField("BuffId", args.BuffId);
                float duration = EditorGUILayout.FloatField("Duration", args.Duration);
                if (target != args.QueryTarget || buffId != args.BuffId || !Mathf.Approximately(duration, args.Duration))
                {
                    args.QueryTarget = target;
                    args.BuffId = buffId;
                    args.Duration = duration;
                    NotifyModified();
                }
            }

            private void DrawAttributeAction(AttributeActionArgs args)
            {
                SkillQueryTargetType target = (SkillQueryTargetType)EditorGUILayout.EnumPopup("Targets", args.QueryTarget);
                SkillAttributeType attributeType = (SkillAttributeType)EditorGUILayout.EnumPopup("AttributeType", args.AttributeType);
                AttributeModifyMode modifyMode = (AttributeModifyMode)EditorGUILayout.EnumPopup("ModifyMode", args.ModifyMode);
                AttributeApplyLifetime applyLifetime = (AttributeApplyLifetime)EditorGUILayout.EnumPopup("ApplyLifetime", args.ApplyLifetime);
                float value = EditorGUILayout.FloatField("Value", args.Value);

                if (target != args.QueryTarget || attributeType != args.AttributeType || modifyMode != args.ModifyMode ||
                    applyLifetime != args.ApplyLifetime || !Mathf.Approximately(value, args.Value))
                {
                    args.QueryTarget = target;
                    args.AttributeType = attributeType;
                    args.ModifyMode = modifyMode;
                    args.ApplyLifetime = applyLifetime;
                    args.Value = value;
                    NotifyModified();
                }
            }

            private void DrawTagAction(TagActionArgs args)
            {
                SkillQueryTargetType target = (SkillQueryTargetType)EditorGUILayout.EnumPopup("Targets", args.QueryTarget);
                args.Tags ??= new List<string>();
                bool tagsChanged = TagSelectionEditorUtility.DrawTagList("Tags", args.Tags);
                int stack = Mathf.Max(1, EditorGUILayout.IntField("Stack", args.Stack));
                AttributeApplyLifetime applyLifetime = (AttributeApplyLifetime)EditorGUILayout.EnumPopup("ApplyLifetime", args.ApplyLifetime);
                if (target != args.QueryTarget || tagsChanged || stack != args.Stack || applyLifetime != args.ApplyLifetime)
                {
                    args.QueryTarget = target;
                    args.Stack = stack;
                    args.ApplyLifetime = applyLifetime;
                    NotifyModified();
                }
            }

            private void NotifyModified()
            {
                if (_entry != null)
                {
                    SkillResourceRepository.MarkDirty(_entry);
                }

                _onModified?.Invoke();
                _owner?.Repaint();
            }
        }

        private sealed class SkillNodeInspectorPanel : ISkillEditorInspectorPanel
        {
            private readonly SkillResourceFileEntry _entry;
            private readonly MetaSkillNodeConfig _node;
            private readonly Action _onModified;

            public SkillNodeInspectorPanel(SkillResourceFileEntry entry, MetaSkillNodeConfig node, Action onModified)
            {
                _entry = entry;
                _node = node;
                _onModified = onModified;
            }

            public string Header => "MetaSkillNode";

            public void Bind(EditorWindow owner)
            {
            }

            public void OnGUI()
            {
                if (_node == null)
                {
                    EditorGUILayout.HelpBox("当前没有选中的 MetaSkillNode。", MessageType.Info);
                    return;
                }

                EditorGUI.BeginChangeCheck();
                _node.DisplayName = EditorGUILayout.TextField("displayName", _node.DisplayName);

                List<SkillResourceFileEntry> metaSkillEntries = SkillResourceRepository.LoadMetaSkills(_entry != null ? _entry.UnitId : string.Empty);
                string[] options = BuildMetaSkillOptions(metaSkillEntries, _node.MetaSkillAssetName, out int currentIndex);
                int nextIndex = EditorGUILayout.Popup("metaSkill", currentIndex, options);
                if (metaSkillEntries.Count > 0 && nextIndex >= 0 && nextIndex < metaSkillEntries.Count)
                {
                    MetaSkillConfig selectedConfig = metaSkillEntries[nextIndex].Config as MetaSkillConfig;
                    string nextAssetName = !string.IsNullOrEmpty(selectedConfig?.MetaSkillId)
                        ? SkillResourceRepository.GetMetaSkillRuntimeId(metaSkillEntries[nextIndex])
                        : metaSkillEntries[nextIndex].BaseName;
                    if (!string.Equals(_node.MetaSkillAssetName, nextAssetName, StringComparison.Ordinal))
                    {
                        _node.MetaSkillAssetName = nextAssetName;
                        if (string.IsNullOrEmpty(_node.DisplayName) || _node.DisplayName == "元技能节点")
                        {
                            _node.DisplayName = SkillResourceRepository.GetDisplayName(metaSkillEntries[nextIndex]);
                        }
                    }
                }

                EditorGUILayout.LabelField("nodeId", _node.NodeId, EditorStyles.miniLabel);
                if (!string.IsNullOrEmpty(_node.MetaSkillAssetName))
                {
                    EditorGUILayout.LabelField("metaSkill", _node.MetaSkillAssetName, EditorStyles.miniLabel);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    SkillResourceRepository.MarkDirty(_entry);
                    _onModified?.Invoke();
                }
            }

            private static string[] BuildMetaSkillOptions(List<SkillResourceFileEntry> entries, string currentMetaSkillAssetName, out int currentIndex)
            {
                currentIndex = 0;
                if (entries == null || entries.Count == 0)
                {
                    return new[] { "<无可用元技能>" };
                }

                string[] options = new string[entries.Count];
                for (int i = 0; i < entries.Count; i++)
                {
                    SkillResourceFileEntry entry = entries[i];
                    MetaSkillConfig metaSkillConfig = entry.Config as MetaSkillConfig;
                    string displayName = SkillResourceRepository.GetDisplayName(entry);
                    options[i] = string.IsNullOrEmpty(metaSkillConfig?.MetaSkillId)
                        ? displayName
                        : $"{displayName} ({metaSkillConfig.MetaSkillId})";
                    if (SkillResourceRepository.IsMatchingMetaSkillReference(entry, currentMetaSkillAssetName))
                    {
                        currentIndex = i;
                    }
                }

                return options;
            }

        }

        private sealed class SkillEventInspectorPanel : ISkillEditorInspectorPanel
        {
            private readonly SkillResourceFileEntry _entry;
            private readonly SkillEventConfig _skillEvent;
            private readonly Action _onModified;

            public SkillEventInspectorPanel(SkillResourceFileEntry entry, SkillEventConfig skillEvent, Action onModified)
            {
                _entry = entry;
                _skillEvent = skillEvent;
                _onModified = onModified;
            }

            public string Header => "EventInfo";

            public void Bind(EditorWindow owner)
            {
            }

            public void OnGUI()
            {
                if (_skillEvent == null)
                {
                    EditorGUILayout.HelpBox("当前没有选中的 Event。", MessageType.Info);
                    return;
                }

                _skillEvent.Events ??= new List<SkillEventEntryConfig>();

                EditorGUI.BeginChangeCheck();
                _skillEvent.EventMode = (SkillConditionMode)EditorGUILayout.EnumPopup("eventMode", _skillEvent.EventMode);
                EditorGUILayout.LabelField("eventId", _skillEvent.EventId, EditorStyles.miniLabel);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
                for (int i = 0; i < _skillEvent.Events.Count; i++)
                {
                    SkillEventEntryConfig entry = _skillEvent.Events[i] ?? new SkillEventEntryConfig();
                    _skillEvent.Events[i] = entry;

                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();
                    entry.EventType = (SkillEventType)EditorGUILayout.EnumPopup("event", entry.EventType);
                    if (GUILayout.Button("-", GUILayout.Width(24f)))
                    {
                        _skillEvent.Events.RemoveAt(i);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                    entry.Argument = EditorGUILayout.TextField("arg", entry.Argument);
                    EditorGUILayout.EndVertical();
                }

                if (GUILayout.Button("Add Event", GUILayout.Height(28f)))
                {
                    _skillEvent.Events.Add(new SkillEventEntryConfig());
                }

                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);
                _skillEvent.ConditionMode = (SkillConditionMode)EditorGUILayout.EnumPopup("conditionMode", _skillEvent.ConditionMode);
                _skillEvent.Conditions ??= new List<SkillConditionConfig>();
                for (int i = 0; i < _skillEvent.Conditions.Count; i++)
                {
                    SkillConditionConfig condition = _skillEvent.Conditions[i] ?? new SkillConditionConfig();
                    _skillEvent.Conditions[i] = condition;

                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Condition {i + 1}", EditorStyles.boldLabel);
                    if (GUILayout.Button("-", GUILayout.Width(24f)))
                    {
                        _skillEvent.Conditions.RemoveAt(i);
                        NotifyModified();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    SkillConditionInspectorUtility.DrawCondition(condition, NotifyModified);
                    EditorGUILayout.EndVertical();
                }

                if (GUILayout.Button("Add Condition", GUILayout.Height(28f)))
                {
                    SkillConditionConfig condition = new SkillConditionConfig();
                    condition.CreateData(SkillConditionType.AttributeCompare);
                    _skillEvent.Conditions.Add(condition);
                    NotifyModified();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    NotifyModified();
                }
            }

            private void NotifyModified()
            {
                if (_entry != null)
                {
                    SkillResourceRepository.MarkDirty(_entry);
                }

                _onModified?.Invoke();
            }
        }

        private static class SkillConditionInspectorUtility
        {
            public static void DrawCondition(SkillConditionConfig config, Action onModified)
            {
                if (config == null)
                {
                    return;
                }

                SkillConditionType nextType = (SkillConditionType)EditorGUILayout.EnumPopup("ConditionType", config.ConditionType);
                if (nextType != config.ConditionType)
                {
                    config.CreateData(nextType);
                    onModified?.Invoke();
                    return;
                }

                switch (config.Data)
                {
                    case AttributeCompare_SkillConditionData attributeCompare:
                        DrawAttributeCompare(attributeCompare.Args, onModified);
                        break;
                    case HasBuff_SkillConditionData hasBuff:
                        DrawBuffCondition(hasBuff.Args, onModified);
                        break;
                    case HasTag_SkillConditionData hasTag:
                        DrawTagCondition(hasTag.Args, onModified);
                        break;
                    case LastActionSucceeded_SkillConditionData succeeded:
                        DrawActionResultCondition(succeeded.Args, onModified);
                        break;
                    case LastActionFailed_SkillConditionData failed:
                        DrawActionResultCondition(failed.Args, onModified);
                        break;
                }
            }

            private static void DrawAttributeCompare(AttributeCompareArgs args, Action onModified)
            {
                SkillQueryTargetType target = (SkillQueryTargetType)EditorGUILayout.EnumPopup("QueryTarget", args.QueryTarget);
                SkillAttributeType attribute = (SkillAttributeType)EditorGUILayout.EnumPopup("AttributeType", args.AttributeType);
                SkillCompareOperator compare = (SkillCompareOperator)EditorGUILayout.EnumPopup("CompareOperator", args.CompareOperator);
                float value = EditorGUILayout.FloatField("Value", args.Value);
                if (target != args.QueryTarget || attribute != args.AttributeType || compare != args.CompareOperator || !Mathf.Approximately(value, args.Value))
                {
                    args.QueryTarget = target;
                    args.AttributeType = attribute;
                    args.CompareOperator = compare;
                    args.Value = value;
                    onModified?.Invoke();
                }
            }

            private static void DrawBuffCondition(BuffConditionArgs args, Action onModified)
            {
                SkillQueryTargetType target = (SkillQueryTargetType)EditorGUILayout.EnumPopup("QueryTarget", args.QueryTarget);
                string buffId = EditorGUILayout.TextField("BuffId", args.BuffId);
                if (target != args.QueryTarget || buffId != args.BuffId)
                {
                    args.QueryTarget = target;
                    args.BuffId = buffId;
                    onModified?.Invoke();
                }
            }

            private static void DrawTagCondition(TagConditionArgs args, Action onModified)
            {
                SkillQueryTargetType target = (SkillQueryTargetType)EditorGUILayout.EnumPopup("QueryTarget", args.QueryTarget);
                string tag = args.Tag;
                bool tagChanged = TagSelectionEditorUtility.DrawSingleTagField("Tag", ref tag);
                if (target != args.QueryTarget || tagChanged)
                {
                    args.QueryTarget = target;
                    args.Tag = tag;
                    onModified?.Invoke();
                }
            }

            private static void DrawActionResultCondition(ActionResultConditionArgs args, Action onModified)
            {
                int actionIndex = EditorGUILayout.IntField("ActionIndex", args.ActionIndex);
                if (actionIndex != args.ActionIndex)
                {
                    args.ActionIndex = actionIndex;
                    onModified?.Invoke();
                }
            }
        }

        private sealed class TimelineInspectorPanel : ISkillEditorInspectorPanel
        {
            private const int FrameRate = 60;

            private readonly SkillResourceFileEntry _entry;
            private readonly TimelineTrackConfig _track;
            private object _item;
            private readonly Action _onModified;

            public TimelineInspectorPanel(SkillResourceFileEntry entry, TimelineTrackConfig track, object item, Action onModified)
            {
                _entry = entry;
                _track = track;
                _item = item;
                _onModified = onModified;
            }

            public string Header => "Inspector";

            public void Bind(EditorWindow owner)
            {
            }

            public void OnGUI()
            {
                if (_track == null)
                {
                    EditorGUILayout.HelpBox("当前没有选中的 Timeline 条目。", MessageType.Info);
                    return;
                }

                if (_item == null)
                {
                    DrawTrackDetails();
                    return;
                }

                if (_item is HitBoxConfig hitBox)
                {
                    DrawHitBoxDetails(hitBox);
                    return;
                }

                if (_item is BulletConfig bullet)
                {
                    DrawBulletDetails(bullet);
                    return;
                }

                if (_item is TimelineVfxConfig vfx)
                {
                    DrawVfxDetails(vfx);
                    return;
                }

                if (_item is TimelineAudioConfig audio)
                {
                    DrawAudioDetails(audio);
                    return;
                }

                if (_item is TimelineEventConfig metaSkillEvent)
                {
                    DrawEventDetails(metaSkillEvent);
                    return;
                }

                EditorGUILayout.HelpBox("当前选择的 Timeline 条目类型不受支持。", MessageType.Warning);
            }

            private void DrawTrackDetails()
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("轨道详情", EditorStyles.boldLabel);

                string displayName = EditorGUILayout.TextField("DisplayName", _track.DisplayName);
                if (displayName != _track.DisplayName)
                {
                    _track.DisplayName = displayName;
                    NotifyModified();
                }

                EditorGUILayout.EnumPopup("TrackType", _track.TrackType);
                bool enabled = EditorGUILayout.Toggle("IsEnabled", _track.IsEnabled);
                if (enabled != _track.IsEnabled)
                {
                    _track.IsEnabled = enabled;
                    NotifyModified();
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawHitBoxDetails(HitBoxConfig config)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("攻击盒详情", EditorStyles.boldLabel);
                DrawCommonItemFields(
                    config.DisplayName,
                    config.IsEnabled,
                    config.TriggerTime,
                    config.Duration,
                    true,
                    (name, enabled, triggerTime, duration) =>
                    {
                        config.DisplayName = name;
                        config.IsEnabled = enabled;
                        config.TriggerTime = triggerTime;
                        config.Duration = duration;
                    });

                SkillSocketSourceType socketSource = Draw挂点来源字段("挂点来源", config.SocketSource);
                if (socketSource != config.SocketSource)
                {
                    config.SocketSource = socketSource;
                    NotifyModified();
                }

                string attachPoint = Draw挂点字段(config.SocketSource, config.AttachPoint, "攻击盒挂点");
                if (attachPoint != config.AttachPoint)
                {
                    config.AttachPoint = attachPoint;
                    NotifyModified();
                }

                HitBoxDetectionType detectionType = Draw攻击盒类型字段(config.ShapeArgs.DetectionType);
                if (detectionType != config.ShapeArgs.DetectionType)
                {
                    config.ShapeArgs.DetectionType = detectionType;
                    NotifyModified();
                }

                Vector3 offsetPosition = EditorGUILayout.Vector3Field("位置偏移", config.ShapeArgs.OffsetPosition);
                if (offsetPosition != config.ShapeArgs.OffsetPosition)
                {
                    config.ShapeArgs.OffsetPosition = offsetPosition;
                    NotifyModified();
                }

                Vector3 offsetRotation = EditorGUILayout.Vector3Field("旋转偏移", config.ShapeArgs.OffsetRotation);
                if (offsetRotation != config.ShapeArgs.OffsetRotation)
                {
                    config.ShapeArgs.OffsetRotation = offsetRotation;
                    NotifyModified();
                }

                Vector3 scale = config.ShapeArgs.Scale;
                float length = EditorGUILayout.FloatField("长度", scale.x);
                float radius = scale.y;
                if (config.ShapeArgs.DetectionType == HitBoxDetectionType.Capsule)
                {
                    radius = EditorGUILayout.FloatField("半径", scale.y);
                }

                Vector3 nextScale = new Vector3(length, radius, scale.z);
                if (nextScale != scale)
                {
                    config.ShapeArgs.Scale = nextScale;
                    NotifyModified();
                }

                float hitInterval = EditorGUILayout.FloatField("重复命中间隔", config.ShapeArgs.HitInterval);
                if (!Mathf.Approximately(hitInterval, config.ShapeArgs.HitInterval))
                {
                    config.ShapeArgs.HitInterval = Mathf.Max(0f, hitInterval);
                    NotifyModified();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(EditorGUI.indentLevel * 15f);
                    EditorGUILayout.HelpBox("间隔为0或者小于0视为只有一次伤害。", MessageType.None);
                }

                int hitLayerMask = Draw层级遮罩字段("检测层级", config.ShapeArgs.HitLayerMask);
                if (hitLayerMask != config.ShapeArgs.HitLayerMask)
                {
                    config.ShapeArgs.HitLayerMask = hitLayerMask;
                    NotifyModified();
                }

                Draw烘焙设置(config);

                float toughnessDamage = EditorGUILayout.FloatField("削减韧性(占位)", config.OnHitResponse.ToughnessDamage);
                if (!Mathf.Approximately(toughnessDamage, config.OnHitResponse.ToughnessDamage))
                {
                    config.OnHitResponse.ToughnessDamage = Mathf.Max(0f, toughnessDamage);
                    NotifyModified();
                }

                float hitStunDuration = EditorGUILayout.FloatField("命中僵直时长(占位)", config.OnHitResponse.HitStunDuration);
                if (!Mathf.Approximately(hitStunDuration, config.OnHitResponse.HitStunDuration))
                {
                    config.OnHitResponse.HitStunDuration = Mathf.Max(0f, hitStunDuration);
                    NotifyModified();
                }

                string hitStunTag = EditorGUILayout.TextField("命中僵直标签(占位)", config.OnHitResponse.HitStunTag);
                if (hitStunTag != config.OnHitResponse.HitStunTag)
                {
                    config.OnHitResponse.HitStunTag = hitStunTag;
                    NotifyModified();
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("OnHit Effects", EditorStyles.boldLabel);
                if (GUILayout.Button("命中 effects(BT)", GUILayout.Height(30f)))
                {
                    config.OnHitEffect ??= new SkillEffectConfig();
                    OpenEffectEditor(_entry, config.OnHitEffect, $"HitBox / {config.DisplayName}", NotifyModified);
                }
                EditorGUILayout.LabelField(BuildEffectSummary(config.OnHitEffect), EditorStyles.miniLabel);

                if (GUILayout.Button("删除攻击盒", GUILayout.Width(100f)))
                {
                    _track.HitBoxes.Remove(config);
                    _item = null;
                    NotifyModified();
                }

                EditorGUILayout.EndVertical();
            }

            private static SkillSocketSourceType Draw挂点来源字段(string label, SkillSocketSourceType currentValue)
            {
                string[] options = { "角色挂点", "武器挂点" };
                int currentIndex = Mathf.Clamp((int)currentValue, 0, options.Length - 1);
                int nextIndex = EditorGUILayout.Popup(label, currentIndex, options);
                return (SkillSocketSourceType)nextIndex;
            }

            private string Draw挂点字段(SkillSocketSourceType socketSource, string currentValue, string label)
            {
                IList<PreviewMountPoint> mountPoints = Get挂点列表(socketSource);
                if (mountPoints == null || mountPoints.Count == 0)
                {
                    return EditorGUILayout.TextField(label, currentValue);
                }

                string[] options = new string[mountPoints.Count + 1];
                options[0] = "根节点";
                for (int i = 0; i < mountPoints.Count; i++)
                {
                    string socketName = mountPoints[i] != null ? mountPoints[i].SocketName : string.Empty;
                    options[i + 1] = string.IsNullOrEmpty(socketName) ? $"挂点 {i + 1}" : socketName;
                }

                int currentIndex = 0;
                for (int i = 1; i < options.Length; i++)
                {
                    if (string.Equals(options[i], currentValue, StringComparison.Ordinal))
                    {
                        currentIndex = i;
                        break;
                    }
                }

                int nextIndex = EditorGUILayout.Popup(label, currentIndex, options);
                return nextIndex <= 0 ? string.Empty : options[nextIndex];
            }

            private static IList<PreviewMountPoint> Get挂点列表(SkillSocketSourceType socketSource)
            {
                GameUnit previewConfig = SkillPreviewUnitSettings.LoadActivePreviewConfig();
                if (socketSource == SkillSocketSourceType.Character)
                {
                    return previewConfig != null ? previewConfig.MountPoints : null;
                }

                if (previewConfig == null || previewConfig.WeaponBindings == null)
                {
                    return null;
                }

                for (int i = 0; i < previewConfig.WeaponBindings.Count; i++)
                {
                    PreviewWeaponBinding binding = previewConfig.WeaponBindings[i];
                    if (binding == null)
                    {
                        continue;
                    }

                    GameObject previewWeaponPrefab = SkillPreviewUnitSettings.LoadPreviewWeaponPrefab(binding.WeaponType);
                    if (previewWeaponPrefab == null)
                    {
                        continue;
                    }

                    PreviewWeaponConfig weaponConfig = previewWeaponPrefab.GetComponent<PreviewWeaponConfig>();
                    if (weaponConfig != null)
                    {
                        return weaponConfig.MountPoints;
                    }
                }

                SkillPreviewWeaponSettingsData[] previewWeapons = SkillPreviewUnitSettings.LoadPreviewWeapons();
                for (int i = 0; i < previewWeapons.Length; i++)
                {
                    SkillPreviewWeaponSettingsData entry = previewWeapons[i];
                    if (entry == null || string.IsNullOrEmpty(entry.WeaponPrefabPath))
                    {
                        continue;
                    }

                    GameObject previewWeaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.WeaponPrefabPath);
                    if (previewWeaponPrefab == null)
                    {
                        continue;
                    }

                    PreviewWeaponConfig weaponConfig = previewWeaponPrefab.GetComponent<PreviewWeaponConfig>();
                    if (weaponConfig != null)
                    {
                        return weaponConfig.MountPoints;
                    }
                }

                return null;
            }

            private static HitBoxDetectionType Draw攻击盒类型字段(HitBoxDetectionType currentValue)
            {
                string[] options = { "胶囊", "射线" };
                int currentIndex = currentValue == HitBoxDetectionType.Raycast ? 1 : 0;
                int nextIndex = EditorGUILayout.Popup("检测类型", currentIndex, options);
                return (HitBoxDetectionType)nextIndex;
            }

            private static int Draw层级遮罩字段(string label, int currentMask)
            {
                string[] layerNames = InternalEditorUtility.layers;
                int compactMask = 0;
                for (int i = 0; i < layerNames.Length; i++)
                {
                    int layer = LayerMask.NameToLayer(layerNames[i]);
                    if (layer >= 0 && (currentMask & (1 << layer)) != 0)
                    {
                        compactMask |= 1 << i;
                    }
                }

                int nextCompactMask = EditorGUILayout.MaskField(label, compactMask, layerNames);
                int expandedMask = 0;
                for (int i = 0; i < layerNames.Length; i++)
                {
                    if ((nextCompactMask & (1 << i)) == 0)
                    {
                        continue;
                    }

                    int layer = LayerMask.NameToLayer(layerNames[i]);
                    if (layer >= 0)
                    {
                        expandedMask |= 1 << layer;
                    }
                }

                return expandedMask;
            }

            private void Draw烘焙设置(HitBoxConfig config)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("烘焙", EditorStyles.boldLabel);
                int bakeCount = EditorGUILayout.IntField("烘焙数量", config.ShapeArgs.BakeCount);
                int sanitizedBakeCount = Mathf.Max(0, bakeCount);
                if (sanitizedBakeCount != config.ShapeArgs.BakeCount)
                {
                    config.ShapeArgs.BakeCount = sanitizedBakeCount;
                    NotifyModified();
                }

                int bakedCount = config.ShapeArgs.BakedParts != null ? config.ShapeArgs.BakedParts.Count : 0;
                Color previousColor = GUI.color;
                GUI.color = bakedCount > 0 ? Color.white : Color.red;
                EditorGUILayout.LabelField(
                    bakedCount > 0 ? $"攻击盒已烘焙数量: {bakedCount}" : "!!从未烘焙过攻击盒!!",
                    EditorStyles.miniBoldLabel);
                GUI.color = previousColor;

                StateTimelineEditorWindow timelineWindow = StateTimelineEditorWindow.GetActiveInstance();
                using (new GUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(config.Duration <= 0f || timelineWindow == null))
                    {
                        if (GUILayout.Button("烘焙", GUILayout.Width(60f)))
                        {
                            timelineWindow.BakeHitBoxPreviewFromInspector(config);
                        }
                    }

                    using (new EditorGUI.DisabledScope(bakedCount == 0))
                    {
                        if (GUILayout.Button("清空烘焙", GUILayout.Width(80f)))
                        {
                            config.ShapeArgs.BakedParts = new System.Collections.Generic.List<HitBoxBakedPart>();
                            NotifyModified();
                        }
                    }
                }

                string[] drawModeOptions = StateTimelineEditorWindow.GetHitBoxDrawModeLabels();
                StateTimelineEditorWindow.BoxDrawType =
                    EditorGUILayout.Popup("绘制类型", StateTimelineEditorWindow.BoxDrawType, drawModeOptions);
                if (StateTimelineEditorWindow.BoxDrawType == 1 || StateTimelineEditorWindow.BoxDrawType == 2)
                {
                    StateTimelineEditorWindow.BakerBoxLife =
                        EditorGUILayout.IntSlider("烘焙攻击盒显示寿命", StateTimelineEditorWindow.BakerBoxLife, 0, 1000);
                }

                EditorGUILayout.HelpBox("烘焙会把攻击盒在持续时间内每次采样得到的局部起点、方向、触发时间保存成 baked parts。运行时优先使用这些 baked parts 做判定，而不是每帧重新按挂点实时计算。", MessageType.Info);
            }

            private void DrawBulletDetails(BulletConfig config)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("子弹详情", EditorStyles.boldLabel);
                DrawCommonItemFields(
                    config.DisplayName,
                    config.IsEnabled,
                    config.TriggerTime,
                    config.Duration,
                    true,
                    (name, enabled, triggerTime, duration) =>
                    {
                        config.DisplayName = name;
                        config.IsEnabled = enabled;
                        config.TriggerTime = triggerTime;
                        config.Duration = duration;
                    });

                SkillSocketSourceType socketSource = Draw挂点来源字段("挂点来源", config.SocketSource);
                if (socketSource != config.SocketSource)
                {
                    config.SocketSource = socketSource;
                    NotifyModified();
                }

                string attachPoint = Draw挂点字段(config.SocketSource, config.AttachPoint, "发射器挂点");
                if (attachPoint != config.AttachPoint)
                {
                    config.AttachPoint = attachPoint;
                    NotifyModified();
                }

                GameObject bulletPrefab = string.IsNullOrEmpty(config.SpawnArgs.BulletPrefabPath)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<GameObject>(config.SpawnArgs.BulletPrefabPath);
                GameObject nextBulletPrefab = (GameObject)EditorGUILayout.ObjectField("BulletPrefab", bulletPrefab, typeof(GameObject), false);
                string nextBulletPrefabPath = nextBulletPrefab == null ? string.Empty : AssetDatabase.GetAssetPath(nextBulletPrefab);
                if (!string.Equals(nextBulletPrefabPath, config.SpawnArgs.BulletPrefabPath, StringComparison.Ordinal))
                {
                    config.SpawnArgs.BulletPrefabPath = nextBulletPrefabPath;
                    NotifyModified();
                }

                BulletFlightMode flightMode = (BulletFlightMode)EditorGUILayout.EnumPopup("FlightMode", config.SpawnArgs.FlightMode);
                if (flightMode != config.SpawnArgs.FlightMode)
                {
                    config.SpawnArgs.FlightMode = flightMode;
                    NotifyModified();
                }

                int spawnCount = EditorGUILayout.IntField("SpawnCount", config.SpawnArgs.SpawnCount);
                int sanitizedSpawnCount = Mathf.Max(1, spawnCount);
                if (sanitizedSpawnCount != config.SpawnArgs.SpawnCount)
                {
                    config.SpawnArgs.SpawnCount = sanitizedSpawnCount;
                    NotifyModified();
                }

                Vector3 positionOffset = EditorGUILayout.Vector3Field("PositionOffset", config.SpawnArgs.PositionOffset);
                if (positionOffset != config.SpawnArgs.PositionOffset)
                {
                    config.SpawnArgs.PositionOffset = positionOffset;
                    NotifyModified();
                }

                Vector3 rotationOffset = EditorGUILayout.Vector3Field("RotationOffset", config.SpawnArgs.RotationOffset);
                if (rotationOffset != config.SpawnArgs.RotationOffset)
                {
                    config.SpawnArgs.RotationOffset = rotationOffset;
                    NotifyModified();
                }

                float speed = EditorGUILayout.FloatField("Speed", config.SpawnArgs.Speed);
                if (!Mathf.Approximately(speed, config.SpawnArgs.Speed))
                {
                    config.SpawnArgs.Speed = Mathf.Max(0f, speed);
                    NotifyModified();
                }

                float maxLifetime = EditorGUILayout.FloatField("MaxLifetime", config.SpawnArgs.MaxLifetime);
                if (!Mathf.Approximately(maxLifetime, config.SpawnArgs.MaxLifetime))
                {
                    config.SpawnArgs.MaxLifetime = Mathf.Max(0.01f, maxLifetime);
                    NotifyModified();
                }

                float collisionRadius = EditorGUILayout.FloatField("CollisionRadius", config.SpawnArgs.CollisionRadius);
                if (!Mathf.Approximately(collisionRadius, config.SpawnArgs.CollisionRadius))
                {
                    config.SpawnArgs.CollisionRadius = Mathf.Max(0f, collisionRadius);
                    NotifyModified();
                }

                if (config.SpawnArgs.FlightMode == BulletFlightMode.Parabola || config.SpawnArgs.FlightMode == BulletFlightMode.HomingParabola)
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("Parabola", EditorStyles.boldLabel);

                    float initialVerticalSpeed = EditorGUILayout.FloatField("InitialVerticalSpeed", config.SpawnArgs.Parabola.InitialVerticalSpeed);
                    if (!Mathf.Approximately(initialVerticalSpeed, config.SpawnArgs.Parabola.InitialVerticalSpeed))
                    {
                        config.SpawnArgs.Parabola.InitialVerticalSpeed = initialVerticalSpeed;
                        NotifyModified();
                    }

                    float gravity = EditorGUILayout.FloatField("Gravity", config.SpawnArgs.Parabola.Gravity);
                    if (!Mathf.Approximately(gravity, config.SpawnArgs.Parabola.Gravity))
                    {
                        config.SpawnArgs.Parabola.Gravity = Mathf.Max(0f, gravity);
                        NotifyModified();
                    }
                }

                int hitLayerMask = Draw层级遮罩字段("HitLayerMask", config.SpawnArgs.HitLayerMask);
                if (hitLayerMask != config.SpawnArgs.HitLayerMask)
                {
                    config.SpawnArgs.HitLayerMask = hitLayerMask;
                    NotifyModified();
                }

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Tracking (预留)", EditorStyles.boldLabel);
                float searchRange = EditorGUILayout.FloatField("SearchRange", config.SpawnArgs.Tracking.SearchRange);
                if (!Mathf.Approximately(searchRange, config.SpawnArgs.Tracking.SearchRange))
                {
                    config.SpawnArgs.Tracking.SearchRange = Mathf.Max(0f, searchRange);
                    NotifyModified();
                }

                float searchAngle = EditorGUILayout.Slider("SearchAngle", config.SpawnArgs.Tracking.SearchAngle, 0f, 180f);
                if (!Mathf.Approximately(searchAngle, config.SpawnArgs.Tracking.SearchAngle))
                {
                    config.SpawnArgs.Tracking.SearchAngle = Mathf.Clamp(searchAngle, 0f, 180f);
                    NotifyModified();
                }

                float centerWeight = EditorGUILayout.Slider("CenterWeight", config.SpawnArgs.Tracking.CenterWeight, 0f, 1f);
                if (!Mathf.Approximately(centerWeight, config.SpawnArgs.Tracking.CenterWeight))
                {
                    config.SpawnArgs.Tracking.CenterWeight = Mathf.Clamp01(centerWeight);
                    NotifyModified();
                }

                float acceleration = EditorGUILayout.FloatField("Acceleration", config.SpawnArgs.Tracking.Acceleration);
                if (!Mathf.Approximately(acceleration, config.SpawnArgs.Tracking.Acceleration))
                {
                    config.SpawnArgs.Tracking.Acceleration = Mathf.Max(0f, acceleration);
                    NotifyModified();
                }

                float straightDistance = EditorGUILayout.FloatField("StraightDistance", config.SpawnArgs.Tracking.StraightDistance);
                if (!Mathf.Approximately(straightDistance, config.SpawnArgs.Tracking.StraightDistance))
                {
                    config.SpawnArgs.Tracking.StraightDistance = Mathf.Max(0.05f, straightDistance);
                    NotifyModified();
                }

                float curveStrength = EditorGUILayout.FloatField("CurveStrength", config.SpawnArgs.Tracking.CurveStrength);
                if (!Mathf.Approximately(curveStrength, config.SpawnArgs.Tracking.CurveStrength))
                {
                    config.SpawnArgs.Tracking.CurveStrength = Mathf.Max(0f, curveStrength);
                    NotifyModified();
                }

                float curveLateralOffset = EditorGUILayout.FloatField("CurveLateralOffset", config.SpawnArgs.Tracking.CurveLateralOffset);
                if (!Mathf.Approximately(curveLateralOffset, config.SpawnArgs.Tracking.CurveLateralOffset))
                {
                    config.SpawnArgs.Tracking.CurveLateralOffset = Mathf.Max(0f, curveLateralOffset);
                    NotifyModified();
                }

                float curveVerticalOffset = EditorGUILayout.FloatField("CurveVerticalOffset", config.SpawnArgs.Tracking.CurveVerticalOffset);
                if (!Mathf.Approximately(curveVerticalOffset, config.SpawnArgs.Tracking.CurveVerticalOffset))
                {
                    config.SpawnArgs.Tracking.CurveVerticalOffset = Mathf.Max(0f, curveVerticalOffset);
                    NotifyModified();
                }

                float curveOscillation = EditorGUILayout.FloatField("CurveOscillation", config.SpawnArgs.Tracking.CurveOscillation);
                if (!Mathf.Approximately(curveOscillation, config.SpawnArgs.Tracking.CurveOscillation))
                {
                    config.SpawnArgs.Tracking.CurveOscillation = Mathf.Max(0f, curveOscillation);
                    NotifyModified();
                }

                float launchYawRange = EditorGUILayout.Slider("LaunchYawRange", config.SpawnArgs.Tracking.LaunchYawRange, 0f, 180f);
                if (!Mathf.Approximately(launchYawRange, config.SpawnArgs.Tracking.LaunchYawRange))
                {
                    config.SpawnArgs.Tracking.LaunchYawRange = Mathf.Clamp(launchYawRange, 0f, 180f);
                    NotifyModified();
                }

                float launchPitchRange = EditorGUILayout.Slider("LaunchPitchRange", config.SpawnArgs.Tracking.LaunchPitchRange, 0f, 89f);
                if (!Mathf.Approximately(launchPitchRange, config.SpawnArgs.Tracking.LaunchPitchRange))
                {
                    config.SpawnArgs.Tracking.LaunchPitchRange = Mathf.Clamp(launchPitchRange, 0f, 89f);
                    NotifyModified();
                }

                EditorGUILayout.HelpBox("Duration 小于等于 0 时，会在 TriggerTime 一次性发射 SpawnCount 个子弹；Duration 大于 0 时，会在该持续时间内按 SpawnCount 均匀发射多枚子弹。当前已实现 Direct、Parabola、HomingParabola 和 HomingCurve。CenterWeight 中，0 表示更偏向距离最近，1 表示更偏向视野中心。HomingParabola 会在发射瞬间锁定一次目标，之后按固定抛物线飞行。HomingCurve 会先按 LaunchYawRange 和 LaunchPitchRange 随机出射，再逐步绕弧跟随目标，并在进入 StraightDistance 后切为直线追击；CurveStrength、CurveLateralOffset、CurveVerticalOffset 一起决定弧度强弱；Speed 为初速度，Acceleration 为飞行过程中的加速度。", MessageType.Info);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("OnHit Effects", EditorStyles.boldLabel);
                if (GUILayout.Button("命中 effects(BT)", GUILayout.Height(30f)))
                {
                    config.OnHitEffect ??= new SkillEffectConfig();
                    OpenEffectEditor(_entry, config.OnHitEffect, $"Bullet / {config.DisplayName}", NotifyModified);
                }
                EditorGUILayout.LabelField(BuildEffectSummary(config.OnHitEffect), EditorStyles.miniLabel);

                if (GUILayout.Button("删除子弹", GUILayout.Width(100f)))
                {
                    _track.Bullets.Remove(config);
                    _item = null;
                    NotifyModified();
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawEventDetails(TimelineEventConfig config)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("事件详情", EditorStyles.boldLabel);
                DrawCommonItemFields(
                    config.DisplayName,
                    config.IsEnabled,
                    config.TriggerTime,
                    config.Duration,
                    EventSupportsDuration(config),
                    (name, enabled, triggerTime, duration) =>
                    {
                        config.DisplayName = name;
                        config.IsEnabled = enabled;
                        config.TriggerTime = triggerTime;
                        config.Duration = duration;
                    });

                TimelineEventType currentType = config.EventType;
                TimelineEventType nextType = (TimelineEventType)EditorGUILayout.EnumPopup("EventType", currentType);
                if (nextType != currentType)
                {
                    config.CreateData(nextType);
                    if (string.IsNullOrEmpty(config.DisplayName) || config.DisplayName == "事件")
                    {
                        config.DisplayName = GetDefaultEventItemName(nextType);
                    }

                    ApplyEventTypeDefaults(config);
                    NotifyModified();
                }

                if (config.Data == null && nextType != TimelineEventType.None)
                {
                    config.CreateData(nextType);
                    ApplyEventTypeDefaults(config);
                    NotifyModified();
                }

                if (config.Data != null)
                {
                    EditorGUILayout.HelpBox(
                        config.Data.SupportsDuration
                            ? "当前单位事件支持持续时间：Duration < 0 表示整个执行轨道持续，= 0 表示单帧触发，> 0 表示在该时段内持续生效。物理事件勾选“覆盖后摇动画”后，仅在 Duration < 0 时会延续到后摇阶段。"
                            : "当前单位事件是单次型事件：只能移动触发时机，不能拉伸持续时间。",
                        MessageType.None);
                }

                if (config.Data is SoftLockTarget_TimelineEventData softLockTarget)
                {
                    float radius = EditorGUILayout.FloatField("最大半径", softLockTarget.Args.Radius);
                    if (!Mathf.Approximately(radius, softLockTarget.Args.Radius))
                    {
                        softLockTarget.Args.Radius = radius;
                        NotifyModified();
                    }

                    float angle = EditorGUILayout.FloatField("最大角度差", softLockTarget.Args.Angle);
                    if (!Mathf.Approximately(angle, softLockTarget.Args.Angle))
                    {
                        softLockTarget.Args.Angle = angle;
                        NotifyModified();
                    }

                    int layerMask = Draw层级遮罩字段("锁定层级", softLockTarget.Args.LayerMask);
                    if (layerMask != softLockTarget.Args.LayerMask)
                    {
                        softLockTarget.Args.LayerMask = layerMask;
                        NotifyModified();
                    }

                    bool referToCamera = EditorGUILayout.Toggle("参考锁定方向至相机", softLockTarget.Args.ReferToCamera);
                    if (referToCamera != softLockTarget.Args.ReferToCamera)
                    {
                        softLockTarget.Args.ReferToCamera = referToCamera;
                        NotifyModified();
                    }

                    float rotationSpeed = EditorGUILayout.FloatField("旋转速度", softLockTarget.Args.RotationSpeed);
                    if (!Mathf.Approximately(rotationSpeed, softLockTarget.Args.RotationSpeed))
                    {
                        softLockTarget.Args.RotationSpeed = Mathf.Max(0f, rotationSpeed);
                        NotifyModified();
                    }

                    int priority = EditorGUILayout.IntField("旋转优先级", softLockTarget.Args.Priority);
                    if (priority != softLockTarget.Args.Priority)
                    {
                        softLockTarget.Args.Priority = priority;
                        NotifyModified();
                    }
                }
                else if (config.Data is HitStop_TimelineEventData hitStop)
                {
                    HitStopEventArgs args = hitStop.Args;
                    FeedbackTriggerMode triggerMode = (FeedbackTriggerMode)EditorGUILayout.EnumPopup("触发模式", args.TriggerMode);
                    if (triggerMode != args.TriggerMode) { args.TriggerMode = triggerMode; NotifyModified(); }
                    if (args.TriggerMode == FeedbackTriggerMode.OnHit && Mathf.Approximately(config.Duration, 0f))
                    {
                        EditorGUILayout.HelpBox("OnHit 模式需要 Duration > 0 或 Duration < 0；Duration 表示命中监听窗口。", MessageType.Warning);
                    }

                    bool affectAttacker = EditorGUILayout.Toggle("影响攻击者", args.AffectAttacker);
                    if (affectAttacker != args.AffectAttacker) { args.AffectAttacker = affectAttacker; NotifyModified(); }
                    if (args.AffectAttacker)
                    {
                        float duration = EditorGUILayout.FloatField("攻击者停顿时长", args.AttackerDuration);
                        float scale = EditorGUILayout.Slider("攻击者时间倍率", args.AttackerTimeScale, 0f, 1f);
                        if (!Mathf.Approximately(duration, args.AttackerDuration)) { args.AttackerDuration = Mathf.Max(0f, duration); NotifyModified(); }
                        if (!Mathf.Approximately(scale, args.AttackerTimeScale)) { args.AttackerTimeScale = scale; NotifyModified(); }
                    }

                    bool affectDefender = EditorGUILayout.Toggle("影响受击者", args.AffectDefender);
                    if (affectDefender != args.AffectDefender) { args.AffectDefender = affectDefender; NotifyModified(); }
                    if (args.AffectDefender)
                    {
                        float duration = EditorGUILayout.FloatField("受击者停顿时长", args.DefenderDuration);
                        float scale = EditorGUILayout.Slider("受击者时间倍率", args.DefenderTimeScale, 0f, 1f);
                        if (!Mathf.Approximately(duration, args.DefenderDuration)) { args.DefenderDuration = Mathf.Max(0f, duration); NotifyModified(); }
                        if (!Mathf.Approximately(scale, args.DefenderTimeScale)) { args.DefenderTimeScale = scale; NotifyModified(); }
                    }

                    bool triggerOnce = EditorGUILayout.Toggle("单事件仅触发一次", args.TriggerOncePerEvent);
                    bool mergeSameFrame = EditorGUILayout.Toggle("合并同帧多目标", args.MergeSameFrameHits);
                    int priority = EditorGUILayout.IntField("优先级", args.Priority);
                    if (triggerOnce != args.TriggerOncePerEvent) { args.TriggerOncePerEvent = triggerOnce; NotifyModified(); }
                    if (mergeSameFrame != args.MergeSameFrameHits) { args.MergeSameFrameHits = mergeSameFrame; NotifyModified(); }
                    if (priority != args.Priority) { args.Priority = priority; NotifyModified(); }
                }
                else if (config.Data is CameraShake_TimelineEventData cameraShake)
                {
                    CameraShakeEventArgs args = cameraShake.Args;
                    FeedbackTriggerMode triggerMode = (FeedbackTriggerMode)EditorGUILayout.EnumPopup("触发模式", args.TriggerMode);
                    if (triggerMode != args.TriggerMode) { args.TriggerMode = triggerMode; NotifyModified(); }
                    if (args.TriggerMode == FeedbackTriggerMode.OnHit && Mathf.Approximately(config.Duration, 0f))
                    {
                        EditorGUILayout.HelpBox("OnHit 模式需要 Duration > 0 或 Duration < 0；Duration 表示命中监听窗口。", MessageType.Warning);
                    }

                    float amplitude = EditorGUILayout.FloatField("振幅", args.Amplitude);
                    float frequency = EditorGUILayout.FloatField("频率", args.Frequency);
                    float shakeDuration = EditorGUILayout.FloatField("震屏时长", args.ShakeDuration);
                    Vector3 direction = EditorGUILayout.Vector3Field("默认方向", args.Direction);
                    bool useHitDirection = EditorGUILayout.Toggle("使用命中方向", args.UseHitDirection);
                    bool triggerOnce = EditorGUILayout.Toggle("单事件仅触发一次", args.TriggerOncePerEvent);
                    bool mergeSameFrame = EditorGUILayout.Toggle("合并同帧多目标", args.MergeSameFrameHits);
                    if (!Mathf.Approximately(amplitude, args.Amplitude)) { args.Amplitude = Mathf.Max(0f, amplitude); NotifyModified(); }
                    if (!Mathf.Approximately(frequency, args.Frequency)) { args.Frequency = Mathf.Max(0.01f, frequency); NotifyModified(); }
                    if (!Mathf.Approximately(shakeDuration, args.ShakeDuration)) { args.ShakeDuration = Mathf.Max(0f, shakeDuration); NotifyModified(); }
                    if (direction != (Vector3)args.Direction) { args.Direction = direction; NotifyModified(); }
                    if (useHitDirection != args.UseHitDirection) { args.UseHitDirection = useHitDirection; NotifyModified(); }
                    if (triggerOnce != args.TriggerOncePerEvent) { args.TriggerOncePerEvent = triggerOnce; NotifyModified(); }
                    if (mergeSameFrame != args.MergeSameFrameHits) { args.MergeSameFrameHits = mergeSameFrame; NotifyModified(); }
                }
                else if (config.Data is HitVfx_TimelineEventData hitVfx)
                {
                    HitVfxEventArgs args = hitVfx.Args;
                    FeedbackTriggerMode mode = (FeedbackTriggerMode)EditorGUILayout.EnumPopup("触发模式", args.TriggerMode);
                    if (mode != args.TriggerMode) { args.TriggerMode = mode; NotifyModified(); }
                    if (args.TriggerMode == FeedbackTriggerMode.OnHit && Mathf.Approximately(config.Duration, 0f)) EditorGUILayout.HelpBox("OnHit 模式需要 Duration > 0 或 Duration < 0。", MessageType.Warning);
                    GameObject prefab = string.IsNullOrEmpty(args.PrefabPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(args.PrefabPath);
                    GameObject nextPrefab = (GameObject)EditorGUILayout.ObjectField("特效 Prefab", prefab, typeof(GameObject), false);
                    string path = nextPrefab == null ? string.Empty : AssetDatabase.GetAssetPath(nextPrefab);
                    if (path != args.PrefabPath) { args.PrefabPath = path; NotifyModified(); }
                    if (nextPrefab != null && nextPrefab.GetComponentInChildren<ParticleSystem>(true) == null) EditorGUILayout.HelpBox("Prefab 中没有 ParticleSystem。", MessageType.Warning);
                    VfxPlaySpace space = (VfxPlaySpace)EditorGUILayout.EnumPopup("播放空间", args.Space);
                    HitVfxRotationMode rotation = (HitVfxRotationMode)EditorGUILayout.EnumPopup("旋转模式", args.RotationMode);
                    Vector3 pos = EditorGUILayout.Vector3Field("位置偏移", args.PositionOffset);
                    Vector3 rot = EditorGUILayout.Vector3Field("旋转偏移", args.RotationOffset);
                    Vector3 scale = EditorGUILayout.Vector3Field("缩放", args.Scale);
                    float life = EditorGUILayout.FloatField("生命周期", args.Lifetime);
                    bool unscaled = EditorGUILayout.Toggle("使用非缩放时间", args.UseUnscaledTime);
                    bool once = EditorGUILayout.Toggle("单事件仅触发一次", args.TriggerOncePerEvent);
                    bool merge = EditorGUILayout.Toggle("合并同帧多目标", args.MergeSameFrameHits);
                    if (space != args.Space) { args.Space = space; NotifyModified(); }
                    if (rotation != args.RotationMode) { args.RotationMode = rotation; NotifyModified(); }
                    if (pos != (Vector3)args.PositionOffset) { args.PositionOffset = pos; NotifyModified(); }
                    if (rot != (Vector3)args.RotationOffset) { args.RotationOffset = rot; NotifyModified(); }
                    if (scale != (Vector3)args.Scale) { args.Scale = scale; NotifyModified(); }
                    if (!Mathf.Approximately(life, args.Lifetime)) { args.Lifetime = Mathf.Max(0.01f, life); NotifyModified(); }
                    if (unscaled != args.UseUnscaledTime) { args.UseUnscaledTime = unscaled; NotifyModified(); }
                    if (once != args.TriggerOncePerEvent) { args.TriggerOncePerEvent = once; NotifyModified(); }
                    if (merge != args.MergeSameFrameHits) { args.MergeSameFrameHits = merge; NotifyModified(); }
                }
                else if (config.Data is HitAudio_TimelineEventData hitAudio)
                {
                    HitAudioEventArgs args = hitAudio.Args;
                    FeedbackTriggerMode mode = (FeedbackTriggerMode)EditorGUILayout.EnumPopup("触发模式", args.TriggerMode);
                    if (mode != args.TriggerMode) { args.TriggerMode = mode; NotifyModified(); }
                    if (args.TriggerMode == FeedbackTriggerMode.OnHit && Mathf.Approximately(config.Duration, 0f)) EditorGUILayout.HelpBox("OnHit 模式需要 Duration > 0 或 Duration < 0。", MessageType.Warning);
                    AudioClip clip = string.IsNullOrEmpty(args.AudioClipPath) ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(args.AudioClipPath);
                    AudioClip nextClip = (AudioClip)EditorGUILayout.ObjectField("AudioClip", clip, typeof(AudioClip), false);
                    string clipPath = nextClip == null ? string.Empty : AssetDatabase.GetAssetPath(nextClip);
                    if (clipPath != args.AudioClipPath) { args.AudioClipPath = clipPath; NotifyModified(); }
                    UnityEngine.Audio.AudioMixerGroup group = null;
                    if (!string.IsNullOrEmpty(args.AudioMixerPath) && !string.IsNullOrEmpty(args.MixerGroupName))
                    {
                        UnityEngine.Audio.AudioMixer mixer = AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>(args.AudioMixerPath);
                        UnityEngine.Audio.AudioMixerGroup[] groups = mixer != null ? mixer.FindMatchingGroups(args.MixerGroupName) : null;
                        if (groups != null && groups.Length > 0) group = groups[0];
                    }
                    UnityEngine.Audio.AudioMixerGroup nextGroup = (UnityEngine.Audio.AudioMixerGroup)EditorGUILayout.ObjectField("Mixer Group", group, typeof(UnityEngine.Audio.AudioMixerGroup), false);
                    string mixerPath = nextGroup == null ? string.Empty : AssetDatabase.GetAssetPath(nextGroup.audioMixer);
                    string groupName = nextGroup == null ? string.Empty : nextGroup.name;
                    if (mixerPath != args.AudioMixerPath || groupName != args.MixerGroupName) { args.AudioMixerPath = mixerPath; args.MixerGroupName = groupName; NotifyModified(); }
                    AudioPlaySpace space = (AudioPlaySpace)EditorGUILayout.EnumPopup("播放空间", args.Space);
                    float volume = EditorGUILayout.Slider("音量", args.Volume, 0f, 1f);
                    float pitch = EditorGUILayout.Slider("Pitch", args.Pitch, 0.01f, 3f);
                    float blend = EditorGUILayout.Slider("Spatial Blend", args.SpatialBlend, 0f, 1f);
                    float minDistance = EditorGUILayout.FloatField("Min Distance", args.MinDistance);
                    float maxDistance = EditorGUILayout.FloatField("Max Distance", args.MaxDistance);
                    bool once = EditorGUILayout.Toggle("单事件仅触发一次", args.TriggerOncePerEvent);
                    bool merge = EditorGUILayout.Toggle("合并同帧多目标", args.MergeSameFrameHits);
                    if (space != args.Space) { args.Space = space; NotifyModified(); }
                    if (!Mathf.Approximately(volume, args.Volume)) { args.Volume = volume; NotifyModified(); }
                    if (!Mathf.Approximately(pitch, args.Pitch)) { args.Pitch = pitch; NotifyModified(); }
                    if (!Mathf.Approximately(blend, args.SpatialBlend)) { args.SpatialBlend = blend; NotifyModified(); }
                    if (!Mathf.Approximately(minDistance, args.MinDistance)) { args.MinDistance = Mathf.Max(0.01f, minDistance); NotifyModified(); }
                    if (!Mathf.Approximately(maxDistance, args.MaxDistance)) { args.MaxDistance = Mathf.Max(args.MinDistance, maxDistance); NotifyModified(); }
                    if (once != args.TriggerOncePerEvent) { args.TriggerOncePerEvent = once; NotifyModified(); }
                    if (merge != args.MergeSameFrameHits) { args.MergeSameFrameHits = merge; NotifyModified(); }
                }
                else if (config.Data != null && config.Data.ArgsObject is ApplyForceEventArgs applyForceArgs)
                {
                    Vector3 force = EditorGUILayout.Vector3Field("Force", applyForceArgs.Force);
                    if (force != applyForceArgs.Force)
                    {
                        applyForceArgs.Force = force;
                        NotifyModified();
                    }

                    bool useLocalSpace = EditorGUILayout.Toggle("UseLocalSpace", applyForceArgs.UseLocalSpace);
                    if (useLocalSpace != applyForceArgs.UseLocalSpace)
                    {
                        applyForceArgs.UseLocalSpace = useLocalSpace;
                        NotifyModified();
                    }

                    bool overrideRecoveryAnimation = EditorGUILayout.Toggle("OverrideRecoveryAnimation", applyForceArgs.OverrideRecoveryAnimation);
                    if (overrideRecoveryAnimation != applyForceArgs.OverrideRecoveryAnimation)
                    {
                        applyForceArgs.OverrideRecoveryAnimation = overrideRecoveryAnimation;
                        NotifyModified();
                    }
                }
                else if (config.Data != null && config.Data.ArgsObject is GravityEventArgs gravityArgs)
                {
                    bool enableGravity = EditorGUILayout.Toggle("EnableGravity", gravityArgs.EnableGravity);
                    if (enableGravity != gravityArgs.EnableGravity)
                    {
                        gravityArgs.EnableGravity = enableGravity;
                        NotifyModified();
                    }

                    bool overrideGravityVector = EditorGUILayout.Toggle("OverrideGravityVector", gravityArgs.OverrideGravityVector);
                    if (overrideGravityVector != gravityArgs.OverrideGravityVector)
                    {
                        gravityArgs.OverrideGravityVector = overrideGravityVector;
                        NotifyModified();
                    }

                    using (new EditorGUI.DisabledScope(!gravityArgs.OverrideGravityVector))
                    {
                        Vector3 gravity = EditorGUILayout.Vector3Field("Gravity", gravityArgs.Gravity);
                        if (gravity != gravityArgs.Gravity)
                        {
                            gravityArgs.Gravity = gravity;
                            NotifyModified();
                        }
                    }

                    bool overrideRecoveryAnimation = EditorGUILayout.Toggle("OverrideRecoveryAnimation", gravityArgs.OverrideRecoveryAnimation);
                    if (overrideRecoveryAnimation != gravityArgs.OverrideRecoveryAnimation)
                    {
                        gravityArgs.OverrideRecoveryAnimation = overrideRecoveryAnimation;
                        NotifyModified();
                    }
                }
                else if (config.Data != null && config.Data.ArgsObject is LaunchByHeightEventArgs launchArgs)
                {
                    float targetHeight = EditorGUILayout.FloatField("TargetHeight", launchArgs.TargetHeight);
                    bool useAttribute = EditorGUILayout.Toggle("UseHeightBonusAttribute", launchArgs.UseHeightBonusAttribute);
                    SkillAttributeType attribute = (SkillAttributeType)EditorGUILayout.EnumPopup("HeightBonusAttribute", launchArgs.HeightBonusAttribute);
                    float attributeScale = EditorGUILayout.FloatField("AttributeScale", launchArgs.AttributeScale);
                    float ungroundDuration = EditorGUILayout.FloatField("ForceUngroundDuration", launchArgs.ForceUngroundDuration);
                    if (!Mathf.Approximately(targetHeight, launchArgs.TargetHeight) ||
                        useAttribute != launchArgs.UseHeightBonusAttribute || attribute != launchArgs.HeightBonusAttribute ||
                        !Mathf.Approximately(attributeScale, launchArgs.AttributeScale) ||
                        !Mathf.Approximately(ungroundDuration, launchArgs.ForceUngroundDuration))
                    {
                        launchArgs.TargetHeight = Mathf.Max(0f, targetHeight);
                        launchArgs.UseHeightBonusAttribute = useAttribute;
                        launchArgs.HeightBonusAttribute = attribute;
                        launchArgs.AttributeScale = attributeScale;
                        launchArgs.ForceUngroundDuration = Mathf.Max(0f, ungroundDuration);
                        NotifyModified();
                    }
                }
                else if (config.Data != null && config.Data.ArgsObject is AddTagEventArgs addTagArgs)
                {
                    addTagArgs.Tags ??= new List<string>();
                    if (TagSelectionEditorUtility.DrawTagList("Tags", addTagArgs.Tags))
                    {
                        NotifyModified();
                    }

                    int stack = EditorGUILayout.IntField("Stack", addTagArgs.Stack);
                    int sanitizedStack = Mathf.Max(1, stack);
                    if (sanitizedStack != addTagArgs.Stack)
                    {
                        addTagArgs.Stack = sanitizedStack;
                        NotifyModified();
                    }
                }

                if (GUILayout.Button("删除事件", GUILayout.Width(100f)))
                {
                    _track.MetaSkillEvents.Remove(config);
                    _item = null;
                    NotifyModified();
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawVfxDetails(TimelineVfxConfig config)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("特效详情", EditorStyles.boldLabel);

                TimelineVfxMode mode = (TimelineVfxMode)EditorGUILayout.EnumPopup("Mode", config.Mode);
                DrawCommonItemFields(
                    config.DisplayName,
                    config.IsEnabled,
                    config.TriggerTime,
                    config.Duration,
                    mode == TimelineVfxMode.Controlled,
                    (name, enabled, triggerTime, duration) =>
                    {
                        config.DisplayName = name;
                        config.IsEnabled = enabled;
                        config.TriggerTime = triggerTime;
                        config.Duration = mode == TimelineVfxMode.Controlled ? duration : 0f;
                    });

                if (mode != config.Mode)
                {
                    config.Mode = mode;
                    config.Duration = mode == TimelineVfxMode.Controlled ? Mathf.Max(1f / FrameRate, config.Duration) : 0f;
                    NotifyModified();
                }

                GameObject currentPrefab = string.IsNullOrEmpty(config.PrefabPath)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<GameObject>(config.PrefabPath);
                GameObject prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", currentPrefab, typeof(GameObject), false);
                string prefabPath = prefab != null ? AssetDatabase.GetAssetPath(prefab) : string.Empty;
                if (prefabPath != config.PrefabPath)
                {
                    config.PrefabPath = prefabPath;
                    if (prefab != null && (string.IsNullOrEmpty(config.DisplayName) || config.DisplayName.StartsWith("特效")))
                    {
                        config.DisplayName = prefab.name;
                    }
                    NotifyModified();
                }

                EditorGUI.BeginChangeCheck();
                SkillSocketSourceType socketSource = Draw挂点来源字段("挂点来源", config.SocketSource);
                if (socketSource != config.SocketSource)
                {
                    config.SocketSource = socketSource;
                    config.AttachPoint = string.Empty;
                }
                config.AttachPoint = Draw挂点字段(config.SocketSource, config.AttachPoint, "特效挂点");
                config.FollowMode = (TimelineFollowMode)EditorGUILayout.EnumPopup("FollowMode", config.FollowMode);
                config.PositionOffset = EditorGUILayout.Vector3Field("PositionOffset", config.PositionOffset);
                config.RotationOffset = EditorGUILayout.Vector3Field("RotationOffset", config.RotationOffset);
                config.Scale = EditorGUILayout.Vector3Field("Scale", config.Scale);
                config.UseUnscaledTime = EditorGUILayout.Toggle("UseUnscaledTime", config.UseUnscaledTime);
                config.TailTimeout = Mathf.Max(0.01f, EditorGUILayout.FloatField("Safety Timeout", config.TailTimeout));
                if (config.Mode == TimelineVfxMode.Controlled)
                {
                    config.StopMode = (TimelineVfxStopMode)EditorGUILayout.EnumPopup("StopMode", config.StopMode);
                    if (config.StopMode == TimelineVfxStopMode.StopEmitting)
                    {
                        EditorGUILayout.HelpBox("控制期结束后停止继续发射，并保留已生成粒子；Safety Timeout 到期后强制回收。", MessageType.Info);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("OneShot 的 Duration 固定为 0，由粒子自然结束或 Safety Timeout 强制回收。", MessageType.Info);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    NotifyModified();
                }

                if (GUILayout.Button("删除特效", GUILayout.Width(100f)))
                {
                    _track.VfxClips.Remove(config);
                    _item = null;
                    NotifyModified();
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawAudioDetails(TimelineAudioConfig config)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("音效详情", EditorStyles.boldLabel);
                DrawCommonItemFields(
                    config.DisplayName,
                    config.IsEnabled,
                    config.TriggerTime,
                    0f,
                    false,
                    (name, enabled, triggerTime, duration) =>
                    {
                        config.DisplayName = name;
                        config.IsEnabled = enabled;
                        config.TriggerTime = triggerTime;
                        config.Duration = 0f;
                    });

                AudioClip currentClip = string.IsNullOrEmpty(config.AudioClipPath)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<AudioClip>(config.AudioClipPath);
                AudioClip clip = (AudioClip)EditorGUILayout.ObjectField("AudioClip", currentClip, typeof(AudioClip), false);
                string clipPath = clip != null ? AssetDatabase.GetAssetPath(clip) : string.Empty;
                if (clipPath != config.AudioClipPath)
                {
                    config.AudioClipPath = clipPath;
                    if (clip != null && (string.IsNullOrEmpty(config.DisplayName) || config.DisplayName.StartsWith("音效")))
                    {
                        config.DisplayName = clip.name;
                    }
                    NotifyModified();
                }
                if (clip == null)
                {
                    EditorGUILayout.HelpBox("请拖入 AudioClip；未设置资源时该时间块不会播放。", MessageType.Warning);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(clip == null))
                    {
                        if (GUILayout.Button("试听", GUILayout.Width(80f)))
                        {
                            StateTimelineEditorWindow.StopAllAudioPreviews();
                            StateTimelineEditorWindow.PlayAudioPreview(clip, config.Volume, config.Pitch);
                        }
                    }

                    if (GUILayout.Button("停止试听", GUILayout.Width(80f)))
                    {
                        StateTimelineEditorWindow.StopAllAudioPreviews();
                    }
                }

                UnityEngine.Audio.AudioMixerGroup currentGroup = null;
                if (!string.IsNullOrEmpty(config.AudioMixerPath) && !string.IsNullOrEmpty(config.MixerGroupName))
                {
                    UnityEngine.Audio.AudioMixer mixer = AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>(config.AudioMixerPath);
                    UnityEngine.Audio.AudioMixerGroup[] groups = mixer != null ? mixer.FindMatchingGroups(config.MixerGroupName) : null;
                    if (groups != null && groups.Length > 0) currentGroup = groups[0];
                }

                UnityEngine.Audio.AudioMixerGroup group = (UnityEngine.Audio.AudioMixerGroup)EditorGUILayout.ObjectField(
                    "Mixer Group", currentGroup, typeof(UnityEngine.Audio.AudioMixerGroup), false);
                string mixerPath = group != null ? AssetDatabase.GetAssetPath(group.audioMixer) : string.Empty;
                string groupName = group != null ? group.name : string.Empty;
                if (mixerPath != config.AudioMixerPath || groupName != config.MixerGroupName)
                {
                    config.AudioMixerPath = mixerPath;
                    config.MixerGroupName = groupName;
                    NotifyModified();
                }

                EditorGUI.BeginChangeCheck();
                config.Space = (AudioPlaySpace)EditorGUILayout.EnumPopup("Space", config.Space);
                if (config.Space != AudioPlaySpace.TwoD)
                {
                    config.SocketSource = Draw挂点来源字段("挂点来源", config.SocketSource);
                    config.AttachPoint = Draw挂点字段(config.SocketSource, config.AttachPoint, "播放挂点");
                }

                config.Volume = EditorGUILayout.Slider("Volume", config.Volume, 0f, 1f);
                config.Pitch = EditorGUILayout.Slider("Pitch", config.Pitch, 0.01f, 3f);
                if (config.Space == AudioPlaySpace.TwoD)
                {
                    config.SpatialBlend = 0f;
                    EditorGUILayout.HelpBox("TwoD 不解析挂点，也不进行距离衰减。", MessageType.Info);
                }
                else
                {
                    config.SpatialBlend = EditorGUILayout.Slider("Spatial Blend", config.SpatialBlend, 0f, 1f);
                    config.MinDistance = Mathf.Max(0.01f, EditorGUILayout.FloatField("Min Distance", config.MinDistance));
                    config.MaxDistance = Mathf.Max(config.MinDistance, EditorGUILayout.FloatField("Max Distance", config.MaxDistance));
                    if (config.SpatialBlend <= 0f)
                    {
                        EditorGUILayout.HelpBox("当前 Spatial Blend 为 0，听感等同 2D；如需位置和距离衰减，请提高该值。", MessageType.Warning);
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    NotifyModified();
                }

                EditorGUILayout.HelpBox("音效轨道当前为 OneShot：状态结束或被打断后不会截断声音，AudioClip 播放结束后自动回池。", MessageType.Info);
                if (GUILayout.Button("删除音效", GUILayout.Width(100f)))
                {
                    _track.AudioClips.Remove(config);
                    _item = null;
                    NotifyModified();
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawCommonItemFields(string displayName, bool isEnabled, float triggerTime, float duration, bool allowDurationEdit, Action<string, bool, float, float> apply)
            {
                string nextName = EditorGUILayout.TextField("DisplayName", displayName);
                bool nextEnabled = EditorGUILayout.Toggle("IsEnabled", isEnabled);
                float nextTriggerTime = Mathf.Max(0f, EditorGUILayout.FloatField("TriggerTime", triggerTime));
                float nextDuration;
                using (new EditorGUI.DisabledScope(!allowDurationEdit))
                {
                    nextDuration = EditorGUILayout.FloatField("Duration", duration);
                }

                if (!allowDurationEdit)
                {
                    nextDuration = duration;
                    EditorGUILayout.HelpBox("当前条目是点触发块，只能拖动位置，不能拉伸宽度改变持续时间。", MessageType.None);
                }

                if (nextName != displayName ||
                    nextEnabled != isEnabled ||
                    !Mathf.Approximately(nextTriggerTime, triggerTime) ||
                    !Mathf.Approximately(nextDuration, duration))
                {
                    apply(nextName, nextEnabled, nextTriggerTime, nextDuration);
                    NotifyModified();
                }
            }

            private void NotifyModified()
            {
                _onModified?.Invoke();
                SceneView.RepaintAll();
            }

            private static bool EventSupportsDuration(TimelineEventConfig config)
            {
                return config != null && config.Data != null && config.Data.SupportsDuration;
            }

            private static void ApplyEventTypeDefaults(TimelineEventConfig config)
            {
                if (config == null || config.Data == null)
                {
                    return;
                }

                if (config.Data.SupportsDuration)
                {
                    if (Mathf.Approximately(config.Duration, 0f) && config.Data.DefaultDuration > 0f)
                    {
                        config.Duration = Mathf.Max(1f / FrameRate, config.Data.DefaultDuration);
                    }
                }
                else
                {
                    config.Duration = 0f;
                }
            }

            private static string GetDefaultEventItemName(TimelineEventType eventType)
            {
                return eventType.ToString();
            }
        }

        private sealed class StateInterruptInspectorPanel : ISkillEditorInspectorPanel
        {
            private readonly SkillResourceFileEntry _entry;
            private readonly StateInterruptTrackConfig _track;
            private readonly StateInterruptConfig _interrupt;
            private readonly Action _onModified;

            public StateInterruptInspectorPanel(SkillResourceFileEntry entry, StateInterruptTrackConfig track, StateInterruptConfig interrupt, Action onModified)
            {
                _entry = entry;
                _track = track;
                _interrupt = interrupt;
                _onModified = onModified;
            }

            public string Header => "Inspector";

            public void Bind(EditorWindow owner)
            {
            }

            public void OnGUI()
            {
                if (_interrupt == null)
                {
                    EditorGUILayout.HelpBox("当前没有选中的打断条目。", MessageType.Info);
                    return;
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("打断详情", EditorStyles.boldLabel);

                bool isEnabled = EditorGUILayout.Toggle("IsEnabled", _interrupt.IsEnabled);
                if (isEnabled != _interrupt.IsEnabled)
                {
                    _interrupt.IsEnabled = isEnabled;
                    NotifyModified();
                }

                DrawTargetStateField();

                float triggerTime = Mathf.Max(0f, EditorGUILayout.FloatField("TriggerTime", _interrupt.TriggerTime));
                if (!Mathf.Approximately(triggerTime, _interrupt.TriggerTime))
                {
                    _interrupt.TriggerTime = triggerTime;
                    NotifyModified();
                }

                float duration = EditorGUILayout.FloatField("Duration", _interrupt.Duration);
                if (!Mathf.Approximately(duration, _interrupt.Duration))
                {
                    _interrupt.Duration = duration;
                    NotifyModified();
                }

                float executeTime = Mathf.Max(0f, EditorGUILayout.FloatField("ExecuteTime", _interrupt.ExecuteTime));
                if (!Mathf.Approximately(executeTime, _interrupt.ExecuteTime))
                {
                    _interrupt.ExecuteTime = executeTime;
                    NotifyModified();
                }

                int sortOrder = EditorGUILayout.IntField("SortOrder", _interrupt.SortOrder);
                if (sortOrder != _interrupt.SortOrder)
                {
                    _interrupt.SortOrder = sortOrder;
                    NotifyModified();
                }

                bool checkAllConditions = EditorGUILayout.Toggle("CheckAllConditions", _interrupt.CheckAllConditions);
                if (checkAllConditions != _interrupt.CheckAllConditions)
                {
                    _interrupt.CheckAllConditions = checkAllConditions;
                    NotifyModified();
                }

                bool useTransitionOverride = EditorGUILayout.Toggle("UseTransitionOverride", _interrupt.UseTransitionOverride);
                if (useTransitionOverride != _interrupt.UseTransitionOverride)
                {
                    _interrupt.UseTransitionOverride = useTransitionOverride;
                    NotifyModified();
                }

                using (new EditorGUI.DisabledScope(!_interrupt.UseTransitionOverride))
                {
                    float transitionDuration = Mathf.Max(0f, EditorGUILayout.FloatField("TransitionDuration", _interrupt.TransitionDuration));
                    if (!Mathf.Approximately(transitionDuration, _interrupt.TransitionDuration))
                    {
                        _interrupt.TransitionDuration = transitionDuration;
                        NotifyModified();
                    }

                    AnimationTransitionTimeUnit transitionTimeUnit = (AnimationTransitionTimeUnit)EditorGUILayout.EnumPopup("TransitionTimeUnit", _interrupt.TransitionTimeUnit);
                    if (transitionTimeUnit != _interrupt.TransitionTimeUnit)
                    {
                        _interrupt.TransitionTimeUnit = transitionTimeUnit;
                        NotifyModified();
                    }
                }

                float targetStartTime = Mathf.Max(0f, EditorGUILayout.FloatField("TargetStartTime", _interrupt.TargetStartTime));
                if (!Mathf.Approximately(targetStartTime, _interrupt.TargetStartTime))
                {
                    _interrupt.TargetStartTime = targetStartTime;
                    NotifyModified();
                }

                AnimationStartTimeUnit targetStartTimeUnit = (AnimationStartTimeUnit)EditorGUILayout.EnumPopup("TargetStartTimeUnit", _interrupt.TargetStartTimeUnit);
                if (targetStartTimeUnit != _interrupt.TargetStartTimeUnit)
                {
                    _interrupt.TargetStartTimeUnit = targetStartTimeUnit;
                    NotifyModified();
                }

                EditorGUILayout.Space(6f);
                DrawInterruptConditionList();

                if (GUILayout.Button("删除打断", GUILayout.Width(100f)))
                {
                    RemoveInterrupt();
                    NotifyModified();
                    OpenState(_entry);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndVertical();
            }

            private void DrawTargetStateField()
            {
                string unitId = _entry != null ? _entry.UnitId : SkillPreviewUnitSettings.ActiveUnitId;
                List<SkillResourceFileEntry> stateEntries = SkillResourceRepository.LoadStates(unitId);
                string[] optionValues = new string[(stateEntries != null ? stateEntries.Count : 0) + 1];
                string[] optionLabels = new string[optionValues.Length];
                optionValues[0] = string.Empty;
                optionLabels[0] = "未设置";

                int currentIndex = 0;
                for (int i = 0; i < (stateEntries != null ? stateEntries.Count : 0); i++)
                {
                    StateConfig stateConfig = stateEntries[i] != null ? stateEntries[i].Config as StateConfig : null;
                    string stateId = stateConfig != null ? stateConfig.StateId : string.Empty;
                    string stateName = stateConfig != null ? stateConfig.StateName : string.Empty;
                    optionValues[i + 1] = stateId;
                    optionLabels[i + 1] = string.IsNullOrEmpty(stateName) ? stateId : $"{stateName} ({stateId})";
                    if (string.Equals(stateId, _interrupt.TargetStateId, StringComparison.Ordinal))
                    {
                        currentIndex = i + 1;
                    }
                }

                int nextIndex = EditorGUILayout.Popup("TargetState", currentIndex, optionLabels);
                string nextValue = nextIndex >= 0 && nextIndex < optionValues.Length ? optionValues[nextIndex] : string.Empty;
                if (!string.Equals(nextValue, _interrupt.TargetStateId, StringComparison.Ordinal))
                {
                    _interrupt.TargetStateId = nextValue;
                    NotifyModified();
                }
            }

            private void DrawInterruptConditionList()
            {
                _interrupt.Conditions ??= new List<IStateInterruptCondition>();
                EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);

                for (int i = 0; i < _interrupt.Conditions.Count; i++)
                {
                    IStateInterruptCondition condition = _interrupt.Conditions[i];
                    if (condition == null)
                    {
                        continue;
                    }

                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField(condition.GetDisplayName(), EditorStyles.miniBoldLabel);
                    DrawInterruptConditionFields(condition);
                    if (GUILayout.Button("删除条件", GUILayout.Width(100f)))
                    {
                        _interrupt.Conditions.RemoveAt(i);
                        NotifyModified();
                        EditorGUILayout.EndVertical();
                        break;
                    }

                    EditorGUILayout.EndVertical();
                }

                if (GUILayout.Button("新增条件", GUILayout.Width(100f)))
                {
                    ShowAddInterruptConditionMenu();
                }
            }

            private void DrawInterruptConditionFields(IStateInterruptCondition condition)
            {
                if (condition == null)
                {
                    return;
                }

                switch (condition.GetType().Name)
                {
                    case "StateKeyInterruptCondition":
                        DrawReflectedActionField(condition, "ActionName", "输入动作");
                        DrawReflectedEnumField(condition, "TriggerMode", "TriggerMode");
                        break;
                    case "StateMoveInputInterruptCondition":
                        DrawReflectedEnumField(condition, "MoveInputMode", "MoveInputMode");
                        DrawReflectedFloatField(condition, "MinimumDuration", "最小持续时间", true);
                        break;
                    case "CompareCharacterForwardToInputInterruptCondition":
                        DrawReflectedFloatField(condition, "AngleThreshold", "夹角阈值", true);
                        break;
                    case "StateTagInterruptCondition":
                        DrawReflectedTextField(condition, "Tag", "Tag");
                        DrawReflectedBoolField(condition, "Inverse", "Inverse");
                        break;
                    case "StateBreakValueInterruptCondition":
                        DrawReflectedFloatField(condition, "MinimumBreakValue", "MinimumBreakValue", true);
                        break;
                    case "StateGroundingInterruptCondition":
                        DrawReflectedEnumField(condition, "GroundingMode", "GroundingMode");
                        DrawReflectedFloatField(condition, "CoyoteTime", "CoyoteTime", true);
                        break;
                    case "StateMotionValueInterruptCondition":
                        DrawReflectedEnumField(condition, "MotionValue", "MotionValue");
                        DrawReflectedEnumField(condition, "Comparison", "Comparison");
                        DrawReflectedFloatField(condition, "Threshold", "Threshold", false);
                        break;
                    case "StateHitInterruptCondition":
                        EditorGUILayout.HelpBox("命中型条件：当本帧技能/状态效果成功命中目标时为 true。", MessageType.None);
                        break;
                    case "StateBeHitInterruptCondition":
                        EditorGUILayout.HelpBox("受击型条件：当前运行时快照已预留，后续接入受击链路后自动生效。", MessageType.None);
                        break;
                }
            }

            private void ShowAddInterruptConditionMenu()
            {
                GenericMenu menu = new GenericMenu();
                AddInterruptConditionMenuItem(menu, "输入动作条件", "StateKeyInterruptCondition");
                AddInterruptConditionMenuItem(menu, "移动输入条件", "StateMoveInputInterruptCondition");
                AddInterruptConditionMenuItem(menu, "角色前向/输入夹角条件", "CompareCharacterForwardToInputInterruptCondition");
                AddInterruptConditionMenuItem(menu, "命中条件", "StateHitInterruptCondition");
                AddInterruptConditionMenuItem(menu, "受击条件", "StateBeHitInterruptCondition");
                AddInterruptConditionMenuItem(menu, "Tag 条件", "StateTagInterruptCondition");
                AddInterruptConditionMenuItem(menu, "BreakValue 条件", "StateBreakValueInterruptCondition");
                AddInterruptConditionMenuItem(menu, "运动/接地条件", "StateGroundingInterruptCondition");
                AddInterruptConditionMenuItem(menu, "运动/速度条件", "StateMotionValueInterruptCondition");
                menu.ShowAsContext();
            }

            private void AddInterruptCondition(IStateInterruptCondition condition)
            {
                if (condition == null)
                {
                    return;
                }

                _interrupt.Conditions ??= new List<IStateInterruptCondition>();
                _interrupt.Conditions.Add(condition);
                NotifyModified();
            }

            private void AddInterruptConditionMenuItem(GenericMenu menu, string label, string typeName)
            {
                Type runtimeType = FindRuntimeType(typeName);
                if (runtimeType == null)
                {
                    menu.AddDisabledItem(new GUIContent(label));
                    return;
                }

                menu.AddItem(new GUIContent(label), false, () =>
                {
                    if (Activator.CreateInstance(runtimeType) is IStateInterruptCondition condition)
                    {
                        AddInterruptCondition(condition);
                    }
                });
            }

            private void DrawReflectedEnumField(object target, string fieldName, string label)
            {
                FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
                if (field == null || !field.FieldType.IsEnum)
                {
                    return;
                }

                Enum currentValue = field.GetValue(target) as Enum;
                Enum nextValue = EditorGUILayout.EnumPopup(label, currentValue);
                if (!Equals(nextValue, currentValue))
                {
                    field.SetValue(target, nextValue);
                    NotifyModified();
                }
            }

            private void DrawReflectedTextField(object target, string fieldName, string label)
            {
                FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
                if (field == null || field.FieldType != typeof(string))
                {
                    return;
                }

                string currentValue = field.GetValue(target) as string ?? string.Empty;
                string nextValue = EditorGUILayout.TextField(label, currentValue);
                if (!string.Equals(nextValue, currentValue, StringComparison.Ordinal))
                {
                    field.SetValue(target, nextValue);
                    NotifyModified();
                }
            }

            private void DrawReflectedActionField(object target, string fieldName, string label)
            {
                FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
                if (field == null || field.FieldType != typeof(string))
                {
                    return;
                }

                string currentValue = field.GetValue(target) as string ?? string.Empty;
                string nextValue = InputActionEditorUtility.DrawActionPopup(label, currentValue);
                if (!string.Equals(nextValue, currentValue, StringComparison.Ordinal))
                {
                    field.SetValue(target, nextValue);
                    NotifyModified();
                }
            }

            private void DrawReflectedBoolField(object target, string fieldName, string label)
            {
                FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
                if (field == null || field.FieldType != typeof(bool))
                {
                    return;
                }

                bool currentValue = (bool)field.GetValue(target);
                bool nextValue = EditorGUILayout.Toggle(label, currentValue);
                if (nextValue != currentValue)
                {
                    field.SetValue(target, nextValue);
                    NotifyModified();
                }
            }

            private void DrawReflectedFloatField(object target, string fieldName, string label, bool clampNonNegative)
            {
                FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
                if (field == null || field.FieldType != typeof(float))
                {
                    return;
                }

                float currentValue = (float)field.GetValue(target);
                float nextValue = EditorGUILayout.FloatField(label, currentValue);
                if (clampNonNegative)
                {
                    nextValue = Mathf.Max(0f, nextValue);
                }

                if (!Mathf.Approximately(nextValue, currentValue))
                {
                    field.SetValue(target, nextValue);
                    NotifyModified();
                }
            }

            private void RemoveInterrupt()
            {
                if (_track == null || _track.Interrupts == null)
                {
                    return;
                }

                _track.Interrupts.Remove(_interrupt);
            }

            private void NotifyModified()
            {
                _onModified?.Invoke();
                SceneView.RepaintAll();
            }
        }

        private static Type FindRuntimeType(string typeName)
        {
            return Type.GetType($"AsiSkillEditor.RunTime.{typeName}, Assembly-CSharp");
        }

        internal static class TagSelectionEditorUtility
        {
            private const int MaxMaskCount = 30;

            public static bool DrawTagContainer(string label, TagContainer container)
            {
                if (container == null)
                {
                    return false;
                }

                container.Tags ??= new List<string>();
                return DrawTagList(label, container.Tags);
            }

            public static bool DrawTagList(string label, List<string> selectedTags)
            {
                selectedTags ??= new List<string>();
                List<string> options = BuildOptions(selectedTags);
                if (options.Count == 0)
                {
                    EditorGUILayout.LabelField(label, "(no TagDefinitionCatalog found)");
                    return false;
                }

                if (options.Count > MaxMaskCount)
                {
                    EditorGUILayout.HelpBox($"Tag options exceed {MaxMaskCount}, only first {MaxMaskCount} are shown.", MessageType.Warning);
                    options = options.GetRange(0, MaxMaskCount);
                }

                int mask = BuildMask(options, selectedTags);
                int nextMask = EditorGUILayout.MaskField(label, mask, options.ToArray());
                if (nextMask == mask)
                {
                    return false;
                }

                selectedTags.Clear();
                for (int i = 0; i < options.Count; i++)
                {
                    if ((nextMask & (1 << i)) != 0)
                    {
                        selectedTags.Add(options[i]);
                    }
                }

                selectedTags.Sort(StringComparer.Ordinal);
                return true;
            }

            public static bool DrawSingleTagField(string label, ref string tag)
            {
                List<string> selected = new List<string>();
                if (!string.IsNullOrEmpty(tag))
                {
                    selected.Add(tag);
                }

                bool changed = DrawTagList(label, selected);
                if (!changed)
                {
                    return false;
                }

                tag = selected.Count > 0 ? selected[0] : string.Empty;
                return true;
            }

            private static List<string> BuildOptions(List<string> selectedTags)
            {
                HashSet<string> tags = new HashSet<string>(StringComparer.Ordinal);
                string[] guids = AssetDatabase.FindAssets("t:ScriptableObject TagDefinitionCatalog");
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    UnityEngine.Object catalog = AssetDatabase.LoadMainAssetAtPath(path);
                    if (catalog == null)
                    {
                        continue;
                    }

                    SerializedObject serializedCatalog = new SerializedObject(catalog);
                    SerializedProperty tagsProperty = serializedCatalog.FindProperty("Tags");
                    if (tagsProperty == null || !tagsProperty.isArray)
                    {
                        continue;
                    }

                    for (int tagIndex = 0; tagIndex < tagsProperty.arraySize; tagIndex++)
                    {
                        SerializedProperty tagProperty = tagsProperty.GetArrayElementAtIndex(tagIndex);
                        string candidate = tagProperty != null ? tagProperty.stringValue : string.Empty;
                        if (!string.IsNullOrEmpty(candidate))
                        {
                            tags.Add(candidate);
                        }
                    }
                }

                for (int i = 0; i < selectedTags.Count; i++)
                {
                    string selected = selectedTags[i];
                    if (!string.IsNullOrEmpty(selected))
                    {
                        tags.Add(selected);
                    }
                }

                List<string> options = new List<string>(tags);
                options.Sort(StringComparer.Ordinal);
                return options;
            }

            private static int BuildMask(List<string> options, List<string> selectedTags)
            {
                int mask = 0;
                for (int i = 0; i < options.Count; i++)
                {
                    if (selectedTags.Contains(options[i]))
                    {
                        mask |= 1 << i;
                    }
                }

                return mask;
            }
        }

        private static class BuffIconSelectionUtility
        {
            public static string DrawIconField(string label, string assetPath)
            {
                List<string> paths = GetIconPaths();
                List<string> displayNames = new List<string> { "None" };
                int selectedIndex = 0;

                for (int i = 0; i < paths.Count; i++)
                {
                    string path = paths[i];
                    displayNames.Add(System.IO.Path.GetFileNameWithoutExtension(path));
                    if (string.Equals(path, assetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIndex = i + 1;
                    }
                }

                int nextIndex = EditorGUILayout.Popup(label, selectedIndex, displayNames.ToArray());
                return nextIndex <= 0 ? string.Empty : paths[nextIndex - 1];
            }

            private static List<string> GetIconPaths()
            {
                List<string> result = new List<string>();
                string folder = SkillEditorResourcePaths.BuffIconFolder;
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!string.IsNullOrEmpty(path))
                    {
                        result.Add(path);
                    }
                }

                result.Sort(StringComparer.OrdinalIgnoreCase);
                return result;
            }
        }

        private static class NumericIdUtility
        {
            public static string Sanitize(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return string.Empty;
                }

                char[] buffer = new char[value.Length];
                int count = 0;
                for (int i = 0; i < value.Length; i++)
                {
                    char current = value[i];
                    if (char.IsDigit(current))
                    {
                        buffer[count++] = current;
                    }
                }

                return count == 0 ? string.Empty : new string(buffer, 0, count);
            }
        }
    }
}
