using System;
using ActionEditor.CameraSystem;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    [TimelineEventRuntime(typeof(CameraShake_TimelineEventData))]
    public sealed class CameraShake_TimelineEventRuntime : TimelineEventRuntimeBase
    {
        private readonly CameraShake_TimelineEventData _data;
        private IUnitHitEventSource _hitEventSource;
        private bool _isSubscribed;
        private bool _hasTriggered;
        private int _lastMergedFrame = -1;

        public CameraShake_TimelineEventRuntime(TimelineEventConfig config) : base(config)
        {
            _data = mData as CameraShake_TimelineEventData;
        }

        protected override void OnBegin()
        {
            EnsureData();
            _hasTriggered = false;
            _lastMergedFrame = -1;
            if (_data.Args.TriggerMode == FeedbackTriggerMode.Immediate)
            {
                TriggerImmediate();
            }
            else
            {
                Subscribe();
            }
        }

        protected override void OnEnd(bool interrupted)
        {
            Unsubscribe();
        }

        protected override void OnTrigger()
        {
            EnsureData();
            _hasTriggered = false;
            _lastMergedFrame = -1;
            if (_data.Args.TriggerMode == FeedbackTriggerMode.Immediate)
            {
                TriggerImmediate();
            }
            else
            {
                PublishTrace("CameraShakeEvent.InvalidZeroDuration", "OnHit requires Duration > 0 or Duration < 0.");
            }
        }

        public override void Dispose()
        {
            Unsubscribe();
            base.Dispose();
        }

        private void EnsureData()
        {
            if (_data == null || _data.Args == null)
            {
                throw new InvalidOperationException("CameraShake timeline event data is invalid.");
            }

            if (mContext == null)
            {
                throw new InvalidOperationException("SkillContext is missing.");
            }
        }

        private void Subscribe()
        {
            if (_isSubscribed || mContext == null || mContext.UnitHitEventSource == null)
            {
                return;
            }

            _hitEventSource = mContext.UnitHitEventSource;
            _hitEventSource.HitConfirmed += OnHitConfirmed;
            _isSubscribed = true;
            PublishTrace("CameraShakeEvent.Subscribed", string.Empty);
        }

        private void Unsubscribe()
        {
            if (_isSubscribed && _hitEventSource != null)
            {
                _hitEventSource.HitConfirmed -= OnHitConfirmed;
            }

            _hitEventSource = null;
            _isSubscribed = false;
        }

        private void OnHitConfirmed(UnitHitEvent hitEvent)
        {
            if (!hitEvent.Resolution.IsValid ||
                (_data.Args.TriggerOncePerEvent && _hasTriggered) ||
                (_data.Args.MergeSameFrameHits && _lastMergedFrame == hitEvent.Frame))
            {
                return;
            }

            Vector3 position = hitEvent.HasHitPoint
                ? hitEvent.HitPoint
                : hitEvent.Defender != null ? hitEvent.Defender.transform.position : Vector3.zero;
            Vector3 direction = _data.Args.UseHitDirection && hitEvent.HitDirection.sqrMagnitude > 0.000001f
                ? hitEvent.HitDirection
                : (Vector3)_data.Args.Direction;
            SubmitRequest(position, direction, hitEvent.Frame);
            _hasTriggered = true;
            _lastMergedFrame = hitEvent.Frame;
            PublishTrace("CameraShakeEvent.Triggered", hitEvent.HitBoxId);
        }

        private void TriggerImmediate()
        {
            Vector3 position = mContext.Caster != null ? mContext.Caster.transform.position : Vector3.zero;
            SubmitRequest(position, _data.Args.Direction, Time.frameCount);
            _hasTriggered = true;
            PublishTrace("CameraShakeEvent.Triggered", "Immediate");
        }

        private void SubmitRequest(Vector3 position, Vector3 direction, int frame)
        {
            ICameraFeedbackService service = mContext != null ? mContext.CameraFeedbackService : null;
            if (service == null && mContext != null && mContext.Caster != null)
            {
                service = CameraFeedbackService.ResolveForLocalPlayer(mContext.Caster);
                mContext.CameraFeedbackService = service;
            }
            if (service == null)
            {
                PublishTrace("CameraShakeEvent.NoCameraService", string.Empty);
                return;
            }

            service.RequestShake(new CameraShakeRequest(
                position,
                direction,
                Mathf.Max(0f, _data.Args.Amplitude),
                Mathf.Max(0.01f, _data.Args.Frequency),
                Mathf.Max(0f, _data.Args.ShakeDuration),
                mConfig != null ? mConfig.EventId : string.Empty,
                frame));
        }

        private void PublishTrace(string traceType, string message)
        {
            SkillRuntimeDebugBus.PublishTrace(mContext, new SkillRuntimeTraceEvent
            {
                TraceType = traceType,
                MetaSkillId = mContext != null && mContext.CurrentMetaSkillConfig != null
                    ? mContext.CurrentMetaSkillConfig.MetaSkillId
                    : string.Empty,
                PayloadId = mConfig != null ? mConfig.EventId : string.Empty,
                Time = mContext != null ? mContext.DebugTimelineTime : 0f,
                Message = message ?? string.Empty,
            });
        }
    }
}
