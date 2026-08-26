using System;
using System.Collections.Generic;
using System.Reflection;
using AsiSkillEditor.RunTime;
using SkillEditor.Preview;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SkillEditor.Editor
{
    public sealed class StateTimelineEditorWindow : EditorWindow
    {
        private static StateTimelineEditorWindow _activeWindow;
        private const float HeaderWidth = 250f;
        private const float ToolbarHeight = 22f;
        private const float StatusBarHeight = 22f;
        private const float TimeHeaderHeight = 28f;
        private const float FlowPreviewRowHeight = 30f;
        private const float RangeSliderHeight = 18f;
        private const float AnimationRowHeight = 40f;
        private const float TrackGroupHeight = 32f;
        private const float TrackRowHeight = 42f;
        private const float TrackRowSpacing = 4f;
        private const float DetailsHeight = 260f;
        private const float MajorTickTargetSpacing = 100f;

        private static readonly Color BackgroundColor = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color HeaderBackground = new Color(0.20f, 0.20f, 0.20f);
        private static readonly Color GroupHeaderBackground = new Color(0.23f, 0.23f, 0.23f);
        private static readonly Color TrackAreaBackground = new Color(0.23f, 0.23f, 0.23f);
        private static readonly Color TrackStripeBackground = new Color(0.23f, 0.23f, 0.23f);
        private static readonly Color ClipBlockColor = new Color(0.52f, 0.52f, 0.52f);
        private static readonly Color ClipBlockEdgeColor = new Color(0.35f, 0.35f, 0.35f);
        private static readonly Color TransitionOverlayColor = new Color(1.00f, 0.92f, 0.35f, 0.28f);
        private static readonly Color PlayheadColor = new Color(0.18f, 0.85f, 0.95f);
        private static readonly Color TimeTextColor = new Color(0.95f, 0.95f, 0.95f);
        private static readonly Color WarningTextColor = new Color(1f, 0.78f, 0.22f);
        private static readonly Color NormalTextColor = new Color(0.92f, 0.92f, 0.92f);
        private static readonly Color HitBoxColor = new Color(0.18f, 0.85f, 0.95f);
        private static readonly Color BulletColor = new Color(1.00f, 0.62f, 0.20f);
        private static readonly Color VfxColor = new Color(0.72f, 0.40f, 1.00f);
        private static readonly Color AudioColor = new Color(0.35f, 0.88f, 0.45f);
        private static readonly Color EventColor = new Color(1.00f, 0.45f, 0.78f);
        private static readonly Color InterruptColor = new Color(0.92f, 0.38f, 0.24f);
        private static readonly Color SelectionColor = new Color(0.20f, 0.80f, 1.00f, 0.18f);
        private static readonly Color SelectedBlockOutlineColor = new Color(0.10f, 0.68f, 0.82f);
        private static readonly string[] HitBoxDrawModes = { "仅常规攻击盒", "仅烘焙攻击盒", "同时绘制" };

        private static readonly TrackGroupDefinition[] TrackGroups =
        {
            new TrackGroupDefinition(TimelineTrackType.HitBox, "攻击盒"),
            new TrackGroupDefinition(TimelineTrackType.Bullet, "子弹"),
            new TrackGroupDefinition(TimelineTrackType.Vfx, "特效"),
            new TrackGroupDefinition(TimelineTrackType.Audio, "音效"),
            new TrackGroupDefinition(TimelineTrackType.MetaSkillEvent, "事件"),
        };

        private SkillResourceFileEntry _entry;
        private StateConfig _config;
        private Action _onOuterModified;
        private string _windowLabel = "StateTimeline";
        private AnimationClip _clip;
        private bool _isPlaying;
        private bool _isLoop = true;
        private bool _isDirty;
        private double _lastUpdateTime;
        private float _previewTime;
        private float _playSpeed = 1f;
        private float _rangeMin;
        private float _rangeMax = 1f;
        private Vector2 _timelineScrollPosition;
        private Vector2 _detailsScrollPosition;
        private GUIStyle _centeredLabel;
        private GUIStyle _leftLabel;
        private GUIStyle _darkMiniLabel;
        private GUIStyle _statusLabel;
        private GUIStyle _trackHeaderLabel;
        private GUIStyle _blockCenteredLabel;
        private GUIStyle _plusLabel;
        private readonly Dictionary<TimelineTrackType, bool> _groupExpanded = new Dictionary<TimelineTrackType, bool>();
        private bool _interruptTracksExpanded = true;
        private TimelineTrackConfig _selectedTrack;
        private SelectedItemKind _selectedItemKind;
        private object _selectedItem;
        private GameObject _vfxPreviewInstance;
        private string _vfxPreviewPrefabPath;
        private float _lastAudioPreviewTime;
        private BlockDragMode _blockDragMode;
        private TimelineTrackConfig _dragTrack;
        private object _dragItem;
        private SelectedItemKind _dragItemKind;
        private bool _dragAllowResize;
        private float _dragStartMouseX;
        private float _dragStartTriggerTime;
        private float _dragStartDuration;

        private enum SelectedItemKind
        {
            None,
            Track,
            HitBox,
            Bullet,
            Vfx,
            Audio,
            Event,
            Interrupt,
        }

        private enum BlockDragMode
        {
            None,
            Move,
            ResizeLeft,
            ResizeRight,
        }

        private readonly struct TrackGroupDefinition
        {
            public readonly TimelineTrackType TrackType;
            public readonly string Label;

            public TrackGroupDefinition(TimelineTrackType trackType, string label)
            {
                TrackType = trackType;
                Label = label;
            }
        }

        internal static void OpenForEntry(SkillResourceFileEntry entry)
        {
            StateTimelineEditorWindow window = GetWindow<StateTimelineEditorWindow>();
            window.titleContent = new GUIContent("StateTimeline");
            window.minSize = new Vector2(1080f, 680f);
            window.Bind(entry);
            window.Show();
        }

        internal static void OpenForEmbeddedState(SkillResourceFileEntry ownerEntry, StateConfig stateConfig, string windowLabel, Action onModified)
        {
            StateTimelineEditorWindow window = GetWindow<StateTimelineEditorWindow>();
            string title = string.IsNullOrEmpty(windowLabel) ? "StateTimeline" : windowLabel;
            window.titleContent = new GUIContent(title);
            window.minSize = new Vector2(1080f, 680f);
            window.BindEmbedded(ownerEntry, stateConfig, title, onModified);
            window.Show();
        }

        private void OnEnable()
        {
            _activeWindow = this;
            EditorApplication.update += OnEditorUpdate;
            SceneView.duringSceneGui += OnSceneGUI;
            EnsureGroupStates();
            EnsureTimelineData();
        }

        private void OnDisable()
        {
            if (_activeWindow == this)
            {
                _activeWindow = null;
            }
            EditorApplication.update -= OnEditorUpdate;
            SceneView.duringSceneGui -= OnSceneGUI;
            StopPreview();
            CleanupVfxPreview();
            StopAllAudioPreviews();
        }

        private void OnGUI()
        {
            EnsureStyles();
            EnsureGroupStates();
            EnsureTimelineData();
            EnsureSelectionValid();

            EditorGUI.DrawRect(new Rect(0f, 0f, position.width, position.height), BackgroundColor);

            DrawToolbar();
            DrawWorkspace();
        }

        private void Bind(SkillResourceFileEntry entry)
        {
            _entry = entry;
            _config = entry != null ? entry.Config as StateConfig : null;
            _onOuterModified = null;
            _windowLabel = "StateTimeline";
            _clip = LoadClip();
            _previewTime = 0f;
            _lastAudioPreviewTime = 0f;
            _isPlaying = false;
            _isDirty = false;
            _selectedTrack = null;
            _selectedItem = null;
            _selectedItemKind = SelectedItemKind.None;
            EnsureGroupStates();
            EnsureTimelineData();
            SamplePreview();
        }

        private void BindEmbedded(SkillResourceFileEntry ownerEntry, StateConfig stateConfig, string windowLabel, Action onModified)
        {
            _entry = ownerEntry;
            _config = stateConfig;
            _onOuterModified = onModified;
            _windowLabel = string.IsNullOrEmpty(windowLabel) ? "StateTimeline" : windowLabel;
            _clip = LoadClip();
            _previewTime = 0f;
            _lastAudioPreviewTime = 0f;
            _isPlaying = false;
            _isDirty = false;
            _selectedTrack = null;
            _selectedItem = null;
            _selectedItemKind = SelectedItemKind.None;
            EnsureGroupStates();
            EnsureTimelineData();
            SamplePreview();
        }

        private void EnsureStyles()
        {
            if (_centeredLabel == null)
            {
                _centeredLabel = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white },
                    fontSize = 12,
                };
            }

            if (_leftLabel == null)
            {
                _leftLabel = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 12,
                };
                _leftLabel.normal.textColor = Color.white;
                _leftLabel.hover.textColor = Color.white;
                _leftLabel.active.textColor = Color.white;
                _leftLabel.focused.textColor = Color.white;
                _leftLabel.onNormal.textColor = Color.white;
                _leftLabel.onHover.textColor = Color.white;
                _leftLabel.onActive.textColor = Color.white;
                _leftLabel.onFocused.textColor = Color.white;
            }

            if (_darkMiniLabel == null)
            {
                _darkMiniLabel = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.08f, 0.08f, 0.08f) },
                    clipping = TextClipping.Clip,
                };
            }

            if (_statusLabel == null)
            {
                _statusLabel = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = NormalTextColor },
                    clipping = TextClipping.Clip,
                };
            }

            if (_trackHeaderLabel == null)
            {
                _trackHeaderLabel = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip,
                    fontSize = 12,
                };
                _trackHeaderLabel.normal.textColor = Color.white;
                _trackHeaderLabel.hover.textColor = Color.white;
                _trackHeaderLabel.active.textColor = Color.white;
                _trackHeaderLabel.focused.textColor = Color.white;
                _trackHeaderLabel.onNormal.textColor = Color.white;
                _trackHeaderLabel.onHover.textColor = Color.white;
                _trackHeaderLabel.onActive.textColor = Color.white;
                _trackHeaderLabel.onFocused.textColor = Color.white;
            }

            if (_blockCenteredLabel == null)
            {
                _blockCenteredLabel = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.black },
                    clipping = TextClipping.Clip,
                    fontStyle = FontStyle.Bold,
                };
            }

            if (_plusLabel == null)
            {
                _plusLabel = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                };
                _plusLabel.normal.textColor = Color.white;
                _plusLabel.hover.textColor = Color.white;
                _plusLabel.active.textColor = Color.white;
                _plusLabel.focused.textColor = Color.white;
                _plusLabel.onNormal.textColor = Color.white;
                _plusLabel.onHover.textColor = Color.white;
                _plusLabel.onActive.textColor = Color.white;
                _plusLabel.onFocused.textColor = Color.white;
            }
        }

        private void EnsureGroupStates()
        {
            for (int i = 0; i < TrackGroups.Length; i++)
            {
                if (!_groupExpanded.ContainsKey(TrackGroups[i].TrackType))
                {
                    _groupExpanded[TrackGroups[i].TrackType] = true;
                }
            }
        }

        private void EnsureTimelineData()
        {
            if (_config == null)
            {
                return;
            }

            if (_config.Timeline == null)
            {
                _config.Timeline = new StateTimelineConfig();
                MarkDirty();
            }

            if (_config.Timeline.Animation == null)
            {
                _config.Timeline.Animation = new TimelineAnimationConfig();
                MarkDirty();
            }

            if (_config.Timeline.Tracks == null)
            {
                _config.Timeline.Tracks = new List<TimelineTrackConfig>();
                MarkDirty();
            }

            if (_config.Timeline.InterruptTracks == null)
            {
                _config.Timeline.InterruptTracks = new List<StateInterruptTrackConfig>();
                MarkDirty();
            }

            if (_config.Timeline.Interrupts == null)
            {
                _config.Timeline.Interrupts = new List<StateInterruptConfig>();
                MarkDirty();
            }

            MigrateLegacyInterruptsToTrack();
        }

        private void EnsureSelectionValid()
        {
            if (_config == null || _config.Timeline == null || _config.Timeline.Tracks == null)
            {
                _selectedTrack = null;
                _selectedItem = null;
                _selectedItemKind = SelectedItemKind.None;
                return;
            }

            if (_selectedItemKind == SelectedItemKind.Interrupt)
            {
                if (_selectedItem == null || !ContainsInterrupt(_selectedItem as StateInterruptConfig))
                {
                    _selectedItem = null;
                    _selectedItemKind = SelectedItemKind.None;
                }

                _selectedTrack = null;
                return;
            }

            if (_selectedTrack != null && !_config.Timeline.Tracks.Contains(_selectedTrack))
            {
                _selectedTrack = null;
                _selectedItem = null;
                _selectedItemKind = SelectedItemKind.None;
            }

            if (_selectedTrack == null)
            {
                _selectedItem = null;
                if (_selectedItemKind != SelectedItemKind.None)
                {
                    _selectedItemKind = SelectedItemKind.None;
                }

                return;
            }

            if (_selectedItem == null)
            {
                if (_selectedItemKind != SelectedItemKind.Track)
                {
                    _selectedItemKind = SelectedItemKind.Track;
                }

                return;
            }

            bool itemStillExists = _selectedItemKind switch
            {
                SelectedItemKind.HitBox => _selectedTrack.HitBoxes != null && _selectedTrack.HitBoxes.Contains((HitBoxConfig)_selectedItem),
                SelectedItemKind.Bullet => _selectedTrack.Bullets != null && _selectedTrack.Bullets.Contains((BulletConfig)_selectedItem),
                SelectedItemKind.Vfx => _selectedTrack.VfxClips != null && _selectedTrack.VfxClips.Contains((TimelineVfxConfig)_selectedItem),
                SelectedItemKind.Audio => _selectedTrack.AudioClips != null && _selectedTrack.AudioClips.Contains((TimelineAudioConfig)_selectedItem),
                SelectedItemKind.Event => _selectedTrack.MetaSkillEvents != null && _selectedTrack.MetaSkillEvents.Contains((TimelineEventConfig)_selectedItem),
                _ => false,
            };

            if (!itemStillExists)
            {
                _selectedItem = null;
                _selectedItemKind = SelectedItemKind.Track;
            }
        }

        private void DrawToolbar()
        {
            Rect toolbarRect = new Rect(0f, 0f, position.width, ToolbarHeight + 6f);
            EditorGUI.DrawRect(toolbarRect, new Color(0.19f, 0.19f, 0.19f));

            GUILayout.BeginArea(new Rect(0f, 0f, position.width, ToolbarHeight));
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EditorGUI.DisabledScope(!CanPreview()))
                {
                    if (DrawToolbarButton("Animation.FirstKey", "|<", "上一帧", 28f))
                    {
                        StepFrame(-1);
                    }

                    if (DrawToolbarButton(_isPlaying ? "PauseButton" : "PlayButton", _isPlaying ? "||" : ">", _isPlaying ? "暂停" : "播放", 28f))
                    {
                        TogglePlay();
                    }

                    if (DrawToolbarButton("Animation.LastKey", ">|", "下一帧", 28f))
                    {
                        StepFrame(1);
                    }

                    if (DrawToolbarButton("PreMatQuad", "[]", "停止", 28f))
                    {
                        StopPreview();
                    }
                }

                GUILayout.Space(10f);
                using (new EditorGUI.DisabledScope(!CanPreview()))
                {
                    _playSpeed = GUILayout.HorizontalSlider(_playSpeed, 0.1f, 2.0f, GUILayout.Width(160f));
                }

                _playSpeed = Mathf.Round(_playSpeed * 100f) / 100f;
                GUILayout.Label(_playSpeed.ToString("0.00"), GUILayout.Width(42f));
                GUILayout.Space(8f);

                Color previousColor = GUI.color;
                GUI.color = _isLoop ? Color.gray : Color.white;
                if (GUILayout.Button("Loop", EditorStyles.toolbarButton, GUILayout.Width(44f)))
                {
                    _isLoop = !_isLoop;
                }

                GUI.color = previousColor;

                GUILayout.Space(12f);
                using (new EditorGUI.DisabledScope(!NeedsDurationSync()))
                {
                    if (GUILayout.Button("同步时长", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    {
                        SyncTimelineDuration();
                    }
                }

                GUILayout.Space(12f);
                GUILayout.Label(BuildTimeLabel(), GUILayout.Width(90f));
                GUILayout.FlexibleSpace();
                GUILayout.Label(_clip != null ? _clip.name : "No AnimationClip", GUILayout.Width(260f));
            }

            GUILayout.EndArea();

            Rect statusRect = new Rect(0f, ToolbarHeight, position.width, StatusBarHeight);
            DrawStatusBar(statusRect);
        }

        private void DrawWorkspace()
        {
            float topOffset = ToolbarHeight + StatusBarHeight + 8f;
            Rect timelineRect = new Rect(0f, topOffset, position.width, Mathf.Max(0f, position.height - topOffset));
            DrawTimelineSurface(timelineRect);
        }

        private void DrawTimelineSurface(Rect outerRect)
        {
            if (outerRect.height <= RangeSliderHeight + 24f)
            {
                return;
            }

            Rect scrollRect = new Rect(outerRect.x, outerRect.y, outerRect.width, outerRect.height - RangeSliderHeight);
            Rect rangeRect = new Rect(outerRect.x + HeaderWidth, outerRect.yMax - RangeSliderHeight, outerRect.width - HeaderWidth, RangeSliderHeight);
            float contentHeight = GetTimelineContentHeight();
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(scrollRect.width - 16f, 0f), contentHeight);

            using (var scroll = new GUI.ScrollViewScope(scrollRect, _timelineScrollPosition, viewRect, false, true))
            {
                _timelineScrollPosition = scroll.scrollPosition;

                Rect topLeftRect = new Rect(0f, 0f, HeaderWidth, TimeHeaderHeight);
                Rect timeRect = new Rect(HeaderWidth, 0f, viewRect.width - HeaderWidth, TimeHeaderHeight);
                DrawTimeHeaderLeft(topLeftRect);
                DrawTimeHeader(timeRect);

                float y = TimeHeaderHeight;
                Rect flowHeaderRect = new Rect(0f, y, HeaderWidth, FlowPreviewRowHeight);
                Rect flowTimelineRect = new Rect(HeaderWidth, y, viewRect.width - HeaderWidth, FlowPreviewRowHeight);
                DrawFlowPreviewRow(flowHeaderRect, flowTimelineRect);
                HandleFlowPreviewInput(flowTimelineRect);
                y += FlowPreviewRowHeight + TrackRowSpacing;

                Rect animationHeaderRect = new Rect(0f, y, HeaderWidth, AnimationRowHeight);
                Rect animationTimelineRect = new Rect(HeaderWidth, y, viewRect.width - HeaderWidth, AnimationRowHeight);
                DrawAnimationRow(animationHeaderRect, animationTimelineRect);
                y += AnimationRowHeight + TrackRowSpacing;

                Rect interruptGroupHeaderRect = new Rect(0f, y, HeaderWidth, TrackGroupHeight);
                Rect interruptGroupTimelineRect = new Rect(HeaderWidth, y, viewRect.width - HeaderWidth, TrackGroupHeight);
                DrawInterruptGroupRow(interruptGroupHeaderRect, interruptGroupTimelineRect);
                y += TrackGroupHeight + TrackRowSpacing;

                if (_interruptTracksExpanded)
                {
                    List<StateInterruptTrackConfig> interruptTracks = GetInterruptTracks();
                    for (int trackIndex = 0; trackIndex < interruptTracks.Count; trackIndex++)
                    {
                        StateInterruptTrackConfig track = interruptTracks[trackIndex];
                        Rect interruptTrackHeaderRect = new Rect(0f, y, HeaderWidth, TrackRowHeight);
                        Rect interruptTrackTimelineRect = new Rect(HeaderWidth, y, viewRect.width - HeaderWidth, TrackRowHeight);
                        DrawInterruptTrackRow(interruptTrackHeaderRect, interruptTrackTimelineRect, track, trackIndex);
                        y += TrackRowHeight + TrackRowSpacing;
                    }
                }

                for (int i = 0; i < TrackGroups.Length; i++)
                {
                    TrackGroupDefinition definition = TrackGroups[i];
                    List<TimelineTrackConfig> tracks = GetTracksForType(definition.TrackType);

                    Rect groupHeaderRect = new Rect(0f, y, HeaderWidth, TrackGroupHeight);
                    Rect groupTimelineRect = new Rect(HeaderWidth, y, viewRect.width - HeaderWidth, TrackGroupHeight);
                    DrawGroupRow(groupHeaderRect, groupTimelineRect, definition, tracks.Count);
                    y += TrackGroupHeight + TrackRowSpacing;

                    if (!_groupExpanded[definition.TrackType])
                    {
                        continue;
                    }

                    for (int trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
                    {
                        TimelineTrackConfig track = tracks[trackIndex];
                        Rect trackHeaderRect = new Rect(0f, y, HeaderWidth, TrackRowHeight);
                        Rect trackTimelineRect = new Rect(HeaderWidth, y, viewRect.width - HeaderWidth, TrackRowHeight);
                        DrawTrackRow(trackHeaderRect, trackTimelineRect, track, trackIndex);
                        y += TrackRowHeight + TrackRowSpacing;
                    }
                }

                DrawPlayhead(new Rect(HeaderWidth, 0f, viewRect.width - HeaderWidth, contentHeight));
                HandleTimelineInput(new Rect(HeaderWidth, 0f, viewRect.width - HeaderWidth, contentHeight));
            }

            DrawRangeSlider(rangeRect);
        }

        private void DrawTimeHeaderLeft(Rect rect)
        {
            EditorGUI.DrawRect(rect, HeaderBackground);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 1f, 90f, rect.height - 2f), BuildTimeLabel(), _leftLabel);
            if (_config != null)
            {
                string title = string.IsNullOrEmpty(_config.StateName) ? _config.StateId : _config.StateName;
                GUI.Label(new Rect(rect.x + 104f, rect.y + 1f, rect.width - 114f, rect.height - 2f), title, _leftLabel);
            }
        }

        private void DrawTimeHeader(Rect rect)
        {
            EditorGUI.DrawRect(rect, HeaderBackground);

            float duration = GetExecutionDisplayDuration();
            if (duration <= 0f)
            {
                GUI.Label(rect, "无动画可预览", _centeredLabel);
                return;
            }

            float visibleStart = GetVisibleStartTime(duration);
            float visibleDuration = GetVisibleDuration(duration);
            int totalFrames = Mathf.Max(1, Mathf.RoundToInt(duration * GetFrameRate()));
            int visibleStartFrame = Mathf.Max(0, Mathf.FloorToInt(visibleStart * GetFrameRate()));
            int visibleEndFrame = Mathf.Min(totalFrames, Mathf.CeilToInt((visibleStart + visibleDuration) * GetFrameRate()));
            int majorStep = GetMajorFrameStep(rect.width, visibleDuration);

            for (int frame = visibleStartFrame; frame <= visibleEndFrame; frame++)
            {
                if (frame % majorStep != 0)
                {
                    continue;
                }

                float time = frame / (float)GetFrameRate();
                float x = TimeToPixel(time, rect, duration);
                EditorGUI.DrawRect(new Rect(x, rect.y, 1f, rect.height), Color.black);
                GUI.contentColor = TimeTextColor;
                GUI.Label(new Rect(x + 3f, rect.y + 2f, 48f, 14f), frame.ToString(), EditorStyles.miniLabel);
                GUI.contentColor = Color.white;
            }
        }

        private void DrawFlowPreviewRow(Rect headerRect, Rect timelineRect)
        {
            EditorGUI.DrawRect(headerRect, new Color(0.19f, 0.19f, 0.19f));
            GUI.Label(new Rect(headerRect.x + 10f, headerRect.y + 5f, 150f, 20f), BuildFlowHeaderLabel(), _trackHeaderLabel);

            EditorGUI.DrawRect(timelineRect, TrackAreaBackground);
            float duration = GetPreviewFlowDuration();
            if (duration <= 0f)
            {
                GUI.Label(timelineRect, "无完整流程可预览", _centeredLabel);
                return;
            }

            float executeDuration = GetExecutionDisplayDuration();
            if (_clip != null && executeDuration > 0f)
            {
                float executeStart = TimeToPixel(0f, timelineRect, duration);
                float executeEnd = TimeToPixel(executeDuration, timelineRect, duration);
                Rect executeRect = new Rect(executeStart, timelineRect.y, Mathf.Max(executeEnd - executeStart, 6f), timelineRect.height);
                EditorGUI.DrawRect(executeRect, ClipBlockColor);
                EditorGUI.DrawRect(new Rect(executeRect.x, executeRect.y, executeRect.width, 1f), ClipBlockEdgeColor);
                EditorGUI.DrawRect(new Rect(executeRect.x, executeRect.yMax - 1f, executeRect.width, 1f), ClipBlockEdgeColor);
                GUI.Label(new Rect(executeRect.x + 8f, executeRect.y + 2f, Mathf.Max(executeRect.width - 12f, 0f), executeRect.height - 4f), $"State: {_clip.name}", _darkMiniLabel);
            }

            DrawFlowPreviewPlayhead(timelineRect, duration);
        }

        private void DrawAnimationRow(Rect headerRect, Rect timelineRect)
        {
            EditorGUI.DrawRect(headerRect, new Color(0.19f, 0.19f, 0.19f));
            GUI.Label(new Rect(headerRect.x + 10f, headerRect.y + 10f, 130f, 20f), "AnimationClip", _trackHeaderLabel);

            EditorGUI.DrawRect(timelineRect, TrackAreaBackground);

            if (_clip != null)
            {
                float duration = GetExecutionDisplayDuration();
                float clipStart = TimeToPixel(0f, timelineRect, duration);
                float clipEnd = TimeToPixel(GetExecutionClipVisualDuration(), timelineRect, duration);
                float clipWidth = Mathf.Max(clipEnd - clipStart, 6f);
                Rect clipRect = new Rect(clipStart, timelineRect.y, clipWidth, timelineRect.height);
                EditorGUI.DrawRect(clipRect, ClipBlockColor);
                EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.y, clipRect.width, 1f), ClipBlockEdgeColor);
                EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.yMax - 1f, clipRect.width, 1f), ClipBlockEdgeColor);
                GUI.Label(new Rect(clipRect.x + 8f, clipRect.y + 2f, Mathf.Max(clipRect.width - 12f, 0f), clipRect.height - 4f), _clip.name, _darkMiniLabel);
            }
        }

        private void DrawInterruptGroupRow(Rect headerRect, Rect timelineRect)
        {
            EditorGUI.DrawRect(headerRect, GroupHeaderBackground);
            EditorGUI.DrawRect(timelineRect, TrackAreaBackground);

            Rect foldoutRect = new Rect(headerRect.x + 4f, headerRect.y + 4f, 22f, 22f);
            GUI.Label(foldoutRect, _interruptTracksExpanded ? "▼" : "▶", _leftLabel);
            GUI.Label(new Rect(headerRect.x + 30f, headerRect.y + 6f, 140f, 20f), $"打断轨 ({GetInterruptTracks().Count})", _leftLabel);

            Rect addRect = new Rect(headerRect.xMax - 40f, headerRect.y + 0f, 34f, 32f);
            GUI.Label(addRect, "+", _plusLabel);

            Event current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 0)
            {
                return;
            }

            if (foldoutRect.Contains(current.mousePosition))
            {
                _interruptTracksExpanded = !_interruptTracksExpanded;
                current.Use();
                Repaint();
                return;
            }

            if (addRect.Contains(current.mousePosition))
            {
                AddInterruptTrack();
                current.Use();
            }
        }

        private void DrawInterruptTrackRow(Rect headerRect, Rect timelineRect, StateInterruptTrackConfig track, int indexWithinGroup)
        {
            EditorGUI.DrawRect(headerRect, new Color(0.19f, 0.19f, 0.19f));

            string displayName = string.IsNullOrEmpty(track.DisplayName) ? $"打断轨道{indexWithinGroup + 1}" : track.DisplayName;
            GUI.Label(new Rect(headerRect.x + 14f, headerRect.y + 11f, 120f, 20f), displayName, _trackHeaderLabel);

            Rect toggleRect = new Rect(headerRect.x + 140f, headerRect.y + 12f, 42f, 18f);
            bool enabled = GUI.Toggle(toggleRect, track != null && track.IsEnabled, GUIContent.none);
            if (track != null && enabled != track.IsEnabled)
            {
                track.IsEnabled = enabled;
                MarkDirty();
            }

            EditorGUI.DrawRect(timelineRect, TrackAreaBackground);
            DrawItems(
                timelineRect,
                null,
                track != null ? track.Interrupts : null,
                InterruptColor,
                SelectedItemKind.Interrupt,
                GetDisplayName,
                GetTriggerTime,
                GetEditorVisualDuration,
                SetTriggerTime,
                SetDuration,
                item => item.IsEnabled,
                item => item.Duration >= 0f);

            Rect rowRect = Rect.MinMaxRect(headerRect.x, headerRect.y, timelineRect.xMax, Mathf.Max(headerRect.yMax, timelineRect.yMax));
            HandleInterruptTrackContextMenu(rowRect, track);
        }

        private void DrawGroupRow(Rect headerRect, Rect timelineRect, TrackGroupDefinition definition, int trackCount)
        {
            EditorGUI.DrawRect(headerRect, GroupHeaderBackground);
            EditorGUI.DrawRect(timelineRect, TrackAreaBackground);

            Rect foldoutRect = new Rect(headerRect.x + 4f, headerRect.y + 4f, 22f, 22f);
            GUI.Label(foldoutRect, _groupExpanded[definition.TrackType] ? "▼" : "▶", _leftLabel);

            Color previousContentColor = GUI.contentColor;
            GUI.contentColor = Color.white;
            GUI.Label(new Rect(headerRect.x + 30f, headerRect.y + 6f, 120f, 20f), $"{definition.Label} ({trackCount})", _leftLabel);

            Rect addRect = new Rect(headerRect.xMax - 40f, headerRect.y + 0f, 34f, 32f);
            GUI.Label(addRect, "+", _plusLabel);
            GUI.contentColor = previousContentColor;

            Event current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 0)
            {
                return;
            }

            if (foldoutRect.Contains(current.mousePosition))
            {
                _groupExpanded[definition.TrackType] = !_groupExpanded[definition.TrackType];
                current.Use();
                Repaint();
                return;
            }

            if (addRect.Contains(current.mousePosition))
            {
                AddTrack(definition.TrackType);
                current.Use();
            }
        }

        private void DrawTrackRow(Rect headerRect, Rect timelineRect, TimelineTrackConfig track, int indexWithinGroup)
        {
            EditorGUI.DrawRect(headerRect, new Color(0.19f, 0.19f, 0.19f));

            string displayName = string.IsNullOrEmpty(track.DisplayName) ? $"轨道{indexWithinGroup + 1}" : track.DisplayName;
            Color previousContentColor = GUI.contentColor;
            GUI.contentColor = Color.white;
            GUI.Label(new Rect(headerRect.x + 14f, headerRect.y + 11f, 98f, 20f), displayName, _trackHeaderLabel);
            GUI.contentColor = previousContentColor;

            Rect toggleRect = new Rect(headerRect.x + 120f, headerRect.y + 12f, 42f, 18f);
            bool enabled = GUI.Toggle(toggleRect, track.IsEnabled, GUIContent.none);
            if (enabled != track.IsEnabled)
            {
                track.IsEnabled = enabled;
                MarkDirty();
            }

            EditorGUI.DrawRect(timelineRect, TrackAreaBackground);
            DrawTrackItems(timelineRect, track);

            Rect rowRect = Rect.MinMaxRect(headerRect.x, headerRect.y, timelineRect.xMax, Mathf.Max(headerRect.yMax, timelineRect.yMax));
            HandleTrackContextMenu(rowRect, track);
        }

        private void DrawTrackItems(Rect timelineRect, TimelineTrackConfig track)
        {
            switch (track.TrackType)
            {
                case TimelineTrackType.HitBox:
                    DrawItems(
                        timelineRect,
                        track,
                        track.HitBoxes,
                        HitBoxColor,
                        SelectedItemKind.HitBox,
                        GetDisplayName,
                        GetTriggerTime,
                        GetDuration,
                        SetTriggerTime,
                        SetDuration,
                        item => item.IsEnabled,
                        item => true);
                    break;
                case TimelineTrackType.Bullet:
                    DrawItems(
                        timelineRect,
                        track,
                        track.Bullets,
                        BulletColor,
                        SelectedItemKind.Bullet,
                        GetDisplayName,
                        GetTriggerTime,
                        GetDuration,
                        SetTriggerTime,
                        SetDuration,
                        item => item.IsEnabled,
                        item => true);
                    break;
                case TimelineTrackType.Vfx:
                    DrawItems(
                        timelineRect,
                        track,
                        track.VfxClips,
                        VfxColor,
                        SelectedItemKind.Vfx,
                        GetDisplayName,
                        GetTriggerTime,
                        GetDuration,
                        SetTriggerTime,
                        SetDuration,
                        item => item.IsEnabled,
                        item => item.Mode == TimelineVfxMode.Controlled);
                    break;
                case TimelineTrackType.Audio:
                    DrawItems(
                        timelineRect,
                        track,
                        track.AudioClips,
                        AudioColor,
                        SelectedItemKind.Audio,
                        GetDisplayName,
                        GetTriggerTime,
                        GetDuration,
                        SetTriggerTime,
                        SetDuration,
                        item => item.IsEnabled,
                        item => false);
                    break;
                case TimelineTrackType.MetaSkillEvent:
                    DrawItems(
                        timelineRect,
                        track,
                        track.MetaSkillEvents,
                        EventColor,
                        SelectedItemKind.Event,
                        GetDisplayName,
                        GetTriggerTime,
                        GetDuration,
                        SetTriggerTime,
                        SetDuration,
                        item => item.IsEnabled,
                        EventSupportsDuration);
                    break;
            }
        }

        private void DrawItems<T>(
            Rect timelineRect,
            TimelineTrackConfig track,
            List<T> items,
            Color color,
            SelectedItemKind itemKind,
            Func<T, string> displayNameGetter,
            Func<T, float> triggerTimeGetter,
            Func<T, float> durationGetter,
            Action<T, float> triggerTimeSetter,
            Action<T, float> durationSetter,
            Func<T, bool> enabledGetter,
            Func<T, bool> allowResizeGetter)
            where T : class
        {
            if (items == null)
            {
                return;
            }

            float duration = GetExecutionDisplayDuration();
            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];
                if (item == null)
                {
                    continue;
                }

                float startTime = Mathf.Max(0f, triggerTimeGetter(item));
                float itemDuration = Mathf.Max(0f, durationGetter(item));
                bool allowResize = allowResizeGetter != null && allowResizeGetter(item);
                float blockStart = TimeToPixel(startTime, timelineRect, duration);
                float visualDuration = GetVisualDuration(allowResize, itemDuration);
                float blockEnd = TimeToPixel(startTime + visualDuration, timelineRect, duration);
                float blockWidth = Mathf.Max(blockEnd - blockStart, 12f);
                Rect blockRect = new Rect(blockStart, timelineRect.y, blockWidth, timelineRect.height);
                Rect leftHandleRect = new Rect(blockRect.x, blockRect.y, 6f, blockRect.height);
                Rect rightHandleRect = new Rect(blockRect.xMax - 6f, blockRect.y, 6f, blockRect.height);

                bool isSelected = _selectedTrack == track && _selectedItem == item && _selectedItemKind == itemKind;
                Color blockColor = enabledGetter(item) ? color : new Color(color.r, color.g, color.b, 0.4f);
                if (isSelected)
                {
                    blockColor = new Color(0.92f, 0.92f, 0.92f);
                }

                EditorGUI.DrawRect(blockRect, blockColor);
                EditorGUI.DrawRect(new Rect(blockRect.x, blockRect.y, blockRect.width, 1f), ClipBlockEdgeColor);
                EditorGUI.DrawRect(new Rect(blockRect.x, blockRect.yMax - 1f, blockRect.width, 1f), ClipBlockEdgeColor);

                if (blockRect.width > 28f)
                {
                    Color previousContentColor = GUI.contentColor;
                    GUI.contentColor = Color.black;
                    GUI.Label(
                        new Rect(blockRect.x + 6f, blockRect.y, Mathf.Max(blockRect.width - 12f, 0f), blockRect.height),
                        displayNameGetter(item),
                        _blockCenteredLabel);
                    GUI.contentColor = previousContentColor;
                }

                if (allowResize)
                {
                    EditorGUIUtility.AddCursorRect(leftHandleRect, MouseCursor.ResizeHorizontal);
                    EditorGUIUtility.AddCursorRect(rightHandleRect, MouseCursor.ResizeHorizontal);
                }

                if (isSelected)
                {
                    EditorGUI.DrawRect(new Rect(blockRect.x, blockRect.y, blockRect.width, 1f), SelectedBlockOutlineColor);
                    EditorGUI.DrawRect(new Rect(blockRect.x, blockRect.yMax - 1f, blockRect.width, 1f), SelectedBlockOutlineColor);
                    EditorGUI.DrawRect(new Rect(blockRect.x, blockRect.y, 1f, blockRect.height), SelectedBlockOutlineColor);
                    EditorGUI.DrawRect(new Rect(blockRect.xMax - 1f, blockRect.y, 1f, blockRect.height), SelectedBlockOutlineColor);
                }

                EditorGUIUtility.AddCursorRect(blockRect, MouseCursor.SlideArrow);
                HandleItemInteraction(
                    timelineRect,
                    track,
                    item,
                    itemKind,
                    blockRect,
                    leftHandleRect,
                    rightHandleRect,
                    triggerTimeGetter,
                    durationGetter,
                    triggerTimeSetter,
                    durationSetter,
                    allowResize);
            }
        }

        private void HandleItemInteraction<T>(
            Rect timelineRect,
            TimelineTrackConfig track,
            T item,
            SelectedItemKind itemKind,
            Rect blockRect,
            Rect leftHandleRect,
            Rect rightHandleRect,
            Func<T, float> triggerTimeGetter,
            Func<T, float> durationGetter,
            Action<T, float> triggerTimeSetter,
            Action<T, float> durationSetter,
            bool allowResize)
            where T : class
        {
            Event current = Event.current;
            if (current.type == EventType.MouseDown &&
                current.button == 0 &&
                blockRect.Contains(current.mousePosition))
            {
                BlockDragMode dragMode = BlockDragMode.Move;
                if (allowResize)
                {
                    if (leftHandleRect.Contains(current.mousePosition))
                    {
                        dragMode = BlockDragMode.ResizeLeft;
                    }
                    else if (rightHandleRect.Contains(current.mousePosition))
                    {
                        dragMode = BlockDragMode.ResizeRight;
                    }
                }

                BeginBlockDrag(track, itemKind, item, dragMode, allowResize, current.mousePosition.x, triggerTimeGetter(item), durationGetter(item));
                SelectItem(track, itemKind, item);
                _isPlaying = false;
                current.Use();
                return;
            }

            if (_dragItem != item || _dragTrack != track || _dragItemKind != itemKind || _blockDragMode == BlockDragMode.None)
            {
                return;
            }

            if (current.type == EventType.MouseDrag && current.button == 0)
            {
                ApplyBlockDrag(current.mousePosition.x, timelineRect, item, itemKind, triggerTimeSetter, durationSetter);
                current.Use();
            }
            else if (current.type == EventType.MouseUp && current.button == 0)
            {
                EndBlockDrag();
                current.Use();
            }
        }

        private void BeginBlockDrag(
            TimelineTrackConfig track,
            SelectedItemKind itemKind,
            object item,
            BlockDragMode dragMode,
            bool allowResize,
            float mouseX,
            float triggerTime,
            float duration)
        {
            _dragTrack = track;
            _dragItem = item;
            _dragItemKind = itemKind;
            _blockDragMode = dragMode;
            _dragAllowResize = allowResize;
            _dragStartMouseX = mouseX;
            _dragStartTriggerTime = triggerTime;
            _dragStartDuration = duration;
        }

        private void ApplyBlockDrag<T>(
            float mouseX,
            Rect timelineRect,
            T item,
            SelectedItemKind itemKind,
            Action<T, float> triggerTimeSetter,
            Action<T, float> durationSetter)
            where T : class
        {
            float totalDuration = GetExecutionDisplayDuration();
            if (timelineRect.width <= 0f || totalDuration <= 0f)
            {
                return;
            }

            float deltaTime = PixelDeltaToTime(mouseX - _dragStartMouseX, timelineRect, totalDuration);
            float triggerTime = _dragStartTriggerTime;
            float duration = Mathf.Max(0f, _dragStartDuration);
            float minDuration = GetMinimumDuration(_dragAllowResize);
            bool canResize = _dragAllowResize;

            switch (_blockDragMode)
            {
                case BlockDragMode.Move:
                    float visualDuration = GetVisualDuration(canResize, canResize ? duration : _dragStartDuration);
                    float maxTriggerTime = Mathf.Max(0f, totalDuration - visualDuration);
                    triggerTime = Mathf.Clamp(_dragStartTriggerTime + deltaTime, 0f, maxTriggerTime);
                    break;
                case BlockDragMode.ResizeLeft:
                    if (canResize)
                    {
                        float maxLeft = _dragStartTriggerTime + Mathf.Max(_dragStartDuration - minDuration, 0f);
                        triggerTime = Mathf.Clamp(_dragStartTriggerTime + deltaTime, 0f, maxLeft);
                        duration = Mathf.Max(minDuration, _dragStartDuration - (triggerTime - _dragStartTriggerTime));
                    }
                    break;
                case BlockDragMode.ResizeRight:
                    if (canResize)
                    {
                        duration = Mathf.Clamp(_dragStartDuration + deltaTime, minDuration, Mathf.Max(minDuration, totalDuration - _dragStartTriggerTime));
                    }
                    break;
            }

            triggerTimeSetter(item, Mathf.Max(0f, triggerTime));
            if (canResize)
            {
                durationSetter(item, Mathf.Max(0f, duration));
            }

            MarkDirty();
            Repaint();
        }

        private void EndBlockDrag()
        {
            _blockDragMode = BlockDragMode.None;
            _dragTrack = null;
            _dragItem = null;
            _dragItemKind = SelectedItemKind.None;
            _dragAllowResize = false;
            _dragStartMouseX = 0f;
            _dragStartTriggerTime = 0f;
            _dragStartDuration = 0f;
        }

        private static float GetVisualDuration(bool allowResize, float actualDuration)
        {
            return allowResize ? Mathf.Max(actualDuration, 0.05f) : 0.08f;
        }

        private static float GetMinimumDuration(bool allowResize)
        {
            return allowResize ? 1f / GetFrameRate() : 0f;
        }

        private float PixelDeltaToTime(float deltaPixels, Rect rect, float totalDuration)
        {
            if (rect.width <= 0f || totalDuration <= 0f)
            {
                return 0f;
            }

            return deltaPixels / rect.width * GetVisibleDuration(totalDuration);
        }

        private static void SetTriggerTime(HitBoxConfig config, float value)
        {
            config.TriggerTime = value;
        }

        private static void SetTriggerTime(BulletConfig config, float value)
        {
            config.TriggerTime = value;
        }

        private static void SetTriggerTime(TimelineEventConfig config, float value)
        {
            config.TriggerTime = value;
        }

        private static void SetTriggerTime(TimelineVfxConfig config, float value)
        {
            config.TriggerTime = value;
        }

        private static void SetTriggerTime(TimelineAudioConfig config, float value)
        {
            config.TriggerTime = value;
        }

        private static void SetDuration(HitBoxConfig config, float value)
        {
            config.Duration = value;
        }

        private static void SetDuration(BulletConfig config, float value)
        {
            config.Duration = value;
        }

        private static void SetDuration(TimelineEventConfig config, float value)
        {
            config.Duration = value;
        }

        private static void SetDuration(TimelineVfxConfig config, float value)
        {
            config.Duration = config.Mode == TimelineVfxMode.Controlled ? value : 0f;
        }

        private static void SetDuration(TimelineAudioConfig config, float value)
        {
            config.Duration = 0f;
        }

        private void DrawDetailsPanel(Rect rect)
        {
            if (rect.height <= 0f)
            {
                return;
            }

            EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f));

            GUILayout.BeginArea(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f));
            _detailsScrollPosition = EditorGUILayout.BeginScrollView(_detailsScrollPosition);

            if (_config == null)
            {
                EditorGUILayout.HelpBox("当前没有可编辑的 State。", MessageType.Info);
                EditorGUILayout.EndScrollView();
                GUILayout.EndArea();
                return;
            }

            DrawTimelineSettingsPanel();
            EditorGUILayout.Space(8f);
            DrawSelectionDetails();

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawTimelineSettingsPanel()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Timeline 设置", EditorStyles.boldLabel);

            float currentDuration = _config.Timeline.Duration;
            float editedDuration = EditorGUILayout.FloatField("Duration", currentDuration);
            editedDuration = Mathf.Max(0f, editedDuration);
            if (!Mathf.Approximately(currentDuration, editedDuration))
            {
                _config.Timeline.Duration = editedDuration;
                MarkDirty();
            }

            float suggestedDuration = GetSuggestedTimelineDuration();
            EditorGUILayout.LabelField("建议时长", $"{suggestedDuration:F3}s");
            if (NeedsDurationSync())
            {
                EditorGUILayout.HelpBox("Timeline.Duration 与动画/轨道条目范围不一致，建议同步。", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSelectionDetails()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("详情", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("所有条目详情统一在 Inspector 窗口编辑。点击轨道或时间块后，请在 Inspector 查看和修改参数。", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawTrackDetails(TimelineTrackConfig track)
        {
            if (track == null)
            {
                EditorGUILayout.HelpBox("未选中轨道。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("轨道详情", EditorStyles.boldLabel);

            string displayName = EditorGUILayout.TextField("DisplayName", track.DisplayName);
            if (displayName != track.DisplayName)
            {
                track.DisplayName = displayName;
                MarkDirty();
            }

            EditorGUILayout.EnumPopup("TrackType", track.TrackType);
            bool enabled = EditorGUILayout.Toggle("IsEnabled", track.IsEnabled);
            if (enabled != track.IsEnabled)
            {
                track.IsEnabled = enabled;
                MarkDirty();
            }

            if (GUILayout.Button("新增条目", GUILayout.Width(100f)))
            {
                AddItemToTrack(track);
            }
        }

        private void DrawHitBoxDetails(TimelineTrackConfig track, HitBoxConfig config)
        {
            if (track == null || config == null)
            {
                EditorGUILayout.HelpBox("攻击盒条目无效。", MessageType.Warning);
                return;
            }

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
                MarkDirty();
            }

            string attachPoint = Draw挂点字段(config.SocketSource, config.AttachPoint, "攻击盒挂点");
            if (attachPoint != config.AttachPoint)
            {
                config.AttachPoint = attachPoint;
                MarkDirty();
            }

            HitBoxDetectionType detectionType = Draw攻击盒类型字段(config.ShapeArgs.DetectionType);
            if (detectionType != config.ShapeArgs.DetectionType)
            {
                config.ShapeArgs.DetectionType = detectionType;
                MarkDirty();
            }

            Vector3 offsetPosition = EditorGUILayout.Vector3Field("位置偏移", config.ShapeArgs.OffsetPosition);
            if (offsetPosition != config.ShapeArgs.OffsetPosition)
            {
                config.ShapeArgs.OffsetPosition = offsetPosition;
                MarkDirty();
            }

            Vector3 offsetRotation = EditorGUILayout.Vector3Field("旋转偏移", config.ShapeArgs.OffsetRotation);
            if (offsetRotation != config.ShapeArgs.OffsetRotation)
            {
                config.ShapeArgs.OffsetRotation = offsetRotation;
                MarkDirty();
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
                MarkDirty();
            }

            float hitInterval = EditorGUILayout.FloatField("重复命中间隔", config.ShapeArgs.HitInterval);
            if (!Mathf.Approximately(hitInterval, config.ShapeArgs.HitInterval))
            {
                config.ShapeArgs.HitInterval = Mathf.Max(0f, hitInterval);
                MarkDirty();
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
                MarkDirty();
            }

            Draw烘焙设置(config);

            float toughnessDamage = EditorGUILayout.FloatField("削减韧性(占位)", config.OnHitResponse.ToughnessDamage);
            if (!Mathf.Approximately(toughnessDamage, config.OnHitResponse.ToughnessDamage))
            {
                config.OnHitResponse.ToughnessDamage = Mathf.Max(0f, toughnessDamage);
                MarkDirty();
            }

            float hitStunDuration = EditorGUILayout.FloatField("命中僵直时长(占位)", config.OnHitResponse.HitStunDuration);
            if (!Mathf.Approximately(hitStunDuration, config.OnHitResponse.HitStunDuration))
            {
                config.OnHitResponse.HitStunDuration = Mathf.Max(0f, hitStunDuration);
                MarkDirty();
            }

            string hitStunTag = EditorGUILayout.TextField("命中僵直标签(占位)", config.OnHitResponse.HitStunTag);
            if (hitStunTag != config.OnHitResponse.HitStunTag)
            {
                config.OnHitResponse.HitStunTag = hitStunTag;
                MarkDirty();
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("OnHit Effects", EditorStyles.boldLabel);
            if (GUILayout.Button("命中 effects(BT)", GUILayout.Height(30f)))
            {
                config.OnHitEffect ??= new SkillEffectConfig();
                OpenEffectEditor(config.OnHitEffect, $"HitBox / {config.DisplayName}");
            }
            EditorGUILayout.LabelField(BuildEffectSummary(config.OnHitEffect), EditorStyles.miniLabel);

            if (GUILayout.Button("删除攻击盒", GUILayout.Width(100f)))
            {
                track.HitBoxes.Remove(config);
                _selectedItem = null;
                _selectedItemKind = SelectedItemKind.Track;
                MarkDirty();
            }
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
                MarkDirty();
            }

            int bakedCount = config.ShapeArgs.BakedParts != null ? config.ShapeArgs.BakedParts.Count : 0;
            Color previousColor = GUI.color;
            GUI.color = bakedCount > 0 ? Color.white : Color.red;
            EditorGUILayout.LabelField(
                bakedCount > 0 ? $"攻击盒已烘焙数量: {bakedCount}" : "!!从未烘焙过攻击盒!!",
                EditorStyles.miniBoldLabel);
            GUI.color = previousColor;

            using (new GUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(config.Duration <= 0f))
                {
                    if (GUILayout.Button("烘焙", GUILayout.Width(60f)))
                    {
                        BakeHitBoxPreview(config);
                    }
                }

                using (new EditorGUI.DisabledScope(bakedCount == 0))
                {
                    if (GUILayout.Button("清空烘焙", GUILayout.Width(80f)))
                    {
                        config.ShapeArgs.BakedParts = new List<HitBoxBakedPart>();
                        MarkDirty();
                    }
                }
            }

            BoxDrawType = EditorGUILayout.Popup("绘制类型", BoxDrawType, HitBoxDrawModes);
            if (BoxDrawType == 1 || BoxDrawType == 2)
            {
                BakerBoxLife = EditorGUILayout.IntSlider("烘焙攻击盒显示寿命", BakerBoxLife, 0, 1000);
            }

            EditorGUILayout.HelpBox("烘焙会把攻击盒在持续时间内每次采样得到的局部起点、方向、触发时间保存成 baked parts。运行时优先使用这些 baked parts 做判定，而不是每帧重新按挂点实时计算。", MessageType.Info);
        }

        private void DrawBulletDetails(TimelineTrackConfig track, BulletConfig config)
        {
            if (track == null || config == null)
            {
                EditorGUILayout.HelpBox("子弹条目无效。", MessageType.Warning);
                return;
            }

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
                MarkDirty();
            }

            string attachPoint = Draw挂点字段(config.SocketSource, config.AttachPoint, "发射器挂点");
            if (attachPoint != config.AttachPoint)
            {
                config.AttachPoint = attachPoint;
                MarkDirty();
            }

            GameObject bulletPrefab = string.IsNullOrEmpty(config.SpawnArgs.BulletPrefabPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(config.SpawnArgs.BulletPrefabPath);
            GameObject nextBulletPrefab = (GameObject)EditorGUILayout.ObjectField("BulletPrefab", bulletPrefab, typeof(GameObject), false);
            string nextBulletPrefabPath = nextBulletPrefab == null ? string.Empty : AssetDatabase.GetAssetPath(nextBulletPrefab);
            if (!string.Equals(nextBulletPrefabPath, config.SpawnArgs.BulletPrefabPath, StringComparison.Ordinal))
            {
                config.SpawnArgs.BulletPrefabPath = nextBulletPrefabPath;
                MarkDirty();
            }

            BulletFlightMode flightMode = (BulletFlightMode)EditorGUILayout.EnumPopup("FlightMode", config.SpawnArgs.FlightMode);
            if (flightMode != config.SpawnArgs.FlightMode)
            {
                config.SpawnArgs.FlightMode = flightMode;
                MarkDirty();
            }

            int spawnCount = EditorGUILayout.IntField("SpawnCount", config.SpawnArgs.SpawnCount);
            int sanitizedSpawnCount = Mathf.Max(1, spawnCount);
            if (sanitizedSpawnCount != config.SpawnArgs.SpawnCount)
            {
                config.SpawnArgs.SpawnCount = sanitizedSpawnCount;
                MarkDirty();
            }

            Vector3 positionOffset = EditorGUILayout.Vector3Field("PositionOffset", config.SpawnArgs.PositionOffset);
            if (positionOffset != config.SpawnArgs.PositionOffset)
            {
                config.SpawnArgs.PositionOffset = positionOffset;
                MarkDirty();
            }

            Vector3 rotationOffset = EditorGUILayout.Vector3Field("RotationOffset", config.SpawnArgs.RotationOffset);
            if (rotationOffset != config.SpawnArgs.RotationOffset)
            {
                config.SpawnArgs.RotationOffset = rotationOffset;
                MarkDirty();
            }

            float speed = EditorGUILayout.FloatField("Speed", config.SpawnArgs.Speed);
            if (!Mathf.Approximately(speed, config.SpawnArgs.Speed))
            {
                config.SpawnArgs.Speed = Mathf.Max(0f, speed);
                MarkDirty();
            }

            float maxLifetime = EditorGUILayout.FloatField("MaxLifetime", config.SpawnArgs.MaxLifetime);
            if (!Mathf.Approximately(maxLifetime, config.SpawnArgs.MaxLifetime))
            {
                config.SpawnArgs.MaxLifetime = Mathf.Max(0.01f, maxLifetime);
                MarkDirty();
            }

            float collisionRadius = EditorGUILayout.FloatField("CollisionRadius", config.SpawnArgs.CollisionRadius);
            if (!Mathf.Approximately(collisionRadius, config.SpawnArgs.CollisionRadius))
            {
                config.SpawnArgs.CollisionRadius = Mathf.Max(0f, collisionRadius);
                MarkDirty();
            }

            if (config.SpawnArgs.FlightMode == BulletFlightMode.Parabola || config.SpawnArgs.FlightMode == BulletFlightMode.HomingParabola)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Parabola", EditorStyles.boldLabel);

                float initialVerticalSpeed = EditorGUILayout.FloatField("InitialVerticalSpeed", config.SpawnArgs.Parabola.InitialVerticalSpeed);
                if (!Mathf.Approximately(initialVerticalSpeed, config.SpawnArgs.Parabola.InitialVerticalSpeed))
                {
                    config.SpawnArgs.Parabola.InitialVerticalSpeed = initialVerticalSpeed;
                    MarkDirty();
                }

                float gravity = EditorGUILayout.FloatField("Gravity", config.SpawnArgs.Parabola.Gravity);
                if (!Mathf.Approximately(gravity, config.SpawnArgs.Parabola.Gravity))
                {
                    config.SpawnArgs.Parabola.Gravity = Mathf.Max(0f, gravity);
                    MarkDirty();
                }
            }

            int hitLayerMask = Draw层级遮罩字段("HitLayerMask", config.SpawnArgs.HitLayerMask);
            if (hitLayerMask != config.SpawnArgs.HitLayerMask)
            {
                config.SpawnArgs.HitLayerMask = hitLayerMask;
                MarkDirty();
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Tracking (预留)", EditorStyles.boldLabel);
            float searchRange = EditorGUILayout.FloatField("SearchRange", config.SpawnArgs.Tracking.SearchRange);
            if (!Mathf.Approximately(searchRange, config.SpawnArgs.Tracking.SearchRange))
            {
                config.SpawnArgs.Tracking.SearchRange = Mathf.Max(0f, searchRange);
                MarkDirty();
            }

            float searchAngle = EditorGUILayout.Slider("SearchAngle", config.SpawnArgs.Tracking.SearchAngle, 0f, 180f);
            if (!Mathf.Approximately(searchAngle, config.SpawnArgs.Tracking.SearchAngle))
            {
                config.SpawnArgs.Tracking.SearchAngle = Mathf.Clamp(searchAngle, 0f, 180f);
                MarkDirty();
            }

            float centerWeight = EditorGUILayout.Slider("CenterWeight", config.SpawnArgs.Tracking.CenterWeight, 0f, 1f);
            if (!Mathf.Approximately(centerWeight, config.SpawnArgs.Tracking.CenterWeight))
            {
                config.SpawnArgs.Tracking.CenterWeight = Mathf.Clamp01(centerWeight);
                MarkDirty();
            }

            float acceleration = EditorGUILayout.FloatField("Acceleration", config.SpawnArgs.Tracking.Acceleration);
            if (!Mathf.Approximately(acceleration, config.SpawnArgs.Tracking.Acceleration))
            {
                config.SpawnArgs.Tracking.Acceleration = Mathf.Max(0f, acceleration);
                MarkDirty();
            }

            float straightDistance = EditorGUILayout.FloatField("StraightDistance", config.SpawnArgs.Tracking.StraightDistance);
            if (!Mathf.Approximately(straightDistance, config.SpawnArgs.Tracking.StraightDistance))
            {
                config.SpawnArgs.Tracking.StraightDistance = Mathf.Max(0.05f, straightDistance);
                MarkDirty();
            }

            float curveStrength = EditorGUILayout.FloatField("CurveStrength", config.SpawnArgs.Tracking.CurveStrength);
            if (!Mathf.Approximately(curveStrength, config.SpawnArgs.Tracking.CurveStrength))
            {
                config.SpawnArgs.Tracking.CurveStrength = Mathf.Max(0f, curveStrength);
                MarkDirty();
            }

            float curveLateralOffset = EditorGUILayout.FloatField("CurveLateralOffset", config.SpawnArgs.Tracking.CurveLateralOffset);
            if (!Mathf.Approximately(curveLateralOffset, config.SpawnArgs.Tracking.CurveLateralOffset))
            {
                config.SpawnArgs.Tracking.CurveLateralOffset = Mathf.Max(0f, curveLateralOffset);
                MarkDirty();
            }

            float curveVerticalOffset = EditorGUILayout.FloatField("CurveVerticalOffset", config.SpawnArgs.Tracking.CurveVerticalOffset);
            if (!Mathf.Approximately(curveVerticalOffset, config.SpawnArgs.Tracking.CurveVerticalOffset))
            {
                config.SpawnArgs.Tracking.CurveVerticalOffset = Mathf.Max(0f, curveVerticalOffset);
                MarkDirty();
            }

            float curveOscillation = EditorGUILayout.FloatField("CurveOscillation", config.SpawnArgs.Tracking.CurveOscillation);
            if (!Mathf.Approximately(curveOscillation, config.SpawnArgs.Tracking.CurveOscillation))
            {
                config.SpawnArgs.Tracking.CurveOscillation = Mathf.Max(0f, curveOscillation);
                MarkDirty();
            }

            float launchYawRange = EditorGUILayout.Slider("LaunchYawRange", config.SpawnArgs.Tracking.LaunchYawRange, 0f, 180f);
            if (!Mathf.Approximately(launchYawRange, config.SpawnArgs.Tracking.LaunchYawRange))
            {
                config.SpawnArgs.Tracking.LaunchYawRange = Mathf.Clamp(launchYawRange, 0f, 180f);
                MarkDirty();
            }

            float launchPitchRange = EditorGUILayout.Slider("LaunchPitchRange", config.SpawnArgs.Tracking.LaunchPitchRange, 0f, 89f);
            if (!Mathf.Approximately(launchPitchRange, config.SpawnArgs.Tracking.LaunchPitchRange))
            {
                config.SpawnArgs.Tracking.LaunchPitchRange = Mathf.Clamp(launchPitchRange, 0f, 89f);
                MarkDirty();
            }

            EditorGUILayout.HelpBox("Duration 小于等于 0 时，会在 TriggerTime 一次性发射 SpawnCount 个子弹；Duration 大于 0 时，会在该持续时间内按 SpawnCount 均匀发射多枚子弹。当前已实现 Direct、Parabola、HomingParabola 和 HomingCurve。CenterWeight 中，0 表示更偏向距离最近，1 表示更偏向视野中心。HomingParabola 会在发射瞬间锁定一次目标，之后按固定抛物线飞行。HomingCurve 会先按 LaunchYawRange 和 LaunchPitchRange 随机出射，再逐步绕弧跟随目标，并在进入 StraightDistance 后切为直线追击；CurveStrength、CurveLateralOffset、CurveVerticalOffset 一起决定弧度强弱；Speed 为初速度，Acceleration 为飞行过程中的加速度。", MessageType.Info);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("OnHit Effects", EditorStyles.boldLabel);
            if (GUILayout.Button("命中 effects(BT)", GUILayout.Height(30f)))
            {
                config.OnHitEffect ??= new SkillEffectConfig();
                OpenEffectEditor(config.OnHitEffect, $"Bullet / {config.DisplayName}");
            }
            EditorGUILayout.LabelField(BuildEffectSummary(config.OnHitEffect), EditorStyles.miniLabel);

            if (GUILayout.Button("删除子弹", GUILayout.Width(100f)))
            {
                track.Bullets.Remove(config);
                _selectedItem = null;
                _selectedItemKind = SelectedItemKind.Track;
                MarkDirty();
            }
        }

        private void DrawEventDetails(TimelineTrackConfig track, TimelineEventConfig config)
        {
            if (track == null || config == null)
            {
                EditorGUILayout.HelpBox("事件条目无效。", MessageType.Warning);
                return;
            }

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
                    config.DisplayName = GetDefaultEventItemName(nextType, track);
                }

                ApplyEventTypeDefaults(config);
                MarkDirty();
            }

            if (config.Data == null && nextType != TimelineEventType.None)
            {
                config.CreateData(nextType);
                ApplyEventTypeDefaults(config);
                MarkDirty();
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
                    MarkDirty();
                }

                float angle = EditorGUILayout.FloatField("最大角度差", softLockTarget.Args.Angle);
                if (!Mathf.Approximately(angle, softLockTarget.Args.Angle))
                {
                    softLockTarget.Args.Angle = angle;
                    MarkDirty();
                }

                int layerMask = Draw层级遮罩字段("锁定层级", softLockTarget.Args.LayerMask);
                if (layerMask != softLockTarget.Args.LayerMask)
                {
                    softLockTarget.Args.LayerMask = layerMask;
                    MarkDirty();
                }

                bool referToCamera = EditorGUILayout.Toggle("参考锁定方向至相机", softLockTarget.Args.ReferToCamera);
                if (referToCamera != softLockTarget.Args.ReferToCamera)
                {
                    softLockTarget.Args.ReferToCamera = referToCamera;
                    MarkDirty();
                }

                float rotationSpeed = EditorGUILayout.FloatField("旋转速度", softLockTarget.Args.RotationSpeed);
                if (!Mathf.Approximately(rotationSpeed, softLockTarget.Args.RotationSpeed))
                {
                    softLockTarget.Args.RotationSpeed = Mathf.Max(0f, rotationSpeed);
                    MarkDirty();
                }

                int priority = EditorGUILayout.IntField("旋转优先级", softLockTarget.Args.Priority);
                if (priority != softLockTarget.Args.Priority)
                {
                    softLockTarget.Args.Priority = priority;
                    MarkDirty();
                }
            }
            else if (config.Data is HitStop_TimelineEventData hitStop)
            {
                HitStopEventArgs args = hitStop.Args;
                FeedbackTriggerMode triggerMode = (FeedbackTriggerMode)EditorGUILayout.EnumPopup("触发模式", args.TriggerMode);
                if (triggerMode != args.TriggerMode) { args.TriggerMode = triggerMode; MarkDirty(); }
                if (args.TriggerMode == FeedbackTriggerMode.OnHit && Mathf.Approximately(config.Duration, 0f))
                {
                    EditorGUILayout.HelpBox("OnHit 模式需要 Duration > 0 或 Duration < 0；Duration 表示命中监听窗口。", MessageType.Warning);
                }

                bool affectAttacker = EditorGUILayout.Toggle("影响攻击者", args.AffectAttacker);
                if (affectAttacker != args.AffectAttacker) { args.AffectAttacker = affectAttacker; MarkDirty(); }
                if (args.AffectAttacker)
                {
                    float duration = EditorGUILayout.FloatField("攻击者停顿时长", args.AttackerDuration);
                    float scale = EditorGUILayout.Slider("攻击者时间倍率", args.AttackerTimeScale, 0f, 1f);
                    if (!Mathf.Approximately(duration, args.AttackerDuration)) { args.AttackerDuration = Mathf.Max(0f, duration); MarkDirty(); }
                    if (!Mathf.Approximately(scale, args.AttackerTimeScale)) { args.AttackerTimeScale = scale; MarkDirty(); }
                }

                bool affectDefender = EditorGUILayout.Toggle("影响受击者", args.AffectDefender);
                if (affectDefender != args.AffectDefender) { args.AffectDefender = affectDefender; MarkDirty(); }
                if (args.AffectDefender)
                {
                    float duration = EditorGUILayout.FloatField("受击者停顿时长", args.DefenderDuration);
                    float scale = EditorGUILayout.Slider("受击者时间倍率", args.DefenderTimeScale, 0f, 1f);
                    if (!Mathf.Approximately(duration, args.DefenderDuration)) { args.DefenderDuration = Mathf.Max(0f, duration); MarkDirty(); }
                    if (!Mathf.Approximately(scale, args.DefenderTimeScale)) { args.DefenderTimeScale = scale; MarkDirty(); }
                }

                bool triggerOnce = EditorGUILayout.Toggle("单事件仅触发一次", args.TriggerOncePerEvent);
                bool mergeSameFrame = EditorGUILayout.Toggle("合并同帧多目标", args.MergeSameFrameHits);
                int priority = EditorGUILayout.IntField("优先级", args.Priority);
                if (triggerOnce != args.TriggerOncePerEvent) { args.TriggerOncePerEvent = triggerOnce; MarkDirty(); }
                if (mergeSameFrame != args.MergeSameFrameHits) { args.MergeSameFrameHits = mergeSameFrame; MarkDirty(); }
                if (priority != args.Priority) { args.Priority = priority; MarkDirty(); }
            }
            else if (config.Data is CameraShake_TimelineEventData cameraShake)
            {
                CameraShakeEventArgs args = cameraShake.Args;
                FeedbackTriggerMode triggerMode = (FeedbackTriggerMode)EditorGUILayout.EnumPopup("触发模式", args.TriggerMode);
                if (triggerMode != args.TriggerMode) { args.TriggerMode = triggerMode; MarkDirty(); }
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
                if (!Mathf.Approximately(amplitude, args.Amplitude)) { args.Amplitude = Mathf.Max(0f, amplitude); MarkDirty(); }
                if (!Mathf.Approximately(frequency, args.Frequency)) { args.Frequency = Mathf.Max(0.01f, frequency); MarkDirty(); }
                if (!Mathf.Approximately(shakeDuration, args.ShakeDuration)) { args.ShakeDuration = Mathf.Max(0f, shakeDuration); MarkDirty(); }
                if (direction != (Vector3)args.Direction) { args.Direction = direction; MarkDirty(); }
                if (useHitDirection != args.UseHitDirection) { args.UseHitDirection = useHitDirection; MarkDirty(); }
                if (triggerOnce != args.TriggerOncePerEvent) { args.TriggerOncePerEvent = triggerOnce; MarkDirty(); }
                if (mergeSameFrame != args.MergeSameFrameHits) { args.MergeSameFrameHits = mergeSameFrame; MarkDirty(); }
            }
            else if (config.Data is HitVfx_TimelineEventData hitVfx)
            {
                HitVfxEventArgs args = hitVfx.Args;
                FeedbackTriggerMode triggerMode = (FeedbackTriggerMode)EditorGUILayout.EnumPopup("触发模式", args.TriggerMode);
                if (triggerMode != args.TriggerMode) { args.TriggerMode = triggerMode; MarkDirty(); }
                if (args.TriggerMode == FeedbackTriggerMode.OnHit && Mathf.Approximately(config.Duration, 0f))
                    EditorGUILayout.HelpBox("OnHit 模式需要 Duration > 0 或 Duration < 0。", MessageType.Warning);
                GameObject prefab = string.IsNullOrEmpty(args.PrefabPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(args.PrefabPath);
                GameObject nextPrefab = (GameObject)EditorGUILayout.ObjectField("特效 Prefab", prefab, typeof(GameObject), false);
                string prefabPath = nextPrefab == null ? string.Empty : AssetDatabase.GetAssetPath(nextPrefab);
                if (prefabPath != args.PrefabPath) { args.PrefabPath = prefabPath; MarkDirty(); }
                if (nextPrefab != null && nextPrefab.GetComponentInChildren<ParticleSystem>(true) == null)
                    EditorGUILayout.HelpBox("Prefab 中没有 ParticleSystem。", MessageType.Warning);
                VfxPlaySpace space = (VfxPlaySpace)EditorGUILayout.EnumPopup("播放空间", args.Space);
                HitVfxRotationMode rotationMode = (HitVfxRotationMode)EditorGUILayout.EnumPopup("旋转模式", args.RotationMode);
                Vector3 positionOffset = EditorGUILayout.Vector3Field("位置偏移", args.PositionOffset);
                Vector3 rotationOffset = EditorGUILayout.Vector3Field("旋转偏移", args.RotationOffset);
                Vector3 scale = EditorGUILayout.Vector3Field("缩放", args.Scale);
                float lifetime = EditorGUILayout.FloatField("生命周期", args.Lifetime);
                bool unscaled = EditorGUILayout.Toggle("使用非缩放时间", args.UseUnscaledTime);
                bool triggerOnce = EditorGUILayout.Toggle("单事件仅触发一次", args.TriggerOncePerEvent);
                bool merge = EditorGUILayout.Toggle("合并同帧多目标", args.MergeSameFrameHits);
                if (space != args.Space) { args.Space = space; MarkDirty(); }
                if (rotationMode != args.RotationMode) { args.RotationMode = rotationMode; MarkDirty(); }
                if (positionOffset != (Vector3)args.PositionOffset) { args.PositionOffset = positionOffset; MarkDirty(); }
                if (rotationOffset != (Vector3)args.RotationOffset) { args.RotationOffset = rotationOffset; MarkDirty(); }
                if (scale != (Vector3)args.Scale) { args.Scale = scale; MarkDirty(); }
                if (!Mathf.Approximately(lifetime, args.Lifetime)) { args.Lifetime = Mathf.Max(0.01f, lifetime); MarkDirty(); }
                if (unscaled != args.UseUnscaledTime) { args.UseUnscaledTime = unscaled; MarkDirty(); }
                if (triggerOnce != args.TriggerOncePerEvent) { args.TriggerOncePerEvent = triggerOnce; MarkDirty(); }
                if (merge != args.MergeSameFrameHits) { args.MergeSameFrameHits = merge; MarkDirty(); }
            }
            else if (config.Data is HitAudio_TimelineEventData hitAudio)
            {
                HitAudioEventArgs args = hitAudio.Args;
                FeedbackTriggerMode triggerMode = (FeedbackTriggerMode)EditorGUILayout.EnumPopup("触发模式", args.TriggerMode);
                if (triggerMode != args.TriggerMode) { args.TriggerMode = triggerMode; MarkDirty(); }
                if (args.TriggerMode == FeedbackTriggerMode.OnHit && Mathf.Approximately(config.Duration, 0f))
                    EditorGUILayout.HelpBox("OnHit 模式需要 Duration > 0 或 Duration < 0。", MessageType.Warning);
                AudioClip clip = string.IsNullOrEmpty(args.AudioClipPath) ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(args.AudioClipPath);
                AudioClip nextClip = (AudioClip)EditorGUILayout.ObjectField("AudioClip", clip, typeof(AudioClip), false);
                string clipPath = nextClip == null ? string.Empty : AssetDatabase.GetAssetPath(nextClip);
                if (clipPath != args.AudioClipPath) { args.AudioClipPath = clipPath; MarkDirty(); }
                UnityEngine.Audio.AudioMixerGroup mixerGroup = null;
                if (!string.IsNullOrEmpty(args.AudioMixerPath) && !string.IsNullOrEmpty(args.MixerGroupName))
                {
                    UnityEngine.Audio.AudioMixer mixer = AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>(args.AudioMixerPath);
                    UnityEngine.Audio.AudioMixerGroup[] groups = mixer != null ? mixer.FindMatchingGroups(args.MixerGroupName) : null;
                    if (groups != null && groups.Length > 0) mixerGroup = groups[0];
                }
                UnityEngine.Audio.AudioMixerGroup nextGroup = (UnityEngine.Audio.AudioMixerGroup)EditorGUILayout.ObjectField("Mixer Group", mixerGroup, typeof(UnityEngine.Audio.AudioMixerGroup), false);
                string mixerPath = nextGroup == null ? string.Empty : AssetDatabase.GetAssetPath(nextGroup.audioMixer);
                string groupName = nextGroup == null ? string.Empty : nextGroup.name;
                if (mixerPath != args.AudioMixerPath || groupName != args.MixerGroupName) { args.AudioMixerPath = mixerPath; args.MixerGroupName = groupName; MarkDirty(); }
                AudioPlaySpace space = (AudioPlaySpace)EditorGUILayout.EnumPopup("播放空间", args.Space);
                float volume = EditorGUILayout.Slider("音量", args.Volume, 0f, 1f);
                float pitch = EditorGUILayout.Slider("Pitch", args.Pitch, 0.01f, 3f);
                float blend = EditorGUILayout.Slider("Spatial Blend", args.SpatialBlend, 0f, 1f);
                float minDistance = EditorGUILayout.FloatField("Min Distance", args.MinDistance);
                float maxDistance = EditorGUILayout.FloatField("Max Distance", args.MaxDistance);
                bool triggerOnce = EditorGUILayout.Toggle("单事件仅触发一次", args.TriggerOncePerEvent);
                bool merge = EditorGUILayout.Toggle("合并同帧多目标", args.MergeSameFrameHits);
                if (space != args.Space) { args.Space = space; MarkDirty(); }
                if (!Mathf.Approximately(volume, args.Volume)) { args.Volume = volume; MarkDirty(); }
                if (!Mathf.Approximately(pitch, args.Pitch)) { args.Pitch = pitch; MarkDirty(); }
                if (!Mathf.Approximately(blend, args.SpatialBlend)) { args.SpatialBlend = blend; MarkDirty(); }
                if (!Mathf.Approximately(minDistance, args.MinDistance)) { args.MinDistance = Mathf.Max(0.01f, minDistance); MarkDirty(); }
                if (!Mathf.Approximately(maxDistance, args.MaxDistance)) { args.MaxDistance = Mathf.Max(args.MinDistance, maxDistance); MarkDirty(); }
                if (triggerOnce != args.TriggerOncePerEvent) { args.TriggerOncePerEvent = triggerOnce; MarkDirty(); }
                if (merge != args.MergeSameFrameHits) { args.MergeSameFrameHits = merge; MarkDirty(); }
            }
            else if (config.Data != null && config.Data.ArgsObject is ApplyForceEventArgs applyForceArgs)
            {
                Vector3 force = EditorGUILayout.Vector3Field("Force", applyForceArgs.Force);
                if (force != applyForceArgs.Force)
                {
                    applyForceArgs.Force = force;
                    MarkDirty();
                }

                bool useLocalSpace = EditorGUILayout.Toggle("UseLocalSpace", applyForceArgs.UseLocalSpace);
                if (useLocalSpace != applyForceArgs.UseLocalSpace)
                {
                    applyForceArgs.UseLocalSpace = useLocalSpace;
                    MarkDirty();
                }

            }
            else if (config.Data != null && config.Data.ArgsObject is GravityEventArgs gravityArgs)
            {
                bool enableGravity = EditorGUILayout.Toggle("EnableGravity", gravityArgs.EnableGravity);
                if (enableGravity != gravityArgs.EnableGravity)
                {
                    gravityArgs.EnableGravity = enableGravity;
                    MarkDirty();
                }

                bool overrideGravityVector = EditorGUILayout.Toggle("OverrideGravityVector", gravityArgs.OverrideGravityVector);
                if (overrideGravityVector != gravityArgs.OverrideGravityVector)
                {
                    gravityArgs.OverrideGravityVector = overrideGravityVector;
                    MarkDirty();
                }

                using (new EditorGUI.DisabledScope(!gravityArgs.OverrideGravityVector))
                {
                    Vector3 gravity = EditorGUILayout.Vector3Field("Gravity", gravityArgs.Gravity);
                    if (gravity != gravityArgs.Gravity)
                    {
                        gravityArgs.Gravity = gravity;
                        MarkDirty();
                    }
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
                    MarkDirty();
                }
            }
            else if (config.Data != null && config.Data.ArgsObject is AddTagEventArgs addTagArgs)
            {
                addTagArgs.Tags ??= new List<string>();
                if (SkillEditorInspectorWindow.TagSelectionEditorUtility.DrawTagList("Tags", addTagArgs.Tags))
                {
                    MarkDirty();
                }

                int stack = EditorGUILayout.IntField("Stack", addTagArgs.Stack);
                int sanitizedStack = Mathf.Max(1, stack);
                if (sanitizedStack != addTagArgs.Stack)
                {
                    addTagArgs.Stack = sanitizedStack;
                    MarkDirty();
                }
            }

            if (GUILayout.Button("删除事件", GUILayout.Width(100f)))
            {
                track.MetaSkillEvents.Remove(config);
                _selectedItem = null;
                _selectedItemKind = SelectedItemKind.Track;
                MarkDirty();
            }
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
                MarkDirty();
            }
        }

        private void DrawInterruptDetails(StateInterruptConfig config)
        {
            if (config == null)
            {
                EditorGUILayout.HelpBox("打断条目无效。", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("打断详情", EditorStyles.boldLabel);

            bool isEnabled = EditorGUILayout.Toggle("IsEnabled", config.IsEnabled);
            if (isEnabled != config.IsEnabled)
            {
                config.IsEnabled = isEnabled;
                MarkDirty();
            }

            DrawTargetStateField(config);

            float triggerTime = Mathf.Max(0f, EditorGUILayout.FloatField("TriggerTime", config.TriggerTime));
            if (!Mathf.Approximately(triggerTime, config.TriggerTime))
            {
                config.TriggerTime = triggerTime;
                MarkDirty();
            }

            float duration = EditorGUILayout.FloatField("Duration", config.Duration);
            if (!Mathf.Approximately(duration, config.Duration))
            {
                config.Duration = duration;
                MarkDirty();
            }

            float executeTime = Mathf.Max(0f, EditorGUILayout.FloatField("ExecuteTime", config.ExecuteTime));
            if (!Mathf.Approximately(executeTime, config.ExecuteTime))
            {
                config.ExecuteTime = executeTime;
                MarkDirty();
            }

            int sortOrder = EditorGUILayout.IntField("SortOrder", config.SortOrder);
            if (sortOrder != config.SortOrder)
            {
                config.SortOrder = sortOrder;
                MarkDirty();
            }

            bool checkAllConditions = EditorGUILayout.Toggle("CheckAllConditions", config.CheckAllConditions);
            if (checkAllConditions != config.CheckAllConditions)
            {
                config.CheckAllConditions = checkAllConditions;
                MarkDirty();
            }

            bool useTransitionOverride = EditorGUILayout.Toggle("UseTransitionOverride", config.UseTransitionOverride);
            if (useTransitionOverride != config.UseTransitionOverride)
            {
                config.UseTransitionOverride = useTransitionOverride;
                MarkDirty();
            }

            using (new EditorGUI.DisabledScope(!config.UseTransitionOverride))
            {
                float transitionDuration = Mathf.Max(0f, EditorGUILayout.FloatField("TransitionDuration", config.TransitionDuration));
                if (!Mathf.Approximately(transitionDuration, config.TransitionDuration))
                {
                    config.TransitionDuration = transitionDuration;
                    MarkDirty();
                }

                AnimationTransitionTimeUnit transitionTimeUnit = (AnimationTransitionTimeUnit)EditorGUILayout.EnumPopup("TransitionTimeUnit", config.TransitionTimeUnit);
                if (transitionTimeUnit != config.TransitionTimeUnit)
                {
                    config.TransitionTimeUnit = transitionTimeUnit;
                    MarkDirty();
                }
            }

            float targetStartTime = Mathf.Max(0f, EditorGUILayout.FloatField("TargetStartTime", config.TargetStartTime));
            if (!Mathf.Approximately(targetStartTime, config.TargetStartTime))
            {
                config.TargetStartTime = targetStartTime;
                MarkDirty();
            }

            AnimationStartTimeUnit targetStartTimeUnit = (AnimationStartTimeUnit)EditorGUILayout.EnumPopup("TargetStartTimeUnit", config.TargetStartTimeUnit);
            if (targetStartTimeUnit != config.TargetStartTimeUnit)
            {
                config.TargetStartTimeUnit = targetStartTimeUnit;
                MarkDirty();
            }

            EditorGUILayout.Space(6f);
            DrawInterruptConditionList(config);

            if (GUILayout.Button("删除打断", GUILayout.Width(100f)))
            {
                RemoveInterrupt(config);
                _selectedItem = null;
                _selectedItemKind = SelectedItemKind.None;
                MarkDirty();
            }
        }

        private void DrawTargetStateField(StateInterruptConfig config)
        {
            List<SkillResourceFileEntry> stateEntries = SkillResourceRepository.LoadStates(SkillPreviewUnitSettings.ActiveUnitId);
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
                if (string.Equals(stateId, config.TargetStateId, StringComparison.Ordinal))
                {
                    currentIndex = i + 1;
                }
            }

            int nextIndex = EditorGUILayout.Popup("TargetState", currentIndex, optionLabels);
            string nextValue = nextIndex >= 0 && nextIndex < optionValues.Length ? optionValues[nextIndex] : string.Empty;
            if (!string.Equals(nextValue, config.TargetStateId, StringComparison.Ordinal))
            {
                config.TargetStateId = nextValue;
                MarkDirty();
            }
        }

        private void DrawInterruptConditionList(StateInterruptConfig config)
        {
            config.Conditions ??= new List<IStateInterruptCondition>();
            EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);

            for (int i = 0; i < config.Conditions.Count; i++)
            {
                IStateInterruptCondition condition = config.Conditions[i];
                if (condition == null)
                {
                    continue;
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(condition.GetDisplayName(), EditorStyles.miniBoldLabel);
                DrawInterruptConditionFields(condition);
                if (GUILayout.Button("删除条件", GUILayout.Width(100f)))
                {
                    config.Conditions.RemoveAt(i);
                    MarkDirty();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("新增条件", GUILayout.Width(100f)))
            {
                ShowAddInterruptConditionMenu(config);
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

        private void ShowAddInterruptConditionMenu(StateInterruptConfig config)
        {
            GenericMenu menu = new GenericMenu();
            AddInterruptConditionMenuItem(menu, config, "输入动作条件", "StateKeyInterruptCondition");
            AddInterruptConditionMenuItem(menu, config, "移动输入条件", "StateMoveInputInterruptCondition");
            AddInterruptConditionMenuItem(menu, config, "命中条件", "StateHitInterruptCondition");
            AddInterruptConditionMenuItem(menu, config, "受击条件", "StateBeHitInterruptCondition");
            AddInterruptConditionMenuItem(menu, config, "Tag 条件", "StateTagInterruptCondition");
            AddInterruptConditionMenuItem(menu, config, "BreakValue 条件", "StateBreakValueInterruptCondition");
            AddInterruptConditionMenuItem(menu, config, "运动/接地条件", "StateGroundingInterruptCondition");
            AddInterruptConditionMenuItem(menu, config, "运动/速度条件", "StateMotionValueInterruptCondition");
            menu.ShowAsContext();
        }

        private void AddInterruptCondition(StateInterruptConfig config, IStateInterruptCondition condition)
        {
            if (config == null || condition == null)
            {
                return;
            }

            config.Conditions ??= new List<IStateInterruptCondition>();
            config.Conditions.Add(condition);
            MarkDirty();
        }

        private void AddInterruptConditionMenuItem(GenericMenu menu, StateInterruptConfig config, string label, string typeName)
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
                    AddInterruptCondition(config, condition);
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
            if (!Equals(currentValue, nextValue))
            {
                field.SetValue(target, nextValue);
                MarkDirty();
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
            if (!string.Equals(currentValue, nextValue, StringComparison.Ordinal))
            {
                field.SetValue(target, nextValue);
                MarkDirty();
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
                MarkDirty();
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
                MarkDirty();
            }
        }

        private void DrawReflectedFloatField(object target, string fieldName, string label, bool clampToZero)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            if (field == null || field.FieldType != typeof(float))
            {
                return;
            }

            float currentValue = (float)field.GetValue(target);
            float nextValue = EditorGUILayout.FloatField(label, currentValue);
            if (clampToZero)
            {
                nextValue = Mathf.Max(0f, nextValue);
            }

            if (!Mathf.Approximately(currentValue, nextValue))
            {
                field.SetValue(target, nextValue);
                MarkDirty();
            }
        }

        private static Type FindRuntimeType(string typeName)
        {
            return Type.GetType($"AsiSkillEditor.RunTime.{typeName}, Assembly-CSharp");
        }

        private void DrawPlayhead(Rect timelineAreaRect)
        {
            float duration = GetExecutionDisplayDuration();
            if (duration <= 0f || timelineAreaRect.width <= 0f)
            {
                return;
            }

            float x = TimeToPixel(Mathf.Min(GetExecutePreviewTime(), duration), timelineAreaRect, duration);
            EditorGUI.DrawRect(new Rect(x, timelineAreaRect.y, 2f, timelineAreaRect.height), PlayheadColor);
            EditorGUI.DrawRect(new Rect(x - 7f, timelineAreaRect.y + 4f, 14f, 16f), PlayheadColor);
        }

        private void HandleTimelineInput(Rect timelineAreaRect)
        {
            if (!CanPreview())
            {
                return;
            }

            Event current = Event.current;
            if ((current.type != EventType.MouseDown && current.type != EventType.MouseDrag) ||
                current.button != 0 ||
                !timelineAreaRect.Contains(current.mousePosition))
            {
                return;
            }

            float duration = GetExecutionDisplayDuration();
            if (duration <= 0f)
            {
                return;
            }

            _previewTime = Mathf.Clamp(PixelToTime(current.mousePosition.x, timelineAreaRect, duration), 0f, duration);
            _isPlaying = false;
            SamplePreview();
            current.Use();
            Repaint();
        }

        private void DrawTimelineGrid(Rect rect)
        {
            float duration = GetExecutionDisplayDuration();
            if (duration <= 0f)
            {
                return;
            }

            float visibleStart = GetVisibleStartTime(duration);
            float visibleEnd = visibleStart + GetVisibleDuration(duration);
            int startFrame = Mathf.Max(0, Mathf.FloorToInt(visibleStart * GetFrameRate()));
            int endFrame = Mathf.CeilToInt(visibleEnd * GetFrameRate());
            int majorStep = GetMajorFrameStep(rect.width, GetVisibleDuration(duration));
            int minorStep = Mathf.Max(1, majorStep / 5);

            for (int frame = startFrame; frame <= endFrame; frame++)
            {
                if (frame % minorStep != 0)
                {
                    continue;
                }

                float time = frame / (float)GetFrameRate();
                float x = TimeToPixel(time, rect, duration);
                Color lineColor = frame % majorStep == 0
                    ? new Color(0f, 0f, 0f, 0.28f)
                    : new Color(0f, 0f, 0f, 0.12f);
                EditorGUI.DrawRect(new Rect(x, rect.y, 1f, rect.height), lineColor);
            }
        }

        private void DrawRangeSlider(Rect rect)
        {
            if (rect.width <= 0f)
            {
                return;
            }

            EditorGUI.DrawRect(new Rect(0f, rect.y, HeaderWidth, rect.height), HeaderBackground);
            EditorGUI.DrawRect(rect, new Color(0.24f, 0.24f, 0.24f));
            EditorGUI.MinMaxSlider(rect, ref _rangeMin, ref _rangeMax, 0f, 1f);

            Event current = Event.current;
            if (rect.Contains(current.mousePosition) && current.type == EventType.MouseDown && current.clickCount == 2)
            {
                _rangeMin = 0f;
                _rangeMax = 1f;
                current.Use();
            }
        }

        private void DrawStatusBar(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f));

            string leftText = BuildStatusText();
            Color previousColor = GUI.contentColor;
            GUI.contentColor = HasPreviewWarning() ? WarningTextColor : NormalTextColor;
            GUI.Label(new Rect(rect.x + 8f, rect.y + 3f, rect.width - 220f, rect.height - 4f), leftText, _statusLabel);
            GUI.contentColor = previousColor;

            string rightText = _entry != null ? $"{_entry.BaseName}{(_isDirty ? " *" : string.Empty)}" : "State";
            GUI.Label(new Rect(rect.xMax - 210f, rect.y + 3f, 200f, rect.height - 4f), rightText, new GUIStyle(_statusLabel)
            {
                alignment = TextAnchor.MiddleRight,
            });
        }

        private bool DrawToolbarButton(string iconName, string fallbackText, string tooltip, float width)
        {
            GUIContent content = EditorGUIUtility.IconContent(iconName, tooltip);
            if (content == null || content.image == null)
            {
                content = new GUIContent(fallbackText, tooltip);
            }
            else
            {
                content.text = string.Empty;
                content.tooltip = tooltip;
            }

            return GUILayout.Button(content, EditorStyles.toolbarButton, GUILayout.Width(width));
        }

        private void TogglePlay()
        {
            if (!CanPreview())
            {
                return;
            }

            _isPlaying = !_isPlaying;
            _lastUpdateTime = EditorApplication.timeSinceStartup;
            if (_isPlaying)
            {
                _lastAudioPreviewTime = _previewTime - 0.0001f;
            }
            if (!_isPlaying)
            {
                SamplePreview();
            }
        }

        private void StepFrame(int frameStep)
        {
            if (!CanPreview())
            {
                return;
            }

            _isPlaying = false;
            float duration = GetPreviewFlowDuration();
            float frameDelta = 1f / GetFrameRate();
            _previewTime = Mathf.Clamp(_previewTime + frameDelta * frameStep, 0f, duration);
            SamplePreview();
            Repaint();
        }

        private void StopPreview()
        {
            _isPlaying = false;
            _previewTime = 0f;
            _lastAudioPreviewTime = 0f;
            StopAllAudioPreviews();
            if (CanPreview())
            {
                SamplePreview();
            }
            else
            {
                SkillPreviewAnimationUtility.Stop();
            }
        }

        private void OnEditorUpdate()
        {
            if (!_isPlaying || !CanPreview())
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(now - _lastUpdateTime);
            _lastUpdateTime = now;

            float duration = GetPreviewFlowDuration();
            if (duration <= 0f)
            {
                _isPlaying = false;
                return;
            }

            _previewTime += deltaTime * _playSpeed;
            if (_previewTime > duration)
            {
                if (_isLoop)
                {
                    _previewTime = 0f;
                }
                else
                {
                    _previewTime = duration;
                    _isPlaying = false;
                }
            }

            SamplePreview();
            Repaint();
        }

        private void SamplePreview()
        {
            SampleAudioPreview();

            GameObject sceneInstance = SkillPreviewSceneInstanceUtility.GetCurrentInstance();
            if (sceneInstance == null)
            {
                SkillPreviewAnimationUtility.Stop();
                CleanupVfxPreview();
                return;
            }

            AnimationClip activeClip = GetActivePreviewClip();
            if (activeClip == null)
            {
                SkillPreviewAnimationUtility.Stop();
                CleanupVfxPreview();
                return;
            }

            SkillPreviewAnimationUtility.Sample(sceneInstance, activeClip, GetActivePreviewSampleTime());
            SampleSelectedVfxPreview(sceneInstance);
        }

        private void SampleAudioPreview()
        {
            if (!_isPlaying)
            {
                StopAllAudioPreviews();
                _lastAudioPreviewTime = _previewTime;
                return;
            }

            float previousAudioTime = _lastAudioPreviewTime;
            if (_previewTime < _lastAudioPreviewTime && _lastAudioPreviewTime >= 0f)
            {
                StopAllAudioPreviews();
                previousAudioTime = -0.0001f;
            }

            if (_config != null && _config.Timeline != null && _config.Timeline.Tracks != null)
            {
                for (int trackIndex = 0; trackIndex < _config.Timeline.Tracks.Count; trackIndex++)
                {
                    TimelineTrackConfig track = _config.Timeline.Tracks[trackIndex];
                    if (track == null || !track.IsEnabled || track.AudioClips == null)
                    {
                        continue;
                    }

                    for (int clipIndex = 0; clipIndex < track.AudioClips.Count; clipIndex++)
                    {
                        TimelineAudioConfig config = track.AudioClips[clipIndex];
                        if (config == null || !config.IsEnabled || string.IsNullOrEmpty(config.AudioClipPath) ||
                            config.TriggerTime <= previousAudioTime || config.TriggerTime > _previewTime)
                        {
                            continue;
                        }

                        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(config.AudioClipPath);
                        if (clip != null)
                        {
                            PlayAudioPreview(clip, config.Volume, config.Pitch);
                        }
                    }
                }
            }

            _lastAudioPreviewTime = _previewTime;
        }

        internal static void PlayAudioPreview(AudioClip clip, float volume, float pitch)
        {
            if (clip == null)
            {
                return;
            }

            Type audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            MethodInfo playMethod = audioUtilType?.GetMethod(
                "PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null);
            if (playMethod == null)
            {
                return;
            }

            playMethod.Invoke(null, new object[] { clip, 0, false });
            MethodInfo setVolumeMethod = audioUtilType.GetMethod(
                "SetPreviewClipVolume",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(float) },
                null);
            setVolumeMethod?.Invoke(null, new object[] { Mathf.Clamp01(volume) });

            MethodInfo setPitchMethod = audioUtilType.GetMethod(
                "SetPreviewClipPitch",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(float) },
                null);
            setPitchMethod?.Invoke(null, new object[] { Mathf.Clamp(pitch, 0.01f, 3f) });
        }

        internal static void StopAllAudioPreviews()
        {
            Type audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            MethodInfo stopMethod = audioUtilType?.GetMethod(
                "StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            stopMethod?.Invoke(null, null);
        }

        private void SampleSelectedVfxPreview(GameObject sceneInstance)
        {
            if (!(_selectedItem is TimelineVfxConfig config) || !config.IsEnabled ||
                string.IsNullOrEmpty(config.PrefabPath) || _previewTime < config.TriggerTime)
            {
                CleanupVfxPreview();
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(config.PrefabPath);
            if (prefab == null || !TryResolvePreviewAnchor(sceneInstance, config.SocketSource, config.AttachPoint, out Transform anchor))
            {
                CleanupVfxPreview();
                return;
            }

            Vector3 previewPosition;
            Quaternion previewRotation;
            if (config.FollowMode == TimelineFollowMode.SpawnAtSocket)
            {
                AnimationClip activeClip = GetActivePreviewClip();
                TimelineAnimationConfig animationConfig = _config != null && _config.Timeline != null ? _config.Timeline.Animation : null;
                SkillPreviewAnimationUtility.Sample(sceneInstance, activeClip, GetAnimationSampleTime(animationConfig, activeClip, config.TriggerTime));
                if (!TryResolvePreviewAnchor(sceneInstance, config.SocketSource, config.AttachPoint, out anchor))
                {
                    CleanupVfxPreview();
                    return;
                }
                previewPosition = anchor.TransformPoint(config.PositionOffset);
                previewRotation = anchor.rotation * Quaternion.Euler(config.RotationOffset);
                SkillPreviewAnimationUtility.Sample(sceneInstance, activeClip, GetActivePreviewSampleTime());
            }
            else
            {
                previewPosition = anchor.TransformPoint(config.PositionOffset);
                previewRotation = anchor.rotation * Quaternion.Euler(config.RotationOffset);
            }

            if (_vfxPreviewInstance == null || _vfxPreviewPrefabPath != config.PrefabPath)
            {
                CleanupVfxPreview();
                _vfxPreviewInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (_vfxPreviewInstance == null)
                {
                    _vfxPreviewInstance = Instantiate(prefab);
                }
                _vfxPreviewInstance.name = $"{prefab.name} (Timeline VFX Preview)";
                _vfxPreviewInstance.hideFlags = HideFlags.HideAndDontSave;
                _vfxPreviewPrefabPath = config.PrefabPath;
            }

            _vfxPreviewInstance.transform.SetPositionAndRotation(previewPosition, previewRotation);
            _vfxPreviewInstance.transform.localScale = config.Scale;

            float localTime = Mathf.Max(0f, _previewTime - config.TriggerTime);
            ParticleSystem[] particles = _vfxPreviewInstance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem particle = particles[i];
                particle.useAutoRandomSeed = false;
                particle.randomSeed = (uint)(137 + i * 977);
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Simulate(0f, true, true, true);

                if (config.Mode == TimelineVfxMode.Controlled && localTime > config.Duration)
                {
                    particle.Simulate(Mathf.Max(0f, config.Duration), true, true, true);
                    particle.Stop(true, config.StopMode == TimelineVfxStopMode.StopAndClear
                        ? ParticleSystemStopBehavior.StopEmittingAndClear
                        : ParticleSystemStopBehavior.StopEmitting);
                    if (config.StopMode == TimelineVfxStopMode.StopEmitting)
                    {
                        particle.Simulate(localTime - config.Duration, true, false, false);
                    }
                }
                else
                {
                    particle.Simulate(localTime, true, true, true);
                }
            }

            SceneView.RepaintAll();
        }

        private void CleanupVfxPreview()
        {
            if (_vfxPreviewInstance != null)
            {
                DestroyImmediate(_vfxPreviewInstance);
            }
            _vfxPreviewInstance = null;
            _vfxPreviewPrefabPath = string.Empty;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (sceneView == null || !sceneView.drawGizmos)
            {
                return;
            }

            GameObject sceneInstance = SkillPreviewSceneInstanceUtility.GetCurrentInstance();
            if (sceneInstance == null)
            {
                return;
            }

            if (_selectedItemKind == SelectedItemKind.HitBox &&
                _selectedItem is HitBoxConfig hitBox &&
                hitBox.IsEnabled)
            {
                DrawHitBoxScenePreview(sceneInstance, hitBox);
            }

            if (_selectedItemKind == SelectedItemKind.Bullet &&
                _selectedItem is BulletConfig bullet &&
                bullet.IsEnabled)
            {
                DrawBulletScenePreview(sceneInstance, bullet);
            }

            DrawActiveEventScenePreviews(sceneInstance);
        }

        private void DrawBulletScenePreview(GameObject sceneInstance, BulletConfig config)
        {
            if (sceneInstance == null || config == null || config.SpawnArgs == null)
            {
                return;
            }

            if (!TryResolvePreviewAnchor(sceneInstance, config.SocketSource, config.AttachPoint, out Transform anchorTransform))
            {
                return;
            }

            Vector3 spawnPosition = anchorTransform.TransformPoint(config.SpawnArgs.PositionOffset);
            Quaternion spawnRotation = anchorTransform.rotation * Quaternion.Euler(config.SpawnArgs.RotationOffset);
            Vector3 direction = spawnRotation * Vector3.forward;
            float radius = Mathf.Max(0.02f, config.SpawnArgs.CollisionRadius);
            float directionLength = Mathf.Max(0.4f, config.SpawnArgs.Speed * 0.08f);

            Color previousColor = Handles.color;
            try
            {
                Handles.color = new Color(1.00f, 0.62f, 0.20f, 0.95f);
                Handles.DrawWireDisc(spawnPosition, Vector3.up, radius);
                if (config.SpawnArgs.FlightMode == BulletFlightMode.Parabola || config.SpawnArgs.FlightMode == BulletFlightMode.HomingParabola)
                {
                    DrawBulletParabolaPreview(spawnPosition, direction, config.SpawnArgs);
                }
                else if (config.SpawnArgs.FlightMode == BulletFlightMode.HomingCurve)
                {
                    DrawBulletHomingCurvePreview(spawnPosition, direction, config);
                }
                else
                {
                    Handles.DrawLine(spawnPosition, spawnPosition + direction * directionLength);
                    Handles.ConeHandleCap(0, spawnPosition + direction * directionLength, spawnRotation, 0.08f, EventType.Repaint);
                }

                Handles.Label(spawnPosition + Vector3.up * Mathf.Max(0.12f, radius), $"Bullet Spawn x{ResolveBulletPreviewSpawnCount(config)}");
            }
            finally
            {
                Handles.color = previousColor;
            }
        }

        private static void DrawBulletHomingCurvePreview(Vector3 spawnPosition, Vector3 forwardDirection, BulletConfig config)
        {
            if (config == null || config.SpawnArgs == null)
            {
                return;
            }

            BulletSpawnArgs spawnArgs = config.SpawnArgs;
            BulletTrackingArgs trackingArgs = spawnArgs.Tracking;
            if (trackingArgs == null)
            {
                return;
            }

            int seed = ComputeStablePreviewSeed(config);
            float yaw = Mathf.Lerp(-trackingArgs.LaunchYawRange, trackingArgs.LaunchYawRange, SampleDeterministic01(seed, 0));
            float pitch = Mathf.Lerp(-trackingArgs.LaunchPitchRange, trackingArgs.LaunchPitchRange, SampleDeterministic01(seed, 1));
            float lateralScale = Mathf.Lerp(-1f, 1f, SampleDeterministic01(seed, 2));
            float verticalScale = Mathf.Lerp(-1f, 1f, SampleDeterministic01(seed, 3));
            float phase = SampleDeterministic01(seed, 4) * Mathf.PI * 2f;

            Vector3 normalizedForward = forwardDirection.sqrMagnitude > Mathf.Epsilon ? forwardDirection.normalized : Vector3.forward;
            Vector3 launchDirection = ResolvePreviewLaunchDirection(normalizedForward, yaw, pitch);
            float previewDistance = Mathf.Max(2f, trackingArgs.SearchRange > 0f ? trackingArgs.SearchRange : spawnArgs.Speed * Mathf.Max(0.35f, spawnArgs.MaxLifetime * 0.4f));
            Vector3 targetPoint = spawnPosition + normalizedForward * previewDistance;

            Handles.DrawWireDisc(targetPoint, Vector3.up, Mathf.Max(0.08f, spawnArgs.CollisionRadius * 1.25f));
            Handles.Label(targetPoint + Vector3.up * 0.12f, "Preview Target");

            int segmentCount = 28;
            Vector3 previousPoint = spawnPosition;
            Vector3 previousDirection = launchDirection;
            Vector3 currentPoint = spawnPosition;
            float speed = Mathf.Max(0.01f, spawnArgs.Speed);
            float currentSpeed = speed;
            float totalTime = Mathf.Max(0.25f, spawnArgs.MaxLifetime);
            float stepTime = totalTime / segmentCount;
            Vector3 planeNormal = Vector3.Cross(launchDirection, targetPoint - spawnPosition);
            if (planeNormal.sqrMagnitude <= 0.001f)
            {
                planeNormal = Vector3.Cross(launchDirection, Vector3.up);
            }

            Vector3 curveAxis = Vector3.Cross(planeNormal.normalized, launchDirection).normalized;
            if (curveAxis.sqrMagnitude <= Mathf.Epsilon)
            {
                curveAxis = Vector3.right;
            }

            for (int i = 1; i <= segmentCount; i++)
            {
                currentSpeed += Mathf.Max(0f, trackingArgs.Acceleration) * stepTime;
                Vector3 toTarget = targetPoint - currentPoint;
                float distanceToTarget = toTarget.magnitude;
                if (distanceToTarget <= Mathf.Epsilon)
                {
                    break;
                }

                float progress = i / (float)segmentCount;
                float arcBlend = distanceToTarget <= Mathf.Max(0.05f, trackingArgs.StraightDistance) ? 0f : Mathf.Sin(progress * Mathf.PI);
                float curveStrength = Mathf.Max(0f, trackingArgs.CurveStrength);
                float oscillation = Mathf.Max(0f, trackingArgs.CurveOscillation);
                float sampledPhase = progress * oscillation * Mathf.PI * 2f + phase;
                Vector3 desiredPoint = targetPoint
                    + curveAxis * (trackingArgs.CurveLateralOffset * lateralScale * curveStrength * arcBlend)
                    + Vector3.up * (trackingArgs.CurveVerticalOffset * verticalScale * curveStrength * Mathf.Sin(sampledPhase) * arcBlend);

                Vector3 desiredDirection = (distanceToTarget <= Mathf.Max(0.05f, trackingArgs.StraightDistance)
                        ? toTarget
                        : desiredPoint - currentPoint).normalized;
                float steerStrength = distanceToTarget <= Mathf.Max(0.05f, trackingArgs.StraightDistance)
                    ? 14f
                    : Mathf.Lerp(1.5f, 7f, progress);
                Vector3 stepDirection = Vector3.Slerp(previousDirection, desiredDirection, Mathf.Clamp01(steerStrength * stepTime));
                if (stepDirection.sqrMagnitude <= Mathf.Epsilon)
                {
                    stepDirection = desiredDirection;
                }

                currentPoint += stepDirection.normalized * currentSpeed * stepTime;
                Handles.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
                previousDirection = stepDirection.normalized;
            }

            if ((previousPoint - spawnPosition).sqrMagnitude > Mathf.Epsilon)
            {
                Quaternion endRotation = Quaternion.LookRotation((previousPoint - spawnPosition).normalized, Vector3.up);
                Handles.ConeHandleCap(0, previousPoint, endRotation, 0.08f, EventType.Repaint);
            }
        }

        private static void DrawBulletParabolaPreview(Vector3 spawnPosition, Vector3 forwardDirection, BulletSpawnArgs spawnArgs)
        {
            if (spawnArgs == null)
            {
                return;
            }

            int segmentCount = 24;
            float totalTime = Mathf.Max(0.1f, spawnArgs.MaxLifetime);
            Vector3 velocity = forwardDirection.normalized * Mathf.Max(0f, spawnArgs.Speed);
            float initialVerticalSpeed = spawnArgs.Parabola != null ? spawnArgs.Parabola.InitialVerticalSpeed : 0f;
            float gravity = spawnArgs.Parabola != null ? Mathf.Max(0f, spawnArgs.Parabola.Gravity) : 0f;
            velocity += Vector3.up * initialVerticalSpeed;

            Vector3 previousPoint = spawnPosition;
            for (int i = 1; i <= segmentCount; i++)
            {
                float time = totalTime * i / segmentCount;
                Vector3 gravityVector = Vector3.down * gravity;
                Vector3 point = spawnPosition + velocity * time + 0.5f * gravityVector * time * time;
                Handles.DrawLine(previousPoint, point);
                previousPoint = point;
            }

            Vector3 endDirection = previousPoint - spawnPosition;
            if (endDirection.sqrMagnitude > Mathf.Epsilon)
            {
                Quaternion endRotation = Quaternion.LookRotation(endDirection.normalized, Vector3.up);
                Handles.ConeHandleCap(0, previousPoint, endRotation, 0.08f, EventType.Repaint);
            }
        }

        private static Vector3 ResolvePreviewLaunchDirection(Vector3 forwardDirection, float yaw, float pitch)
        {
            Vector3 yawDirection = Quaternion.AngleAxis(yaw, Vector3.up) * forwardDirection;
            Vector3 pitchAxis = Vector3.Cross(Vector3.up, yawDirection);
            if (pitchAxis.sqrMagnitude <= Mathf.Epsilon)
            {
                pitchAxis = Vector3.right;
            }

            Vector3 launchDirection = Quaternion.AngleAxis(pitch, pitchAxis.normalized) * yawDirection;
            return launchDirection.sqrMagnitude > Mathf.Epsilon ? launchDirection.normalized : forwardDirection;
        }

        private static int ComputeStablePreviewSeed(BulletConfig config)
        {
            unchecked
            {
                int hash = 17;
                string bulletId = config != null ? config.BulletId : string.Empty;
                string displayName = config != null ? config.DisplayName : string.Empty;
                hash = hash * 31 + (bulletId != null ? bulletId.GetHashCode() : 0);
                hash = hash * 31 + (displayName != null ? displayName.GetHashCode() : 0);
                return hash;
            }
        }

        private static float SampleDeterministic01(int seed, int salt)
        {
            unchecked
            {
                int value = seed ^ (salt * 374761393);
                value = (value << 13) ^ value;
                int hashed = value * (value * value * 15731 + 789221) + 1376312589;
                return Mathf.Abs((hashed & 0x7fffffff) / (float)int.MaxValue);
            }
        }

        private static int ResolveBulletPreviewSpawnCount(BulletConfig config)
        {
            if (config == null || config.SpawnArgs == null)
            {
                return 0;
            }

            return Mathf.Max(1, config.SpawnArgs.SpawnCount);
        }

        private void DrawActiveEventScenePreviews(GameObject sceneInstance)
        {
            if (sceneInstance == null || _config == null || _config.Timeline == null || _config.Timeline.Tracks == null)
            {
                return;
            }

            for (int trackIndex = 0; trackIndex < _config.Timeline.Tracks.Count; trackIndex++)
            {
                TimelineTrackConfig track = _config.Timeline.Tracks[trackIndex];
                if (track == null || !track.IsEnabled || track.MetaSkillEvents == null)
                {
                    continue;
                }

                for (int eventIndex = 0; eventIndex < track.MetaSkillEvents.Count; eventIndex++)
                {
                    TimelineEventConfig metaSkillEvent = track.MetaSkillEvents[eventIndex];
                    if (!IsEventPreviewActive(metaSkillEvent))
                    {
                        continue;
                    }

                    if (metaSkillEvent.Data is SoftLockTarget_TimelineEventData softLockTarget)
                    {
                        DrawSoftLockScenePreview(sceneInstance, softLockTarget.Args);
                    }
                }
            }
        }

        private bool IsEventPreviewActive(TimelineEventConfig config)
        {
            if (config == null || !config.IsEnabled || config.Data == null)
            {
                return false;
            }

            float startTime = Mathf.Max(0f, config.TriggerTime);
            float previewTime = GetExecutePreviewTime();
            if (previewTime < startTime)
            {
                return false;
            }

            if (config.Data.SupportsDuration)
            {
                if (Mathf.Approximately(config.Duration, 0f))
                {
                    return false;
                }

                if (config.Duration < 0f)
                {
                    return true;
                }

                return previewTime <= startTime + config.Duration;
            }

            return Mathf.Abs(previewTime - startTime) <= 1f / GetFrameRate();
        }

        private void DrawSoftLockScenePreview(GameObject sceneInstance, SoftLockTargetEventArgs args)
        {
            if (sceneInstance == null || args == null)
            {
                return;
            }

            Transform rootTransform = sceneInstance.transform;
            Vector3 origin = rootTransform.position + Vector3.up * 0.05f;
            Vector3 forward = Vector3.ProjectOnPlane(rootTransform.forward, Vector3.up);
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            float radius = Mathf.Max(0f, args.Radius);
            float angle = Mathf.Clamp(args.Angle, 0f, 360f);
            float halfAngle = angle * 0.5f;
            Vector3 leftDirection = Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward;
            Vector3 rightDirection = Quaternion.AngleAxis(halfAngle, Vector3.up) * forward;

            Color previousColor = Handles.color;
            try
            {
                Color fillColor = new Color(1f, 0.35f, 0.75f, 0.08f);
                Color lineColor = new Color(1f, 0.55f, 0.85f, 0.95f);
                Handles.color = fillColor;
                if (radius > 0f && angle > 0f)
                {
                    Handles.DrawSolidArc(origin, Vector3.up, leftDirection, angle, radius);
                }

                Handles.color = lineColor;
                if (radius > 0f)
                {
                    if (angle >= 360f)
                    {
                        Handles.DrawWireDisc(origin, Vector3.up, radius);
                    }
                    else if (angle > 0f)
                    {
                        Handles.DrawWireArc(origin, Vector3.up, leftDirection, angle, radius);
                        Handles.DrawLine(origin, origin + leftDirection * radius);
                        Handles.DrawLine(origin, origin + rightDirection * radius);
                    }

                    Handles.DrawLine(origin, origin + forward * radius);
                }

                Handles.Label(origin + forward * Mathf.Max(0.4f, radius * 0.55f), $"SoftLock  R:{radius:0.##}  A:{angle:0.##}  Rot:{args.RotationSpeed:0.##}");
            }
            finally
            {
                Handles.color = previousColor;
            }
        }

        private void DrawHitBoxScenePreview(GameObject sceneInstance, HitBoxConfig config)
        {
            List<HitBoxPreviewShape> shapes = BuildHitBoxPreviewShapes(sceneInstance, config);
            if (shapes.Count == 0)
            {
                return;
            }

            Color previousColor = Handles.color;
            try
            {
                for (int i = 0; i < shapes.Count; i++)
                {
                    HitBoxPreviewShape shape = shapes[i];
                    Color baseColor = shape.IsBakedPreview ? Color.blue : Color.green;
                    Color color = new Color(baseColor.r, baseColor.g, baseColor.b, shape.IsCurrentSample ? 1f : 0.45f);
                    Handles.color = color;
                    DrawHitBoxPreviewShape(shape);
                }
            }
            finally
            {
                Handles.color = previousColor;
                RestorePreviewSample(sceneInstance);
            }
        }

        private List<HitBoxPreviewShape> BuildHitBoxPreviewShapes(GameObject sceneInstance, HitBoxConfig config)
        {
            List<HitBoxPreviewShape> shapes = new List<HitBoxPreviewShape>();
            if (sceneInstance == null || config == null || config.ShapeArgs == null)
            {
                return shapes;
            }

            if ((BoxDrawType == 0 || BoxDrawType == 2) && ShouldDrawDefaultHitBox(config))
            {
                if (TryBuildHitBoxPreviewShape(sceneInstance, config, out HitBoxPreviewShape singleShape))
                {
                    singleShape.IsCurrentSample = true;
                    singleShape.IsBakedPreview = false;
                    shapes.Add(singleShape);
                }
            }

            if ((BoxDrawType == 1 || BoxDrawType == 2) && config.ShapeArgs.BakedParts != null && config.ShapeArgs.BakedParts.Count > 0)
            {
                AppendBakedPreviewShapes(sceneInstance, config, shapes);
            }

            return shapes;
        }

        private bool ShouldDrawDefaultHitBox(HitBoxConfig config)
        {
            if (config == null)
            {
                return false;
            }

            float startTime = Mathf.Max(0f, config.TriggerTime);
            float previewTime = GetExecutePreviewTime();
            if (previewTime < startTime)
            {
                return false;
            }

            float duration = Mathf.Max(0f, config.Duration);
            if (duration <= 0f)
            {
                return Mathf.Abs(previewTime - startTime) <= 1f / GetFrameRate();
            }

            return previewTime <= startTime + duration;
        }

        private void AppendBakedPreviewShapes(GameObject sceneInstance, HitBoxConfig config, List<HitBoxPreviewShape> shapes)
        {
            if (sceneInstance == null || config == null || config.ShapeArgs == null || config.ShapeArgs.BakedParts == null)
            {
                return;
            }

            float currentLocalTime = GetExecutePreviewTime() - config.TriggerTime;
            if (currentLocalTime < 0f)
            {
                return;
            }

            float bakedLife = BakerBoxLife / (float)GetFrameRate();
            List<HitBoxBakedPart> bakedParts = config.ShapeArgs.BakedParts;
            for (int i = 0; i < bakedParts.Count; i++)
            {
                HitBoxBakedPart bakedPart = bakedParts[i];
                if (bakedPart == null || currentLocalTime < bakedPart.TriggerTime)
                {
                    continue;
                }

                bool isVisible;
                if (i == bakedParts.Count - 1)
                {
                    isVisible = currentLocalTime <= bakedPart.TriggerTime + bakedLife;
                }
                else
                {
                    HitBoxBakedPart nextPart = bakedParts[i + 1];
                    float nextTriggerTime = nextPart != null ? nextPart.TriggerTime : bakedPart.TriggerTime;
                    isVisible = currentLocalTime < nextTriggerTime + bakedLife;
                }

                if (!isVisible)
                {
                    continue;
                }

                Transform rootTransform = sceneInstance.transform;
                Vector3 start = rootTransform.TransformPoint(bakedPart.StartPos);
                Vector3 end = start + rootTransform.TransformDirection(bakedPart.Direction) * Mathf.Max(0f, config.ShapeArgs.Scale.x);
                HitBoxPreviewShape shape = CreatePreviewShapeFromSegment(config, start, end, Mathf.Approximately(currentLocalTime, bakedPart.TriggerTime));
                shape.IsBakedPreview = true;
                shapes.Add(shape);
            }
        }

        private static float GetBakeSampleTime(HitBoxConfig config, int index, int bakeCount)
        {
            if (config == null)
            {
                return 0f;
            }

            if (bakeCount <= 1)
            {
                return config.TriggerTime;
            }

            float t = index / (float)(bakeCount - 1);
            return config.TriggerTime + Mathf.Max(0f, config.Duration) * t;
        }

        private void SampleSceneInstance(GameObject sceneInstance, float sampleTime)
        {
            if (sceneInstance == null)
            {
                return;
            }

            if (_clip == null)
            {
                return;
            }

            TimelineAnimationConfig executeAnimationConfig = _config != null && _config.Timeline != null
                ? _config.Timeline.Animation
                : null;
            float animationSampleTime = GetAnimationSampleTime(executeAnimationConfig, _clip, Mathf.Max(0f, sampleTime));
            SkillPreviewAnimationUtility.Sample(sceneInstance, _clip, animationSampleTime);
        }

        private void RestorePreviewSample(GameObject sceneInstance)
        {
            if (sceneInstance == null)
            {
                return;
            }

            SamplePreview();
        }

        private bool TryBuildHitBoxPreviewShape(GameObject sceneInstance, HitBoxConfig config, out HitBoxPreviewShape shape)
        {
            shape = default;
            if (sceneInstance == null || config == null || config.ShapeArgs == null)
            {
                return false;
            }

            if (!TryResolvePreviewAnchor(sceneInstance, config.SocketSource, config.AttachPoint, out Transform anchorTransform))
            {
                return false;
            }

            Vector3 scale = config.ShapeArgs.Scale;
            Vector3 start = anchorTransform.TransformPoint(config.ShapeArgs.OffsetPosition);
            Quaternion rotation = anchorTransform.rotation * Quaternion.Euler(config.ShapeArgs.OffsetRotation);
            Vector3 end = start + rotation * Vector3.forward * Mathf.Max(0f, scale.x);
            shape = CreatePreviewShapeFromSegment(config, start, end, true);
            return true;
        }

        private static HitBoxPreviewShape CreatePreviewShapeFromSegment(HitBoxConfig config, Vector3 start, Vector3 end, bool isCurrentSample)
        {
            Vector3 scale = config.ShapeArgs.Scale;
            Vector3 direction = end - start;
            Quaternion rotation = direction.sqrMagnitude > Mathf.Epsilon
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : Quaternion.identity;

            return new HitBoxPreviewShape
            {
                DetectionType = config.ShapeArgs.DetectionType,
                Start = start,
                End = end,
                Rotation = rotation,
                Radius = Mathf.Max(0f, scale.y),
                IsCurrentSample = isCurrentSample,
            };
        }

        private void DrawHitBoxPreviewShape(HitBoxPreviewShape shape)
        {
            switch (shape.DetectionType)
            {
                case HitBoxDetectionType.Capsule:
                    DrawCapsuleWire(shape.Start, shape.End, shape.Radius, shape.Rotation);
                    break;

                case HitBoxDetectionType.Raycast:
                    Handles.DrawLine(shape.Start, shape.End);
                    Handles.ConeHandleCap(0, shape.End, shape.Rotation, 0.06f, EventType.Repaint);
                    break;
            }
        }

        private static void DrawCapsuleWire(Vector3 start, Vector3 end, float radius, Quaternion rotation)
        {
            Handles.DrawWireDisc(start, rotation * Vector3.forward, radius);
            Handles.DrawWireDisc(end, rotation * Vector3.forward, radius);
            Vector3 right = rotation * Vector3.right * radius;
            Vector3 up = rotation * Vector3.up * radius;
            Handles.DrawLine(start + right, end + right);
            Handles.DrawLine(start - right, end - right);
            Handles.DrawLine(start + up, end + up);
            Handles.DrawLine(start - up, end - up);
        }

        private bool TryResolvePreviewAnchor(GameObject sceneInstance, SkillSocketSourceType socketSource, string attachPoint, out Transform anchorTransform)
        {
            Transform rootTransform = null;
            if (socketSource == SkillSocketSourceType.Character)
            {
                rootTransform = sceneInstance.transform;
                GameUnit previewConfig = sceneInstance.GetComponent<GameUnit>() ?? sceneInstance.GetComponentInChildren<GameUnit>(true);
                if (TryResolvePreviewMountPoint(previewConfig != null ? previewConfig.MountPoints : null, attachPoint, out anchorTransform))
                {
                    return true;
                }
            }
            else
            {
                PreviewWeaponConfig previewWeaponConfig = sceneInstance.GetComponentInChildren<PreviewWeaponConfig>(true);
                rootTransform = previewWeaponConfig != null ? previewWeaponConfig.transform : sceneInstance.transform;
                if (TryResolvePreviewMountPoint(previewWeaponConfig != null ? previewWeaponConfig.MountPoints : null, attachPoint, out anchorTransform))
                {
                    return true;
                }
            }

            if (rootTransform == null)
            {
                anchorTransform = null;
                return false;
            }

            if (string.IsNullOrEmpty(attachPoint))
            {
                anchorTransform = rootTransform;
                return true;
            }

            Transform child = FindChildRecursive(rootTransform, attachPoint);
            anchorTransform = child != null ? child : rootTransform;
            return true;
        }

        private static bool TryResolvePreviewMountPoint(IList<PreviewMountPoint> mountPoints, string attachPoint, out Transform mountTransform)
        {
            mountTransform = null;
            if (mountPoints == null || string.IsNullOrEmpty(attachPoint))
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

                if (string.Equals(mountPoint.SocketName, attachPoint, StringComparison.Ordinal))
                {
                    mountTransform = mountPoint.MountTransform;
                    return true;
                }
            }

            return false;
        }

        private static Transform FindChildRecursive(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrEmpty(targetName))
            {
                return null;
            }

            if (string.Equals(root.name, targetName, StringComparison.Ordinal))
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindChildRecursive(root.GetChild(i), targetName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private struct HitBoxPreviewShape
        {
            public HitBoxDetectionType DetectionType;
            public Vector3 Start;
            public Vector3 End;
            public Quaternion Rotation;
            public float Radius;
            public bool IsCurrentSample;
            public bool IsBakedPreview;
        }

        private void SaveCurrentEntry()
        {
            if (_entry == null || _config == null)
            {
                return;
            }

            SanitizeTimelineData();
            SkillResourceRepository.Save(_entry);
            _onOuterModified?.Invoke();
            _isDirty = false;
        }

        private void SyncTimelineDuration()
        {
            if (_config == null)
            {
                return;
            }

            _config.Timeline.Duration = GetSuggestedTimelineDuration();
            MarkDirty();
        }

        private void SanitizeTimelineData()
        {
            EnsureTimelineData();
            float maxEndTime = 0f;

            for (int i = _config.Timeline.Tracks.Count - 1; i >= 0; i--)
            {
                TimelineTrackConfig track = _config.Timeline.Tracks[i];
                if (track == null)
                {
                    _config.Timeline.Tracks.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrEmpty(track.TrackId))
                {
                    track.TrackId = Guid.NewGuid().ToString("N");
                }

                if (track.HitBoxes == null)
                {
                    track.HitBoxes = new List<HitBoxConfig>();
                }

                if (track.Bullets == null)
                {
                    track.Bullets = new List<BulletConfig>();
                }

                if (track.VfxClips == null)
                {
                    track.VfxClips = new List<TimelineVfxConfig>();
                }

                if (track.AudioClips == null)
                {
                    track.AudioClips = new List<TimelineAudioConfig>();
                }

                if (track.MetaSkillEvents == null)
                {
                    track.MetaSkillEvents = new List<TimelineEventConfig>();
                }

                SanitizeHitBoxes(track.HitBoxes, ref maxEndTime);
                SanitizeBullets(track.Bullets, ref maxEndTime);
                SanitizeVfxClips(track.VfxClips, ref maxEndTime);
                SanitizeAudioClips(track.AudioClips, ref maxEndTime);
                SanitizeEvents(track.MetaSkillEvents, ref maxEndTime);
            }

            SanitizeInterruptTracks(_config.Timeline.InterruptTracks, ref maxEndTime);
            if ((_config.Timeline.InterruptTracks == null || _config.Timeline.InterruptTracks.Count == 0) && _config.Timeline.Interrupts != null)
            {
                SanitizeInterrupts(_config.Timeline.Interrupts, ref maxEndTime);
            }

            _config.Timeline.Duration = Mathf.Max(_config.Timeline.Duration, maxEndTime, _clip != null ? _clip.length : 0f);
        }

        private static void SanitizeInterruptTracks(List<StateInterruptTrackConfig> tracks, ref float maxEndTime)
        {
            if (tracks == null)
            {
                return;
            }

            for (int i = tracks.Count - 1; i >= 0; i--)
            {
                StateInterruptTrackConfig track = tracks[i];
                if (track == null)
                {
                    tracks.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrEmpty(track.TrackId))
                {
                    track.TrackId = Guid.NewGuid().ToString("N");
                }

                track.Interrupts ??= new List<StateInterruptConfig>();
                SanitizeInterrupts(track.Interrupts, ref maxEndTime);
            }
        }

        private static void SanitizeInterrupts(List<StateInterruptConfig> interrupts, ref float maxEndTime)
        {
            if (interrupts == null)
            {
                return;
            }

            for (int i = interrupts.Count - 1; i >= 0; i--)
            {
                StateInterruptConfig config = interrupts[i];
                if (config == null)
                {
                    interrupts.RemoveAt(i);
                    continue;
                }

                config.TriggerTime = Mathf.Max(0f, config.TriggerTime);
                config.ExecuteTime = Mathf.Max(0f, config.ExecuteTime);
                config.TransitionDuration = Mathf.Max(0f, config.TransitionDuration);
                config.TargetStartTime = Mathf.Max(0f, config.TargetStartTime);
                config.Conditions ??= new List<IStateInterruptCondition>();

                float visualEnd = config.Duration < 0f ? config.TriggerTime : config.TriggerTime + Mathf.Max(0f, config.Duration);
                maxEndTime = Mathf.Max(maxEndTime, visualEnd);
            }
        }

        private void AddInterruptTrack()
        {
            EnsureTimelineData();
            StateInterruptTrackConfig track = new StateInterruptTrackConfig
            {
                TrackId = Guid.NewGuid().ToString("N"),
                DisplayName = $"打断轨道{GetInterruptTracks().Count + 1}",
                IsEnabled = true,
                Interrupts = new List<StateInterruptConfig>(),
            };

            _config.Timeline.InterruptTracks.Add(track);
            MarkDirty();
        }

        private static void SanitizeHitBoxes(List<HitBoxConfig> hitBoxes, ref float maxEndTime)
        {
            for (int i = hitBoxes.Count - 1; i >= 0; i--)
            {
                HitBoxConfig config = hitBoxes[i];
                if (config == null)
                {
                    hitBoxes.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrEmpty(config.HitBoxId))
                {
                    config.HitBoxId = Guid.NewGuid().ToString("N");
                }

                config.TriggerTime = Mathf.Max(0f, config.TriggerTime);
                config.Duration = Mathf.Max(0f, config.Duration);
                if (config.ShapeArgs != null)
                {
                    config.ShapeArgs.BakeCount = Mathf.Max(0, config.ShapeArgs.BakeCount);
                    config.ShapeArgs.BakedParts ??= new List<HitBoxBakedPart>();
                }
                maxEndTime = Mathf.Max(maxEndTime, config.TriggerTime + config.Duration);
            }
        }

        private static void SanitizeBullets(List<BulletConfig> bullets, ref float maxEndTime)
        {
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                BulletConfig config = bullets[i];
                if (config == null)
                {
                    bullets.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrEmpty(config.BulletId))
                {
                    config.BulletId = Guid.NewGuid().ToString("N");
                }

                config.TriggerTime = Mathf.Max(0f, config.TriggerTime);
                config.Duration = Mathf.Max(0f, config.Duration);
                maxEndTime = Mathf.Max(maxEndTime, config.TriggerTime + config.Duration);
            }
        }

        private static void SanitizeEvents(List<TimelineEventConfig> events, ref float maxEndTime)
        {
            for (int i = events.Count - 1; i >= 0; i--)
            {
                TimelineEventConfig config = events[i];
                if (config == null)
                {
                    events.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrEmpty(config.EventId))
                {
                    config.EventId = Guid.NewGuid().ToString("N");
                }

                config.TriggerTime = Mathf.Max(0f, config.TriggerTime);
                config.Duration = EventSupportsDuration(config) ? config.Duration : 0f;
                maxEndTime = Mathf.Max(maxEndTime, ResolveEventEndTimeForEditor(config, maxEndTime));
            }
        }

        private static void SanitizeVfxClips(List<TimelineVfxConfig> clips, ref float maxEndTime)
        {
            for (int i = clips.Count - 1; i >= 0; i--)
            {
                TimelineVfxConfig config = clips[i];
                if (config == null)
                {
                    clips.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrEmpty(config.VfxId)) config.VfxId = Guid.NewGuid().ToString("N");
                config.TriggerTime = Mathf.Max(0f, config.TriggerTime);
                config.Duration = config.Mode == TimelineVfxMode.Controlled ? Mathf.Max(1f / GetFrameRate(), config.Duration) : 0f;
                config.TailTimeout = Mathf.Max(0.01f, config.TailTimeout);
                maxEndTime = Mathf.Max(maxEndTime, config.TriggerTime + config.Duration);
            }
        }

        private static void SanitizeAudioClips(List<TimelineAudioConfig> clips, ref float maxEndTime)
        {
            for (int i = clips.Count - 1; i >= 0; i--)
            {
                TimelineAudioConfig config = clips[i];
                if (config == null)
                {
                    clips.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrEmpty(config.AudioId)) config.AudioId = Guid.NewGuid().ToString("N");
                config.TriggerTime = Mathf.Max(0f, config.TriggerTime);
                config.Duration = 0f;
                config.Volume = Mathf.Clamp01(config.Volume);
                config.Pitch = Mathf.Clamp(config.Pitch, 0.01f, 3f);
                config.SpatialBlend = Mathf.Clamp01(config.SpatialBlend);
                config.MinDistance = Mathf.Max(0.01f, config.MinDistance);
                config.MaxDistance = Mathf.Max(config.MinDistance, config.MaxDistance);
                maxEndTime = Mathf.Max(maxEndTime, config.TriggerTime);
            }
        }

        private void AddTrack(TimelineTrackType trackType)
        {
            EnsureTimelineData();
            TimelineTrackConfig track = new TimelineTrackConfig
            {
                TrackType = trackType,
                DisplayName = $"{GetTrackGroupLabel(trackType)}轨道{GetTracksForType(trackType).Count + 1}",
                IsEnabled = true,
            };

            _config.Timeline.Tracks.Add(track);
            _groupExpanded[trackType] = true;
            SelectTrack(track);
            MarkDirty();
        }

        private void DeleteTrack(TimelineTrackConfig track)
        {
            if (track == null || _config == null || _config.Timeline == null || _config.Timeline.Tracks == null)
            {
                return;
            }

            _config.Timeline.Tracks.Remove(track);
            if (_selectedTrack == track)
            {
                _selectedTrack = null;
                _selectedItem = null;
                _selectedItemKind = SelectedItemKind.None;
            }

            MarkDirty();
        }

        private void AddItemToTrack(TimelineTrackConfig track)
        {
            if (track == null)
            {
                return;
            }

            switch (track.TrackType)
            {
                case TimelineTrackType.HitBox:
                    HitBoxConfig hitBox = CreateDefaultHitBox(track);
                    track.HitBoxes.Add(hitBox);
                    SelectItem(track, SelectedItemKind.HitBox, hitBox);
                    break;
                case TimelineTrackType.Bullet:
                    BulletConfig bullet = CreateDefaultBullet(track);
                    track.Bullets.Add(bullet);
                    SelectItem(track, SelectedItemKind.Bullet, bullet);
                    break;
                case TimelineTrackType.Vfx:
                    TimelineVfxConfig vfx = CreateDefaultVfx(track);
                    track.VfxClips.Add(vfx);
                    SelectItem(track, SelectedItemKind.Vfx, vfx);
                    break;
                case TimelineTrackType.Audio:
                    TimelineAudioConfig audio = CreateDefaultAudio(track);
                    track.AudioClips.Add(audio);
                    SelectItem(track, SelectedItemKind.Audio, audio);
                    break;
                case TimelineTrackType.MetaSkillEvent:
                    TimelineEventConfig metaSkillEvent = CreateDefaultEvent(track);
                    track.MetaSkillEvents.Add(metaSkillEvent);
                    SelectItem(track, SelectedItemKind.Event, metaSkillEvent);
                    break;
            }

            MarkDirty();
        }

        private void AddEventToTrack(TimelineTrackConfig track, TimelineEventType eventType)
        {
            if (track == null || eventType == TimelineEventType.None)
            {
                return;
            }

            TimelineEventConfig metaSkillEvent = new TimelineEventConfig
            {
                DisplayName = GetDefaultEventItemName(eventType, track),
                TriggerTime = _previewTime,
            };
            metaSkillEvent.CreateData(eventType);
            ApplyEventTypeDefaults(metaSkillEvent);
            track.MetaSkillEvents.Add(metaSkillEvent);
            SelectItem(track, SelectedItemKind.Event, metaSkillEvent);
            MarkDirty();
        }

        private HitBoxConfig CreateDefaultHitBox(TimelineTrackConfig track)
        {
            return new HitBoxConfig
            {
                DisplayName = $"攻击盒{track.HitBoxes.Count + 1}",
                TriggerTime = _previewTime,
                Duration = 0.20f,
                SocketSource = SkillSocketSourceType.Weapon,
                AttachPoint = "Attack",
                ShapeArgs = new HitBoxShapeArgs
                {
                    DetectionType = HitBoxDetectionType.Capsule,
                    Size = new Vector3(1f, 0.2f, 0f),
                    BakeCount = 0,
                    HitLayerMask = ~0,
                }
            };
        }

        private BulletConfig CreateDefaultBullet(TimelineTrackConfig track)
        {
            return new BulletConfig
            {
                DisplayName = $"子弹{track.Bullets.Count + 1}",
                TriggerTime = _previewTime,
                Duration = 0.10f,
                SocketSource = SkillSocketSourceType.Weapon,
                AttachPoint = "Emitter",
                SpawnArgs = new BulletSpawnArgs
                {
                    FlightMode = BulletFlightMode.Direct,
                    SpawnCount = 1,
                    Speed = 12f,
                    MaxLifetime = 3f,
                    HitLayerMask = ~0,
                    CollisionRadius = 0.1f,
                },
            };
        }

        private TimelineVfxConfig CreateDefaultVfx(TimelineTrackConfig track)
        {
            return new TimelineVfxConfig
            {
                DisplayName = $"特效{track.VfxClips.Count + 1}",
                TriggerTime = _previewTime,
            };
        }

        private TimelineAudioConfig CreateDefaultAudio(TimelineTrackConfig track)
        {
            return new TimelineAudioConfig
            {
                DisplayName = $"音效{track.AudioClips.Count + 1}",
                TriggerTime = _previewTime,
            };
        }

        private TimelineEventConfig CreateDefaultEvent(TimelineTrackConfig track)
        {
            TimelineEventConfig config = new TimelineEventConfig
            {
                DisplayName = GetDefaultEventItemName(TimelineEventType.SoftLockTarget, track),
                TriggerTime = _previewTime,
            };
            config.CreateData(TimelineEventType.SoftLockTarget);
            ApplyEventTypeDefaults(config);
            return config;
        }

        private string GetDefaultEventItemName(TimelineEventType eventType, TimelineTrackConfig track)
        {
            return $"{eventType}_{track.MetaSkillEvents.Count + 1}";
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
                    config.Duration = Mathf.Max(1f / GetFrameRate(), config.Data.DefaultDuration);
                }
            }
            else
            {
                config.Duration = 0f;
            }
        }

        private static float ResolveEventEndTimeForEditor(TimelineEventConfig config, float currentMaxEndTime)
        {
            if (config == null)
            {
                return currentMaxEndTime;
            }

            if (config.Duration < 0f)
            {
                return Mathf.Max(currentMaxEndTime, config.TriggerTime);
            }

            return config.TriggerTime + config.Duration;
        }

        private void SelectTrack(TimelineTrackConfig track)
        {
            _selectedTrack = track;
            _selectedItem = null;
            _selectedItemKind = track != null ? SelectedItemKind.Track : SelectedItemKind.None;
            CleanupVfxPreview();
            OpenInspectorForSelection();
            Repaint();
        }

        private void SelectItem(TimelineTrackConfig track, SelectedItemKind itemKind, object item)
        {
            _selectedTrack = track;
            _selectedItemKind = itemKind;
            _selectedItem = item;
            if (itemKind == SelectedItemKind.Vfx)
            {
                SamplePreview();
            }
            else
            {
                CleanupVfxPreview();
            }
            OpenInspectorForSelection();
            Repaint();
        }

        private void OpenInspectorForSelection()
        {
            Type windowType = Type.GetType("SkillEditor.Editor.SkillEditorInspectorWindow, Assembly-CSharp-Editor");
            if (windowType == null)
            {
                Debug.LogError("SkillEditorInspectorWindow 未能加载。");
                return;
            }

            if (_selectedItemKind == SelectedItemKind.Interrupt)
            {
                StateInterruptTrackConfig interruptTrack = FindInterruptTrack(_selectedItem as StateInterruptConfig);
                MethodInfo openInterruptMethod = windowType.GetMethod("OpenStateInterruptSelection", BindingFlags.Static | BindingFlags.NonPublic);
                if (openInterruptMethod == null)
                {
                    Debug.LogError("SkillEditorInspectorWindow.OpenStateInterruptSelection 未找到。");
                    return;
                }

                openInterruptMethod.Invoke(null, new object[] { _entry, interruptTrack, _selectedItem as StateInterruptConfig, (Action)MarkDirty });
                return;
            }

            if (_selectedTrack == null)
            {
                return;
            }

            MethodInfo openMethod = windowType.GetMethod("OpenTimelineSelection", BindingFlags.Static | BindingFlags.NonPublic);
            if (openMethod == null)
            {
                Debug.LogError("SkillEditorInspectorWindow.OpenTimelineSelection 未找到。");
                return;
            }

            openMethod.Invoke(null, new object[] { _entry, _selectedTrack, _selectedItem, (Action)MarkDirty });
        }

        private StateInterruptTrackConfig FindInterruptTrack(StateInterruptConfig interrupt)
        {
            if (interrupt == null || _config == null || _config.Timeline == null || _config.Timeline.InterruptTracks == null)
            {
                return null;
            }

            for (int i = 0; i < _config.Timeline.InterruptTracks.Count; i++)
            {
                StateInterruptTrackConfig track = _config.Timeline.InterruptTracks[i];
                if (track != null && track.Interrupts != null && track.Interrupts.Contains(interrupt))
                {
                    return track;
                }
            }

            return null;
        }

        private void HandleTrackContextMenu(Rect rowRect, TimelineTrackConfig track)
        {
            Event current = Event.current;
            if (track == null ||
                current.type != EventType.ContextClick ||
                !rowRect.Contains(current.mousePosition))
            {
                return;
            }

            GenericMenu menu = new GenericMenu();
            switch (track.TrackType)
            {
                case TimelineTrackType.HitBox:
                    menu.AddItem(new GUIContent("创建攻击盒"), false, () => AddItemToTrack(track));
                    break;
                case TimelineTrackType.Bullet:
                    menu.AddItem(new GUIContent("创建子弹"), false, () => AddItemToTrack(track));
                    break;
                case TimelineTrackType.Vfx:
                    menu.AddItem(new GUIContent("创建特效"), false, () => AddItemToTrack(track));
                    break;
                case TimelineTrackType.Audio:
                    menu.AddItem(new GUIContent("创建音效"), false, () => AddItemToTrack(track));
                    break;
                case TimelineTrackType.MetaSkillEvent:
                    AddEventCreateMenu(menu, track);
                    break;
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("删除轨道"), false, () => DeleteTrack(track));
            menu.ShowAsContext();
            current.Use();
        }

        private void HandleInterruptTrackContextMenu(Rect rowRect, StateInterruptTrackConfig track)
        {
            Event current = Event.current;
            if (track == null ||
                current.type != EventType.ContextClick ||
                !rowRect.Contains(current.mousePosition))
            {
                return;
            }

            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("创建打断"), false, () => AddInterruptToTrack(track));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("删除轨道"), false, () => DeleteInterruptTrack(track));
            menu.ShowAsContext();
            current.Use();
        }

        private void AddInterruptToTrack(StateInterruptTrackConfig track)
        {
            if (track == null)
            {
                return;
            }

            track.Interrupts ??= new List<StateInterruptConfig>();
            StateInterruptConfig interrupt = new StateInterruptConfig
            {
                TriggerTime = _previewTime,
                Duration = 0.15f,
                ExecuteTime = 0f,
                IsEnabled = true,
            };

            track.Interrupts.Add(interrupt);
            SelectItem(null, SelectedItemKind.Interrupt, interrupt);
            MarkDirty();
        }

        private void DeleteInterruptTrack(StateInterruptTrackConfig track)
        {
            if (track == null || _config == null || _config.Timeline == null || _config.Timeline.InterruptTracks == null)
            {
                return;
            }

            if (_selectedItemKind == SelectedItemKind.Interrupt && _selectedItem is StateInterruptConfig interrupt && track.Interrupts != null && track.Interrupts.Contains(interrupt))
            {
                _selectedItem = null;
                _selectedItemKind = SelectedItemKind.None;
            }

            _config.Timeline.InterruptTracks.Remove(track);
            MarkDirty();
        }

        private void AddEventCreateMenu(GenericMenu menu, TimelineTrackConfig track)
        {
            Array values = Enum.GetValues(typeof(TimelineEventType));
            for (int i = 0; i < values.Length; i++)
            {
                TimelineEventType eventType = (TimelineEventType)values.GetValue(i);
                if (eventType == TimelineEventType.None)
                {
                    continue;
                }

                TimelineEventType capturedType = eventType;
                menu.AddItem(new GUIContent($"创建事件/{capturedType}"), false, () => AddEventToTrack(track, capturedType));
            }
        }

        private void MarkDirty()
        {
            _isDirty = true;
            SkillResourceRepository.MarkDirty(_entry);
            _onOuterModified?.Invoke();
            if (CanPreview())
            {
                SamplePreview();
            }
            else
            {
                SceneView.RepaintAll();
            }
            Repaint();
        }

        private string BuildEffectSummary(SkillEffectConfig effectConfig)
        {
            if (effectConfig == null || effectConfig.Nodes == null || effectConfig.Nodes.Count == 0 || string.IsNullOrEmpty(effectConfig.RootNodeId))
            {
                return "空效果树";
            }

            return $"Root={effectConfig.RootNodeId}  Nodes={effectConfig.Nodes.Count}";
        }

        private void OpenEffectEditor(SkillEffectConfig effectConfig, string targetTitle)
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

            openMethod.Invoke(null, new object[] { _entry, effectConfig, targetTitle, (Action)MarkDirty });
        }

        private void BakeHitBoxPreview(HitBoxConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.HitBoxId))
            {
                return;
            }

            GameObject sceneInstance = SkillPreviewSceneInstanceUtility.GetCurrentInstance();
            if (sceneInstance == null || _clip == null)
            {
                return;
            }

            int targetBakeCount = GetTargetBakeCount(config);

            List<HitBoxBakedPart> bakedParts = new List<HitBoxBakedPart>(targetBakeCount);
            float originalPreviewTime = _previewTime;
            try
            {
                for (int i = 0; i < targetBakeCount; i++)
                {
                    float bakeTime = GetBakeSampleTime(config, i, targetBakeCount);
                    SampleSceneInstance(sceneInstance, bakeTime);
                    if (!TryResolvePreviewAnchor(sceneInstance, config.SocketSource, config.AttachPoint, out Transform anchorTransform))
                    {
                        continue;
                    }

                    Vector3 worldStart = anchorTransform.TransformPoint(config.ShapeArgs.OffsetPosition);
                    Quaternion worldRotation = anchorTransform.rotation * Quaternion.Euler(config.ShapeArgs.OffsetRotation);
                    Vector3 worldDirection = worldRotation * Vector3.forward;
                    Transform unitRoot = sceneInstance.transform;
                    bakedParts.Add(new HitBoxBakedPart
                    {
                        StartPos = unitRoot.InverseTransformPoint(worldStart),
                        Direction = unitRoot.InverseTransformDirection(worldDirection),
                        TriggerTime = Mathf.Max(0f, bakeTime - config.TriggerTime),
                    });
                }
            }
            finally
            {
                _previewTime = originalPreviewTime;
                RestorePreviewSample(sceneInstance);
            }

            config.ShapeArgs.BakedParts = bakedParts;
            MarkDirty();
            if (CanPreview())
            {
                SamplePreview();
            }
            else
            {
                SceneView.RepaintAll();
            }

            Repaint();
        }

        private int GetTargetBakeCount(HitBoxConfig config)
        {
            if (config == null)
            {
                return 0;
            }

            if (config.ShapeArgs.BakeCount > 0)
            {
                return config.ShapeArgs.BakeCount;
            }

            float duration = Mathf.Max(0f, config.Duration);
            if (duration <= 0f)
            {
                return 0;
            }

            // Align with AsiActionEditor's bake behavior: auto-bake should sample the
            // whole active duration, not depend on the current editor zoom level.
            int frameRate = GetFrameRate();
            int targetBakeCount = Mathf.RoundToInt(duration * frameRate);
            return Mathf.Max(2, targetBakeCount + 1);
        }

        internal static StateTimelineEditorWindow GetActiveInstance()
        {
            return _activeWindow;
        }

        internal void BakeHitBoxPreviewFromInspector(HitBoxConfig config)
        {
            BakeHitBoxPreview(config);
        }

        internal bool IsHitBoxPreviewBakedFromInspector(HitBoxConfig config)
        {
            return IsHitBoxPreviewBaked(config);
        }

        internal bool MatchesEntry(string jsonAssetPath)
        {
            return _entry != null && string.Equals(_entry.JsonAssetPath, jsonAssetPath, StringComparison.OrdinalIgnoreCase);
        }

        internal SkillResourceFileEntry GetBoundEntry()
        {
            return _entry;
        }

        internal void MarkSavedFromOuter()
        {
            _isDirty = false;
            Repaint();
        }

        public void PrepareForOuterSave()
        {
            if (_entry == null || _config == null)
            {
                return;
            }

            SanitizeTimelineData();
        }

        private bool IsHitBoxPreviewBaked(HitBoxConfig config)
        {
            return config != null &&
                   config.ShapeArgs != null &&
                   config.ShapeArgs.BakedParts != null &&
                   config.ShapeArgs.BakedParts.Count > 0;
        }

        private AnimationClip LoadClip()
        {
            return _config == null || string.IsNullOrEmpty(_config.AnimationClipPath)
                ? null
                : SkillAnimationReferenceUtility.LoadClip(_config.AnimationClipPath);
        }

        private bool CanPreview()
        {
            return _clip != null && SkillPreviewSceneInstanceUtility.GetCurrentInstance() != null;
        }

        private float GetDisplayDuration()
        {
            return GetExecutionDisplayDuration();
        }

        private float GetExecutionDisplayDuration()
        {
            return Mathf.Max(0f, GetExecutionClipVisualDuration(), _config != null && _config.Timeline != null ? _config.Timeline.Duration : 0f);
        }

        private float GetPreviewFlowDuration()
        {
            return GetExecutionDisplayDuration();
        }

        private float GetExecutionClipVisualDuration()
        {
            if (_clip == null)
            {
                return 0f;
            }

            TimelineAnimationConfig animationConfig = _config != null && _config.Timeline != null ? _config.Timeline.Animation : null;
            return Mathf.Max(0f, _clip.length - GetAnimationStartTime(animationConfig, _clip));
        }

        private float GetSuggestedTimelineDuration()
        {
            float duration = GetExecutionClipVisualDuration();
            if (_config == null || _config.Timeline == null || _config.Timeline.Tracks == null)
            {
                return duration;
            }

            for (int i = 0; i < _config.Timeline.Tracks.Count; i++)
            {
                TimelineTrackConfig track = _config.Timeline.Tracks[i];
                if (track == null)
                {
                    continue;
                }

                duration = Mathf.Max(duration, GetMaxEndTime(track.HitBoxes));
                duration = Mathf.Max(duration, GetMaxEndTime(track.Bullets));
                duration = Mathf.Max(duration, GetMaxEndTime(track.VfxClips));
                duration = Mathf.Max(duration, GetMaxEndTime(track.AudioClips));
                duration = Mathf.Max(duration, GetMaxEndTime(track.MetaSkillEvents));
            }

            if (_config.Timeline.InterruptTracks != null && _config.Timeline.InterruptTracks.Count > 0)
            {
                for (int trackIndex = 0; trackIndex < _config.Timeline.InterruptTracks.Count; trackIndex++)
                {
                    StateInterruptTrackConfig track = _config.Timeline.InterruptTracks[trackIndex];
                    if (track == null || track.Interrupts == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < track.Interrupts.Count; i++)
                    {
                        StateInterruptConfig interrupt = track.Interrupts[i];
                        if (interrupt == null)
                        {
                            continue;
                        }

                        duration = Mathf.Max(duration, interrupt.TriggerTime + Mathf.Max(0f, interrupt.Duration));
                    }
                }
            }
            else if (_config.Timeline.Interrupts != null)
            {
                for (int i = 0; i < _config.Timeline.Interrupts.Count; i++)
                {
                    StateInterruptConfig interrupt = _config.Timeline.Interrupts[i];
                    if (interrupt != null)
                    {
                        duration = Mathf.Max(duration, interrupt.TriggerTime + Mathf.Max(0f, interrupt.Duration));
                    }
                }
            }

            return duration;
        }

        private bool NeedsDurationSync()
        {
            if (_config == null || _config.Timeline == null)
            {
                return false;
            }

            return Mathf.Abs(_config.Timeline.Duration - GetSuggestedTimelineDuration()) > 0.01f;
        }

        private float GetTimelineContentHeight()
        {
            float height = TimeHeaderHeight + FlowPreviewRowHeight + TrackRowSpacing + AnimationRowHeight + TrackRowSpacing + TrackGroupHeight + TrackRowSpacing;
            if (_interruptTracksExpanded)
            {
                height += GetInterruptTracks().Count * (TrackRowHeight + TrackRowSpacing);
            }

            for (int i = 0; i < TrackGroups.Length; i++)
            {
                height += TrackGroupHeight + TrackRowSpacing;
                if (_groupExpanded[TrackGroups[i].TrackType])
                {
                    height += GetTracksForType(TrackGroups[i].TrackType).Count * (TrackRowHeight + TrackRowSpacing);
                }
            }

            return height;
        }

        private List<StateInterruptTrackConfig> GetInterruptTracks()
        {
            if (_config == null || _config.Timeline == null)
            {
                return new List<StateInterruptTrackConfig>();
            }

            _config.Timeline.InterruptTracks ??= new List<StateInterruptTrackConfig>();
            return _config.Timeline.InterruptTracks;
        }

        private void MigrateLegacyInterruptsToTrack()
        {
            if (_config == null || _config.Timeline == null || _config.Timeline.Interrupts == null || _config.Timeline.Interrupts.Count == 0)
            {
                return;
            }

            _config.Timeline.InterruptTracks ??= new List<StateInterruptTrackConfig>();
            if (_config.Timeline.InterruptTracks.Count > 0)
            {
                return;
            }

            StateInterruptTrackConfig track = new StateInterruptTrackConfig
            {
                TrackId = Guid.NewGuid().ToString("N"),
                DisplayName = "打断轨道1",
                IsEnabled = true,
                Interrupts = new List<StateInterruptConfig>(_config.Timeline.Interrupts),
            };
            _config.Timeline.InterruptTracks.Add(track);
            _config.Timeline.Interrupts.Clear();
        }

        private bool ContainsInterrupt(StateInterruptConfig interrupt)
        {
            if (interrupt == null || _config == null || _config.Timeline == null)
            {
                return false;
            }

            if (_config.Timeline.InterruptTracks != null)
            {
                for (int trackIndex = 0; trackIndex < _config.Timeline.InterruptTracks.Count; trackIndex++)
                {
                    StateInterruptTrackConfig track = _config.Timeline.InterruptTracks[trackIndex];
                    if (track != null && track.Interrupts != null && track.Interrupts.Contains(interrupt))
                    {
                        return true;
                    }
                }
            }

            return _config.Timeline.Interrupts != null && _config.Timeline.Interrupts.Contains(interrupt);
        }

        private void RemoveInterrupt(StateInterruptConfig interrupt)
        {
            if (interrupt == null || _config == null || _config.Timeline == null)
            {
                return;
            }

            if (_config.Timeline.InterruptTracks != null)
            {
                for (int trackIndex = 0; trackIndex < _config.Timeline.InterruptTracks.Count; trackIndex++)
                {
                    StateInterruptTrackConfig track = _config.Timeline.InterruptTracks[trackIndex];
                    if (track != null && track.Interrupts != null && track.Interrupts.Remove(interrupt))
                    {
                        return;
                    }
                }
            }

            _config.Timeline.Interrupts?.Remove(interrupt);
        }

        private string BuildTimeLabel()
        {
            int frame = Mathf.RoundToInt(_previewTime * GetFrameRate());
            return $"{_previewTime:F3}({frame}) {GetPreviewPhaseLabel()}";
        }

        private string BuildStatusText()
        {
            if (_config == null)
            {
                return "当前没有绑定 State。";
            }

            string dirtyText = _isDirty ? "存在未保存修改，请使用最外层窗口的保存按钮。" : "已与磁盘同步。";

            if (_clip == null)
            {
                return $"{dirtyText} 当前 State 未配置有效动画，Timeline 只能编辑配置。";
            }

            if (SkillPreviewSceneInstanceUtility.GetCurrentInstance() == null)
            {
                return $"{dirtyText} 当前 Scene 里没有预览单位复制体，请先在预览单位里 Apply。";
            }

            if (NeedsDurationSync())
            {
                return $"{dirtyText} Timeline.Duration 与动画/轨道条目范围不一致，建议点击“同步时长”。";
            }

            return $"{dirtyText} 顶部 Flow 预览条会预览当前 State 动画，下面的 Timeline 编辑 OnUpdate 轨道。";
        }

        private bool HasPreviewWarning()
        {
            return _config == null || _clip == null || SkillPreviewSceneInstanceUtility.GetCurrentInstance() == null || NeedsDurationSync();
        }

        private string BuildFlowHeaderLabel()
        {
            return $"State Flow  {GetPreviewPhaseLabel()}";
        }

        private string GetPreviewPhaseLabel()
        {
            return "State";
        }

        private float GetExecutePreviewTime()
        {
            return Mathf.Clamp(_previewTime, 0f, GetExecutionDisplayDuration());
        }

        private void DrawFlowPreviewPlayhead(Rect rect, float duration)
        {
            float x = TimeToPixel(Mathf.Clamp(_previewTime, 0f, duration), rect, duration);
            EditorGUI.DrawRect(new Rect(x, rect.y, 2f, rect.height), PlayheadColor);
        }

        private void HandleFlowPreviewInput(Rect rect)
        {
            if (!CanPreview())
            {
                return;
            }

            Event current = Event.current;
            if ((current.type != EventType.MouseDown && current.type != EventType.MouseDrag) ||
                current.button != 0 ||
                !rect.Contains(current.mousePosition))
            {
                return;
            }

            float duration = GetPreviewFlowDuration();
            if (duration <= 0f)
            {
                return;
            }

            _previewTime = Mathf.Clamp(PixelToTime(current.mousePosition.x, rect, duration), 0f, duration);
            _isPlaying = false;
            SamplePreview();
            current.Use();
            Repaint();
        }

        private AnimationClip GetActivePreviewClip()
        {
            return _clip;
        }

        private float GetActivePreviewSampleTime()
        {
            TimelineAnimationConfig executeAnimationConfig = _config != null && _config.Timeline != null ? _config.Timeline.Animation : null;
            return GetAnimationSampleTime(executeAnimationConfig, _clip, GetExecutePreviewTime());
        }

        private static float GetAnimationSampleTime(TimelineAnimationConfig animationConfig, AnimationClip clip, float localTime)
        {
            if (clip == null)
            {
                return 0f;
            }

            float startTime = GetAnimationStartTime(animationConfig, clip);
            return Mathf.Clamp(startTime + Mathf.Max(0f, localTime), 0f, clip.length);
        }

        private static float GetAnimationStartTime(TimelineAnimationConfig animationConfig, AnimationClip clip)
        {
            if (animationConfig == null || clip == null)
            {
                return 0f;
            }

            float startTime = Mathf.Max(0f, animationConfig.StartTime);
            if (animationConfig.StartTimeUnit == AnimationStartTimeUnit.NormalizedTime)
            {
                startTime *= Mathf.Max(0f, clip.length);
            }

            return Mathf.Clamp(startTime, 0f, Mathf.Max(0f, clip.length));
        }

        private List<TimelineTrackConfig> GetTracksForType(TimelineTrackType type)
        {
            List<TimelineTrackConfig> tracks = new List<TimelineTrackConfig>();
            if (_config == null || _config.Timeline == null || _config.Timeline.Tracks == null)
            {
                return tracks;
            }

            for (int i = 0; i < _config.Timeline.Tracks.Count; i++)
            {
                TimelineTrackConfig track = _config.Timeline.Tracks[i];
                if (track != null && track.TrackType == type)
                {
                    tracks.Add(track);
                }
            }

            return tracks;
        }

        private static string GetTrackGroupLabel(TimelineTrackType trackType)
        {
            switch (trackType)
            {
                case TimelineTrackType.HitBox:
                    return "攻击盒";
                case TimelineTrackType.Bullet:
                    return "子弹";
                case TimelineTrackType.Vfx:
                    return "特效";
                case TimelineTrackType.Audio:
                    return "音效";
                case TimelineTrackType.MetaSkillEvent:
                    return "事件";
                default:
                    return trackType.ToString();
            }
        }

        private static string GetDisplayName(HitBoxConfig config)
        {
            return string.IsNullOrEmpty(config.DisplayName) ? config.HitBoxId : config.DisplayName;
        }

        private static string GetDisplayName(BulletConfig config)
        {
            return string.IsNullOrEmpty(config.DisplayName) ? config.BulletId : config.DisplayName;
        }

        private static string GetDisplayName(TimelineEventConfig config)
        {
            if (!string.IsNullOrEmpty(config.DisplayName))
            {
                return config.DisplayName;
            }

            return config.EventType != TimelineEventType.None ? config.EventType.ToString() : config.EventId;
        }

        private static string GetDisplayName(TimelineVfxConfig config)
        {
            return string.IsNullOrEmpty(config.DisplayName) ? config.VfxId : config.DisplayName;
        }

        private static string GetDisplayName(TimelineAudioConfig config)
        {
            return string.IsNullOrEmpty(config.DisplayName) ? config.AudioId : config.DisplayName;
        }

        private static string GetDisplayName(StateInterruptConfig config)
        {
            if (config == null)
            {
                return string.Empty;
            }

            return string.IsNullOrEmpty(config.TargetStateId) ? "打断" : config.TargetStateId;
        }

        private static float GetTriggerTime(HitBoxConfig config)
        {
            return config.TriggerTime;
        }

        private static float GetTriggerTime(BulletConfig config)
        {
            return config.TriggerTime;
        }

        private static float GetTriggerTime(TimelineEventConfig config)
        {
            return config.TriggerTime;
        }

        private static float GetTriggerTime(TimelineVfxConfig config)
        {
            return config.TriggerTime;
        }

        private static float GetTriggerTime(TimelineAudioConfig config)
        {
            return config.TriggerTime;
        }

        private static float GetTriggerTime(StateInterruptConfig config)
        {
            return config.TriggerTime;
        }

        private static float GetDuration(HitBoxConfig config)
        {
            return config.Duration;
        }

        private static float GetDuration(BulletConfig config)
        {
            return config.Duration;
        }

        private static float GetDuration(TimelineEventConfig config)
        {
            return config.Duration;
        }

        private static float GetDuration(TimelineVfxConfig config)
        {
            return config.Duration;
        }

        private static float GetDuration(TimelineAudioConfig config)
        {
            return 0f;
        }

        private float GetEditorVisualDuration(StateInterruptConfig config)
        {
            if (config == null)
            {
                return 0f;
            }

            if (config.Duration < 0f)
            {
                return Mathf.Max(1f / GetFrameRate(), GetExecutionDisplayDuration() - config.TriggerTime);
            }

            return config.Duration;
        }

        private static void SetTriggerTime(StateInterruptConfig config, float value)
        {
            config.TriggerTime = value;
        }

        private static void SetDuration(StateInterruptConfig config, float value)
        {
            config.Duration = value;
        }

        private float TimeToPixel(float time, Rect rect, float duration)
        {
            if (duration <= 0f)
            {
                return rect.x;
            }

            float visibleStart = GetVisibleStartTime(duration);
            float visibleDuration = GetVisibleDuration(duration);
            float normalized = Mathf.InverseLerp(visibleStart, visibleStart + visibleDuration, time);
            return Mathf.Lerp(rect.x, rect.xMax, normalized);
        }

        private float PixelToTime(float pixelX, Rect rect, float duration)
        {
            float visibleStart = GetVisibleStartTime(duration);
            float visibleDuration = GetVisibleDuration(duration);
            float normalized = Mathf.InverseLerp(rect.x, rect.xMax, pixelX);
            return visibleStart + visibleDuration * normalized;
        }

        private float GetVisibleStartTime(float duration)
        {
            return duration * Mathf.Clamp01(_rangeMin);
        }

        private float GetVisibleDuration(float duration)
        {
            float clampedMax = Mathf.Max(_rangeMin + 0.01f, _rangeMax);
            return duration * Mathf.Clamp01(clampedMax - _rangeMin);
        }

        private static int GetFrameRate()
        {
            return 60;
        }

        internal static int BoxDrawType
        {
            get => PlayerPrefs.GetInt("SkillEditor.HitBox.BoxDrawType", 0);
            set => PlayerPrefs.SetInt("SkillEditor.HitBox.BoxDrawType", Mathf.Clamp(value, 0, HitBoxDrawModes.Length - 1));
        }

        internal static int BakerBoxLife
        {
            get => PlayerPrefs.GetInt("SkillEditor.HitBox.BakerBoxLife", 0);
            set => PlayerPrefs.SetInt("SkillEditor.HitBox.BakerBoxLife", Mathf.Max(0, value));
        }

        internal static string[] GetHitBoxDrawModeLabels()
        {
            return HitBoxDrawModes;
        }

        private static int GetMajorFrameStep(float width, float visibleDuration)
        {
            int visibleFrames = Mathf.Max(1, Mathf.RoundToInt(visibleDuration * GetFrameRate()));
            int targetCount = Mathf.Max(1, Mathf.RoundToInt(width / MajorTickTargetSpacing));
            int rawStep = Mathf.Max(1, Mathf.CeilToInt(visibleFrames / (float)targetCount));

            int[] preferredSteps = { 5, 10, 20, 30, 60, 120, 240, 300, 600 };
            for (int i = 0; i < preferredSteps.Length; i++)
            {
                if (preferredSteps[i] >= rawStep)
                {
                    return preferredSteps[i];
                }
            }

            return preferredSteps[preferredSteps.Length - 1];
        }

        private static float GetMaxEndTime(List<HitBoxConfig> items)
        {
            float max = 0f;
            if (items == null)
            {
                return max;
            }

            for (int i = 0; i < items.Count; i++)
            {
                HitBoxConfig item = items[i];
                if (item == null)
                {
                    continue;
                }

                float endTime = item.Duration < 0f ? item.TriggerTime : item.TriggerTime + item.Duration;
                max = Mathf.Max(max, endTime);
            }

            return max;
        }

        private static float GetMaxEndTime(List<BulletConfig> items)
        {
            float max = 0f;
            if (items == null)
            {
                return max;
            }

            for (int i = 0; i < items.Count; i++)
            {
                BulletConfig item = items[i];
                if (item == null)
                {
                    continue;
                }

                max = Mathf.Max(max, item.TriggerTime + item.Duration);
            }

            return max;
        }

        private static float GetMaxEndTime(List<TimelineVfxConfig> items)
        {
            float max = 0f;
            if (items == null)
            {
                return max;
            }

            for (int i = 0; i < items.Count; i++)
            {
                TimelineVfxConfig item = items[i];
                if (item == null)
                {
                    continue;
                }

                float duration = item.Mode == TimelineVfxMode.Controlled ? Mathf.Max(0f, item.Duration) : 0f;
                max = Mathf.Max(max, item.TriggerTime + duration);
            }

            return max;
        }

        private static float GetMaxEndTime(List<TimelineAudioConfig> items)
        {
            float max = 0f;
            if (items == null)
            {
                return max;
            }

            for (int i = 0; i < items.Count; i++)
            {
                TimelineAudioConfig item = items[i];
                if (item != null)
                {
                    max = Mathf.Max(max, item.TriggerTime);
                }
            }

            return max;
        }

        private static float GetMaxEndTime(List<TimelineEventConfig> items)
        {
            float max = 0f;
            if (items == null)
            {
                return max;
            }

            for (int i = 0; i < items.Count; i++)
            {
                TimelineEventConfig item = items[i];
                if (item == null)
                {
                    continue;
                }

                max = Mathf.Max(max, item.TriggerTime + item.Duration);
            }

            return max;
        }
    }
}
