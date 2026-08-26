using System;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    [TimelineEventRuntime(typeof(HitVfx_TimelineEventData))]
    public sealed class HitVfx_TimelineEventRuntime : TimelineEventRuntimeBase
    {
        private readonly HitVfx_TimelineEventData _data;
        private IUnitHitEventSource _source;
        private bool _subscribed;
        private bool _triggered;
        private int _lastFrame = -1;

        public HitVfx_TimelineEventRuntime(TimelineEventConfig config) : base(config) => _data = mData as HitVfx_TimelineEventData;

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
            else Trace("HitVfx.InvalidZeroDuration", "OnHit requires Duration > 0 or Duration < 0.");
        }

        public override void Dispose()
        {
            Unsubscribe();
            base.Dispose();
        }

        private void EnsureData()
        {
            if (_data?.Args == null) throw new InvalidOperationException("HitVfx timeline event data is invalid.");
            if (mContext == null) throw new InvalidOperationException("SkillContext is missing.");
        }

        private void Subscribe()
        {
            if (_subscribed || mContext.UnitHitEventSource == null) return;
            _source = mContext.UnitHitEventSource;
            _source.HitConfirmed += OnHit;
            _subscribed = true;
            Trace("HitVfx.Subscribed", string.Empty);
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
            Play(hit.HitPoint, hit.HasHitPoint, hit.HitNormal, hit.HasHitNormal,
                hit.HitDirection, hit.Attacker, hit.Defender);
            _triggered = true;
            _lastFrame = hit.Frame;
            Trace("HitVfx.Triggered", hit.HitBoxId);
        }

        private void TriggerImmediate()
        {
            Transform caster = mContext.Caster != null ? mContext.Caster.transform : null;
            Play(caster != null ? caster.position : Vector3.zero, caster != null, Vector3.zero, false,
                caster != null ? caster.forward : Vector3.forward, mContext.Caster, mContext.PrimaryTarget);
            _triggered = true;
            Trace("HitVfx.Triggered", "Immediate");
        }

        private void Play(Vector3 hitPoint, bool hasPoint, Vector3 hitNormal, bool hasNormal,
            Vector3 hitDirection, SkillEditor.Preview.GameUnit attacker, SkillEditor.Preview.GameUnit defender)
        {
            GameObject prefab = FeedbackAssetRuntimeCatalog.LoadVfxPrefab(_data.Args.PrefabPath);
            if (prefab == null)
            {
                Trace("HitVfx.ResourceMissing", _data.Args.PrefabPath);
                return;
            }

            IVfxService service = mContext.VfxService ?? GameFeedbackServiceHost.Instance.Vfx;
            mContext.VfxService = service;
            Transform follow = defender != null ? defender.transform : null;
            Vector3 position = hasPoint ? hitPoint : follow != null ? follow.position : attacker != null ? attacker.transform.position : Vector3.zero;
            Vector3 forward = ResolveForward(hitNormal, hasNormal, hitDirection, attacker, defender);
            Quaternion rotation = _data.Args.RotationMode == HitVfxRotationMode.Identity
                ? Quaternion.identity
                : Quaternion.LookRotation(forward, Vector3.up);
            rotation *= Quaternion.Euler(_data.Args.RotationOffset);
            Vector3 offset = _data.Args.PositionOffset;
            position += rotation * offset;
            bool shouldFollow = _data.Args.Space == VfxPlaySpace.FollowTarget && follow != null;
            Vector3 localPosition = shouldFollow ? follow.InverseTransformPoint(position) : Vector3.zero;
            Quaternion localRotation = shouldFollow ? Quaternion.Inverse(follow.rotation) * rotation : Quaternion.identity;
            service?.Play(new VfxPlayArgs(prefab, shouldFollow ? VfxPlaySpace.FollowTarget : VfxPlaySpace.World,
                position, rotation, _data.Args.Scale, follow, localPosition, localRotation,
                Mathf.Max(0.01f, _data.Args.Lifetime), _data.Args.UseUnscaledTime));
        }

        private Vector3 ResolveForward(Vector3 normal, bool hasNormal, Vector3 direction,
            SkillEditor.Preview.GameUnit attacker, SkillEditor.Preview.GameUnit defender)
        {
            Vector3 forward;
            switch (_data.Args.RotationMode)
            {
                case HitVfxRotationMode.HitNormal: forward = hasNormal ? normal : -direction; break;
                case HitVfxRotationMode.OppositeHitDirection: forward = -direction; break;
                case HitVfxRotationMode.AttackerForward: forward = attacker != null ? attacker.transform.forward : Vector3.forward; break;
                case HitVfxRotationMode.DefenderForward: forward = defender != null ? defender.transform.forward : Vector3.forward; break;
                default: forward = Vector3.forward; break;
            }
            return forward.sqrMagnitude > 0.000001f ? forward.normalized : Vector3.up;
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
