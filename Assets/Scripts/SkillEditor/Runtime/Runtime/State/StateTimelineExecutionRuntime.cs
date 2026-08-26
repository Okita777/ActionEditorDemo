using System;
using System.Collections.Generic;
using SkillEditor.Preview;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    /// <summary>
    /// 状态时间线执行运行时。
    /// 负责按状态已运行时间触发技能事件、命中盒和子弹，并维护持续事件与持续命中盒的生命周期。
    /// </summary>
    public sealed class StateTimelineExecutionRuntime
    {
        private readonly StateTimelineConfig _config;
        private readonly SkillContext _context;
        private readonly Dictionary<string, TimelineEventRuntimeBase> _activeEventRuntimes = new Dictionary<string, TimelineEventRuntimeBase>();
        private readonly Dictionary<string, TimelineHitBoxRuntime> _activeHitBoxRuntimes = new Dictionary<string, TimelineHitBoxRuntime>();
        private readonly Dictionary<string, TimelineVfxRuntime> _activeVfxRuntimes = new Dictionary<string, TimelineVfxRuntime>();
        private readonly HashSet<string> _triggeredEventIds = new HashSet<string>();
        private readonly HashSet<string> _triggeredHitBoxIds = new HashSet<string>();
        private readonly HashSet<string> _triggeredBulletIds = new HashSet<string>();
        private readonly HashSet<string> _triggeredVfxIds = new HashSet<string>();
        private readonly HashSet<string> _triggeredAudioIds = new HashSet<string>();
        private float _elapsedTime;

        /// <summary>
        /// 创建状态时间线执行运行时。
        /// </summary>
        /// <param name="config">状态时间线配置。</param>
        /// <param name="context">技能运行上下文；为空时会创建一个空上下文兜底。</param>
        public StateTimelineExecutionRuntime(StateTimelineConfig config, SkillContext context)
        {
            _config = config;
            _context = context ?? new SkillContext();
        }

        /// <summary>
        /// 当前时间线已经运行的时间，单位为秒。
        /// </summary>
        public float ElapsedTime => _elapsedTime;

        /// <summary>
        /// 时间线是否已经达到配置的结束时间。
        /// </summary>
        public bool IsCompleted => _config != null && _config.Duration > 0f && _elapsedTime >= _config.Duration;

        /// <summary>
        /// 结束当前时间线，停止所有仍在运行的持续事件和命中盒。
        /// </summary>
        /// <param name="interrupted">是否以中断方式结束。</param>
        public void End(bool interrupted)
        {
            if (_config != null && _config.Tracks != null)
            {
                for (int i = 0; i < _config.Tracks.Count; i++)
                {
                    TimelineTrackConfig track = _config.Tracks[i];
                    if (track == null)
                    {
                        continue;
                    }

                    StopActiveEvents(track.MetaSkillEvents, interrupted);
                    StopActiveHitBoxes(track.HitBoxes, interrupted);
                    StopActiveVfxClips(track.VfxClips, interrupted);
                }
            }

            DisposeAllActiveEventRuntimes();
            DisposeAllActiveHitBoxRuntimes();
            DisposeAllActiveVfxRuntimes();
        }

        /// <summary>
        /// 重置时间线运行状态，清空触发记录并释放活动运行时。
        /// </summary>
        public void Reset()
        {
            DisposeAllActiveEventRuntimes();
            DisposeAllActiveHitBoxRuntimes();
            DisposeAllActiveVfxRuntimes();
            _elapsedTime = 0f;
            _triggeredEventIds.Clear();
            _triggeredHitBoxIds.Clear();
            _triggeredBulletIds.Clear();
            _triggeredVfxIds.Clear();
            _triggeredAudioIds.Clear();
        }

        /// <summary>
        /// 退出技能执行段。
        /// 会直接停止当前执行段中的活动事件和命中盒，不再保留恢复段延续逻辑。
        /// </summary>
        public void ExitExecutionPhase()
        {
            if (_config == null || _config.Tracks == null)
            {
                return;
            }

            // [AICode] recovery carryover 已移除，执行段退出与正常结束统一走同一套时间线收口。
            End(false);
        }

        /// <summary>
        /// 推进时间线一帧，依次检查各轨道上的事件、命中盒和子弹。
        /// </summary>
        /// <param name="deltaTime">本帧推进时间。</param>
        public void Tick(float deltaTime)
        {
            if (_config == null)
            {
                return;
            }

            float previousTime = _elapsedTime;
            _elapsedTime += deltaTime;
            _context.DebugTimelineTime = _elapsedTime;

            if (_config.Tracks == null || _config.Tracks.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _config.Tracks.Count; i++)
            {
                TimelineTrackConfig track = _config.Tracks[i];
                if (track == null)
                {
                    continue;
                }

                if (!track.IsEnabled)
                {
                    StopActiveEvents(track.MetaSkillEvents, true);
                    StopActiveHitBoxes(track.HitBoxes, true);
                    StopActiveVfxClips(track.VfxClips, true);
                }
            }

            // 单位事件必须先进入监听状态，攻击盒随后才能发布本帧命中。
            // 否则当 HitBox 轨位于事件轨之前时，窗口首帧命中会被监听器漏掉。
            for (int i = 0; i < _config.Tracks.Count; i++)
            {
                TimelineTrackConfig track = _config.Tracks[i];
                if (track != null && track.IsEnabled)
                {
                    TriggerEvents(track.MetaSkillEvents, previousTime, deltaTime);
                    UpdateVfxClips(track.VfxClips, previousTime, deltaTime);
                    TriggerAudioClips(track.AudioClips);
                }
            }

            for (int i = 0; i < _config.Tracks.Count; i++)
            {
                TimelineTrackConfig track = _config.Tracks[i];
                if (track == null || !track.IsEnabled)
                {
                    continue;
                }

                TriggerHitBoxes(track.HitBoxes, previousTime, deltaTime);
                TriggerBullets(track.Bullets);
            }
        }

        private void TriggerAudioClips(List<TimelineAudioConfig> clips)
        {
            if (clips == null)
            {
                return;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                TimelineAudioConfig config = clips[i];
                if (config == null || !config.IsEnabled || string.IsNullOrEmpty(config.AudioId) ||
                    _triggeredAudioIds.Contains(config.AudioId))
                {
                    continue;
                }

                float triggerTime = Mathf.Max(0f, config.TriggerTime);
                if (triggerTime > _elapsedTime)
                {
                    continue;
                }

                _triggeredAudioIds.Add(config.AudioId);
                PlayAudioClip(config);
            }
        }

        private void PlayAudioClip(TimelineAudioConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.AudioClipPath))
            {
                return;
            }

            AudioClip clip = FeedbackAssetRuntimeCatalog.LoadAudioClip(config.AudioClipPath);
            if (clip == null)
            {
                EmitTimelineTrace("TimelineAudio.ResourceMissing", config.AudioId, config.TriggerTime);
                return;
            }

            Transform socket = null;
            Vector3 position = Vector3.zero;
            AudioPlaySpace playSpace = config.Space;
            if (playSpace != AudioPlaySpace.TwoD)
            {
                if (!TryResolveSocketTransform(_context, config.SocketSource, config.AttachPoint, out socket))
                {
                    EmitTimelineTrace("TimelineAudio.SocketMissing", config.AudioId, config.TriggerTime);
                    return;
                }

                position = socket.position;
                if (playSpace == AudioPlaySpace.FollowTarget && socket == null)
                {
                    playSpace = AudioPlaySpace.World;
                }
            }

            UnityEngine.Audio.AudioMixerGroup mixer = FeedbackAssetRuntimeCatalog.LoadMixerGroup(
                config.AudioMixerPath,
                config.MixerGroupName);
            float minDistance = Mathf.Max(0.01f, config.MinDistance);
            float maxDistance = Mathf.Max(minDistance, config.MaxDistance);
            IAudioService service = _context.AudioService ?? GameFeedbackServiceHost.Instance.Audio;
            _context.AudioService = service;
            service?.Play(new AudioPlayArgs(
                clip,
                mixer,
                playSpace,
                position,
                playSpace == AudioPlaySpace.FollowTarget ? socket : null,
                Mathf.Clamp01(config.Volume),
                Mathf.Clamp(config.Pitch, 0.01f, 3f),
                Mathf.Clamp01(config.SpatialBlend),
                minDistance,
                maxDistance));
            EmitTimelineTrace("TimelineAudio.Triggered", config.AudioId, config.TriggerTime);
        }

        private void UpdateVfxClips(List<TimelineVfxConfig> clips, float previousTime, float deltaTime)
        {
            if (clips == null)
            {
                return;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                TimelineVfxConfig config = clips[i];
                if (config == null || !config.IsEnabled || string.IsNullOrEmpty(config.VfxId))
                {
                    StopActiveVfx(config, true);
                    continue;
                }

                if (config.Mode == TimelineVfxMode.OneShot)
                {
                    if (!_triggeredVfxIds.Contains(config.VfxId) && config.TriggerTime <= _elapsedTime)
                    {
                        _triggeredVfxIds.Add(config.VfxId);
                        TimelineVfxRuntime runtime = new TimelineVfxRuntime(config, _context);
                        runtime.Begin();
                        EmitTimelineTrace("VFX", config.VfxId, config.TriggerTime);
                    }
                    continue;
                }

                float startTime = Mathf.Max(0f, config.TriggerTime);
                float endTime = startTime + Mathf.Max(1f / 60f, config.Duration);
                if (_elapsedTime > endTime)
                {
                    StopActiveVfx(config, false);
                    continue;
                }

                bool overlapsWindow = previousTime <= endTime && _elapsedTime >= startTime;
                if (!overlapsWindow)
                {
                    continue;
                }

                if (!_activeVfxRuntimes.TryGetValue(config.VfxId, out TimelineVfxRuntime activeRuntime))
                {
                    activeRuntime = new TimelineVfxRuntime(config, _context);
                    activeRuntime.Begin();
                    _activeVfxRuntimes.Add(config.VfxId, activeRuntime);
                    EmitTimelineTrace("VFX.Begin", config.VfxId, startTime);
                }

                activeRuntime.Tick(Mathf.Max(0f, deltaTime));
            }
        }

        private void StopActiveVfx(TimelineVfxConfig config, bool interrupted)
        {
            if (config == null || string.IsNullOrEmpty(config.VfxId) ||
                !_activeVfxRuntimes.TryGetValue(config.VfxId, out TimelineVfxRuntime runtime))
            {
                return;
            }

            runtime.End(interrupted);
            _activeVfxRuntimes.Remove(config.VfxId);
            EmitTimelineTrace("VFX.End", config.VfxId, _elapsedTime);
        }

        private void StopActiveVfxClips(List<TimelineVfxConfig> clips, bool interrupted)
        {
            if (clips == null)
            {
                return;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                StopActiveVfx(clips[i], interrupted);
            }
        }

        private void DisposeAllActiveVfxRuntimes()
        {
            foreach (KeyValuePair<string, TimelineVfxRuntime> pair in _activeVfxRuntimes)
            {
                pair.Value?.End(true);
            }
            _activeVfxRuntimes.Clear();
        }

        /// <summary>
        /// 检查并触发一组时间线事件。
        /// 支持持续时间的事件会进入 Begin/Tick/End 生命周期，瞬时事件只触发一次。
        /// </summary>
        /// <param name="events">事件配置列表。</param>
        /// <param name="previousTime">上一帧时间线时间。</param>
        /// <param name="deltaTime">本帧推进时间。</param>
        private void TriggerEvents(List<TimelineEventConfig> events, float previousTime, float deltaTime)
        {
            if (events == null)
            {
                return;
            }

            for (int i = 0; i < events.Count; i++)
            {
                TimelineEventConfig config = events[i];
                if (config == null)
                {
                    continue;
                }

                if (!config.IsEnabled || string.IsNullOrEmpty(config.EventId) || config.Data == null)
                {
                    StopActiveEvent(config, true);
                    continue;
                }

                if (SupportsDuration(config) && !Mathf.Approximately(config.Duration, 0f))
                {
                    UpdateDurationEvent(config, previousTime, deltaTime);
                    continue;
                }
                
                //okita:这啥意思，_triggeredEventIds一直是空，而且这个和metaskillevent有什么关系
                TriggerOnce(
                    config,
                    _triggeredEventIds,
                    evt => evt.EventId,
                    evt => evt.IsEnabled,
                    evt => evt.TriggerTime,
                    TriggerMetaSkillEvent);
            }
        }

        /// <summary>
        /// 检查并更新一组命中盒配置。
        /// </summary>
        /// <param name="hitBoxes">命中盒配置列表。</param>
        /// <param name="previousTime">上一帧时间线时间。</param>
        /// <param name="deltaTime">本帧推进时间。</param>
        private void TriggerHitBoxes(List<HitBoxConfig> hitBoxes, float previousTime, float deltaTime)
        {
            if (hitBoxes == null)
            {
                return;
            }

            for (int i = 0; i < hitBoxes.Count; i++)
            {
                HitBoxConfig config = hitBoxes[i];
                if (config == null)
                {
                    continue;
                }

                UpdateHitBox(config, previousTime, deltaTime);
            }
        }

        /// <summary>
        /// 检查并触发一组子弹配置。
        /// </summary>
        /// <param name="bullets">子弹配置列表。</param>
        private void TriggerBullets(List<BulletConfig> bullets)
        {
            if (bullets == null)
            {
                return;
            }

            for (int i = 0; i < bullets.Count; i++)
            {
                BulletConfig config = bullets[i];
                if (config == null)
                {
                    continue;
                }

                TriggerBulletShots(config);
            }
        }

        /// <summary>
        /// 根据子弹配置的发射数量和持续时间触发每一发子弹。
        /// </summary>
        /// <param name="config">子弹配置。</param>
        private void TriggerBulletShots(BulletConfig config)
        {
            if (config == null || !config.IsEnabled || string.IsNullOrEmpty(config.BulletId))
            {
                return;
            }

            int shotCount = ResolveBulletShotCount(config);
            for (int shotIndex = 0; shotIndex < shotCount; shotIndex++)
            {
                float triggerTime = ResolveBulletShotTriggerTime(config, shotIndex, shotCount);
                string shotId = shotCount <= 1 ? config.BulletId : $"{config.BulletId}:{shotIndex}";
                if (_triggeredBulletIds.Contains(shotId) || triggerTime > _elapsedTime)
                {
                    continue;
                }

                _triggeredBulletIds.Add(shotId);
                SpawnBullet(config, triggerTime, shotIndex, shotCount);
            }
        }

        /// <summary>
        /// 在配置挂点处生成一发子弹，并写入调试黑板信息。
        /// </summary>
        /// <param name="bullet">子弹配置。</param>
        /// <param name="triggerTime">该发子弹对应的时间线触发时间。</param>
        /// <param name="shotIndex">当前发射序号。</param>
        /// <param name="shotCount">本配置总发射数量。</param>
        private void SpawnBullet(BulletConfig bullet, float triggerTime, int shotIndex, int shotCount)
        {
            if (TryResolveSocketTransform(_context, bullet.SocketSource, bullet.AttachPoint, out Transform socketTransform))
            {
                Vector3 spawnPosition = socketTransform.TransformPoint(bullet.SpawnArgs.PositionOffset);
                Quaternion spawnRotation = socketTransform.rotation * Quaternion.Euler(bullet.SpawnArgs.RotationOffset);
                Debug.Log($"StateTimelineExecutionRuntime.Bullet.Spawn: {bullet.BulletId}");
                //okita:记得改掉
                global::AsiSkillEditor.RunTime.SkillBulletPoolRuntime.Spawn(bullet, _context, spawnPosition, spawnRotation);
            }
            else
            {
                Debug.Log($"StateTimelineExecutionRuntime.Bullet.SpawnPointMissing: {bullet.BulletId}");
            }

            string message = shotCount > 1 ? $"Bullet.{shotIndex + 1}/{shotCount}" : "Bullet";
            EmitTimelineTrace(message, bullet.BulletId, triggerTime);
        }

        /// <summary>
        /// 解析一次子弹配置实际要生成的发射数量。
        /// </summary>
        /// <param name="config">子弹配置。</param>
        /// <returns>至少为 1 的发射数量；配置无效时返回 0。</returns>
        private static int ResolveBulletShotCount(BulletConfig config)
        {
            if (config == null || config.SpawnArgs == null)
            {
                return 0;
            }

            if (config.Duration <= 0f)
            {
                return Mathf.Max(1, config.SpawnArgs.SpawnCount);
            }

            return Mathf.Max(1, config.SpawnArgs.SpawnCount);
        }

        /// <summary>
        /// 解析第 N 发子弹在时间线上的触发时间。
        /// 多发且存在持续时间时，会把发射点平均分布在持续时间内。
        /// </summary>
        /// <param name="config">子弹配置。</param>
        /// <param name="shotIndex">当前发射序号。</param>
        /// <param name="shotCount">总发射数量。</param>
        /// <returns>该发子弹的触发时间。</returns>
        private static float ResolveBulletShotTriggerTime(BulletConfig config, int shotIndex, int shotCount)
        {
            float startTime = Mathf.Max(0f, config != null ? config.TriggerTime : 0f);
            float duration = Mathf.Max(0f, config != null ? config.Duration : 0f);
            if (shotCount <= 1 || duration <= 0f)
            {
                return startTime;
            }

            float t = shotIndex / (float)(shotCount - 1);
            return startTime + duration * t;
        }

        /// <summary>
        /// 按 Id 去重触发一次性时间线条目。
        /// </summary>
        /// <typeparam name="T">时间线条目类型。</typeparam>
        /// <param name="item">待检查的条目。</param>
        /// <param name="triggeredIds">已经触发过的 Id 集合。</param>
        /// <param name="idGetter">Id 读取函数。</param>
        /// <param name="enabledGetter">启用状态读取函数。</param>
        /// <param name="timeGetter">触发时间读取函数。</param>
        /// <param name="onTriggered">满足触发条件时执行的回调。</param>
        private void TriggerOnce<T>(
            T item,
            HashSet<string> triggeredIds,
            Func<T, string> idGetter,
            Func<T, bool> enabledGetter,
            Func<T, float> timeGetter,
            Action<T> onTriggered)
            where T : class
        {
            if (item == null || triggeredIds == null || idGetter == null || enabledGetter == null || timeGetter == null || onTriggered == null)
            {
                return;
            }

            string id = idGetter(item);
            if (string.IsNullOrEmpty(id) || triggeredIds.Contains(id) || !enabledGetter(item) || timeGetter(item) > _elapsedTime)
            {
                return;
            }

            triggeredIds.Add(id);
            onTriggered(item);
        }

        /// <summary>
        /// 触发一个瞬时时间线事件。
        /// 事件运行时只执行 Trigger，然后立即释放。
        /// </summary>
        /// <param name="config">事件配置。</param>
        /// okita:疑似遗留，这个不用了
        private void TriggerMetaSkillEvent(TimelineEventConfig config)
        {
            if (config == null || config.Data == null)
            {
                return;
            }

            try
            {
                TimelineEventRuntimeBase runtime = TimelineEventRuntimeFactory.Create(config, _context);
                try
                {
                    runtime.Trigger();
                }
                finally
                {
                    runtime.Dispose();
                }
                EmitTimelineTrace(config.EventType.ToString(), config.EventId, config.TriggerTime);
            }
            catch (Exception)
            {
                EmitTimelineTrace($"{config.EventType}.Failed", config.EventId, config.TriggerTime);
            }
        }

        /// <summary>
        /// 更新一个持续时间线事件的生命周期。
        /// 首次进入窗口时 Begin，窗口内每帧 Tick，到达结束时间后 End。
        /// </summary>
        /// <param name="config">事件配置。</param>
        /// <param name="previousTime">上一帧时间线时间。</param>
        /// <param name="deltaTime">本帧推进时间。</param>
        private void UpdateDurationEvent(TimelineEventConfig config, float previousTime, float deltaTime)
        {
            float startTime = Mathf.Max(0f, config.TriggerTime);
            float endTime = ResolveEventEndTime(config, startTime);
            bool overlapsWindow = previousTime <= endTime && _elapsedTime >= startTime;
            if (!overlapsWindow)
            {
                StopActiveEvent(config, true);
                return;
            }

            float clampedPreviousTime = Mathf.Clamp(previousTime, startTime, endTime);
            float clampedCurrentTime = Mathf.Clamp(_elapsedTime, startTime, endTime);

            if (!_activeEventRuntimes.TryGetValue(config.EventId, out TimelineEventRuntimeBase runtime))
            {
                runtime = TimelineEventRuntimeFactory.Create(config, _context);
                runtime.Begin();
                _activeEventRuntimes[config.EventId] = runtime;
                EmitTimelineTrace($"{config.EventType}.Begin", config.EventId, startTime);
            }

            float activeDeltaTime = Mathf.Max(0f, clampedCurrentTime - clampedPreviousTime);
            runtime.Tick(activeDeltaTime, Mathf.Max(0f, clampedCurrentTime - startTime));

            if (_elapsedTime > endTime)
            {
                StopActiveEvent(config, false);
            }
        }

        /// <summary>
        /// 解析持续事件的结束时间。
        /// 负时长表示持续到当前状态时间线结束；不再支持跨执行段延续。
        /// </summary>
        /// <param name="config">事件配置。</param>
        /// <param name="startTime">事件开始时间。</param>
        /// <returns>事件结束时间。</returns>
        private float ResolveEventEndTime(TimelineEventConfig config, float startTime)
        {
            if (config == null)
            {
                return startTime;
            }

            if (config.Duration < 0f)
            {
                // [AICode] 移除 recovery carryover 后，负时长事件只允许持续到当前状态时间线结束。
                return _config != null && _config.Duration > 0f
                    ? Mathf.Max(startTime, _config.Duration)
                    : startTime;
            }

            return startTime + Mathf.Max(0f, config.Duration);
        }

        /// <summary>
        /// 停止指定持续事件并释放对应运行时。
        /// </summary>
        /// <param name="config">事件配置。</param>
        /// <param name="interrupted">是否以中断方式结束。</param>
        private void StopActiveEvent(TimelineEventConfig config, bool interrupted)
        {
            if (config == null || string.IsNullOrEmpty(config.EventId))
            {
                return;
            }

            if (!_activeEventRuntimes.TryGetValue(config.EventId, out TimelineEventRuntimeBase runtime))
            {
                return;
            }

            try
            {
                runtime.End(interrupted);
            }
            finally
            {
                runtime.Dispose();
                _activeEventRuntimes.Remove(config.EventId);
            }

            EmitTimelineTrace($"{config.EventType}.End", config.EventId, _elapsedTime);
        }

        /// <summary>
        /// 停止一组持续事件。
        /// </summary>
        /// <param name="events">事件配置列表。</param>
        /// <param name="interrupted">是否以中断方式结束。</param>
        private void StopActiveEvents(List<TimelineEventConfig> events, bool interrupted)
        {
            if (events == null)
            {
                return;
            }

            for (int i = 0; i < events.Count; i++)
            {
                StopActiveEvent(events[i], interrupted);
            }
        }

        /// <summary>
        /// 更新单个命中盒。
        /// 持续命中盒会在窗口内保持运行，零时长命中盒只检测一次。
        /// </summary>
        /// <param name="config">命中盒配置。</param>
        /// <param name="previousTime">上一帧时间线时间。</param>
        /// <param name="deltaTime">本帧推进时间。</param>
        private void UpdateHitBox(HitBoxConfig config, float previousTime, float deltaTime)
        {
            if (config == null || !config.IsEnabled || string.IsNullOrEmpty(config.HitBoxId) || config.ShapeArgs == null)
            {
                StopActiveHitBox(config, true);
                return;
            }

            float startTime = Mathf.Max(0f, config.TriggerTime);
            float duration = Mathf.Max(0f, config.Duration);
            if (duration <= 0f)
            {   
                //okita:攻击盒小于等于0，也是不太可能出现的情况，而且写法也很怪
                TriggerInstantHitBox(config);
                return;
            }

            float endTime = startTime + duration;
            bool overlapsWindow = previousTime <= endTime && _elapsedTime >= startTime;
            if (!overlapsWindow)
            {
                StopActiveHitBox(config, false);
                return;
            }

            float clampedPreviousTime = Mathf.Clamp(previousTime, startTime, endTime);
            float clampedCurrentTime = Mathf.Clamp(_elapsedTime, startTime, endTime);

            if (!_activeHitBoxRuntimes.TryGetValue(config.HitBoxId, out TimelineHitBoxRuntime runtime))
            {
                runtime = new TimelineHitBoxRuntime(config, _context);
                runtime.Begin(startTime);
                _activeHitBoxRuntimes[config.HitBoxId] = runtime;
                EmitTimelineTrace("HitBox.Begin", config.HitBoxId, startTime);
            }

            float activeDeltaTime = Mathf.Max(0f, clampedCurrentTime - clampedPreviousTime);
            runtime.Tick(activeDeltaTime, clampedCurrentTime);

            if (_elapsedTime > endTime)
            {
                StopActiveHitBox(config, false);
            }
        }

        /// <summary>
        /// 触发零时长命中盒。
        /// 通过 Id 去重，确保同一个瞬时命中盒只执行一次。
        /// </summary>
        /// <param name="config">命中盒配置。</param>
        /// okita:不需要处理0时长攻击盒，不允许配置0帧攻击盒
        private void TriggerInstantHitBox(HitBoxConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.HitBoxId))
            {
                return;
            }

            if (_triggeredHitBoxIds.Contains(config.HitBoxId) || config.TriggerTime > _elapsedTime)
            {
                return;
            }

            _triggeredHitBoxIds.Add(config.HitBoxId);

            TimelineHitBoxRuntime runtime = new TimelineHitBoxRuntime(config, _context);
            runtime.Begin(config.TriggerTime);
            runtime.Tick(0f, config.TriggerTime);
            runtime.End(false);
            EmitTimelineTrace("HitBox", config.HitBoxId, config.TriggerTime);
        }

        /// <summary>
        /// 停止指定持续命中盒。
        /// </summary>
        /// <param name="config">命中盒配置。</param>
        /// <param name="interrupted">是否以中断方式结束。</param>
        private void StopActiveHitBox(HitBoxConfig config, bool interrupted)
        {
            if (config == null || string.IsNullOrEmpty(config.HitBoxId))
            {
                return;
            }

            if (!_activeHitBoxRuntimes.TryGetValue(config.HitBoxId, out TimelineHitBoxRuntime runtime))
            {
                return;
            }

            runtime.End(interrupted);
            _activeHitBoxRuntimes.Remove(config.HitBoxId);
            EmitTimelineTrace("HitBox.End", config.HitBoxId, _elapsedTime);
        }

        /// <summary>
        /// 停止一组持续命中盒。
        /// </summary>
        /// <param name="hitBoxes">命中盒配置列表。</param>
        /// <param name="interrupted">是否以中断方式结束。</param>
        private void StopActiveHitBoxes(List<HitBoxConfig> hitBoxes, bool interrupted)
        {
            if (hitBoxes == null)
            {
                return;
            }

            for (int i = 0; i < hitBoxes.Count; i++)
            {
                StopActiveHitBox(hitBoxes[i], interrupted);
            }
        }

        /// <summary>
        /// 强制结束并释放所有活动事件运行时。
        /// </summary>
        private void DisposeAllActiveEventRuntimes()
        {
            foreach (KeyValuePair<string, TimelineEventRuntimeBase> pair in _activeEventRuntimes)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                try
                {
                    pair.Value.End(true);
                }
                finally
                {
                    pair.Value.Dispose();
                }
            }

            _activeEventRuntimes.Clear();
        }

        /// <summary>
        /// 强制结束所有活动命中盒运行时。
        /// </summary>
        private void DisposeAllActiveHitBoxRuntimes()
        {
            foreach (KeyValuePair<string, TimelineHitBoxRuntime> pair in _activeHitBoxRuntimes)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                pair.Value.End(true);
            }

            _activeHitBoxRuntimes.Clear();
        }

        /// <summary>
        /// 判断事件数据是否支持持续时间生命周期。
        /// </summary>
        /// <param name="config">事件配置。</param>
        /// <returns>支持 Begin/Tick/End 时返回 true。</returns>
        private static bool SupportsDuration(TimelineEventConfig config)
        {
            return config != null && config.Data != null && config.Data.SupportsDuration;
        }

        /// <summary>
        /// 写入最后触发的时间线条目调试信息，并向调试总线发布 trace。
        /// </summary>
        /// <param name="itemType">条目类型或阶段名称。</param>
        /// <param name="itemId">条目 Id。</param>
        /// <param name="triggerTime">条目触发时间。</param>
        private void EmitTimelineTrace(string itemType, string itemId, float triggerTime)
        {
            _context.DebugLastTimelineItemType = itemType;
            _context.DebugLastTimelineItemId = itemId;
            SkillRuntimeDebugBus.PublishTrace(_context, new SkillRuntimeTraceEvent
            {
                TraceType = "TimelineTrigger",
                MetaSkillId = _context.CurrentMetaSkillConfig != null ? _context.CurrentMetaSkillConfig.MetaSkillId : string.Empty,
                PayloadId = itemId,
                Time = triggerTime,
                Message = itemType,
            });
        }

        //okita:找挂点相关的函数不建议写在这

        /// <summary>
        /// 根据角色或武器挂点配置解析实际 Transform。
        /// 优先读取预览配置中的挂点，找不到时回退到同名子节点，最后回退到根节点。
        /// </summary>
        /// <param name="context">技能上下文。</param>
        /// <param name="socketSource">挂点来源，角色或武器。</param>
        /// <param name="attachPoint">挂点名称。</param>
        /// <param name="anchorTransform">解析出的挂点 Transform。</param>
        /// <returns>成功解析到可用根节点时返回 true。</returns>
        internal static bool TryResolveSocketTransform(
            SkillContext context,
            SkillSocketSourceType socketSource,
            string attachPoint,
            out Transform anchorTransform)
        {
            object sourceTarget = socketSource == SkillSocketSourceType.Weapon ? context?.EquippedWeapon : context?.Caster;
            Transform rootTransform = ExtractTransform(sourceTarget);
            if (rootTransform == null && socketSource == SkillSocketSourceType.Weapon)
            {
                rootTransform = FindPreviewWeaponRoot(context?.Caster);
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

            if (socketSource == SkillSocketSourceType.Character)
            {
                GameUnit gameUnit = rootTransform.GetComponent<GameUnit>() ?? rootTransform.GetComponentInChildren<GameUnit>(true);
                if (TryResolveMountPoint(gameUnit != null ? gameUnit.MountPoints : null, attachPoint, out anchorTransform))
                {
                    return true;
                }
            }
            else
            {
                PreviewWeaponConfig previewWeaponConfig = rootTransform.GetComponent<PreviewWeaponConfig>() ?? rootTransform.GetComponentInChildren<PreviewWeaponConfig>(true);
                if (TryResolveMountPoint(previewWeaponConfig != null ? previewWeaponConfig.MountPoints : null, attachPoint, out anchorTransform))
                {
                    return true;
                }
            }

            Transform namedTransform = FindChildRecursive(rootTransform, attachPoint);
            anchorTransform = namedTransform != null ? namedTransform : rootTransform;
            return true;
        }

        /// <summary>
        /// 从预览挂点列表中按名称查找 Transform。
        /// </summary>
        /// <param name="mountPoints">预览挂点列表。</param>
        /// <param name="attachPoint">挂点名称。</param>
        /// <param name="mountTransform">命中的挂点 Transform。</param>
        /// <returns>找到匹配挂点时返回 true。</returns>
        internal static bool TryResolveMountPoint(IList<PreviewMountPoint> mountPoints, string attachPoint, out Transform mountTransform)
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

                if (string.Equals(mountPoint.SocketName, attachPoint))
                {
                    mountTransform = mountPoint.MountTransform;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 从常见 Unity 对象中提取 Transform。
        /// </summary>
        /// <param name="target">GameObject 或 Component。</param>
        /// <returns>目标 Transform；不支持的对象返回 null。</returns>
        internal static Transform ExtractTransform(object target)
        {
            switch (target)
            {
                case GameUnit gameUnit:
                    return gameUnit.UnitObject != null ? gameUnit.UnitObject.transform : null;

                case GameObject gameObject:
                    return gameObject.transform;

                case Component component:
                    return component.transform;

                default:
                    return null;
            }
        }

        /// <summary>
        /// 在角色层级中查找预览武器根节点。
        /// 用于上下文没有直接提供 EquippedWeapon 时的兜底。
        /// </summary>
        /// <param name="caster">施法 GameUnit。</param>
        /// <returns>武器预览根节点；找不到时返回 null。</returns>
        /// okita:后面会把预览的概念去掉，改为正式的武器配置
        /// TODO: 3C 稳定后，用正式武器配置/运行时替换 PreviewWeapon fallback。
        internal static Transform FindPreviewWeaponRoot(GameUnit caster)
        {
            Transform casterTransform = ExtractTransform(caster);
            if (casterTransform == null)
            {
                return null;
            }

            PreviewWeaponConfig weaponConfig = casterTransform.GetComponentInChildren<PreviewWeaponConfig>(true);
            return weaponConfig != null ? weaponConfig.transform : null;
        }

        /// <summary>
        /// 在层级中递归查找指定名称的子节点。
        /// </summary>
        /// <param name="root">查找起点。</param>
        /// <param name="targetName">目标节点名称。</param>
        /// <returns>匹配节点；找不到时返回 null。</returns>
        internal static Transform FindChildRecursive(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrEmpty(targetName))
            {
                return null;
            }

            if (string.Equals(root.name, targetName))
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
    }

    /// <summary>
    /// 单个命中盒配置的运行时实例。
    /// 负责按时间检测命中、去重已命中目标，并触发命中效果、韧性和硬直相关占位逻辑。
    /// </summary>
    internal sealed class TimelineHitBoxRuntime
    {
        private readonly HitBoxConfig _config;
        private readonly SkillContext _context;
        private readonly List<GameUnit> _beHitTargets = new List<GameUnit>();

        private float _currentTime;
        private float _repeatHitElapsed;
        private int _lastHitPartIndex;
        private float _eventStartTime;
        private LayerMask _layerMask;
        private bool _hasRegisteredHit;
        private bool _loggedAnchorMissing;
        private bool _loggedBakedRootMissing;

        /// <summary>
        /// 创建命中盒运行时。
        /// </summary>
        /// <param name="config">命中盒配置。</param>
        /// <param name="context">技能运行上下文。</param>
        public TimelineHitBoxRuntime(HitBoxConfig config, SkillContext context)
        {
            _config = config;
            _context = context ?? new SkillContext();
        }

        /// <summary>
        /// 开始命中盒生命周期，并写入当前活动命中盒黑板信息。
        /// </summary>
        /// <param name="startTime">命中盒在时间线上的开始时间。</param>
        public void Begin(float startTime)
        {
            _currentTime = Mathf.Max(0f, startTime);
            _eventStartTime = Mathf.Max(0f, startTime);
            _repeatHitElapsed = 0f;
            _lastHitPartIndex = 0;
            _layerMask = _config != null ? _config.ShapeArgs.HitLayerMask : ~0;
            _hasRegisteredHit = false;
            _loggedAnchorMissing = false;
            _loggedBakedRootMissing = false;
            _beHitTargets.Clear();
            LogDebug("HitBox.Begin");
        }

        /// <summary>
        /// 推进命中盒检测。
        /// 配置了重复命中间隔时，会周期性清空已命中目标列表。
        /// </summary>
        /// <param name="deltaTime">本帧推进时间。</param>
        /// <param name="elapsedTime">当前时间线时间。</param>
        public void Tick(float deltaTime, float elapsedTime)
        {
            if (_config == null || _config.ShapeArgs == null)
            {
                return;
            }

            if (_config.ShapeArgs.HitInterval > 0f)
            {
                _repeatHitElapsed += deltaTime;
                if (_repeatHitElapsed >= _config.ShapeArgs.HitInterval)
                {
                    _repeatHitElapsed = 0f;
                    _beHitTargets.Clear();
                }
            }

            if (HasBakedParts())
            {
                CheckBakedParts(_currentTime, elapsedTime);
            }
            //okita:不烘焙就不让过
            else if (TryGetDefaultSegment(out Vector3 startPos, out Vector3 endPos))
            {
                CheckBox(startPos, endPos);
            }

            _currentTime = elapsedTime;
        }

        /// <summary>
        /// 结束命中盒生命周期。
        /// 非中断结束且存在烘焙段时，会补查剩余命中段。
        /// </summary>
        /// <param name="interrupted">是否以中断方式结束。</param>
        public void End(bool interrupted)
        {
            if (!interrupted && HasBakedParts())
            {
                CheckBakedParts(_currentTime, float.MaxValue);
            }

            if (!_hasRegisteredHit)
            {
                LogDebug(interrupted ? "HitBox.EndNoHitInterrupted" : "HitBox.EndNoHit");
            }
        }

        /// <summary>
        /// 判断当前命中盒是否使用烘焙出来的命中段数据。
        /// </summary>
        /// <returns>存在烘焙段时返回 true。</returns>
        private bool HasBakedParts()
        {
            return _config != null &&
                   _config.ShapeArgs != null &&
                   _config.ShapeArgs.BakedParts != null &&
                   _config.ShapeArgs.BakedParts.Count > 0;
        }

        /// <summary>
        /// 检查指定时间窗口内的烘焙命中段。
        /// 没有新的烘焙段时，会使用最近一段做一次兜底检测。
        /// </summary>
        /// <param name="startTime">窗口开始时间。</param>
        /// <param name="endTime">窗口结束时间。</param>
        private void CheckBakedParts(float startTime, float endTime)
        {
            List<HitBoxBakedPart> bakedParts = _config.ShapeArgs.BakedParts;
            if (bakedParts == null || bakedParts.Count == 0)
            {
                return;
            }

            if (!TryGetBakedRootTransform(out Transform rootTransform))
            {
                if (!_loggedBakedRootMissing)
                {
                    _loggedBakedRootMissing = true;
                    LogDebug("HitBox.BakedRootMissing");
                }

                return;
            }

            bool foundBox = false;
            int startIndex = Mathf.Clamp(_lastHitPartIndex, 0, bakedParts.Count - 1);
            for (int i = startIndex; i < bakedParts.Count; i++)
            {
                HitBoxBakedPart bakedPart = bakedParts[i];
                if (bakedPart == null)
                {
                    continue;
                }

                float boxTriggerTime = bakedPart.TriggerTime + _eventStartTime;
                
                //okita:直到检测到最后一个攻击盒
                if (boxTriggerTime < startTime)
                {
                    continue;
                }
                
                //okita:说明当前帧还没到这个攻击盒判定区
                if (boxTriggerTime > endTime)
                {
                    break;
                }

                Vector3 segmentStart = rootTransform.TransformPoint(bakedPart.StartPos);
                Vector3 segmentEnd = segmentStart + rootTransform.TransformDirection(bakedPart.Direction) * _config.ShapeArgs.Scale.x;
                CheckBox(segmentStart, segmentEnd);
                _lastHitPartIndex = i;
                foundBox = true;
            }
            
            //okita:这个保护机制好像没必要
            if (!foundBox)
            {
                int safeIndex = Mathf.Clamp(_lastHitPartIndex, 0, bakedParts.Count - 1);
                HitBoxBakedPart bakedPart = bakedParts[safeIndex];
                if (bakedPart == null)
                {
                    return;
                }

                Vector3 segmentStart = rootTransform.TransformPoint(bakedPart.StartPos);
                Vector3 segmentEnd = segmentStart + rootTransform.TransformDirection(bakedPart.Direction) * _config.ShapeArgs.Scale.x;
                CheckBox(segmentStart, segmentEnd);
            }
        }

        /// <summary>
        /// 根据挂点和偏移计算默认命中段的起止点。
        /// </summary>
        /// <param name="startPos">命中段起点。</param>
        /// <param name="endPos">命中段终点。</param>
        /// <returns>成功解析挂点时返回 true。</returns>
        /// okita:不烘焙的攻击盒，挂点找不到就直接放弃检测了
        private bool TryGetDefaultSegment(out Vector3 startPos, out Vector3 endPos)
        {
            startPos = Vector3.zero;
            endPos = Vector3.zero;

            if (!TryResolveAnchorTransform(out Transform anchorTransform))
            {
                if (!_loggedAnchorMissing)
                {
                    _loggedAnchorMissing = true;
                    LogDebug("HitBox.AnchorMissing");
                }

                return false;
            }

            Vector3 offsetPosition = _config.ShapeArgs.OffsetPosition;
            Vector3 offsetRotation = _config.ShapeArgs.OffsetRotation;
            startPos = anchorTransform.TransformPoint(offsetPosition);
            Quaternion rotation = anchorTransform.rotation * Quaternion.Euler(offsetRotation);
            endPos = startPos + rotation * Vector3.forward * _config.ShapeArgs.Scale.x;
            return true;
        }

        /// <summary>
        /// 按配置检测类型执行一次物理命中检测。
        /// Capsule 使用 OverlapCapsule，Raycast 使用 RaycastAll。
        /// </summary>
        /// <param name="startPos">检测起点。</param>
        /// <param name="endPos">检测终点。</param>
        private void CheckBox(Vector3 startPos, Vector3 endPos)
        {
            switch (_config.ShapeArgs.DetectionType)
            {
                case HitBoxDetectionType.Capsule:
                {
                    Collider[] colliders = Physics.OverlapCapsule(
                        startPos,
                        endPos,
                        Mathf.Max(0f, _config.ShapeArgs.Scale.y),
                        _layerMask);
                    for (int i = 0; i < colliders.Length; i++)
                    {
                        RegisterHit(colliders[i], (startPos + endPos) * 0.5f);
                    }

                    break;
                }

                case HitBoxDetectionType.Raycast:
                {
                    Vector3 direction = endPos - startPos;
                    if (direction.sqrMagnitude <= Mathf.Epsilon)
                    {
                        break;
                    }

                    RaycastHit[] hits = Physics.RaycastAll(
                        startPos,
                        direction.normalized,
                        direction.magnitude,
                        _layerMask);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        RegisterHit(hits[i]);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// 注册一次命中。
        /// 同一个命中盒周期内已命中的目标不会重复结算，除非重复命中间隔清空了列表。
        /// </summary>
        /// <param name="targetCollider">命中的碰撞体。</param>
        private void RegisterHit(Collider targetCollider, Vector3 referencePoint)
        {
            if (!GameUnitResolver.TryResolve(targetCollider, referencePoint, out GameUnitTargetInfo targetInfo) || targetInfo.Unit == null)
            {
                Debug.Log($"[AICode] HitBox.RegisterHit(Collider): failed to resolve GameUnit from collider '{(targetCollider != null ? targetCollider.name : "null")}' for hitbox '{(_config != null ? _config.HitBoxId : string.Empty)}'.");
                return;
            }

            if (_beHitTargets.Contains(targetInfo.Unit))
            {
                return;
            }

            _hasRegisteredHit = true;
            _beHitTargets.Add(targetInfo.Unit);
            Debug.Log($"[AICode] HitBox.RegisterHit(Collider): hitbox='{(_config != null ? _config.HitBoxId : string.Empty)}', unit='{targetInfo.Unit.name}', targetObject='{(targetInfo.TargetObject != null ? targetInfo.TargetObject.name : "null")}', collider='{(targetInfo.TargetCollider != null ? targetInfo.TargetCollider.name : "null")}', hasHitPoint={targetInfo.HasHitPoint}, hitPoint='{targetInfo.HitPoint}'.", targetInfo.Unit);
            LogDebug("HitBox.Hit");

            GameUnit previousTarget = _context.PrimaryTarget;
            try
            {
                _context.PrimaryTarget = targetInfo.Unit;
                SkillEffectResult effectResult = ApplyOnHitEffectPlaceholder();
                ApplyToughnessPlaceholder();
                ApplyHitStunPlaceholder();
                PublishUnitHit(targetInfo, effectResult);
            }
            finally
            {
                _context.PrimaryTarget = previousTarget;
            }
        }

        /// <summary>
        /// 注册一次射线命中。
        /// 会保留 RaycastHit 自带的精确命中点信息。
        /// </summary>
        /// <param name="hit">命中结果。</param>
        private void RegisterHit(RaycastHit hit)
        {
            if (!GameUnitResolver.TryResolve(hit, out GameUnitTargetInfo targetInfo) || targetInfo.Unit == null)
            {
                Debug.Log($"[AICode] HitBox.RegisterHit(RaycastHit): failed to resolve GameUnit from hit collider '{(hit.collider != null ? hit.collider.name : "null")}' for hitbox '{(_config != null ? _config.HitBoxId : string.Empty)}'.");
                return;
            }

            if (_beHitTargets.Contains(targetInfo.Unit))
            {
                return;
            }

            _hasRegisteredHit = true;
            _beHitTargets.Add(targetInfo.Unit);
            Debug.Log($"[AICode] HitBox.RegisterHit(RaycastHit): hitbox='{(_config != null ? _config.HitBoxId : string.Empty)}', unit='{targetInfo.Unit.name}', targetObject='{(targetInfo.TargetObject != null ? targetInfo.TargetObject.name : "null")}', collider='{(targetInfo.TargetCollider != null ? targetInfo.TargetCollider.name : "null")}', hasHitPoint={targetInfo.HasHitPoint}, hitPoint='{targetInfo.HitPoint}'.", targetInfo.Unit);
            LogDebug("HitBox.Hit");

            GameUnit previousTarget = _context.PrimaryTarget;
            try
            {
                _context.PrimaryTarget = targetInfo.Unit;
                SkillEffectResult effectResult = ApplyOnHitEffectPlaceholder();
                ApplyToughnessPlaceholder();
                ApplyHitStunPlaceholder();
                PublishUnitHit(targetInfo, effectResult);
            }
            finally
            {
                _context.PrimaryTarget = previousTarget;
            }
        }

        /// <summary>
        /// 执行命中盒命中效果图。
        /// 当前目标会临时写入 SkillContext.PrimaryTarget，执行完后恢复。
        /// </summary>
        private SkillEffectResult ApplyOnHitEffectPlaceholder()
        {
            if (_config.OnHitEffect == null)
            {
                Debug.Log($"[AICode] HitBox.OnHitEffect: effect config is null for hitbox '{(_config != null ? _config.HitBoxId : string.Empty)}'.");
                LogDebug("HitBox.OnHitEffectNull");
                return SkillEffectResult.None;
            }

            if (string.IsNullOrEmpty(_config.OnHitEffect.RootNodeId) || _config.OnHitEffect.Nodes == null || _config.OnHitEffect.Nodes.Count == 0)
            {
                Debug.Log($"[AICode] HitBox.OnHitEffect: effect graph is empty for hitbox '{(_config != null ? _config.HitBoxId : string.Empty)}'. root='{_config.OnHitEffect.RootNodeId}'.");
                LogDebug("HitBox.OnHitEffectEmpty");
                return SkillEffectResult.None;
            }

            Debug.Log($"[AICode] HitBox.OnHitEffect: executing root='{_config.OnHitEffect.RootNodeId}' for target='{(_context.PrimaryTarget != null ? _context.PrimaryTarget.name : "null")}'.", _context.PrimaryTarget);
            LogDebug("HitBox.OnHitEffectExecute");
            SkillEffectResult result = ExecuteTimelineEffect(_config.OnHitEffect);
            Debug.Log($"[AICode] HitBox.OnHitEffect: result success={(result != null && result.Succeeded)}, failure='{(result != null ? result.FailureKind.ToString() : "None")}'.", _context.PrimaryTarget);
            PublishTrace("HitBox.OnHitEffect", result != null && !result.Succeeded ? result.FailureKind.ToString() : string.Empty);
            return result ?? SkillEffectResult.None;
        }

        private void PublishUnitHit(GameUnitTargetInfo targetInfo, SkillEffectResult effectResult)
        {
            if (_context.UnitHitEventPublisher == null || _context.Caster == null || targetInfo.Unit == null)
            {
                return;
            }

            Transform attackerTransform = TimelineMotionBridgeUtility.ExtractTransform(_context.Caster);
            Transform defenderTransform = TimelineMotionBridgeUtility.ExtractTransform(targetInfo.Unit);
            Vector3 hitPoint = targetInfo.HasHitPoint
                ? targetInfo.HitPoint
                : defenderTransform != null ? defenderTransform.position : Vector3.zero;
            Vector3 hitDirection = attackerTransform != null && defenderTransform != null
                ? defenderTransform.position - attackerTransform.position
                : Vector3.zero;
            if (hitDirection.sqrMagnitude > Mathf.Epsilon)
            {
                hitDirection.Normalize();
            }

            bool effectSucceeded = effectResult == null || !effectResult.HasValue || effectResult.Succeeded;
            UnitHitEvent hitEvent = new UnitHitEvent(
                _context.Caster,
                targetInfo.Unit,
                _config != null ? _config.HitBoxId : string.Empty,
                hitPoint,
                hitDirection,
                targetInfo.HasHitPoint,
                targetInfo.HitNormal,
                targetInfo.HasHitNormal,
                Time.frameCount,
                _context,
                new HitResolutionResult(true, effectSucceeded, HitReactionType.Normal));
            _context.UnitHitEventPublisher.Publish(in hitEvent);
            PublishTrace("UnitHit.Published", targetInfo.Unit.name);
        }

        private SkillEffectResult ExecuteTimelineEffect(SkillEffectConfig effectConfig)
        {
            if (effectConfig == null || _context == null)
            {
                return SkillEffectResult.None;
            }

            if (_context.EffectExecutor == null)
            {
                return SkillEffectResult.None;
            }

            return _context.EffectExecutor.Execute(effectConfig, _context) ?? SkillEffectResult.None;
        }

        /// <summary>
        /// 记录韧性伤害占位信息。
        /// 当前只写入黑板并发送 trace，实际韧性系统可在这里接入。
        /// </summary>
        private void ApplyToughnessPlaceholder()
        {
            if (_config.OnHitResponse == null || _config.OnHitResponse.ToughnessDamage <= 0f)
            {
                return;
            }

            PublishTrace("HitBox.ToughnessPlaceholder", _config.OnHitResponse.ToughnessDamage.ToString("0.###"));
        }

        /// <summary>
        /// 记录硬直占位信息。
        /// 当前只写入黑板并发送 trace，实际硬直系统可在这里接入。
        /// </summary>
        private void ApplyHitStunPlaceholder()
        {
            if (_config.OnHitResponse == null || _config.OnHitResponse.HitStunDuration <= 0f)
            {
                return;
            }

            string message = string.Format(
                "duration={0:0.###}, tag={1}",
                _config.OnHitResponse.HitStunDuration,
                _config.OnHitResponse.HitStunTag ?? string.Empty);
            PublishTrace("HitBox.HitStunPlaceholder", message);
        }

        /// <summary>
        /// 发布命中盒相关调试 trace。
        /// </summary>
        /// <param name="traceType">trace 类型。</param>
        /// <param name="message">trace 附加消息。</param>
        private void PublishTrace(string traceType, string message)
        {
            SkillRuntimeDebugBus.PublishTrace(_context, new SkillRuntimeTraceEvent
            {
                TraceType = traceType,
                MetaSkillId = _context.CurrentMetaSkillConfig != null ? _context.CurrentMetaSkillConfig.MetaSkillId : string.Empty,
                PayloadId = _config != null ? _config.HitBoxId : string.Empty,
                Time = _context.DebugTimelineTime,
                Message = message,
            });
        }

        /// <summary>
        /// 输出命中盒调试日志。
        /// </summary>
        /// <param name="stage">命中盒阶段名称。</param>
        private void LogDebug(string stage)
        {
            Debug.Log($"StateTimelineExecutionRuntime.{stage}: {(_config != null ? _config.HitBoxId : string.Empty)}");
        }

        /// <summary>
        /// 解析命中盒挂点 Transform。
        /// </summary>
        /// <param name="anchorTransform">挂点 Transform。</param>
        /// <returns>解析成功时返回 true。</returns>
        private bool TryResolveAnchorTransform(out Transform anchorTransform)
        {
            return StateTimelineExecutionRuntime.TryResolveSocketTransform(_context, _config.SocketSource, _config.AttachPoint, out anchorTransform);
        }

        /// <summary>
        /// 解析命中盒来源根节点。
        /// 武器来源会在没有 EquippedWeapon 时回退到预览武器根节点。
        /// </summary>
        /// <param name="rootTransform">命中盒来源根节点。</param>
        /// <returns>解析成功时返回 true。</returns>
        private bool TryGetHitBoxRootTransform(out Transform rootTransform)
        {
            object sourceTarget = _config.SocketSource == SkillSocketSourceType.Weapon ? _context.EquippedWeapon : _context.Caster;
            rootTransform = StateTimelineExecutionRuntime.ExtractTransform(sourceTarget);
            if (rootTransform == null && _config.SocketSource == SkillSocketSourceType.Weapon)
            {
                rootTransform = StateTimelineExecutionRuntime.FindPreviewWeaponRoot(_context.Caster);
            }

            return rootTransform != null;
        }

        /// <summary>
        /// 解析烘焙命中段使用的根节点。
        /// 当前烘焙数据默认跟随施法者根节点。
        /// </summary>
        /// <param name="rootTransform">烘焙根节点。</param>
        /// <returns>解析成功时返回 true。</returns>
        private bool TryGetBakedRootTransform(out Transform rootTransform)
        {
            rootTransform = StateTimelineExecutionRuntime.ExtractTransform(_context.Caster);
            return rootTransform != null;
        }
    }

    /// <summary>
    /// 技能子弹运行时对象池。
    /// 按子弹预制体路径维护对象池，避免频繁 Instantiate/Destroy。
    /// </summary>
    public static class SkillBulletPoolRuntime
    {
        private static readonly Dictionary<string, UnityEngine.Pool.ObjectPool<SkillBulletInstance>> Pools = new Dictionary<string, UnityEngine.Pool.ObjectPool<SkillBulletInstance>>(StringComparer.Ordinal);
        private static Transform s_poolRoot;
        private static Transform s_activeRoot;

        /// <summary>
        /// 从对象池生成并激活一枚子弹。
        /// </summary>
        /// <param name="config">子弹配置。</param>
        /// <param name="context">技能运行上下文。</param>
        /// <param name="position">生成位置。</param>
        /// <param name="rotation">生成旋转。</param>
        public static void Spawn(BulletConfig config, SkillContext context, Vector3 position, Quaternion rotation)
        {
            if (config == null || config.SpawnArgs == null || string.IsNullOrEmpty(config.SpawnArgs.BulletPrefabPath))
            {
                return;
            }

            string prefabKey = config.SpawnArgs.BulletPrefabPath;
            if (!Pools.TryGetValue(prefabKey, out UnityEngine.Pool.ObjectPool<SkillBulletInstance> pool))
            {
                GameObject prefab = SkillBulletRuntimeCatalog.LoadPrefab(prefabKey);
                if (prefab == null)
                {
                    return;
                }

                EnsurePoolRoot();
                pool = CreatePool(prefabKey, prefab);
                Pools[prefabKey] = pool;
            }

            SkillBulletInstance bullet = pool.Get();
            bullet.Activate(config, context, position, rotation);
        }

        /// <summary>
        /// 为指定预制体创建 Unity 对象池。
        /// </summary>
        /// <param name="prefabKey">预制体资源路径。</param>
        /// <param name="prefab">子弹预制体。</param>
        /// <returns>子弹实例对象池。</returns>
        private static UnityEngine.Pool.ObjectPool<SkillBulletInstance> CreatePool(string prefabKey, GameObject prefab)
        {
            return new UnityEngine.Pool.ObjectPool<SkillBulletInstance>(
                () => CreateInstance(prefabKey, prefab),
                bullet =>
                {
                    if (bullet != null)
                    {
                        bullet.gameObject.SetActive(true);
                        if (s_activeRoot != null)
                        {
                            bullet.transform.SetParent(s_activeRoot, false);
                        }
                    }
                },
                bullet =>
                {
                    if (bullet != null)
                    {
                        bullet.gameObject.SetActive(false);
                        if (s_poolRoot != null)
                        {
                            bullet.transform.SetParent(s_poolRoot, false);
                        }
                    }
                },
                bullet =>
                {
                    if (bullet != null)
                    {
                        UnityEngine.Object.Destroy(bullet.gameObject);
                    }
                },
                true,
                4,
                32);
        }

        /// <summary>
        /// 实例化一个子弹对象并挂接 SkillBulletInstance 组件。
        /// </summary>
        /// <param name="prefabKey">预制体资源路径。</param>
        /// <param name="prefab">子弹预制体。</param>
        /// <returns>创建出的子弹实例。</returns>
        private static SkillBulletInstance CreateInstance(string prefabKey, GameObject prefab)
        {
            EnsurePoolRoot();
            GameObject instanceObject = UnityEngine.Object.Instantiate(prefab, s_poolRoot);
            instanceObject.name = $"{prefab.name}_Bullet";
            SkillBulletInstance bullet = instanceObject.GetComponent<SkillBulletInstance>();
            if (bullet == null)
            {
                bullet = instanceObject.AddComponent<SkillBulletInstance>();
            }

            bullet.Configure(prefabKey, Release);
            instanceObject.SetActive(false);
            return bullet;
        }

        /// <summary>
        /// 将子弹实例归还到对应对象池。
        /// </summary>
        /// <param name="bullet">待回收的子弹实例。</param>
        private static void Release(SkillBulletInstance bullet)
        {
            if (bullet == null || string.IsNullOrEmpty(bullet.PrefabKey))
            {
                return;
            }

            if (Pools.TryGetValue(bullet.PrefabKey, out UnityEngine.Pool.ObjectPool<SkillBulletInstance> pool))
            {
                pool.Release(bullet);
            }
        }

        /// <summary>
        /// 确保场景中存在子弹池根节点和活动节点。
        /// </summary>
        private static void EnsurePoolRoot()
        {
            if (s_poolRoot != null && s_activeRoot != null)
            {
                return;
            }

            GameObject rootObject = GameObject.Find("/SkillBulletRuntime");
            if (rootObject == null)
            {
                rootObject = new GameObject("SkillBulletRuntime");
            }

            Transform poolRoot = rootObject.transform.Find("Pool");
            if (poolRoot == null)
            {
                GameObject poolObject = new GameObject("Pool");
                poolObject.transform.SetParent(rootObject.transform, false);
                poolRoot = poolObject.transform;
            }

            Transform activeRoot = rootObject.transform.Find("Active");
            if (activeRoot == null)
            {
                GameObject activeObject = new GameObject("Active");
                activeObject.transform.SetParent(rootObject.transform, false);
                activeRoot = activeObject.transform;
            }

            s_poolRoot = poolRoot;
            s_activeRoot = activeRoot;
        }
    }

    /// <summary>
    /// 单枚技能子弹的运行时组件。
    /// 负责飞行、追踪、碰撞检测、命中效果执行以及回收到对象池。
    /// </summary>
    public sealed class SkillBulletInstance : MonoBehaviour
    {
        private Action<SkillBulletInstance> _releaseAction;
        private BulletConfig _config;
        private SkillContext _context;
        private float _elapsedTime;
        private float _maxLifetime;
        private float _speed;
        private float _collisionRadius;
        private int _hitLayerMask;
        private BulletFlightMode _flightMode;
        private Vector3 _velocity;
        private float _gravity;
        private float _currentSpeed;
        private Transform _trackingTarget;
        private Vector3 _curveLateralAxis;
        private float _curveLateralScale;
        private float _curveVerticalScale;
        private float _curvePhase;
        private float _trackingStartDistance;
        private bool _hasHitTarget;

        /// <summary>
        /// 当前实例所属的预制体资源路径，用于回收到正确对象池。
        /// </summary>
        public string PrefabKey { get; private set; }

        /// <summary>
        /// 配置对象池回收信息。
        /// </summary>
        /// <param name="prefabKey">预制体资源路径。</param>
        /// <param name="releaseAction">回收回调。</param>
        internal void Configure(string prefabKey, Action<SkillBulletInstance> releaseAction)
        {
            PrefabKey = prefabKey;
            _releaseAction = releaseAction;
        }

        /// <summary>
        /// 激活子弹实例并初始化飞行参数。
        /// 根据配置选择直线、抛物线、追踪抛物线或追踪曲线飞行。
        /// </summary>
        /// <param name="config">子弹配置。</param>
        /// <param name="context">技能运行上下文。</param>
        /// <param name="position">生成位置。</param>
        /// <param name="rotation">生成旋转。</param>
        internal void Activate(BulletConfig config, SkillContext context, Vector3 position, Quaternion rotation)
        {
            _config = config;
            _context = context;
            _elapsedTime = 0f;
            _maxLifetime = Mathf.Max(0.01f, config != null && config.SpawnArgs != null ? config.SpawnArgs.MaxLifetime : 0.01f);
            _speed = Mathf.Max(0f, config != null && config.SpawnArgs != null ? config.SpawnArgs.Speed : 0f);
            _collisionRadius = Mathf.Max(0f, config != null && config.SpawnArgs != null ? config.SpawnArgs.CollisionRadius : 0f);
            _hitLayerMask = config != null && config.SpawnArgs != null ? config.SpawnArgs.HitLayerMask : ~0;
            _flightMode = config != null && config.SpawnArgs != null ? config.SpawnArgs.FlightMode : BulletFlightMode.Direct;
            _currentSpeed = _speed;
            _trackingTarget = null;
            _curveLateralAxis = Vector3.zero;
            _curveLateralScale = 0f;
            _curveVerticalScale = 0f;
            _curvePhase = 0f;
            _trackingStartDistance = 0f;
            _hasHitTarget = false;
            _gravity = config != null && config.SpawnArgs != null && config.SpawnArgs.Parabola != null
                ? Mathf.Max(0f, config.SpawnArgs.Parabola.Gravity)
                : 0f;
            transform.SetPositionAndRotation(position, rotation);
            Vector3 forwardVelocity = transform.forward * _speed;
            if (_flightMode == BulletFlightMode.HomingParabola)
            {
                Transform ignoredRoot = StateTimelineExecutionRuntime.ExtractTransform(_context != null ? _context.Caster : null);
                if (!TryResolveHomingParabolaVelocity(position, transform.forward, ignoredRoot, out _velocity))
                {
                    float initialVerticalSpeed = config != null && config.SpawnArgs != null && config.SpawnArgs.Parabola != null
                        ? config.SpawnArgs.Parabola.InitialVerticalSpeed
                        : 0f;
                    _velocity = forwardVelocity + Vector3.up * initialVerticalSpeed;
                }
            }
            else if (_flightMode == BulletFlightMode.Parabola)
            {
                float initialVerticalSpeed = config != null && config.SpawnArgs != null && config.SpawnArgs.Parabola != null
                    ? config.SpawnArgs.Parabola.InitialVerticalSpeed
                    : 0f;
                _velocity = forwardVelocity + Vector3.up * initialVerticalSpeed;
            }
            else if (_flightMode == BulletFlightMode.HomingCurve)
            {
                InitializeHomingCurve(position, transform.forward);
            }
            else
            {
                _velocity = forwardVelocity;
            }

            if (_velocity.sqrMagnitude > Mathf.Epsilon)
            {
                transform.rotation = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 初始化追踪曲线飞行参数。
        /// 会先选择目标，再生成随机发射方向、侧向偏移轴和曲线相位。
        /// </summary>
        /// <param name="origin">发射起点。</param>
        /// <param name="forward">默认发射方向。</param>
        private void InitializeHomingCurve(Vector3 origin, Vector3 forward)
        {
            Vector3 defaultDirection = forward.sqrMagnitude > Mathf.Epsilon ? forward.normalized : transform.forward;

            if (_config == null || _config.SpawnArgs == null || _config.SpawnArgs.Tracking == null)
            {
                _velocity = defaultDirection * Mathf.Max(0.01f, _speed);
                return;
            }

            Transform ignoredRoot = StateTimelineExecutionRuntime.ExtractTransform(_context != null ? _context.Caster : null);
            BulletTrackingArgs trackingArgs = _config.SpawnArgs.Tracking;
            if (!SkillTargetSelectionUtility.TrySelectBestTarget(
                    origin,
                    defaultDirection,
                    trackingArgs.SearchRange,
                    trackingArgs.SearchAngle,
                    _hitLayerMask,
                    trackingArgs.CenterWeight,
                    ignoredRoot,
                    out SkillWeightedTargetSelectionResult selectionResult))
            {
                _velocity = defaultDirection * Mathf.Max(0.01f, _speed);
                return;
            }

            _trackingTarget = selectionResult.Target != null ? selectionResult.Target.transform : null;
            _trackingStartDistance = Mathf.Max(0.01f, selectionResult.Distance);

            Vector3 randomizedLaunchDirection = ResolveRandomizedLaunchDirection(defaultDirection, trackingArgs);
            _velocity = randomizedLaunchDirection * Mathf.Max(0.01f, _speed);

            Vector3 toTarget = ResolveTrackedTargetPoint() - origin;
            Vector3 planeNormal = Vector3.Cross(randomizedLaunchDirection, toTarget);
            if (planeNormal.sqrMagnitude <= 0.001f)
            {
                planeNormal = Vector3.Cross(randomizedLaunchDirection, Vector3.up);
            }

            _curveLateralAxis = Vector3.Cross(planeNormal.normalized, randomizedLaunchDirection).normalized;
            if (_curveLateralAxis.sqrMagnitude <= Mathf.Epsilon)
            {
                _curveLateralAxis = Vector3.right;
            }

            _curveLateralScale = UnityEngine.Random.Range(-1f, 1f);
            if (Mathf.Abs(_curveLateralScale) < 0.2f)
            {
                _curveLateralScale = Mathf.Sign(_curveLateralScale == 0f ? 1f : _curveLateralScale) * 0.2f;
            }

            _curveVerticalScale = UnityEngine.Random.Range(-1f, 1f);
            _curvePhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }

        /// <summary>
        /// 根据追踪配置随机扰动发射方向。
        /// </summary>
        /// <param name="defaultDirection">默认发射方向。</param>
        /// <param name="trackingArgs">追踪参数。</param>
        /// <returns>随机扰动后的单位方向。</returns>
        private static Vector3 ResolveRandomizedLaunchDirection(Vector3 defaultDirection, BulletTrackingArgs trackingArgs)
        {
            Vector3 normalizedDirection = defaultDirection.sqrMagnitude > Mathf.Epsilon ? defaultDirection.normalized : Vector3.forward;
            float yaw = UnityEngine.Random.Range(-Mathf.Abs(trackingArgs.LaunchYawRange), Mathf.Abs(trackingArgs.LaunchYawRange));
            float pitch = UnityEngine.Random.Range(-Mathf.Abs(trackingArgs.LaunchPitchRange), Mathf.Abs(trackingArgs.LaunchPitchRange));

            Vector3 yawDirection = Quaternion.AngleAxis(yaw, Vector3.up) * normalizedDirection;
            Vector3 pitchAxis = Vector3.Cross(Vector3.up, yawDirection);
            if (pitchAxis.sqrMagnitude <= Mathf.Epsilon)
            {
                pitchAxis = Vector3.right;
            }

            Vector3 launchDirection = Quaternion.AngleAxis(pitch, pitchAxis.normalized) * yawDirection;
            return launchDirection.sqrMagnitude > Mathf.Epsilon ? launchDirection.normalized : normalizedDirection;
        }

        /// <summary>
        /// 为追踪抛物线计算可命中目标点的初速度。
        /// 水平速度由子弹速度决定，垂直速度按重力和飞行时间反推。
        /// </summary>
        /// <param name="origin">发射起点。</param>
        /// <param name="forward">默认搜索方向。</param>
        /// <param name="ignoredRoot">命中搜索时忽略的施法者根节点。</param>
        /// <param name="initialVelocity">计算出的初速度。</param>
        /// <returns>成功找到目标并计算速度时返回 true。</returns>
        private bool TryResolveHomingParabolaVelocity(Vector3 origin, Vector3 forward, Transform ignoredRoot, out Vector3 initialVelocity)
        {
            initialVelocity = default;
            if (_config == null || _config.SpawnArgs == null || _config.SpawnArgs.Tracking == null)
            {
                return false;
            }

            BulletTrackingArgs trackingArgs = _config.SpawnArgs.Tracking;
            if (!SkillTargetSelectionUtility.TrySelectBestTarget(
                    origin,
                    forward,
                    trackingArgs.SearchRange,
                    trackingArgs.SearchAngle,
                    _hitLayerMask,
                    trackingArgs.CenterWeight,
                    ignoredRoot,
                    out SkillWeightedTargetSelectionResult selectionResult))
            {
                return false;
            }

            Vector3 targetPoint = ResolveTargetPoint(selectionResult.Target);
            Vector3 toTarget = targetPoint - origin;
            if (toTarget.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            float planarSpeed = Mathf.Max(0.01f, _speed);
            Vector3 planarOffset = Vector3.ProjectOnPlane(toTarget, Vector3.up);
            float planarDistance = planarOffset.magnitude;
            float flightTime = planarDistance > 0.01f
                ? planarDistance / planarSpeed
                : Mathf.Max(0.05f, toTarget.magnitude / planarSpeed);
            if (flightTime <= Mathf.Epsilon)
            {
                return false;
            }

            Vector3 planarVelocity = planarDistance > 0.01f
                ? planarOffset / flightTime
                : Vector3.zero;
            float verticalVelocity = (toTarget.y + 0.5f * _gravity * flightTime * flightTime) / flightTime;
            initialVelocity = planarVelocity + Vector3.up * verticalVelocity;
            return true;
        }

        /// <summary>
        /// 获取目标的命中参考点。
        /// 优先使用 Collider bounds 中心，没有碰撞体时使用 Transform 位置。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <returns>目标参考点。</returns>
        private static Vector3 ResolveTargetPoint(GameUnit targetUnit)
        {
            if (targetUnit == null)
            {
                return Vector3.zero;
            }

            GameObject targetObject = targetUnit.gameObject;
            Collider collider = targetObject.GetComponent<Collider>();
            if (collider == null)
            {
                collider = targetObject.GetComponentInChildren<Collider>();
            }

            return collider != null ? collider.bounds.center : targetObject.transform.position;
        }

        /// <summary>
        /// Unity 每帧更新。
        /// 推进子弹位置、检测命中、处理生命周期超时和对象池回收。
        /// </summary>
        private void Update()
        {
            if (_config == null || _config.SpawnArgs == null)
            {
                Release();
                return;
            }

            _elapsedTime += Time.deltaTime;
            if (_elapsedTime >= _maxLifetime)
            {
                if (!_hasHitTarget)
                {
                    LogBulletDebug("Bullet.Timeout");
                }

                Release();
                return;
            }

            Vector3 currentPosition = transform.position;
            Vector3 nextPosition;
            if (_flightMode == BulletFlightMode.Parabola || _flightMode == BulletFlightMode.HomingParabola)
            {
                float deltaTime = Time.deltaTime;
                Vector3 gravityVector = Vector3.down * _gravity;
                nextPosition = currentPosition + _velocity * deltaTime + 0.5f * gravityVector * deltaTime * deltaTime;
                _velocity += gravityVector * deltaTime;
            }
            else if (_flightMode == BulletFlightMode.HomingCurve)
            {
                nextPosition = UpdateHomingCurve(currentPosition, Time.deltaTime);
            }
            else
            {
                nextPosition = currentPosition + transform.forward * (_speed * Time.deltaTime);
            }

            Vector3 delta = nextPosition - currentPosition;
            if (delta.sqrMagnitude > Mathf.Epsilon)
            {
                transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            }

            if (TryDetectHit(currentPosition, nextPosition, out GameUnitTargetInfo hitTargetInfo))
            {
                transform.position = nextPosition;
                _hasHitTarget = true;
                LogBulletDebug("Bullet.Hit");
                ApplyOnHitEffect(hitTargetInfo);
                Release();
                return;
            }

            transform.position = nextPosition;
        }

        /// <summary>
        /// 推进追踪曲线飞行。
        /// 远距离时带侧向/垂直曲线偏移，接近目标后切换为更直接的末端追踪。
        /// </summary>
        /// <param name="currentPosition">当前子弹位置。</param>
        /// <param name="deltaTime">本帧时间。</param>
        /// <returns>下一帧位置。</returns>
        private Vector3 UpdateHomingCurve(Vector3 currentPosition, float deltaTime)
        {
            BulletTrackingArgs trackingArgs = _config != null && _config.SpawnArgs != null ? _config.SpawnArgs.Tracking : null;
            if (trackingArgs == null)
            {
                return currentPosition + transform.forward * (_currentSpeed * deltaTime);
            }

            _currentSpeed = Mathf.Max(0.01f, _currentSpeed + Mathf.Max(0f, trackingArgs.Acceleration) * deltaTime);
            Vector3 targetPoint = ResolveTrackedTargetPoint();
            if (_trackingTarget == null)
            {
                _velocity = transform.forward * _currentSpeed;
                return currentPosition + _velocity * deltaTime;
            }

            Vector3 toTarget = targetPoint - currentPosition;
            float distanceToTarget = toTarget.magnitude;
            if (distanceToTarget <= Mathf.Epsilon)
            {
                return targetPoint;
            }

            float straightDistance = Mathf.Max(0.05f, trackingArgs.StraightDistance);
            bool useStraightTerminal = distanceToTarget <= straightDistance;
            float progress = _trackingStartDistance > 0.01f
                ? 1f - Mathf.Clamp01(distanceToTarget / _trackingStartDistance)
                : Mathf.Clamp01(_elapsedTime / Mathf.Max(0.01f, _maxLifetime));
            float arcBlend = useStraightTerminal ? 0f : Mathf.Sin(progress * Mathf.PI);
            Vector3 desiredPoint = targetPoint;
            if (!useStraightTerminal)
            {
                Vector3 currentDirection = _velocity.sqrMagnitude > Mathf.Epsilon ? _velocity.normalized : transform.forward;
                Vector3 curveAxis = _curveLateralAxis;
                if (curveAxis.sqrMagnitude <= Mathf.Epsilon)
                {
                    curveAxis = Vector3.Cross(Vector3.up, currentDirection).normalized;
                }

                float oscillation = Mathf.Max(0f, trackingArgs.CurveOscillation);
                float phase = _elapsedTime * oscillation + _curvePhase;
                float curveStrength = Mathf.Max(0f, trackingArgs.CurveStrength);
                Vector3 lateralOffset = curveAxis * (trackingArgs.CurveLateralOffset * _curveLateralScale * curveStrength * arcBlend);
                Vector3 verticalOffset = Vector3.up * (trackingArgs.CurveVerticalOffset * _curveVerticalScale * curveStrength * Mathf.Sin(phase) * arcBlend);
                desiredPoint += lateralOffset + verticalOffset;
            }

            Vector3 desiredDirection = desiredPoint - currentPosition;
            if (desiredDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                desiredDirection = toTarget;
            }

            Vector3 currentVelocityDirection = _velocity.sqrMagnitude > Mathf.Epsilon ? _velocity.normalized : transform.forward;
            Vector3 targetDirection = useStraightTerminal ? toTarget.normalized : desiredDirection.normalized;
            float steerStrength = useStraightTerminal
                ? 14f
                : Mathf.Lerp(1.5f, 7f, progress);
            Vector3 blendedDirection = Vector3.Slerp(currentVelocityDirection, targetDirection, Mathf.Clamp01(steerStrength * deltaTime));
            if (blendedDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                blendedDirection = targetDirection;
            }

            _velocity = blendedDirection.normalized * _currentSpeed;
            return currentPosition + _velocity * deltaTime;
        }

        /// <summary>
        /// 获取追踪目标的当前参考点。
        /// </summary>
        /// <returns>目标参考点；目标丢失时返回子弹前方一点。</returns>
        private Vector3 ResolveTrackedTargetPoint()
        {
            if (_trackingTarget == null)
            {
                return transform.position + transform.forward;
            }

            Collider collider = _trackingTarget.GetComponent<Collider>();
            if (collider == null)
            {
                collider = _trackingTarget.GetComponentInChildren<Collider>();
            }

            return collider != null ? collider.bounds.center : _trackingTarget.position;
        }

        /// <summary>
        /// 检测从当前位置到下一位置之间是否命中有效目标。
        /// 有半径时使用 SphereCast/OverlapSphere，否则使用 Raycast。
        /// </summary>
        /// <param name="currentPosition">当前位置。</param>
        /// <param name="nextPosition">下一位置。</param>
        /// <param name="hitTarget">命中的目标。</param>
        /// <returns>命中有效目标时返回 true。</returns>
        private bool TryDetectHit(Vector3 currentPosition, Vector3 nextPosition, out GameUnitTargetInfo hitTargetInfo)
        {
            hitTargetInfo = default;
            Vector3 delta = nextPosition - currentPosition;
            float distance = delta.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                if (_collisionRadius <= 0f)
                {
                    return false;
                }

                Collider[] overlaps = Physics.OverlapSphere(currentPosition, _collisionRadius, _hitLayerMask, QueryTriggerInteraction.Collide);
                for (int i = 0; i < overlaps.Length; i++)
                {
                    if (GameUnitResolver.TryResolve(overlaps[i], out GameUnitTargetInfo candidate) && IsValidHitTarget(candidate.Unit))
                    {
                        hitTargetInfo = candidate;
                        return true;
                    }
                }

                return false;
            }

            RaycastHit[] hits = _collisionRadius > 0f
                ? Physics.SphereCastAll(currentPosition, _collisionRadius, delta.normalized, distance, _hitLayerMask, QueryTriggerInteraction.Collide)
                : Physics.RaycastAll(currentPosition, delta.normalized, distance, _hitLayerMask, QueryTriggerInteraction.Collide);

            float nearestDistance = float.MaxValue;
            GameUnitTargetInfo nearestTargetInfo = default;
            for (int i = 0; i < hits.Length; i++)
            {
                if (!GameUnitResolver.TryResolve(hits[i], out GameUnitTargetInfo candidate) || !IsValidHitTarget(candidate.Unit))
                {
                    continue;
                }

                if (hits[i].distance < nearestDistance)
                {
                    nearestDistance = hits[i].distance;
                    nearestTargetInfo = candidate;
                }
            }

            hitTargetInfo = nearestTargetInfo;
            return hitTargetInfo.Unit != null;
        }

        /// <summary>
        /// 判断碰撞到的对象是否是有效目标。
        /// 会排除子弹自身层级以及施法者自身层级。
        /// </summary>
        /// <param name="targetObject">候选目标对象。</param>
        /// <returns>可以命中时返回 true。</returns>
        private bool IsValidHitTarget(GameUnit targetUnit)
        {
            if (targetUnit == null)
            {
                return false;
            }

            Transform targetTransform = targetUnit.transform;
            if (targetTransform == transform || targetTransform.IsChildOf(transform))
            {
                return false;
            }

            Transform casterTransform = StateTimelineExecutionRuntime.ExtractTransform(_context != null ? _context.Caster : null);
            if (casterTransform == null)
            {
                return true;
            }

            return targetTransform != casterTransform && !targetTransform.IsChildOf(casterTransform);
        }

        /// <summary>
        /// 对子弹命中的目标执行命中效果图。
        /// 执行期间会临时把 PrimaryTarget 切换为命中目标。
        /// </summary>
        /// <param name="targetInfo">命中的目标信息。</param>
        private void ApplyOnHitEffect(GameUnitTargetInfo targetInfo)
        {
            if (_context == null || targetInfo.Unit == null)
            {
                return;
            }

            GameUnit previousTarget = _context.PrimaryTarget;
            try
            {
                _context.PrimaryTarget = targetInfo.Unit;
                SkillEffectResult result = SkillEffectResult.None;
                if (_config != null && _config.OnHitEffect == null)
                {
                    LogBulletDebug("Bullet.OnHitEffectNull");
                }
                else if (_config != null && (string.IsNullOrEmpty(_config.OnHitEffect.RootNodeId) || _config.OnHitEffect.Nodes == null || _config.OnHitEffect.Nodes.Count == 0))
                {
                    LogBulletDebug("Bullet.OnHitEffectEmpty");
                }
                else if (_config != null && _config.OnHitEffect != null)
                {
                    LogBulletDebug("Bullet.OnHitEffectExecute");
                    result = ExecuteTimelineEffect(_config.OnHitEffect);
                }
            }
            finally
            {
                _context.PrimaryTarget = previousTarget;
            }
        }

        /// <summary>
        /// 清空运行时引用并归还到对象池。
        /// </summary>
        private void Release()
        {
            _config = null;
            _context = null;
            _releaseAction?.Invoke(this);
        }

        private SkillEffectResult ExecuteTimelineEffect(SkillEffectConfig effectConfig)
        {
            if (effectConfig == null || _context == null)
            {
                return SkillEffectResult.None;
            }

            if (_context.EffectExecutor == null)
            {
                return SkillEffectResult.None;
            }

            return _context.EffectExecutor.Execute(effectConfig, _context) ?? SkillEffectResult.None;
        }

        /// <summary>
        /// 输出子弹调试日志。
        /// </summary>
        /// <param name="stage">子弹阶段名称。</param>
        private void LogBulletDebug(string stage)
        {
            Debug.Log($"StateTimelineExecutionRuntime.{stage}: {(_config != null ? _config.BulletId : string.Empty)}");
        }
    }
}
