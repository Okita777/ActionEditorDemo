namespace AsiSkillEditor.RunTime
{
    [SkillConditionRuntime(typeof(AttributeCompare_SkillConditionData))]
    public sealed class AttributeCompare_SkillConditionRuntime : SkillConditionRuntimeBase
    {
        private readonly AttributeCompare_SkillConditionData _data;

        public AttributeCompare_SkillConditionRuntime(SkillConditionConfig config) : base(config)
        {
            _data = mData as AttributeCompare_SkillConditionData;
        }

        public override bool Evaluate(SkillEffectResult lastResult)
        {
            if (_data == null || _data.Args == null || mContext == null)
            {
                return false;
            }

            SkillEditor.Preview.GameUnit target = SkillTargetResolver.Resolve(_data.Args.QueryTarget, mContext);
            if (target == null)
            {
                return false;
            }

            float currentValue = target.GetAttribute(_data.Args.AttributeType);

            switch (_data.Args.CompareOperator)
            {
                case SkillCompareOperator.Equal:
                    return currentValue == _data.Args.Value;
                case SkillCompareOperator.NotEqual:
                    return currentValue != _data.Args.Value;
                case SkillCompareOperator.Greater:
                    return currentValue > _data.Args.Value;
                case SkillCompareOperator.GreaterOrEqual:
                    return currentValue >= _data.Args.Value;
                case SkillCompareOperator.Less:
                    return currentValue < _data.Args.Value;
                case SkillCompareOperator.LessOrEqual:
                    return currentValue <= _data.Args.Value;
                default:
                    return false;
            }
        }
    }
}
