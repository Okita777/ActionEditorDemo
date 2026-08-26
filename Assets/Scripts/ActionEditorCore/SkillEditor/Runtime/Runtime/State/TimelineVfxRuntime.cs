using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    internal sealed class TimelineVfxRuntime
    {
        private readonly TimelineVfxConfig _config;
        private readonly SkillContext _context;
        private IVfxService _service;
        private IVfxPlayback _playback;

        public TimelineVfxRuntime(TimelineVfxConfig config, SkillContext context)
        {
            _config = config;
            _context = context ?? new SkillContext();
        }

        public void Begin()
        {
            if (_config == null || string.IsNullOrEmpty(_config.PrefabPath))
            {
                return;
            }

            GameObject prefab = FeedbackAssetRuntimeCatalog.LoadVfxPrefab(_config.PrefabPath);
            if (prefab == null)
            {
                Trace("TimelineVfx.ResourceMissing");
                return;
            }

            if (!StateTimelineExecutionRuntime.TryResolveSocketTransform(
                    _context, _config.SocketSource, _config.AttachPoint, out Transform socket))
            {
                Trace("TimelineVfx.SocketMissing");
                return;
            }

            Vector3 position = socket.TransformPoint(_config.PositionOffset);
            Quaternion rotation = socket.rotation * Quaternion.Euler(_config.RotationOffset);
            bool follow = _config.FollowMode == TimelineFollowMode.FollowSocket;
            float safetyTimeout = _config.Mode == TimelineVfxMode.Controlled
                ? Mathf.Max(86400f, _config.Duration + _config.TailTimeout + 1f)
                : Mathf.Max(0.01f, _config.TailTimeout);

            _service = _context.VfxService ?? GameFeedbackServiceHost.Instance.Vfx;
            _context.VfxService = _service;
            _playback = _service?.Play(new VfxPlayArgs(
                prefab,
                follow ? VfxPlaySpace.FollowTarget : VfxPlaySpace.World,
                position,
                rotation,
                _config.Scale,
                follow ? socket : null,
                follow ? (Vector3)_config.PositionOffset : Vector3.zero,
                follow ? Quaternion.Euler(_config.RotationOffset) : Quaternion.identity,
                safetyTimeout,
                _config.UseUnscaledTime,
                _config.Mode == TimelineVfxMode.OneShot));

            Trace("TimelineVfx.Triggered");
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool interrupted)
        {
            if (_config == null || _config.Mode != TimelineVfxMode.Controlled || _service == null || _playback == null)
            {
                return;
            }

            VfxStopBehavior behavior = _config.StopMode == TimelineVfxStopMode.StopAndClear
                ? VfxStopBehavior.StopAndClear
                : VfxStopBehavior.StopEmitting;
            _service.Stop(_playback, behavior, Mathf.Max(0.01f, _config.TailTimeout));
            _playback = null;
            Trace(interrupted ? "TimelineVfx.Interrupted" : "TimelineVfx.Stopped");
        }

        private void Trace(string type)
        {
            SkillRuntimeDebugBus.PublishTrace(_context, new SkillRuntimeTraceEvent
            {
                TraceType = type,
                MetaSkillId = _context.CurrentMetaSkillConfig != null ? _context.CurrentMetaSkillConfig.MetaSkillId : string.Empty,
                PayloadId = _config != null ? _config.VfxId : string.Empty,
                Time = _context.DebugTimelineTime,
                Message = _config != null ? _config.DisplayName : string.Empty,
            });
        }
    }
}
