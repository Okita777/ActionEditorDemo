using SkillEditor.Preview;

namespace AsiSkillEditor.RunTime
{
    public interface IBuffService
    {
        bool HasBuff(GameUnit target, string buffId);
        void AddBuff(GameUnit target, BuffActionArgs args, SkillContext context);
        void RemoveBuff(GameUnit target, BuffActionArgs args, SkillContext context);
    }
}
