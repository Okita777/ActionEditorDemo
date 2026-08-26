using System;
using SkillEditor.Preview;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    [TimelineEventRuntime(typeof(HitStop_TimelineEventData))]
    public sealed class HitStop_TimelineEventRuntime : TimelineEventRuntimeBase
    {
        private readonly HitStop_TimelineEventData _data;
        private IUnitHitEventSource _hitEventSource;
        private bool _isSubscribed;
        private bool _hasTriggered;
        private int _lastMergedFrame = -1;

        public HitStop_TimelineEventRuntime(TimelineEventConfig config) : base(config)
        {
            _data = mData as HitStop_TimelineEventData;
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
                PublishTrace("HitStopEvent.InvalidZeroDuration", "OnHit requires Duration > 0 or Duration < 0.");
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
                throw new InvalidOperationException("HitStop timeline event data is invalid.");
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
            PublishTrace("HitStopEvent.Subscribed", string.Empty);
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

            _hasTriggered = true;
            _lastMergedFrame = hitEvent.Frame;
            SubmitRequests(hitEvent.Attacker, hitEvent.Defender, hitEvent.Frame);
            PublishTrace("HitStopEvent.Triggered", hitEvent.HitBoxId);
        }

        private void TriggerImmediate()
        {
            GameUnit attacker = mContext != null ? mContext.Caster : null;
            GameUnit defender = mContext != null ? mContext.PrimaryTarget : null;
            SubmitRequests(attacker, defender, Time.frameCount);
            _hasTriggered = true;
            PublishTrace("HitStopEvent.Triggered", "Immediate");
        }

        private void SubmitRequests(GameUnit attacker, GameUnit defender, int frame)
        {
            IUnitHitStopService service = mContext != null ? mContext.HitStopService : null;
            if (service == null)
            {
                return;
            }

            string sourceId = mConfig != null ? mConfig.EventId : string.Empty;
            if (_data.Args.AffectAttacker && attacker != null)
            {
                service.Request(new HitStopRequest(
                    attacker,
                    Mathf.Max(0f, _data.Args.AttackerDuration),
                    Mathf.Clamp01(_data.Args.AttackerTimeScale),
                    _data.Args.Priority,
                    sourceId + ":Attacker",
                    frame));
            }

            if (_data.Args.AffectDefender && defender != null)
            {
                IUnitHitStopService defenderService = UnitHitStopService.ResolveOrCreate(defender);
                defenderService?.Request(new HitStopRequest(
                    defender,
                    Mathf.Max(0f, _data.Args.DefenderDuration),
                    Mathf.Clamp01(_data.Args.DefenderTimeScale),
                    _data.Args.Priority,
                    sourceId + ":Defender",
                    frame));
            }
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
