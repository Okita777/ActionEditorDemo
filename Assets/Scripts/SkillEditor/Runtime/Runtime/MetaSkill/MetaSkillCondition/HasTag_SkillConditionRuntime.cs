using ActionEditor.TagSystem;
using SkillEditor.Preview;

namespace AsiSkillEditor.RunTime
{
    [SkillConditionRuntime(typeof(HasTag_SkillConditionData))]
    public sealed class HasTag_SkillConditionRuntime : SkillConditionRuntimeBase
    {
        private readonly HasTag_SkillConditionData _data;

        public HasTag_SkillConditionRuntime(SkillConditionConfig config) : base(config)
        {
            _data = mData as HasTag_SkillConditionData;
        }

        public override bool Evaluate(SkillEffectResult lastResult)
        {
            if (_data == null || _data.Args == null || mContext == null || mContext.TagQueryService == null)
            {
                UnityEngine.Debug.LogWarning("[AICode] HasTag_SkillConditionRuntime: context/data/tag service is missing.");
                return false;
            }

            GameUnit target = SkillTargetResolver.Resolve(_data.Args.QueryTarget, mContext);
            bool result = target != null && mContext.TagQueryService.HasTag(target, _data.Args.Tag);
            UnityEngine.Debug.Log($"[AICode] HasTag_SkillConditionRuntime: queryTarget={_data.Args.QueryTarget}, target='{(target != null ? target.name : "null")}', tag='{_data.Args.Tag}', result={result}.", target);
            return result;
        }
    }
}
