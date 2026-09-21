using ActionEditor.CharacterMotion;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    [TimelineEventRuntime(typeof(RootMotionSteering_TimelineEventData))]
    public sealed class RootMotionSteering_TimelineEventRuntime : TimelineEventRuntimeBase
    {
        private readonly RootMotionSteering_TimelineEventData _data;
        private CustomCharacterController _controller;
        private int _steeringHandle;

        public RootMotionSteering_TimelineEventRuntime(TimelineEventConfig config) : base(config)
        {
            _data = mData as RootMotionSteering_TimelineEventData;
        }

        protected override void OnBegin()
        {
            if (_data == null || _data.Args == null || mContext == null || mContext.Caster == null)
            {
                return;
            }

            _controller = mContext.Caster.GetComponent<CustomCharacterController>() ??
                mContext.Caster.GetComponentInChildren<CustomCharacterController>(true);
            if (_controller == null)
            {
                return;
            }

            RootMotionSteeringEventArgs args = _data.Args;
            _steeringHandle = _controller.BeginRootMotionSteering(
                args.TranslationWeightFrom,
                args.RotationWeightFrom,
                args.TranslationSteeringSpeed,
                args.RotationSteeringSpeed);
        }

        protected override void OnTick()
        {
            if (_controller == null || _steeringHandle <= 0 || _data == null || _data.Args == null)
            {
                return;
            }

            RootMotionSteeringEventArgs args = _data.Args;
            float normalizedTime = ExecutionContext.NormalizedTime;
            _controller.UpdateRootMotionSteering(
                _steeringHandle,
                Mathf.Lerp(args.TranslationWeightFrom, args.TranslationWeightTo, normalizedTime),
                Mathf.Lerp(args.RotationWeightFrom, args.RotationWeightTo, normalizedTime),
                args.TranslationSteeringSpeed,
                args.RotationSteeringSpeed);
        }

        protected override void OnEnd(bool interrupted)
        {
            ReleaseSteering();
        }

        public override void Dispose()
        {
            ReleaseSteering();
            base.Dispose();
        }

        private void ReleaseSteering()
        {
            if (_controller != null && _steeringHandle > 0)
            {
                _controller.EndRootMotionSteering(_steeringHandle);
            }

            _controller = null;
            _steeringHandle = 0;
        }
    }

    [TimelineEventRuntime(typeof(RotationModeOverride_TimelineEventData))]
    public sealed class RotationModeOverride_TimelineEventRuntime : TimelineEventRuntimeBase
    {
        private readonly RotationModeOverride_TimelineEventData _data;
        private CustomCharacterController _controller;
        private int _overrideHandle;

        public RotationModeOverride_TimelineEventRuntime(TimelineEventConfig config) : base(config)
        {
            _data = mData as RotationModeOverride_TimelineEventData;
        }

        protected override void OnBegin()
        {
            if (_data == null || _data.Args == null || mContext == null || mContext.Caster == null)
            {
                return;
            }

            _controller = mContext.Caster.GetComponent<CustomCharacterController>() ??
                mContext.Caster.GetComponentInChildren<CustomCharacterController>(true);
            if (_controller != null)
            {
                _overrideHandle = _controller.BeginRotationModeOverride(
                    _data.Args.RotationMode,
                    _data.Args.BlendDuration,
                    _data.Args.DirectionTurnSpeed);
            }
        }

        protected override void OnEnd(bool interrupted)
        {
            ReleaseOverride();
        }

        public override void Dispose()
        {
            ReleaseOverride();
            base.Dispose();
        }

        private void ReleaseOverride()
        {
            if (_controller != null && _overrideHandle > 0)
            {
                _controller.EndRotationModeOverride(_overrideHandle);
            }

            _controller = null;
            _overrideHandle = 0;
        }
    }
}