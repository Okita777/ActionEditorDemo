using UnityEngine;
using SkillEditor.Preview;

namespace AsiSkillEditor.RunTime
{
    internal static class TimelineMotionBridgeUtility
    {
        public static Transform ExtractTransform(GameUnit target)
        {
            return target != null && target.UnitObject != null ? target.UnitObject.transform : null;
        }

        public static bool TryResolveBridge(SkillContext context, out SkillMotionBridge bridge, out Transform casterTransform)
        {
            bridge = null;
            casterTransform = ExtractTransform(context != null ? context.Caster : null);
            if (casterTransform == null)
            {
                return false;
            }

            bridge = casterTransform.GetComponent<SkillMotionBridge>();
            if (bridge == null)
            {
                bridge = casterTransform.GetComponentInChildren<SkillMotionBridge>();
            }

            return bridge != null;
        }
    }
}
