using System;
using UnityEngine;
using UnityEngine.Audio;

namespace AsiSkillEditor.RunTime
{
    [TimelineEventRuntime(typeof(HitAudio_TimelineEventData))]
    public sealed class HitAudio_TimelineEventRuntime : TimelineEventRuntimeBase
    {
        private readonly HitAudio_TimelineEventData _data;
        private IUnitHitEventSource _source;
        private bool _subscribed;
        private bool _triggered;
        private int _lastFrame = -1;

        public HitAudio_TimelineEventRuntime(TimelineEventConfig config) : base(config) => _data = mData as HitAudio_TimelineEventData;

        protected override void OnBegin()
        {
            EnsureData();
            _triggered = false;
            _lastFrame = -1;
            if (_data.Args.TriggerMode == FeedbackTriggerMode.Immediate) TriggerImmediate();
            else Subscribe();
        }

        protected override void OnEnd(bool interrupted) => Unsubscribe();

        protected override void OnTrigger()
        {
            EnsureData();
            _triggered = false;
            _lastFrame = -1;
            if (_data.Args.TriggerMode == FeedbackTriggerMode.Immediate) TriggerImmediate();
            else Trace("HitAudio.InvalidZeroDuration", "OnHit requires Duration > 0 or Duration < 0.");
        }

        public override void Dispose()
        {
            Unsubscribe();
            base.Dispose();
        }

        private void EnsureData()
        {
            if (_data?.Args == null) throw new InvalidOperationException("HitAudio timeline event data is invalid.");
            if (mContext == null) throw new InvalidOperationException("SkillContext is missing.");
        }

        private void Subscribe()
        {
            if (_subscribed || mContext.UnitHitEventSource == null) return;
            _source = mContext.UnitHitEventSource;
            _source.HitConfirmed += OnHit;
            _subscribed = true;
            Trace("HitAudio.Subscribed", string.Empty);
        }

        private void Unsubscribe()
        {
            if (_subscribed && _source != null) _source.HitConfirmed -= OnHit;
            _source = null;
            _subscribed = false;
        }

        private void OnHit(UnitHitEvent hit)
        {
            if (!hit.Resolution.IsValid || (_data.Args.TriggerOncePerEvent && _triggered) ||
                (_data.Args.MergeSameFrameHits && _lastFrame == hit.Frame)) return;
            Play(hit.HitPoint, hit.HasHitPoint, hit.Attacker, hit.Defender);
            _triggered = true;
            _lastFrame = hit.Frame;
            Trace("HitAudio.Triggered", hit.HitBoxId);
        }

        private void TriggerImmediate()
        {
            Transform caster = mContext.Caster != null ? mContext.Caster.transform : null;
            Play(caster != null ? caster.position : Vector3.zero, caster != null, mContext.Caster, mContext.PrimaryTarget);
            _triggered = true;
            Trace("HitAudio.Triggered", "Immediate");
        }

        private void Play(Vector3 hitPoint, bool hasPoint, SkillEditor.Preview.GameUnit attacker, SkillEditor.Preview.GameUnit defender)
        {
            AudioClip clip = FeedbackAssetRuntimeCatalog.LoadAudioClip(_data.Args.AudioClipPath);
            if (clip == null)
            {
                Trace("HitAudio.ResourceMissing", _data.Args.AudioClipPath);
                return;
            }

            AudioMixerGroup mixer = FeedbackAssetRuntimeCatalog.LoadMixerGroup(_data.Args.AudioMixerPath, _data.Args.MixerGroupName);
            IAudioService service = mContext.AudioService ?? GameFeedbackServiceHost.Instance.Audio;
            mContext.AudioService = service;
            Transform follow = defender != null ? defender.transform : null;
            Vector3 position = hasPoint ? hitPoint : follow != null ? follow.position : attacker != null ? attacker.transform.position : Vector3.zero;
            bool shouldFollow = _data.Args.Space == AudioPlaySpace.FollowTarget && follow != null;
            service?.Play(new AudioPlayArgs(clip, mixer,
                shouldFollow ? AudioPlaySpace.FollowTarget : _data.Args.Space,
                position, follow, Mathf.Max(0f, _data.Args.Volume),
                Mathf.Clamp(_data.Args.Pitch, 0.01f, 3f), Mathf.Clamp01(_data.Args.SpatialBlend),
                Mathf.Max(0.01f, _data.Args.MinDistance), Mathf.Max(_data.Args.MinDistance, _data.Args.MaxDistance)));
        }

        private void Trace(string type, string message)
        {
            SkillRuntimeDebugBus.PublishTrace(mContext, new SkillRuntimeTraceEvent
            {
                TraceType = type,
                MetaSkillId = mContext?.CurrentMetaSkillConfig?.MetaSkillId ?? string.Empty,
                PayloadId = mConfig?.EventId ?? string.Empty,
                Time = mContext?.DebugTimelineTime ?? 0f,
                Message = message ?? string.Empty,
            });
        }
    }
}
