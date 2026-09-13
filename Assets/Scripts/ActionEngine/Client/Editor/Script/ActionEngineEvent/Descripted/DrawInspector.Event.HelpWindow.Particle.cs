using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    public partial class DrawInspectorHelpWindow
    {
        private static Vector2 m_ParticleScrollPos;
        private static bool m_PtFoldOverview = true;
        private static bool m_PtFoldProperties = true;
        private static bool m_PtFoldLifecycle = true;
        private static bool m_PtFoldUseCases = true;
        private static bool m_PtFoldNotes = true;

        private static void DrawParticle(Event_PlayParticle _particle)
        {
            HelpWindowDrawUtility.UpdateAnimTime();

            m_ParticleScrollPos = EditorGUILayout.BeginScrollView(m_ParticleScrollPos);

            HelpWindowDrawUtility.DrawTitle("Particle Effect", "粒子特效事件", "Event_PlayParticle", new Color(1f, 0.6f, 0.2f));
            DrawPt_Overview();
            DrawPt_Properties(_particle);
            DrawPt_Lifecycle();
            DrawPt_UseCases();
            DrawPt_Notes();

            GUILayout.Space(20);
            EditorGUILayout.EndScrollView();
        }

        #region Particle Sections

        private static void DrawPt_Overview()
        {
            HelpWindowDrawUtility.DrawOverview(ref m_PtFoldOverview,
                "在动作时间轴上播放粒子特效。\n\n" +
                "核心能力：\n" +
                "  ● 通过对象池异步加载粒子预制体，自动管理生命周期\n" +
                "  ● 支持挂点绑定或蓝图定义的自定义位置\n" +
                "  ● 支持单次发射或按间隔循环发射\n" +
                "  ● 支持位置/旋转偏移和缩放控制（固定值或蓝图动态值）\n" +
                "  ● 可选始终跟随单位移动");
        }

        private static void DrawPt_Properties(Event_PlayParticle _particle)
        {
            m_PtFoldProperties = HelpWindowDrawUtility.DrawSectionHeader("  属性说明", m_PtFoldProperties, new Color(0.8f, 0.6f, 0.1f));
            if (!m_PtFoldProperties) return;

            HelpWindowDrawUtility.DrawPropertyItem(
                "粒子特效",
                "粒子预制体的资源路径。\n" +
                "通过 EngineResourcesManager.CreactObjToComponent 从对象池异步加载。\n" +
                "预制体上必须挂载 ActionEngine_Effects 组件。",
                "此项为[<color=#ffcc00>必填项</color>]（Required），路径为空时 Enter 阶段会报错。",
                string.IsNullOrEmpty(_particle.PartoclePath) ? "当前：未配置（错误）" : $"当前：{_particle.PartoclePath}",
                string.IsNullOrEmpty(_particle.PartoclePath) ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.9f, 0.3f));

            HelpWindowDrawUtility.DrawPropertyItem(
                "始终跟随",
                "开启后，特效在 Update 阶段每帧跟随单位的挂点位置更新。\n" +
                "关闭时，特效仅在 Enter 时设置一次位置，之后不再跟随。",
                "适用于需要[<color=#ffcc00>粘附在角色身上</color>]的特效，如 Buff 光环、持续燃烧等。",
                _particle.AlwaysFollow ? "当前：每帧跟随" : "当前：仅初始定位",
                _particle.AlwaysFollow ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.6f, 0.6f, 0.6f));

            HelpWindowDrawUtility.DrawPropertyItem(
                "粒子寿命(s)",
                "特效的存活时间（秒）。\n\n" +
                "• > 0 时：Exit 时仅停止发射（Stop），等待自然消亡\n" +
                "    Update 期间持续刷新寿命，保持存活\n" +
                "• ≤ 0 时：Exit 时立即回收到对象池（RemoveComponent）",
                "设为 0 可实现[<color=#ffcc00>事件结束即回收</color>]的效果，适合严格同步的特效。",
                $"当前值：{_particle.Life:F2}s",
                _particle.Life > 0 ? new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.6f, 0.3f));

            HelpWindowDrawUtility.DrawPropertyItem(
                "粒子发射间隔(s)(0为仅发射一次)",
                "控制特效发射频率：\n\n" +
                "• = 0 → 仅在 Enter 时发射一次（Play）\n" +
                "• > 0 → 每隔指定秒数调用一次 Play，循环发射\n" +
                "    Update / LateUpdate 阶段累计计时",
                "循环发射适用于[<color=#ffcc00>持续性效果</color>]，如连续的火花、治疗光圈脉冲等。",
                Mathf.Approximately(_particle.Interval, 0) ? "当前：仅发射一次" : $"当前：每 {_particle.Interval:F2}s 发射一次",
                Mathf.Approximately(_particle.Interval, 0) ? new Color(0.6f, 0.8f, 1f) : new Color(0.3f, 0.9f, 0.6f));

            HelpWindowDrawUtility.DrawPropertyItem(
                "使用蓝图定义特效位置",
                "开启后，特效的位置由蓝图节点 [<color=#ffcc00>粒子位置设定</color>] 动态计算，\n" +
                "忽略 [目标挂点] 和 [位置偏移] 设定。\n\n" +
                "关闭时，使用 [目标挂点] + [位置偏移] + [角度偏移] 确定特效位置。",
                "适用于需要[<color=#ffcc00>动态计算位置</color>]的场景，如追踪弹道起点、蓝图公式计算的坐标。",
                _particle.IsUseBluePrintPoint ? "当前：蓝图定位" : "当前：挂点定位",
                _particle.IsUseBluePrintPoint ? new Color(0.3f, 0.9f, 0.6f) : new Color(0.6f, 0.8f, 1f));

            if (_particle.IsUseBluePrintPoint)
            {
                HelpWindowDrawUtility.DrawPropertyItem(
                    "    粒子位置设定",
                    "蓝图节点（GraphEvent_NoValue_Point），运行时返回 PointData（pos + rot）。\n" +
                    "特效的位置和旋转直接使用该蓝图的输出值。",
                    null,
                    "当前：蓝图节点",
                    new Color(0.8f, 0.8f, 0.3f));
            }
            else
            {
                HelpWindowDrawUtility.DrawPropertyItem(
                    "    目标挂点",
                    "特效绑定的角色挂点位置 (ECharacteLimbType)。\n" +
                    "从单位的 CharacterConfig.HelpPointDic 中查找对应的 Transform。\n" +
                    "如果挂点不存在，回退到单位根 Transform。",
                    null,
                    $"当前：{(ECharacteLimbType)_particle.PartPointType}",
                    new Color(0.6f, 0.8f, 1f));
            }

            HelpWindowDrawUtility.DrawPropertyItem(
                "位置偏移",
                "相对于挂点的本地坐标偏移量（Vector3）。\n" +
                "通过 TransformPoint 转换为世界坐标。",
                null,
                $"当前：({_particle.OffsetPos.x:F2}, {_particle.OffsetPos.y:F2}, {_particle.OffsetPos.z:F2})",
                new Color(0.6f, 0.8f, 1f));

            HelpWindowDrawUtility.DrawPropertyItem(
                "角度偏移",
                "相对于挂点的旋转偏移量（欧拉角 Vector3）。\n" +
                "叠加到挂点的 rotation 上。",
                null,
                $"当前：({_particle.OffsetRot.x:F2}, {_particle.OffsetRot.y:F2}, {_particle.OffsetRot.z:F2})",
                new Color(0.6f, 0.8f, 1f));

            HelpWindowDrawUtility.DrawPropertyItem(
                "使用蓝图缩放",
                "控制缩放的来源：\n\n" +
                "• 开启 → 使用 [<color=#ffcc00>缩放(全比例)</color>] 蓝图节点返回的 float 值，等比缩放\n" +
                "• 关闭 → 使用 [<color=#ffcc00>缩放</color>] 的固定 Vector3 值",
                null,
                _particle.UseBluePrint_Scale ? "当前：蓝图缩放" : "当前：固定缩放",
                _particle.UseBluePrint_Scale ? new Color(0.3f, 0.9f, 0.6f) : new Color(0.6f, 0.8f, 1f));

            if (_particle.UseBluePrint_Scale)
            {
                HelpWindowDrawUtility.DrawPropertyItem(
                    "    缩放(全比例)",
                    "蓝图节点（GraphEvent_NoValue_Float），运行时返回 float 值。\n" +
                    "等比应用于 localScale（Vector3.one * value）。",
                    null,
                    "当前：蓝图节点",
                    new Color(0.8f, 0.8f, 0.3f));
            }
            else
            {
                HelpWindowDrawUtility.DrawPropertyItem(
                    "    缩放",
                    "固定的 localScale 值（Vector3）。\n" +
                    "直接赋值给特效的 transform.localScale。",
                    null,
                    $"当前：({_particle.LocalScale.x:F2}, {_particle.LocalScale.y:F2}, {_particle.LocalScale.z:F2})",
                    new Color(0.6f, 0.8f, 1f));
            }

            GUILayout.Space(6);
        }

        private static void DrawPt_Lifecycle()
        {
            m_PtFoldLifecycle = HelpWindowDrawUtility.DrawSectionHeader("  执行流程", m_PtFoldLifecycle, new Color(0.3f, 0.7f, 0.4f));
            if (!m_PtFoldLifecycle) return;

            float flowAlpha = 0.7f + Mathf.PingPong(HelpWindowDrawUtility.AnimTime * 0.8f, 0.3f);

            HelpWindowDrawUtility.DrawFlowPhase("Enter（进入）", new Color(0.3f, 0.8f, 0.4f, flowAlpha), new[]
            {
                "① 通过对象池异步加载 [粒子特效] 预制体",
                "② 加载完成回调：",
                "   ├─ 技能实体（IsTem）→ 注册到 InstanceComponent，按实体坐标定位",
                "   └─ 普通单位 → 按 [目标挂点] 或 [蓝图位置] 设置位置",
                "③ 设置缩放（固定值 / 蓝图值）",
                "④ 单帧事件或仅发射一次 → 立即 Play()"
            });

            HelpWindowDrawUtility.DrawFlowArrow();

            HelpWindowDrawUtility.DrawFlowPhase("Update（普通单位持续更新）", new Color(0.8f, 0.7f, 0.2f, flowAlpha), new[]
            {
                "● 持续刷新 [粒子寿命]，保持特效存活",
                "● [始终跟随] = 开 → 每帧更新位置/旋转/缩放",
                "● 循环发射模式 → 累计时间，达到 [发射间隔] 时 Play()"
            });

            HelpWindowDrawUtility.DrawFlowArrow();

            HelpWindowDrawUtility.DrawFlowPhase("LateUpdate（技能实体持续更新）", new Color(0.6f, 0.7f, 0.9f, flowAlpha), new[]
            {
                "● 仅技能实体（IsTem）时工作",
                "● 通过 InstanceComponent 查找特效对象",
                "● 刷新寿命，跟随实体位置",
                "● 循环发射模式 → 累计时间，达到 [发射间隔] 时 Play()"
            });

            HelpWindowDrawUtility.DrawFlowArrow();

            HelpWindowDrawUtility.DrawFlowPhase("Exit（退出）", new Color(0.8f, 0.3f, 0.3f, flowAlpha), new[]
            {
                "● 分支判断 [粒子寿命]：",
                "   ├─ ≤ 0 → 立即回收：RemoveComponent 归还对象池",
                "   │    └─ 技能实体额外清理 InstanceComponent 注册",
                "   └─ > 0 → 仅停止发射（Stop），等待自然消亡"
            });

            GUILayout.Space(6);
        }

        private static void DrawPt_UseCases()
        {
            m_PtFoldUseCases = HelpWindowDrawUtility.DrawSectionHeader("  使用场景", m_PtFoldUseCases, new Color(0.7f, 0.4f, 0.8f));
            if (!m_PtFoldUseCases) return;

            HelpWindowDrawUtility.DrawUseCaseItem(
                "Particle Effect",
                "攻击拖尾 / 挥砍特效",
                "在攻击动作期间播放一次性挥砍特效。\n" +
                "配置：<color=#ffcc00>粒子发射间隔</color>=0, <color=#ffcc00>目标挂点</color>=武器挂点, <color=#ffcc00>粒子寿命</color>=特效自然时长");

            HelpWindowDrawUtility.DrawUseCaseItem(
                "d_Profiler.NextFrame",
                "持续 Buff 光环",
                "角色身上持续显示的[<color=#ffcc00>环绕光效</color>]，跟随角色移动。\n" +
                "配置：<color=#ffcc00>始终跟随</color>=开, <color=#ffcc00>粒子寿命</color>=0（事件结束即回收）, <color=#ffcc00>目标挂点</color>=Root");

            HelpWindowDrawUtility.DrawUseCaseItem(
                "d_PreMatCylinder",
                "循环脉冲特效",
                "按间隔持续发射的[<color=#ffcc00>脉冲式粒子</color>]，如治疗波、能量涌动。\n" +
                "配置：<color=#ffcc00>粒子发射间隔</color>=0.5, <color=#ffcc00>始终跟随</color>=开");

            HelpWindowDrawUtility.DrawUseCaseItem(
                "d_SceneViewFx",
                "蓝图动态定位",
                "特效位置由[<color=#ffcc00>蓝图节点</color>]运行时动态计算。\n" +
                "配置：<color=#ffcc00>使用蓝图定义特效位置</color>=开, <color=#ffcc00>粒子位置设定</color>=对应蓝图");

            GUILayout.Space(6);
        }

        private static void DrawPt_Notes()
        {
            m_PtFoldNotes = HelpWindowDrawUtility.DrawSectionHeader("  注意事项", m_PtFoldNotes, new Color(0.9f, 0.35f, 0.3f));
            if (!m_PtFoldNotes) return;

            HelpWindowDrawUtility.DrawNoteItem(MessageType.Warning,
                "[粒子特效] 路径为必填项。路径为空或预制体缺少 ActionEngine_Effects 组件时会导致加载失败。");

            HelpWindowDrawUtility.DrawNoteItem(MessageType.Warning,
                "通过对象池创建的特效必须通过 RemoveComponent 归还。\n" +
                "[粒子寿命] ≤ 0 时在 Exit 中自动归还；> 0 时仅 Stop，由对象池根据寿命自动回收。");

            HelpWindowDrawUtility.DrawNoteItem(MessageType.Info,
                "技能实体（IsTem=true）的特效额外注册到 InstanceComponent 中。\n" +
                "Exit 时需同时清理 InstanceComponent 和归还对象池，两者均已在代码中处理。");

            HelpWindowDrawUtility.DrawNoteItem(MessageType.Info,
                "[使用蓝图定义特效位置] 开启时，[目标挂点] / [位置偏移] / [角度偏移] 不生效。\n" +
                "位置完全由蓝图节点（GraphEvent_NoValue_Point）决定。");

            HelpWindowDrawUtility.DrawNoteItem(MessageType.Info,
                "[使用蓝图缩放] 开启时使用全比例（float → Vector3.one * value），\n" +
                "关闭时使用独立三轴 Vector3 缩放值。两者互斥，不会叠加。");

            HelpWindowDrawUtility.DrawNoteItem(MessageType.Info,
                "[始终跟随] 仅对普通单位生效。技能实体的跟随逻辑在 LateUpdate 中独立处理，\n" +
                "始终跟随实体的 Pos / Rot。");

            GUILayout.Space(6);
        }

        #endregion
    }
}
