using System;
using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    public abstract class SkillConditionRuntimeBase
    {
        protected SkillConditionConfig mConfig;
        protected SkillConditionData mData;
        protected SkillContext mContext;
        private readonly Stack<SkillContext> _contextStack = new Stack<SkillContext>();

        public SkillConditionConfig Config => mConfig;
        public SkillConditionData Data => mData;
        public SkillContext Context => mContext;

        protected SkillConditionRuntimeBase(SkillConditionConfig config)
        {
            mConfig = config;
            mData = config != null ? config.Data : null;
        }

        internal void BindContext(SkillContext context)
        {
            _contextStack.Push(mContext);
            mContext = context;
            OnCreate();
        }

        internal void UnbindContext()
        {
            mContext = _contextStack.Count > 0 ? _contextStack.Pop() : null;
        }

        internal bool EvaluateWithContext(SkillContext context, SkillEffectResult lastResult)
        {
            BindContext(context);
            try
            {
                return Evaluate(lastResult);
            }
            finally
            {
                UnbindContext();
            }
        }

        protected virtual void OnCreate()
        {
        }

        public virtual void Dispose()
        {
            mContext = null;
            _contextStack.Clear();
            mConfig = null;
            mData = null;
        }

        public abstract bool Evaluate(SkillEffectResult lastResult);
    }
}
