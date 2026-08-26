using System;
using UnityEngine;
using SkillEditor.Preview;

namespace AsiSkillEditor.RunTime
{
    [TimelineEventRuntime(typeof(SetGravity_TimelineEventData))]
    public sealed class SetGravity_TimelineEventRuntime : TimelineEventRuntimeBase
    {
        private readonly SetGravity_TimelineEventData _data;
        private MonoBehaviour _bridge;
        private bool _hasCapturedPreviousState;
        private bool _previousGravityEnabled;
        private Vector3 _previousGravity;

        public SetGravity_TimelineEventRuntime(TimelineEventConfig config) : base(config)
        {
            _data = mData as SetGravity_TimelineEventData;
        }

        protected override void OnBegin()
        {
            if (!EnsureEventData())
            {
                return;
            }

            CacheBridge();
            CapturePreviousGravityState();
            ApplyCurrentGravitySettings();
        }

        protected override void OnTick()
        {
            if (!EnsureEventData())
            {
                return;
            }

            CacheBridge();
            CapturePreviousGravityState();
            ApplyCurrentGravitySettings();
        }

        protected override void OnEnd(bool interrupted)
        {
            RestorePreviousGravityState();
        }

        protected override void OnTrigger()
        {
            if (!EnsureEventData())
            {
                return;
            }

            CacheBridge();
            CapturePreviousGravityState();
            ApplyCurrentGravitySettings();
            // Duration == 0 is a single-frame execution and should not leak persistent gravity state.
            RestorePreviousGravityState();
        }

        private void RestorePreviousGravityState()
        {
            if (!_hasCapturedPreviousState)
            {
                return;
            }

            CacheBridge();
            if (_bridge == null)
            {
                return;
            }

            InvokeSetGravityEnabled(_bridge, _previousGravityEnabled);
            InvokeSetGravity(_bridge, _previousGravity);
            _hasCapturedPreviousState = false;
        }

        private bool EnsureEventData()
        {
            if (_data == null || _data.Args == null)
            {
                throw new InvalidOperationException("SetGravity meta skill event data is invalid.");
            }

            if (mContext == null)
            {
                throw new InvalidOperationException("SkillContext is missing.");
            }
            return true;
        }

        private void CacheBridge()
        {
            if (_bridge != null)
            {
                return;
            }

            TryResolveBridge(mContext, out _bridge, out _);
        }

        private void CapturePreviousGravityState()
        {
            if (_bridge == null || _hasCapturedPreviousState)
            {
                return;
            }

            _previousGravityEnabled = InvokeIsGravityEnabled(_bridge);
            _previousGravity = InvokeGetGravity(_bridge);
            _hasCapturedPreviousState = true;
        }

        private void ApplyCurrentGravitySettings()
        {
            if (_bridge == null)
            {
                return;
            }

            InvokeSetGravityEnabled(_bridge, _data.Args.EnableGravity);
            if (_data.Args.OverrideGravityVector)
            {
                InvokeSetGravity(_bridge, _data.Args.Gravity);
            }
        }

        private static bool TryResolveBridge(SkillContext context, out MonoBehaviour bridge, out Transform casterTransform)
        {
            bridge = null;
            casterTransform = ExtractTransform(context != null ? context.Caster : null);
            if (casterTransform == null)
            {
                return false;
            }

            Component bridgeComponent = FindBridgeComponent(casterTransform);
            bridge = bridgeComponent as MonoBehaviour;
            return bridge != null;
        }

        private static Component FindBridgeComponent(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null && string.Equals(behaviour.GetType().Name, "SkillMotionBridge", StringComparison.Ordinal))
                {
                    return behaviour;
                }
            }

            return null;
        }

        private static Transform ExtractTransform(GameUnit target)
        {
            return target != null && target.UnitObject != null ? target.UnitObject.transform : null;
        }

        private static void InvokeSetGravityEnabled(MonoBehaviour bridge, bool enabled)
        {
            if (bridge == null)
            {
                return;
            }

            Type bridgeType = bridge.GetType();
            System.Reflection.MethodInfo method = bridgeType.GetMethod("SetGravityEnabled", new[] { typeof(bool) });
            method?.Invoke(bridge, new object[] { enabled });
        }

        private static void InvokeSetGravity(MonoBehaviour bridge, Vector3 gravity)
        {
            if (bridge == null)
            {
                return;
            }

            Type bridgeType = bridge.GetType();
            System.Reflection.MethodInfo method = bridgeType.GetMethod("SetGravity", new[] { typeof(Vector3) });
            method?.Invoke(bridge, new object[] { gravity });
        }

        private static bool InvokeIsGravityEnabled(MonoBehaviour bridge)
        {
            if (bridge == null)
            {
                return false;
            }

            Type bridgeType = bridge.GetType();
            System.Reflection.MethodInfo method = bridgeType.GetMethod("IsGravityEnabled", Type.EmptyTypes);
            if (method == null)
            {
                return false;
            }

            object result = method.Invoke(bridge, null);
            return result is bool value && value;
        }

        private static Vector3 InvokeGetGravity(MonoBehaviour bridge)
        {
            if (bridge == null)
            {
                return Physics.gravity;
            }

            Type bridgeType = bridge.GetType();
            System.Reflection.MethodInfo method = bridgeType.GetMethod("GetGravity", Type.EmptyTypes);
            if (method == null)
            {
                return Physics.gravity;
            }

            object result = method.Invoke(bridge, null);
            return result is Vector3 vector ? vector : Physics.gravity;
        }

        public override void Dispose()
        {
            _bridge = null;
            _hasCapturedPreviousState = false;
            _previousGravityEnabled = false;
            _previousGravity = Vector3.zero;
            base.Dispose();
        }
    }
}
