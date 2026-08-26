using System;
using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    /// <summary>
    /// 按目标高度生成确定性的角色 Up 轴初速度，并主动脱离 KCC 地面吸附。
    /// 最终高度会在触发时读取单位属性，因此 Buff 修改可立即生效。
    /// </summary>
    [TimelineEventRuntime(typeof(LaunchByHeight_TimelineEventData))]
    public sealed class LaunchByHeight_TimelineEventRuntime : TimelineEventRuntimeBase
    {
        private readonly LaunchByHeight_TimelineEventData _data;

        public LaunchByHeight_TimelineEventRuntime(TimelineEventConfig config) : base(config)
        {
            _data = mData as LaunchByHeight_TimelineEventData;
        }

        protected override void OnTrigger()
        {
            if (_data == null || _data.Args == null)
            {
                throw new InvalidOperationException("LaunchByHeight timeline event data is invalid.");
            }

            if (!TimelineMotionBridgeUtility.TryResolveBridge(mContext, out SkillMotionBridge bridge, out _))
            {
                return;
            }

            float attributeBonus = 0f;
            if (_data.Args.UseHeightBonusAttribute && mContext?.Caster != null)
            {
                attributeBonus = mContext.Caster.GetAttribute(_data.Args.HeightBonusAttribute) * _data.Args.AttributeScale;
            }

            float targetHeight = Mathf.Max(0f, _data.Args.TargetHeight + attributeBonus);
            bridge.LaunchByHeight(targetHeight, Mathf.Max(0f, _data.Args.ForceUngroundDuration));
        }
    }
}
