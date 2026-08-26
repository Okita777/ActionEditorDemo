using System;
using UnityEngine;
using SkillEditor.Preview;

namespace AsiSkillEditor.RunTime
{
    [TimelineEventRuntime(typeof(ApplyForce_TimelineEventData))]
    public sealed class ApplyForce_TimelineEventRuntime : TimelineEventRuntimeBase
    {
        private readonly ApplyForce_TimelineEventData _data;
        private MonoBehaviour _bridge;
        private Transform _casterTransform;

        public ApplyForce_TimelineEventRuntime(TimelineEventConfig config) : base(config)
        {
            _data = mData as ApplyForce_TimelineEventData;
        }

        protected override void OnBegin()
        {
            CacheBridge();
        }

        protected override void OnTick()
        {
            if (!EnsureEventData())
            {
                return;
            }

            CacheBridge();
            if (_bridge == null)
            {
                return;
            }

            float deltaTime = Mathf.Max(0f, ExecutionContext.DeltaTime);
            if (deltaTime <= 0f)
            {
                return;
            }

            // Continuous duration force is integrated as per-frame impulse.
            ApplyResolvedForceAsImpulse(deltaTime);
        }

        protected override void OnTrigger()
        {
            if (!EnsureEventData())
            {
                return;
            }

            CacheBridge();
            if (_bridge == null)
            {
                return;
            }

            ApplyResolvedForceAsImpulse(1f);
        }

        private bool EnsureEventData()
        {
            if (_data == null || _data.Args == null)
            {
                throw new InvalidOperationException("ApplyForce meta skill event data is invalid.");
            }

            if (mContext == null)
            {
                throw new InvalidOperationException("SkillContext is missing.");
            }
            return true;
        }

        private void CacheBridge()
        {
            if (_bridge != null && _casterTransform != null)
            {
                return;
            }

            TryResolveBridge(mContext, out _bridge, out _casterTransform);
        }

        private void ApplyResolvedForceAsImpulse(float scale)
        {
            if (_bridge == null || _data == null || _data.Args == null)
            {
                return;
            }

            Vector3 force = _data.Args.Force;
            if (_data.Args.UseLocalSpace && _casterTransform != null)
            {
                force = _casterTransform.TransformDirection(force);
            }

            InvokeApplyImpulse(_bridge, force * Mathf.Max(0f, scale));
        }

        public override void Dispose()
        {
            _bridge = null;
            _casterTransform = null;
            base.Dispose();
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

        private static void InvokeApplyImpulse(MonoBehaviour bridge, Vector3 force)
        {
            if (bridge == null)
            {
                return;
            }

            Type bridgeType = bridge.GetType();
            System.Reflection.MethodInfo method = bridgeType.GetMethod("ApplyImpulse", new[] { typeof(Vector3), typeof(string) });
            if (method != null)
            {
                method.Invoke(bridge, new object[] { force, "SkillEvent.ApplyForce" });
                return;
            }

            method = bridgeType.GetMethod("ApplyImpulse", new[] { typeof(Vector3) });
            method?.Invoke(bridge, new object[] { force });
        }
    }
}
