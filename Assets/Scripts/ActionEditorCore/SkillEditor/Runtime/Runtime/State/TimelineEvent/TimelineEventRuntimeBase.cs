using UnityEngine;

namespace AsiSkillEditor.RunTime
{
    public struct TimelineEventExecutionContext
    {
        public readonly float DeltaTime;
        public readonly float ElapsedTime;
        public readonly float TriggerTime;
        public readonly float Duration;

        public TimelineEventExecutionContext(float deltaTime, float elapsedTime, float triggerTime, float duration)
        {
            DeltaTime = deltaTime;
            ElapsedTime = elapsedTime;
            TriggerTime = triggerTime;
            Duration = duration;
        }

        public bool IsSingleFrame => Mathf.Approximately(Duration, 0f);

        public bool IsInRange
        {
            get
            {
                if (Mathf.Approximately(Duration, 0f))
                {
                    return Mathf.Approximately(ElapsedTime, 0f);
                }

                if (Duration < 0f)
                {
                    return ElapsedTime >= 0f;
                }

                return ElapsedTime >= 0f && ElapsedTime <= Duration;
            }
        }

        public float NormalizedTime
        {
            get
            {
                if (Duration <= 0f)
                {
                    return 1f;
                }

                return Mathf.Clamp01(ElapsedTime / Duration);
            }
        }
    }

    public abstract class TimelineEventRuntimeBase
    {
        protected TimelineEventConfig mConfig;
        protected TimelineEventData mData;
        protected SkillContext mContext;
        private TimelineEventExecutionContext _executionContext;

        public TimelineEventConfig Config => mConfig;
        public TimelineEventData Data => mData;
        public SkillContext Context => mContext;
        public TimelineEventExecutionContext ExecutionContext => _executionContext;

        protected TimelineEventRuntimeBase(TimelineEventConfig config)
        {
            mConfig = config;
            mData = config != null ? config.Data : null;
        }

        internal void BindContext(SkillContext context)
        {
            mContext = context;
            OnCreate();
        }

        protected virtual void OnCreate()
        {
        }

        public virtual void Dispose()
        {
            mConfig = null;
            mData = null;
            mContext = null;
        }

        public virtual void Begin()
        {
            _executionContext = CreateExecutionContext(0f, 0f);
            OnBegin();
        }

        public virtual void Tick(float deltaTime, float elapsedTime)
        {
            _executionContext = CreateExecutionContext(deltaTime, elapsedTime);
            OnTick();
        }

        public virtual void End(bool interrupted)
        {
            OnEnd(interrupted);
        }

        public virtual void Trigger()
        {
            _executionContext = CreateExecutionContext(0f, 0f);
            OnTrigger();
        }

        protected virtual void OnTrigger()
        {
            OnBegin();
            OnEnd(false);
        }

        protected virtual void OnBegin()
        {
        }

        protected virtual void OnTick()
        {
        }

        protected virtual void OnEnd(bool interrupted)
        {
        }

        private TimelineEventExecutionContext CreateExecutionContext(float deltaTime, float elapsedTime)
        {
            float triggerTime = mConfig != null ? mConfig.TriggerTime : 0f;
            float duration = mConfig != null ? mConfig.Duration : 0f;
            return new TimelineEventExecutionContext(deltaTime, elapsedTime, triggerTime, duration);
        }
    }
}
