using System;
using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    public abstract class SkillActionRuntimeBase
    {
        protected SkillActionConfig mConfig;
        protected SkillActionData mData;
        protected SkillContext mContext;
        private readonly Stack<SkillContext> _contextStack = new Stack<SkillContext>();

        public SkillActionConfig Config => mConfig;
        public SkillActionData Data => mData;
        public SkillContext Context => mContext;

        protected SkillActionRuntimeBase(SkillActionConfig config)
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

        internal SkillEffectResult ExecuteWithContext(SkillContext context, SkillEffectResult lastResult)
        {
            BindContext(context);
            try
            {
                return Execute(lastResult);
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

        public abstract SkillEffectResult Execute(SkillEffectResult lastResult);
    }
}
