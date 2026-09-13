using System.Collections.Generic;

namespace AsiActionEngine.RunTime
{
    public partial class ActionStateMachine
    {
        // 防环 visited 集合: 单线程游戏循环下静态复用,入口处 Clear
        private static readonly HashSet<ActionEngine_Unit> sFanoutVisited = new HashSet<ActionEngine_Unit>(32);

        /// <summary>
        /// initiator 命中其它单位后,在其 m_SkillDic 子树上扇出 InterruptList_OnHit 跳转。
        /// </summary>
        private void FanoutOnHit_ToSkills(ActionEngine_Unit initiator)
        {
            if (initiator == null) return;
            sFanoutVisited.Clear();
            sFanoutVisited.Add(initiator);
            FanoutHit_Recursive(initiator, initiator, true);
        }

        /// <summary>
        /// initiator 被命中后,在其 m_SkillDic 子树上扇出 InterruptList_BeHit 跳转。
        /// </summary>
        private void FanoutBeHit_ToSkills(ActionEngine_Unit initiator)
        {
            if (initiator == null) return;
            sFanoutVisited.Clear();
            sFanoutVisited.Add(initiator);
            FanoutHit_Recursive(initiator, initiator, false);
        }

        /// <summary>
        /// 递归遍历 host.m_SkillDic 各 skill 的 ActionStateMachine,对其 part 跑对应的 InterruptList。
        /// </summary>
        /// <param name="host">本层 skill 容器(初次为 initiator,递归时为 skill 自身)</param>
        /// <param name="initiator">最初触发命中/被击的单位,跳转条件以它为基准</param>
        /// <param name="isOnHit">true=OnHit 路径;false=BeHit 路径</param>
        private void FanoutHit_Recursive(ActionEngine_Unit host, ActionEngine_Unit initiator, bool isOnHit)
        {
            if (host == null) return;
            Dictionary<int, List<ActionEngine_Skill>> skillDic = host.m_SkillDic;
            if (skillDic == null || skillDic.Count == 0) return;

            foreach (var kv in skillDic)
            {
                List<ActionEngine_Skill> list = kv.Value;
                if (list == null) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    ActionEngine_Skill skill = list[i];
                    if (skill == null) continue;
                    if (!sFanoutVisited.Add(skill)) continue;

                    if (!skill.TryGetActionStateMachine(out ActionStateMachine skillMachine)) continue;
                    if (skillMachine == null) continue;

                    DispatchHitJump_OnSkill(skillMachine, initiator, isOnHit);

                    // 递归 skill 子树
                    FanoutHit_Recursive(skill, initiator, isOnHit);
                }
            }
        }

        /// <summary>
        /// 在 skill 自身的所有可用 ActionStatePart 上扫描 InterruptList,匹配条件后写值并跳转。
        /// </summary>
        private void DispatchHitJump_OnSkill(ActionStateMachine skillMachine, ActionEngine_Unit initiator, bool isOnHit)
        {
            List<ActionStatePart> parts = skillMachine.mIsSkill
                ? skillMachine.AllActionStatePart_Tmp
                : skillMachine.AllActionStatePart;
            if (parts == null) return;

            for (int p = 0; p < parts.Count; p++)
            {
                ActionStatePart part = parts[p];
                if (part == null) continue;
                if (!part.ActionEnble) continue;
                ActionState state = part.CurrentActionState;
                if (state == null) continue;

                List<ActionInterrupt> trackList = isOnHit ? state.InterruptList_OnHit : state.InterruptList_BeHit;
                if (trackList == null || trackList.Count == 0) continue;

                for (int t = 0; t < trackList.Count; t++)
                {
                    ActionInterrupt interrupt = trackList[t];
                    if (!CheckValueTrack(interrupt, (int)part.ElapsedTime)) continue;
                    if (!part.TryCheckInterrupCondition(interrupt, initiator,
                            out _, out int targetActionID, out _))
                        continue;

                    // host != initiator 时把 initiator 的命中数据同步到 host(skill)
                    ActionEngine_Unit host = skillMachine.CurUnit;
                    if (host != initiator)
                    {
                        if (isOnHit)
                        {
                            skillMachine.HitUnit = initiator.ActionStateMachine.HitUnit;
                            skillMachine.OnHitObject = initiator.ActionStateMachine.OnHitObject;
                            skillMachine.CurOnHitValid = initiator.ActionStateMachine.CurOnHitValid;
                        }
                        else
                        {
                            skillMachine.AttackerUnit = initiator.ActionStateMachine.AttackerUnit;
                        }
                    }

                    if (isOnHit)
                    {
                        part.ChangeState(targetActionID,
                            interrupt.CrossFadeTime,
                            interrupt.OffsetTime,
                            part.CurrentActionState);
                    }
                    else
                    {
                        part.ChangeState(targetActionID,
                            interrupt.CrossFadeTime,
                            interrupt.OffsetTime);
                    }
                    break; // 单 part 单跳
                }
            }
        }

    }
}
